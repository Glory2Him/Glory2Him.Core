# G2H Design

## IDX1. Design Overview *(formerly §1)*

### IDX1.1 Purpose *(formerly §1.1)*

Glory 2 Him (G2H) is a content management system designed to allow users to contribute, organise, review, approve, publish, associate, and consume gospel-focused content.

The system is centred around `ContentItem`, which represents primary user-contributed content. Examples of content types include:

1. `Quote`
2. `Story`
3. `Testimony`
4. `Topic`
5. Future content types

All user-contributed and configurable content is subject to an approval process before it is considered trusted, visible, or publishable.

### IDX1.2 Core Design Principles *(formerly §1.2)*

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

### IDX1.3 Source Inputs *(formerly §1.3)*

This design is based on:

1. The `Glory 2 Him.drawio` design file.
2. The current C# entity model files.
3. The current EF Core model snapshot.
4. The supplied design direction for approval, settings, feed, topic, versioning, visibility, and soft delete behaviour.

### IDX1.4 Current Model Completion Status *(formerly §1.4)*

The current source files are not complete. This document separates the design into:

1. Current implemented model.
2. Diagram-driven intended model.
3. Required model extensions.
4. Recommended design rules.
5. Final agreed direction where this supersedes earlier diagram wording.

### IDX1.5 Numbering And The Citation Form *(formerly §1.5)*

**Numbering — prefix-preserving, not renumbered**

A relocated heading keeps its number exactly and gains its file's prefix:

<!-- The three example lines are indented so that they are quoted heading syntax
     rather than lines a line-based reader takes for headings. Do not dedent:
     `Tools/design-split-audit.sh` gate G3 counts every `^#{1,6} ` line under
     `Documentation/` as a heading, and at column 0 these three would make
     §8, §8.6 and §8.6.1 resolve to two headings each. -->
```
  ## 8. Approval Settings Design            ->  ## APR8. Approval Settings Design *(formerly §8)*
  ### 8.6 Self-Approval Rules               ->  ### APR8.6 Self-Approval Rules *(formerly §8.6)*
  #### 8.6.1 Where These Rules Are Enforced ->  #### APR8.6.1 Where These Rules Are Enforced *(formerly §8.6.1)*
```

Nothing renumbers. Section order within a file is the order the sections stand in
today. Numbering within a file is therefore **not contiguous** — `Domain.md` runs
DOM2–DOM6, DOM11, DOM19 — and that is accepted, not a defect to tidy. Each file
**must** open with a contents list, so the gaps read as a table of contents
rather than as missing content.

Three reasons, in order of weight:

1. **The transformation becomes mechanical and therefore checkable.** Old `§N.M`
   maps to new `§<PREFIX>N.M` by a rule a script can apply and a script can
   verify. The document carries 213 numbered headings, 192 of them subsections,
   re-measured at the commit the split branches from; renumbering them into six
   files by hand is a transformation with 213 chances to drop one, which is
   precisely the incident class #481 names as its first non-negotiable. *(214 is
   the count of heading **lines**, which includes the unnumbered `# G2H Design`
   title — not a section, and it does not move. Gate G2 compares numbered
   headings, so 213 and 192 are the figures it works from.)*
2. **Old citations stay legible.** `§8.6.1` and `§APR8.6.1` are recognisably the
   same section to a human reading a five-year-old comment, which no renumbering
   scheme gives for free.
3. **Ordering anomalies survive untouched.** `§16.7.5` currently stands between
   `§16.7.2` and `§16.7.3`. Under prefix-preserving numbering it stays
   `§ARC16.7.5` in its current position and nothing about it is a decision the
   split has to make. **Do not reorder or renumber it** — that is a separate
   change with its own citation consequences.

**This deliberately diverges from `Events.md`, and the divergence is not an
inconsistency to correct.** `Events.md` renumbered §10.X to §EVNx because it was a
*merge* of two documents with new sections interleaved between them; the two
sources numbered independently, so no scheme that preserved either source's
numbers could have identified a section unambiguously. A split has no such
forcing. What the split inherits from `Events.md` is the prefix idea and the
`*(formerly §N.M)*` annotation, not the renumbering.

**The citation form**

A citation is the bare prefixed number and nothing else:

```
§DOM4.2      §APR8.6.1      §ARC12.5      §SEC14.7      §UI20.6.1      §EVN18      §IDX1.5
```

- **No filename.** `Domain.md §4.2` — the form issue #498 used — is **retired**.
  A prefix already identifies the file uniquely, so a filename in a citation is a
  second thing that can be wrong, and a stale path in a code comment is worse than
  no path.
- **No bare dotted number** inside `Documentation/`. A `§8.6.1` with no prefix
  written after the split is a finding. Pre-split occurrences in code are left
  alone.
- **This document carries the `IDX` prefix, and so does a citation of it.** §IDX1
  is front matter that governs every area file, so it is cited from them, and a
  bare `§1.5` would be the one form this rule forbids. `§1.1.3` already stands in
  sixteen code comments meaning The Standard rather than this document, which is
  the ambiguity a prefix removes. The pointer stubs below keep their bare old
  numbers — `## 8. Approval Settings Design` — because a stale deep link is what
  they exist to catch; inside this document a prefixed heading is a home and is
  citable, a bare-numbered one is a signpost and is not.
- **Lettered forms** follow `Events.md`: where comments cite `§10.17(a)`, the
  heading annotation carries no letter, so the section body names the lettered
  forms explicitly. `§ARC12.1 rule 2` and similar rule-number citations are
  unaffected — the prefix goes on the section number only.

## Where Each Section Lives

A stale deep link into this file lands here, at the top, rather than at a
heading that has moved. This map is the one hop back: find the old number, and
it names the file the section is in now and the number to cite it by.

| Old | Title | Lives in | Cite it as |
| --- | --- | --- | --- |
| §1 | Design Overview | This document — front matter for the whole design, not an area | `§IDX1` |
| §2 | Domain Model Overview | [`Design/Domain.md`](Design/Domain.md) — extracted; §2 below is the pointer stub | `§DOM2` |
| §3 | Content Design | [`Design/Domain.md`](Design/Domain.md) — extracted; §3 below is the pointer stub | `§DOM3` |
| §4 | Association Design | [`Design/Domain.md`](Design/Domain.md) — extracted; §4 below is the pointer stub | `§DOM4` |
| §5 | Supporting Content Entities | [`Design/Domain.md`](Design/Domain.md) — extracted; §5 below is the pointer stub | `§DOM5` |
| §6 | ContentItemSetting Design | [`Design/Domain.md`](Design/Domain.md) — extracted; §6 below is the pointer stub | `§DOM6` |
| §7 | Approval Design | [`Design/Approval.md`](Design/Approval.md) — extracted; §7 below is the pointer stub | `§APR7` |
| §8 | Approval Settings Design | [`Design/Approval.md`](Design/Approval.md) — extracted; §8 below is the pointer stub | `§APR8` |
| §9 | Approval Lifecycle | [`Design/Approval.md`](Design/Approval.md) — extracted; §9 below is the pointer stub | `§APR9` |
| §10 | Event Design | [`Design/Events.md`](Design/Events.md) — extracted; §10 below is the pointer stub | `§EVNn` — renumbered rather than prefixed, so look the old number up in that file's *(formerly §10.X)* annotations rather than deriving it |
| §11 | Topic and Feed Design | [`Design/Domain.md`](Design/Domain.md) — extracted; §11 below is the pointer stub | `§DOM11` |
| §12 | Component Architecture | [`Design/Architecture.md`](Design/Architecture.md) — extracted; §12 below is the pointer stub | `§ARC12` |
| §13 | AI Content Analysis | [`Design/Approval.md`](Design/Approval.md) — extracted; §13 below is the pointer stub | `§APR13` |
| §14 | Visibility Rules | [`Design/Security.md`](Design/Security.md) — extracted; §14 below is the pointer stub | `§SEC14` |
| §15 | Recommended Corrections | **Retired** — checked item by item against the repository and deleted rather than relocated. The one live rule it still carried, that `ApprovalId` must not be placed on any approvable entity, is now §APR7.4 item 6 | — |
| §16 | Recommended Service Responsibilities | [`Design/Architecture.md`](Design/Architecture.md) — extracted; §16 below is the pointer stub | `§ARC16` |
| §17 | Recommended API Design | [`Design/Architecture.md`](Design/Architecture.md) — extracted; §17 below is the pointer stub | `§ARC17` |
| §18 | Authentication and Authorisation | [`Design/Security.md`](Design/Security.md) — extracted; §18 below is the pointer stub | `§SEC18` |
| §19 | Search Engine Optimisation | [`Design/Domain.md`](Design/Domain.md) — extracted; §19 below is the pointer stub | `§DOM19` |
| §20 | UI / UX Design | [`Design/UI.md`](Design/UI.md) — extracted; §20 below is the pointer stub | `§UI20` |
| §21 | Summary | **Retired** — §21.1 restated rules that are defined elsewhere and was deleted sentence by sentence; §21.2 is in this document under *Roadmap — Immediate Next Changes*, which is a work plan rather than design | — |

The six area files, one line each:

- [`Design/Domain.md`](Design/Domain.md) — `DOM` — the entity model: content, associations, supporting entities, settings, topic and feed, SEO. **Exists.**
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

## Roadmap — Immediate Next Changes

**This is a live work plan, not design.** Nothing under this heading is a rule,
and nothing here is authoritative about what the system does — the area files
under `Documentation/Design/` are. The split that turned this document into an
index **verified none of these items**: they were moved here unaudited, and an
item may already be built, abandoned or superseded.

The next changes to look at, in dependency order (revised 2026-08-17 — the images, attachments and SEO workstream):

1. Seed content types including `Quote`, `Story`, `Testimony`, and `Topic` — verify seeding exists in migrations or startup pipeline.
2. The `Attachment` slice: exceptions, `AttachmentService` (§ARC12.3 entry 12 — its approve operation must call `IAccessBroker`, §APR8.6.1), `AttachmentProcessingService` (§ARC12.4 entry 3), registration and event subscriptions; the metadata columns (§DOM5.6); `IBlobStorageBroker` with Azurite (§DOM5.6.1). Update the dependency graph when the broker and services are built — its data is a snapshot of current source.
3. Upload and media endpoints (§DOM5.6.2, §DOM5.6.3, §ARC17.6) and paste-to-upload in the editor (§DOM5.6.6).
4. `Purpose` + `IsDefault` on `Association` (§DOM4.9): columns, check constraints, index changes, foundation validation, `SetAssociationDefaultAsync`, the orchestration's `Attachment` endpoint arm, and the header-image picker UI. With it, the §DOM5.6.5 derived approval on the host-approving publisher flow — the interim synchronous rule, moving to §ARC12.5.3 responsibility 12 when the approval orchestration lands.
5. Stored SEO fields on `ContentItem` — `Slug`, `MetaDescription`, `ShortCode` (§DOM19.2) — with the filtered unique indexes of §DOM19.3 rule 2 and §DOM19.7 rule 2, slug generation in `ContentItemProcessingService` (§ARC12.4.1 rule 12), and short-code derivation in the approve transition (§APR9.7.1 rule 3).
6. `GET /api/content-items/by-slug/{contentType}/{slug}`, and feed fields including the resolved header-image media URL (§DOM19.4).
7. The crawler middleware and `/{ContentType}/{Slug}` route (§DOM19.8 — carries the §DOM19.5 JSON-LD), and `/s/{code}` (§DOM19.7).
8. Sitemap and `robots.txt` endpoints (§DOM19.6).
9. The unused-attachment sweep, purge and blob-orphan operations (§DOM5.6.7).
10. The replication proof: a `BibleReference` verse image end-to-end — the same upload (§DOM5.6.3), an `Attachment` ↔ `BibleReference` association with `Purpose = Verse` (§DOM4.9), derived approval (§DOM5.6.5) and the same top-1 resolution, with zero `BibleReference` schema changes.

Item 5 can proceed independently of items 2–4; item 6 needs both tracks (the §DOM4.9 resolution from item 4 and the columns from item 5); items 7–8 follow 5–6; items 9–10 close the workstream. The portal rendering real content items (§UI20) is the surface items 6–8 exist for.
