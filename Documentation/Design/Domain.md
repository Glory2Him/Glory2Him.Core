# Domain

Carries `G2H Design.md` §2 "Domain Model Overview", §3 "Content Design",
§4 "Association Design", §5 "Supporting Content Entities", §6
"ContentItemSetting Design", §11 "Topic and Feed Design" and §19 "Search
Engine Optimisation" — the entity model: content, associations, supporting
entities, settings, topic and feed, SEO.

Section numbers below carry a **`DOM` prefix** and are otherwise the numbers
these sections already had: an old `§2.N` is now `§DOM2.N`, an old `§3.N` is
now `§DOM3.N`, an old `§4.N` is now `§DOM4.N`, an old `§5.N` is now `§DOM5.N`,
an old `§6.N` is now `§DOM6.N`, an old `§11.N` is now `§DOM11.N`, and an old
`§19.N` is now `§DOM19.N`, and nothing was renumbered, reordered, merged or
split in the move. That is the prefix-preserving rule of §1.5. It means this
file runs from `DOM2` to `DOM6` and then jumps to `DOM11` and then to `DOM19`,
so its numbering is not contiguous with the other area files, and the contents
list below is what makes those two gaps read as a table of contents rather than
as missing content. **Unlike `Approval.md`, `Architecture.md` and
`Security.md`, this file carries no heading-level anomaly to preserve** — every
heading's level matches its number's depth throughout, `##` for `N`, `###` for
`N.M` and `####` for `N.M.K`. The other `Documentation/Design/*.md` files carry
their own prefixes — `APR`, `ARC`, `EVN`, `SEC`, `UI` — so a bare `§DOM4.2` is
unambiguous once they exist. Where this file cites one of them, the map at the
top of [`G2H Design.md`](../G2H%20Design.md) is what resolves it.

**`Events.md` is the one file a citation into it cannot be derived for.** It
renumbered rather than prefix-preserved, so an into-Events citation is looked
up in that file's own *(formerly §10.X)* annotations instead of having a
prefix applied to the number it already had. This file carries exactly one
such citation — `§EVN4`, in §DOM5.6.4 — looked up against `Events.md`'s
own heading ("EVN4. Soft Delete Behaviour *(formerly §10.4)*") rather than
derived: `§EVN10.4` would have been the wrong answer, and `§EVN10` already
stands as a different section.

This repository's C# and TypeScript comments cite design sections extensively,
and this file's seven relocated ranges are no exception, so every relocated
section also carries a *(formerly §2.X)*, *(formerly §3.X)*, *(formerly
§4.X)*, *(formerly §5.X)*, *(formerly §6.X)*, *(formerly §11.X)* or
*(formerly §19.X)* annotation naming its old position. The literal old
string still appears on the right heading, so an old citation resolves by grep
even though the citable number is now prefixed.

**Contents**

- [DOM2. Domain Model Overview](#dom2-domain-model-overview-formerly-2)
  - [DOM2.1 Main Domain Areas](#dom21-main-domain-areas-formerly-21)
  - [DOM2.2 Main Entity Groups](#dom22-main-entity-groups-formerly-22)
- [DOM3. Content Design](#dom3-content-design-formerly-3)
  - [DOM3.1 ContentItem](#dom31-contentitem-formerly-31)
  - [DOM3.2 ContentItem Properties](#dom32-contentitem-properties-formerly-32)
  - [DOM3.3 Content Versioning](#dom33-content-versioning-formerly-33)
  - [DOM3.4 Content Versioning Rules](#dom34-content-versioning-rules-formerly-34)
    - [DOM3.4.1 The Version Tip and the Published Row](#dom341-the-version-tip-and-the-published-row-formerly-341)
    - [DOM3.4.2 Duplicate Content Rule](#dom342-duplicate-content-rule-formerly-342)
  - [DOM3.5 Approval Invalidation Rules](#dom35-approval-invalidation-rules-formerly-35)
  - [DOM3.6 ContentType](#dom36-contenttype-formerly-36)
  - [DOM3.7 ContentType Properties](#dom37-contenttype-properties-formerly-37)
  - [DOM3.8 Content Type Rules](#dom38-content-type-rules-formerly-38)
  - [DOM3.9 Series vs. Topic — to revisit](#dom39-series-vs-topic--to-revisit-formerly-39)
- [DOM4. Association Design](#dom4-association-design-formerly-4)
  - [DOM4.1 Purpose](#dom41-purpose-formerly-41)
  - [DOM4.2 Endpoint Shape](#dom42-endpoint-shape-formerly-42)
  - [DOM4.3 Scope Rules](#dom43-scope-rules-formerly-43)
  - [DOM4.4 Canonical Ordering](#dom44-canonical-ordering-formerly-44)
  - [DOM4.5 Derived and Pinned Endpoint Fields](#dom45-derived-and-pinned-endpoint-fields-formerly-45)
  - [DOM4.6 Effective Id](#dom46-effective-id-formerly-46)
  - [DOM4.7 Associated Entity Types](#dom47-associated-entity-types-formerly-47)
  - [DOM4.8 Association Approval](#dom48-association-approval-formerly-48)
  - [DOM4.9 Purposeful Placements — Purpose and IsDefault](#dom49-purposeful-placements--purpose-and-isdefault-formerly-49)
- [DOM5. Supporting Content Entities](#dom5-supporting-content-entities-formerly-5)
  - [DOM5.1 Tag](#dom51-tag-formerly-51)
  - [DOM5.2 Reaction](#dom52-reaction-formerly-52)
  - [DOM5.3 Comment](#dom53-comment-formerly-53)
  - [DOM5.4 BibleReference](#dom54-biblereference-formerly-54)
  - [DOM5.5 Link](#dom55-link-formerly-55)
  - [DOM5.6 Attachment](#dom56-attachment-formerly-56)
    - [DOM5.6.1 Physical Storage](#dom561-physical-storage-formerly-561)
    - [DOM5.6.2 Serving — the Media Endpoint](#dom562-serving--the-media-endpoint-formerly-562)
    - [DOM5.6.3 Upload Rules](#dom563-upload-rules-formerly-563)
    - [DOM5.6.4 Versioning and Replacement](#dom564-versioning-and-replacement-formerly-564)
    - [DOM5.6.5 Approval — Derived From the Host](#dom565-approval--derived-from-the-host-formerly-565)
    - [DOM5.6.6 Inline Images](#dom566-inline-images-formerly-566)
    - [DOM5.6.7 Unused-Attachment Lifecycle](#dom567-unused-attachment-lifecycle-formerly-567)
- [DOM6. ContentItemSetting Design](#dom6-contentitemsetting-design-formerly-6)
  - [DOM6.1 Purpose](#dom61-purpose-formerly-61)
  - [DOM6.2 Default and Override Behaviour](#dom62-default-and-override-behaviour-formerly-62)
  - [DOM6.3 Default Rule](#dom63-default-rule-formerly-63)
  - [DOM6.4 Override Rule](#dom64-override-rule-formerly-64)
  - [DOM6.5 Current Settings](#dom65-current-settings-formerly-65)
  - [DOM6.6 ContentItemSetting Properties](#dom66-contentitemsetting-properties-formerly-66)
  - [DOM6.7 Recommended Settings Extension](#dom67-recommended-settings-extension-formerly-67)
  - [DOM6.8 ContentItemSetting.ContentType typing — done](#dom68-contentitemsettingcontenttype-typing--done-formerly-68)
  - [DOM6.9 BibleReferenceSetting](#dom69-biblereferencesetting-formerly-69)
  - [DOM6.10 Resolving Settings for an Association](#dom610-resolving-settings-for-an-association-formerly-610)
- [DOM11. Topic and Feed Design](#dom11-topic-and-feed-design-formerly-11)
  - [DOM11.1 Topic as Content](#dom111-topic-as-content-formerly-111)
  - [DOM11.2 Topic Is Not a Feed Item](#dom112-topic-is-not-a-feed-item-formerly-112)
  - [DOM11.3 Feed as a Domain Projection](#dom113-feed-as-a-domain-projection-formerly-113)
  - [DOM11.4 Topic Parent/Child Relationship](#dom114-topic-parentchild-relationship-formerly-114)
  - [DOM11.5 Topic Visibility](#dom115-topic-visibility-formerly-115)
  - [DOM11.6 Topic Child Visibility](#dom116-topic-child-visibility-formerly-116)
  - [DOM11.7 Topic Ordering](#dom117-topic-ordering-formerly-117)
  - [DOM11.8 Future Topic Subscriptions](#dom118-future-topic-subscriptions-formerly-118)
- [DOM19. Search Engine Optimisation](#dom19-search-engine-optimisation-formerly-19)
  - [DOM19.1 Purpose](#dom191-purpose-formerly-191)
  - [DOM19.2 ContentItem SEO Fields](#dom192-contentitem-seo-fields-formerly-192)
  - [DOM19.3 Slug Rules](#dom193-slug-rules-formerly-193)
  - [DOM19.4 API SEO Considerations](#dom194-api-seo-considerations-formerly-194)
  - [DOM19.5 Structured Data Recommendations](#dom195-structured-data-recommendations-formerly-195)
  - [DOM19.6 Sitemap and Indexing](#dom196-sitemap-and-indexing-formerly-196)
  - [DOM19.7 Short Links](#dom197-short-links-formerly-197)
  - [DOM19.8 Crawler Rendering — Head Injection](#dom198-crawler-rendering--head-injection-formerly-198)

---

## DOM2. Domain Model Overview *(formerly §2)*

### DOM2.1 Main Domain Areas *(formerly §2.1)*

The domain model is grouped into the following areas:

1. Content
2. Content Types
3. Content Settings
4. Content Associations
5. Approval
6. Approval Policy Settings
7. Supporting Content Entities
8. Feed Projection
9. Topic Grouping
10. Events
11. AI Content Analysis
12. Security and Audit
13. Soft Delete

### DOM2.2 Main Entity Groups *(formerly §2.2)*

| Area | Entities |
| --- | --- |
| Content | `ContentItem`, `ContentType`, `ContentItemSetting`, `Association` |
| Approval | `Approval`, `ApprovalReview`, `ApprovalComment`, `ApprovalSetting` |
| Associated Entities | `Tag`, `Reaction`, `Comment`, `BibleReference`, `Link`, `Attachment` |
| Enum / Lookup | `EntityType`, `ApprovalStatus`, `Scope` |
| Future Subscription | `Subscription`, `SubscriptionDelivery`, or equivalent decoupled subscription records |

## DOM3. Content Design *(formerly §3)*

### DOM3.1 ContentItem *(formerly §3.1)*

`ContentItem` is the central content entity in the system.

It represents a versioned item of contributed content such as a quote, story, testimony, topic, or future content type.

### DOM3.2 ContentItem Properties *(formerly §3.2)*

The content item model should contain the following design-relevant properties:

| Property | Purpose |
| --- | --- |
| `Id` | Unique identifier for this specific content version. |
| `ContentType` | Identifies the type of content, such as `Quote`, `Story`, `Testimony`, or `Topic`. |
| `Title` | Optional content title. |
| `Author` | Optional content author. |
| `Content` | Required body content. |
| `ContentHash` | SHA-256 hash of the normalized `Content` (trim, collapse whitespace, lowercase). Control field computed on every write. Non-unique index on (`ContentType`, `ContentHash`) for duplicate detection (§DOM3.4.2). |
| `GroupId` | Groups multiple versions of the same logical content item. |
| `Version` | Version number for the item. The group's **tip** — the row edits go to — is the highest `Version` among its non-deleted rows, derived rather than stored (§DOM3.4.1). |
| `IsPublished` | Identifies the currently published version. Only one row per `GroupId` may be published. |
| `ApprovalStatus` | Denormalized approval state (`Draft`, `Submitted`, `Approved`, `Rejected`). Mirrors the linked `Approval` record. `Approval` remains the source of truth. |
| `PublishDate` | Optional date/time from which the content can be visible. |
| `IsDeleted` | Soft-delete flag. When `true` the item is excluded from all public visibility. |
| `CreatedBy` | User who created the item. |
| `CreatedWhen` | Creation timestamp. |
| `UpdatedBy` | User who last updated the item. |
| `UpdatedWhen` | Last update timestamp. |
| `DeletedBy` | User who deleted the item. |
| `DeletedWhen` | Deletion timestamp. |
| `DeletionReason` | Reason for deletion. |

SEO and share fields — `Slug`, `MetaDescription` and `ShortCode` — are specified in §DOM19.2, §DOM19.3 and §DOM19.7 and are not columns yet. `Content` may reference uploaded images inline by their media URL (§DOM5.6.6); an inline reference is body text, not a column and not an association.

### DOM3.3 Content Versioning *(formerly §3.3)*

Content is versioned by using:

1. `Id` for the specific version.
2. `GroupId` for the logical content item across all versions.
3. `Version` for the version number.
4. The highest `Version` among the group's non-deleted rows to identify the latest editable version. This is **derived, not a stored flag** (§DOM3.4.1).
5. `IsPublished` to identify the current public version.

### DOM3.4 Content Versioning Rules *(formerly §3.4)*

The following rules apply:

1. A new content item starts with `Version = 1`.
2. A new content item is its group's tip by construction — it is the only row, so it carries the highest `Version`. Nothing is written to say so (§DOM3.4.1).
3. A new content item starts with `IsPublished = false` unless it is approved and published through the approval workflow.
4. A content item in `Draft` or `Submitted` may be edited in place.
5. Editing a `Draft` or `Submitted` item does not create a new version.
6. If approval reviews have already been submitted and the content item itself changes, those reviews must be dismissed (subject to `ApprovalSetting.RequireReapprovalOnChange`) and the item must be reviewed again. The item itself remains in its current status.
7. **`Approved` and `Rejected` are terminal.** A content item in either state is immutable in place — to its owner, to a publisher, and to an administrator alike. No role amends a terminal row's content, and there is no in-place exception (rule 16).
8. Editing a terminal content item creates a new `ContentItem` row with the same `GroupId` and incremented `Version`. The owner is the only creator of new versions — `Publishers` and `Administrators` roles never create version forks.

   A rejected row is terminal on the same terms as an approved one, and for the same reason: reviewers reached a verdict on that text, and text that changes underneath a verdict makes the verdict a record of nothing. The difference is only in what stays live — a **rejected** row never published, so a fork off one leaves the group with no public row until the new version is approved, where a fork off an approved one leaves the approved row published throughout (rule 12).
9. The new version becomes the group's tip, because its `Version` is the highest in the group. The fork is therefore ONE write.
10. The previous latest version stops being the tip for the same reason, and **nothing is written to demote it**. This rule used to require a second write, and that shape is withdrawn: a fork that demoted first and then failed to insert satisfied the old unique index while leaving the group with no tip at all — permanently uneditable, because the demote only ever wrote `false` (issue #265). Derived, that state cannot be represented.
11. The new version must not become `IsPublished = true` until approved.
12. The previously published version remains `IsPublished = true` until the new version is approved and published.
13. Exactly one content item per `GroupId` is the tip at any moment, and the derivation makes that true rather than enforcing it: the unique index on (`GroupId`, `Version`) admits one row per version, so the highest non-deleted `Version` in a group names exactly one row.
14. Only one content item per `GroupId` may have `IsPublished = true`.
15. Previous versions must remain available for audit, approval history, comparison, and rollback.
16. **There is no in-place amendment of a terminal item, by any role.** This rule previously granted an administrator one: amend an approved item without forking, resetting its approval to `Submitted` and dismissing active reviews. It is withdrawn, because a state that one role can edit out of is not terminal, and rule 7 depends on it being terminal for everyone.

    What replaces it is narrower and leaves a record. An administrator may move a terminal item's **status** back to `Submitted` through the approval transition operation (§APR8.6 HR-4, §APR9.7.1 rule 3) — an override, gated to `Administrators` alone, which unpublishes the row on the way out of `Approved`. Ordinary editing resumes only once the row is no longer terminal. The two acts stay separate: a status transition changes no content, and a content edit changes no status.
17. While such a re-opened item is pending, it no longer satisfies canonical content visibility (its `ApprovalStatus` is `Submitted`) and is not publicly visible until approved again.
18. **There is no stored latest-version flag to write.** This rule used to name the two points at which `IsLatestVersion` was written; the column is gone (issue #265) and the tip is read off the group's rows, so no operation — submit, review, approve, publish, or an administrator status override — can move the tip other than by adding a version.

#### DOM3.4.1 The Version Tip and the Published Row *(formerly §3.4.1)*

The **tip** of the version chain is the row edits go to: the highest `Version` among the group's non-deleted rows. It is **derived, never stored**. `IsPublished` marks the row the public sees, and is stored. During a review window the two deliberately sit on different rows.

Exactly one row per `GroupId` is the tip at any moment — a consequence of the unique (`GroupId`, `Version`) index rather than a rule anything has to uphold. At most one `IsPublished = true` per `GroupId`, and that one *is* enforced, by a unique filtered index over the group's **live** rows — `WHERE IsPublished = 1 AND IsDeleted = 0`. The `IsDeleted` term is what stops a soft-deleted row holding the slot against versions that cannot see it; every versioned entity's slot index is declared from one place so the three cannot drift apart again, and a model test fails when a new one arrives without it (§DOM5.6.4 rule 4).

The asymmetry is the point. "Exactly one tip" was previously enforced in two halves — a filtered unique index guaranteed *at most* one, and application code was trusted for *at least* one — and the halves came apart: a fork was demote-then-insert, so an insert that failed left a group with no tip, permanently uneditable. Derived, the state cannot be represented, and a failed fork writes nothing at all. "At most one published" has no matching failure mode: a group with no published row is an ordinary, recoverable state (§APR9.7.7 rule 7), so the stored flag and its index stay.

| Lifecycle event | Tip of the group | `IsPublished` |
| --- | --- | --- |
| Create V1 | V1 (the only row) | V1 = `false` |
| Edit a `Draft` or `Submitted` item (in place) | unchanged | unchanged |
| Owner edits a terminal item — `Approved` or `Rejected` (fork) | the new row, by carrying the higher `Version`; nothing is written to the previous tip | new row = `false`; previously published row unchanged |
| Submit / review / reject | unchanged | unchanged |
| Approve + publish | unchanged (approval adds no version) | approved row = `true`; previously published row = `false` |
| `Administrators` overrides a terminal item's status back to `Submitted` | unchanged | that row = `false` (§APR8.6 HR-4); no other row is republished |

Worked example (V1 published, owner edits):

| Step | V1 | V2 |
| --- | --- | --- |
| V1 approved + published | tip, published=`true` | — |
| Owner edits → fork V2 | published=`true` (still live) | tip, published=`false`, `Draft` |
| V2 submitted, under review | published=`true` | tip, published=`false`, `Submitted` |
| V2 approved + published | published=`false` | tip, published=`true` |

Worked example (V1 rejected, owner edits) — the case that distinguishes a rejected terminal row:

| Step | V1 | V2 |
| --- | --- | --- |
| V1 rejected | tip, published=`false`, `Rejected` | — |
| Owner edits → fork V2 | published=`false` | tip, published=`false`, `Draft` |
| V2 approved + published | published=`false` | tip, published=`true` |

Note the middle row: the group has **no published version at all** while V2 is in review, because V1 never had one to keep. Nothing is demoted at V2's publish, so §APR9.7.7 rule 7's ordering has nothing to order.

#### DOM3.4.2 Duplicate Content Rule *(formerly §3.4.2)*

Purpose: two different people cannot submit the exact same content.

1. The duplicate match compares `Content` only (not `Title` or `Author`).
2. The match is normalized: trim ends, collapse whitespace/newline runs to a single space, lowercase (invariant culture). The normalization function is a frozen contract — changing it requires recomputing every stored hash in a migration.
3. The match is scoped per `ContentType`.
4. The match compares against all non-deleted rows (any status, any version). On modify, the item's own `GroupId` is excluded.
5. Mechanism: `ContentHash` = SHA-256 of the normalized content, computed by the orchestration on every write and stored on `ContentItem`. A non-unique index on (`ContentType`, `ContentHash`) makes the check an index seek. The index must not be unique — rows within one group may legitimately share a hash (for example a later version reverting to earlier wording); enforcement is application-side.
6. Response on a duplicate: add → polite acknowledgement ("Thank you for your submission") without creating the record and without revealing the duplicate; modify → validation error.

   **What the acknowledgement is, settled (#412).** The add answers *exactly* as a successful add answers — `201 Created`, with a `ContentItem` body composed field for field the way the persisted row would have been: a minted `Id` and `GroupId`, `Version` 1, unpublished, `Draft`, the computed `ContentHash`, and the same audit stamps the foundation applies. Nothing is written and **no completion fact is published**: a fact says a row exists, and none does. On the event path the same acknowledgement comes back as the delivery's *reply*, which is recorded as that delivery's response and reaches the requester alone — that asymmetry between a reply and a fact is what makes the acknowledgement safe to send there.

   The rule is not satisfied by hiding the message alone. **Both halves of the answer must be indistinguishable, or the arm that differs is the probe.** Two consequences follow, and both are enforcement, not commentary:

   - **Every rule the add applies to caller-supplied fields must be asked at the processing layer**, above the duplicate probe — not only at the foundation the quiet arm never reaches. A rule asked in one place only answers "duplicate" with an acknowledgement and "not a duplicate" with a validation error, which is a cleaner oracle than the message this rule replaces. The foundation keeps asking them too (§APR8.6.1, §SEC14.6 rule 2).
   - **The client must not act on the acknowledgement's `Id`.** Following it lands on a row that is not there, and a 404 names the duplicate as plainly as any message. The contribution page therefore thanks the contributor and returns them to their own posts, taking the same journey for both outcomes.

   **What this deliberately does not close, stated at its real size.** No row is created, so the contributor's own reads will not show one. The rule closes the *answer*, not a later look; closing that too would mean creating a record, which is the thing the rule forbids.

   **Timing is not closed at all, and calling it "close" would be false.** The genuine arm performs a row insert, a `ProcessedEvents` insert and two publishes to the event store (the foundation's `ContentItem-Added` and this service's `ContentItemProcessing-Added`); the quiet arm performs none of them. That is a database write plus two cross-store round trips, not a sub-millisecond difference, and an attacker timing the endpoint recovers the yes/no answer the body was rewritten to hide — on few enough samples to be practical. **The response-shape rule therefore raises the cost of the probe; it does not eliminate it.** It is recorded as an open residual rather than padded, because a sleep long enough to cover a variable multi-store write is both unreliable and a denial-of-service surface of its own. Closing it properly means making the two arms do comparable work — the quiet arm performing the write into a discarded transaction, or the probe moving behind a uniform-latency boundary — and that is a larger change than rule 6 itself, to be designed rather than bolted on here.

### DOM3.5 Approval Invalidation Rules *(formerly §3.5)*

Approval invalidation is entity-scoped.

A change to an entity only invalidates approvals for that specific entity and must not reset approvals of unrelated entities.

For `ContentItem`:

1. Changes to `Title`, `Author`, `Content`, `ContentType`, `PublishDate`, or other approval-sensitive content metadata may invalidate the content item's own approval.
2. If reviews exist for the content item, the reviews should be marked as `Dismissed` when the content changes.
3. The approval status of the item does not change when reviews are dismissed — a `Submitted` item remains `Submitted`. There is no exception: the `Administrators` in-place amendment that used to be one is withdrawn (§DOM3.4 rule 16), and an amendment of a terminal item forks rather than resetting anything.
4. Reviewers must review the updated content again.

For linked entities:

1. Changes to tags, comments, reactions, Bible references, links, or attachments must not invalidate the parent `ContentItem` approval.
2. Only the changed entity's own approval lifecycle is affected.
3. Only the changed association's approval lifecycle is affected when the association itself changes.

Example:

1. A story is approved and published.
2. A new tag is associated to the story.
3. The tag or association may require approval.
4. The story remains approved and published.
5. The tag is only visible on the story once the tag and association are visible according to policy.

### DOM3.6 ContentType *(formerly §3.6)*

`ContentType` is a fixed C# enum, not a database entity or a `ContentItem`. There is no `ContentTypes` table, no foundation service, no orchestration, and no lifecycle — a content type is a compile-time constant of the running application, not admin- or user-defined data.

```csharp
public enum ContentType
{
    Quote = 0,
    Story = 1,
    Testimony = 2,
    Devotional = 3,
    BibleStudy = 4,
    BlogPost = 5,
    Series = 999,
    Topic = 1000
}
```

`Series` and `Topic` are numbered apart from the standalone content types above — see §DOM3.9.

### DOM3.7 ContentType Properties *(formerly §3.7)*

Not applicable. `ContentType` has no properties of its own — it is persisted as a string (`HasConversion<string>()`, matching `Scope`, and matching `EntityType` on `ApprovalSettings` and `Associations`; the unconverted exceptions are `ApprovalStatus`, which has no conversion on any table, and `EntityType` on `Approvals` — both persist as `int`) wherever it is stored, and it is `ContentItem`, `ContentItemSetting`, and `ApprovalSetting` that carry a `ContentType` value, not the reverse. Adding, renaming, or removing a member is a code change and a release, not a runtime CRUD operation.

### DOM3.8 Content Type Rules *(formerly §3.8)*

The following rules apply:

1. `Topic` and `Series` are `ContentType` members, not separate root entities.
2. The feed must exclude `Topic` and `Series` content items.
3. Any publishable content type except `Topic` and `Series` can appear in the feed.
4. `ContentItem.ContentType` is set on creation and never accepted from a caller on modify (§ARC12.4.1 business rule 7a) — different content types carry different validation rules, so an item cannot be relabelled into a type its content was never checked against.
5. Adding a new `ContentType` member requires a code change; it can never be introduced by an end user or admin at runtime.

### DOM3.9 Series vs. Topic — to revisit *(formerly §3.9)*

`Series` and `Topic` currently have identical documented behaviour: both are grouping content items excluded from the feed (§DOM3.8 rules 1–3), and neither carries a rule that distinguishes one from the other. §DOM11 ("Topic and Feed Design") describes the grouping mechanism only in terms of `Topic`; `Series` was added to the enum without an equivalent section or without checking whether it duplicates `Topic`.

**Open question:** are `Series` and `Topic` the same concept under two names, or do they represent genuinely different groupings (e.g. an ordered, authored sequence vs. an unordered subject tag)? This needs a design decision — either give `Series` its own rules distinct from §DOM11, fold it into `Topic` and remove the member, or document the distinction explicitly (e.g. `Series` implies `Association.SortOrder`-based ordering per §APR9.6/§APR9.7.1 rule 4, `Topic` does not).

Until this is resolved, `Series` and `Topic` are numbered apart from the standalone content types (§DOM3.6) as a placeholder, not as a statement that the design question is settled.

## DOM4. Association Design *(formerly §4)*

### DOM4.1 Purpose *(formerly §4.1)*

`Association` is the generic link between **two entities**. Both endpoints are generic and symmetric: neither is hard-wired to a `ContentItem`.

It supports:

1. Tags
2. Reactions
3. Comments
4. Bible References
5. Links
6. Attachments
7. Child content items
8. Topic and series membership
9. Related content
10. Related Bible references — a `BibleReference` to `BibleReference` pair, which the earlier one-sided shape could not express at all

**There is no `Kind` and no `SourceEndpoint`.** The `(EntityType, ContentType)` pair on each endpoint already carries the meaning, and direction falls out of the asymmetry — a `Series` paired with a `Story` is always container-to-member, because the reverse is not a thing that exists. A separate discriminator would be a second source of truth for something the endpoints already say, and two sources of truth for one fact eventually disagree.

**One narrow, structurally-constrained exception exists: `Purpose` (§DOM4.9).** The no-discriminator rule rests on the premise that the endpoint pair carries the meaning — and for an `Attachment` endpoint the premise fails: `ContentItem` ↔ `Attachment` could equally mean header image, gallery member or downloadable file, and nothing on either endpoint can say which. `Purpose` names that slot. It is permitted **only** when an endpoint is an `Attachment` and forbidden otherwise, so it cannot grow into a general `Kind`: everywhere the pair is self-describing, a discriminator stays refused for the reason above.

### DOM4.2 Endpoint Shape *(formerly §4.2)*

Each endpoint carries the same six fields:

| Field | Purpose | Owned by |
| --- | --- | --- |
| `Entity{A,B}Type` | The `EntityType` of the endpoint. | caller, create-only |
| `Entity{A,B}KeyId` | The specific row — the version, for a versioned entity type. | caller, create-only |
| `Entity{A,B}GroupId` | The version group. Equal to `KeyId` when the entity type is not versioned, so every endpoint has a group id and one set of rules covers both kinds. | caller for a versioned type; derived otherwise; create-only |
| `Entity{A,B}Scope` | Whether the association follows the endpoint across versions. | derived (§DOM4.5); the only endpoint field that may change after creation |
| `Entity{A,B}EffectiveId` | `GroupId` under `AllVersions`, `KeyId` under `ThisVersionOnly`. | the database (§DOM4.6) |
| `Entity{A,B}ContentType` | The endpoint's `ContentType`, denormalised so authorization composes from the row alone (§SEC18.6). Null unless the type is `ContentItem`. | derived from the resolved endpoint, never caller-supplied |

Plus `UserId`, set only where the association is personal rather than editorial — today a `Reaction` endpoint. Null means editorial.

`Association` also implements `ISortOrder` (§DOM11.7) and `IConfidence` (§APR9.7.1 rule 5), and carries `Purpose` and `IsDefault` for purposeful attachment placements (§DOM4.9) — row-level fields like `UserId` and `SortOrder`, not part of either endpoint block.

### DOM4.3 Scope Rules *(formerly §4.3)*

| Scope | Meaning | Effective id |
| --- | --- | --- |
| `AllVersions` | The association follows the endpoint's whole version group — a tag on a story survives the story being amended. | `GroupId` |
| `ThisVersionOnly` | The association applies to one specific row. | `KeyId` |

### DOM4.4 Canonical Ordering *(formerly §4.4)*

The two endpoints are stored in a fixed order, A before B, computed on add. One row therefore serves both endpoints' lists, and "is X linked to Y" is one lookup rather than two.

A is the endpoint with the lower `(EntityType name, GroupId)` tuple; B is the other.

1. **Order on the enum name, not its numeric value**, using `string.CompareOrdinal`. The name is what SQL stores and what the §DOM4.4 check constraint compares. A rename then breaks loudly at the constraint; a renumber would silently reorder existing rows.
2. **Order on `GroupId`, not the effective id.** `GroupId` never changes, so a scope toggle can never force A and B to swap columns — which would otherwise turn a set-scope operation into a repoint.
3. **Guid comparison must use SQL Server's ordering, not .NET's.** SQL Server orders `uniqueidentifier` by bytes 10–15 first; .NET compares the leading `_a`/`_b`/`_c` fields as integers. The two disagree on most pairs, so `Guid.CompareTo` would produce an order the database's own canonical-order constraint rejects. Use `new SqlGuid(a).CompareTo(new SqlGuid(b))`.
4. **Normalisation runs inside `DoAddAssociationAsync`, before the storage call** — not in the public method and not in an orchestration. `Association-Adding` is a public event address whose substrate handler enters `DoAdd` directly, so anything layered above it is bypassed.

### DOM4.5 Derived and Pinned Endpoint Fields *(formerly §4.5)*

1. `Scope` is **derived, never accepted from a caller**: a non-versioned entity type resolves to `ThisVersionOnly` (it has exactly one row, so `AllVersions` would be a distinction without a difference); a versioned one defaults to `AllVersions`. The publication model comes from the §APR7.5.1 lookup — **never** from probing the entity for `IVersion` at runtime, which this repository has already proved unreliable twice.
2. `GroupId` is derived as `KeyId` for a non-versioned endpoint.
3. `ContentType` is derived from the resolved endpoint and never caller-supplied — it is an authorization input, so a caller who could set it could claim authority over a content type they hold no role for. Resolving it requires reading the endpoint row, which is an orchestration read; the foundation enforces the structural half of the rule (a value is permitted only on a `ContentItem` endpoint) and leaves a null endpoint costing the caller only the narrow role tier.
4. **Reclassification is forbidden.** `Type`, `KeyId` and `GroupId` are pinned against storage on every modify. Repointing an association is indistinguishable from deleting one link and creating another — except that it carries the original's approval state and review history across to a pair nobody reviewed.
5. The two endpoints must differ: `EntityAGroupId != EntityBGroupId`. Because a non-versioned endpoint's group id is its key id, this one rule covers an entity associated with itself, two versions of the same entity, and a tag paired with itself.

### DOM4.6 Effective Id *(formerly §4.6)*

`Entity{A,B}EffectiveId` is a `PERSISTED` computed column — `CASE WHEN Scope = 'AllVersions' THEN GroupId ELSE KeyId END` — read-only to application code. It earns its keep twice:

1. **It is the read predicate.** Every tag panel and related-passage panel asks "associations for this entity". Without the column that is an `OR` across `KeyId`/`GroupId` plus two scope tests per side; with it, one seekable comparison on the query that runs on every page render.
2. **It makes uniqueness a database guarantee.** Two `AllVersions` rows for the same group differing only in `KeyId` mean the same thing; over the raw columns they are distinct rows, and the effective id collapses them. This matters because foundation services are reachable through public event addresses and cannot assume an orchestration's retrieve-or-add ran first. `UX_Associations_Pair` is the unique index over it, filtered on `IsDeleted = 0`, keyed on both endpoints' type and effective id with `UserId` last — nullable, so one index means "one per user" when set and "one globally" when null. It is paired with `CK_Association_CanonicalOrder`, without which the same pair written the other way round is a different key and the duplicate lands.

   `Purpose` (§DOM4.9 — designed, not built) joins the key: `(EntityAType, EntityAEffectiveId, EntityBType, EntityBEffectiveId, UserId, Purpose)`, so the same pair may exist once per purpose — `UserId` keeps its position and its null-collapse role, with `Purpose` extending the key behind it. The index remains **deduplication**, not selection — it answers "has this exact statement been made before", while §DOM4.9 rule 5 answers "which candidate renders". Existing rows all carry `Purpose = NULL`, which the unique index treats as a value, so per-pair semantics on non-attachment associations are unchanged.

### DOM4.7 Associated Entity Types *(formerly §4.7)*

The supported entity types on either endpoint are defined by `EntityType`.

| EntityType | Purpose |
| --- | --- |
| `ContentItem` | Related content, topic and series children, parent/child links. |
| `Association` | Allows association records themselves to be approved. |
| `Tag` | Categorisation and labelling. |
| `Reaction` | Reactions such as love, like, celebrate. |
| `BibleReference` | Scripture references, including reference-to-reference pairs. |
| `Comment` | Comments on content. |
| `Link` | External or internal links. |
| `Attachment` | Files or binary resources. |

### DOM4.8 Association Approval *(formerly §4.8)*

Associations are themselves subject to approval.

This means that even if a `Tag`, `Comment`, `BibleReference`, or `Link` is approved as an entity, the association between that entity and a `ContentItem` can still require its own approval.

Example:

1. A tag named `Faith` may already be approved.
2. A user associates `Faith` with a story.
3. The association can require approval based on the effective `ApprovalSetting` for `EntityType.Association` — see §APR8.4. This is **not** a `ContentItemSetting` concern (§DOM6.1).
4. The tag becomes visible on the story only when both the tag and association are visible.

**Associations hosted on something other than a content item.** Because associations are symmetric, either endpoint may be any entity type, so a `BibleReference` ↔ `Tag` or `BibleReference` ↔ `BibleReference` association has no `ContentItem` to resolve settings from.

`ContentItemSetting` is not generalised to cover that. It stays scoped to content items (§DOM6.1), and each host entity type gets its own settings entity instead — `BibleReferenceSetting` (§DOM6.9) for the reference page. An association resolves the allowed/show switches per endpoint, from that endpoint's own settings entity, and is permitted only when both ends allow it (§DOM6.10).

Approval is unaffected either way: `ApprovalSetting` is keyed on `(EntityType, ContentType, IsPersonal)` (§APR8.4) and needs no host at all. A personal association — one whose `UserId` is set (§DOM4.2) — resolves the `(Association, IsPersonal = TRUE)` tier, which is how a user's own reaction can be exempt from review while an editorial placement is not.

### DOM4.9 Purposeful Placements — Purpose and IsDefault *(formerly §4.9)*

**Status: designed, not built** (agreed 2026-08-17). Two row-level fields and one narrow operation, so an attachment can be attributed to a host *for a stated reason* — the header image of a story, the verse image of a Bible reference — without reintroducing the general discriminator §DOM4.1 refuses.

| Field | Shape | Ownership |
| --- | --- | --- |
| `Purpose` | Nullable enum, persisted as a string via `HasConversion<string>()`, like the association's string-converted enum columns — `EntityAType` / `EntityBType`, the two `Scope`s and the two `ContentType`s — rather than its `int`-persisted `ApprovalStatus` (§DOM3.7). Members are append-only: `Header = 0`, `Verse = 1`, `Gallery = 2` *(reserved)*. | Caller-chosen on add, then pinned against storage like the endpoints — re-purposing a row is remove + add, for the §DOM4.5 rule 4 reason. |
| `IsDefault` | `bit NOT NULL DEFAULT 0`. Marks the preferred row among same-purpose candidates. | Refused on add; written only by the set-default operation (rule 4); pinned on the general modify. |

Rules:

1. **`Purpose` is mandatory when an endpoint is an `Attachment`, and forbidden when none is.** Enforced twice: `CK_Association_PurposeMatchesAttachmentEndpoint` — `(Purpose IS NULL AND EntityAType <> 'Attachment' AND EntityBType <> 'Attachment') OR (Purpose IS NOT NULL AND (EntityAType = 'Attachment' OR EntityBType = 'Attachment'))` — and the same rule in foundation validation with a typed exception. Every attachment association must say *why* the file is attached; a future purpose-less attachment (a downloadable file, say) is a new enum member such as `Download`, never a null.
2. **`IsDefault` requires a `Purpose`** — `CK_Association_DefaultRequiresPurpose`: `IsDefault = 0 OR Purpose IS NOT NULL`. A default with no slot to be the default *of* is meaningless.
3. **`Purpose` joins `UX_Associations_Pair` and both retrieve-or-add probes** (§DOM4.6). The pair index is deduplication — so the same image may be a header candidate *and* a gallery member of one host as two rows, and a header add cannot false-positive against an existing gallery row of the same pair.
4. **`SetAssociationDefaultAsync` is the only writer of `IsDefault`.** It follows the set-scope/set-confidence shape (§SEC14.7): it clears same-host, same-purpose siblings and then flags the target within one save — ordered so the rule 6 index never sees two flagged rows mid-flight — and publishes `Association-DefaultSet` on its own address. It **refuses a target that is not `Approved`** (or is deleted): only a vetted candidate can be promoted, so the default is always a member of the rendered set, never a hidden intention.

   **Who may call it is not yet ruled.** The nearest precedents disagree: `Sort` (also presentation) admits the owner and `Administrators` — but the row-local owner is the association's `CreatedBy`, who for a suggested candidate is not the host content's owner, and resolving the host's owner is not row-local (§SEC14.7 posture A′) — while set-confidence and set-scope admit `Publishers` and `Administrators`. Until ruled, the conservative reading is `Administrators`, the one caller every precedent admits; whether the `Publishers` tier or the owner joins it is the open half of the ruling.
5. **Selection is a resolution rule, not a constraint.** Any number of same-purpose candidates may exist per host. The rendered attachment for a (host, purpose) slot is chosen among **visible candidates** — §SEC14.3's association-visibility composite, which is association `Approved` + published + not deleted, both endpoints visible under their own §SEC14.1 rule (so the attachment group must hold a published, non-deleted version), and the host's effective settings permitting display (§DOM6.10 — `ShowAttachments` for a content item) — by `ORDER BY IsDefault DESC, PublishDate ASC, CreatedWhen ASC, Id ASC`, take 1: the default wins, otherwise the first approved candidate, with the ordering tail making "first" deterministic on every read surface. **`IsDefault` never overrides approval** — the flag orders candidates *within* the vetted set; a row outside it (`Draft`, `Submitted`, `Rejected`, unpublished, or deleted) does not exist to the resolver, flagged or not. A flagged row that later leaves the vetted set — an administrator status override, a takedown of the image — simply stops being a candidate, and the fallback covers the gap without an edit.
6. **At most one default per (host, purpose) slot** — `UX_Associations_DefaultPurpose`, unique over `(EntityBType, EntityBEffectiveId, Purpose)` filtered `WHERE IsDefault = 1 AND IsDeleted = 0`. Keying the host on the B side works because canonical ordering (§DOM4.4) sorts on the enum *name*, and `"Attachment"` precedes every other resolvable endpoint name ordinally — so the attachment lands on A and the host on B. The one ordinal exception is `Association` itself, which sorts before `Attachment`; an `Association` ↔ `Attachment` pair is not a resolvable shape today and must stay refused while this index keys the host on B. Note the level: the constraint bites per **(host, purpose)** — per (pair, purpose) it would be vacuous, since rule 3 already permits only one row there. What it forbids is two *different* images both flagged as the default header of one item.
7. **Scope needs nothing new.** §DOM4.5 rule 1's defaults are correct here: `AllVersions` on both sides for `ContentItem` ↔ `Attachment` means one candidate set per content group, resolving to the attachment group's newest published bytes (§DOM5.6.4); a non-versioned host such as `BibleReference` derives `ThisVersionOnly` as always.
8. **The orchestration add flow threads `Purpose` through** — the add and both probes match on it — and refuses a caller-supplied `IsDefault`. The `Attachment` arm of endpoint resolution is unblocked by the `AttachmentService` of §ARC12.3 entry 12; until that exists the arm keeps throwing, exactly as today.
9. **Approval of the attachment itself derives from the host** — §DOM5.6.5. Nothing here changes association approval (§DOM4.8): a purposeful association is approvable like any other.

## DOM5. Supporting Content Entities *(formerly §5)*

### DOM5.1 Tag *(formerly §5.1)*

`Tag` represents a categorisation label.

**The `GroupId` / `Version` / `IsLatestVersion` rows below are not implemented and never have been.** `Tag` carries `IApproval` only, `EntityTypeVersioning` declares it Single-Row (§APR7.5.1), and `IsLatestVersion` does not exist on any entity any more (§DOM3.4.1). The rows are left standing because §APR7.5.1 rule 1 and §ARC12.3.1 both cite this table as their worked example of documentation drift.

| Property | Purpose |
| --- | --- |
| `Id` | Unique tag identifier. |
| `Name` | Tag name. |
| `GroupId` | Groups all versions of this tag record together. Populated on creation and shared across all versions. |
| `Version` | Version number of this tag record, defaults to 1. |
| `IsLatestVersion` | Identifies the latest version of this tag record. |
| `PublishDate` | Optional date/time from which this tag becomes visible. |
| `IsPublished` | Identifies whether the current version of this tag is published. |
| `ApprovalStatus` | Denormalized approval state (`Draft`, `Submitted`, `Approved`, `Rejected`). |
| `IsDeleted` | Soft-delete flag. When `true` the tag is excluded from all public visibility. |
| `CreatedBy` | User who created the tag. |
| `CreatedWhen` | Creation timestamp. |
| `UpdatedBy` | User who last updated the tag. |
| `UpdatedWhen` | Last update timestamp. |
| `DeletedBy` | User who deleted the item. |
| `DeletedWhen` | Deletion timestamp. |
| `DeletionReason` | Reason for deletion. |

### DOM5.2 Reaction *(formerly §5.2)*

`Reaction` represents a reusable reaction definition.

**As with `Tag` (§DOM5.1), the `GroupId` / `Version` / `IsLatestVersion` rows below are not implemented and never have been.**

| Property | Purpose |
| --- | --- |
| `Id` | Unique reaction identifier. |
| `Name` | Reaction name. |
| `UnicodeEmoji` | Emoji representation. |
| `GroupId` | Groups all versions of this reaction record together. Populated on creation and shared across all versions. |
| `Version` | Version number of this reaction record, defaults to 1. |
| `IsLatestVersion` | Identifies the latest version of this reaction record. |
| `PublishDate` | Optional date/time from which this reaction becomes visible. |
| `IsPublished` | Identifies whether the current version of this reaction is published. |
| `ApprovalStatus` | Denormalized approval state (`Draft`, `Submitted`, `Approved`, `Rejected`). |
| `IsDeleted` | Soft-delete flag. When `true` the reaction is excluded from all public visibility. |
| `CreatedBy` | User who created the reaction. |
| `CreatedWhen` | Creation timestamp. |
| `UpdatedBy` | User who last updated the reaction. |
| `UpdatedWhen` | Last update timestamp. |
| `DeletedBy` | User who deleted the item. |
| `DeletedWhen` | Deletion timestamp. |
| `DeletionReason` | Reason for deletion. |

### DOM5.3 Comment *(formerly §5.3)*

`Comment` represents user or reviewer visible discussion attached to content through `Association`.

| Property | Purpose |
| --- | --- |
| `Id` | Unique comment identifier. |
| `Content` | Comment body text. |
| `CreatedBy` | User who created the comment. |
| `CreatedWhen` | Creation timestamp. |
| `UpdatedBy` | User who last updated the comment. |
| `UpdatedWhen` | Last update timestamp. |
| `DeletedBy` | User who deleted the item. |
| `DeletedWhen` | Deletion timestamp. |
| `DeletionReason` | Reason for deletion. |

### DOM5.4 BibleReference *(formerly §5.4)*

`BibleReference` represents scripture references associated with content.

| Property | Purpose |
| --- | --- |
| `Id` | Unique Bible reference identifier. |
| `USFM` | Canonical passage key including translation, such as `JHN.3.16.NIV`. Unique across non-deleted rows and immutable after creation (§APR7.5.1 rule 4, §ARC12.3.1 rule 2a). |
| `Reference` | Bible reference, such as `John 3:16`. |
| `Translation` | Bible translation, such as NIV, KJV, ESV. |
| `Scripture` | Optional scripture text. |
| `CreatedBy` | User who created the Bible reference. |
| `CreatedWhen` | Creation timestamp. |
| `UpdatedBy` | User who last updated the Bible reference. |
| `UpdatedWhen` | Last update timestamp. |
| `DeletedBy` | User who deleted the item. |
| `DeletedWhen` | Deletion timestamp. |
| `DeletionReason` | Reason for deletion. |

### DOM5.5 Link *(formerly §5.5)*

`Link` represents an external or internal link associated with content.

| Property | Purpose |
| --- | --- |
| `Id` | Unique link identifier. |
| `Name` | Display name. |
| `Url` | Target URL. |
| `LinkType` | Internal, external, video, article, source, etc. |
| `CreatedBy` | User who created the link. |
| `CreatedWhen` | Creation timestamp. |
| `UpdatedBy` | User who last updated the link. |
| `UpdatedWhen` | Last update timestamp. |
| `DeletedBy` | User who deleted the item. |
| `DeletedWhen` | Deletion timestamp. |
| `DeletionReason` | Reason for deletion. |

### DOM5.6 Attachment *(formerly §5.6)*

`Attachment` represents a file or binary resource, attributed to hosts through `Association` (§DOM4.9 for purposeful placements) or referenced inline from content bodies (§DOM5.6.6).

| Property | Purpose |
| --- | --- |
| `Id` | Unique attachment identifier. |
| `Name` | Display name. |
| `BlobUri` | Storage location (§DOM5.6.1). Never exposed to any client. |
| `Hash` | SHA-256 of the original uploaded bytes, for integrity and later deduplication (§DOM5.6.3 rule 5). |
| `MimeType` | Served `Content-Type`; recorded from the re-encoded result, never trusted from the caller. |
| `SizeInBytes` | Size of the stored bytes — quotas, sweep reporting, storage telemetry. |
| `Width` / `Height` | Pixel dimensions, nullable — `og:image:width/height` (§DOM19.8) and layout stability. |
| `AltText` | Optional accessibility text for purposeful placements (§DOM5.6.3 rule 6). |
| `CreatedBy` | User who created the attachment. |
| `CreatedWhen` | Creation timestamp. |
| `UpdatedBy` | User who last updated the attachment. |
| `UpdatedWhen` | Last update timestamp. |
| `DeletedBy` | User who deleted the item. |
| `DeletedWhen` | Deletion timestamp. |
| `DeletionReason` | Reason for deletion. |

`MimeType`, `SizeInBytes`, `Width`, `Height` and `AltText` are agreed design (2026-08-17), not columns yet. §DOM5.6.1–§DOM5.6.7 specify the storage, serving, upload, approval and lifecycle design that lands with them; all of it shares that status.

#### DOM5.6.1 Physical Storage *(formerly §5.6.1)*

1. Binaries live in **Azure Blob Storage**; SQL keeps metadata only. The `varbinary`-in-SQL alternative was rejected: the one precedent — profile avatars in the Identity database — is bounded at 256px, and content images are not, so database size, backup time and buffer-pool pressure would pay for the convenience forever. The entity was born with `BlobUri`; this section gives the column its producer.
2. One private container, `attachments`. Blob name = the attachment row's `Id` — one blob per **version row**, and a version's bytes never change, so blob names are immutable and cacheable (§DOM5.6.2 rule 3).
3. Access goes through an `IBlobStorageBroker` wrapping `Azure.Storage.Blobs` — the same wrap-the-external-library pattern as the existing image-processing broker. Operations: upload, download, delete, exists, and list-by-prefix (for the §DOM5.6.7 orphan sweep).
4. **Write order is blob first, row second**; deletion is the mirror — row first, blob second. An orphan blob is recoverable noise for the sweep; a row pointing at a deleted blob is a broken image.
5. `BlobUri` never leaves the server — no API response, no SAS URL handed to a browser. Everything serves through §DOM5.6.2, which keeps storage swappable and the visibility gate in one place.
6. Development uses **Azurite** on the same broker code path. Azure-side blob soft delete (14 days) is enabled as belt-and-braces against sweep defects.

#### DOM5.6.2 Serving — the Media Endpoint *(formerly §5.6.2)*

All attachment bytes are served by one endpoint: `GET /media/{attachmentId}` (§ARC17.6).

1. Load the attachment metadata, apply the visibility gate, stream from blob storage with `Content-Type = MimeType`.
2. **The gate** (§SEC14.7 posture A″): a published, approved, non-deleted attachment is public. A soft-deleted attachment is not found for **every** caller, including `Administrators` (§SEC14.5 rule 3). Anything else answers **not-found** per §SEC14.5 — never unauthorized — except to the uploader, the `Attachment` review roles, and reviewers or publishers of an entity whose row references the attachment, so a reviewer sees a draft's images in context. The gate does its own host check: it resolves the referencing host row — a §DOM4.9 placement or a §DOM5.6.6 inline body reference — and reads the host's state directly, never treating an association row's existence or approval as proof of host visibility (§SEC14.3's composite rule is implemented nowhere yet — §ARC12.5 entry 1 — and this gate must not repeat that gap).
3. Cache headers: a given `Id`'s bytes are immutable, so a published attachment serves `public, max-age=31536000, immutable`; a non-public one serves `private, no-store`. The consequence to accept deliberately: a takedown cannot recall bytes already cached downstream. It is bounded — ids are never reused, so a purged attachment's URL goes to not-found rather than to someone else's image — but a takedown that must be immediate needs a cache purge at the CDN, not a database write.
4. A CDN, when wanted, is a layer in front of `/media/*` — a configuration change, not a design change.

#### DOM5.6.3 Upload Rules *(formerly §5.6.3)*

1. Upload is an authenticated multipart endpoint (`POST /api/attachments`, §ARC17.6), following the existing profile-image endpoint's shape. The row is created at `Draft`: an attachment is never submitted by its uploader, because submission and approval derive from the host (§DOM5.6.5).
2. **Raster images only** — `jpeg`, `png`, `webp`, `gif`. **SVG is refused**, not sanitised: script-capable XML is a stored-XSS vector.
3. The declared content type is a hint; the decision is **magic-bytes sniffing**. Size cap 10 MB; minimum dimensions 200×200 (§DOM19.8 rule 6). A per-user rolling byte quota applies — an upload endpoint without one is a free file host.
4. **Every upload is re-encoded** through the image-processing broker before storage, and the original bytes are never stored. Re-encoding is the sanitiser: it destroys embedded payloads and strips EXIF metadata — including GPS coordinates, a real privacy concern for photos taken on phones.
5. `Hash` is the SHA-256 of the **original** uploaded bytes, computed before re-encode and recorded for integrity and later dedup (`IX_Attachments_Hash` already exists). Dedup itself is deferred — record now, collapse later.
6. `MimeType`, `SizeInBytes`, `Width` and `Height` are recorded from the re-encoded result. `AltText` is optional caller metadata; a header placement falls back to the host's `Title`, a verse image to its `Reference`, and inline images carry alt text in the markdown (§DOM5.6.6).

#### DOM5.6.4 Versioning and Replacement *(formerly §5.6.4)*

1. A stored binary is immutable. "Editing" an image is uploading a replacement: a **new version row pointing at a new blob** in the same `GroupId`, entering at `Draft` like any other versioned amendment (§DOM3.4, §APR7.5.1).
2. The group's **published** row is the vetted one, and it is the only row the §DOM4.9 resolution and the §DOM5.6.2 gate ever surface publicly. A `Draft` replacement is invisible until it passes approval; the previously approved image keeps serving meanwhile.
3. An association with `AllVersions` on its attachment endpoint follows the group, so a vetted replacement propagates to every host with no association write.
4. **A deleted version does not hold the group's published slot.** The filtered unique index `UX_Attachments_GroupId_IsPublished` filters on `[IsPublished] = 1 AND [IsDeleted] = 0`, so soft-deleting a published version frees the slot and a later version can still be approved and published. The `IsDeleted` term is not redundant against §APR9.7.6 rule 1's unpublish-on-remove mandate — that is the flow half, this is the defence-in-depth half, for any row that reaches the state another way. Nor does it launder a takedown in the sense §DOM4 closes for `Association`: the slot is a position within a group, not a name, so freeing it resurrects nothing — the deleted row stays deleted and unreadable to every read (§EVN4). The sibling latest-version index this rule once also named is gone: `Attachment` lost `IsLatestVersion` with `ContentItem` and `Link` (§DOM3.4.1), and the derived tip already excludes deleted rows.

    **The slot is not reserved while a row is deleted, so a restore must not assume it is still there.** A restored version comes back unpublished. §APR9.7.6 rule 1 clears `IsPublished` on the way out, and a row that reached the deleted state another way must be demoted on the way back in rather than re-entering the index against whatever now holds the slot. Approval status resumes as §APR9.7.6 says; publication does not resume with it.

    **`Link` and `ContentItem` carry the same term, from the same declaration.** All three published-slot indexes were written out by hand and all three drifted to the flag-only filter, so they are now configured through one shared declaration and a model test asserts the filter of every one — the case that matters most being a new versioned entity arriving without the index at all. For those two the term is defence in depth alone: their promote path already runs an unfiltered incumbent probe that clears a tombstone's flag before publishing (§ARC12.4.1, §ARC12.4.2). `GroupId` is the whole key in all three, since the filter pins `IsPublished` and a constant carries no selectivity. Index predicates are invisible to ordinary tests, and `has-pending-model-changes` detects a model the migrations do not match rather than a model that is wrong, so the guard is explicit at both ends: a model test on the declared filter, an integration test on the deployed one.

#### DOM5.6.5 Approval — Derived From the Host *(formerly §5.6.5)*

`Attachment` keeps its full `IApproval` surface like every governed entity, and its approve operation must call `IAccessBroker` (§APR8.6.1) like every other. What differs is *where approval comes from*: nobody reviews an image out of context, so an attachment's approval derives from the host that displays it.

| Trigger | Effect |
| --- | --- |
| A host completes approval + publication, and a §DOM4.9 purposeful association points at the attachment | The system approves and publishes the attachment, audited through the existing bypass mechanism — `IsApprovedByBypass = true`, reason `"Approved with host <EntityType>/<GroupId>"` — and the purposeful association is approved with it. |
| The approved host's body references `/media/{attachmentId}` inline (§DOM5.6.6) | The same derived approval, from a single scan of the approved version's body at approval time. |
| A replacement version is uploaded into a group whose host is already approved and published (§DOM5.6.4) | The host's approval is **not** re-opened — §DOM3.5 keeps attachment changes from invalidating the parent — so the replacement has no host approval to ride on and must be vetted on its own: a publisher approves the new attachment version, and §DOM5.6.4 rule 2's resolution picks it up. **Whether that is an explicit publisher review or an automatic re-derivation from the still-approved host was not decided at sign-off**; until it is, the explicit review is the safe reading, and the previously approved version keeps serving meanwhile. |
| A host is later unpublished or soft-deleted | **No automatic revocation** — another host may reference the attachment; reclaim is the §DOM5.6.7 sweep, which checks every reference. |

The derived flow does not waive the §SEC14.7 submission requirement — it satisfies it: an attachment (or purposeful association) still `Draft` is first submitted, then approved, in the same unit of work. Submit is already an owner-or-publisher act (§APR9.2) and the whole flow is publisher-gated, so no new permission is invented.

**The write is a bypass, and the slice no longer has to build a verb for it.** `IsApprovedByBypass` is derived from the access decision and an ordinary approve always clears it (§APR9.7.1 rule 3), so only a bypass can record what the derived flow does. This paragraph previously read that the path existed on `Association` alone and that the §ARC12.3 entry 12 slice would therefore add a second bypass *verb* for `Attachment`. That is superseded: the bypass folded into the widened approval transition and is available on every approvable entity (§APR9.7.1 rule 3, §ARC12.5.3 business rule 11), so `Attachment` inherits it with the transition #181 builds, and the derived flow requests it by setting the bypass pair on the payload. The bypass reason is supplied as on every other bypass — here composed by the flow rather than typed by a human, which is the one respect in which it differs.

**Open — whose identity performs the derived writes, and what happens when the bypass is refused.** Neither was ruled at sign-off, and both are real: a host publisher holding only a scoped role such as `ContentItem-Story-Publishers` is **not** in the `Attachment` `Publishers` tier, so either the derived writes run under a system actor that holds it or the flow requires the host's approver to hold it as well; and the bypass can be refused two ways — §SEC14.7's decision refuses outright when `DoNotAllowBypassingSettings = true` for `Attachment`, and the same decision re-applies HR-2 to any acting identity that is the attachment's or the association's own `CreatedBy` and is not an `Administrators` taking HR-2's bypass exception (the case §DOM4.9 rule 4 already contemplates, where the candidate was suggested by someone other than the host's owner). Either refusal leaves a vetted host published with its images non-public — survivable for a header (the §DOM4.9 rule 5 fallback picks another candidate, or §DOM19.8 rule 4's brand image) but not for an inline body image, which would simply be missing. Recorded here rather than assumed.

**Wiring.** The event-driven home for this is `ApprovalOrchestrationService` (§ARC12.5.3 responsibility 12), which does not exist yet. **Interim rule:** the publisher action that approves the host also derives the attachment approvals synchronously in the same flow — acceptable because both operations are publisher-gated. Moving the side-effect into the orchestration later is a refactor, not a redesign.

`AttachmentsAllowed` / `ShowAttachments` (§DOM6.5) remain the policy switches for whether a host may carry and display attachments; they gate the upload and association flows, not the `/media` read.

#### DOM5.6.6 Inline Images *(formerly §5.6.6)*

The GitHub model: paste an image into the content editor, get an attachment and a markdown reference.

1. The editor posts the pasted image to the upload endpoint (§DOM5.6.3) and receives the new attachment's id and media URL; it inserts `![alt](/media/{attachmentId})` at the cursor. Preview just renders the markdown — §DOM5.6.2 serves the draft image to its uploader.
2. The attachment is standalone, owned by the uploader. **No association is created**, deliberately: the body markdown already **is** the authoritative record of inline placement — a reconciled association table is a cache of it, and caches of a body drift — and every association is an approvable governance object (§DOM4.8), so a ten-image post would mint ten approval-bearing rows whose lifecycle means nothing to anyone.
3. Consequently, "referenced" means: `/media/{attachmentId}` appears in the `Content` of any **non-deleted `ContentItem` row — any version, any status**. Old versions keep their images renderable until hard-removed. The reference scan across the corpus is a sweep-time batch concern (§DOM5.6.7), not a write-path one — and it must stay one for the *unused* determination. The §DOM5.6.2 gate is the separate question: it cannot run a body scan per request, so its inline branch resolves against a **materialised reference index**, with the uploader and `Attachment`-role checks in front of it as a fast path rather than as a substitute — a host's reviewer is normally neither, and that reviewer is exactly who the §SEC14.7 posture A″ admit exists for. The index is written from the body in the same unit of work as the save, and rebuilt wholesale by the sweep. It is a derived cache the body always overrules — never a second source of truth beside it, and never an approvable object like an association (§DOM5.6.6 rule 2).
4. Associations are reserved for **purposeful placements** (§DOM4.9): few, meaningful, individually governed. The two mechanisms answer different questions and neither duplicates the other.
5. An abandoned upload — pasted, never saved anywhere — is reclaimed by the §DOM5.6.7 grace rules.

#### DOM5.6.7 Unused-Attachment Lifecycle *(formerly §5.6.7)*

Storage must not grow unbounded, and reclaim must not destroy anything vetted history still needs.

An attachment **group** is unused when all three hold:

1. No non-deleted association touches any version of the group.
2. No `/media/{attachmentId}` reference — any version's id — appears in the `Content` of any non-deleted `ContentItem` row (§DOM5.6.6 rule 3).
3. The newest version's `UpdatedWhen` is older than **30 days** — grace for paste-then-save gaps and slow drafts.

Process, as operations on `AttachmentProcessingService` (§ARC12.4 entry 3), run manually until background-job infrastructure exists. **The gate splits on whether the step deletes.** The report is a read and admits the `Publishers` tier or `Administrators`; every step that deletes is **`Administrators` only**. The sweep acts across other people's attachments, so posture A's remove branch — owner or `Administrators` (§APR9.7.1 rule 7, §SEC14.7 posture A.3) — has no owner to act as, and `Publishers` never removes at all; hard removal is `Administrators`-only everywhere (§SEC14.6 rule 3).

1. **Sweep (report)** — list candidates with sizes; dry-run by default.
2. **Sweep (execute)** — soft-delete candidates with `DeletionReason = "Unused attachment sweep"`. A soft-deleted attachment answers not-found at `/media` and remains restorable.
3. **Purge** — rows soft-deleted for **90+ days**: hard-delete the row, then the blob (§DOM5.6.1 rule 4's mirror order). The retention must exceed any content-restore window policy: condition 2 counts references only in non-deleted content rows, so an image referenced solely by a soft-deleted item reads as unused — and purge is the one step that cannot be undone if that item is later restored.
4. **Blob-orphan sweep** — blobs with no attachment row and age over 7 days are deleted; they are crash residue of the two-phase upload.

Storage telemetry — the sum of `SizeInBytes` by status — belongs on the admin dashboard, so "is storage growing?" never needs a database query.

## DOM6. ContentItemSetting Design *(formerly §6)*

### DOM6.1 Purpose *(formerly §6.1)*

`ContentItemSetting` exists primarily to **drive UI component visibility**, with a matching server-side gate so the UI cannot be bypassed.

Each facet has exactly two switches:

| Switch | Governs |
| --- | --- |
| `<Facet>Allowed` | Whether the *contribute* component is shown (e.g. the "Suggest a tag" box), **and** whether the association submit process will persist the record. When `false` the submit is rejected server-side, not merely hidden. |
| `Show<Facet>` | Whether the *display* component is shown (e.g. the tag panel). |

**`<Facet>AssociationsRequireApproval` is removed.** Whether an association requires approval is answered by `ApprovalSetting` and the approval workflow (§APR8.4), keyed on `(EntityType, ContentType, IsPersonal)`. Keeping a second copy here would create two sources of truth for one question and two places to look when an approval fails to fire. Six columns are dropped: the `RequireApproval` switch for each of Tags, Reactions, Links, Attachments, Comments and Bible References.

**Scope.** `ContentItemSetting` governs associations hosted on a `ContentItem` and nothing else. It is keyed on `ContentType` (required) with an optional `ContentItemId` override, both `ContentItem` concepts, and it is not generalised to other hosts. A host of another type gets its own settings entity following the same shape — see §DOM6.9 for `BibleReferenceSetting` and §DOM6.10 for how an association resolves the two.

### DOM6.2 Default and Override Behaviour *(formerly §6.2)*

`ContentItemSetting` can apply at two levels:

1. Content type default.
2. Specific content item override.

### DOM6.3 Default Rule *(formerly §6.3)*

If `ContentItemId` is null, the setting applies to all content items of the given content type.

Example:

1. All `Quote` items may allow tags.
2. All `Story` items may allow comments.
3. All `Topic` items may allow child content associations.

### DOM6.4 Override Rule *(formerly §6.4)*

If `ContentItemId` is supplied, the setting applies only to that specific content item and overrides the content type default.

### DOM6.5 Current Settings *(formerly §6.5)*

| Area | Settings |
| --- | --- |
| Tags | `TagsAllowed`, `ShowTags` |
| Reactions | `ReactionsAllowed`, `ShowReactions` |
| Links | `LinksAllowed`, `ShowLinks` |
| Attachments | `AttachmentsAllowed`, `ShowAttachments` |
| Comments | `CommentsAllowed`, `ShowComments` |
| Bible References | `BibleReferenceAllowed`, `ShowBibleReferences` |

### DOM6.6 ContentItemSetting Properties *(formerly §6.6)*

| Property | Purpose |
| --- | --- |
| `Id` | Unique content item setting identifier. |
| `ContentType` | Content type this setting applies to. |
| `ContentItemId` | Optional specific content item override. |
| `SortOrder` | Where this content type sits wherever the types are presented as a list — the contribute page's type picker above all. Lower first; defaults to `1000`, past every value the seed curates, so a row written without a considered order lands after the ordered types rather than in front of them. Must be `0` or greater — the foundation rejects a negative on both write paths. |
| `IsDeleted` | Soft-delete flag. When `true` the setting is excluded from active policy resolution. |
| `CreatedBy` | User who created the setting. |
| `CreatedWhen` | Creation timestamp. |
| `UpdatedBy` | User who last updated the setting. |
| `UpdatedWhen` | Last update timestamp. |
| `DeletedBy` | User who deleted the item. |
| `DeletedWhen` | Deletion timestamp. |
| `DeletionReason` | Reason for deletion. |

### DOM6.7 Recommended Settings Extension *(formerly §6.7)*

Recommended property:

```csharp
public bool LimitReactionsToLoveOnly { get; set; }
```

This supports favourite-style behaviour where only a love reaction should be allowed.

### DOM6.8 ContentItemSetting.ContentType typing — done *(formerly §6.8)*

`ContentItemSetting.ContentType` is typed `ContentType` (§DOM3.6), persisted as a string via `HasConversion<string>()` like every other `ContentType` column in the schema (§DOM3.7). There is no `Guid` involved on either side — `ContentType` is not an entity and never had an `Id`.

```csharp
public ContentType ContentType { get; set; }
```

### DOM6.9 BibleReferenceSetting *(formerly §6.9)*

`ContentItemSetting` is scoped to content items and nothing else. A Bible reference page hosts its own associations — suggested tags, related passages — and needs the equivalent switches, so it gets its own settings entity following the same shape.

| Property | Purpose |
| --- | --- |
| `Id` | Unique Bible reference setting identifier. |
| `BibleReferenceId` | Optional specific Bible reference override. Null means this row is the system-wide default. |
| `TagsAllowed` | Whether the "Suggest a tag" component renders, and whether the association submit persists. |
| `ShowTags` | Whether the tag panel renders. |
| `RelatedBibleReferencesAllowed` | Whether the "Suggest a Bible reference" component renders, and whether the association submit persists. |
| `ShowRelatedBibleReferences` | Whether the related-references panel renders. |
| `ReactionsAllowed` | Whether the reaction bar accepts a reaction, and whether the association submit persists. |
| `ShowReactions` | Whether the reaction bar renders. |
| `LimitReactionsToLoveOnly` | Restricts the passage to a single love reaction, as §DOM6.7 does for content items. |
| `IsDeleted` | Soft-delete flag. When `true` the setting is excluded from active policy resolution. |
| audit fields | As `ContentItemSetting`. |

Rules:

1. There is no type dimension. `BibleReference` has no equivalent of `ContentType`, so the default tier is a single system-wide row rather than one per type.
2. At most one default may exist: `UNIQUE(Id) WHERE BibleReferenceId IS NULL` semantics — one row with a null `BibleReferenceId`.
3. At most one override per reference: `UNIQUE(BibleReferenceId) WHERE BibleReferenceId IS NOT NULL`.
4. An override takes full precedence over the default; the tiers are not merged, matching §DOM6.4.
5. `BibleReference` is a Single-Row entity (§APR7.5.1), so the override keys on the row identifier directly with no version or group ambiguity.
6. As with `ContentItemSetting`, these switches never answer *whether approval is required* — that is `ApprovalSetting` (§APR8.4).

7. The reaction switches mirror `ContentItemSetting` exactly, so the reaction bar on a passage is configurable the same way it is on a story.

### DOM6.10 Resolving Settings for an Association *(formerly §6.10)*

An association has two endpoints, so the settings entity that governs it is resolved from the **host** entity type of each end:

| Host endpoint type | Settings entity |
| --- | --- |
| `ContentItem` | `ContentItemSetting` |
| `BibleReference` | `BibleReferenceSetting` |

Rules:

1. The allowed/show switches are resolved per endpoint, from that endpoint's own settings entity.
2. Where both endpoints resolve a switch — a `BibleReference` ↔ `BibleReference` related-passage link resolves `RelatedBibleReferencesAllowed` on each end — the association is permitted only when **both** allow it. Denials union restrictively, matching the read-only role veto in §ARC16.6.
3. An endpoint type with no settings entity imposes no restriction. It cannot silently deny, and it cannot silently grant on another endpoint's behalf.
4. Each new entity type that becomes a *host* for associations needs its own settings entity under this pattern. Entity types that only ever appear as the far end of an association — `Tag`, `Reaction` — do not.

## DOM11. Topic and Feed Design *(formerly §11)*

### DOM11.1 Topic as Content *(formerly §11.1)*

`Topic` is a `ContentType` used to group related content.

A topic is a `ContentItem` whose `ContentType` is `Topic`.

Example:

1. Create a `ContentItem` with `ContentType = Topic`.
2. Title it `God's Love`.
3. Associate other content items with that topic through `Association`.
4. The associated content may be `Quote`, `Story`, `Testimony`, or any future publishable content type.

### DOM11.2 Topic Is Not a Feed Item *(formerly §11.2)*

A `Topic` must not appear directly in the feed.

A topic acts as:

1. A grouping container.
2. A landing page.
3. A subscription target.
4. A thematic collection.
5. A way to organise related content without introducing a separate database entity.

### DOM11.3 Feed as a Domain Projection *(formerly §11.3)*

The feed is not a database entity.

The feed is a domain projection of visible content ordered by publish date descending.

Conceptually:

```sql
SELECT *
FROM ContentItems
WHERE
    ContentType <> 'Topic'
    AND DeletedWhen IS NULL
    AND ApprovalStatus = 'Approved'
    AND IsPublished = 1
    AND (
        PublishDate IS NULL
        OR PublishDate <= SYSUTCDATETIME()
    )
ORDER BY PublishDate DESC, CreatedWhen DESC;
```

### DOM11.4 Topic Parent/Child Relationship *(formerly §11.4)*

Topics use `Association` for parent/child relationships.

A child item is associated to the topic by creating a `Association` where:

| Field | Value |
| --- | --- |
| `ContentItemId` or `GroupId` | The parent topic content item or topic group. |
| `EntityType` | `ContentItem` |
| `EntityId` | The child content item or child content item group. |
| `Scope` | Whether the association applies to one version or all versions. |
| `PublishDate` | Optional date/time from which the child association becomes visible. |

### DOM11.5 Topic Visibility *(formerly §11.5)*

A topic can have its own visibility as a landing page or subscription target, but it does not appear in the feed.

A topic page is visible only when:

1. The topic content item is not soft deleted.
2. The topic content item is approved.
3. The topic content item is published.
4. The topic `PublishDate` is null or has passed.

### DOM11.6 Topic Child Visibility *(formerly §11.6)*

A child item is visible under a topic only when:

1. The topic is visible.
2. The child content item is visible.
3. The `Association` between the topic and child is approved if approval is required.
4. The `Association.PublishDate` is null or has passed.
5. The effective `ContentItemSetting` allows the relationship or associated content to be shown.

### DOM11.7 Topic Ordering *(formerly §11.7)*

`Association` implements `ISortOrder` (§APR9.7.1 rule 4), carrying a nullable `int? SortOrder` written only by the sort operation.

Ordering is resolved as:

1. `SortOrder`, if supplied.
2. Association `PublishDate`, if supplied.
3. Child `PublishDate`, if supplied.
4. `CreatedWhen`.
5. `Id`, so the order is total and paging cannot skip or repeat a row.

`SortOrder` is the position of the association within the **containing** endpoint's list — the series a post belongs to. It is null where neither endpoint is a container, because canonical ordering means one row serves both endpoints' lists and a bare integer would then have no owner. Values are sparse rather than dense, so a move rewrites a single row; see §APR9.7.1 rule 4.

### DOM11.8 Future Topic Subscriptions *(formerly §11.8)*

Subscriptions should remain decoupled from the content model, similar to approvals.

A future subscription system may record:

1. Subscriber user id.
2. Target `EntityType`.
3. Target `EntityId`.
4. Preferred communication method.
5. Subscription status.
6. Last delivered content.
7. Delivery history.

A topic subscription means the user subscribes to a topic and receives associated child content according to subscription delivery rules.

Subscriptions should not control whether content is visible on the public UI.

## DOM19. Search Engine Optimisation *(formerly §19)*

### DOM19.1 Purpose *(formerly §19.1)*

Search engine optimisation (SEO) ensures that gospel content published through G2H is discoverable by search engines and social platforms, maximising the reach of the content.

### DOM19.2 ContentItem SEO Fields *(formerly §19.2)*

**Status: designed, not built** (revised 2026-08-17 — this replaces the earlier nine-field list). **Stored fields are write-time facts — one authored (`MetaDescription`), two derived and then frozen at the group's first publish (`Slug`, `ShortCode`); everything else is derived at render time.** Every derived value has exactly one source of truth already, and a stored copy of a derivable value is a future stale bug — the sharpest case being a stored `OgImageUrl`, which goes stale the moment the header image changes (§DOM19.8 rule 2). `MetaKeywords` is dropped outright: no engine has read it since roughly 2009, so storing it is pure liability.

Stored on `ContentItem`:

| Property | Purpose |
| --- | --- |
| `Slug` | URL-friendly identifier used in canonical URLs (§DOM19.3). `nvarchar(160)`. |
| `MetaDescription` | Author-editable description for `<meta name="description">` and social preview cards. `nvarchar(300)`, optional — render falls back to a trimmed excerpt of `Content`. |
| `ShortCode` | Share code behind `/s/{code}` (§DOM19.7). `nvarchar(16)`, null until the group first publishes. |

Derived at render time, never stored:

| Value | Derived from |
| --- | --- |
| `MetaTitle` / `OgTitle` | `Title` |
| `OgDescription` | `MetaDescription`, else the excerpt |
| `OgImageUrl` (+ `og:image:width/height`) | the header-image resolution (§DOM4.9 rule 5) → the media URL (§DOM5.6.2) — see §DOM19.8 |
| `CanonicalUrl` | the route: `https://{host}/{ContentType}/{Slug}` |
| JSON-LD | the typed projection per §DOM19.5; there is no `StructuredDataJson` column |

`Slug` and `ShortCode` are group-level facts stored on every version row: the version fork copies them forward, modify pins them once the group has published (§ARC12.4.1 rule 12), and the by-slug read resolves through the published row, so a slug lookup naturally returns the publicly visible version.

### DOM19.3 Slug Rules *(formerly §19.3)*

The following rules apply to `Slug`:

1. A slug must be URL-safe — lowercase letters, digits, and hyphens only.
2. A slug must be unique per content type across **published, non-deleted** rows — a filtered unique index on (`ContentType`, `Slug`) `WHERE IsPublished = 1 AND IsDeleted = 0`. The filter cannot be `IsDeleted = 0` alone: version forks legitimately share one slug within a group, so only a one-row-per-group predicate can host the uniqueness. The `IsDeleted` term is not redundant against §APR9.7.6 rule 1's unpublish-on-remove mandate: §DOM5.6.4 rule 4 records that the analogous group-slot indexes were all built on the flag alone and that no remove flow clears `IsPublished` today — so a new index must carry the term rather than inherit the trap they were built with. A taken-down group's slug therefore leaves the index and is not reserved: a later item may legitimately generate the same slug. Uniqueness across never-published groups is application-side, at generation time, over non-deleted rows.
3. A slug is always generated from `Title` — never accepted from a caller (§ARC12.4.1 rules 6 and 12). Generation: lowercase, ASCII-fold, non-alphanumerics to hyphens, collapse and trim; on collision, suffix `-2`, `-3`, and so on.
4. A slug must not change once any version of the group has been published, to protect inbound links.
5. If an approved content item is edited and a new version is created, the new version inherits the slug from the previous published version.
6. An unpublished group's slug is provisional: it re-derives when `Title` changes, and freezes at the group's first publish. It is derived either way — "provisional" describes its stability, not a caller-editable window.

### DOM19.4 API SEO Considerations *(formerly §19.4)*

The following API behaviour should be supported for SEO:

1. A `GET /api/content-items/by-slug/{contentType}/{slug}` endpoint should return the currently published version of a content item by slug and content type.
2. Content item API responses should include the stored SEO fields (§DOM19.2) and the derived head values, so a client renders `<head>` metadata without a second request.
3. The feed API response should include `Slug`, `MetaDescription`, and the resolved header-image media URL (§DOM4.9 rule 5, §DOM5.6.2).
4. Topic landing page responses should include SEO fields for the topic content item itself.
5. APIs should not expose draft or unpublished SEO fields to unauthenticated callers.
6. The public content route is `/{ContentType}/{Slug}`. The content-type segment is a closed enum, so it cannot collide with application routes — and slug uniqueness is per content type (§DOM19.3 rule 2), which is exactly the scope the route shape requires.

### DOM19.5 Structured Data Recommendations *(formerly §19.5)*

Recommended JSON-LD schema types for G2H content:

| Content Type | Recommended Schema |
| --- | --- |
| `Quote` | `Quotation` |
| `Story` | `Article` |
| `Testimony` | `Article` |
| `Topic` | `CollectionPage` |

Structured data is derived from the typed projection and injected by the crawler middleware (§DOM19.8); there is no stored JSON-LD column (§DOM19.2).

### DOM19.6 Sitemap and Indexing *(formerly §19.6)*

The following sitemap and indexing support should be considered:

1. A `/sitemap.xml` endpoint should list all published, non-deleted, non-topic content items with their slug-based canonical URLs.
2. A `/sitemap-topics.xml` endpoint should list all published, non-deleted topic content items.
3. Each sitemap entry should include `lastmod` derived from `UpdatedWhen`.
4. Soft-deleted or unapproved content must not appear in the sitemap.
5. A `robots.txt` endpoint should disallow indexing of draft, admin, and API routes, and point at the sitemaps through `Sitemap:` directives.

### DOM19.7 Short Links *(formerly §19.7)*

**Status: designed, not built** (agreed 2026-08-17). Self-hosted — an external shortener (bit.ly and kin) was rejected outright: every shared link would depend on a third party for its lifetime, and every click would leak to one.

1. `ShortCode` is base62 — `[0-9A-Za-z]{7}`, roughly 3.5 × 10¹² codes — generated from a CSPRNG at the **group's first publish**, the same moment the slug freezes — written by the foundation's approve transition (§APR9.7.1 rule 3), the only operation that runs at that moment; collision-checked against its unique index; immutable thereafter and copied across version forks like the slug (§DOM19.2).
2. The unique index is filtered `WHERE ShortCode IS NOT NULL AND IsPublished = 1 AND IsDeleted = 0` — the same one-row-per-group predicate as the slug index, for the same fork reason, and carrying the `IsDeleted` term for the same reason (§DOM19.3 rule 2). A taken-down item's short link answers not-found (its target fails §SEC14.1); its code leaves the index and could in principle be reissued, though at 3.5 × 10¹² CSPRNG codes an accidental reuse is negligible.
3. `GET /s/{code}` resolves the code and answers **301** to the canonical URL — permanent, so link equity consolidates on the canonical route. An unknown code, or one whose target is not visible, answers **404** per §SEC14.5. No Open Graph tags are needed at `/s/` — unfurlers follow the redirect and read the destination's head (§DOM19.8).
4. Share buttons compose real intents from the short link — the WhatsApp and Twitter/X share URLs — replacing the placeholder `href="#"` buttons.
5. A branded short **domain** is a DNS and host-binding decision layered on later; nothing in the schema or code changes. `/s/` works on the main host meanwhile.
6. A generic `ShortLink` entity (`EntityType` / `EntityId` / `Code`) was considered and deferred: `ContentItem` is today the only consumer, and extracting the column into a table should a second consumer appear is a mechanical migration.

### DOM19.8 Crawler Rendering — Head Injection *(formerly §19.8)*

**Status: designed, not built** (agreed 2026-08-17). The frontend is a client-side-rendered SPA, and crawlers and social unfurlers (Google, `facebookexternalhit`, WhatsApp, `Twitterbot`) do not execute JavaScript — a meta tag added client-side is never seen. Two alternatives were rejected: a full SSR migration is a platform rewrite to solve a meta-tag problem, and prerender-on-build cannot follow content that changes at approval time, not build time.

1. The WebApp host already owns the `index.html` fallback. A middleware intercepts requests matching `/{ContentType}/{Slug}` (§DOM19.4 rule 6), resolves the published item through §SEC14.1, and rewrites `<head>` before serving: `<title>`, the meta description, `og:type` / `og:title` / `og:description` / `og:url` / `og:image` (with width and height from the attachment metadata, §DOM5.6), `twitter:card = summary_large_image`, the canonical link, and the §DOM19.5 JSON-LD. The SPA hydrates and takes over navigation exactly as before.
2. **Nothing sets or stores the OG image — it is derived on every read**, through one chain: resolve the item's Header slot by §DOM4.9 rule 5 → follow the association's attachment endpoint to its group → take the group's published version row → emit its absolute media URL (§DOM5.6.2). Promote a different candidate, or publish a vetted replacement image, and the next crawl sees it; a stamped-at-approval URL would need re-stamping on the first and would 404 to crawlers on the second.
3. **Derived never means unvetted — approval gates every hop.** The §DOM4.9 resolver only sees approved candidates; only a published attachment version is ever emitted; `/media` independently answers not-found for anything unpublished; and the page itself only exists for hosts passing §SEC14.1.
4. An item with no resolvable header falls back to a **static site-brand OG image**, so shares are never imageless.
5. The resolution is one indexed top-1 query on a page render that already loads the item — not worth caching until profiling says otherwise.
6. `og:image` launches with the **stored full-size image** — the re-encoded upload of §DOM5.6.3 rule 4, at its uploaded dimensions — validated at least 200×200 on upload (WhatsApp's floor, §DOM5.6.3 rule 3), with 1200×630 recommended in the editor UI. A cached 1200×630 derivative through the image-processing broker is a deferred optimisation, deliberately not launch scope.

