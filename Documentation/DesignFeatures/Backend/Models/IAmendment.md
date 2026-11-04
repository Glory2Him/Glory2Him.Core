# Amender record
Parent: [Approval.md §APR8.6](../../../Design/Approval.md) — HR-5 and its record, §APR8.6.3
Level: model — `IAmendment` (`Glory2Him.Core/Models/Bases/`), new, and the six models that implement it (`Glory2Him.Core/Models/Foundations/`), with their storage configurations (`Glory2Him.Core/Brokers/Storages/Sql/StorageBroker.<Entity>.Configurations.cs`) and one migration. The Likes feature's work on the `Reaction` model is `Backend/Models/Reaction.md`.
Inherits: §APR8.6.3 rules 1, 2, 9 and 10, §APR9.7.1 rule 2, §ARC12.3.1 shared rules 1 and 2, §ARC17.7, §SEC14.7 posture A rule 6

The record HR-5 reads is one property on six models, declared by one interface (§APR8.6.3 rules 1 and 2). It reaches storage in two steps, because a column that exists before every write path derives it would store whatever a caller sends (§APR8.6.3 rule 10): the model first, ignored by storage, and the column once every foundation derives it. Groundwork has no operation and no test paths of its own; each section's proof is the build and CI's model checks, and §2's an integration suite.

## 1. The model, not yet stored (#996)

1. **`IAmendment`** declares one member, `List<string> AmenderUserIds`, never null and empty by default, beside `IAudit` and `IApproval` in `Models/Bases` (§APR8.6.3 rule 1).
2. **Six models implement it** — `ContentItem`, `Link`, `Tag`, `Reaction`, `Comment` and `BibleReference` — and no other (§APR8.6.3 rule 2).
3. **Storage ignores the property** in each of the six configurations, so no column exists, no migration is added and nothing a caller sends is stored. §2 replaces the ignore with the column.
4. **A `$filter` naming it is refused, before §2 and after** (§APR8.6.3 rule 9). The property is on the item, and so on the six items' `[EnableQuery]` routes as a collection of strings. §ARC17.7 admits no term over a collection-valued property, so every `$filter` naming it is refused with `400` when the query is validated, before EF is handed a query to translate, whether or not a column exists. Filtering by amender, if it is ever needed, is a narrow, server-side query of its own (owner ruling 2026-11-04, GHSA-3m43-v266-m6qf).
5. **A plain `$select` naming it answers a server error (`500`), before §2 and after.** §ARC17.7 accepts it (*`$select`, `$top`, `$skip` and `$count`*), and OData projects the list in a form EF does not compile, whether storage ignores the property or maps it as a JSON primitive collection (found outside the host, QA round 1 on design PR #1392). It reads nothing, writes nothing and depends on no row, so §ARC17.7's property holds. It is accepted rather than closed: no designed reader selects the record by name, since the React model reads the whole item (#1019–#1021), and closing it would mean refusing a `$select` of a collection-valued property, a change to §ARC17.7 that nothing designed needs.

## 2. The column and its migration (#1009)

1. **Each of the six tables gains `AmenderUserIds`**, `nvarchar(max) NOT NULL`, holding the set as a JSON array, with the column default `'[]'` (§APR8.6.3 rule 9). The configuration maps the property in place of §1's ignore and keeps the store default from overwriting a value the entity carries.
2. **One migration, one `ADD` per table**, so the script is one batch with nothing to `EXEC`. Existing rows take `[]`. No index.
3. **Mapping it changes nothing on the query surface.** A `$filter` naming it is still refused, as §1 item 4 says, because §ARC17.7 refuses it whatever storage holds. A `$select` item on it that carries a nested option is refused too (§ARC17.7, *What `$select` may hold*): the property is the first collection-valued structural property in reach, which is the case that rule exists for. A plain `$select` naming it still answers `500`, as §1 item 5 says.
4. **It waits for every derivation** — every task of `ContentItemService.Amenders.md`, `LinkService.md`, `TagService.md`, `ReactionService.Amenders.md`, `CommentService.md` and `BibleReferenceService.md` — on `main` (§APR8.6.3 rule 10).
