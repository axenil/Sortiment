# 0016. Categories

- **Status:** Accepted
- **Date:** 2026-11-15

## Context

Product data is organized in categories: a web shop navigates by *Clothing → T-shirts → Linen*, a print catalog has its own chapters, and a marketplace has its own taxonomy. Families (ADR 0010) say what a product *is* and which attributes it has. Categories say where it *belongs* when it is presented. The two are independent: a linen t-shirt is in the `t_shirts` family, but can be in both *Summer* and *Linen* in the web shop.

ADR 0002 lists categories as a definition entity, with an ID and an immutable code, but they do not exist yet. The model has to answer:

- how categories are organized, and whether there is one hierarchy or several
- which rules keep a hierarchy sound, and which type enforces them
- how products, product models and variants are put into categories
- what happens to products when a category is moved or removed

## Decision

### Category trees

- Categories are organized in **trees**. A `CategoryTree` has a `CategoryTreeId`, an immutable `CategoryTreeCode`, and the categories in it, for example `web_shop` or `print_catalog`. A catalog can have several trees, because channels organize the same products differently.
- **A tree is one aggregate.** It holds all its categories, so it can enforce the rules that span them:
  - every category has exactly one parent within the tree, or is at the top level
  - a category cannot be moved into its own subtree, so there are no cycles (`category_tree.move_into_own_subtree`)
  - the order of each category's children is kept, so a web shop menu can follow it
- Catalogs with a few thousand categories per tree are common. A tree of that size is small enough to load and change as a whole. Larger taxonomies are not a goal for now.

### Categories

- A `Category` is an entity within its tree. It has a `CategoryId` and an immutable `CategoryCode` (ADR 0002), for example `linen_t_shirts`.
- Category codes are unique across all trees, like other definition codes, so imports and feeds can refer to a category by code alone. The tree enforces uniqueness among its own categories. Uniqueness across trees is enforced by the application service and storage.
- Categories can be added under any category or at the top level, moved within their tree, and reordered among their siblings. A category cannot be moved to another tree.
- **Removing a category that has subcategories is refused** (`category_tree.category_has_children`). Its subcategories must be moved or removed first, so a single call never removes a whole branch by accident.
- Localized labels, such as *T-shirts* and *T-tröjor*, are not part of this decision. Families, attributes and options have no labels yet either, and all definition entities should get labels the same way.

### Classifying products

- Products and product models have a set of **categories**, referred to by `CategoryId`. A product can be in any number of categories, in any number of trees, and in categories at any level, not only the lowest one.
- Putting a product in a category takes the category's tree, so the category is resolved by code and known to exist (`product.category_not_found`). Removing a product from a category needs only the ID.
- **A variant's categories are its own categories plus its model's**, in the same way that a variant sees its model's values (ADR 0015). Shared classification, such as *Linen*, is set once on the model. Variant-specific classification, such as *Red*, can be set on the variant.
- Categories do not affect validation or completeness. A product without categories is still valid and can still be complete.

### Changing a tree after products use it

- Moving a category keeps its ID, so products stay in it and simply appear in its new place in the tree.
- Products and product models cannot see the trees. When a category is removed, the application service removes it from the products and product models that use it, as it does for values when an attribute is removed from a family (ADR 0010).

### Restoring

- `CategoryTree` gets a `Restore` factory, and the `Restore` factories of `Product` and `ProductModel` take their categories (ADR 0011). The tree checks its structural invariants on restore: one parent per category, no cycles, and unique codes within the tree.

## Consequences

- Classification is independent of families, so the same products can be organized in different ways for different channels.
- The tree enforces its own structure in the domain, at the cost of loading a whole tree to change it.
- Imports and feeds can refer to categories by code. Feeds can later select the products of a channel by tree, or map categories to a marketplace taxonomy such as Google's. How channels and trees are linked is decided with the feeds (step 5).
- Variants inherit their model's categories, so shared classification is entered once, like shared values.
- Removing categories depends on the application service to clean up products, as removing attributes from families already does.
- Categories have no labels until labels are designed for all definition entities.
