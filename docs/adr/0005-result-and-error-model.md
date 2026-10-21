# 0005. Result and error model

- **Status:** Accepted
- **Date:** 2026-10-21

## Context

CLAUDE.md (principle 10) states that expected failures, such as validation and import errors, are returned as results rather than thrown. All errors are collected instead of stopping at the first. Sortiment.Core cannot depend on a third-party result library, so it needs its own small type. That type appears in nearly every public signature, so its shape is hard to change later.

## Decision

- **`Result` and `Result<T>` are sealed classes.** A struct would have a `default` value that looks like a success with no errors list, which breaks the invariant.
- **A failed result carries one or more errors**, as `IReadOnlyList<ResultError>`. Creating a failure without errors throws.
- **`Result<T>.Value` throws `InvalidOperationException` on a failed result.** Reading it without checking `IsSuccess` is a programming error.
- **The factory methods live on the non-generic `Result`** (`Result.Success(value)`, `Result.Failure<T>(error)`), so the value type is inferred and generic types carry no static members (CA1000).
- **No implicit conversions**, and only a minimal set of combinators (`Map`). More are added only when real code needs them, to keep the API easy to read for contributors who don't use a functional style.
- **The error type is named `ResultError`.** `Error` is a reserved keyword in Visual Basic (CA1716).
- **Errors have three parts:**
  - A stable, machine-readable `Code`, written as `<area>.<problem>` in snake_case, for example `code.too_long` or `attribute.option_not_found`. Error codes are part of the public contract.
  - A human-readable `Message`, which may change between versions.
  - A `Context` dictionary of strings, such as attribute, option, value, and later row and column. Well-known keys are listed in `ErrorContextKeys`.
- **Value objects that parse input offer two factories.** `Create(string)` returns a `Result<T>` for input that may be invalid. `Parse(string)` throws `ArgumentException`, and is meant for values known to be valid, such as literals in code and tests.

## Consequences

- Callers always see every problem with their input at once, which matters for imports and forms.
- Error codes can be relied on for translation, API problem details and tests. Renaming one is a breaking change.
- Results allocate a small object. That cost is acceptable for domain operations. Hot paths can be optimized later without changing the public shape.
