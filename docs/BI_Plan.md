# BI Plan — Databricks Lakehouse (Medallion) for Warehouse

> Grounded in the actual domain model — see [`01-domain-overview.md`](01-domain-overview.md),
> [`02-bounded-contexts.md`](02-bounded-contexts.md) and [`04-domain-model.md`](04-domain-model.md).
> This document proposes a **Bronze / Silver / Gold** lakehouse on Databricks that feeds **Power BI**,
> written from the point of view of a data analyst who has to build reports and dashboards on top of it.
>
> **Scope of this document:** data models only (schemas, grains, keys). No ETL/transformation code,
> no Spark/SQL. The "how to load it" is a separate, later concern.

## 1. Who this is for, and what they actually ask for

A Power BI analyst working against this domain typically gets asked questions like:

- "How much stock do we have, where, and is it aging out?"
- "Are we receiving what suppliers promised, on time?"
- "Are we shipping what customers ordered, on time and in full?"
- "Where is the warehouse close to capacity, and where is it underused?"
- "How much stock do we write off to expiry, and why?"
- "Which operators/shifts are most productive?"

None of those questions are answerable from a single microservice's database — they all require
**joining across bounded contexts** (Catalog + Topology + Inventory + Logistics + Partners) and
**over time** (trends, not just current state). That's exactly the job of the lakehouse: each
service already publishes versioned domain events via the transactional outbox
(`02-bounded-contexts.md` §Messaging) — those events, plus periodic full extracts of current
state, are the source for Bronze.

## 2. Personas

| Persona | Cadence | What they need in Power BI |
|---|---|---|
| **Warehouse Manager** | Daily | Stock levels & value by warehouse/room, capacity utilization, aging/expiry risk, stocktake variance |
| **Logistics Coordinator** | Daily/hourly | Inbound ASN vs. actual receipt, dock schedule adherence, outbound order status funnel, OTIF |
| **Quality Inspector / Compliance** | Weekly | QC hold rate & duration, cold-chain compliance, batch rejection trends |
| **Ops Director / Exec** | Weekly/monthly | Cross-warehouse KPIs, trend lines, cost-relevant write-offs, carrier performance |
| **Finance** (once Payments exists, §9) | Monthly | Inventory carrying value, payment/COD collection, chargebacks, DSO |

## 3. Source systems → Bronze

| Source | Mechanism | What lands in Bronze |
|---|---|---|
| `masterdata-service` (Catalog + Partners) | Outbox events (`ProductDefined`, ...) + periodic full snapshot | Product, Partner (+roles) |
| `warehouse-service` (Inventory + Topology) | Outbox events (`StockReceived`, `StockPicked`, ...) + periodic full snapshot | Warehouse, Room, Location, StockItem, StockMovement, Batch, StockReservation, Allocation, Stocktake |
| `logistics-service` | Outbox events (`DeliveryAnnounced`, `OrderReserved`, `ShipmentDispatched`, ...) + periodic full snapshot | InboundDelivery(+lines), OutboundOrder(+lines), PickList, Shipment |
| Identity (Keycloak) | Periodic poll — `Warehouse.IdentityBridge`, §11 | User, Role (for `DimUser`) — **built**, smoke-tested against a real Keycloak container |

Each Bronze table is an **append-only, minimally-transformed** copy of the source event/table, plus
ingestion metadata. No joins, no dedup, no business logic in Bronze — that's Silver's job.

## 4. Medallion overview

```
Bronze  — raw, 1:1 with source events/tables, append-only, schema-on-read tolerant
Silver  — conformed, deduplicated, typed, one row = one current business fact/entity
Gold    — star schema (dimensions + facts), built for Power BI: pre-aggregatable, wide, denormalized
```

---

## 5. Bronze layer — raw ingestion models

Every Bronze table carries the same ingestion envelope on top of the source fields:

```
_ingested_at        : timestamp   // when the lakehouse received it
_source_system       : string     // "warehouse-service" | "logistics-service" | "masterdata-service" | "identity"
_event_type          : string     // e.g. "StockReceivedV1", or "snapshot" for full extracts
_event_id            : string     // dedup key for event-sourced tables (idempotent re-ingest)
_schema_version      : int
```

### 5.1 From `masterdata-service`

```
BronzeProduct(
  sku, name, ean, category, length_cm, width_cm, height_cm,
  unit_weight_kg, base_unit, temp_min_c, temp_max_c, requires_cold_chain,
  is_hazardous, is_batch_tracked, has_expiry_date,
  event_type            // "ProductDefined" | "ProductStorageChanged" | "snapshot"
)

BronzeParty(
  party_id, name, tax_id, contact_email, contact_phone,
  role_type,            // "Supplier" | "Customer" | "Carrier"
  role_code,            // SupplierCode / CustomerCode / CarrierCode
  shipping_addresses,   // raw array, customers only
  service_levels        // raw array, carriers only
)
```

### 5.2 From `warehouse-service`

```
BronzeWarehouse(warehouse_code, name, address_line, city, country)

BronzeRoom(warehouse_code, room_code, room_type, temp_min_c, temp_max_c, humidity_controlled)

BronzeLocation(warehouse_code, room_code, location_code, capacity_m3, max_load_kg, location_kind)

BronzeStockItem(          // periodic full snapshot — current state, not history
  stock_item_id, sku, batch_number, location_code, warehouse_code,
  on_hand_qty, allocated_qty, uom, status   // Available | Quarantine | Blocked
)

BronzeStockMovement(       // event-sourced, append-only — this IS the ledger
  movement_id, movement_type, sku, batch_number,
  from_location_code, to_location_code, warehouse_code,
  quantity, uom, performed_by_user_id, occurred_at, reason
)

BronzeBatch(batch_id, sku, batch_number, expiry_date, quality_status, supplier_ref)

BronzeStockReservation(
  reservation_id, sku, warehouse_code, order_ref,
  promised_qty, allocated_qty, uom, status, created_at, updated_at
)

BronzeAllocation(
  allocation_id, reservation_id, order_ref, sku, batch_number, location_code,
  quantity, uom, status, created_at
)

BronzeStocktake(
  stocktake_id, warehouse_code, scope_location_codes, status,
  approved_at, approved_by_user_id
)

BronzeStocktakeCountLine(
  stocktake_id, sku, location_code, batch_number,
  system_qty, counted_qty, uom
)
```

### 5.3 From `logistics-service`

```
BronzeInboundDelivery(
  delivery_id, supplier_party_id, warehouse_code, dock_code, dock_slot_at,
  status, announced_at, arrived_at, receipt_confirmed_at
)

BronzeInboundDeliveryLine(
  delivery_id, line_no, sku, expected_qty, received_qty, batch_number,
  discrepancy_qty, uom
)

BronzeOutboundOrder(
  order_id, customer_party_id, warehouse_code, ship_to_address,
  status, created_at, requested_ship_at
)

BronzeOutboundOrderLine(order_id, line_no, sku, ordered_qty, uom)

BronzePickTask(
  order_id, pick_list_id, task_seq, sku, batch_number, location_code,
  qty, status, picked_at, picked_by_user_id
)

BronzeShipment(
  shipment_id, order_id, carrier_party_id, tracking_number,
  package_count, total_weight_kg, dispatched_at, delivered_at, status
)
```

### 5.4 From Identity

```
BronzeUser(user_id, display_name, role, home_warehouse_code, active)
```

**Built** (§11's Identity-bridge subsection has the implementation details). Keycloak runs in
dev-mode in this stack with no persistent database (`start-dev`, embedded storage) — CDC can't reach
it, so this is a genuine periodic poll of Keycloak's Admin REST API, not row-level replication.
Field mapping (Keycloak `UserRepresentation` → `BronzeUser`):

| `BronzeUser` column | Keycloak field | Notes |
|---|---|---|
| `user_id` | `username` | The badge number in this repo (e.g. `"7700"`) — identical to `attributes.badge` |
| `display_name` | `firstName` + `lastName` | Concatenated by the exporter (not left for Silver to join) |
| `role` | `realmRoles` | Requires a second call per user — `GET /users/{id}/role-mappings/realm`, since `UserRepresentation` doesn't include roles itself. Multiple roles join as a comma-separated string (this repo's grain is one row per user, not one row per role) |
| `home_warehouse_code` | `attributes.default_warehouse` | Custom attribute, e.g. `"WH01"` — matches `WarehouseCode` from Topology |
| `active` | `enabled` | Verbatim |

---

## 6. Silver layer — conformed entities

Silver deduplicates (by natural/business key), resolves late-arriving snapshots vs. events into a
single current-state row per entity, types everything properly (dates, decimals with units), and
starts enforcing **referential integrity** across the sources that Bronze can't (a `SilverStockMovement`
row's `sku` must exist in `SilverProduct`, etc.). One row = one current fact, in business terms —
not yet shaped for BI (that's Gold).

```
SilverProduct(
  sku PK, name, ean, category, length_cm, width_cm, height_cm, unit_volume_m3 (derived),
  unit_weight_kg, base_unit, temp_min_c, temp_max_c, requires_cold_chain, is_hazardous,
  is_batch_tracked, has_expiry_date,
  valid_from, valid_to, is_current           // SCD2 — category/storage requirements do change
)

SilverPartner(
  party_id PK, name, tax_id, contact_email, contact_phone
)

SilverPartnerRole(
  party_id FK, role_type PK, role_code, service_levels, shipping_addresses
)

SilverWarehouse(warehouse_code PK, name, city, country)

SilverRoom(warehouse_code FK, room_code PK, room_type, temp_min_c, temp_max_c, humidity_controlled)

SilverLocation(
  warehouse_code FK, room_code FK, location_code PK, capacity_m3, max_load_kg, location_kind
)

SilverBatch(
  batch_id PK, sku FK, batch_number, expiry_date, quality_status, supplier_ref
)

SilverStockItem(                              // current-state, deduplicated snapshot
  stock_item_id PK, sku FK, batch_number FK, location_code FK, warehouse_code FK,
  on_hand_qty, allocated_qty, available_qty (derived), uom, status, as_of
)

SilverStockMovement(                          // deduped ledger, one row per movement_id
  movement_id PK, movement_type, sku FK, batch_number FK,
  from_location_code, to_location_code, warehouse_code FK,
  quantity, uom, performed_by_user_id FK, occurred_at, reason
)

SilverStockReservation(
  reservation_id PK, sku FK, warehouse_code FK, order_ref,
  promised_qty, allocated_qty, outstanding_qty (derived), uom, status, created_at, updated_at
)

SilverAllocation(
  allocation_id PK, reservation_id FK, order_ref, sku FK, batch_number FK, location_code FK,
  quantity, uom, status, created_at
)

SilverStocktake(stocktake_id PK, warehouse_code FK, status, approved_at, approved_by_user_id FK)

SilverStocktakeCountLine(
  stocktake_id FK, sku FK, location_code FK, batch_number FK,
  system_qty, counted_qty, variance_qty (derived), uom
)

SilverInboundDelivery(
  delivery_id PK, supplier_party_id FK, warehouse_code FK, dock_code,
  status, announced_at, arrived_at, receipt_confirmed_at
)

SilverInboundDeliveryLine(
  delivery_id FK, line_no PK, sku FK, expected_qty, received_qty, batch_number FK,
  discrepancy_qty (derived), uom
)

SilverOutboundOrder(
  order_id PK, customer_party_id FK, warehouse_code FK, ship_to_country, ship_to_city,
  status, created_at, requested_ship_at
)

SilverOutboundOrderLine(order_id FK, line_no PK, sku FK, ordered_qty, uom)

SilverPickTask(
  order_id FK, pick_list_id, task_seq PK, sku FK, batch_number FK, location_code FK,
  qty, status, picked_at, picked_by_user_id FK
)

SilverShipment(
  shipment_id PK, order_id FK, carrier_party_id FK, tracking_number,
  package_count, total_weight_kg, dispatched_at, delivered_at, status
)

SilverUser(user_id PK, display_name, role, home_warehouse_code FK, active)
```

---

## 7. Gold layer — star schema for Power BI

Wide, denormalized, one star per subject area, surrogate integer keys, conformed dimensions shared
across facts (`DimProduct`, `DimWarehouse`, `DimPartner`, `DimDate` are reused by every fact below).

### 7.1 Dimensions

```
DimDate(date_key PK, date, year, quarter, month, month_name, iso_week, day_of_week, is_weekend)

DimProduct(                                    // SCD2
  product_key PK, sku, name, ean, category, unit_volume_m3, unit_weight_kg, base_unit,
  requires_cold_chain, is_hazardous, is_batch_tracked, has_expiry_date,
  valid_from, valid_to, is_current
)

DimWarehouse(warehouse_key PK, warehouse_code, name, city, country)

DimRoom(room_key PK, warehouse_key FK, room_code, room_type, temp_min_c, temp_max_c)

DimLocation(
  location_key PK, room_key FK, warehouse_key FK, location_code, location_kind,
  capacity_m3, max_load_kg
)

DimPartner(                                    // one row per party+role — a customer who is also
  partner_key PK, party_id, name, country,     // a supplier gets two rows, same party_id
  role_type,                                   // "Supplier" | "Customer" | "Carrier"
  role_code, service_level
)

DimBatch(batch_key PK, batch_number, product_key FK, expiry_date, quality_status)

DimUser(user_key PK, user_id, display_name, role, home_warehouse_key FK)

DimMovementType(movement_type_key PK, code, description, direction)  // In | Out | Transfer | Adjustment
```

### 7.2 Facts

```
FactStockMovement                              // grain: one row per StockMovement ledger entry
(
  movement_id (degenerate),
  date_key FK, occurred_at,
  product_key FK, batch_key FK,
  from_location_key FK, to_location_key FK, warehouse_key FK,
  movement_type_key FK, performed_by_user_key FK,
  quantity, uom
)

FactStockSnapshot                              // grain: product+batch+location, per day (periodic snapshot)
(
  date_key FK, product_key FK, batch_key FK, location_key FK, warehouse_key FK,
  on_hand_qty, allocated_qty, available_qty, days_until_expiry, stock_value  // stock_value once cost data exists
)

FactInboundReceipt                             // grain: one row per delivery line, at receipt
(
  delivery_id (degenerate), line_no,
  announced_date_key FK, arrived_date_key FK, receipt_confirmed_date_key FK,
  supplier_partner_key FK, warehouse_key FK, product_key FK,
  expected_qty, received_qty, discrepancy_qty, uom,
  dock_to_stock_hours, lead_time_days                        // announced -> arrived
)

FactOutboundFulfillment                        // grain: one row per order line
(
  order_id (degenerate), line_no,
  order_date_key FK, requested_ship_date_key FK, actual_ship_date_key FK,
  customer_partner_key FK, warehouse_key FK, product_key FK, carrier_partner_key FK,
  ordered_qty, allocated_qty, picked_qty, shipped_qty, uom,
  order_cycle_time_hours, on_time_flag, in_full_flag
)

FactShipment                                   // grain: one row per shipment
(
  shipment_id (degenerate), tracking_number (degenerate),
  dispatched_date_key FK, delivered_date_key FK,
  carrier_partner_key FK, warehouse_key FK,
  package_count, total_weight_kg, transit_days, otif_flag
)

FactReservation                                // grain: one row per reservation, latest state
(
  reservation_id (degenerate), order_ref (degenerate),
  date_key FK, product_key FK, warehouse_key FK,
  promised_qty, allocated_qty, outstanding_qty, uom, status
)

FactStocktakeVariance                          // grain: one row per count line
(
  stocktake_id (degenerate),
  date_key FK, product_key FK, location_key FK, warehouse_key FK,
  system_qty, counted_qty, variance_qty, variance_pct, uom
)

FactQualityHold                                // grain: one row per QC state change (quarantine/block/release)
(
  date_key FK, product_key FK, batch_key FK, warehouse_key FK,
  quality_status, reason, duration_hours, qty_affected, uom
)

FactLaborActivity                              // grain: one row per user+day+warehouse (aggregated)
(
  date_key FK, user_key FK, warehouse_key FK,
  tasks_completed, units_moved, avg_task_duration_seconds
)
```

---

## 8. Metrics & KPI catalog

| Area | Metric | Definition (Gold source) |
|---|---|---|
| **Inventory** | Inventory accuracy % | `1 − Σ|variance_qty| / Σ system_qty` from `FactStocktakeVariance` |
| | Stock turnover ratio | units shipped (period) / avg `on_hand_qty` (`FactStockSnapshot`) |
| | Days of inventory on hand (DOH) | avg `on_hand_qty` / avg daily units shipped |
| | Dead / slow-moving stock | SKUs in `FactStockSnapshot` with no `FactStockMovement` in N days |
| | Expiry risk value | Σ `stock_value` where `days_until_expiry` < threshold |
| | Capacity utilization % | Σ used volume (from stock at a location) / `DimLocation.capacity_m3` |
| | Cold-chain storage compliance % | % of time a cold-chain SKU's location room stayed within `temp_min_c..temp_max_c` |
| | Adjustment frequency | count of `FactStockMovement` where `movement_type = Adjustment`, by reason |
| **Inbound** | Dock-to-stock time | `dock_to_stock_hours` avg, from `FactInboundReceipt` |
| | ASN accuracy % | 1 − `discrepancy_qty` / `expected_qty` |
| | Supplier on-time delivery % | arrivals within promised slot / total deliveries, by `supplier_partner_key` |
| | Receiving discrepancy rate | lines with `discrepancy_qty ≠ 0` / total lines |
| **Outbound / Fulfillment** | Order cycle time | avg `order_cycle_time_hours` |
| | OTIF % | `on_time_flag AND in_full_flag` / total order lines |
| | Fill rate % | Σ `shipped_qty` / Σ `ordered_qty` |
| | Backorder rate | order lines with `shipped_qty < ordered_qty` at expected ship date / total |
| | Pick accuracy % | picked qty matching order line / total picks (from `SilverPickTask`) |
| **Reservations** | Reservation fulfillment rate | `allocated_qty` / `promised_qty`, `FactReservation` |
| | ATP trend | `available_qty` over time, `FactStockSnapshot` |
| **Quality** | QC hold rate | batches quarantined / batches received |
| | Avg hold duration | avg `duration_hours`, `FactQualityHold` |
| | Rejected batch value | Σ value where `quality_status = Rejected` |
| **Labor / Ops** | Units per labor hour | `units_moved` / hours worked, `FactLaborActivity` |
| | Tasks per operator per shift | `tasks_completed` by `user_key`, by shift |
| **Carrier** | Carrier OTIF | `otif_flag` rate by `carrier_partner_key`, `FactShipment` |
| | Avg transit time | avg `transit_days` by carrier/lane |

---

## 9. Future source: a payment service (PayPal-/Stripe-like)

If Warehouse later needs to settle **COD deliveries, supplier payouts, or marketplace-style
customer payments**, a dedicated `payments-service` would sit alongside the existing three
services, linked to `OutboundOrder`/`InboundDelivery` by reference (`order_ref` /
`supplier_payout_ref`), the same "Partnership via events" style already used for
Logistics↔Inventory. Modeled after the object shapes PayPal/Stripe/Adyen all converge on
(`PaymentIntent`/`Charge`, `Refund`, `Payout`, `Dispute`, `Balance`) — these aren't invented, they
are the industry-standard shape for a reason: double-entry ledger + idempotent transaction states.

### Proposed domain model (source for a future Bronze/Silver/Gold extension)

```
PaymentAccount(
  account_id PK, owner_party_id FK,     // -> Partner (customer, supplier, or carrier)
  account_type,                          // Customer | SupplierPayout | Platform
  currency, status,                      // Active | Restricted | Closed
  created_at
)

PaymentMethod(
  payment_method_id PK, account_id FK,
  type,                                  // Card | BankTransfer | Wallet | BLIK | COD
  brand, last4, expiry_month, expiry_year,
  is_default
)

PaymentTransaction(                      // the "Charge" / "PaymentIntent"
  transaction_id PK, order_ref,          // -> OutboundOrder.order_id
  payer_party_id FK, payee_party_id FK,
  amount, currency,
  status,                                // Created | Authorized | Captured | Failed | Cancelled | Refunded
  payment_method_id FK,
  gateway,                               // "PayPal" | "Stripe" | internal
  gateway_reference_id,
  risk_score,
  created_at, authorized_at, captured_at, failed_reason
)

Refund(
  refund_id PK, transaction_id FK,
  amount, reason, status,                // Requested | Completed | Failed
  created_at, completed_at
)

Payout(                                  // e.g. paying a supplier, or a marketplace-style seller payout
  payout_id PK, payee_party_id FK,
  amount, currency, status,              // Scheduled | InTransit | Paid | Failed
  scheduled_at, completed_at
)

Dispute(                                 // chargeback
  dispute_id PK, transaction_id FK,
  reason, amount, status,                // Open | Won | Lost
  opened_at, resolved_at
)

Fee(
  fee_id PK, transaction_id FK,
  fee_type,                              // ProcessingFee | FxFee | PlatformFee
  amount, currency
)

FxRate(fx_rate_id PK, from_currency, to_currency, rate, as_of)

LedgerEntry(                             // double-entry, immutable — same "ledger" pattern as StockMovement
  ledger_entry_id PK, account_id FK, transaction_id FK,
  direction,                             // Debit | Credit
  amount, currency, balance_after, recorded_at
)

WebhookEvent(                            // raw inbound PSP callbacks — the payments equivalent of Bronze
  event_id PK, event_type, payload_json, received_at, processed_at, status
)
```

### What this unlocks in Gold, and new metrics

New facts: `FactPayment` (grain: transaction), `FactRefund`, `FactDispute`, `FactPayout`. New
dims: `DimPaymentMethod`, `DimCurrency`. Because `PaymentTransaction.order_ref` links back to
`FactOutboundFulfillment.order_id`, you get order-to-cash metrics for free:

| Metric | Why it matters |
|---|---|
| Payment success rate | Captured / Created transactions |
| Authorization-to-capture time | Payment ops latency |
| Chargeback rate | Σ Disputes / Σ Transactions, fraud/quality signal |
| Refund rate & reasons | Product/fulfillment quality signal (ties back to `FactQualityHold`, `FactStocktakeVariance`) |
| COD collection rate | For orders with `PaymentMethod.type = COD`, captured / dispatched |
| Days Sales Outstanding (DSO) | For invoiced B2B customers |
| Average transaction value / take rate | Finance reporting |
| FX exposure | Σ amount by currency vs. settlement currency |

---

## 10. Future source: fleet & cold-chain telemetry (proposed new service)

The domain already has **cold rooms** and a **temperature-compatibility invariant**
(`01-domain-overview.md` rule 1) enforced *inside* the warehouse. That invariant currently stops
at the dock — nothing tracks whether a **refrigerated shipment stays in range in transit**. A
`fleet-telemetry-service` closes that gap and is a natural extension of the existing `Shipment` /
`CarrierRole` model (`04-domain-model.md` §5–6), rather than an unrelated bolt-on. This mirrors
what real-world TMS/telematics platforms (Samsara, Geotab, Project44, FourKites) and carrier EDI
milestone feeds (EDI 214) model:

```
Vehicle(
  vehicle_id PK, carrier_party_id FK,   // -> Partner (CarrierRole)
  plate_number, vehicle_type,           // Van | Truck | Reefer
  has_cold_unit
)

Driver(driver_id PK, carrier_party_id FK, display_name, license_number)

Trip(                                   // one leg of a Shipment
  trip_id PK, shipment_ref,             // -> Shipment.shipment_id
  vehicle_id FK, driver_id FK,
  planned_departure_at, planned_arrival_at,
  actual_departure_at, actual_arrival_at,
  status                                // Planned | InProgress | Completed | Exception
)

GpsPing(                                // high-frequency — lands raw in Bronze, aggregated in Silver
  ping_id PK, trip_id FK, vehicle_id FK,
  ts, latitude, longitude, speed_kmh, heading_degrees
)

TelemetryReading(                       // ELD/engine data
  reading_id PK, vehicle_id FK, ts,
  fuel_level_pct, odometer_km, engine_on, harsh_braking_event, idle_minutes
)

ColdChainSensorReading(                 // the direct extension of the in-warehouse temperature rule
  reading_id PK, trip_id FK, shipment_ref, sensor_id, ts,
  temperature_c, humidity_pct, door_open_event
)

DeliveryMilestone(                      // proof-of-delivery / status feed, EDI-214-style
  event_id PK, shipment_ref, ts,
  event_type,                           // PickedUp | InTransit | OutForDelivery | Delivered | Exception
  latitude, longitude, notes, signature_url, photo_url
)

GeofenceEvent(
  event_id PK, trip_id FK, geofence_name,    // "WarehouseDock" | "CustomerSite"
  entered_at, exited_at
)

EtaPrediction(
  trip_id FK, predicted_at, predicted_arrival_at, confidence_score
)
```

### What this unlocks in Gold, and new metrics

New facts: `FactTrip` (grain: trip/leg), `FactColdChainExcursion` (grain: an out-of-range
temperature interval — derived from consecutive `ColdChainSensorReading`s outside
`DimProduct`'s temperature range), `FactDeliveryMilestone`. New dims: `DimVehicle`, `DimDriver`
(with `DimPartner` already covering the carrier itself).

| Metric | Why it matters |
|---|---|
| GPS-verified on-time pickup/delivery % | More trustworthy than carrier-reported status alone |
| Cold-chain excursion count & duration per shipment | Direct extension of the warehouse's own temperature-compliance metric — end-to-end cold chain visibility |
| Transit time variance vs. planned | Route/carrier reliability |
| Predicted vs. actual ETA accuracy | Feeds customer-facing ETAs |
| Harsh driving events per 100 km | Carrier/driver safety scorecard |
| Idle time % / fuel efficiency | Carrier cost-efficiency, if carrier shares telemetry |
| Carrier scorecard (composite) | OTIF (§8) + excursion rate + ETA accuracy + safety, blended per carrier — a single ranking for carrier sourcing decisions |

This is the strongest "second service" candidate because it **extends an invariant the domain
already enforces** (environment compatibility) across the one boundary where it currently isn't
checked at all (in transit) — rather than introducing a disconnected concern.

---

## 11. Ingestion architecture: getting data into Databricks

The stack this actually has to run on is **AWS** (`infra/terraform/` — ECS Fargate, ECR, S3) with
**RabbitMQ** as the broker (`02-bounded-contexts.md` §Messaging), not Azure. That rules out
Azure Data Factory and Azure Event Hub — both would mean paying cross-cloud egress and running a
second cloud's control plane just to move data one hop. Databricks itself is multi-cloud (runs
fine on AWS), so nothing about "we chose Databricks" forces an Azure detour.

### Recommended path: S3 landing zone + Databricks Auto Loader

```
[warehouse-service]  ─┐
[logistics-service]   ├─ outbox → RabbitMQ ─▶ [bi-export consumer] ─▶ S3 (Bronze landing)
[masterdata-service]  ─┘                                                    │
                                                                              ▼
                                                          Databricks Auto Loader (cloudFiles)
                                                                              │
                                                                              ▼
                                                        Bronze Delta tables (§5) ─▶ Silver ─▶ Gold
```

1. **Reuse the transactional outbox — don't add a new integration mechanism.** Each service
   already publishes versioned, past-tense domain events to RabbitMQ. Add one more consumer per
   service — the same shape as the existing local-replica consumers (e.g. Inventory's
   `ProductSnapshot` updater) — subscribed to the same exchange, whose only job is to serialize
   each event to JSON/Parquet and drop it in S3:
   ```
   s3://warehouse-lake/bronze/<source_system>/<event_type>/date=YYYY-MM-DD/<event_id>.parquet
   ```
   This is a thin, stateless "BI export" consumer — a few hundred lines per service at most — not
   a new platform. It's idempotent by construction: `_event_id` (the outbox message id) is the
   object key, so redelivery just overwrites the same file.
2. **Databricks Auto Loader (`cloudFiles`)** watches that S3 prefix and incrementally loads new
   files into the Bronze Delta tables from §5 — exactly-once, schema-evolution built in, no
   polling logic to hand-write. It's a first-party S3 connector; nothing else to provision.
3. **For current-state tables** (`StockItem`, `Batch`, etc.) you likely don't need a separate
   periodic export at all: since every state change already flows through the ledger
   (`StockMovement`) or an outbox event, Silver can derive current state by replaying/aggregating
   Bronze events. Keep a periodic full snapshot only as a reconciliation safety net (e.g. nightly),
   landed the same way, into the same S3 prefix structure.
4. **Orchestration: Databricks Workflows**, not a separate orchestrator like ADF. For the
   Bronze→Silver→Gold transforms themselves, consider **Lakeflow Declarative Pipelines** (formerly
   Delta Live Tables) — it lets you declare each table plus data-quality expectations (e.g.
   "`quantity` is never negative" — the same invariant already enforced in the domain, §1 rule 3)
   instead of hand-writing and scheduling notebooks.
5. **Latency:** micro-batch (Auto Loader triggered every few minutes) is enough for every metric in
   §8 — none of them are sub-second dashboards. Reach for a real streaming broker (Kafka/MSK) only
   if that changes; it isn't justified at this scale today.

### Why not the alternatives

| Option | Why not (for this stack, today) |
|---|---|
| Azure Data Factory | Wrong cloud — adds Azure as a second control plane + cross-cloud egress cost, purely to move data one hop |
| Azure Event Hub | Same — plus you'd be running two message buses (RabbitMQ for the app, Event Hub for BI) |
| Kafka / MSK / Kinesis | Real option later if sub-minute latency is ever required; unjustified operational cost today given RabbitMQ already exists and micro-batch is sufficient |
| AWS DMS for CDC | Managed, but AWS-proprietary — the explicit requirement here is a cloud-agnostic pipeline, so a service that only runs in AWS is disqualified regardless of how little ops overhead it saves |

**CDC is not "instead of" the event-driven pipeline above — it's a second, narrower source for the
one thing events can't cover.** Business facts (a delivery was put away, an order was placed) belong
in events: they carry intent, not just row state, and forcing Silver to reconstruct that intent from
raw diffs is strictly worse than a service just saying so. But event coverage in this codebase is
**opt-in per handler** (`add-bi-export`) — plenty of internal-state writes (the `StockMovement`
ledger, stocktake reconciliation, quality holds, all of Partners — see below) never call the outbox
at all, because no other *service* needs to react to them. Those are invisible to the event pipeline
by construction, not by oversight, and adding an event purely to feed BI would couple domain code
to an analytics concern it has no other reason to know about. CDC, scoped to exactly those tables, is
the tool for that gap — see the dedicated section below.

**Deliberate exception: `DeliveryAnnouncedV1`/`DockSlotAssignedV1`/`DeliveryArrivedV1`.** These three
have no other-service consumer either — by the rule above they look like CDC territory
(`logistics.inbound_deliveries` is already in the CDC table list, §below). They're event-driven
anyway because, unlike a stock-ledger row, each one *is* a named business milestone with a clear
"when" (ASN raised, dock booked, truck checked in) — cheaper for Silver to consume as a fact than to
re-derive intent from a row diff, same reasoning that puts `GoodsReceiptConfirmed` in events rather
than leaving it to CDC on `inbound_deliveries`. Worth revisiting if `logistics.inbound_deliveries`
CDC ever gets verified end-to-end and the duplication stops paying for itself.

### Reference implementation (project side only — no Databricks resources)

Scope, per the decision to prepare the app/infra side now and wire up Databricks itself later: land
files in the right place, in the right shape, and nothing more.

- **Reusable writer** — `src/ServiceDefaults/Warehouse.ServiceDefaults/BiExport/`:
  `BronzeEvent` (the flat envelope every export writes), `IBronzeExportWriter`,
  `S3ParquetBronzeExportWriter` (the real implementation — one Parquet file per event, written to
  `bronze/<source_system>/<event_type>/date=.../<event_id>.parquet`), `NullBronzeExportWriter`
  (registered instead when export is off), `BiExportOptions`, and the `AddBiExport()` host
  extension. Reusable by design — every service opts in with one line in its `Program.cs`, the same
  way `AddServiceDefaults()`/`AddWarehouseMessaging()` already work.
- **Consumers — every published integration event in all 3 backend services now has a matching
  export consumer.** Each service calls `AddBiExport()` once in its `Program.cs` and binds one
  dedicated `<service>.bi-export` queue to the exchange(s) it already publishes on — fully decoupled
  from the business-critical replica/consumer queues, so a slow/backed-up export never touches the
  domain flow:
  - **`warehouse-service`** (Warehousing/Inventory, `Application/BiExport/`) — queue
    `warehousing.bi-export`, bound to the `inventory` exchange:
    - `GoodsReceivedBiExportConsumer` — `GoodsReceivedV1` (feeds `FactInboundReceipt`, §7)
    - `PutAwayCompletedBiExportConsumer` — `PutAwayCompletedV1` (put-away lead time, §8)
    - `StockReservedBiExportConsumer` — `StockReservedV1` (feeds `FactReservation`/fulfillment rate, §7/§8)
    - `PicksPlannedBiExportConsumer` — `PicksPlannedV1` (picking metrics, §8); nested `Picks` travels
      as `picks_json` + `pick_count` (see below)
  - **`logistics-service`** (`Warehouse.Logistics.Core`, `Application/BiExport/`) — queue
    `logistics.bi-export`, bound to the `logistics` exchange:
    - `GoodsReceiptConfirmedBiExportConsumer` — `GoodsReceiptConfirmedV1`; nested `Lines` → `lines_json` + `line_count`
    - `OutboundOrderPlacedBiExportConsumer` — `OutboundOrderPlacedV1`; nested `Lines` → `lines_json` + `line_count`
    - `OutboundOrderCancelledBiExportConsumer` — `OutboundOrderCancelledV1`
    - `ShipmentDispatchedBiExportConsumer` — `ShipmentDispatchedV1`
    - `PickingReleasedBiExportConsumer` — `PickingReleasedV1`
    - `PickConfirmedBiExportConsumer` — `PickConfirmedV1`
    - `DeliveryAnnouncedBiExportConsumer` — `DeliveryAnnouncedV1` (delivery ASN'd — feeds
      `BronzeInboundDelivery.announced_at`/`supplier_party_id`, §5.3)
    - `DockSlotAssignedBiExportConsumer` — `DockSlotAssignedV1` (feeds
      `BronzeInboundDelivery.dock_code`/`dock_slot_at`, §5.3)
    - `DeliveryArrivedBiExportConsumer` — `DeliveryArrivedV1` (feeds
      `BronzeInboundDelivery.arrived_at`, §5.3)

      These three didn't exist as integration events before — `AnnounceDelivery`/`AssignDockSlot`/
      `RegisterArrival` only ever mutated the aggregate via `IUnitOfWork`, with no consumer anywhere
      needing to react. They were added (same transactional-outbox pattern as `ConfirmReceiptHandler`
      → `GoodsReceiptConfirmedV1`) specifically to close the `BronzeInboundDelivery` timeline columns
      that had no source. `ASN accuracy` (expected vs. received qty per line,
      `BronzeInboundDeliveryLine`) is a deliberately separate, not-yet-built slice.
  - **`masterdata-service`** (`Warehouse.MasterData.Catalog`, `Application/BiExport/`) — queue
    `masterdata.bi-export`, bound to the `catalog` exchange:
    - `ProductDefinedBiExportConsumer` — `ProductDefinedV2` (feeds `DimProduct`, §6/§7)

  **Nested lists don't fit the flat `BronzeEvent.Payload` string map** (`PicksPlannedV1.Picks`,
  `GoodsReceiptConfirmedV1.Lines`, `OutboundOrderPlacedV1.Lines`) — each travels as a JSON string
  column (`picks_json`/`lines_json`) alongside a `*_count` column; Bronze is schema-on-read tolerant
  by design (§5), Silver is where it gets exploded into per-line rows.

  **To export another event**, add another class with the same shape (constructor-inject
  `IBronzeExportWriter`, map the event to a `BronzeEvent` in `Handle`) on the service's existing
  `bi-export` queue — Wolverine discovers it automatically; nothing else changes. Adding BI export to
  a service that doesn't have it yet needs one more thing: bind a new `<service>.bi-export` queue to
  the exchange(s) it publishes on, alongside its `AddBiExport()` call.
- **Infra** — `infra/terraform/bootstrap/main.tf` provisions the durable `bi_lake` S3 bucket (§11
  above); `infra/terraform/environment/ecs.tf` adds one shared ECS task role (all 3 backends run in
  the same Fargate task) scoped to `s3:PutObject` on that bucket's `bronze/*` prefix only;
  `locals.tf` passes `BiExport__BucketName`/`BiExport__Region` to `warehousing-api`,
  `logistics-api`, and `masterdata-api`. Everything is off by default (`bi_lake_bucket_name = ""`) —
  turning it on is a one-variable change (`BI_LAKE_BUCKET` repo variable, see `docs/deploy.md`).
- **Not built yet, on purpose**: no Databricks workspace, no Auto Loader job, no Bronze/Silver/Gold
  tables. This reference implementation only proves the app can deliver correctly-shaped,
  idempotent files to the right S3 prefix — everything downstream of that prefix is a separate,
  later piece of work.

### CDC section: closing the state-table gap, cloud-agnostically

State that never becomes an integration event — the `stock_movements` ledger, stocktake counts,
current on-hand, the entirety of `partners` (that module publishes no events at all) — needs a
different mechanism than `add-bi-export`, because there's no event to hook a consumer onto. The
requirement was explicitly a **cloud-agnostic** pipeline (no AWS DMS, no managed CDC service), so
this uses **Debezium**, self-hosted, reading Postgres logical replication directly:

```
Postgres (wal_level=logical) ─▶ Debezium Server (pgoutput) ─▶ RabbitMQ (existing broker, no Kafka)
                                                                        │
                                                       Warehouse.CdcBridge (raw AMQP consumer)
                                                                        │
                                                                        ▼
                                          S3 bronze-cdc/<schema>/<table>/date=.../<lsn>.json
```

- **Debezium Server** (not Kafka Connect — no Kafka broker in this stack), one instance per source
  database (`masterdata`, `warehouse`, `logistics`), publishing onto RabbitMQ via its
  `debezium-server-rabbitmq` sink — plain AMQP `basicPublish`, the same protocol Wolverine already
  speaks, so no new broker or protocol is introduced. Debezium's own `op`/`before`/`after`/`source`
  envelope is left untouched (`DEBEZIUM_FORMAT_VALUE_SCHEMAS_ENABLE=false` strips only the Kafka
  Connect schema wrapper), so it plugs directly into Databricks' `APPLY CHANGES INTO` later without a
  custom transform.
- **Table scope** (deliberately narrow — current-state/ledger tables only, never a downstream
  replica of another service's own data, e.g. NOT `inventory.product_snapshots`). Cross-checked
  against a domain-event inventory from the Databricks team (every domain event with no integration
  contract and no consumer): all but three were already covered by the tables below; those three —
  `allocations`, `handling_units`(+lines), `catalog.product_types` — were promoted from an initial
  "phase 2, low-value" call once it was clear real BI demand existed for them
  (`StockAllocated`/`AllocationReleased`, `HandlingUnitMoved`, `ProductStorageChanged` — cold-chain/
  hazard fields — all have no other path to Bronze):
  - `warehouse` DB: `inventory.stock_items`, `stock_movements`, `stock_reservations`, `batches`,
    `stocktakes`, `stocktake_count_lines`, `allocations`, `handling_units`, `handling_unit_lines`;
    `topology.warehouses`, `rooms`, `locations`, `docks`
  - `logistics` DB: `inbound_deliveries`(+lines), `outbound_orders`(+lines), `pick_lists`,
    `pick_tasks`, `shipments`, `packages`
  - `masterdata` DB: `partners.parties`, `party_roles`, `customer_shipping_addresses` (the *only*
    path this data has into Bronze, since Partners publishes no integration events at all),
    `catalog.product_types`. `catalog.product_unit_conversions` stays excluded — genuinely
    low-value static lookup, not flagged by anyone as needed
- **`Warehouse.CdcBridge`** (`src/CdcBridge/`) — a small worker, not an API: a raw
  `RabbitMQ.Client` consumer (not Wolverine — these are Debezium's own JSON records, not typed C#
  integration events), one durable queue per source-database exchange, landing each record
  byte-for-byte at `bronze-cdc/<schema>/<table>/date=yyyy-MM-dd/<lsn>.json`. Keyed on the Postgres
  LSN (the change's physical WAL position) for the same reason `S3ParquetBronzeExportWriter` keys on
  the Wolverine envelope id: a redelivery overwrites the same object instead of duplicating a row.
- **Infra** (`infra/terraform/environment/locals.tf`) — `postgres` gets `wal_level=logical`;
  `db-init` also creates one `PUBLICATION ... FOR ALL TABLES` per database (table-level scoping
  happens entirely in each Debezium connector's `table.include.list`, not in the publication, so
  adding a table to CDC scope later never needs a new migration); a `rabbitmq-init` one-shot
  container declares the 3 CDC topic exchanges (Debezium's RabbitMQ sink never declares its own
  exchange); 3 Debezium Server containers + `cdc-bridge` are spliced into the task **only when**
  `bi_lake_bucket_name` is set — 4 extra JVM/worker processes is real Fargate cost an ephemeral
  environment shouldn't pay for BiExport-only use.
- **Confidence note — updated after a real smoke test.** Two layers were verified independently:
  1. `Warehouse.CdcBridge` (the RabbitMQ consumer + S3 writer) against real Testcontainers
     (RabbitMQ + LocalStack) — see `tests/Warehouse.CdcBridge.IntegrationTests`.
  2. The **full real pipeline** — `docker compose` with real `postgres:16-alpine`
     (`wal_level=logical`), real `quay.io/debezium/server:2.7`, real `rabbitmq:3-management-alpine`,
     and the actual `Warehouse.CdcBridge` binary (unmodified) pointed at LocalStack. A plain SQL
     `INSERT` into a scoped table produced a change event that flowed
     Postgres → Debezium → RabbitMQ → `CdcBridge` → S3, landing at
     `bronze-cdc/inventory/stock_items/date=.../<lsn>.json` with the exact envelope shape this
     design assumed. This confirms the `DEBEZIUM_SOURCE_*`/`DEBEZIUM_SINK_*` env var convention,
     the publication/replication-slot setup, and the RabbitMQ sink all work as configured.

  **Two things the smoke test changed or flagged:**
  - **Fixed, confirmed required**: Debezium Server crashes on startup without an explicit
    `DEBEZIUM_SOURCE_OFFSET_STORAGE_FILE_FILENAME` (it otherwise resolves to the `/debezium/data`
    mount point itself — `IOException: Is a directory`). Added to `locals.tf`.
  - **Noted, not fixed (harmless)**: `DEBEZIUM_SINK_RABBITMQ_ROUTING_KEY_SOURCE=topic` doesn't appear
    to take effect — delivered messages had an empty routing key rather than a per-table topic name,
    likely a camelCase-to-env-var mapping quirk (`routingKey.source` → `ROUTINGKEY_SOURCE`, not
    `ROUTING_KEY_SOURCE`). Doesn't matter functionally: `Warehouse.CdcBridge` binds its queue with
    routing key `#` and never inspects the actual key.

  **Still not exercised**: multi-table capture (smoke-tested one table), the 3-database
  multi-instance shape (smoke-tested one Debezium instance against one database), container restart
  / offset-file persistence across restarts (no volume mounted in the smoke test), and the
  `rabbitmq-init` exchange-declare step specifically as an ECS one-shot container (declared the
  exchange with a manual `curl` call instead, same HTTP call `rabbitmq-init`'s command makes).

### Identity-bridge: the one source neither events nor CDC can reach

Keycloak publishes no integration events and has no persistent database in this stack (`start-dev`,
embedded storage — see the file header of `infra/docker/keycloak/Dockerfile`), so neither
`add-bi-export` nor the CDC pipeline above can reach it. `Warehouse.IdentityBridge`
(`src/IdentityBridge/`) is a third, narrower mechanism: a periodic poller against Keycloak's own
Admin REST API.

```
Keycloak Admin REST API ─▶ Warehouse.IdentityBridge (poll every N seconds)
                                       │
                                       ▼
                    S3 bronze/identity-service/UserSnapshot/date=.../<badge>.parquet
```

- **`KeycloakAdminClient`** — gets a token via the master realm's built-in `admin-cli` client using
  the dev bootstrap admin credentials (this environment has no dedicated service-account client;
  same accepted simplification `docs/deploy.md` already documents for Keycloak elsewhere), lists
  users in the `warehouse` realm, then fetches realm role mappings **per user** (a second call —
  `UserRepresentation` doesn't carry roles on its own).
- **Reuses the existing Bronze writer wholesale** — `UserSnapshotPollingService` maps each Keycloak
  user onto a `BronzeEvent` (§5.4's field mapping table) and hands it to the same
  `IBronzeExportWriter` every event-driven consumer uses. No new S3/Parquet code was needed; this
  data lands in the identical `bronze/<source_system>/<event_type>/...` shape as everything else,
  just with `source_system = "identity-service"`.
- **Snapshot grain**: keyed by `user_id` (the badge), dated by poll time — a re-poll on the same UTC
  day overwrites that user's file (latest wins), a poll on a new day starts a fresh dated snapshot.
  Same idempotency/history shape as every other periodic-snapshot source in this plan.
- **Infra** — one more container in `infra/terraform/environment/locals.tf`, gated on
  `bi_lake_bucket_name` alongside the CDC containers (no RabbitMQ dependency — it only needs
  Keycloak + the S3 bucket). No new IAM permission needed: `bronze/identity-service/*` already falls
  under the task role's existing `bronze/*` grant.
- **Verified for real** — unlike the CDC pipeline's single-table smoke test, this one was
  smoke-tested **fully in scope**: a real `quay.io/keycloak/keycloak:26.0.7` container with the
  actual `warehouse-realm.json` imported, all 5 real users, real role-mapping calls, all landing
  correctly in a real S3-compatible endpoint (LocalStack) on the **first run, no bugs found** —
  a meaningfully higher confidence bar than anything else built in this session.

### Actual state — every model, verified vs configured vs missing

No aspirational language below — this is what has actually run, on what evidence, as of this
writing. "Verified" never means "deployed to the real AWS environment" (that has never happened);
it means "run for real, locally, against real Postgres/Debezium/RabbitMQ/S3-compatible containers,"
which is a meaningfully lower bar.

**Event-driven Bronze (`bronze/`) — 11 of 11 published integration events have a consumer.**
Every row below: code exists, `dotnet build` is clean, unit tests pass (mocked S3), and
`S3ParquetBronzeExportWriter` itself is proven against a real S3-compatible endpoint
(`Warehouse.ServiceDefaults.IntegrationTests`, LocalStack). **None of this has been deployed to a
live AWS environment** — `BI_LAKE_BUCKET` has never actually been set on a real `terraform apply`.

| Service | Event | Status |
|---|---|---|
| warehouse-service | `GoodsReceivedV1`, `PutAwayCompletedV1`, `StockReservedV1`, `PicksPlannedV1` | Built + unit tested, not live-deployed |
| logistics-service | `GoodsReceiptConfirmedV1`, `OutboundOrderPlacedV1`, `OutboundOrderCancelledV1`, `ShipmentDispatchedV1`, `PickingReleasedV1`, `PickConfirmedV1` | Built + unit tested, not live-deployed |
| masterdata-service | `ProductDefinedV2` | Built + unit tested, not live-deployed |

**CDC-to-Bronze (`bronze-cdc/`) — 25 tables configured, 1 verified end-to-end for real.**

| Table | DB / schema | Status |
|---|---|---|
| `inventory.stock_items` | warehouse | **Verified** — real `INSERT` flowed Postgres → Debezium → RabbitMQ → `CdcBridge` → S3 in the smoke test |
| `inventory.stock_movements`, `stock_reservations`, `batches`, `stocktakes`, `stocktake_count_lines`, `allocations`, `handling_units`, `handling_unit_lines` | warehouse | Configured, same Debezium instance/config pattern as the verified table above — not individually run |
| `topology.warehouses`, `rooms`, `locations`, `docks` | warehouse | Configured, not run |
| `logistics.inbound_deliveries`(+lines), `outbound_orders`(+lines), `pick_lists`, `pick_tasks`, `shipments`, `packages` | logistics | Configured only — this Debezium instance has never been started, even in the smoke test |
| `partners.parties`, `party_roles`, `customer_shipping_addresses` | masterdata | Configured only — same, this instance has never run |
| `catalog.product_types` | masterdata | Configured only — added in this session, never run at all (not even build-checked, since it's a Terraform-only string, not compiled code) |
| `catalog.product_unit_conversions` | masterdata | **Not in scope** — excluded, nobody has asked for it |

**Identity Bronze (`bronze/identity-service/`) — fully verified, the strongest confidence of
anything in this document.**

| Source | Status |
|---|---|
| `BronzeUser` (all 5 realm users, real role-mapping calls) | **Verified** — real Keycloak container, real realm import, first run, no bugs |

**Everything else mentioned anywhere in this document** — Silver (§6), Gold star schema (§7), the
metrics catalog (§8), the payment service model (§9), fleet/cold-chain telemetry (§10) — is design
only. No Databricks workspace, no Delta tables, no Silver/Gold transforms exist. The entire content
of §5–§10 describes an intended shape, not a built one.

---

## 12. Summary: what to build, in order

1. **Bronze (events)** ingestion for the 3 existing services — a thin outbox-consuming "BI export"
   service per backend service, landing events in S3, picked up by Databricks Auto Loader (§11). No
   new business services required — **built**, all 11 published integration events covered.
2. **Bronze (CDC)** for the state that never becomes an event — Debezium + `Warehouse.CdcBridge`
   (§11 CDC section) — **built**, table scope + writer verified end-to-end for one table/instance;
   the other 24 tables and 2 remaining Debezium instances need a real-deploy smoke test.
3. **Bronze (Identity)** — `Warehouse.IdentityBridge` polling Keycloak's Admin API (§11
   Identity-bridge section) — **built and verified in full**: real Keycloak container, real realm
   import, all users, first run, no bugs. The strongest-confidence piece of this entire plan.
4. **Silver** conformance — the biggest lift is `DimProduct`/`DimPartner` SCD2 handling and
   reconciling event-sourced facts against the CDC-fed `stock_items`/`stock_movements`.
5. **Gold** star schema (§7) + the metrics catalog (§8) wired into Power BI — this alone answers
   every question in §1.
6. **Payments** (§9) — once a payment service exists, extends Gold with order-to-cash metrics.
7. **Fleet & cold-chain telemetry** (§10) — extends Gold with end-to-end cold-chain visibility and
   carrier performance; the recommended "second service" given it directly extends an existing
   domain invariant rather than being a disconnected addition.
