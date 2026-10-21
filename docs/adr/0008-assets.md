# 0008. Assets

- **Status:** Accepted
- **Date:** 2026-10-21

## Context

Product images and documents are core product data. Channels need them, and teams expect to upload and manage them in the PIM rather than host them somewhere else and paste links. Basic asset handling is therefore part of Sortiment itself. Step 8 (DAM) is reserved for advanced capabilities on top of it: renditions, usage rights workflows, automatic linking rules, and connectors to external DAM systems.

The same file is often used by many products. Color variants share lifestyle images, and a manual is shared across a product line. Different kinds of files also need different rules and descriptive data: a packshot must be an image and needs alt text, while a manual must be a PDF. Files are large binary content, so they cannot live inside product values or in Sortiment.Core, which does no I/O.

Imports and integrations must be able to reference an existing asset without uploading the file again, and the reference must work in every environment.

## Decision

### Asset families

- An `AssetFamily` defines a kind of asset, for example `packshots`, `lifestyle_images` or `user_manuals`. It has an `AssetFamilyId` and an immutable `AssetFamilyCode` (ADR 0002).
- An asset family decides which files its assets accept:
  - allowed content types, either exact (`application/pdf`) or wildcards (`image/*`)
  - an optional maximum file size
- Asset families will get their own attributes, such as localized alt text, photographer or usage rights end date, built on the same `AttributeDefinition`, value and validation model as products. This is planned as the next increment after product families, so both families follow the same pattern. It is not part of the first version.

### Assets

- An `Asset` belongs to exactly one asset family. It has:
  - an `AssetId`
  - an immutable `AssetCode`, unique within its family, with the same format rules as other codes
  - its current file: original file name, content type, size in bytes, SHA-256 checksum, storage key, and width and height in pixels for images where they are known
- Imports, the API and people refer to an asset by family code and asset code, for example `packshots/ts001_front`. Internal references use `AssetId`.
- When no code is given at upload, one is derived from the file name: `TS001-Front.jpg` becomes `ts001_front`. A suffix is added when the code is already taken.
- An asset's file can be replaced, for example after a reshoot. The asset keeps its ID and code, so every product that uses it shows the new file. Stored files themselves never change: a replacement is stored under a new storage key, which keeps cached copies correct. Replaced files are cleaned up from storage.
- A file is accepted only if its content type and size are allowed by the asset's family. This is checked when an asset is created and when its file is replaced.

### Product attributes and values

- The `AssetCollection` attribute type holds assets from one asset family. The family is chosen when the attribute is created and cannot change.
- An `AssetCollectionValue` is an ordered, non-empty list of asset IDs without duplicates.
  - Order is meaningful: the first asset is the main one. Values are compared as sequences.
  - Asset collection attributes can be localizable and scopable like any other attribute, for example packaging images per language.
- `MaxCount` limits how many assets a value holds, for example 1 for a main image.

### Validation

- `AttributeDefinition.Validate` checks what it can see: the value is not empty, has no duplicates and respects `MaxCount`.
- Checking that each asset exists and belongs to the attribute's asset family needs the assets. This happens when products are validated against a resolved context of attributes and assets, which follows how ADR 0006 keeps cross-aggregate lookups out of the domain types.
- File types and sizes are enforced by the asset family when files are added, so product validation never has to inspect files.
- Uniqueness of asset codes within a family, like uniqueness of all codes, is enforced by the application service and storage, because a single aggregate cannot see its siblings.

### File storage

- Sortiment.Core defines `IFileStorage`, an async abstraction for saving, reading and deleting file content. Every method accepts a `CancellationToken`. Implementations generate the storage keys.
- Implementations are adapter packages:
  - `Sortiment.FileStorage.FileSystem` comes first and covers local development and small installations.
  - Cloud storage adapters follow later and need dependency approval.
- `Sortiment.Api` handles upload and download. How channels get public URLs for files (served by the API, signed URLs, or a CDN) is decided in step 3.
- Assets reference only files that Sortiment stores. External URLs are not supported.

## Consequences

- Assets can be reused across products and variants, referenced portably by code, and governed per family.
- A file system or cloud bucket holds the bytes, while the database holds assets, families and references.
- Deleting an asset that products still reference must be prevented by the application service that deletes it, because the asset cannot see product values.
- Codes make automatic linking by naming convention possible later, for example attaching `packshots/ts001_front` to the product with SKU `TS001`.
- Teams that already host files elsewhere must upload them to Sortiment. Imports can fetch a file from a URL and store it as an asset.
- The step 8 DAM features build on asset families and assets instead of replacing them.
