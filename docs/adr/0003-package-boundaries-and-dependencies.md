# 0003. Package boundaries and dependency rules

- **Status:** Accepted
- **Date:** 2026-10-21

## Context

Sortiment is a family of NuGet packages, and each package should be useful on its own. That goal needs a precise meaning. Read strictly, "usable without the rest of Sortiment" would prevent `Sortiment.Import` from using the product model in `Sortiment.Core`, which would force it to duplicate validation and value types.

The abstractions that connect the packages also need a home. Import writes to an `IProductTarget`, and feeds read from an `IProductSource`. If those interfaces lived in the Import and Feeds packages, any storage implementation would have to reference both.

## Decision

Dependencies point inward:

```
Sortiment.Core                     no dependencies (BCL only)
Sortiment.Import                   → Core
Sortiment.Import.Csv/.Excel/.Json  → Import
Sortiment.Feeds.*                  → Core
Sortiment.Storage.PostgreSql       → Core
Sortiment.Api                      → Core, Storage, Import
Sortiment.Admin                    → Api, over HTTP only
```

- `Sortiment.Core` depends on nothing except the .NET base class library. It does no I/O, no logging and no configuration. Build-only tools that add no runtime dependency (such as MinVer) are allowed.
- "Usable on its own" means usable without Storage, Api or Admin. Any package may depend on `Sortiment.Core`, because Core adds no transitive dependencies.
- `IProductTarget`, `IProductSource` and the types they exchange are defined in `Sortiment.Core`. They are async interfaces that accept a `CancellationToken`. Defining them performs no I/O, so this keeps Core pure.
- Format readers (`Sortiment.Import.Csv`, `.Excel`, `.Json`) produce a neutral stream of records that does not depend on the product model, so they are useful even without it. The mapping engine in `Sortiment.Import` turns records into product changes.
- Nothing references `Sortiment.Api` or `Sortiment.Admin`. The admin UI uses the public HTTP API like any other client. A shared contracts or client package may be added when step 4 needs it.
- `Sortiment.ArchitectureTests` enforces these rules.

## Consequences

- Storage implements `IProductTarget` and `IProductSource` without depending on Import or Feeds.
- Import and Feeds share Core's validation and value types instead of duplicating them.
- Core's public surface grows with the target and source abstractions, so they must be designed carefully. They are hard to change after release.
- Consumers who only want CSV parsing still get Core as a dependency. This is acceptable, since Core is small and has no dependencies of its own.
