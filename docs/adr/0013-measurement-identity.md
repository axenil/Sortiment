# 0013. Measurement identity

- **Status:** Accepted
- **Date:** 2026-11-02

## Context

ADR 0007 references measurement families and units by code: a measurement attribute holds its family, and a `MeasurementValue` stores the code of its unit. ADR 0002 and the architecture principles say that definition entities have both an ID and a code, and that internal references always use IDs.

Sortiment will support user-defined measurement families. Users will then be able to correct or rename the code of a family or a unit. If stored values refer to units by code, every such rename means rewriting product values. Attribute options already avoid this: a select value stores the option's ID, so the option's code can change freely. Nothing is stored yet, so changing the reference now costs nothing.

## Decision

- A `MeasurementFamily` has a `MeasurementFamilyId` and a `MeasurementFamilyCode`. A `MeasurementUnit` has a `MeasurementUnitId` and a `UnitCode`. Both IDs are strongly typed, as in ADR 0002.
- A `MeasurementValue` is an amount and a `MeasurementUnitId`. Conversion targets a unit ID as well.
- A measurement attribute still holds its `MeasurementFamily`, so validation needs no lookup. Storage keeps the family's ID and resolves the family when restoring the attribute.
- Built-in families and their units have fixed IDs, written into the code, so they are the same in every installation and stored references stay valid across upgrades. These IDs are part of the public contract and never change.
- Imports, feeds and the API keep referring to families and units by code. They resolve codes at the boundary, through `MeasurementFamilies.Find` and `MeasurementFamily.FindUnit`, which accept either a code or an ID.
- Errors about a unit that is only known by ID report it under `unit_id`, following ADR 0012.

ADR 0007 is amended by this decision. ADR 0011 is amended as well: measurement families are restored and stored by ID, with `MeasurementFamily.Restore` for user-defined families.

## Consequences

- Units and families of user-defined measurement families can be renamed without touching stored values.
- Callers that show or export a measurement look up the unit to get its code.
- The fixed IDs of the built-in families and units must never be changed or reused. A test pins them.
- Whether users may extend built-in families is still decided when user-defined families are built (ADR 0007).
