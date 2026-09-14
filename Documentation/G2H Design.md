# G2H Design

## 1. Design Overview

### 1.1 Purpose

Glory 2 Him (G2H) is a content management system designed to allow users to contribute, organise, review, approve, publish, associate, and consume gospel-focused content.

The system is centred around `ContentItem`, which represents primary user-contributed content. Examples of content types include:

1. `Quote`
2. `Story`
3. `Testimony`
4. `Topic`
5. Future content types

All user-contributed and configurable content is subject to an approval process before it is considered trusted, visible, or publishable.

### 1.2 Core Design Principles

The design follows these principles:

1. Content must be versioned.
2. Content must be approvable.
3. Approval must be reusable across multiple entity types.
4. Approval must not be tightly coupled to each entity through direct database relationships.
5. Content associations must support both a specific content version and all versions of a content item group.
6. Content-specific behaviour must be policy-driven through settings.
7. `Topic` must be modelled as a `ContentType`, not as a separate database entity.
8. A `Topic` groups other content items through `Association`.
9. The feed is a domain projection only, not a database entity.
10. Any publishable content type except `Topic` can appear in the feed.
11. All deletes are soft deletes.
12. Soft-deleted content must be excluded from public visibility.

### 1.3 Source Inputs

This design is based on:

1. The `Glory 2 Him.drawio` design file.
2. The current C# entity model files.
3. The current EF Core model snapshot.
4. The supplied design direction for approval, settings, feed, topic, versioning, visibility, and soft delete behaviour.

### 1.4 Current Model Completion Status

The current source files are not complete. This document separates the design into:

1. Current implemented model.
2. Diagram-driven intended model.
3. Required model extensions.
4. Recommended design rules.
5. Final agreed direction where this supersedes earlier diagram wording.

### 1.5 How This Document Is Laid Out

**This document is the index.** Design is being split into area-scoped files
under `Documentation/Design/`, each owning one area and numbering its sections
with a prefix of its own. Five areas have moved so far: event design to
[`Documentation/Design/Events.md`](Design/Events.md) (§10 below is the pointer),
UI design to [`Documentation/Design/UI.md`](Design/UI.md) (§20), security
design to [`Documentation/Design/Security.md`](Design/Security.md) (§14 and
§18), architecture design to
[`Documentation/Design/Architecture.md`](Design/Architecture.md) (§12, §16 and
§17), and approval design to
[`Documentation/Design/Approval.md`](Design/Approval.md) (§7, §8, §9 and §13).
Everything else is still written here, in the section that already owns the
subject, and is still cited as `§N.M` until its own file exists.

The map immediately below says where every section lives, what to cite it as,
and — for an area not yet extracted — where it is going. The rulings behind the
split, its filenames, prefixes, citation form and completeness gates, are in
[`Documentation/Design/Split.md`](Design/Split.md), issue #481. `Split.md`
retires once the split has landed.

## Where Each Section Lives

A stale deep link into this file lands here, at the top, rather than at a
heading that has moved. This map is the one hop back: find the old number, and
it names the file the section is in now and the number to cite it by.

An area with no link is **not extracted yet** — its section is still in this
document under its existing number, and the prefixed form is what it will be
cited as once its file lands. A forward citation to one of those is expected to
dangle until then; this map is what resolves it.

| Old | Title | Lives in | Cite it as |
| --- | --- | --- | --- |
| §1 | Design Overview | This document — front matter for the whole design, not an area | `§1` |
| §2 | Domain Model Overview | `Design/Domain.md` — not extracted yet, still in this document | `§DOM2` |
| §3 | Content Design | `Design/Domain.md` — not extracted yet, still in this document | `§DOM3` |
| §4 | Association Design | `Design/Domain.md` — not extracted yet, still in this document | `§DOM4` |
| §5 | Supporting Content Entities | `Design/Domain.md` — not extracted yet, still in this document | `§DOM5` |
| §6 | ContentItemSetting Design | `Design/Domain.md` — not extracted yet, still in this document | `§DOM6` |
| §7 | Approval Design | [`Design/Approval.md`](Design/Approval.md) — extracted; §7 below is the pointer stub | `§APR7` |
| §8 | Approval Settings Design | [`Design/Approval.md`](Design/Approval.md) — extracted; §8 below is the pointer stub | `§APR8` |
| §9 | Approval Lifecycle | [`Design/Approval.md`](Design/Approval.md) — extracted; §9 below is the pointer stub | `§APR9` |
| §10 | Event Design | [`Design/Events.md`](Design/Events.md) — extracted; §10 below is the pointer stub | `§EVNn` — renumbered rather than prefixed, so look the old number up in that file's *(formerly §10.X)* annotations rather than deriving it |
| §11 | Topic and Feed Design | `Design/Domain.md` — not extracted yet, still in this document | `§DOM11` |
| §12 | Component Architecture | [`Design/Architecture.md`](Design/Architecture.md) — extracted; §12 below is the pointer stub | `§ARC12` |
| §13 | AI Content Analysis | [`Design/Approval.md`](Design/Approval.md) — extracted; §13 below is the pointer stub | `§APR13` |
| §14 | Visibility Rules | [`Design/Security.md`](Design/Security.md) — extracted; §14 below is the pointer stub | `§SEC14` |
| §15 | Recommended Corrections | This document — to be retired item by item, not relocated (`Split.md` §S4.1) | — |
| §16 | Recommended Service Responsibilities | [`Design/Architecture.md`](Design/Architecture.md) — extracted; §16 below is the pointer stub | `§ARC16` |
| §17 | Recommended API Design | [`Design/Architecture.md`](Design/Architecture.md) — extracted; §17 below is the pointer stub | `§ARC17` |
| §18 | Authentication and Authorisation | [`Design/Security.md`](Design/Security.md) — extracted; §18 below is the pointer stub | `§SEC18` |
| §19 | Search Engine Optimisation | `Design/Domain.md` — not extracted yet, still in this document | `§DOM19` |
| §20 | UI / UX Design | [`Design/UI.md`](Design/UI.md) — extracted; §20 below is the pointer stub | `§UI20` |
| §21 | Summary | This document — §21.1 to be retired, §21.2 to be kept here as a roadmap (`Split.md` §S4.2, §S4.3) | — |

The six area files, one line each:

- `Design/Domain.md` — `DOM` — the entity model: content, associations, supporting entities, settings, topic and feed, SEO. *(planned)*
- [`Design/Approval.md`](Design/Approval.md) — `APR` — the approval entity, its settings, its lifecycle, and AI content analysis. **Exists.**
- [`Design/Architecture.md`](Design/Architecture.md) — `ARC` — the layer model, per-service responsibilities, and the API surface. **Exists.**
- [`Design/Security.md`](Design/Security.md) — `SEC` — visibility, enforcement posture, authentication and authorisation. **Exists.**
- [`Design/UI.md`](Design/UI.md) — `UI` — the React application's pages, components, navigation and authentication. **Exists.**
- [`Design/Events.md`](Design/Events.md) — `EVN` — event naming, addressing, the envelope, and the substrate. **Exists.**

## 2. Domain Model Overview

Moved to [`Documentation/Design/Domain.md`](Design/Domain.md). Sections
there carry a `DOM` prefix and otherwise keep the numbers they had here, so
`§2.1` is now `§DOM2.1`. Each relocated section also keeps a
`(formerly §2.X)` annotation naming its old position, so a `§2.X`
citation in code still resolves by grep even though the citable number
itself is now prefixed.

## 3. Content Design

Moved to [`Documentation/Design/Domain.md`](Design/Domain.md). Sections
there carry a `DOM` prefix and otherwise keep the numbers they had here, so
`§3.4` is now `§DOM3.4`. Each relocated section also keeps a
`(formerly §3.X)` annotation naming its old position, so a `§3.X`
citation in code still resolves by grep even though the citable number
itself is now prefixed.

## 4. Association Design

Moved to [`Documentation/Design/Domain.md`](Design/Domain.md). Sections
there carry a `DOM` prefix and otherwise keep the numbers they had here, so
`§4.9` is now `§DOM4.9`. Each relocated section also keeps a
`(formerly §4.X)` annotation naming its old position, so a `§4.X`
citation in code still resolves by grep even though the citable number
itself is now prefixed.

## 5. Supporting Content Entities

Moved to [`Documentation/Design/Domain.md`](Design/Domain.md). Sections
there carry a `DOM` prefix and otherwise keep the numbers they had here, so
`§5.6.2` is now `§DOM5.6.2`. Each relocated section also keeps a
`(formerly §5.X)` annotation naming its old position, so a `§5.X`
citation in code still resolves by grep even though the citable number
itself is now prefixed.

## 6. ContentItemSetting Design

Moved to [`Documentation/Design/Domain.md`](Design/Domain.md). Sections
there carry a `DOM` prefix and otherwise keep the numbers they had here, so
`§6.10` is now `§DOM6.10`. Each relocated section also keeps a
`(formerly §6.X)` annotation naming its old position, so a `§6.X`
citation in code still resolves by grep even though the citable number
itself is now prefixed.

## 7. Approval Design

Moved to [`Documentation/Design/Approval.md`](Design/Approval.md). Sections
there carry an `APR` prefix and otherwise keep the numbers they had here, so
`§7.5.1` is now `§APR7.5.1`. Each relocated section also keeps a
`(formerly §7.X)` annotation naming its old position, so a `§7.X` citation
in code still resolves by grep even though the citable number itself is now
prefixed.

## 8. Approval Settings Design

Moved to [`Documentation/Design/Approval.md`](Design/Approval.md). Sections
there carry an `APR` prefix and otherwise keep the numbers they had here, so
`§8.6.1` is now `§APR8.6.1`. Each relocated section also keeps a
`(formerly §8.X)` annotation naming its old position, so a `§8.X` citation
in code still resolves by grep even though the citable number itself is now
prefixed.

## 9. Approval Lifecycle

Moved to [`Documentation/Design/Approval.md`](Design/Approval.md). Sections
there carry an `APR` prefix and otherwise keep the numbers they had here, so
`§9.7.1` is now `§APR9.7.1`. Each relocated section also keeps a
`(formerly §9.X)` annotation naming its old position, so a `§9.X` citation
in code still resolves by grep even though the citable number itself is now
prefixed.

## 10. Event Design

Moved to [`Documentation/Design/Events.md`](Design/Events.md) — unifies this section with the
former standalone `EventSubstrate.md`, removing the duplication between them.
Sections there carry an `EVN` prefix (`§EVN1`, `§EVN2`, ...) rather than
restarting bare at 1, so a citation stays unambiguous once other
`Documentation/Design/*.md` files exist with their own prefixes. Each relocated
section keeps a `(formerly §10.X)` annotation naming its old position, so a
`§10.X` citation in code still resolves by grep even though the citable number
itself is now `§EVNx`, not `§10.X` verbatim. Lettered citations are anchored
separately: `§10.17(a)` and `§10.17(b)` appear in service and test comments and
the heading annotation carries no letter, so `§EVN18` lists those forms
explicitly.

Resolving is not the same as being right. The annotation maps an old number to
a new one and asserts nothing about whether the section was the correct one to
cite in the first place. A `§10.2` in a comment about how a value is
*persisted* lands on event naming and addressing because that is what §10.2
always was, not because the move sent it there; enum string persistence is
§3.7.

## 11. Topic and Feed Design

Moved to [`Documentation/Design/Domain.md`](Design/Domain.md). Sections
there carry a `DOM` prefix and otherwise keep the numbers they had here, so
`§11.7` is now `§DOM11.7`. Each relocated section also keeps a
`(formerly §11.X)` annotation naming its old position, so a `§11.X`
citation in code still resolves by grep even though the citable number
itself is now prefixed.

## 12. Component Architecture

Moved to [`Documentation/Design/Architecture.md`](Design/Architecture.md). Sections
there carry an `ARC` prefix and otherwise keep the numbers they had here, so
`§12.5` is now `§ARC12.5`. Each relocated section also keeps a
`(formerly §12.X)` annotation naming its old position, so a `§12.X` citation
in code still resolves by grep even though the citable number itself is now
prefixed.

## 13. AI Content Analysis

Moved to [`Documentation/Design/Approval.md`](Design/Approval.md). Sections
there carry an `APR` prefix and otherwise keep the numbers they had here, so
`§13.4` is now `§APR13.4`. Each relocated section also keeps a
`(formerly §13.X)` annotation naming its old position, so a `§13.X` citation
in code still resolves by grep even though the citable number itself is now
prefixed.

## 14. Visibility Rules

Moved to [`Documentation/Design/Security.md`](Design/Security.md). Sections
there carry a `SEC` prefix and otherwise keep the numbers they had here, so
`§14.6.1` is now `§SEC14.6.1`. Each relocated section also keeps a
`(formerly §14.X)` annotation naming its old position, so a `§14.X` citation
in code still resolves by grep even though the citable number itself is now
prefixed.

## 15. Recommended Corrections

### 15.1 Correct Typographical Issues

The draw.io model includes `ConentItemAssociation`.

The correct name should be:

```text
Association
```

### 15.2 Remove ApprovalId from Approvable Entities

The draw.io model included `ApprovalId` on `ContentItem` and `Association` as a direct foreign key to the `Approval` record. This has been resolved.

Final direction:

1. `ApprovalId` must not be placed on any approvable entity.
2. Approval lookup is performed generically through `Approval.EntityType` and `Approval.EntityId`.
3. `ApprovalId` on `Association` has been removed. Approval for an association is resolved through `Approval(EntityType = Association, EntityId = Association.Id)`.
4. `ApprovalId` remains valid only on `ApprovalReview` and `ApprovalComment` as a direct foreign key to their parent `Approval` record, not as a lookup from approvable entities.

### 15.3 Add Association to EntityType — done

`EntityType` includes `Association = 7`.

```csharp
Association = 7
```

This allows association records themselves to be approved through the same approval mechanism.

### 15.4 Add Topic Content Type

`Topic` does not require a separate `EntityType` because it is represented as a `ContentItem` with `ContentType = Topic`.

Recommended direction:

1. Add `Topic` as a seeded `ContentType`.
2. Use `EntityType.ContentItem` for topic parent/child associations.
3. Use `Association` to connect topics to child content items.
4. Exclude `Topic` from feed projections.

### 15.5 ContentItemSetting Type Mismatch — done

Resolved by converting `ContentType` from a database entity to a fixed enum (§3.6) rather than by changing `ContentItemSetting.ContentType` to a `Guid`. There is no `ContentType.Id` any more for the two sides to mismatch against — `ContentItem.ContentType`, `ContentItemSetting.ContentType`, and the nullable `ApprovalSetting.ContentType` (§8.4) are all typed `ContentType` and persisted as a string via `HasConversion<string>()`.

## 16. Recommended Service Responsibilities

Moved to [`Documentation/Design/Architecture.md`](Design/Architecture.md). Sections
there carry an `ARC` prefix and otherwise keep the numbers they had here, so
`§16.7.1` is now `§ARC16.7.1`. Each relocated section also keeps a
`(formerly §16.X)` annotation naming its old position, so a `§16.X` citation
in code still resolves by grep even though the citable number itself is now
prefixed.

## 17. Recommended API Design

Moved to [`Documentation/Design/Architecture.md`](Design/Architecture.md). Sections
there carry an `ARC` prefix and otherwise keep the numbers they had here, so
`§17.6` is now `§ARC17.6`. Each relocated section also keeps a
`(formerly §17.X)` annotation naming its old position, so a `§17.X` citation
in code still resolves by grep even though the citable number itself is now
prefixed.

## 18. Authentication and Authorisation

Moved to [`Documentation/Design/Security.md`](Design/Security.md). Sections
there carry a `SEC` prefix and otherwise keep the numbers they had here, so
`§18.6` is now `§SEC18.6`. Each relocated section also keeps a
`(formerly §18.X)` annotation naming its old position, so a `§18.X` citation
in code still resolves by grep even though the citable number itself is now
prefixed.

## 19. Search Engine Optimisation

Moved to [`Documentation/Design/Domain.md`](Design/Domain.md). Sections
there carry a `DOM` prefix and otherwise keep the numbers they had here, so
`§19.8` is now `§DOM19.8`. Each relocated section also keeps a
`(formerly §19.X)` annotation naming its old position, so a `§19.X`
citation in code still resolves by grep even though the citable number
itself is now prefixed.

## 20. UI / UX Design

Moved to [`Documentation/Design/UI.md`](Design/UI.md). Sections there carry a
`UI` prefix and otherwise keep the numbers they had here, so `§20.6.1` is now
`§UI20.6.1`. Each relocated section also keeps a `(formerly §20.X)` annotation
naming its old position, so a `§20.X` citation in code still resolves by grep
even though the citable number itself is now prefixed.

## 21. Summary

### 21.1 Final Design Direction

G2H should use `ContentItem` as the primary content model and represent different kinds of content through `ContentType`.

All content and supporting entities should use a shared approval workflow based on `EntityType` and `EntityId`, rather than direct entity-specific database relationships.

`Association` should be the generic relationship table that links content items to tags, reactions, comments, Bible references, links, attachments, and other content items.

`Topic` should be implemented as a `ContentItem` of type `Topic`, with child content items attached using `Association`.

The feed should not be a database entity. It should be a projection of visible, approved, published, non-deleted content items excluding `Topic`, ordered by publish date descending.

### 21.2 Immediate Next Changes

The next changes to look at, in dependency order (revised 2026-08-17 — the images, attachments and SEO workstream):

1. Seed content types including `Quote`, `Story`, `Testimony`, and `Topic` — verify seeding exists in migrations or startup pipeline.
2. The `Attachment` slice: exceptions, `AttachmentService` (§12.3 entry 12 — its approve operation must call `IAccessBroker`, §8.6.1), `AttachmentProcessingService` (§12.4 entry 3), registration and event subscriptions; the metadata columns (§5.6); `IBlobStorageBroker` with Azurite (§5.6.1). Update the dependency graph when the broker and services are built — its data is a snapshot of current source.
3. Upload and media endpoints (§5.6.2, §5.6.3, §17.6) and paste-to-upload in the editor (§5.6.6).
4. `Purpose` + `IsDefault` on `Association` (§4.9): columns, check constraints, index changes, foundation validation, `SetAssociationDefaultAsync`, the orchestration's `Attachment` endpoint arm, and the header-image picker UI. With it, the §5.6.5 derived approval on the host-approving publisher flow — the interim synchronous rule, moving to §12.5.3 responsibility 12 when the approval orchestration lands.
5. Stored SEO fields on `ContentItem` — `Slug`, `MetaDescription`, `ShortCode` (§19.2) — with the filtered unique indexes of §19.3 rule 2 and §19.7 rule 2, slug generation in `ContentItemProcessingService` (§12.4.1 rule 12), and short-code derivation in the approve transition (§9.7.1 rule 3).
6. `GET /api/content-items/by-slug/{contentType}/{slug}`, and feed fields including the resolved header-image media URL (§19.4).
7. The crawler middleware and `/{ContentType}/{Slug}` route (§19.8 — carries the §19.5 JSON-LD), and `/s/{code}` (§19.7).
8. Sitemap and `robots.txt` endpoints (§19.6).
9. The unused-attachment sweep, purge and blob-orphan operations (§5.6.7).
10. The replication proof: a `BibleReference` verse image end-to-end — the same upload (§5.6.3), an `Attachment` ↔ `BibleReference` association with `Purpose = Verse` (§4.9), derived approval (§5.6.5) and the same top-1 resolution, with zero `BibleReference` schema changes.

Item 5 can proceed independently of items 2–4; item 6 needs both tracks (the §4.9 resolution from item 4 and the columns from item 5); items 7–8 follow 5–6; items 9–10 close the workstream. The portal rendering real content items (§20) is the surface items 6–8 exist for.

