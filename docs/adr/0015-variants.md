# 0015. Variants

- **Status:** Accepted
- **Date:** 2026-11-02

## Context

Many products come in variants: a t-shirt in several colors and sizes, a drink in several volumes. The variants share most of their data, such as the name, description, material and images, and differ only in a few attributes. Without variants, each variant is a separate product, and shared data has to be entered and kept in sync many times over.

Families and products exist (ADR 0010), and ADR 0010 left variants to their own decision. The model has to answer:

- which attributes make the variants different, and which are shared
- where each value is stored, and what a variant sees
- how variants are kept distinct from each other
- how validation and completeness work when a variant's data is spread over two places
- what happens when families, attributes or the variant structure change after data exists

Channels and feeds work with sellable items, so whatever we choose, each variant must still be a product with its own SKU.

## Decision

### Family variants

- A `FamilyVariant` describes one way a family's products vary, for example `t_shirts_by_color_and_size`. It belongs to one family and has a `FamilyVariantId` and an immutable `FamilyVariantCode` (ADR 0002). A family can have several, because not every product in it varies the same way: some t-shirts come in sizes only.
- It has **one to five variant axes**: the attributes whose values tell the variants apart, for example `color` and `size`.
- It lists the **variant attributes**: the family attributes that are set per variant, for example `ean` and `weight`. The axes are always variant attributes. Every other attribute of the family is a **model attribute** and is shared by all variants.
- Axes must be attributes of the family, of type single-select or boolean, and neither localizable nor scopable, because a variant has exactly one value per axis. Other attribute types can be allowed as axes later.
- The axes cannot change after the family variant is created, because existing variants are defined by them. The other variant attributes can change; see *Changing the structure*.
- There is one level of variants: product model → variant products. A second level (model → color → size), as some PIMs have, would add a sub-model between them and get its own decision. Data stored with one level stays valid if that is added.

### Product models

- A `ProductModel` holds the data that its variants share. It has a `ProductModelId`, an immutable `ProductModelCode`, a family variant, and values.
- It has values only for model attributes. Setting a variant attribute on a model fails with `product_model.attribute_not_on_model`.
- A product model has no SKU and no enabled flag. It is not sold or sent to channels itself; its variants are.
- Its family is the family of its family variant, and its family variant cannot change after creation.

### Variant products

- A variant is an ordinary `Product` with a parent: it has a `ProductModelId` in addition to its SKU, family, enabled flag and values. Everything that works on products, such as imports, feeds, SKU lookups and completeness, works on variants too.
- A variant's family is its model's family. `ChangeFamily` is not allowed on a variant.
- A variant has values only for variant attributes. Setting a model attribute on a variant fails with `product.attribute_on_model`.
- A variant must have a value for every axis. A variant without all axis values is invalid (`product.axis_value_missing`).
- **A variant's effective values** are its own values plus its model's values. Variants cannot override a model value. Where a value differs per variant, the attribute should be a variant attribute.
- Products without a model are unaffected and work as before.

### Distinct variants

- Every variant of a model must have a different combination of axis values. Core represents the combination as a value, `AxisCombination`, so it can be compared and indexed.
- Like SKU uniqueness, this is enforced by the application service and storage, because a variant cannot see its siblings.

### Validation and completeness

- Operations on a variant take a resolved view of its family variant and its model, in the same way products take a `FamilySchema` (ADR 0010). Domain types still look nothing up.
- Validation of a variant checks its own values and its axis values. Validation of a model checks the model's values.
- Completeness is calculated for variants, on their effective values. A required model attribute counts as filled for every variant once the model has a value.
- Product models have no completeness of their own. The admin can show how many of a model's variants are complete.

### Changing the structure

- Removing an attribute from a family that is an axis of one of its family variants is refused by the application service, because it would make the variants indistinguishable.
- Moving a non-axis attribute between model and variant attributes is allowed. Existing values then sit at the wrong level until the application service moves them: from the model to every variant, or from the variants to the model when they all agree. `Validate` reports values at the wrong level, as it does for values of attributes removed from the family (ADR 0010).
- Detaching a variant into a standalone product, attaching a product to a model, and moving a variant to another model are not part of this decision. They can be added later as explicit operations.

### Restoring

- `FamilyVariant`, `ProductModel` and variant products get `Restore` factories (ADR 0011). As with other aggregates, they check structural invariants only.

## Consequences

- Shared data is entered once, on the model, and every variant sees it. Changing the model changes all its variants.
- Channels, feeds and imports keep working with products and SKUs, so variants need little special handling beyond reading effective values.
- The family variant makes the structure explicit and the same for every model that uses it, so the admin UI can build model and variant forms, and imports know which columns belong where.
- Allowing only single-select and boolean axes keeps axis values exact and comparable. Products that vary by a measured quantity, such as volume, model it as a select option until measurement axes are added.
- Not allowing overrides keeps every value in exactly one place, at the cost of flexibility: a variant that needs a different description than its siblings needs the description to be a variant attribute.
- Uniqueness of axis combinations depends on the application service and storage, as SKU uniqueness already does.
- Variant operations need more context than product operations: a family schema, the family variant and the model. Services load these once and reuse them, as they do with family schemas.
