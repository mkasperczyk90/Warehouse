---
name: add-bi-export
description: Export an already-published integration event to the BI Bronze landing zone on S3 (docs/BI_Plan.md) so Databricks Auto Loader can pick it up later — a sink-only Wolverine consumer that maps the event to a BronzeEvent and writes it via the shared IBronzeExportWriter (Warehouse.ServiceDefaults.BiExport), on its own dedicated queue so a slow/backed-up export can never affect the business flow. Use when a metric in the BI plan's Gold/metrics catalog needs an event that isn't landing in Bronze yet.
---

# Add a BI export consumer (Bronze landing)

Bronze export is a **side observer**, never a participant in the domain transaction: a service
already publishes its integration events for other services to consume (`add-integration-event`);
BI export just adds one more, dedicated, sink-only subscriber to that same stream so a copy lands
in S3 for Databricks (`docs/BI_Plan.md` §5/§11). **Mirror the reference implementation**:
`Warehouse.Warehousing.Inventory/Application/BiExport/GoodsReceivedBiExportConsumer.cs`, wired in
`Warehouse.Warehousing.Api/Program.cs`. The reusable machinery — `BronzeEvent`,
`IBronzeExportWriter`, the S3/Parquet writer, `BiExportOptions`, `AddBiExport()` — lives in
`Warehouse.ServiceDefaults/BiExport/` and needs **no changes** for a new event; it's shared by
every service.

## Steps

1. **Consumer — `<Module>/Application/BiExport/<Event>BiExportConsumer.cs`.** A small class,
   constructor-injecting `IBronzeExportWriter` (from `Warehouse.ServiceDefaults.BiExport`) — nothing
   else, no repositories, no `IUnitOfWork`, no outbox:
   ```csharp
   public sealed class <Event>BiExportConsumer(IBronzeExportWriter writer)
   {
       public Task Handle(<Event>V1 message, Envelope envelope, CancellationToken cancellationToken)
       {
           var bronzeEvent = new BronzeEvent(
               SourceSystem: "<service>-service",
               EventType: nameof(<Event>V1),
               EventId: envelope.Id.ToString(),   // Wolverine's own message id — stable across redeliveries
               message.OccurredAt,
               Payload: new Dictionary<string, string?> { ["field"] = message.Field /* ... */ });

           return writer.WriteAsync(bronzeEvent, cancellationToken);
       }
   }
   ```
   Payload is a flat `string` map (Bronze is schema-on-read tolerant — typing happens in Silver, see
   `docs/BI_Plan.md` §5/§6). Use `envelope.Id` as `EventId`, not a field composed from the message,
   so a redelivery overwrites the same S3 object instead of writing a near-duplicate.
2. **Wire a dedicated queue in the module's `Program.cs`**, bound to the exchange the event is
   *already* published to (don't reuse a queue another consumer listens to):
   ```csharp
   rabbit.BindExchange("<exchange>", ExchangeType.Fanout).ToQueue("<service>.bi-export");
   opts.ListenToRabbitQueue("<service>.bi-export");
   ```
   If the module already has a `*.bi-export` queue (e.g. `warehousing.bi-export` in Warehousing),
   reuse it — one BI-export queue per service is enough; Wolverine dispatches each bound message
   type to its own `Handle` overload.
3. **Enable export once, per service** (not per event): `builder.AddBiExport();` in the service's
   `Program.cs`, right after `AddServiceDefaults()`. Idempotent to call more than once across
   services sharing a host. No-op until `BiExport:BucketName` is configured (empty by default —
   see `appsettings.json` and `infra/terraform/environment/locals.tf`).

## Why a dedicated queue

The export queue is intentionally **decoupled** from queues that exist to consume *other* services'
streams (e.g. `warehousing.logistics`) — it only ever carries this service's own published events.
If the BI lake or Databricks is ever slow or unreachable, this queue backs up on its own; it can
never delay or block the actual cross-service business flow those other queues carry.

## Packages

None — `Warehouse.ServiceDefaults` already carries `AWSSDK.S3` + `Parquet.Net`. The consuming module
only needs a `ProjectReference` to `Warehouse.ServiceDefaults` (see the Inventory module's
`.csproj` for the reference) to see `IBronzeExportWriter`/`BronzeEvent`.

## Finish

`dotnet build Warehouse.slnx`. To verify end-to-end, set a real `BiExport:BucketName` locally (or
point `S3ParquetBronzeExportWriter` at a local S3-compatible endpoint), trigger the event via the
Aspire host, and check `s3://<bucket>/bronze/<service>-service/<Event>V1/date=.../<event-id>.parquet`
lands. Without a bucket configured, `NullBronzeExportWriter` is registered instead — the consumer
still runs (proving the wiring/discovery is correct) but writes nothing.
