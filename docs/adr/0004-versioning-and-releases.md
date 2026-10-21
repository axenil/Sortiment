# 0004. Versioning and releases

- **Status:** Accepted
- **Date:** 2026-10-21

## Context

Sortiment ships several packages that depend on each other. Versions have to be set consistently, preferably without editing version numbers by hand, and consumers should be able to tell easily which package versions belong together.

We considered MinVer (Apache-2.0), which derives versions from git tags, and Nerdbank.GitVersioning (MIT), which derives them from a `version.json` file and the git height.

## Decision

- We use **MinVer**. A release is created by tagging a commit `vX.Y.Z` (for example `v0.1.0`). Untagged builds get a pre-release version calculated from the most recent tag, starting at `0.1.0-alpha.0`.
- All packages are versioned in **lockstep**: every release publishes all packages with the same version.
- Packages follow semantic versioning. Until `1.0.0`, minor versions may contain breaking changes.
- Package validation is enabled. After the first release, we set the released version as the validation baseline, so each build checks the packages against it and catches accidental breaking changes.

## Consequences

- No version numbers live in project files, and a release is a single git tag.
- CI must check out the full git history (`fetch-depth: 0`) for MinVer to work.
- A release of one package republishes all of them, even unchanged ones. This is acceptable while the packages evolve together. If they diverge, we can revisit independent versioning.
