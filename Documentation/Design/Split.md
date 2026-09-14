# The Design-Document Split — Rulings

Architect rulings for issue #481, "Split G2H Design.md Into Area-Scoped Documents".

This file settles **how** the split is done. It does not perform it and it is not
design: nothing here changes a rule about the system. It exists because #481 is a
move of roughly four thousand lines of load-bearing prose, and the failure mode
this repository has already paid for three times is a relocated rule going stale.
A move with no agreed target, numbering scheme or completeness gate is how that
happens again.

**Retire this file once the split has landed and the gates in §S6 are green.** Its
durable content by then is the map in §S2, and the map's job is over the moment
every file exists.

Measurements below were taken against `Documentation/G2H Design.md` at 4,036 lines,
21 top-level sections, and re-verified rather than accepted from the analyst pass
that proposed them. §S7 records what that pass got wrong. The §1.5 pointer this
ruling adds to that document takes it to 4,048; the §1 row in §S2 is the only line
count that pointer affects.

**Re-measured at the commit the split actually branches from, with #548 merged:
4,051 lines.** #548 rewrote §8.6.2.1 and took §8 from 409 lines to 412 — the only
row in §S2 it moves, and it takes `Approval.md`'s resulting size to ~1,104. Every
other count below stands. Re-measure rather than trusting this paragraph if you
branch from a later commit: a rulings file carrying a stale count is exactly the
drift this split exists to prevent.

---

## S1. Filenames, prefixes, and the citation form

### S1.1 The files

| File | Prefix | Owns |
| --- | --- | --- |
| `Documentation/G2H Design.md` | *(none)* | The index. §1 and the §10 stub stay in it — see §S3. |
| `Documentation/Design/Domain.md` | `DOM` | The entity model: content, associations, supporting entities, settings, topic and feed, SEO. |
| `Documentation/Design/Approval.md` | `APR` | The approval entity, settings, lifecycle, and AI content analysis. |
| `Documentation/Design/Architecture.md` | `ARC` | Layers, per-service responsibilities, API surface. |
| `Documentation/Design/Security.md` | `SEC` | Visibility, enforcement posture, authentication and authorisation. |
| `Documentation/Design/UI.md` | `UI` | React UI and component design. |
| `Documentation/Design/Events.md` | `EVN` | Event design. **Already exists.** |

**`UI.md`, capital I, is the user's ruling and is not open.** An earlier draft of
this file chose `Ui.md` because that was the spelling `DEVELOPERS.md` §5.4 and §8
already used illustratively; that reason is retired, and those illustrative paths
are now what must be corrected (§S3.1). The file was created under the old name
before the ruling, so carrying the rename on a case-insensitive filesystem takes
`git mv --force` or a two-step through a temporary name — a plain filesystem
rename is one git does not record.

**`APR` is a new prefix.** `Events.md`'s header paragraph reserves `ARC`, `DOM`,
`SEC` and `UI` and does not mention approval, because approval was expected to be
divided between domain and security. §S2 rules otherwise, so that paragraph in
`Events.md` must gain `APR` as part of the split. It is the one edit the split
makes to `Events.md`.

### S1.2 Numbering — prefix-preserving, not renumbered

A relocated heading keeps its number exactly and gains its file's prefix:

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
   maps to new `§<PREFIX>N.M` by a rule a script can apply and a script can verify
   (§S6). The document carries 213 numbered headings, 192 of them subsections,
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

### S1.3 The citation form

A citation is the bare prefixed number and nothing else:

```
§DOM4.2      §APR8.6.1      §ARC12.5      §SEC14.7      §UI20.6.1      §EVN18
```

- **No filename.** `Domain.md §4.2` — the form issue #498 used — is **retired**.
  A prefix already identifies the file uniquely, so a filename in a citation is a
  second thing that can be wrong, and a stale path in a code comment is worse than
  no path.
- **No bare dotted number** inside `Documentation/`. A `§8.6.1` with no prefix
  written after the split is a finding (§S6 gate G4). Pre-split occurrences in
  code are left alone (§S6).
- **Lettered forms** follow `Events.md`: where comments cite `§10.17(a)`, the
  heading annotation carries no letter, so the section body names the lettered
  forms explicitly. `§ARC12.1 rule 2` and similar rule-number citations are
  unaffected — the prefix goes on the section number only.

**#498 is closed and cannot be corrected in place.** Its `Domain.md §4.2` form is
superseded by this ruling. Per `CLAUDE.md`, the document on main outranks an issue,
so #498's form is stale intent and not an instruction. The split must post one
comment on #498 pointing at this ruling, because a closed issue is exactly the
artefact a future author reads without checking whether it still holds.

### S1.4 The `design:` label names

Not created by the split (§S5), but named here so #498 cannot invent a divergent
set: one label per file — `design: domain`, `design: approval`,
`design: architecture`, `design: security`, `design: ui`, `design: events`.

---

## S2. The allocation — all of §1 to §21

| Old | Title | Lines | Goes to | Becomes |
| --- | --- | ---: | --- | --- |
| §1 | Design Overview | 64 | `G2H Design.md` | `§1` — unchanged, unprefixed |
| §2 | Domain Model Overview | 30 | `Domain.md` | `§DOM2` |
| §3 | Content Design | 200 | `Domain.md` | `§DOM3` |
| §4 | Association Design | 132 | `Domain.md` | `§DOM4` |
| §5 | Supporting Content Entities | 218 | `Domain.md` | `§DOM5` |
| §6 | ContentItemSetting Design | 129 | `Domain.md` | `§DOM6` |
| §7 | Approval Design | 269 | `Approval.md` | `§APR7` |
| §8 | Approval Settings Design | 412 | `Approval.md` | `§APR8` |
| §9 | Approval Lifecycle | 340 | `Approval.md` | `§APR9` |
| §10 | Event Design | 21 | `G2H Design.md` | stub stays verbatim, points at `Events.md` |
| §11 | Topic and Feed Design | 117 | `Domain.md` | `§DOM11` |
| §12 | Component Architecture | 698 | `Architecture.md` | `§ARC12` |
| §13 | AI Content Analysis | 83 | `Approval.md` | `§APR13` |
| §14 | Visibility Rules | 275 | `Security.md` | `§SEC14` |
| §15 | Recommended Corrections | 48 | *retired — §S4.1* | — |
| §16 | Recommended Service Responsibilities | 226 | `Architecture.md` | `§ARC16` |
| §17 | Recommended API Design | 83 | `Architecture.md` | `§ARC17` |
| §18 | Authentication and Authorisation | 324 | `Security.md` | `§SEC18` |
| §19 | Search Engine Optimisation | 97 | `Domain.md` | `§DOM19` |
| §20 | UI / UX Design | 252 | `UI.md` | `§UI20` |
| §21 | Summary | 31 | *§21.1 retired, §21.2 to the index — §S4.2, §S4.3* | — |

Resulting sizes: `Domain.md` ~923, `Approval.md` ~1,104, `Architecture.md` ~1,007,
`Security.md` ~599, `UI.md` ~252. The index retains ~102 lines (§1 at 64, the §10
stub at 21, §21.2 at 17) plus its new map.

### S2.1 The straddlers — lives here, referenced there

Every entry below has a **single** home. Nothing is duplicated; the other file
carries a link, never a restatement.

**§7/§8/§9 — approval gets its own file, not a division.** Approval is one
lifecycle. Sending §8.6/§8.6.1 to `Security.md` would put the self-approval rules
in a different file from the thresholds, statuses and settings resolution they
operate on, and §8.6.1 is a table of enforcement sites keyed line by line to the
rules above it, and both are cited from code throughout.
`Security.md` §SEC14.6 and §SEC18.6 link to `§APR8.6` and `§APR8.6.1`;
`Architecture.md` §ARC16.4–§ARC16.7 link to `§APR9.7`.

**§13 AI Content Analysis — `Approval.md`, not `Domain.md`.** Every rule in it is
subordinate to the approval process: §13.2 is an approval rule, §13.4 rule 2 gates
approval through `BlockOnZeroApprovalScore`, §13.5 rule 5 opens an approval round
per suggestion, and §13.2's closing paragraph routes AI output into the round as an
`ApprovalComment` under the Berean identity of §8.6.2. What *is* domain is the
`IConfidence` field set on `Association`; that stays defined where it is defined
today and `§APR13` links to it rather than restating it.

**§14 Visibility — `Security.md`, whole.** §14.1–§14.4 read as domain projections
and §14.5–§14.7 as enforcement, but §14.6 rule 3 and the §14.7 posture tables cite
§14.1's predicate directly, and that predicate is what both halves rest on.
Splitting would put the rule in one file and its enforcement in another.
**`Domain.md` §DOM11 does not link to `§SEC14.1` and `§SEC14.2` — it restates
them**, and the split neither creates nor repairs that. Corrected in §S7 item 5,
which rules where the single definition lives and routes the de-duplication to
its own issue.

**§16 Service Responsibilities — `Architecture.md`, whole, including §16.7.x.**
§16.7 describes `ApprovalOrchestrationService` and reads approval-flavoured, but a
per-service responsibility list is architecture, and §16.7.1 is the **Florance
deviation register** that `architect.md` and `qa.md` both cite as this
repository's record of approved dependency-count exceptions. That register belongs
with the layer model in §ARC12.5, not with the approval rules it happens to
implement. `Approval.md` §APR9.7 links to `§ARC16.7`.

**§17 API — `Architecture.md`.** At 83 lines it does not justify a file, and its
endpoint tables are the exposer surface of §ARC12.6. §17.6 (media, share, crawler)
is cited by SEO: `§DOM19.7` and `§DOM19.8` link to `§ARC17.6`, and §ARC17.6 links
back.

**§19 SEO — `Domain.md`, whole.** Its load-bearing content is `ContentItem`
columns, filtered unique indexes and slug generation (§19.2, §19.3) — entity
design. §19.5 structured data and §19.8 crawler head injection are rendering
consequences of those stored fields rather than component design, so they travel
with the fields. **`UI.md` §UI20 does not link to `§DOM19.5` or `§DOM19.8`, and
no such link is to be added** — corrected in §S7 item 6.

**§12 — `Architecture.md`, whole**, including §12.9 Content Analysis Service (a
component, even though its subject matter is §APR13's) and §12.5, which
`CLAUDE.md` cites by number for the orchestration exception.

---

## S3. What `G2H Design.md` becomes

**It becomes the index, at its existing path, and is neither deleted nor renamed.**

It is named authoritative by `CLAUDE.md`, listed in `Glory2Him.Core.slnx`, cited by
all three agent files and `DEVELOPERS.md`, and carries roughly 3,550 inbound `§N.M`
citations across 714 files. Moving or deleting it invalidates all of that for no
gain. Keeping the path also salvages GitHub deep links: a URL anchored at a heading
that has moved now lands at the top of the file, and the first content in the file
is the map that says where the heading went — a one-hop recovery instead of a 404.

It keeps exactly three things, and §S4.3 adds a fourth:

1. **§1 Design Overview**, unchanged and unprefixed. It is front matter for the
   whole design, not an area, and leaving it in place means `§1.2` keeps resolving
   with no annotation and no move.
2. **The §10 stub, verbatim.** It already points at `Events.md`, and its second
   paragraph ("Resolving is not the same as being right") is a *ruling* about
   citation resolution, not navigation. Do not paraphrase it into the new map —
   rewriting it is how a rule goes stale.
3. **The map** — the §S2 table, one row per old section, each linking to its new
   file and prefixed number, plus a one-line description per area file as #481
   requires.

### S3.1 What else must change, and where

Targets below are named by **section heading and quoted phrase, never by line
number**. This list outlives several commits and every one of them shifts the
numbers: `architect.md`'s `## What you produce` paragraph grew by three lines in
this extraction alone, which silently moved four of the five targets originally
recorded here. A phrase moves with what it names.

| Artefact | Change |
| --- | --- |
| `Glory2Him.Core.slnx` | Add a `/Documentation/Design/` folder listing all six area files. Note `Events.md` is **already** missing from the solution file — a pre-existing gap the split closes. The UI entry added ahead of the §S1.1 ruling is spelled `Ui.md` and must be respelled `UI.md`. |
| `CLAUDE.md` | "`Documentation/G2H Design.md` on main is authoritative" becomes "is the index and entry point; the area files under `Documentation/Design/` are authoritative for their areas". In the non-negotiables, `§12.1 rule 2` becomes `§ARC12.1 rule 2` and `§12.5` becomes `§ARC12.5`. |
| `.claude/agents/architect.md` | The opening paragraph of `## What you produce` (where it writes — §S3.2 rules the wording to use while the split is in progress). Under `## Boundaries you enforce`, "In this repository that register is" and the file's single `§12.5` citation (the Florance register and `§12.5` — becomes `Architecture.md §ARC16.7.1` / `§ARC12.5`). Under `## How you work`, "on main is authoritative, together with" (what is authoritative). Under `## Hard rules`, the "Never edit a file outside `Documentation/`" bullet (the path-scoping paragraph, which describes the split as begun and should describe it as done). |
| `.claude/agents/qa.md` | Under `## What you check, in order`, the `§12.5` citation in the orchestration-dependency clause and "In this repository the register is" (the deviation register's location). Under `## Reviewing the issues before any code exists`, "The unit of review is the feature" (where the design is read). Under `## Hard rules`, "on main outranks the issue". |
| `.claude/agents/analyst.md` | Under `## How you work`, "on main is authoritative, together with". |
| `DEVELOPERS.md` | Every illustrative `Design/Ui.md` path respelled `Design/UI.md` (§S1.1); §5.3 "Where it writes, today" (no longer true); §6's "adopt as you touch sections" (see §S5); §12 "What does not exist yet" — three of its bullets (filenames not decided, no index, only `Events.md`) are discharged by the split and must be **deleted**, not amended. |
| The `UI.md` rename (§S1.1) | `Documentation/Design/Ui.md` -> `Documentation/Design/UI.md` by `git mv --force`, plus every path that spells it: `Glory2Him.Core.slnx`, `Documentation/G2H Design.md` (four links: §1.5, the map, the area list, and the §20 stub), `Documentation/Mockups/README.md`, `Tools/design-split-audit.sh`, and `DEVELOPERS.md`'s illustrative paths. |
| Issue #498 | One comment pointing at §S1.3. It is closed and cannot be edited. |

### S3.2 Where an architect writes while the split is in progress

`.claude/agents/architect.md`'s `## What you produce` names `Documentation/G2H
Design.md` "in the section that already owns the subject", carves out
`Documentation/Design/Events.md` for event design alone, and closes with
"Nothing else." That is already untrue — §20 is a pointer stub, so an architect
settling UI design writes into a signpost — and it becomes untrue again at each
of #552–#556. A wording that lists the extracted files by name has to be
rewritten five more times, and a stale instruction in an agent file is the drift
this split exists to stop.

**The instruction names no file. It names the map.** The form of words, good from
the first extraction to the last:

> Write into the file that owns the subject's area: the area file under
> `Documentation/Design/` where one exists for that area, and
> `Documentation/G2H Design.md` where one does not, in the section that already
> owns the subject. The map in `G2H Design.md` §1.5 says which of the two, one row
> per section. A section whose row points at an area file has moved, and what is
> left behind is a pointer stub — a signpost, not a home. Never write design into
> a stub.

Two consequences:

1. **It is edited once and not again at #552–#556.** Every extraction updates the
   map rows it moves, and the instruction reads off the map, so it stays true
   without being touched. This discharges the "where it writes" part of §S3.1's
   `architect.md` row for the whole split. The rest of that row — the Florance
   register, what is authoritative, the path-scoping paragraph — still lands with
   the file each part names.
2. **The path-scoping hard rule under `## Hard rules` is left alone for now.**
   It says the split "has begun" and that the scope "should narrow to those two
   paths as the rest of the split lands", which is true today and at every step
   up to #556, where §S3.1 already has it rewritten.

This rules the architect's **write target** only. The sentences that say what is
authoritative to *read* — `architect.md` and `analyst.md` under `## How you
work`, `qa.md` under `## Hard rules` — are stale today for the same reason, each
naming `Events.md` as the only area file, and §S3.1 schedules them. Whoever
edits them should use the same shape: name the files under
`Documentation/Design/` as authoritative for their areas and the map as what
says which areas those are, so they too survive the remaining extractions
unedited.

---

## S4. §15 Recommended Corrections and §21 Summary

Neither is design. Both are to-do lists that outlived their items, and relocating a
stale to-do list into an area file makes it look like a rule.

### S4.1 §15 — retired, item by item, each against named evidence

Retire the section; do **not** relocate it. But a to-do item is not always only a
to-do: several of these carry a live rule inside the recommendation, and dropping
the item would drop the rule. So for each, check the evidence, and where a rule is
found inside it, confirm that rule already stands in its owning section — and if it
does not, **move the rule** rather than dropping the item.

| Item | Evidence required before it is dropped |
| --- | --- |
| §15.1 Typographical (`ConentItemAssociation`) | A repo-wide grep for `ConentItem` returns zero, the `.drawio` included. |
| §15.2 Remove `ApprovalId` from approvable entities | No `ApprovalId` on `ContentItem` or `Association` in the model or in any migration. **And** its rules 2 and 4 — generic lookup by `EntityType`/`EntityId`, `ApprovalId` valid only on `ApprovalReview`/`ApprovalComment` — are found stated in `§APR7.4`. Those are live rules, not corrections. |
| §15.3 Add `Association` to `EntityType` — marked done | `EntityType.Association = 7` present in the enum. |
| §15.4 Add `Topic` content type | `ContentType.Topic` present in the enum, **and** the seed walk covers it, **and** the feed exclusion is stated in `§DOM11` / `§SEC14.2`. An unseeded type or role fails silently, so the seed check is not optional. |
| §15.5 `ContentItemSetting` type mismatch — marked done | `§DOM3.6` carries the enum-with-`HasConversion<string>()` substance. |

Anything whose evidence does not check out is **not** quietly kept in a corrections
list. It becomes an issue, and the heading that should own the rule gets it.

### S4.2 §21.1 Final Design Direction — retired

Five sentences, every one a restatement of a rule defined elsewhere. A restatement
in a second file is the drift mechanism #481's third non-negotiable exists to
prevent. Drop each sentence only once its defining section is confirmed: the
content model `§DOM3`; shared approval keyed by `EntityType`/`EntityId` `§APR7.4`;
`Association` as the generic relationship `§DOM4`; `Topic` as a `ContentItem`
`§DOM11.1`; the feed as a projection `§DOM11` with `§SEC14.2`. A sentence with no
defining section is an open question, not a deletion.

### S4.3 §21.2 Immediate Next Changes — relocated to the index

Ten items in dependency order, revised 2026-08-17, densely cross-referenced. It is
a live work plan, not a rule, so it belongs in no area file — a roadmap inside
`Domain.md` would be read as design. Move it verbatim into `G2H Design.md` under a
heading that says plainly that it is a roadmap, and state that the split verifies
none of its items. Auditing ten roadmap entries against the repository is real work
and it is not this issue's.

"Verbatim" here means what it means everywhere in this split: the prose is
unchanged, and its intra-document citations are reprefixed to their new homes
(§S6 gate G4).

---

## S5. #498's heading tags — deferred, explicitly

**The split applies no `(#N)` or `(needs issue)` tags.**

No heading in `Events.md` carries one today, the `design:` labels do not exist, and
tagging is not clerical: each tag requires finding the most recent issue that
authoritatively defined that section, across roughly three hundred headings. That
research roughly doubles the extraction and buries a pure move inside a diff nobody
can review for the thing that actually matters — whether every rule survived
intact.

`DEVELOPERS.md` §6 says to adopt the tags "as you touch sections". That is
corrected here for this one case: **relocating a heading is not touching it.** A
move changes no rule, and so answers no question about which issue defines it.

#498 remains the issue that introduces the tags, and it is easier after the split
rather than harder — the files it needs will exist. The split's only contribution
is naming the labels (§S1.4) so #498 does not invent a second set.

---

## S6. The inbound-citation surface — annotate, do not rewrite

**Confirmed: code citations are not rewritten.** Roughly 3,550 `§N.M` citations sit
across 714 files, 119 of them distinct and outside `Documentation/`. Rewriting them
is a 714-file diff in which a genuine design regression would be invisible, and it
would have to be redone for every future move. The `Events.md` precedent — the
literal old number left on the heading as a grep anchor — already works and costs
one annotation per heading.

Under §S1.2's prefix-preserving numbering the annotation is nearly redundant, since
`§APR8.6.1` contains `8.6.1` as a substring. Carry it anyway, and literally: it is
what makes a whole-word grep resolve, it is what an `Events.md` reader already
expects, and it is what survives if a later change *does* renumber.

### The gates

| | Gate |
| --- | --- |
| **G1** | Every relocated heading carries `*(formerly §N.M)*` with the literal old number. |
| **G2** | The set of heading numbers in `G2H Design.md` at the commit before the split equals the set of `(formerly §…)` annotations across `Documentation/Design/*.md`, plus the numbers deliberately retained in the index (§1, §10) and those deliberately retired (§15, §21). Any asymmetry is a dropped or an invented section. Produce this as a script diff and show it in the PR — this is the completeness proof #481 asks for, and a read-through is not a substitute for it. |
| **G3** | For every distinct `§N.M` citation appearing outside `Documentation/` before the split, a grep after the split finds the literal old number on **exactly one heading**. Not "somewhere" — on one heading. Two conditions make that anchor unique, and both must be stated because each defeats a naive grep: the grep is **right-anchored**, `§8\.6(?![0-9.])`, or `§8.6` matches the `§8.6.1` heading too and every parent number reports a false duplicate; and G4 must hold, since a bare old number left in prose is indistinguishable from the anchor. **§10 is out of G3's scope** — `Events.md` already carries its `(formerly §10.X)` anchors, and both the retained §10 stub and `Events.md`'s own prose repeat `§10.X` deliberately, so §10 fails a uniqueness check by design and is not a finding. |
| **G4** | Inside `Documentation/`, citations **are** rewritten to the prefixed form. A bare `§8.6.1` left in `Security.md` prose is a finding. The annotation serves code and closed issues, not the design's own prose. Two deliberate exemptions: the retained §10 stub, which is kept verbatim (§S3), and `Events.md`, which the split does not otherwise touch. |
| **G5** | `§12.4.7` is exempt and stays dangling. |
| **G6** | `§1.1.3` and `§534` are exempt and are not citations of this document. |

### The three exceptions, verified

Checked against the working tree rather than taken on trust:

1. **`§12.4.7` is genuinely dangling, and deliberately so.** One occurrence:
   `Websites/Glory2Him.WebApp/Controllers/Tags/TagsController.cs:36`, reading "the
   withdrawn `TagOrchestration` of the **old** §12.4.7 is not coming". §12.4 today
   has only §12.4.1 and §12.4.2 — §12.4.7 was removed long before this split was
   contemplated. The comment names it as historical and is not broken by being
   unresolvable. The split neither creates nor repairs it. *(Advisory, for whoever
   next edits that file: "the old §12.4.7" would read better as "§12.4.7, withdrawn
   and no longer in the document". Not the split's job.)*
2. **`§1.1.3` is not a citation of this document.** Sixteen occurrences across nine
   files — four orchestration services and five of their test files — every one
   attached to "no foundation exception leaks to a higher layer", The Standard's
   exception rule. `G2H Design.md` §1.1 is "Purpose" and has no numbered
   subsections at all, so it has no §1.1.3 to cite. *(A repository-wide grep
   returns twenty rather than sixteen; the other four are this file's own three
   mentions and one in `Tools/design-split-audit.sh`, none of which is a citation
   that has to resolve.)*
3. **`§534` is an issue number typed with a section sign.** One occurrence, at
   `Glory2Him.Core.Tests.Unit/Services/Orchestrations/AIReviewers/AIReviewerOrchestrationServiceTests.cs:205`,
   reading `(§534/#532)` — a pair of issue numbers, one of them mis-sigiled.

### What the gates do not cover

GitHub deep links anchored at moved headings
(`...G2H Design.md#86-self-approval-rules`) in old issues and PR comments. They
cannot be rewritten and are not tracked. The mitigation is §S3's: the file stays at
its path and the map is the first thing in it, so such a link degrades to one extra
hop rather than to nothing.

---

## S7. What this corrects in the proposed allocation

1. **§10 was unaccounted for.** It is a 21-line pointer stub and stays in the
   index; the residual is ~152 lines, not 120.
2. **§13 was placed in Domain.** It is approval (§S2.1).
3. **Approval was not a file.** #481 proposed five areas with approval
   "straddling" domain and security; §S2.1 rules it a sixth file with its own
   `APR` prefix, which obliges the one edit to `Events.md`'s reserved-prefix
   paragraph.
4. **Resulting sizes.** Domain 923 (not 1,015), Approval 1,104, Architecture 1,007,
   Security 599, UI 252.
5. **§DOM11 restates §SEC14 rather than linking to it, and the restatement has
   already diverged.** The §S2.1 sentence claiming §DOM11 links to `§SEC14.1` and
   `§SEC14.2` was never true of the prose. Measured at the commit #555 branches
   from, §11 carries two numbered citations, both `§9.7.1`, and no `§14` citation
   anywhere. What it has instead is a second copy of the rule: §11.3's `WHERE`
   clause is §SEC14.1's predicate term for term plus §SEC14.2's two feed rules and
   its ordering; §11.2's opening sentence is §SEC14.2 rule 2; §11.5 is §SEC14.4's
   topic-page list with §SEC14.1 inlined; and §11.6 is §SEC14.4's topic-children
   list with §SEC14.3 partly inlined. **This is duplication and not a coincidental
   resemblance between a read projection and an access rule**: two such rules could
   legitimately differ somewhere, and these cannot — §11.3 selects exactly the rows
   §SEC14.1 and §SEC14.2 admit, and a feed carrying a row that visibility hides
   would be a defect under either reading. **The drift has already fired.**
   §SEC14.3 and §SEC14.4 were rewritten for symmetric endpoints — §SEC14.3 rules 3
   and 4, its `Layer.` paragraph and its §DOM6.10 citation — and §11.6 was not; and
   §11.4's field table (`ContentItemId` or `GroupId`, `EntityType`, `EntityId`) is
   the retired one-sided association shape that §DOM4.2's six symmetric endpoint
   fields replaced.

   **Ruled: §SEC14 is the single definition of who may see what.** §DOM11 keeps
   what a topic *is* — §11.1, the association shape of §11.4, the ordering of §11.7
   and the subscriptions of §11.8 — and every visibility statement in it becomes a
   citation of §SEC14. The stale §11.4 table is repaired against §DOM4.2 in the
   same pass, being the same defect in the same section. One thing that work must
   settle rather than transcribe: §SEC14.2 orders the feed on two columns while
   §DOM11.7 rule 5 requires a total order so paging cannot skip or repeat a row,
   and the merged rule cannot have it both ways.

   **None of this happens at #555.** It predates the split, the verbatim gate is
   the assurance mechanism for a 923-line move and deleting prose voids it, and
   turning a restatement into a link is a design decision needing its own review.
   It is raised as its own issue on the #559 precedent. **No marker, tag or TODO is
   written into `Domain.md`** — this ruling and that issue are the record, and a
   note in the moved prose would be the fifth normalisation exception #555's
   criterion 2 refuses.
6. **`UI.md` §UI20 does not cite `§DOM19.5` or `§DOM19.8`, and is not to be made
   to.** Measured: `UI.md` carries one §DOM19 citation in the whole file,
   `§DOM19.7`, in the `ShareBar` row of §UI20.6, and it earns its place because
   §DOM19.7 rule 4 describes share buttons, which are a component. §DOM19.5 and
   §DOM19.8 have no counterpart there: §DOM19.5 is JSON-LD derived from the typed
   projection and injected by the middleware of §DOM19.8, and §DOM19.8 is host
   middleware rewriting `<head>` before `index.html` is served. Neither is a page,
   a component, a service or a broker, and those four are what §UI20 enumerates, so
   adding the citations means writing a sentence into §UI20 about something §UI20
   does not describe. The cross-file link those two sections need already exists
   and points the other way: §ARC17.6 tabulates `§DOM19.6`–`§DOM19.8` against the
   crawler, share and media endpoints. **The §S2.1 sentence was aspirational and is
   withdrawn rather than scheduled** — nothing is owed at #555 and no follow-up
   issue is raised. The §S8 bullet on whether `UI.md` should later absorb §DOM19.5
   and §DOM19.8 stands untouched, and citations added now would be work that bullet
   might delete.

## S8. Left open, deliberately

- **Whether `UI.md` should later absorb §DOM19.8 and §DOM19.5.** Ruled into
  `Domain.md` for now because the stored fields are the load-bearing part. If the
  crawler and JSON-LD sections grow with real rendering design, revisit — as a
  design decision, not a tidy-up.
- **§16.7.5's position between §16.7.2 and §16.7.3.** Preserved as-is. Fixing it is
  a renumber, and a renumber invalidates citations silently; it needs its own issue.
- **§15 and §21.1's per-item evidence checks** (§S4.1, §S4.2). This rules *how* to
  decide, not what was decided. Each check is a repository lookup the split
  performs; anything that fails becomes an issue.
- **Whether the split lands as one PR or six.** A sizing question for the analyst.
  Gate G2 is a whole-document check, so however it is staged, G2 runs against the
  final state rather than per file.
