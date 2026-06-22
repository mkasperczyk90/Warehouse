---
name: add-integration-event
description: Add a versioned cross-service integration event and wire its transactional-outbox publisher and Wolverine inbox consumer across the Warehouse microservices — a primitives-only Contracts record, IDbContextOutbox publish in the producing handler, a Wolverine Handle() consumer in the receiving module, and the RabbitMQ fanout exchange/queue binding via AddWarehouseMessaging. Use when one service must react to something that happened in another (choreography), or when adding a new published/consumed event.
---

# Add an integration event (cross-service choreography)

Services own their own databases and never query each other (ADR-0003,
[ADR-0001](../../../docs/adr/0001-microservices-from-day-one.md)); they integrate through events on
RabbitMQ with a **transactional outbox/inbox** (Wolverine). **Mirror existing wiring**: contract →
`Contracts/Warehouse.Contracts/Logistics/GoodsReceiptConfirmedV1.cs` (or `Catalog/ProductDefinedV1.cs`);
publish → `Logistics.Core/Application/ConfirmReceipt`; consume → `Warehousing.Inventory/Application/
ReceiveGoodsReceipt` + `Logistics.Core/Application/Consumers`; the shared setup → `ServiceDefaults/Messaging.cs`.

## 1. The contract (`Warehouse.Contracts`)

`Contracts/Warehouse.Contracts/<Context>/<Event>V1.cs` — a `sealed record` of **primitives only** (no domain
types; ids `Guid`, codes/enums as `string`, lists of nested primitive records). **Additive-only**: never edit
a published `Vn`; a new field ships as `V<n+1>`. Document the rule in the XML summary.

## 2. Publish it (producing service)

In the slice that completes the fact (see `add-service-slice`), inject `IDbContextOutbox<TDbContext>` and:
`await outbox.PublishAsync(new <Event>V1(...))` then `await outbox.SaveChangesAndFlushMessagesAsync(ct)` — the
event row and the aggregate change commit in **one transaction**, relayed after commit. In `Program.cs`'s
`AddWarehouseMessaging(db, (opts, rabbit) => { ... })` add
`opts.PublishMessage<<Event>V1>().ToRabbitExchange("<exchange>", e => e.ExchangeType = ExchangeType.Fanout);`.
One fanout exchange per producing context (`catalog`, `logistics`, `inventory`).

## 3. Consume it (receiving service)

1. **`Application/Consumers/<Event>Consumer.cs`** (or a slice folder) — a class with
   `public async Task Handle(<Event>V1 message, <deps...>, CancellationToken ct)`. Wolverine discovers it by the
   `Handle` convention and resolves deps from DI. Ctor-inject the deps (not method params) so the analyzer
   doesn't force a static method. Make it **idempotent** — guard on aggregate state and no-op on a redelivery
   rather than throwing (which dead-letters a duplicate). If the consumer itself publishes a reply, use
   `IDbContextOutbox<TDbContext>` (inbox→process→outbox in one go).
2. In the receiving `Program.cs`'s `AddWarehouseMessaging` callback, bind + listen:
   `rabbit.BindExchange("<exchange>", ExchangeType.Fanout).ToQueue("<service>.<exchange>");`
   `opts.ListenToRabbitQueue("<service>.<exchange>");` and ensure
   `opts.Discovery.IncludeAssembly(typeof(<Module>DbContext).Assembly);` so handlers in the module are found.

## Packages

The module hosting a publisher/consumer needs `WolverineFx` + `WolverineFx.EntityFrameworkCore` (the Api adds
`WolverineFx.Postgresql` + `WolverineFx.RabbitMQ`). The store/transport/outbox themselves are configured once
in `ServiceDefaults/Messaging.cs::AddWarehouseMessaging` — don't re-wire them per service. If a service has no
messaging yet, add `builder.AddWarehouseMessaging("<dbConnName>", (opts, rabbit) => { ... })` to its `Program.cs`.

## Finish

`dotnet build Warehouse.slnx`. End-to-end check needs the Aspire host (Postgres + RabbitMQ): `dotnet run` the
AppHost, trigger the producer, and confirm the event reaches the consumer's queue and the reply (if any) flows
back. Exchanges/queues auto-provision in Development.
