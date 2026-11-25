# 0018. Effective values of variants

- **Status:** Accepted
- **Date:** 2026-11-25

## Context

A variant holds values for the variant attributes of its family variant and shares the rest with its product model (ADR 0015). Its effective values, its completeness and the values feeds will publish combine the two levels.

Which level an attribute belongs to can change after values are set. `FamilyVariant.AddVariantAttribute` moves an attribute from the model to the variants, and `RemoveVariantAttribute` moves it back. ADR 0015 leaves moving the stored values to the application service, so for a while, or for good if the move fails or is forgotten, values sit on the wrong level. `Validate` reports them, but a combination that simply takes whatever value it finds still uses them:

- After an attribute such as `gtin` becomes a variant attribute, every variant still sees the model's value. Variants count as complete without a value of their own, and every variant appears to have the same GTIN.
- After an attribute such as `name` becomes a model attribute, a variant's old value hides the model's value, and the combined values contain two values for the same attribute and scope.

## Decision

- **Each attribute is read from the level the current family variant gives it.** Variant attributes, including the axes, are read only from the variant. Model attributes are read only from the product model. Attributes no longer in the family are read from neither.
- **Values on the wrong level count nowhere.** `Product.EffectiveValues`, `Product.GetEffectiveValue` and variant completeness leave them out. `Product.Validate` and `ProductModel.Validate` still report them, and moving them remains the application service's job (ADR 0015).
- `EffectiveValues` and `GetEffectiveValue` take the `VariantContext`, because the level of an attribute comes from the family variant, not from the model alone.

## Consequences

- A variant cannot look complete, or share a value that should be its own, because of values that have not been moved yet.
- The effective values have at most one value per attribute and scope.
- While a change to a family variant is being migrated, the moved attribute counts as empty, so completeness drops until the values are moved. This errs on the side of "not ready yet" rather than "ready" with the wrong data.
- Reading effective values needs the variant schema as well as the model. Services already resolve it for every other variant operation.
