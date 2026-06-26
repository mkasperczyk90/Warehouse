# Logistics

`src/Services/Logistics/Modules/Warehouse.Logistics.Core` — the flow of goods across the
warehouse boundary. All four aggregates are 🟨 Moment-Intervals: they carry process state;
physical stock truth stays in Inventory.

```mermaid
classDiagram
    class InboundDelivery {
        <<AggregateRoot, id = DeliveryId>>
        +PartyRoleRef Supplier
        +WarehouseRef Warehouse
        +DateTimeOffset PlannedAt
        +DockSlot? Slot
        +DeliveryStatus Status
        +Announce(supplier, warehouse, plannedAt, lines)$ InboundDelivery
        +AssignDockSlot(slot)
        +RegisterArrival()
        +StartReceiving()
        +RecordReceipt(lineNo, actual, batch?, discrepancy, note?)
        +ConfirmReceipt()
        +StartPutAway() +CompletePutAway()
        +Cancel()
    }
    class DeliveryLine {
        +int LineNo
        +ProductCode Product
        +Quantity Expected
        +DeliveryPack? Pack «delivery-specific UoM, null = catalog default»
        +Quantity? Actual
        +BatchInfo? Batch
        +DiscrepancyType Discrepancy
    }
    class DeliveryPack {
        <<ValueObject>>
        +UnitOfMeasure Unit
        +decimal FactorToBase
        +ToBaseUnits(announced, baseUnit) Quantity
    }
    class OutboundOrder {
        <<AggregateRoot, id = OrderId>>
        +PartyRoleRef Customer
        +Address ShipTo
        +WarehouseRef Warehouse
        +DateTimeOffset RequiredAt
        +OrderStatus Status
        +Create(customer, shipTo, warehouse, requiredAt, lines)$ OutboundOrder
        +MarkReserved(fully)
        +StartPicking() +MarkPacked() +MarkDispatched()
        +Cancel()
    }
    class OrderLine {
        +int LineNo
        +ProductCode Product
        +Quantity Ordered
    }
    class PickList {
        <<AggregateRoot, id = PickListId>>
        +OrderId OrderId
        +bool IsCompleted
        +CreateFor(orderId, plannedPicks)$ PickList
        +ConfirmPick(sequence, pickedBy)
        +ReportShort(sequence, reportedBy)
    }
    class PickTask {
        +int Sequence
        +LocationRef Location
        +ProductCode Product
        +BatchInfo? Batch
        +Quantity Quantity
        +PickTaskStatus Status «Pending|Picked|ShortPick»
    }
    class Shipment {
        <<AggregateRoot, id = ShipmentId>>
        +OrderId OrderId
        +PartyRoleRef? Carrier
        +string? Pickup
        +TrackingNumber? Tracking
        +ShipmentStatus Status
        +CreateAwaitingCarrier(orderId)$ Shipment
        +AddPackage(weight, dimensions, description?) Package
        +AssignCarrier(carrier, pickup)
        +SendPickupNotice()
        +AssignTracking(tracking)
        +Dispatch()
    }
    class Package {
        +int Number
        +Weight Weight
        +string? Description
    }
    InboundDelivery "1" *-- "*" DeliveryLine
    DeliveryLine "0..1" *-- "1" DeliveryPack
    OutboundOrder "1" *-- "*" OrderLine
    OutboundOrder <.. PickList : OrderId
    OutboundOrder <.. Shipment : OrderId
    PickList "1" *-- "*" PickTask
    Shipment "1" *-- "*" Package
```

## State machines (as implemented)

```mermaid
stateDiagram-v2
    direction LR
    state "InboundDelivery.Status" as D {
        [*] --> Announced: Announce
        Announced --> Arrived: RegisterArrival
        Arrived --> Receiving: StartReceiving
        Receiving --> Received: ConfirmReceipt
        Received --> PutAwayInProgress: StartPutAway
        PutAwayInProgress --> Completed: CompletePutAway
        Announced --> Cancelled: Cancel
    }
```

```mermaid
stateDiagram-v2
    direction LR
    state "OutboundOrder.Status" as O {
        [*] --> Created: Create
        Created --> PartiallyReserved: MarkReserved(false)
        Created --> Reserved: MarkReserved(true)
        PartiallyReserved --> Reserved: MarkReserved(true)
        Reserved --> Picking: StartPicking
        Picking --> Packed: MarkPacked
        Packed --> Dispatched: MarkDispatched
        Created --> Cancelled: Cancel
        PartiallyReserved --> Cancelled: Cancel
        Reserved --> Cancelled: Cancel
    }
```

```mermaid
stateDiagram-v2
    direction LR
    state "Shipment.Status" as S {
        [*] --> AwaitingCarrier: CreateAwaitingCarrier (at MarkPacked)
        AwaitingCarrier --> CarrierAssigned: AssignCarrier
        CarrierAssigned --> ReadyForPickup: SendPickupNotice
        ReadyForPickup --> Dispatched: Dispatch
    }
```

The shipment opens when the order is packed (UC-11) and walks the dispatch board column by column
(UC-12): the admin advances it a step at a time, while the terminal's `ConfirmDispatch` fast-forwards the
same states to `Dispatched` in one call. Every transition guard throws `delivery_invalid_status` /
`order_invalid_status` / `shipment_invalid_status` when called out of order.

## Notable rules

| Rule | Where |
|---|---|
| ASN needs ≥ 1 line; only announced deliveries can be received (ad-hoc ASN otherwise) | `Announce`, status machine |
| On `ConfirmReceipt`, unrecorded lines become **implicit full shortages** | `ConfirmReceipt` |
| A line may carry a delivery-specific `DeliveryPack` (e.g. this truck: 1 plt = 36 pcs); else receiving uses the catalog default | `DeliveryLine.Pack`, `DeliveryPack.ToBaseUnits` |
| Dock slot window must be positive (`from < to`) | `DockSlot.Of` |
| Pick task short → status `ShortPick`, the saga replans from another location/batch | `ReportShort` |
| Shipment cannot be ready for pickup with zero packages | `MarkReadyForPickup` |
| Cancelling a reserved order → saga releases Inventory reservations | `Cancel` + event flow |

## Cross-context references (all by value)

| VO | Points at |
|---|---|
| `PartyRoleRef(Guid)` | a role in Partners (supplier/customer/carrier) |
| `WarehouseRef` / `LocationRef` | codes owned by Topology |
| `ProductCode` | a product in Catalog — **loose by design**: holds "what the scanner read", even an unknown code awaiting clarification (UC-01). Not Catalog's strict `Sku`. |
| `BatchInfo(number, expiry)` | becomes a `Batch` in Inventory at receipt |

## Domain events

`DeliveryAnnounced`, `DeliveryArrived`, `GoodsReceiptConfirmed` (→ Inventory receives stock
into the dock buffer), `OutboundOrderCreated`, `OrderReserved`, `ShipmentDispatched`
(→ Inventory deducts stock).
