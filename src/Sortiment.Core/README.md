# Sortiment.Core

A flexible, typed product model for .NET: attributes, families, localized and channel-scoped values, assets, validation and completeness.

`Sortiment.Core` has no dependencies and performs no I/O. It can be used on its own, without the rest of Sortiment.

## Loading stored data

New domain objects are created with `Create`, which assigns a new ID. Objects loaded from storage are rebuilt with `Restore`, which takes the stored ID and state. For example, `Product.Restore`, `Family.Restore` and `AttributeDefinition.Restore`. `Restore` checks that the data is structurally sound but does not revalidate it against constraints that may have changed since it was saved. Use `Product.Validate` for that.

> **Status:** early development. The API is not stable yet.
