# Amender record
Parent: [Approval.md §APR8.6](../../../Design/Approval.md) — HR-5 and its record, §APR8.6.3
Level: model — `IAmendment` (`Glory2Him.Core/Models/Bases/`), new, and the six models that implement it (`Glory2Him.Core/Models/Foundations/`), with their storage configurations (`Glory2Him.Core/Brokers/Storages/Sql/StorageBroker.<Entity>.Configurations.cs`) and one migration. The Likes feature's work on the `Reaction` model is `Backend/Models/Reaction.md`.
Inherits: §APR8.6.3 rules 1, 2, 9 and 10, §APR9.7.1 rule 2, §ARC12.3.1 shared rules 1 and 2, §SEC14.7 posture A rule 6

The record HR-5 reads is one property on six models, declared by one interface (§APR8.6.3 rules 1 and 2). It reaches storage in two steps, because a column that exists before every write path derives it would store whatever a caller sends (§APR8.6.3 rule 10): the model first, ignored by storage, and the column once every foundation derives it. Groundwork has no operation and no test paths of its own; each section's proof is the build and CI's model checks, and §2's an integration suite.

## 1. The model, not yet stored (#996)

1. **`IAmendment`** declares one member, `List<string> AmenderUserIds`, never null and empty by default, beside `IAudit` and `IApproval` in `Models/Bases` (§APR8.6.3 rule 1).
2. **Six models implement it** — `ContentItem`, `Link`, `Tag`, `Reaction`, `Comment` and `BibleReference` — and no other (§APR8.6.3 rule 2).
3. **Storage ignores the property** in each of the six configurations, so no column exists, no migration is added and nothing a caller sends is stored. §2 replaces the ignore with the column.

## 2. The column and its migration (#1009)

1. **Each of the six tables gains `AmenderUserIds`**, `nvarchar(max) NOT NULL`, holding the set as a JSON array, with the column default `'[]'` (§APR8.6.3 rule 9). The configuration maps the property in place of §1's ignore and keeps the store default from overwriting a value the entity carries.
2. **One migration, one `ADD` per table**, so the script is one batch with nothing to `EXEC`. Existing rows take `[]`. No index.
3. **It waits for every derivation** — every task of `ContentItemService.Amenders.md`, `LinkService.md`, `TagService.md`, `ReactionService.Amenders.md`, `CommentService.md` and `BibleReferenceService.md` — on `main` (§APR8.6.3 rule 10).
