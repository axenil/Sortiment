# 0012. Identifiers in error context

- **Status:** Accepted
- **Date:** 2026-11-02

## Context

ADR 0005 gives every `ResultError` a context of well-known keys, such as `attribute` and `channel`, so that imports, the API and the admin can show where an error occurred.

The keys were documented as holding codes, but some errors put an ID under them instead. This happens where the domain only has the ID at hand:
- an attribute removed from a family by ID
- a value whose attribute is no longer in the product's family
- a missing attribute definition
- a value scope, which refers to its channel by ID

A caller cannot tell from the key whether it got a code or an ID, so it cannot reliably show the error or link it to the right entity.

## Decision

- The keys `attribute` and `channel` always hold **codes**.
- The keys `attribute_id` and `channel_id` hold **IDs**. They are used when the code is not known where the error is created.
- An error carries the ID key instead of the code key, not both under the same key. A caller that needs the code resolves the ID itself.
- Error factories take typed identifiers (`AttributeCode` or `AttributeId`) instead of strings, so which key is used follows from the type.
- The same rule applies to keys added later for other entities: the plain key holds the code, and `<entity>_id` holds the ID.

ADR 0005 is amended by this decision.

## Consequences

- Callers can rely on the key to know what kind of value they are reading, without parsing it.
- Errors about values report the channel as `channel_id`, so a caller that wants to show the channel's code resolves it from the ID.
- Error contexts written before this change used `attribute` and `channel` for IDs as well. Sortiment has not been released, so no stored errors or integrations depend on the old form.
