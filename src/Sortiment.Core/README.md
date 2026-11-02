# Sortiment.Core

A flexible, typed product model for .NET: attributes, families, variants, localized and channel-scoped values, assets, validation and completeness.

`Sortiment.Core` has no dependencies and performs no I/O. It can be used on its own, without the rest of Sortiment.

## Variants

Products that come in several colors, sizes or other variations are modeled with three types:

- A `FamilyVariant` describes one way a family's products vary: its axes, such as `color` and `size`, and the other attributes that are set per variant.
- A `ProductModel` holds the values its variants share, such as the name and description.
- A variant is an ordinary `Product` with a `ProductModelId`. It holds its axis values and the other variant attributes, and sees its model's values through `EffectiveValues`.

Operations on a variant take a `VariantContext`: the variant's product model together with the `VariantSchema` of its family variant. See [ADR 0015](../../docs/adr/0015-variants.md).

## Loading stored data

New domain objects are created with `Create`, which assigns a new ID. Objects loaded from storage are rebuilt with `Restore`, which takes the stored ID and state. For example, `Product.Restore`, `Family.Restore` and `AttributeDefinition.Restore`. `Restore` checks that the data is structurally sound but does not revalidate it against constraints that may have changed since it was saved. Use `Product.Validate` for that.

> **Status:** early development. The API is not stable yet.
