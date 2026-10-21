# 0010. Families and products

- **Status:** Accepted
- **Date:** 2026-10-21

## Context

Attributes, values and scopes exist (ADRs 0006 and 0009), but nothing yet ties them to products. A PIM needs to know which attributes a kind of product has, which of them must be filled in before a product can go to a channel, and how complete each product is.

Several aggregates are involved: a product's values must match attribute definitions, the attributes it may have come from its family, and completeness depends on the channel's locales. Domain types cannot look these up themselves (ADR 0006), so the design must say how they meet.

Variants (a product model with variant products) build on top of families and products and get their own ADR.

## Decision

### Families

- A `Family` defines a kind of product, for example `t_shirts` or `televisions`. It has a `FamilyId` and an immutable `FamilyCode` (ADR 0002).
- A family lists the attributes its products have, by `AttributeId`, in display order. An attribute can be in any number of families.
- A family has **requirements per channel**: the attributes that must have a value before a product is complete for that channel. Only attributes in the family can be required. Removing an attribute from the family also removes its requirements.

### Resolved family schema

- A `FamilySchema` is a family together with the `AttributeDefinition` of each of its attributes. The caller resolves it, typically from storage, and creating it fails if an attribute is missing.
- Product operations that need attribute definitions take a `FamilySchema`. Domain types never look anything up, and one schema can be reused for many products, for example during an import.

### Products

- A `Product` has a `ProductId`, a `Sku`, a family, an enabled flag and values.
- **The SKU** is a first-class property (ADR 0002):
  - unique across the catalog, enforced by the application service and storage
  - changeable through `ChangeSku`
  - 1–100 characters, with no control characters and no leading or trailing whitespace
  - **case-insensitive but case-preserving:**
    - `ts-001` and `TS-001` are the same SKU for equality, uniqueness and lookups.
    - The casing it was entered with is kept and used in exports, so connected systems get back exactly what they sent.
    - An import that refers to an existing product with different casing does not change the stored casing. Changing only the casing takes an explicit `ChangeSku`.
- **Every product belongs to exactly one family.** There are no products without a family; a catalog with no real structure can use a single general family.
- **A value is stored per attribute and scope** (ADR 0006). Setting a value:
  - requires the attribute to be in the product's family (`product.attribute_not_in_family`)
  - validates the value against the attribute definition, including its scope
  - replaces any existing value for the same attribute and scope

  Removing a value is the only way to clear one, since empty values are invalid.
- **Changing a product's family removes values for attributes the new family does not have,** and returns them so the caller can report what was dropped. Values never silently stay behind for attributes the product no longer has.
- `Product.Validate(schema)` revalidates every value, including against constraints that were tightened after the value was set, and reports values for attributes no longer in the family.

### Completeness

- Completeness is calculated per channel and per locale active in that channel: the share of the channel's required attributes that have a value.
- A required attribute is filled when the product has a value in the matching scope: the locale if the attribute is localizable, and the channel if it is scopable.
- Completeness checks presence only. Whether values are valid is reported by validation, not completeness.
- Completeness is calculated on demand from the product, its family schema and the channel. Storing it for search and filtering is a storage concern.

## Consequences

- Each product's attributes are exactly those of its family, so the admin UI can build the product form from the family, and imports know which columns map to which attributes.
- Product operations need a resolved `FamilySchema`. Services load it once and reuse it across many products.
- Removing an attribute from a family leaves values on that family's products until they are cleaned up. `Product.Validate` reports them, and the application service that changes the family removes them.
- SKUs that differ only in casing cannot coexist. Integrations that change the casing of SKUs still find the right product, and two systems with different casing conventions cannot flip a product's stored SKU back and forth.
- Products cannot have one-off attributes outside their family. Akeneo allows this, but it makes forms, imports and completeness harder to reason about. It can be added later as an explicit feature if needed.
- Variant products will build on this model: a product model holds the common values, and variants hold the values of the variant axes.
