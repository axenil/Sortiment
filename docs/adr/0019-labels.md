# 0019. Labels

- **Status:** Accepted
- **Date:** 2026-11-25

## Context

Definition entities have immutable codes such as `t_shirts` or `linen_t_shirts` (ADR 0002), which imports, feeds and the API use to refer to them. People need something else: an admin UI shows *T-tröjor* to a Swedish editor and *T-shirts* to an English one, and a feed sends a marketplace the category path in the language of the channel. Nothing in the model holds that text yet. ADR 0016 left category labels out on purpose, so that all definition entities get labels the same way.

The model has to answer which entities have labels, what a valid label is, how labels relate to codes, and how labels of entities that belong to an aggregate, such as attribute options and categories, are changed.

## Decision

### Which entities have labels

- **Channels, attributes, attribute options, families, family variants, category trees, categories and asset families** have labels. These are the definition entities people see by name.
- **Products and product models** do not. Their name is an attribute value, such as `name`, because it can be localized, scoped and required like any other value.
- **Assets** do not. Their display text, such as alt text, comes with the attributes of asset families.
- **Built-in measurement families and units** do not yet. They are shared values that Core would have to ship translations for. They get labels with user-defined measurement families.
- **Locales** do not. `CultureInfo` already has display names for them.

### Labels

- A label is display text for one locale. An entity has at most one label per locale, and any number of locales, including none.
- A label can be in any valid `Locale`, not only the locales of a channel, because the languages of the people editing the catalog are not the languages it is published in.
- A label is valid if it is not empty or only whitespace, has at most 255 characters, and has no control characters or invisible formatting characters. Input is not trimmed or otherwise changed, as with codes. The errors are `label.empty`, `label.too_long` and `label.invalid_characters`, with the locale and the echoed input in their context.
- **Labels are not identifiers.** They do not have to be unique, and nothing refers to an entity by label. Codes remain the only natural key. Whether imports may match attribute options by label is decided with the import engine.

### Changing labels

- Every entity with labels has the same members: `Labels`, `GetLabel(locale)`, `SetLabel(locale, text)` and `RemoveLabel(locale)`.
- Entities that belong to an aggregate are labeled through it, so the aggregate stays in control of them: options through their attribute's `SelectSettings`, and categories through their `CategoryTree`. A category keeps its labels when it is moved.

### Reading labels

- `GetLabel` returns the label in exactly the requested locale, or nothing. Core has no fallback rules: whether `sv-FI` falls back to `sv-SE`, to English or to the code depends on the caller, and differs between an admin UI and a feed.

### Restoring

- The `Restore` factories of entities with labels take their labels (ADR 0011). Labels are not revalidated on restore, so a label that breaks a rule tightened after it was saved can still be loaded.

## Consequences

- Admin UIs, feeds and imports get one way to show and send the names of definitions, the same for every entity.
- Codes stay stable while labels change freely, because nothing refers to a label.
- Callers decide the fallback when a label is missing, at the cost of writing that rule themselves.
- Built-in measurement units have no labels until user-defined measurement families exist.
