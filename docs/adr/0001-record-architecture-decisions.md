# 0001. Record architecture decisions

- **Status:** Accepted
- **Date:** 2026-10-21

## Context

Sortiment is built as a family of packages that are meant to stay stable for a long time. Several early decisions, especially in the product model, are expensive to change once the packages are adopted. Contributors, both humans and AI agents, need to know what was decided and why, without access to the discussions that led there.

## Decision

Significant design decisions are recorded as short Architecture Decision Records (ADRs) in `docs/adr/`, using [`template.md`](template.md).

- ADRs are numbered sequentially (`NNNN-short-title.md`) and never renumbered.
- An accepted ADR is not rewritten. When a decision changes, a new ADR supersedes it, and the old ADR's status links to the new one.
- An ADR is added in the same pull request as the change it describes, or before it.

## Consequences

- The reasoning behind the architecture is versioned next to the code.
- Writing an ADR adds a small amount of work to significant changes.
