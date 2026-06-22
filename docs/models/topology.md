# Warehouse Topology (Warehousing)

`src/Services/Warehousing/Modules/Warehouse.Warehousing.Topology` — the physical structure:
*where* goods can be stored. The aggregate is named `WarehouseSite` in code only because
`Warehouse` would clash with the root namespace; in the ubiquitous language it is "Warehouse".

```mermaid
classDiagram
    class WarehouseSite {
        <<AggregateRoot 🟩Place, id = WarehouseCode>>
        +WarehouseCode Code
        +string Name
        +Address Address
        +IReadOnlyCollection~Room~ Rooms
        +IReadOnlyCollection~Dock~ Docks
        +Establish(code, name, address)$ WarehouseSite
        +AddRoom(code, type, environment?) Room
        +AddLocation(roomCode, code, kind, capacity, maxLoad) Location
        +AddDock(code, direction) Dock
        +ChangeRoomEnvironment(roomCode, environment)
    }
    class Room {
        <<Entity 🟩Place, id = RoomCode>>
        +RoomCode Code
        +RoomType Type
        +RoomEnvironment Environment
        +IReadOnlyCollection~Location~ Locations
    }
    class Location {
        <<Entity 🟩Place, id = LocationCode>>
        +LocationCode Code
        +LocationKind Kind
        +Volume Capacity
        +Weight MaxLoad
    }
    class Dock {
        <<Entity 🟩Place, id = DockCode>>
        +DockCode Code
        +DockDirection Direction
    }
    class RoomEnvironment {
        <<ValueObject>>
        +TemperatureRange MaintainedTemperature
        +bool HumidityControlled
        +For(type, temperature?, humidity?)$
    }
    class RoomType {
        <<enum>>
        Standard | ColdRoom | Freezer | HazmatZone
    }
    class LocationKind {
        <<enum>>
        Rack | Floor | DockBuffer
    }
    class DockDirection {
        <<enum>>
        Inbound | Outbound | Both
    }
    WarehouseSite "1" *-- "*" Room
    WarehouseSite "1" *-- "*" Dock
    Room "1" *-- "*" Location
    Room *-- RoomEnvironment
    Room --> RoomType
    Location --> LocationKind
    Dock --> DockDirection
```

## Codes (value objects in `Codes.cs`)

| Code | Format | Example |
|---|---|---|
| `WarehouseCode` | 2–10 chars A-Z/0-9 | `WAW1` |
| `RoomCode` | 2–10 chars A-Z/0-9 | `CHLD1` |
| `DockCode` | 2–10 chars A-Z/0-9 | `D01` |
| `LocationCode` | 2–5 dash-joined segments; `Compose(warehouse, room, …)` | `WAW1-CHLD1-A-03-2` |

`LocationCode` is the **scannable physical address** printed on racks — stable by design.

## Invariants

| Rule | Error code |
|---|---|
| Room/dock codes unique within a warehouse | `room_code_duplicate`, `dock_code_duplicate` |
| Location codes unique across the **whole warehouse** (not just the room) | `location_code_duplicate` |
| Cold room maintains ≤ 8 °C; freezer ≤ −15 °C (defaults: 0..8 and −25..−18) | `room_environment_invalid` |
| A location must have positive capacity | `location_capacity_required` |
| Deleting a non-empty room/location — application-level policy consulting Inventory (not in the aggregate) | — |

## Domain events

| Event | Raised by | Downstream effect |
|---|---|---|
| `LocationDefined(warehouse, room, location, kind, capacity, maxLoad, environment)` | `AddLocation` | Inventory creates/updates `LocationSnapshot` |
| `RoomEnvironmentChanged(warehouse, room, environment)` | `ChangeRoomEnvironment` | Inventory re-validates stock in that room and reports incompatibilities |
