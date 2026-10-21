# Sortiment

A lightweight, open-source **PIM (Product Information Management)** system and product data toolkit for .NET.

Sortiment is built inside-out as a family of independently useful NuGet packages: a typed product model, a declarative import engine, a headless PIM backend, an admin UI and channel feeds. Each package is useful on its own, even without the rest of Sortiment.

> **Status:** early development. Nothing is released yet, and the APIs will change.

## Packages

| Package | Purpose | Status |
|---|---|---|
| `Sortiment.Core` | Typed product model: attributes, families, variants, localized and channel-scoped values, validation, completeness | In progress |
| `Sortiment.Import.*` | Declarative import and mapping (CSV, Excel, JSON) with validation and dry-run diffs | Planned |
| `Sortiment.Storage.PostgreSql`, `Sortiment.Api` | Headless PIM backend | Planned |
| `Sortiment.Admin` | Blazor admin UI | Planned |
| `Sortiment.Feeds.*` | Validated channel feeds (Google Merchant, PriceRunner, …) | Planned |

## Building

Prerequisites: the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet build
dotnet test
dotnet format --verify-no-changes
```

## Documentation

Architecture decisions are recorded in [`docs/adr`](docs/adr).

## License

[MIT](LICENSE)
