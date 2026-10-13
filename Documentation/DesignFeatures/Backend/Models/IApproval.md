# Approval interface — every approvable item carries a confidence score
Parent: [BereanReview.md](../../BereanReview.md)
Level: model — `IApproval` and `IConfidence` (`Glory2Him.Core/Models/Bases/`), the eight models that implement `IApproval` (`Glory2Him.Core/Models/Foundations/`), their storage configurations (`Glory2Him.Core/Brokers/Storages/Sql/StorageBroker.<Entity>.Configurations.cs`) and one migration. The Likes feature's work on the `Association` model is `Backend/Models/Association.md`.
Inherits: §APR9.7.1 rules 2 and 5, §APR13.4 rule 5, §ARC12.2.1, BereanReview.md rule 2, *Storage and migration* items 1 and 5, *What ships together* step 3

`IApproval` extends `IConfidence`, as the owner ruled on 2026-10-13: *"It is recorded against the entity that implements IApproval since IApproval implements IConfidence … Association must not implement IConfidence again since it is getting it via IApproval"* (BereanReview.md, A4). It reaches storage in two steps, because a column that exists before every add and modify refuses the fields would store whatever a caller sends: the model first, ignored by storage, and the columns once the six foundations refuse them. Groundwork has no operation and no test paths of its own. Each section's proof is the build, CI's model checks, and §2's integration suite.

## 1. The model, not yet stored (needs issue)

1. **`IApproval` is declared `public interface IApproval : IConfidence`**, its own members unchanged.
2. **`Association` drops `IConfidence` from its own list**, and keeps its four properties, which now implement the interface through `IApproval`. Its storage is unchanged.
3. **`ContentItem`, `Link`, `Tag`, `Reaction`, `Comment`, `BibleReference` and `Attachment` gain the four properties**: `decimal? ConfidenceScore`, `string? ConfidenceReason`, `Guid? SourceBatchId`, `string? ModelVersion`, each null by default.
4. **Storage ignores the four properties** in each of the seven configurations, so no column exists, no migration is added and nothing a caller sends is stored. §2 replaces each ignore with the column.
5. **`IConfidence`'s summary is rewritten** (BereanReview.md rule 1): only the system writes the four fields, a score always carries its provenance, and a null score means none has been recorded, or one was cleared because the content changed. It no longer says that a null `SourceBatchId` means *"a human set it by hand"*, or that *"only an explicit zero"* blocks.
6. **Until §2, a `$filter` naming one of the seven items' new properties fails** on a route that hands OData a query EF has yet to run: OData binds it, and EF has no column to translate it against. The request answers a server error. It reads nothing, writes nothing and discloses nothing, and §2 closes it, as `Backend/Models/IAmendment.md §1` item 4 accepts the same for its own property.

## 2. The columns and their migration (needs issue)

1. **`ContentItems`, `Links`, `Tags`, `Reactions`, `Comments`, `BibleReferences` and `Attachments` each gain** `ConfidenceScore decimal(4,2) NULL`, `ConfidenceReason nvarchar(500) NULL`, `SourceBatchId uniqueidentifier NULL` and `ModelVersion nvarchar(128) NULL`, mapped as `Associations` maps them (`StorageBroker.Association.Configurations.cs`), and a check constraint `CK_<Entity>_ConfidenceScoreRange` holding `ConfidenceScore IS NULL OR ConfidenceScore BETWEEN 0 AND 10`, as `CK_Association_ConfidenceScoreRange` does.
2. **One migration**: for each table, four nullable `ADD`s with no default and one `ADD CONSTRAINT`. Existing rows read null and nothing is updated, so the script is one batch with nothing to `EXEC`. No index — no reader filters on them.
3. **It waits for both refusals on all six foundations** — `Backend/Foundations/<Entity>Service.BereanReview.md §1` and `§2` for `ContentItem`, `Link`, `Tag`, `Reaction`, `Comment` and `BibleReference` — on `main`. `Attachment` has no foundation service and no write path that could carry them.
4. **Against §APR7.4 item 6's stage 2**, which adds `ApprovalId` to `IApproval` and to five of the same tables in one pull request (D1): whichever reaches `main` second generates its migration after the other's. Neither needs the other.
5. **Not yet scheduled**: it waits for the confidence process's design, which carves it with the six foundations' refusals (BereanReview.md, *What ships together* step 5). §1 is carved now, because the write-back names each item's model.
6. `Down` drops the columns and the constraints, for the developer's rollback alone. In effect the change is not reversible once a score is written, because dropping the columns loses it (BereanReview.md, *Risks*).
