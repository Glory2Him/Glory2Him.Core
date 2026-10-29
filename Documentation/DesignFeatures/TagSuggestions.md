# Tag suggestions
Epic: [INTENT.md](../../INTENT.md)
Inherits: §APR9.2, §APR9.7.1 rules 1 and 2, §ARC12.1 rules 1–3, §ARC12.2.1 rules 1, 3, 5 and 6, §ARC12.3.1 rules 2a, 5 and 5a, §ARC12.5 (*an orchestration holds brokers*, and the two-to-three rule), §ARC12.5.2 rule 6 (`Association-Adding`'s write flow), §ARC16.2.1, §ARC16.2.2, §ARC16.8.1, §ARC17.2 (shape versus served literal), §ARC17.4, §DOM4.4 rule 4, §DOM4.5, §DOM4.6, §EVN13 rule 3, §EVN18 rule 1, §SEC14.3, §SEC14.5, §SEC14.6, §SEC14.7 posture A rules 1 and 4 and posture A′ rule 1, RejectedEndpoints.md rules 1, 10 and 12
Mockups: none. The *Suggest a tag* box is built (`Design/UI/Components/AssociationPanel.TagAssociationPanel.md`); this feature writes what it sends.
Design task: #1356

A reader types a tag into a post's *Suggest a tag* box. This feature sends it once, by its name, and the server finds the tag the name names, or adds it, and pairs it with the post unless that pairing already exists. A rejected tag is never paired, and the answer does not say so. What becomes of the new rows afterwards is the approval process's.

## Problem

When this feature was designed (2026-10-28, `main` at `4a839256`), the pages answered a suggested tag with a *coming soon* notice (`UI/Pages/PostDetail.md §6 item 17`, `UI/Pages/MyPostDetail.md §6 item 18`). The tag panel raises `onAdd` with the text the reader typed (`Design/UI/Components/AssociationPanel.md` rule 3.1.15), and the only write that pairs a tag with a post, `POST /api/associations`, takes two endpoint ids (§ARC16.8.1). Nothing turned a name into a tag: `ITagService` had no read by name, its add took its `Id` from its caller, and every tag read is filtered by who asks, so a read the caller made could not say whether a name was already taken. And a name is held by at most one row, whatever that row's state (`IX_Tags_Name`, unique and unfiltered; §ARC12.3.1 rule 2a), so a name the caller cannot see the tag of could not be added either.

## The rulings

The product owner stated the requirement under #1352, before it was split:

> This is my expectation for both the tag and the bible reference components:
> 1. I submit a tag or a bible reference
> 2. this gets submitted via an api
> 3. the api does a read or add on the tag OR bible reference to get its Id
> 4. the tag / bible reference Id is then used to create an association if none exist already.
>
> The above is the submission process. The approval proces is separate. Both the tag / bible reference and the association will be subject to approval. That is separate to the submission process.

The rest of this section is the owner's rulings, word for word, each question as it was put where there was one. Each was recorded on #1352 or #1356 when it was made, save the seven of 2026-10-29, which were answered through the session's question prompt and are recorded here: five answered this design's open questions, and two, *Splitting the suggestion* and *Two readers pairing one tag at once*, answered questions QA's second round on design PR #1374 raised. *The box on a rejected post* names the Bible reference control too, so it is recorded on #1357 as well, and so is each other ruling of that day that #1357 asks whether a Bible reference shares.

**Where the submission lives, and what it refuses** (2026-10-26):

> It will be good if we can extend the association controller to accommodate two endpoints that can cater for tag and bible reference. The association orchestration can facilitate the process of reading/creating the tag or bible reference and then creating the association.
>
> Duplicate associations should not be allowed.
> If a tag or bible reference was rejected, then the association should not be allowed. I new tag OR bible reference MUST NOT be created since we have already ruled that the one submitted is unfit.

Of the two endpoints, this feature is the tag's. The Bible reference's is #1357's.

**Both deviations are signed off** (2026-10-26):

> Q: The design will put the submission on AssociationOrchestrationService, a seventh-dependency exception of its own, and the two new routes will return a status instead of the created row. Do you sign off both, each recorded in this feature's own document?
>
> A: "Sign off both" — the option read: "The planner records both as approved by you, with today's date."

The question's *"seventh-dependency exception"* means the service's existing seven entity services: the submission adds no eighth. Each is recorded in the user story document of the component it is a deviation of, as `Design/design.md` has a service's deviation recorded (*Record every deviation where it happens*): `Backend/Orchestrations/AssociationOrchestrationService.TagSuggestions.md` and `Backend/Controllers/AssociationsController.TagSuggestions.md`, *Deviations*.

**The routes answer `200` where the pair already existed** (2026-10-27):

> Q: The two new routes depart from the exposer rule that a POST returns 201 with the created row. You signed off returning a status instead of the row. Do you also sign off answering 200 where nothing was created, for example when the association already exists?
>
> A: "Yes, sign off 200" — the option read: "201 when something was created, 200 when the pair already existed. Both recorded as approved by you."

**A tag the reader cannot see** (2026-10-27):

> Q: A reader types a tag name that already belongs to a tag they can't see: another reader's Draft or Submitted tag, or an Approved tag that is unpublished or scheduled. Names are unique, so no new tag can be added. What happens?
>
> A: "Pair it (Recommended)" — the option read: "Pair the post with that tag and answer as if the reader had created it. The pairing shows once the tag is approved and published. The tag's creator can still rename or remove it."

**A soft-deleted tag** (2026-10-27), in the owner's own words, chosen over both options put:

> Q: A reader types a tag name held by a soft-deleted tag. The name stays reserved (the index ignores deletion), and a deleted tag can't be paired. What happens?
>
> A: "If a tag was soft deleted, it can be undeleted and will be subject to approval.  I think this block should only be on items that has been marked rejected.  Any new associations can be linked to this existing item.
>
> If if a tag or bible reference has been marked as rejected, it will block creation of a duplicate tag / bible reference.  It MUST also block the creation of any associations on a rejected item."

**Who triggers the undelete** (2026-10-27):

> Q: A submission names a soft-deleted tag and you ruled it is undeleted and goes back through approval. Today only administrators and publishers may recover a soft-deleted item (#1326, ruled 2026-10-22). Who triggers this undelete, and from what?
>
> A: "Any deletion" — the option read: "The submission undeletes it on the reader's behalf, whoever deleted it (creator or moderator takedown), and it goes back to approval."

The undelete itself, and which routes reach it, are #1362's to design (#1362, *How far it reaches*, *Which endpoint types*, and its open questions 1 and 2, answered 2026-10-28). This feature reaches it for the soft-deleted tag its read by name finds (rule 4).

**A pairing that was itself rejected** (2026-10-27), in the owner's own words:

> Q: The pairing of this post with this tag was itself rejected before (the tag is fine). A reader submits it again. What happens?
>
> A: "Since this will lead to a duplicate, the db will not allow its creation. In terms of the UI we can just thank the user for the contribution with stating that this association has already been  suggested by another user and is under review."

**The reader's own pending pairing** (2026-10-27):

> Q: A reader resubmits a pairing they themselves suggested, which is still pending. "Already suggested by another user" would be untrue. What should the panel say?
>
> A: "Say it's theirs" — the option read: "Thank them and say they already suggested it and it is under review. Others still see the 'another user' wording."

**What a rejected item is** (2026-10-27), recorded on #1352 in the owner's own words: *"A rejected item is an item where its approval status is REJECTED"*. So a rejected post refuses the suggestion too (RejectedEndpoints.md rule 1).

**The caller's own pairing that is no longer pending** (2026-10-29):

> Q: You suggest a tag on a post where you yourself suggested that pairing before, and it is no longer pending (since approved, rejected, withdrawn by you, or taken down). "Already suggested by another user and is under review" would be untrue. What should the panel say?
>
> A: "You suggested, thanks" — the option read: “"Thank you. You have already suggested this tag for this post." Never says whether it was approved or rejected.”

The other option put was "You suggested it", which read: “"You have already suggested this tag." with no mention of review, whatever happened to it since.”

**A rejected name** (2026-10-29):

> Q: A reader types a name held by another reader's Rejected tag (or, until #1362 lands, a soft-deleted tag) and is refused with one message, "The tag cannot be suggested." Since hidden tags that aren't rejected get paired, that refusal tells the reader a tag holds the name and can't be used. No read reveals that today. Do you approve this disclosure as a recorded deviation?
>
> A: "Hide the refusal" — the option read: "Don't approve it. The design must find an answer that reveals nothing, which may mean a success answer that pairs nothing."

So the deviation this design proposed for that refusal is not approved, and the suggestion withholds the pairing instead of refusing it (rule 4).

**A withheld tag's code** (2026-10-29):

> Q: To hide a rejected tag, the design now "withholds" it: no tag, no pairing, and the reader gets the same answer a hidden tag would, 201 Created, though nothing was created. You approved 201 only "when something was created". Approve 201 for a withheld tag when the pairing is free?
>
> A: "Approve 201 (Recommended)" — the option read: "Withheld tag answers 201 Created, exactly like a hidden tag in the same case, so the two can't be told apart."

**A withheld tag suggested twice** (2026-10-29):

> Q: A reader suggests a withheld (rejected) tag twice for the same post. The second answer is Created again, while a hidden tag the first suggestion did pair would answer "You have already suggested this tag" — so the repeat gives away what you asked to hide. What should happen?
>
> A: "B: always Created (Rec.)" — the option read: "Every suggestion of a tag the reader can't see answers Created, whatever already holds the pairing. Reveals nothing, no new storage, but such readers are never told a pairing is theirs."

So a tag the reader cannot see answers `Created` whatever holds the pair (rule 5), and the rulings that tell a reader whose a pairing is — *The reader's own pending pairing*, *A pairing that was itself rejected* and *The caller's own pairing that is no longer pending* — reach a tag the reader can see alone (rule 7).

**The box on a rejected post** (2026-10-29), in the owner's own words, chosen over both options put (*"Withhold it"* and *"Keep it"*):

> Q: /myposts/{id} shows an owner their own rejected post with the Suggest a tag box, and the server refuses a suggestion there. Should the box be withheld on a rejected post, as Like is?
>
> A: "In terms of the UI the approval status of the post can be applied to the association control (tag control / bible reference control) as a property value.  When the post is rejected, this control will only display what is already associated and prevent any new submissions.
>
> The API / server will refuse it.
>
> The presentation controls pretty much become read-only in terms of additions.  I don't mind if it show any existing associations for Likes / Tags / Bible References.  Even if removal is still present that is fine as well i.e. you could remove the tag / bible reference, but not add new ones.
>
> This is by design as we DO NOT want to generate approval workflows for REJECTED content."

**Splitting the suggestion** (2026-10-29):

> Q: #1368 (the orchestration's suggest-a-tag operation) now has 14 logic criteria, past the planner's size limit of about eight. Split it, or let it stand?
>
> A: "Split it (Recommended)" — the option read: "Finding or adding the tag by its name, with the withholding of a rejected or deleted tag, becomes an operation and task of its own beneath the suggestion."

So the find-or-add — `Backend/Orchestrations/AssociationOrchestrationService.TagSuggestions.md §1` rule 1's steps 9 and 10, with step 9's withholding and the entry it logs — becomes an operation of its own, with a task of its own, beneath the suggestion. Where it sits is open question 6.

**Two readers pairing one tag at once** (2026-10-29):

> Q: Two readers suggest the same new pairing at the same moment. The second insert hits the unique index and answers 409. For a tag the reader can't see, that 409 tells it apart from every other answer, against your "always Created" ruling. What should the second reader get?
>
> A: "Answer Created (Rec.)" — the option read: "The race answers Created for a tag the reader can't see, like every other case. Nothing is revealed."

So a second reader who cannot see the tag is answered `Created` where the pair is taken between the occupancy check and the insert (rule 10), and a `409` reaches only a reader who can see the tag.

## Business rules

1. **A reader suggests a tag for a post by its name, in one request** (*The requirement*). The page sends the post's id and the name, once for each name the panel raises. The server finds the tag the name names, or adds one, and pairs it with the post unless the pairing already exists. The submission is the whole of this feature: the tag's approval and the pairing's are the approval process's, and separate (*The requirement*; §APR9.7).
2. **What names a tag.** The submission decides it before anything is read:
   - **White space around the name is trimmed by the server**, and a name that is empty once trimmed is refused. White space inside the name is kept as it was typed.
   - **The server strips no hash.** The panel strips a leading hash before it raises `onAdd` (`Design/UI/Components/AssociationPanel.TagAssociationPanel.md` rule 2.3). A hash that reaches the server is part of the name, as it is on `POST /api/Tags`.
   - **Letter case is the column's to decide.** `Tag.Name` has no collation of its own, so it takes the catalogue's default, `SQL_Latin1_General_CP1_CI_AS` on every catalogue in use today (§EVN25, *What was found*), which ignores letter case and respects accents. So two names that differ only in letter case name one tag, and `grace` finds `Grace`, while `café` and `cafe` are two names. The tag keeps the name it was created with: the submission never renames a tag.
   - **A name is at most 30 characters once trimmed**, the cap `Tag.Name` has (`TagService.Validations.cs`). A longer name is refused before anything is read.
3. **Which tag the name names is decided by a read no identity filters.** The read by name (`Backend/Foundations/TagService.TagSuggestions.md §3`) finds the one row a name is held by, whatever its approval status, whoever owns it, and whether or not it is soft-deleted, and answers its id, its status and whether it is deleted, and nothing else. An identity-filtered read never decides whether a name is taken (root `CLAUDE.md`, *Non-negotiables*): the tag reads answer a row the caller may not see as not found (§SEC14.7 posture A rule 4; §SEC14.5 rules 1 and 3), and an add over a taken name would be refused by `IX_Tags_Name`. The foundation authors the read's condition, as a query-shaping function, and the storage broker hands it to the storage client (§ARC12.2.1 rules 1 and 3).
4. **What the submission does with the tag the name names:**

   | The name is held by | The submission | Ruling |
   | --- | --- | --- |
   | no tag | adds a tag, at `Submitted` (rule 14), and pairs it | *The requirement*, step 3 |
   | a public tag — `Approved`, published, its publish date absent or past | pairs it | *The requirement*, step 4 |
   | the caller's own `Draft` or `Submitted` tag | pairs it | *The requirement*, step 4 |
   | a tag the caller cannot see — another reader's `Draft` or `Submitted` tag, or an `Approved` tag that is unpublished or scheduled | pairs it where its pair is free, and answers `Created` whatever holds the pair (rule 5) | *A tag the reader cannot see*; *A withheld tag suggested twice* |
   | a `Rejected` tag, whoever's, soft-deleted or not | withholds it: no pairing, and no new tag, and the answer is the one a tag the caller cannot see would get (below) | *Where the submission lives* (2026-10-26); *A soft-deleted tag*, its second paragraph; *A rejected name* |
   | a soft-deleted tag that is not `Rejected` | withholds it, as a rejected one, until #1362's undelete replaces the withholding on this route; then undeletes it on the caller's behalf, whoever deleted it, and pairs it, the tag going back to approval | *A soft-deleted tag*; *Who triggers the undelete*; §ARC12.3.1 rule 5; *A rejected name* |

   §ARC12.3.1 rule 5 bars a soft-deleted endpoint from a new pairing, and stands as written until #1362's design replaces it (rule 5's own note). So the first build withholds a soft-deleted tag. #1362's design adds the section that replaces that withholding with its undelete on this route, beside `Backend/Orchestrations/AssociationOrchestrationService.TagSuggestions.md §1`, and carves its task; the read, the operation that writes the undelete, the fact it publishes and how the round starts again are #1362's (#1362, *What the design must settle*, points 3–5). This feature hands it the row its read by name found, at the place in the flow where a found tag is paired (rule 9, step 7).

   **A withheld tag tells the caller nothing** (*A rejected name*; RejectedEndpoints.md rule 12). A tag is withheld where it is `Rejected`, or soft-deleted until #1362's undelete replaces that. The submission adds no tag and inserts no pairing, and answers as it answers any tag it finds (rule 7). Where the caller cannot see the tag, the answer is `Created`, whatever holds the pair (rule 5); no caller can see a soft-deleted tag (§SEC14.5 rule 3). Where the caller can see it — their own rejected tag, or one a review role reads — the answer is what the occupancy check finds, and `Created` where no row holds or covers the pair, though nothing was created (*A withheld tag's code*). So a caller who cannot see a withheld tag is answered as for another reader's hidden tag on every suggestion, a repeat included (*A withheld tag suggested twice*), and an outward answer is all a caller can tell apart (§SEC14.5, *Indistinguishability is a property of the OUTWARD ANSWER*): they can tell neither a rejected tag nor a soft-deleted one from a hidden one, nor tell that a tag they paired earlier has since been rejected. The true reason is logged server-side, as information, before the answer is returned, naming the tag's id and whether it is rejected or soft-deleted, and noting that the tag was not paired and the caller was not told so (§SEC14.5 rules 6 and 7).
5. **A tag the caller cannot see is paired, and nothing in the answer tells it from a tag the caller created** (*A tag the reader cannot see*). The answer carries a status and nothing else (rule 7): no tag id, and no association id either, since an id the caller holds can be read back, and a read answers not found where the tag is hidden (§SEC14.7 posture A rule 4; §SEC14.3 rules 3 and 4). Pairing a tag the caller cannot see departs from §ARC12.3.1 rule 5a's visibility precondition, on this route alone (*Deviations* 1).

   **Whether the caller can see the tag is asked of the tag, and decides only the answer** (*A withheld tag suggested twice*). A tag the read by name finds is read once more, by its id, under the caller's identity, through the tag's own read, which answers a row the caller may see and answers not found for every other (§SEC14.7 posture A rule 4; §SEC14.5 rules 1 and 3). A tag that read answers not found is one the caller cannot see, and the answer to it is `Created`, whatever holds the pair: the caller's own pending pairing, their own earlier one, another reader's, or none, and a pairing another reader inserts between the occupancy check and the insert too (rule 10; *Two readers pairing one tag at once*). It is still paired where its pair is free and it is not withheld (rules 4 and 6), so the read decides which answer, and never whether the tag is paired (root `CLAUDE.md`, *Non-negotiables*). A tag the submission has just added is the caller's own, and is not read again. So a caller is never told whether a pairing with a tag they cannot see is theirs or another reader's, as the owner's ruling accepts (*"such readers are never told a pairing is theirs"*).

   What the creator of a tag the caller cannot see keeps, by *A tag the reader cannot see*:
   - **A rename** moves the pairing with the tag: the pairing is made by the tag's id, never its name, so it shows the new name once the tag and the pairing are approved and published. §ARC12.3.1 rule 2a's rename question stays open.
   - **A removal** soft-deletes the tag. Every pairing made through it is hidden while it is deleted (§SEC14.3 rule 3), and its name stays reserved (§ARC12.3.1 rule 2a), so the next suggestion of the name meets the soft-deleted row of rule 4.
6. **No pairing is made twice** (*Where the submission lives*: *"Duplicate associations should not be allowed."*). The pairing is the upsert's editorial arm as it stands (`Backend/Orchestrations/AssociationOrchestrationService.md §1` rule 3 and §6): the post and the tag are resolved into a pair the way the upsert resolves one (§DOM4.5), the exact-pair probe asks whether a row holds that pair and, if none does, the overlap probe asks whether a differently scoped live row covers it, and only a pair neither finds is inserted, at `Submitted` (§ARC16.8.1, #872). An occupied pair is never written again, whatever holds it: a live row at any status, a row `Rejected` included (*A pairing that was itself rejected*); a soft-deleted row, until #1326's design settles what an editorial add answers there (#1326, deliverable 2), the later of the two designs rebasing on the earlier's words; and a differently scoped live row that covers the pair.
7. **The submission answers a status and nothing else**, as §ARC16.8.1's result does, without its id (rule 5):

   | Status | When | Code |
   | --- | --- | --- |
   | `Created` | a pairing was inserted, whether the tag was found, added or undeleted; or the tag is one the caller cannot see, whatever holds the pair, inserted or not, the insert refused because another reader's took the pair in between included (rules 5 and 10); or the tag is withheld, the caller can see it, and no row holds or covers the pair, though nothing was inserted (rule 4) | `201` |
   | `AlreadySuggestedByCaller` | the caller can see the tag, and the pair is held, or covered, by a live row the caller created, at `Draft` or `Submitted` (*The reader's own pending pairing*) | `200` |
   | `PreviouslySuggestedByCaller` | the caller can see the tag, and the pair is held, or covered, by a row the caller created that is no longer pending: a live row at `Approved` or `Rejected`, or a soft-deleted row at any status, withdrawn by them or taken down (*The caller's own pairing that is no longer pending*) | `200` |
   | `AlreadySuggested` | the caller can see the tag, and the pair is held, or covered, by a row another reader created, at any status, live or soft-deleted (*A pairing that was itself rejected*) | `200` |

   The codes are the owner's (*The routes answer `200` where the pair already existed*; *A withheld tag's code*; *A withheld tag suggested twice*): `201` for every `Created`, those where nothing was created included, so that a tag the caller cannot see and a withheld one are answered alike (`Backend/Controllers/AssociationsController.TagSuggestions.md`, *Deviations* 2). A row covers a pair only by holding this post's pairing with this tag at one of the post's versions (§DOM4.6), so the pair already existed for every `200`, and the caller's own pairing is theirs whichever probe finds it: the exact-pair probe, or, where that finds no row, the overlap probe, which answers a live row only (rule 6). Whose a row is, is its `CreatedBy`. An approved pairing never answers a status of its own, because the caller may not see it, and a state of a row the caller may not see is never told (§SEC14.5 rule 2). So another reader's approved pairing answers `AlreadySuggested`, and the caller's own answers `PreviouslySuggestedByCaller` with their own rejected and removed ones, which never says which (*The caller's own pairing that is no longer pending*). A pairing the caller can see, approved and published with a public tag, is in the panel's collection already, and the panel drops a value it already lists before raising `onAdd` (`Design/UI/Components/AssociationPanel.md` rule 2.22).
8. **What the reader is told.** The page answers each outcome in a toast, as it answers today's *coming soon* (`toastSuccess`):
   - `Created`: *"Thank you for your contribution. Your tag has been suggested and is under review."*
   - `AlreadySuggested`: *"Thank you for your contribution. This tag has already been suggested by another user and is under review."* (*A pairing that was itself rejected*)
   - `AlreadySuggestedByCaller`: *"Thank you for your contribution. You have already suggested this tag, and it is under review."* (*The reader's own pending pairing*)
   - `PreviouslySuggestedByCaller`: *"Thank you. You have already suggested this tag for this post."* (*The caller's own pairing that is no longer pending*)
   - A refusal or a failure: the app's global error toast, as for any failed write (`src/brokers/apiBroker.globals.ts`; RejectedEndpoints.md rule 9).

   Once the write settles, whether it succeeded or failed, the page's association reads are read again (`UI/Foundations/AssociationService.TagSuggestions.md §1`), so the panel's collection shows whatever the reader may now see of the pairing once the page reads it (`UI/Pages/PostDetail.md §6 item 4`, `UI/Pages/MyPostDetail.md §6 item 5`).
9. **The gates come in this order, and nothing is read or written past a refusal that could be known first** (`Backend/Orchestrations/AssociationOrchestrationService.TagSuggestions.md §1`):
   1. authentication and the global `ReadOnly` (§SEC14.7 posture A′ rule 1), before anything is read;
   2. the suggestion's own fields: the post's id, and the name, under rule 2;
   3. the post, resolved under the caller's envelope as the upsert resolves an endpoint (§DOM4.5): a post that does not exist, or that the caller may not see, is not found (§SEC14.5 rule 1);
   4. the pairing's endpoint veto, composed from the post's derived content type and the tag's type: `ContentItem-ReadOnly`, `ContentItem-{ContentType}-ReadOnly` and `Tag-ReadOnly` (posture A′ rule 1). It is the tag add's veto as well, which is `ReadOnly` or `Tag-ReadOnly` (posture A rule 1), so a caller who could not add the tag is refused before the tag is read;
   5. a rejected post (RejectedEndpoints.md rules 1 and 10);
   6. the facet gate: the post's winning setting must allow `TagsAllowed` (§ARC16.2.1). It comes before the read by name, so a caller refused on the post's setting learns nothing of the name;
   7. the read by name; a rejected or soft-deleted tag it finds is withheld (rule 4), and the flow goes on to step 9 with no add;
   8. the add of a new tag, where none holds the name;
   9. whether the caller can see a tag the read by name found, by the tag's own read under the caller's identity (rule 5);
   10. the occupancy check (rule 6), a withheld tag's pair included;
   11. the insert of the pairing, where the pair is free and the tag is not withheld.
10. **What a failure midway leaves, and what a retry then does.**
    - **The tag was added, and the pairing was not written** — the occupancy check or the insert failed. The tag stays, a pending suggestion at `Submitted`, unpaired, and its round is open, since its add published `Tag-Added` (rule 13). It is not removed: a removal would hold its name for good (§ARC12.3.1 rule 2a), and removal is its owner's act or an `Administrators`' takedown (§APR9.9). A retry of the same name finds the caller's own `Submitted` tag and pairs it, `Created`, and so does another reader's suggestion of the name (rule 4).
    - **Two readers add the same new name at once.** Both find no tag; the second's add is refused by `IX_Tags_Name` (`AlreadyExistsTagException`). The submission then reads the name once more and goes on with the tag it finds as rule 4 has it — the first reader's `Submitted` tag, which it pairs, or a tag it withholds — and answers as rule 7 answers. So the second reader receives what they would have received had the first reader's tag been there before they asked (*A tag the reader cannot see*). If that second read finds no row, the add's refusal stands, as a fault (rule 11).
    - **Two readers pair the same tag with the same post at once.** Both probes find the pair free; the second insert is refused by the editorial pair index (`AlreadyExistsAssociationException`). Where the second reader can see the tag, the refusal is answered `409`, as the upsert's editorial arm answers it (`Backend/Controllers/AssociationsController.md`, the shared mapping). Where they cannot, it is not thrown on: they are answered `Created`, as every suggestion of a tag they cannot see is answered (rule 5), so nothing is revealed (*Two readers pairing one tag at once*). Either way nothing more is written, and a retry answers as rule 7 answers the row the first reader's insert wrote: `AlreadySuggested` where the second reader can see the tag, `Created` where they cannot.
    - **An undelete followed by a failed pairing** is #1362's to settle (#1362, point 8).
11. **Every refusal `ITagService` could make is asked first by the submission**, so none reaches the caller from beneath: the tag's veto (rule 9, step 4), the name's emptiness and length (step 2), and a taken name (rule 10, the second read). The not-found of rule 5's read is an answer, and refuses nothing (step 9). What still reaches the orchestration's closing `catch (Xeption)` from `ITagService` is a fault beneath it, not the caller's, and leaves as its dependency exception, `424`, as a refusal from the summary read's foundations does (`Backend/Orchestrations/AssociationOrchestrationService.md §4` rule 9). No catch arm is added, for that rule's reason.
12. **No request address: the submission is direct only**, as the personal upsert is, which has no `Association-Upserting` (§ARC16.2.2). Nothing in the system publishes a suggestion. Its answer is a status, and a reply on an event address carries an envelope of the row, which is what the status withholds (§ARC12.5.2 rule 6, *"Rule 2 — the occupancy check runs on both doors"*). And an address is an event contract nobody has asked for. If one is minted, it binds `IAssociationOrchestrationService`, which derives the pairing's content type, an authorization input (§ARC12.5.2 rule 6, *an address whose handler derives an authorization input binds the layer that derives it*), and its deduplication is designed with it.
13. **The facts are the writes' own.** The submission publishes no fact of its own, as the upsert publishes none: the orchestration holds no `IEventBroker` (§ARC12.5, *an orchestration holds brokers*). A new tag's add publishes `Tag-Added`, which `ApprovalOrchestrationService.OnTagAddedAsync` hears, opening the tag's round (§EVN18 rule 1; §APR9.2 rule 1). A new pairing's add publishes `Association-Added`, which `ApprovalOrchestrationService.OnAssociationAddedAsync` hears. #1362 designs what its undelete publishes. No subscriber changes.
14. **The status each new row opens at.** A new tag opens at `Submitted`: suggesting a tag *"is a finished act with no draft stage"* (§APR9.2), the add takes its status from its caller (§APR9.7.1 rule 1), and the submission is that caller. A new pairing opens at `Submitted` (§ARC16.8.1, #872). A tag the submission finds is left as it is: the submission writes nothing to a tag that exists, save #1362's undelete.
15. **The tag's add mints the new tag's `Id`.** An `Id` is an `IKey` member, the identifier broker's, never accepted from a caller (§APR9.7.1 rule 2), and the orchestration holds no identifier broker (§ARC12.5, *an orchestration holds brokers*), so it cannot hand the add one. Until this feature the add took its caller's and refused an empty one (`TagService.Validations.cs`, `ValidateOnAddTagAsync`). It mints it instead, on both entry paths, as the association's add does (`Backend/Foundations/AssociationService.md §13`; `Backend/Foundations/TagService.TagSuggestions.md §1` and §2). So `POST /api/Tags` writes the minted `Id` whatever its caller sends, and answers it.
16. **The two post pages send it.** `/posts/{id}` and `/myposts/{id}` send each tag their panel raises to this feature's route and answer it under rule 8 (`UI/Pages/PostDetail.md §6 item 17`, `UI/Pages/MyPostDetail.md §6 item 18`). Their panels' collections are the association read's (#1128), which each page wires separately (`UI/Pages/PostDetail.md §6 item 4`, `UI/Pages/MyPostDetail.md §6 item 5`). The panel raises `onAdd` and calls nothing (`Design/UI/Components/AssociationPanel.md` rule 2.7). It gains one property, the post's approval status, for rule 17.
17. **A rejected post takes no suggestion** (*The box on a rejected post*). Where the post is `Rejected`, the tag panel offers no *Suggest a tag* box, and still shows what is already associated, its owner's withdrawal included (`Design/UI/Components/AssociationPanel.md` rule 2.33). Each page hands the tag panel the post's approval status, as data, for it (`UI/Pages/PostDetail.md` rule 2.24, `UI/Pages/MyPostDetail.md` rule 2.25). The server refuses a suggestion for a rejected post all the same (rule 9, step 5), and a refusal that reaches a page, from a page shown before its post was rejected, is answered as any failed write (rule 8), since a rejection is not pushed to an open page (`LiveUpdates.md` rule 3). The ruling names the Bible reference control too; its panel's box on a rejected post is #1357's, with the rest of that panel's write (`UI/Pages/PostDetail.md §6 item 18`, `UI/Pages/MyPostDetail.md §6 item 19`).

## User stories

| User story | Level |
| --- | --- |
| [Backend/Brokers/StorageBroker.TagSuggestions.md](Backend/Brokers/StorageBroker.TagSuggestions.md) | broker — `IStorageBroker`, the tag pass-through the read by name hands its condition to |
| [Backend/Foundations/TagService.TagSuggestions.md](Backend/Foundations/TagService.TagSuggestions.md) | foundation — `ITagService`: the add mints the `Id`, on both doors, and the read by name |
| [Backend/Orchestrations/AssociationOrchestrationService.TagSuggestions.md](Backend/Orchestrations/AssociationOrchestrationService.TagSuggestions.md) | orchestration — `IAssociationOrchestrationService.SuggestTagAsync` |
| [Backend/Controllers/AssociationsController.TagSuggestions.md](Backend/Controllers/AssociationsController.TagSuggestions.md) | exposer — `AssociationsController`, the route |
| [UI/Brokers/AssociationBroker.TagSuggestions.md](UI/Brokers/AssociationBroker.TagSuggestions.md) | React broker — `AssociationBroker`, the write |
| [UI/Foundations/AssociationService.TagSuggestions.md](UI/Foundations/AssociationService.TagSuggestions.md) | React foundation — `associationService.useSuggestTag` |

Each component but the storage broker's tag partial already has a user story document under another feature, so this feature's work on each is in a document of its own beside it (`Design/design.md`, *A component that a second feature adds operations to gets a second user story document*).

The panel's half is designed in the document the presentation component owns (§UI20.6.4), and the two pages' halves in their page documents (§UI20.5.1). Each task is carved from its gap:

| Document and gap | What it is |
| --- | --- |
| `Design/UI/Components/AssociationPanel.md §10 item 25` | the panel takes the post's approval status, and offers no add box on a rejected post (rule 17) |
| `Design/UI/Pages/PostDetail.md §6 item 17` | `/posts/{id}` sends a suggested tag and says what came of it, and hands the tag panel the post's status |
| `Design/UI/Pages/MyPostDetail.md §6 item 18` | `/myposts/{id}` does the same |

**Build order.** The storage broker's pass-through, then the read by name. The add's mint, then its event door's pins. The orchestration once both are in, and once #1358 has `ResolvedEndpoint` keep the status a rejected post is refused by. The route after the orchestration, and after #1124, whose record of the controller's built routes the route's record extends. The React broker, then its service. The panel's gap in any order with those, and the two pages, in either order, once the service and the panel's gap are in.

**Layer placement — ruled.** The owner placed the submission on `IAssociationOrchestrationService`, behind a route on `AssociationsController` (*Where the submission lives*). The flow spans three entity types (below), which is §ARC12.1 rule 2's orchestration. The controller binds that one service (§EVN13 rule 3; `the-standard-exposers` ts-exposers-001). The rules that decide a pairing — the endpoint veto, the rejected post, the facet gate, the occupancy check — are already the orchestration's, so the submission reaches them there rather than copying them.

- **What it holds.** It adds no dependency: `ITagService`, `IContentItemService` and `IAssociationService` are among the seven entity services the orchestration holds, and the brokers it already holds are enough. Its seven entity services are a two-to-three deviation, approved for this operation on its own argument (`Backend/Orchestrations/AssociationOrchestrationService.TagSuggestions.md`, *Deviations* 1). The Likes user story's deviation (`Backend/Orchestrations/AssociationOrchestrationService.md`, *Deviations* 1) is not its reason.
- **The read by name is the tag foundation's.** It is a read of one entity, and its condition is a decision the foundation authors and unit-tests (§ARC12.2.1 rule 1). It is `internal`, as the identity-carrying tag read is, and it answers a projection rather than the row (`Backend/Foundations/TagService.TagSuggestions.md §3` rules 1 and 6), because `TagsController` binds the foundation (§EVN13 rule 3) and a public member answering the row would hand a row the caller may not see to any route wired to it (§SEC14.5 rules 1 and 3). This is §SEC14.6's pattern for a cross-row fact under visibility filtering — an unfiltered read that answers no row data and carries the contribution gate (§SEC14.6, *Cross-row rules under visibility filtering*) — as the pair probe's `AssociationPairMatch` is.
- **The find-or-add goes beneath the orchestration** (*Splitting the suggestion*). A read by name and then an add, over one entity, is a probe-then-write, which §ARC12.1 rules 1 and 3 give to a processing service. Both places beneath the orchestration were weighed and rejected before the owner's ruling (`Backend/Orchestrations/AssociationOrchestrationService.TagSuggestions.md`, *Deviations* 1), and each departs from a rule this solution holds, so where it sits is the owner's to rule (open question 6). Until it is ruled, the orchestration's user story writes it as steps 9 and 10 of its flow.

## Entity count, events and storage

- **Entities: three.** The `Tag` the name names, read and perhaps added; the `Association` that pairs it; and the host `ContentItem`, read under the caller's envelope to show the caller may see it and to derive its content type, an authorization input of the pairing's veto and facet gate (§DOM4.5 rule 3).
- **Events.** None new: no request address (rule 12) and no fact of its own (rule 13). The writes publish the facts they publish today, `Tag-Added` and `Association-Added`, with their subscribers unchanged.
- **Storage.** No schema, no index and no migration. The read by name asks `Tags` by `Name`, which `IX_Tags_Name` indexes, through a new storage-broker pass-through (`Backend/Brokers/StorageBroker.TagSuggestions.md §1`).

## Risks

- **Reversible throughout.** No schema, no event and no migration. The add's mint changes what `POST /api/Tags` stores for a caller's `Id`, and nothing reads a caller's `Id` back but tests (rule 15).
- **The page's refresh can tell what the answer does not.** The answer to a tag the caller cannot see is the answer to a tag they created (rule 5). Once the page reads its associations, though, the panel shows the caller their own pending pairing with their own tag, since both rows are theirs (§SEC14.7 posture A rule 4), and does not show a pairing with another reader's hidden tag until that tag is approved and published (§SEC14.3 rule 4), which is what the owner's ruling has the pairing do (*"The pairing shows once the tag is approved and published."*).
- **A pairing with another reader's `Draft` tag waits on them.** A `Draft` tag reaches no reviewer until its creator submits it (§APR9.2 rule 2), so the pairing does not show until they do.
- **A tag added by a failed suggestion sits in review unpaired** (rule 10), until a retry pairs it or the review decides it.
- **The add's mint changes `POST /api/Tags`.** A caller's `Id` is ignored, and the route answers the minted one. Tests that post a tag and read it back by the `Id` they posted must read it by the `Id` answered (`Backend/Foundations/TagService.TagSuggestions.md §1` rule 5).
- **A withheld tag is answered as a pairing, and nothing was paired** (rule 4). Where the reader cannot see it, or its pair is free, the reader is thanked as for a new suggestion, *"Your tag has been suggested and is under review."* (rule 8), though nothing was written, and the page's refresh shows no pairing, as it shows none with another reader's hidden tag (the risk above). The true reason is in the server's log alone. That is what the owner's rulings allow (*A rejected name*: *"which may mean a success answer that pairs nothing"*; *A withheld tag's code*).
- **A reader is never told that a pairing with a tag they cannot see is theirs** (rule 5; *A withheld tag suggested twice*). Suggesting such a tag again is thanked as a new suggestion, `Created`, though the pairing their first suggestion made holds the pair and nothing is written.

## Out of scope

- The approval process for the tag and for the pairing, Berean's review among it (*The requirement*).
- Suggesting a Bible reference: #1357.
- The undelete itself, and which routes reach it: #1362 (rule 4).
- The refusal of a rejected item on every other door: `DesignFeatures/RejectedEndpoints.md`.
- Reading an entity's associations (#857's tasks), and removing one by id (#700).
- The panels on `/BibleReferences` (#700), and on `/Admin/Posts/{id}`, where the card's association chips replace them (`UI/Pages/ContentItemModerationDetailPage.md §6 item 17`).
- A reader's reaction, the personal arm of the upsert.
- Renaming a tag, and with it §ARC12.3.1 rule 2a's index fix.
- The panels' read-only roles and facet switches (`Design/UI/Components/AssociationPanel.md §10 items 1 and 20`; `UI/Pages/PostDetail.md §6 item 8`; `UI/Pages/MyPostDetail.md §6 item 6`).
- A per-tag length cap in the panel. The box caps what is typed at 100 characters (`Design/UI/Components/AssociationPanel.md` rule 3.1.14) and several tags may be typed at once; a single name over 30 characters is refused by the server (rule 2) and answered as a failed write (rule 8).

## Deviations

1. **§ARC12.3.1 rule 5a's visibility precondition, and §SEC14.3's paragraph *The symmetry with the write is the argument*, on this route.** Rule 5a binds a pairing to an endpoint the caller can see, *"An unrelated caller still cannot, because the endpoint reports not-found to them"*, and §SEC14.3 calls a pairing the caller could not then read back *"the one outcome neither rule intends"*. On this route a tag the caller cannot see is paired, and the caller cannot read the pairing back until the tag is public (rule 5). **Why:** a name is held by at most one tag (`IX_Tags_Name`), so a suggestion of a name another reader's hidden tag holds can neither add a tag nor, under rule 5a, pair the one there; the owner ruled that it pairs it. **Instead:** the tag is found by the read by name, which no identity filters (rule 3), and paired; the answer tells nothing of it (rules 5 and 7). The post, the other endpoint, keeps rule 5a's precondition: it is resolved under the caller's envelope (rule 9, step 3). Every other door keeps rule 5a as written. Recorded here rather than in rule 5a, because it is this route's departure and no rule every feature inherits (`Design/design.md`, *Record every deviation where it happens*). **Approved** by the owner on 2026-10-27 (*A tag the reader cannot see*: *"Pair the post with that tag and answer as if the reader had created it."*).

The route's departures from `the-standard-exposers` are recorded in `Backend/Controllers/AssociationsController.TagSuggestions.md`, *Deviations*, and the orchestration's dependency count in `Backend/Orchestrations/AssociationOrchestrationService.TagSuggestions.md`, *Deviations*.

## Open questions

One is open, question 6. The owner answered the first five on 2026-10-29, through the session's question prompt (*The rulings*).

1. ~~**The caller's own pairing that is no longer pending.**~~ **Answered 2026-10-29** (*The caller's own pairing that is no longer pending*): it answers `PreviouslySuggestedByCaller`, *"Thank you. You have already suggested this tag for this post."*, which never says whether it was approved or rejected (rules 7 and 8). *(This question used to ask whether the caller's own `Approved`, `Rejected` or soft-deleted pairing answers "You have already suggested this tag" in place of the "another user" wording, and with what in place of "and it is under review".)*
2. ~~**The refusal of a rejected name.**~~ **Answered 2026-10-29** (*A rejected name*): the disclosure is not approved, and a rejected or soft-deleted tag is withheld with an answer that tells it from no other tag (rule 4). *(This question used to ask whether the owner approved, as a recorded deviation of §SEC14.5 rules 1 and 3, a refusal telling a caller that a tag they cannot see holds the name and may not be suggested.)*
3. ~~**The box on a rejected post.**~~ **Answered 2026-10-29** (*The box on a rejected post*): the panel takes the post's approval status, and offers no add box on a rejected post (rule 17). *(This question used to ask whether the box is withheld on a rejected post, as Like is, or offered with the refusal answered as a failed write.)*
4. ~~**The code of a withheld tag's answer where its pair is free.**~~ **Answered 2026-10-29** (*A withheld tag's code*): `201 Created`, as for a hidden tag in the same case (rules 4 and 7; `Backend/Controllers/AssociationsController.TagSuggestions.md`, *Deviations* 2). *(This question used to ask whether the owner approved `201 Created` for a withheld tag whose pair is free, though nothing was created, when `201` was approved only where something was.)*
5. ~~**A withheld tag suggested twice.**~~ **Answered 2026-10-29** (*A withheld tag suggested twice*): option B. Every suggestion of a tag the caller cannot see answers `Created`, whatever holds the pair, and whether the caller can see a tag is asked of the tag's own read under their identity (rules 5 and 7). *(This question used to ask what a second suggestion of a withheld tag for the same post answers, since a stateless answer gave the repeat away, and offered A, accepting it as a deviation; B, the option chosen; and C, a store of withheld suggestions.)*
6. **Where the find-or-add sits** (*Splitting the suggestion*). The owner ruled that finding or adding the tag by its name, with the withholding of a rejected or soft-deleted tag, becomes an operation and a task of its own beneath the suggestion. There are two places beneath the orchestration for it, and each departs from a rule this solution holds:
   - **A member of the tag foundation**, `internal`, as the read by name is (`Backend/Foundations/TagService.TagSuggestions.md §3` rule 1). The orchestration keeps the seven foundation services its deviation was approved for, all of one kind (`Backend/Orchestrations/AssociationOrchestrationService.TagSuggestions.md`, *Deviations* 1). It departs from §ARC12.1 rule 1, which gives a probe-then-write over one entity to a processing service, as `the-standard-processings` ts-processings-003 gives it an *Ensure* operation. The read by name would then be a step of the new member rather than a member the orchestration calls, so #1367 would be carved again with it.
   - **A tag processing service**, new, over `ITagService`, where §ARC12.1 rule 1 and The Standard put the work, and #1364 to #1367 stand as they are. The orchestration still reads `ITagService` to resolve a tag endpoint and to ask whether the caller can see a tag (`Backend/Orchestrations/AssociationOrchestrationService.TagSuggestions.md §1` rule 1, step 11), so it would hold eight entity services where seven are approved, and both kinds, reaching `Tag` at two levels at once. Mixing kinds is barred by the root `CLAUDE.md` *Non-negotiables* (*"never a mix of foundation and processing"*) and by the same-kind rule (§ARC12.5; `.claude/agents/planner.md`, *One kind of dependency, never a mix*), so this needs two deviations, the count and the mix, besides a new service with its own exception families, registration and tests.

   #1368 is not signed off until this is ruled. The planner then writes the new operation's section in the user story document of the component that holds it, carves its task, and moves the find-or-add's criteria out of #1368 into it.
