# #6 — The SharedKernel: the most dangerous package in your solution

*Series: Building a real microservices application, brick by brick.
Previous: [#5 Archetypes in practice](05-archetypes-in-practice.md).
Code: [`src/SharedKernel/Warehouse.SharedKernel`](../../src/SharedKernel/Warehouse.SharedKernel).*

---

Every microservices codebase grows a `Common` project. It starts innocently — a base
entity, a string helper — and two years later every service depends on it, nobody can
change a line without a cross-team meeting, and your "independent" services deploy in
lockstep. The distributed monolith was not built by the architecture; it was built by
`Common.dll`.

DDD has a name for sharing model fragments between contexts: the **Shared Kernel** — and
Evans defines it as a *liability you accept knowingly*: whatever is inside requires
coordination between all its consumers to change. The design goal is therefore not
"what can we share?" but **"what is so stable we can afford to share it?"**

## Our admission rules

A type enters the SharedKernel only if **all four** hold:

1. It is part of the **ubiquitous language of every context** (not just two of them).
2. It is a **closed concept** — its rules are physics or standards, not business policy
   (kilograms, ISO currency codes, temperature ranges).
3. It has **zero dependencies** — no EF, no messaging, no ASP.NET. The csproj has no
   `PackageReference` at all, and an architecture test keeps it that way.
4. Changing it would mean **every context was wrong about the same thing** — in which case
   coordinated change is what you want.

## What made it in

### Base types — the grammar of the model

```csharp
public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];
    protected void Raise(IDomainEvent domainEvent) { ... }
    public IReadOnlyList<IDomainEvent> DequeueDomainEvents() { ... } // infra drains after save
}

public class DomainException : Exception
{
    public string ErrorCode { get; } // stable API surface: "quantity_insufficient"
}
```

`Entity`, `AggregateRoot`, `IDomainEvent`, `DomainException`. Note what `DomainException`
carries: a **stable error code**. Messages are for humans and may change; codes are the
contract that API ProblemDetails and frontends bind to. That decision — made once, here —
spares every module from inventing its own error convention.

### The measurement value objects — physics, not policy

`Quantity` + `UnitOfMeasure`, `Weight`, `Volume`, `TemperatureRange`, `Money`. These pass
rule 2 perfectly: a kilogram means the same thing in every context, and
`TemperatureRange.Contains(other)` is arithmetic. They are also where the archetype
investment from post #5 pays compound interest — every context gets non-negative,
unit-safe quantities for free.

### `Address` — the borderline case that made it in

`Address` (used by warehouses, customer shipping, party contacts) is a closed concept —
ISO country codes, postal format — and means the same thing everywhere. It passes all four
rules, so in it goes.

### `Sku` — the borderline case we got *wrong* (and how the rules caught it)

The first cut of this kernel contained `Sku`. The argument felt airtight: every context
speaks in SKUs, so the **format** (grammar `^[A-Z0-9][A-Z0-9-]{1,31}$`) is ubiquitous
language even if the **meaning** is Catalog-only. Share the grammar, keep the dictionary in
Catalog. Clean, right?

A review caught it. Run `Sku` against rule 2 ("closed concept, not business policy") honestly
and it fails: a SKU is *not* the same kind of thing in every context. In **Catalog** a SKU is
strictly validated and linked to an EAN. In **Logistics** a SKU is often just *what the
scanner read* — including a code that isn't a valid catalog SKU yet (receiving must accept it
and flag it for clarification — that's UC-01). A shared strict type would have either rejected
legitimate scanner input or forced Logistics to smuggle raw strings around its own type. Worse,
the shared `Sku` would have become a magnet for "just one more" validation that some context
needs and another doesn't.

So `Sku` moved **out**: Catalog owns the strict `Sku`, Inventory keeps a lighter `Sku` (stock
only exists for cataloged products, so it's known-good), and Logistics uses a deliberately
loose `ProductCode`. Three types, one shared *convention* (the code format). This is the whole
thesis of the post, and we almost violated it on page one — which is exactly why the four
rules are written down instead of left to taste.

## What was deliberately kept out

This list matters more than the previous one:

| Excluded | Lives instead in | Why |
|---|---|---|
| `Sku` | Catalog (strict) / Inventory (light) / Logistics (`ProductCode`, loose) | "SKU" means different things per context; a shared strict type rejects legitimate scanner input and attracts universal validation |
| `StorageRequirement` | Catalog **and again** (as snapshot fields) in Inventory | It is business policy, and each context needs the freedom to evolve its copy |
| `LocationCode` | Topology **and again** in Inventory | Same string grammar, two types — Inventory's copy can't accidentally grow Topology behavior |
| Status enums (`StockStatus`, `OrderStatus`…) | Each owning module | A status is the heart of a context's process — sharing it couples state machines |
| Strongly-typed IDs (`StockItemId`, `PartyId`…) | Each owning module | An ID type names a concept the context owns |
| Repository interfaces, `Result<T>`, mediator abstractions | Nowhere / later, per module | Infrastructure opinions don't belong in the domain's shared language |

> **Trade-off — duplication as a feature:** `LocationCode` is defined twice with the same
> regex. Painful for the DRY reflex, and we did it on purpose. The duplication costs us a
> few lines; sharing the type would cost us independence — every future Topology-specific
> behavior on that type would silently ship into Inventory. **DRY applies within a
> boundary; across boundaries, decoupling beats deduplication.** When the *grammar* itself
> changes (it's part of the ubiquitous language), changing two files in one repo is the
> coordination Evans warned about — made small and visible.

## SharedKernel ≠ Contracts

Easy to confuse, fundamentally different artifacts:

| | `Warehouse.SharedKernel` | `Warehouse.Contracts` |
|---|---|---|
| Contains | Language primitives (VOs, base types) | **Integration events** between services |
| Consumed by | Domain code, at compile time | Message consumers, at runtime |
| Change policy | Coordinated, rare | **Additive-only**: `ProductDefinedV1` is frozen forever; changes ship as `V2` |
| Coupling created | Compile-time, within the repo | Wire-format, across deployments — strictly versioned |

The SharedKernel may evolve carefully; a published contract may not evolve at all.

## Keeping it honest

Rules that aren't enforced are wishes. These guardrails are sketched here and land for
real in **Part II**, when the test projects go in — the kernel stays pure, and modules
never reference each other (only SharedKernel and Contracts):

```csharp
// Planned (Part II) — NetArchTest + xUnit. The .Should() below is NetArchTest's own
// fluent API; the final assertion is plain xUnit (we don't use FluentAssertions —
// it went commercial in 2025, same story as MediatR/MassTransit).
var result = Types.InAssembly(typeof(Quantity).Assembly)
    .Should().NotHaveDependencyOnAny(
        "Microsoft.EntityFrameworkCore", "MassTransit", "Microsoft.AspNetCore")
    .GetResult();

Assert.True(result.IsSuccessful);
```

Plus a social rule: **adding a type to the SharedKernel requires touching this blog post**
(the admission-rules section above). If you can't argue all four rules hold, it goes into
a module, and duplication is fine.

## Where we are after five posts

We have a domain that a warehouse manager would recognize, five contexts with honest
boundaries, models guarded by invariants, and a shared language small enough to stay
shared. **Zero infrastructure so far** — and that was the point: every line written in
Part I will survive any future change of database, broker or framework, because none of
it knows those things exist.

## What's next

Part I has one thing left: the bill. [Post #7](07-the-price-tag.md) totals up the receipt for our
domain decisions — where the model might creak under ten thousand pallets — and
[post #8](08-wrapping-up-part-i.md) closes the domain part with a retrospective. *Then* we open the
IDE, and Part II turns the whiteboard into running code.
