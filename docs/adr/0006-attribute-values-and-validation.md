# 0006. Attribute values and validation

- **Status:** Accepted. The constraints section is superseded by [0009](0009-attribute-settings-per-type.md).
- **Date:** 2026-10-21

## Context

Products hold values for attributes. Each value must match its attribute's type, respect the attribute's constraints and options, and apply to the right locale and channel. Imports, the API, the admin UI and future change tracking all create and compare values, so the value model sits at the center of Sortiment.Core.

## Decision

- **Values are a closed set of immutable records:** `TextValue`, `BooleanValue`, `NumberValue`, `DateValue`, `SingleSelectValue` and `MultiSelectValue`.
  - `AttributeValue` has a `private protected` constructor, so only Sortiment.Core can add value types.
  - Values are compared by value. Multi-select values compare as sets: order does not matter, and display order comes from the attribute.
  - `Text`, `TextArea` and `Identifier` attributes all use `TextValue`. Their differences are validation rules.
- **Value constructors only reject `null`.** All other rules depend on the attribute, so `AttributeDefinition.Validate(value, scope)` is the single place where a value is validated. It checks:
  - the type match
  - the constraints
  - the options
  - the scope

  Every problem is reported, not only the first.
- **Empty is not a value.** Blank text and an empty multi-select are rejected with `value.empty`. Clearing a value means removing it, so "no value" has exactly one representation. That matters for completeness and change tracking.
- **Constraints are properties of the attribute definition:**
  - `MaxLength` applies to textual attributes.
  - `MinValue`, `MaxValue` and `DecimalPlaces` apply to numbers.

  Setting one on an attribute type that doesn't support it fails with `attribute.constraint_not_supported`. Constraints can change after values exist; values that break a tightened constraint are reported the next time they are validated.
- **Text and number rules:**
  - Text length is counted in Unicode code points, like PostgreSQL's `varchar(n)` and most channel specifications.
  - `Text` and `Identifier` values must be a single line.
  - Identifiers cannot start or end with whitespace.
  - All numbers are `decimal`. `DecimalPlaces = 0` means whole numbers only, and trailing zeros don't count (`1.50` has one decimal place).
- **Select values reference options by ID** (ADR 0002). Validation reports unknown and duplicate options.
- **`ValueScope` holds an optional locale and an optional channel ID:**
  - Localizable attributes require a locale, and scopable attributes require a channel.
  - Other attributes must not have them.
  - Whether a locale is active in a channel is checked where channels are available, not by the attribute.

## Consequences

- Callers can build values freely and validate them in one call, which suits imports that collect all problems per row.
- Diffing and change tracking can rely on value equality.
- Adding a value type means changing Sortiment.Core, by design. This is how the Measurement, Price and MediaReference types will be added.
- Validation needs the attribute definition at hand, so products are validated against a resolved set of attributes rather than looking them up.
