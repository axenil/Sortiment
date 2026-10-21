# 0002. Entity identity: IDs and codes

- **Status:** Accepted
- **Date:** 2026-10-21

## Context

Everything in a PIM refers to other things: a product belongs to a family, a value belongs to an attribute, and an import mapping says which column goes to which attribute. We need to decide what value is used to refer to an entity.

Strongly-typed GUID IDs (CLAUDE.md, principle 6) are good identities, but people and external systems cannot work with them. Import mappings, feed definitions, spreadsheets and API URLs need short, readable keys such as `color`, `t_shirts` or `TS-001-RED-M`. A mapping written in one environment must also work in another, where the GUIDs differ.

We considered three options:

- **GUIDs only.** Readable names would have to be looked up ad hoc everywhere, and configuration could not move between environments.
- **Codes only.** Codes are the identity, as in Akeneo for attributes, families and other definitions. This is simple, but codes can never be renamed, and stored data is locked to them.
- **Both.** A GUID is the identity, and a readable code or SKU is the key used at the boundaries.

Akeneo started with codes as the identity everywhere, including the SKU for products. It later added UUIDs for products so that the identifier could change, which required new API endpoints and a data migration after adoption.

## Decision

We use both IDs and codes.

- Every entity has a strongly-typed ID (`readonly record struct` wrapping a `Guid` v7), generated in code. IDs are the identity, and all internal references use IDs. This includes references between aggregates and stored product values.
- Definition entities have an immutable, validated code as their natural key: attributes, attribute options, families, channels, categories and product models. Each has its own code type, for example `AttributeCode`. Codes are unique per entity type, use only lowercase `a-z`, `0-9` and `_`, and are 1–100 characters long. Imports, feeds, the API and people refer to definitions by code.
- Products have a required `Sku` that is unique across the catalog. It is a first-class property of `Product`, not an ordinary attribute. A SKU can be changed through an explicit operation, because `ProductId` is the stable identity. SKUs use looser rules than codes, since they come from other systems. Other identifiers such as GTIN are ordinary `Identifier` attributes, because not every product has one.
- Locales use standard BCP 47 tags (for example `sv-SE`) and are not entities.
- Public contracts (API, mapping and feed definitions) are code-first. IDs may appear in responses, but they are secondary keys.
- Raw `Guid` and `string` values are never used for IDs or codes. The strongly-typed types are used everywhere.

## Consequences

- Configuration and data exchanged with the outside world are readable and portable between environments.
- SKUs can change without breaking references, which happens in practice (ERP migrations, renumbering).
- Renaming a definition code is not supported in v1. Because internal references use IDs, a rename operation can be added later without migrating stored data.
- Every boundary (import, feeds, API) resolves codes to IDs. This is cheap, since there are few definitions and they can be cached in resolved schemas.
- Stored product values are keyed by ID, so raw `jsonb` is less readable than if it were keyed by code.
- Revisit this decision before the first public release. Until then, dropping the IDs for definitions (codes only) is a mostly mechanical refactor. After release, either change breaks the public API.
