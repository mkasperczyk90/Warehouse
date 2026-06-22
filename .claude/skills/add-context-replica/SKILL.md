---
name: add-context-replica
description: Add a local read replica (snapshot) of another bounded context's data, kept fresh by an integration event, inside a backend module (src/Services/*/Modules/*) — a minimal Domain/Replicas entity with an Apply() updater, an Application port + EF repository, a DbContext DbSet + EF config + migration, and an event consumer that upserts it (ADR-0003). Use when a context needs another context's data to enforce an invariant or validate input without a cross-service query.
---

# Add a context replica (event-fed snapshot)

Contexts never query each other; each keeps a **minimal local replica** of just the foreign data it needs,
updated by integration events — eventual consistency is the accepted trade-off
([ADR-0003](../../../docs/adr/0003-replicas-over-cross-service-queries.md)). **Mirror existing replicas**:
`Logistics.Core/Domain/Replicas/CatalogProductSnapshot.cs` (validates ASN SKUs) and
`Warehousing.Inventory/Domain/Replicas/ProductSnapshot.cs` (put-away invariants).

## Steps

1. **Entity — `<Module>/Domain/Replicas/<Name>Snapshot.cs`.** Hold **only** the fields this context needs.
   Public ctor (validates) + private parameterless ctor (EF); private setters; an `Apply(...)` method that
   mutates the projected fields (used to update a tracked row in place). Pick the natural key (e.g. a code).
2. **Port — `Application/Abstractions/I<Name>Repository.cs`.** `FindAsync(key)`, `Add`, `Update`, plus any
   lookup the use case needs (e.g. `FindUnknownAsync(codes)` to flag unknown ids). Implement in
   `Infrastructure/Persistence/<Name>Repository.cs`. Avoid pushing `Contains`/`StartsWith` over a
   value-converted column — load the (small) replica and diff in memory instead.
3. **Persistence.** Add `DbSet<<Name>Snapshot>` to the module's `DbContext`; add
   `Infrastructure/Persistence/Configurations/<Name>SnapshotConfiguration.cs` (table, key, value conversions,
   `snake_case` columns). `ApplyConfigurationsFromAssembly` picks it up. Register the port in
   `Infrastructure/<Module>Infrastructure.cs`.
4. **Migration.** `dotnet ef migrations add Add<Name>Snapshot -p src/Services/<Service>/Modules/<Module>/<Module>.csproj`
   (each module has a design-time `…DbContextFactory`). Inspect the generated `Up` — it should only create the
   one table.
5. **Consumer.** Add a Wolverine handler that projects the source event onto the replica (see
   `add-integration-event`): `Find` → if present `existing.Apply(...)`; else `Add(new <Name>Snapshot(...))`;
   then `SaveChangesAsync`. **Upsert by mutating the tracked `existing`** — never `Update()` a fresh instance
   with the same key (EF throws on the duplicate-tracked key). Wire the source exchange→queue binding in the
   consuming `Program.cs`.

Map only what the event carries; default the rest (e.g. the minimal `ProductDefinedV1` has no weight/volume,
so `ProductSnapshot` seeds those neutrally) and note the gap.

## Finish

`dotnet build Warehouse.slnx`; the dev host applies the migration on startup (`Database.MigrateAsync`).
Confirm the replica fills by publishing the source event against the Aspire host.
