# 0017. Stale family schemas

- **Status:** Accepted
- **Date:** 2026-11-25

## Context

A `FamilySchema` is a family together with the definitions of its attributes, resolved by the caller and reused for every product in a unit of work (ADR 0010). The schema copies the definitions when it is created, but reads the family's attributes and requirements from the family itself.

If an attribute is added to the same `Family` instance while a schema is still in use, the schema has no definition for it. Without a rule for this, the schema can only leave the attribute out, and every answer built on it is silently wrong:

- completeness leaves out the new requirement, so a product without a value for it is reported as complete
- setting a value for the new attribute fails with `product.attribute_not_in_family`, although the attribute is in the family
- validation and the list of attributes leave it out

Attributes removed from the family are not a problem: the schema reads the family, so they disappear from it at once.

## Decision

- **A schema that has fallen behind its family refuses to answer.** When the family has an attribute that the schema has no definition for, the schema's members throw `InvalidOperationException` and say to create a new schema. Product, product model and variant operations that take a schema check it first, so they throw too, whether or not they would have looked up the new attribute.
- **This is a programming error, not an expected failure** (ADR 0005). A schema belongs to one unit of work, and it only falls behind when the caller changes the family in that unit of work and keeps using the old schema. A loud error is better than a result that is wrong and looks right.
- **`FamilySchema.IsCurrent` tells callers whether the schema can still be used,** so a caller that changes families can create a new schema rather than catch the exception.
- **The check is cheap.** The family counts the attributes added to it, without storing the count. A schema compares that count with the count it was created with, and only looks at the family's attributes when the count has changed. A schema reused for many products in an import costs nothing extra.

Alternatives that were rejected:

- **Copying the whole family into the schema** would make the schema consistent, but still out of date: it would keep giving answers that leave the new attribute out.
- **Looking up missing definitions on demand** would mean domain types look things up, which ADR 0010 rules out.
- **Returning `Result` from the schema's queries** would burden every caller with a failure that only a bug can cause.

## Consequences

- A stale schema can no longer make a product look complete or valid when it is not.
- Callers that change a family and then work with its products must create a new schema after the change. Services already resolve a schema per unit of work, so this mostly affects tests and long-running in-memory work.
- Removing attributes keeps working with an existing schema, as before.
