# 0011. Restoring aggregates from stored state

- **Status:** Accepted. Restoring measurement families is amended by [0013](0013-measurement-identity.md).
- **Date:** 2026-11-02

## Context

Domain types in `Sortiment.Core` are created through `Create` factories that generate a new ID and start from an empty or default state. Storage has to load products, families, attributes, channels and assets with the IDs and state they were saved with, and so do test fakes and any other `IProductSource` implementation.

Core cannot depend on EF Core or any other persistence technology (ADR 0003), so persistence conventions such as parameterless constructors or private-field mapping do not belong in it. Loading must also cope with data that was valid when it was saved but breaks a constraint that was tightened later (ADR 0006).

## Decision

### An explicit `Restore` factory

- Every aggregate has a static `Restore` factory next to `Create`. It takes the aggregate's full state: its ID, its code or SKU, its flags and its child collections.
- Storage maps its own persistence model to `Restore`. Core has no persistence-specific constructors or mapping conventions.
- Factories that create different kinds of the same aggregate get a matching `Restore`, for example `AttributeDefinition.RestoreMeasurement` next to `CreateMeasurement`.
- References to other aggregates are restored by ID. For example, `Family.Restore` takes attribute IDs, and `AttributeDefinition.RestoreAssetCollection` takes an `AssetFamilyId`. Restoring one aggregate never requires loading another.

### Structural invariants only

- `Restore` checks the invariants that make an aggregate structurally sound. Examples:
  - A channel has at least one locale and no duplicates.
  - A family lists each attribute once and only requires its own attributes.
  - A product has at most one value per attribute and scope.
  - Identifier attributes are neither localizable nor scopable.
- `Restore` does not recheck business constraints that can change after data is saved. Product values are not validated against attribute definitions, and asset files are not validated against their asset family's limits. `Product.Validate` and `AssetFamily.ValidateFile` report those problems.

### Broken invariants throw

- `Restore` throws `ArgumentException` when stored state breaks an invariant. The message lists every problem found.
- Stored data that breaks an invariant is an infrastructure failure, not an expected failure (ADR 0005), so it is not returned as a `Result`.

### Settings and options

- Attribute settings are restored through their existing setters, such as `TextSettings.SetMaxLength`.
- Select options keep their IDs, so they are restored with `SelectSettings.RestoreOption`.

### What is not restored

- Derived results such as completeness are recalculated rather than restored. A copy kept for search or filtering is a storage concern (ADR 0010).
- The built-in measurement families have no ID. Storage refers to them by code and resolves them with `MeasurementFamilies.Find`.

### Public state is enough

The public properties of every aggregate hold everything needed to restore it. Round-trip tests create an aggregate through the normal API, restore it from its public properties and compare the two.

## Consequences

- Storage adapters, test fakes and other sources can load aggregates without Core knowing anything about how they are stored.
- Any new state added to an aggregate must be public and accepted by `Restore`, or the round-trip tests fail.
- Data that breaks a tightened constraint can still be loaded, shown and fixed, instead of making the product impossible to open.
- Each aggregate has a second factory to maintain. Where `Create` and `Restore` check the same invariants, they share the check.
- `Restore` trusts its input more than `Create` does, so it must only be used for data that has already been through the domain once, such as data from storage.
