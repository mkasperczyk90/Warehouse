# Warehouse ↔ DataFoundry integration contract

> This is the producer-side contract for **DataFoundry** (the Databricks lakehouse project that
> consumes this data) — what actually lands in S3 today, in what shape, at what path, with what
> guarantees. For the *why* behind each design choice (cloud-agnostic CDC, Auto Loader vs.
> alternatives, Silver/Gold design, the metrics catalog) see [`BI_Plan.md`](BI_Plan.md) — this file
> only states what DataFoundry can rely on right now.
>
> Nothing below has been deployed to a live AWS environment yet (`BI_LAKE_BUCKET` has never been set
> on a real `terraform apply`) — every guarantee here has been verified locally against real
> Postgres/Debezium/RabbitMQ/S3-compatible (LocalStack) containers, not the real thing. Status per
> row is called out explicitly; nothing is glossed over as "done" that hasn't actually run.

## 1. The landing zone

One S3 bucket, on/off with a single Terraform variable:

- **Bucket**: `warehouse-bi-lake-<account-id>` (override via `bi_lake_bucket_name`,
  `infra/terraform/bootstrap/variables.tf`)
- **Switch**: setting the `BI_LAKE_BUCKET` repo variable turns on *both* producer paths below at
  once (`docs/deploy.md`) — there's no way to enable one without the other today.
- **IAM**: a single shared ECS task role can `s3:PutObject` under this bucket's root; nothing else
  writes to it.

Three independent writers land into the same bucket, under three separate prefixes. DataFoundry's
Auto Loader jobs should point at prefixes, not the bucket root.

| Prefix | Writer | Format | Trigger |
|---|---|---|---|
| `bronze/<source_system>/<event_type>/date=YYYY-MM-DD/<event_id>.parquet` | `S3ParquetBronzeExportWriter` (`Warehouse.ServiceDefaults.BiExport`) — one instance per backend service | Parquet, one row per file | Each service's own published integration events, via a dedicated `<service>.bi-export` RabbitMQ queue |
| `bronze-cdc/<schema>/<table>/date=YYYY-MM-DD/<lsn>.json` | `Warehouse.CdcBridge` (one shared worker) | Raw Debezium change-event JSON (`before`/`after`/`op`/`source`), byte-for-byte, schemas stripped | Postgres logical replication → Debezium Server → RabbitMQ → CdcBridge, for internal state that never becomes an integration event |
| `bronze/identity-service/UserSnapshot/date=YYYY-MM-DD/<event_id>.parquet` | `Warehouse.IdentityBridge` (reuses the same Parquet writer) | Parquet, one row per user per poll | Periodic poll of Keycloak's Admin REST API (no CDC/events possible — Keycloak dev-mode has no persistent DB) |

**Idempotency**: every key is deterministic — the event-driven path keys on the Wolverine outbox
message id, CDC keys on the Postgres LSN (the WAL position of the change). A redelivery from the
at-least-once outbox, or from Debezium, overwrites the same S3 object rather than producing a
duplicate row. DataFoundry's ingestion does **not** need its own dedup step for either path.

**Schema-on-read**: every Parquet column (`bronze/...`) is written as a string, including numbers and
timestamps (ISO-8601, `"O"` format) — typing happens in Silver, not Bronze, by design. The CDC path
is native JSON with no imposed schema at all; Debezium's own envelope carries the typed `before`/
`after` row images.

## 2. Common envelope (event-driven path only)

Every `bronze/...` Parquet file carries these columns before the event's own fields:

```
_source_system : string   // "warehouse-service" | "logistics-service" | "masterdata-service" | "identity-service"
_event_type    : string   // e.g. "GoodsReceivedV1"
_event_id      : string   // Wolverine's envelope id — the S3 key's dedup key
_occurred_at   : string   // ISO-8601 — when the event happened, not when it was exported; partitions by business date
```

The CDC path has no equivalent envelope of its own — `source.schema`/`source.table`/`source.lsn`/
`ts_ms` inside Debezium's native JSON serve the same role.

## 3. Event-driven catalog (`bronze/`) — every field, as actually implemented

One row per published integration event that has a BiExport consumer today. Field names here are the
literal Parquet column names (payload keys), not the aspirational Bronze table shapes in
`BI_Plan.md` §5 — those two mostly agree, but this table is the wire truth.

### `warehouse-service` (queue `warehousing.bi-export`, exchange `inventory`)

| `_event_type` | Payload columns |
|---|---|
| `GoodsReceivedV1` | `delivery_id`, `warehouse_code`, `buffer_location`, `line_count` |
| `PutAwayCompletedV1` | `delivery_id` |
| `StockReservedV1` | `order_id`, `fully` |
| `PicksPlannedV1` | `order_id`, `pick_count`, `picks_json` (nested array as a JSON string — explode in Silver) |

### `logistics-service` (queue `logistics.bi-export`, exchange `logistics`)

| `_event_type` | Payload columns |
|---|---|
| `GoodsReceiptConfirmedV1` | `delivery_id`, `warehouse_code`, `line_count`, `lines_json` (nested — explode in Silver) |
| `DeliveryAnnouncedV1` | `delivery_id`, `warehouse_code`, `supplier_party_id`, `planned_at` |
| `DockSlotAssignedV1` | `delivery_id`, `warehouse_code`, `dock_code`, `dock_slot_at`, `dock_slot_to` |
| `DeliveryArrivedV1` | `delivery_id`, `warehouse_code`, `arrived_at` |
| `OutboundOrderPlacedV1` | `order_id`, `customer_role_id`, `warehouse_code`, `line_count`, `lines_json` (nested) |
| `OutboundOrderCancelledV1` | `order_id` |
| `PickingReleasedV1` | `order_id`, `warehouse_code` |
| `PickConfirmedV1` | `order_id`, `sequence`, `location`, `sku`, `batch_number`, `quantity`, `unit` |
| `ShipmentDispatchedV1` | `order_id`, `shipment_id`, `warehouse_code`, `carrier_role_id`, `tracking_number` (nullable) |

`DeliveryAnnouncedV1`/`DockSlotAssignedV1`/`DeliveryArrivedV1` together give DataFoundry the full
inbound-delivery timeline (`announced_at`/`dock_code`/`dock_slot_at`/`arrived_at`) that
`GoodsReceiptConfirmedV1` alone didn't cover — join all four on `delivery_id` in Silver to build one
`fact_inbound_delivery` row per delivery. Note: `supplier_party_id`/`customer_role_id`/
`carrier_role_id` are **opaque `PartyRoleRef` strings today** (dev-seed free-text labels, not a real
Partner id/code) — resolving them to a Partner dimension needs the party-role crosswalk work, not yet
built.

### `masterdata-service` (queue `masterdata.bi-export`, exchange `catalog`)

| `_event_type` | Payload columns |
|---|---|
| `ProductDefinedV2` | `sku`, `name`, `base_unit`, `requires_cold_chain`, `is_hazardous`, `is_batch_tracked`, `unit_weight_kg`, `unit_volume_m3`, `min_celsius` (nullable), `max_celsius` (nullable) |

### Identity (`bronze/identity-service/`)

| `_event_type` | Payload columns |
|---|---|
| `UserSnapshot` | `user_id` (Keycloak `username`, the badge number), `display_name` (nullable), `role` (comma-joined if multiple, nullable), `home_warehouse_code` (nullable), `active` |

## 4. CDC catalog (`bronze-cdc/`) — tables configured, per Debezium source instance

CDC exists for internal state that **never crosses a service boundary as an integration event** —
adding an event purely to feed BI would couple domain code to an analytics concern it has no other
reason to know about (see `BI_Plan.md` §11 for the full rationale). One Debezium Server instance per
source database, each with its own `table.include.list`; DataFoundry gets the raw row image, not an
intent-carrying fact, so dedup/point-in-time logic belongs entirely in Silver.

| Source DB | Tables | Status |
|---|---|---|
| `warehouse` | `inventory.stock_items` | **Verified** — a real Postgres `INSERT` flowed through a real Debezium container to S3 in a local smoke test |
| `warehouse` | `inventory.stock_movements`, `stock_reservations`, `batches`, `stocktakes`, `stocktake_count_lines`, `allocations`, `handling_units`, `handling_unit_lines` | Configured, same instance/config as the verified table — not individually run |
| `warehouse` | `topology.warehouses`, `rooms`, `locations`, `docks` | Configured, not run |
| `logistics` | `inbound_deliveries`, `inbound_delivery_lines`, `outbound_orders`, `outbound_order_lines`, `pick_lists`, `pick_tasks`, `shipments`, `packages` | Configured only — this Debezium instance has never been started, even in the smoke test |
| `masterdata` | `partners.parties`, `party_roles`, `customer_shipping_addresses`, `catalog.product_types` | Configured only — same, never run |

`catalog.product_unit_conversions` is deliberately **not** in scope — nobody has asked for it.

**Important overlap to know about**: `logistics.inbound_deliveries` is in the CDC list above *and*
`DeliveryAnnouncedV1`/`DockSlotAssignedV1`/`DeliveryArrivedV1` (§3) cover much of the same columns via
events instead. That's a deliberate, documented exception (`BI_Plan.md` §11) — those three are named
business milestones, cheaper for Silver to consume as facts than re-derived from a row diff — not an
oversight. Don't build a Silver transform that expects the CDC path to be the only source for those
columns.

## 5. What's deliberately not here yet

- **ASN accuracy** (`expected_qty`/`received_qty`/`discrepancy_qty` per delivery line) — the delivery
  *timeline* (§3) is covered; the line-level expected-vs-actual comparison from UC-02 is a separate,
  not-yet-built slice.
- **Cost/valuation** (`stock_value`, `Money` anywhere) — no source system carries cost data yet; a
  future payments service is the anticipated origin (`BI_Plan.md` §9).
- **Delivery/shipment confirmation past `Dispatched`** — out of scope for this domain today; proposed
  as a separate future `fleet-telemetry-service` (`BI_Plan.md` §10).
- **Party-role → Partner resolution** — `PartyRoleRef` fields travel as opaque strings; a crosswalk
  resolving them to real Partner attributes is a real backlog item, blocked on real Partner
  ids/codes reaching the producing services' seed/prod data.
- **No Databricks-side objects exist yet** — no workspace, no Auto Loader job, no Bronze/Silver/Gold
  Delta tables. Everything above describes what the Warehouse side delivers into S3; wiring
  Databricks to actually read it is DataFoundry's next step, not built from this repo.

## 6. Versioning contract

Every event name is suffixed `V<n>` (e.g. `GoodsReceiptConfirmedV1`). Wire shapes are **additive-only
and immutable**: a new field ships as a new `V<n+1>` event, never as an edit to an existing one, and
old and new versions can be in flight at the same time. DataFoundry's Silver mappings should key on
`_event_type` (the exact versioned name), not assume a field list is stable across versions of "the
same" event.
