---
name: add-service-slice
description: Add a use-case vertical slice (command/query + handler) and its minimal-API endpoint to a backend bounded-context module (src/Services/*/Modules/*) following the established conventions — one folder per use case under Application/ (ADR-0007), repository ports + IUnitOfWork/outbox, a thin endpoint group, and DomainException→HTTP mapping. Use when adding or extending a backend write action (command) or read (query) on an aggregate, or exposing it over HTTP.
---

# Add a service slice (backend use case)

Recipe for a .NET bounded-context module under `src/Services/<Service>/Modules/<Module>`. Each module is
Clean Architecture: `Domain/` (aggregates + behaviour), `Application/` (use cases + `Abstractions/` ports),
`Infrastructure/` (EF Core). Read the module's `Application/README.md` and
[ADR-0007](../../../docs/adr/0007-vertical-slices-in-application-layer.md) first. **Mirror an existing slice**
rather than inventing structure: command → `Logistics.Core/Application/AnnounceDelivery` or `ConfirmReceipt`
(publishes an event); query → `Logistics.Core/Application/GetDelivery` / `ListDeliveries`; endpoints →
`Warehouse.Logistics.Api/InboundEndpoints.cs`.

The domain is already built — invariants live in the aggregate. A slice **orchestrates**: load → call a
domain method → persist. Never re-implement a rule in the handler.

## A command (write)

1. **`Application/<UseCase>/<UseCase>Command.cs`** — a `record` of primitives (ids as `Guid`, codes as
   `string`, no domain types).
2. **`Application/<UseCase>/<UseCase>Handler.cs`** — ctor-inject the repo port(s) + `IUnitOfWork`. Load via
   `GetByIdAsync` (`?? throw new KeyNotFoundException(...)` → 404), build value objects from the command,
   call the aggregate's behaviour, `repo.Update(agg)`, `await unitOfWork.SaveChangesAsync(ct)`. If the slice
   raises an integration event, inject `IDbContextOutbox<TDbContext>` instead and
   `PublishAsync` + `SaveChangesAndFlushMessagesAsync` (see `add-integration-event`).
3. **Register** the handler in `Application/<Module>Application.cs` (`AddScoped<...Handler>()`, wired from
   `Program.cs` via `Add<Module>Application()`). Ensure `IUnitOfWork → DbContext` is registered in
   `Infrastructure/<Module>Infrastructure.cs`; add any new port + its EF impl there too.

## A query (read)

1. **`Application/<UseCase>/<UseCase>Query.cs`** holding the query record, the DTO record(s), and the handler.
2. Inject the **DbContext** directly; `AsNoTracking()`; project to scalars on the server and map the enum/
   DTO **in memory** (translating `enum.ToString()` server-side fails — see `ListDeliveries`). Return `null`
   for a missing aggregate so the endpoint can 404.

## Expose it (HTTP)

1. **`<Service>.Api/<Area>Endpoints.cs`** — `app.MapGroup("/<context>/<resource>")`; each route injects the
   handler, maps the request (route id + body) onto the command, and returns `Results.Created`/`NoContent`/
   `Ok`. Keep request DTOs for `{id}`-plus-body routes in this file.
2. In `Program.cs`: `builder.Services.Add<Module>Application()`, `AddExceptionHandler<DomainExceptionHandler>()`
   + `AddProblemDetails()`, then `app.UseExceptionHandler()` and `app.Map<Area>Endpoints()`. The handler maps
   `DomainException` → `409 {code,message}` and `KeyNotFoundException` → `404` (the shape the web `api` seam
   parses). Copy `DomainExceptionHandler.cs` from an existing Api if the service has none.
3. If this adds a new public path prefix, add a YARP route+cluster in `src/Gateway/.../appsettings.json`
   (`/api/<prefix>/{**catch-all}` → service, `PathRemovePrefix: /api`).

## Finish

`dotnet build Warehouse.slnx` (warnings are errors) and `dotnet test tests/Warehouse.ArchitectureTests`
(module boundaries; a slice must not cross contexts). If you added persistence, see `add-context-replica`
for the migration step.
