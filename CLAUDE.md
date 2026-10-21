# CLAUDE.md

Guidance for AI coding agents (and humans) working in this repository.

## What Sortiment is

Sortiment is a lightweight, open-source **PIM (Product Information Management)** system and product data toolkit for .NET.

**The problem it solves:** small and mid-sized companies need to manage product data (attributes, variants, translations, channel-specific content), but enterprise PIM systems are too expensive, and the open-source alternatives (Akeneo, Pimcore) are PHP-based. .NET teams end up rebuilding the same things on every project: a flexible product model, supplier file imports, and channel feeds.

**The approach:** Sortiment is built **inside-out as a family of independently useful NuGet packages**. Each layer must be valuable on its own, even if the full PIM is never finished:

| Step | Packages | Useful on its own as |
|---|---|---|
| 1 | `Sortiment.Core` | A flexible, typed product model: attributes, families, variants, localized and channel-scoped values, validation, completeness |
| 2 | `Sortiment.Import.*` | A declarative import/mapping engine (CSV, Excel, JSON → any target) with validation and dry-run diffs |
| 3 | `Sortiment.Storage.PostgreSql`, `Sortiment.Api` | A headless PIM backend |
| 4 | `Sortiment.Admin` | A complete PIM with a Blazor admin UI. Start with an MVP: manage families and attributes, browse and edit products, see import status. |
| 5 | `Sortiment.Feeds.*` | Validated channel feeds (Google Merchant, PriceRunner, …) |
| 6 | `Sortiment.Excel` | Excel round-trip for bulk editing large numbers of products. Builds on the Excel reader from step 2. |
| 7 | `Sortiment.Enrichment` | AI-assisted enrichment (descriptions, attribute extraction, translation) |
| 8 | Extras | Workflow, DAM, versioning, permissions |

**Status:** early development. Work on steps 1–2 first. Getting the product model in `Sortiment.Core` right is the top priority, because it is the most expensive thing to change later.

## Tech stack

| Area | Choice | Why |
|---|---|---|
| Runtime | **.NET 10 (LTS)**, C# latest | Long-term support, modern language features |
| Persistence | **EF Core + PostgreSQL** | Free, runs anywhere, and `jsonb` fits flexible attribute values well. Persistence is a pluggable layer, so other providers can be added without touching `Core`. |
| API | **ASP.NET Core Minimal APIs** + built-in OpenAPI | Lightweight, no extra framework needed |
| Admin UI (planned, step 4) | **Blazor**. The component library and render mode are decided later. | Keeps the stack in C#, and editing forms can be generated from the C# product model |
| Local dev | **Aspire** (`Sortiment.AppHost`, `Sortiment.ServiceDefaults`) | One-command local environment, including the database, plus a dashboard with logs and traces |
| Testing | **xUnit v3**, **Shouldly** (assertions), **NSubstitute** (only when a fake is clumsy), **Testcontainers** (integration), architecture tests | Standard and free. Tests read like documentation, and architecture tests enforce the layering rules. |
| CI | **GitHub Actions** | Lives next to the code. Build and test on every PR. |
| Build | Central Package Management (`Directory.Packages.props`), `Directory.Build.props`, nullable enabled, warnings as errors | Consistent settings across all packages |
| Versioning | **MinVer**: versions come from git tags (`v0.1.0`). All packages are released together with the same version. | No version numbers to maintain by hand, and consumers never have to match package versions |

## Repository layout

The structure is still being created. This is the target layout:

```
src/        Sortiment.* packages (one folder per package)
tests/      One test project per package: Sortiment.<Package>.Tests,
            plus Sortiment.ArchitectureTests for the dependency rules
samples/    Small runnable samples showing each package used standalone
docs/       Documentation; docs/adr/ holds Architecture Decision Records
```

## Setup and commands

Prerequisites: .NET 10 SDK, and Docker for integration tests and the local Aspire environment.

Run from the repo root:

```bash
dotnet build
dotnet test
dotnet format --verify-no-changes
```

## Architecture principles

1. **Dependencies point inward.** `Sortiment.Core` depends on nothing in this repo. Nothing may depend on `Api` or `Admin`. Architecture tests enforce this.
2. **The core is pure.** `Sortiment.Core` has no I/O, no EF Core, no ASP.NET Core and no NuGet package dependencies. It may use anything in the .NET base class library (for example `System.Text.Json`). Build-only tools that add no runtime dependency, such as MinVer, are allowed. Core does not log and does not use `IOptions`, so principles 14 and 15 apply to the packages around it. All I/O (database, files, HTTP, AI services) lives in adapter packages at the edges.
3. **Tools target abstractions, not the PIM.** Import writes to an `IProductTarget` and feeds read from an `IProductSource`. Both interfaces are defined in `Sortiment.Core`, so storage can implement them without depending on Import or Feeds. The Sortiment PIM is just one implementation. Every package must be usable without Storage, Api or Admin. Depending on `Sortiment.Core` is fine, since it has no dependencies of its own. Likewise, the admin UI talks to Sortiment **only through the public API**, like any external client, so everything the admin can do, an integration can do too.
4. **Rich domain model (DDD-light).** Domain types such as `Product`, `Family` and `AttributeDefinition` enforce their own invariants (for example, a value must match its attribute's type). They expose behavior through methods, not public setters. Business rules live in the domain, not in services or endpoints.
5. **Plain application services.** Use cases are explicit service classes (for example `ProductService`) called directly from Minimal API endpoints. No mediator, CQRS dispatcher or other hidden indirection.
6. **Strongly-typed IDs.** Every entity ID is its own `readonly record struct` (for example `ProductId`) wrapping a time-ordered `Guid` v7 (`Guid.CreateVersion7()`). IDs are generated in code, not by the database. Definition entities (attributes, attribute options, families, channels, categories, product models) also have an immutable, validated code (for example `AttributeCode`), which imports, feeds and the API use to refer to them. Products have a unique, changeable `Sku`. Internal references always use IDs. See [ADR 0002](docs/adr/0002-entity-identity-ids-and-codes.md).
7. **One responsibility per type and method.** Keep classes and methods small and focused. Prefer composition over inheritance.
8. **No global or static mutable state.** No service locator. Dependencies come in through the constructor.
9. **Async all the way for I/O,** and every async method accepts a `CancellationToken`.
10. **Expected failures are results, not exceptions.** Validation and import errors are returned through Sortiment's own small `Result` / `Result<T>` type in `Sortiment.Core`, with structured `ResultError`s (a stable code such as `attribute.option_not_found`, a message, and context such as row, column or attribute). See [ADR 0005](docs/adr/0005-result-and-error-model.md). Collect all errors rather than stopping at the first one. Exceptions are for programming errors and infrastructure failures. Every external call (database, files, HTTP, AI) has explicit error handling, and exceptions are never swallowed.
11. **Prefer immutability.** Use records for value objects, and avoid exposing mutable collections.
12. **The public API is a contract.** Every public type and member has XML docs. Keep things `internal` unless they are meant to be used by consumers.
13. **Tests are written alongside the implementation, not after.** Every behavior change comes with tests. Core logic is unit tested; adapters get integration tests.
14. **Configuration uses the options pattern** (`IOptions<T>`) and is passed in through DI, never read from static or global places. Secrets come from user secrets or environment variables.
15. **Logging uses `Microsoft.Extensions.Logging`** (`ILogger<T>`) only. Libraries log through the abstraction and never configure logging providers.
16. **Every package has a README and a runnable sample** in `samples/`.

## Code conventions

- **Formatting:** follow `.editorconfig` and keep `dotnet format` clean.
- **Naming:** `PascalCase` for types, methods, properties and constants. `camelCase` for locals and parameters, `_camelCase` for private fields. Interfaces start with `I`, and async methods end with `Async`. Don't name types after common BCL types (`Attribute`, `Type`, `Action`, `Task`, …); they clash with implicit usings for us and for consumers.
- **Files:** one public type per file, with the file named after the type. Use file-scoped namespaces matching the folder structure (`Sortiment.Core.Products`, …).
- **Style:** use `var` when the type is obvious, prefer `sealed` classes unless designed for inheritance, and enable nullable reference types everywhere.
- **Tests:** assert with Shouldly. Prefer hand-written fakes (for example an in-memory `IProductTarget`) over mocks, and use NSubstitute only when a fake would be clumsy. Assert outcomes, not which methods were called.
- **Comments:** XML docs on public APIs. Inside methods, comment only when the reason behind the code isn't obvious: a non-obvious *why*, a constraint or a trade-off. Never restate *what* the code does. If code needs a comment to explain what it does, split or rename it instead.
- **Comments must stand on their own.** A reader with no access to the conversation, issue or debugging session behind a change must understand them. Never reference the conversation ("as discussed", "per your request"). Never describe the change history ("changed from X to fix the failing test", "previously this used…", "now handles…"). Never narrate what went wrong during development. That context belongs in the commit message or PR description, not in the code.

## Workflow

- **Language:** all code, comments, documentation, commit messages and issues are written in **English**, even if instructions are given in Swedish.
- **Branches:** do not commit directly to `main` (the initial repository setup is the only exception). Work in feature branches (`feat/…`, `fix/…`, `docs/…`) and merge through pull requests.
- **Commits:** Conventional Commits (`feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`), small and focused.
- **Agent autonomy:** the agent may create feature branches and commit locally without asking. It must ask before pushing, opening a pull request or merging.
- **Merging:** pull requests are squash-merged. The PR title is a Conventional Commit (for example `feat(core): add product families`) and becomes the single commit on `main`.
- **Decisions:** significant design decisions get a short ADR in `docs/adr/`.
- **Local files:** the parent folder (`C:\Projects\Sortiment`) is for local, non-repo files such as notes and scratch data. Never commit them.

## What the agent must NOT do

- **Do not change the tech stack** (database, frameworks, test tools) without asking first.
- **Do not add NuGet packages or other external dependencies without asking.** When proposing one, state its license. Only MIT, Apache-2.0 or BSD licenses are allowed. No commercial or copyleft packages; this rules out, for example, FluentAssertions 8+, MediatR 13+ and AutoMapper 15+.
- **Do not add any dependencies to `Sortiment.Core`** beyond the .NET base class library (see principle 2).
- **Do not push, open pull requests or merge without explicit approval.** Never commit directly to `main`, and never force-push or rewrite its history.
- **Do not reference Aspire from library packages.** Aspire belongs only in `Sortiment.AppHost`, `Sortiment.ServiceDefaults` and the application hosts (`Api`, `Admin`).
- **Do not add a logging framework** (Serilog, NLog, …) or other cross-cutting infrastructure to library packages.
- **Do not create new configuration files** (`appsettings.*.json`, `.env`, …) without asking. Never commit real configuration values.
- **Do not disable, skip or delete tests, analyzers or warnings** to make a build pass. Fix the cause or ask.
- **Do not make breaking changes to the public API** of a released package without asking.
- **Do not commit secrets, connection strings or personal data.** Use user secrets or environment variables.
- **Do not edit an EF Core migration that has already been committed.** Add a new one instead. Before the first release, resetting migrations may be cleaner, but ask first.
- **Do not change CI workflows, repository settings or the license** without asking.
- **Do not write documentation or code comments in Swedish.**
- **Do not write unnecessary comments,** or comments that depend on context the reader doesn't have (conversations, debugging sessions, earlier versions of the code).
