# 0007. Measurements

- **Status:** Accepted
- **Date:** 2026-10-21

## Context

Product data is full of physical quantities such as weight, dimensions and volume. Channels often require a specific unit; Google Merchant, for example, accepts shipping weight only in certain units. Sortiment needs measurement values that carry their unit and can be converted.

Teams will eventually need units that Sortiment does not ship with. Supporting user-defined measurement families means configuration entities, admin screens and storage. That is not needed yet, but adding it must not require migrating data or breaking the API.

## Decision

- A `MeasurementFamily` groups units that measure the same quantity. Each `MeasurementUnit` has a code and a conversion factor relative to the family's reference unit. Conversion is linear, so quantities that also need an offset, such as temperature, are not supported.
- Sortiment ships three built-in families in `MeasurementFamilies`, with exact conversion factors (imperial and US units use their legal metric definitions):
  - `weight`: `mg`, `g`, `kg`, `oz`, `lb`
  - `length`: `mm`, `cm`, `m`, `in`, `ft`
  - `volume`: `ml`, `cl`, `dl`, `l`, `us_fl_oz`
- Built-in families are ordinary `MeasurementFamily` instances, not enum members. No code branches on whether a family is built-in. Their codes are reserved.
- A measurement attribute is created with `AttributeDefinition.CreateMeasurement(code, family)`. The family is fixed at creation and held by the attribute, so validation needs no lookup. Imports and the API resolve family codes at the boundary, through `MeasurementFamilies.Find`.
- A `MeasurementValue` is an amount and a `UnitCode`. Values are compared exactly as given: `1 kg` and `1000 g` are different values. Validation checks that the unit belongs to the attribute's family.
- Families and units are referenced by code in values, imports and the API.

## Consequences

- Feeds and exports can convert measurements to whatever unit a channel requires.
- User-defined families can be added later as another source of `MeasurementFamily` instances next to the built-in ones, without changing stored values, attributes or validation. When that is built, we still need to decide whether users may extend built-in families, and whether custom families are entities with IDs (ADR 0002) or reference data like locales.
- Units not in the built-in families cannot be used until user-defined families exist.
- Measurement attributes have no range or decimal-place constraints. A range would need unit conversion, and none of the built-in quantities has a use case for it yet.
