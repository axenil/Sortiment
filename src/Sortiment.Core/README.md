# Sortiment.Core

A flexible, typed product model for .NET: attributes, families, variants, categories, localized and channel-scoped values, assets, validation and completeness.

`Sortiment.Core` has no dependencies and performs no I/O. It can be used on its own, without the rest of Sortiment.

## Family schemas

Product operations take a `FamilySchema`: a family together with the definitions of its attributes, resolved by the caller and reused for every product in a unit of work. If an attribute is added to the family after the schema was created, the schema is no longer current (`IsCurrent`), and operations that use it throw `InvalidOperationException` rather than leave the new attribute out. Create a new schema after changing a family. See [ADR 0017](../../docs/adr/0017-stale-family-schemas.md).

## Variants

Products that come in several colors, sizes or other variations are modeled with three types:

- A `FamilyVariant` describes one way a family's products vary: its axes, such as `color` and `size`, and the other attributes that are set per variant.
- A `ProductModel` holds the values its variants share, such as the name and description.
- A variant is an ordinary `Product` with a `ProductModelId`. It holds its axis values and the other variant attributes, and sees its model's values through `EffectiveValues`. Each attribute is read from the level the family variant gives it, so a value left on the wrong level after the family variant changed does not count until it is moved ([ADR 0018](../../docs/adr/0018-effective-values-of-variants.md)).

Operations on a variant take a `VariantContext`: the variant's product model together with the `VariantSchema` of its family variant. See [ADR 0015](../../docs/adr/0015-variants.md).

## Categories

Categories organize products for presentation, independently of families. A `CategoryTree`, such as `web_shop` or `print_catalog`, holds its categories and enforces the hierarchy: every `Category` has one parent or is at the top level, a category cannot be moved into its own subtree, and the order of siblings is kept. A catalog can have several trees, because channels organize the same products differently. See [ADR 0016](../../docs/adr/0016-categories.md).

Products and product models are put in categories with `AddCategory`, which takes the tree and the category's code. A product can be in any number of categories, in any trees and at any level. A variant is also in its model's categories, which `EffectiveCategories` combines with its own. Categories do not affect validation or completeness. When a category is removed from its tree, remove it from its products and product models with `RemoveCategory`.

## Loading stored data

New domain objects are created with `Create`, which assigns a new ID. Objects loaded from storage are rebuilt with `Restore`, which takes the stored ID and state. For example, `Product.Restore`, `Family.Restore` and `AttributeDefinition.Restore`. `Restore` checks that the data is structurally sound but does not revalidate it against constraints that may have changed since it was saved. Use `Product.Validate` for that.

> **Status:** early development. The API is not stable yet.
