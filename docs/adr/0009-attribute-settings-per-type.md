# 0009. Attribute settings per type

- **Status:** Accepted
- **Date:** 2026-10-21

## Context

ADR 0006 put constraints directly on `AttributeDefinition` as properties. As attribute types were added, the definition gained a growing set of properties that each apply to only some types: maximum length, range, decimal places, options, measurement family, asset family and maximum count. Every setter had to check the attribute type at runtime and return `attribute.constraint_not_supported` or `attribute.options_not_supported`. The rules for one type were also split between the definition, which held the constraints, and a separate validator, which applied them.

Every new attribute type would have added more properties of this kind.

## Decision

- **`AttributeDefinition` holds only what all attributes share:** ID, code, type, the localizable and scopable flags, and validation of a value and its scope.
- **Type-specific settings live in `AttributeDefinition.Settings`,** an `AttributeSettings` object whose type matches the attribute type:

  | Attribute types | Settings | Contents |
  |---|---|---|
  | `Text`, `TextArea`, `Identifier` | `TextSettings` | `MaxLength` |
  | `Number` | `NumberSettings` | `MinValue`, `MaxValue`, `DecimalPlaces` |
  | `SingleSelect`, `MultiSelect` | `SelectSettings` | Options |
  | `Measurement` | `MeasurementSettings` | Measurement family |
  | `AssetCollection` | `AssetCollectionSettings` | Asset family, `MaxCount` |
  | `Boolean` | `BooleanSettings` | Nothing yet |
  | `Date` | `DateSettings` | Nothing yet |

- **Each settings type owns its constraints, the methods that change them, and the validation of its type's values.** All rules for a type are in one class.
- **The set of settings types is closed,** like the set of value types (ADR 0006).
- **Callers reach the settings through `attribute.SettingsAs<TSettings>()`,** which throws when the attribute has other settings, or by pattern matching on `attribute.Settings`.
- **A setting that doesn't apply to a type can no longer be called on it.** The error codes `attribute.constraint_not_supported` and `attribute.options_not_supported` are removed.

This replaces the constraints section of ADR 0006. The rest of ADR 0006 still applies.

## Consequences

- New attribute types add a settings class instead of more nullable properties on the definition.
- Changing constraints on the wrong attribute type is caught at compile time rather than at runtime.
- Storage and the API can serialize settings per type, and the admin UI can render one settings editor per type.
- Callers write `attribute.SettingsAs<TextSettings>().SetMaxLength(150)` instead of `attribute.SetMaxLength(150)`, which is slightly longer.
