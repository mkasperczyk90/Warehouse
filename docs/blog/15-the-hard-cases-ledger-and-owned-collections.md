# #15 — The hard cases: the ledger, owned collections, and two contexts in one database

*Series: Building a real microservices application, brick by brick.
Previous: [#14 From port to table](14-persisting-an-aggregate-with-ef-core.md).
Code: [`Warehouse.Warehousing.Inventory`](../../src/Services/Warehousing/Modules/Warehouse.Warehousing.Inventory).*

---

[Post #14](14-persisting-an-aggregate-with-ef-core.md) persisted Topology — one aggregate, owned all
the way down, not under load. **Inventory is the core**, and it leans on the three seams from
[#13](13-repository-unit-of-work-and-events.md) in ways Topology never did:

1. an **append-only ledger** that has to commit in the *same* transaction as the stock it records;
2. **owned collections on a hot aggregate**, plus a second aggregate it points at by id;
3. a **value-converted column you can't translate to SQL** for a `StartsWith` query;
4. **two bounded contexts sharing one physical database**.

Each one is where a naive persistence layer quietly breaks an invariant. Here's how the seams hold.

## 1. The ledger and the stock change commit together

Part I's hardest rule: **stock cannot change without a ledger entry.** Every stock-mutating behavior
*returns* the `StockMovement` it produced, instead of writing it itself:

```csharp
public StockMovement Pick(AllocationId allocationId, Quantity quantity, string performedBy)
{
    var allocation = GetActiveAllocation(allocationId);
    // ...invariants: pick ≤ allocation...
    OnHand = OnHand.Subtract(quantity);
    Allocated = Allocated.Subtract(quantity);
    allocation.ReduceBy(quantity);

    Raise(new StockPicked(Id, allocationId, Sku, Location, quantity, DateTimeOffset.UtcNow));
    return StockMovement.Record(
        MovementType.Pick, Sku, Batch, from: Location, to: null, quantity, performedBy,
        reason: $"Order {allocation.OrderRef}");
}
```

The application layer adds the returned movement to the ledger and calls `SaveChangesAsync` **once**:
the changed `StockItem` and the new `StockMovement` are one Unit of Work. There is no window in which
stock moved but the ledger didn't — they are the same commit or neither happens. This is the entire
reason the Unit of Work from [#13](13-repository-unit-of-work-and-events.md) is a single
`SaveChanges` and not two repository "saves".

The ledger table is mapped to match its nature — **append-only, so no concurrency token:**

```csharp
builder.ToTable("stock_movements");
builder.HasKey(m => m.Id);
builder.Property(m => m.Type).HasConversion<string>().HasColumnName("type").HasMaxLength(24);
builder.Property(m => m.From).HasConversion(x => x!.Value, v => LocationCode.Of(v)).HasColumnName("from_location");
builder.Property(m => m.To).HasConversion(x => x!.Value, v => LocationCode.Of(v)).HasColumnName("to_location");
builder.OwnsOne(m => m.Quantity, q => QuantityMap.Configure(q, "qty"));
builder.HasIndex(m => new { m.Sku, m.OccurredAt });
// no xmin: rows are inserted once and never updated.
```

> **Trade-off — discipline in the domain instead of a trigger in the database.** We could enforce
> "every stock change writes a movement" with a database trigger. We put it in the aggregate instead
> (behaviors return the movement; the app persists both). The cost is that every call site must
> persist what it's handed. The win is that the rule is *testable without a database* and reads in
> the domain language — and a future reader sees it in `StockItem`, not buried in DDL.

## 2. Owned collection on the aggregate, second aggregate by reference

Two-stage allocation (Part I) splits into two persistence shapes, and they're different on purpose:

- A **soft `StockReservation`** is its own aggregate — its own table, its own `xmin`, looked up by
  order. It protects available-to-promise without pinning stock.
- A **hard `Allocation`** lives *inside* `StockItem` — an owned collection, because "allocated ≤ on
  hand" is a `StockItem` invariant and must travel with it.

```csharp
builder.OwnsMany(s => s.Allocations, a =>
{
    a.ToTable("allocations");
    a.WithOwner().HasForeignKey("stock_item_id");
    a.Property(x => x.Id).HasConversion(id => id.Value, v => new AllocationId(v)).HasColumnName("id");
    a.HasKey("stock_item_id", "Id");
    a.Property(x => x.ReservationId).HasConversion(id => id.Value, v => new StockReservationId(v));
    a.Property(x => x.Status).HasConversion<string>().HasColumnName("status").HasMaxLength(16);
    a.OwnsOne(x => x.Quantity, q => QuantityMap.Configure(q, "qty"));
});

builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
```

The `Allocation` carries its `StockReservationId` as a plain converted column — a reference *by
value* to the other aggregate, never an EF navigation across the boundary. The repository loads a
`StockItem` with its allocations in one shot (owned data comes for free, as in
[#14](14-persisting-an-aggregate-with-ef-core.md)); the reservation is loaded separately when needed.

> **Trade-off — one concept, two storage shapes.** It would be "simpler" to make `Allocation` a
> standalone table with FKs both ways. We don't: the allocation's invariant belongs to `StockItem`,
> so it's owned; the reservation's invariant (≤ available-to-promise) is its own, so it's an
> aggregate. Letting the *consistency boundary* decide the storage shape — not the ER diagram — is
> what keeps the two stages from leaking into each other.

## 3. The query EF can't translate — and the honest workaround

`ListBySkuAsync` sums a SKU's stock across a warehouse into available-to-promise. A location code is
`<warehouse>-<aisle>-...`, so "this warehouse's stock" is every item whose location *starts with* the
warehouse code. But `Location` is a **value-converted column** — EF maps the whole `LocationCode`
through a converter and can't translate `StartsWith` against it. So we narrow in the database by what
*does* translate (SKU equality), and filter the warehouse prefix in memory:

```csharp
public async Task<IReadOnlyCollection<StockItem>> ListBySkuAsync(
    Sku sku, WarehouseCode warehouse, CancellationToken ct = default)
{
    var prefix = warehouse.Value + "-";
    var items = await context.StockItems.Where(s => s.Sku == sku).ToListAsync(ct);
    return items.Where(s => s.Location.Value.StartsWith(prefix, StringComparison.Ordinal)).ToList();
}
```

> **Trade-off — correctness over a clever SQL translation.** We could drop the value converter and
> store a raw string to make `StartsWith` translate, or denormalise a `warehouse` column onto every
> stock row. Both trade a domain type or a duplicated fact for query convenience. Instead we keep the
> `LocationCode` type and accept an in-memory filter on a *per-SKU* result set — which is small by
> design (the aggregate is one SKU+batch+location). If a SKU ever spans enough locations for this to
> bite, the fix is local to this one repository method, behind the port.

## 4. Two bounded contexts, one database

Topology and Inventory are two contexts but live in the **same Warehousing service** — because their
hard invariants (temperature, capacity) must validate in one transaction (the 3-services decision
from [#3](03-bounded-contexts-and-use-cases.md)). They share one Postgres database, **one schema
each**: `topology` and `inventory`. Each `DbContext` owns its schema and keeps its *own* EF
migrations-history table, so the two never collide. The host registers both against the same
connection name:

```csharp
// Both contexts share the "warehouse" database, one schema each.
builder.AddNpgsqlDbContext<TopologyDbContext>("warehouse");
builder.AddNpgsqlDbContext<InventoryDbContext>("warehouse");
builder.Services.AddTopologyRepositories();
builder.Services.AddInventoryRepositories();
```

The Aspire AppHost still treats it as **database-per-service** — one database resource per service,
referenced by name:

```csharp
var warehouseDb = postgres.AddDatabase("warehouse");
builder.AddProject<Projects.Warehouse_Warehousing_Api>("warehousing-api")
    .WithReference(warehouseDb).WaitFor(warehouseDb);
```

> **Trade-off — schema-per-module inside database-per-service.** Two contexts in one database means a
> careless query *could* join across schemas — the database wouldn't stop it. We rely on the module
> boundary (separate `DbContext`s, separate repositories, no cross-schema navigation) and, later,
> architecture tests to enforce what the database permits. The payoff is that Topology and Inventory
> commit a cross-context invariant in a single local transaction, with no distributed-transaction
> machinery — exactly the coupling we *chose* by putting them in one service.

## Events in flight (the seam, not yet the mechanism)

Every behavior above raised a domain event — `StockPicked`, `StockReceived`, `StockAllocated`,
`BatchBlocked`. Per [#13](13-repository-unit-of-work-and-events.md) they're collected, not sent;
after `SaveChangesAsync` succeeds, the application drains them with `DequeueDomainEvents()` and turns
the ones that matter to other services into **integration events** written to the transactional
outbox. We've built the seam — the place where that happens — but not the mechanism. That's the next
post, and it earns its own, because "publish after save" done naively is a bug.

## What's next

The persistence layer is real: aggregates load and save through ports, the ledger and its stock
commit as one Unit of Work, and the whole Warehousing service stands up with `dotnet run`. But a
domain event that never leaves the service is a fact nobody else can react to. **The transactional
outbox** — why "save then publish" loses messages, how an idempotent inbox makes redelivery safe, and
the 2025 OSS-licensing story behind our messaging choice — is next.

**Post #16: The transactional outbox — making "publish after save" safe →**
