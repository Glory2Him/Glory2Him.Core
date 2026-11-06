# Porting an existing document

How a document written before this workflow — a specification, a requirements
document, an older design — becomes feature requests and global rules that can be
planned and built. [new-feature.md](new-feature.md) takes each feature on from there.

Moving a whole repository made from the older template, with tasks already signed
against its design documents? Start with
[migrating-an-existing-repository.md](migrating-an-existing-repository.md); it uses
this procedure for the global documents and for any feature not yet carved into tasks.

## Why it is ported, not read

Existing documents live in `Documentation/Planning/`, and **nothing is planned, built,
reviewed or documented from them.** A document counts only once its contents have been
ported through the issue forms:

- each **feature** it describes becomes a **Feature request**, filed through the form,
  with the document linked in the form's **Ported from** field;
- each **rule** every feature must follow, and each **term**, goes into the **Global
  Rules issue**, through its form if it has not been filed yet, otherwise through a
  reviewed change.

That way every feature and rule is logged the same way — the same fields, labels,
reviews and approvals — whatever it started as, and an old document can never quietly
override something that was agreed later. The planner, developer, QA and releaser never
open `Documentation/Planning/`. Only `/grill-with-docs` does, for the one document you
name, with you in the session. The one exception is a task labelled `legacy`, which is
built and verified against the documents it cites
([Planning README](../Planning/README.md#legacy)).

## At a glance

| Step | Who | What happens | It ends with |
| --- | --- | --- | --- |
| [1. Add the document](#1-add-the-document) | You | Copy it into `Documentation/Planning/` | The file in your checkout |
| [2. Run the port](#2-run-the-port) | You | `/grill-with-docs Documentation/Planning/<document>` | A session working through it |
| [3. Agree what it holds](#3-agree-what-it-holds) | You | Correct Claude's list of features, rules and terms | The list to port |
| [4. Answer the rounds](#4-answer-the-rounds) | You, with whoever owns the content | Settle what the document leaves open or gets wrong | Decisions, in your words |
| [5. File each feature](#5-file-each-feature-through-the-form) | You | Submit each drafted request through the form | Feature requests at `status: needs-design` |
| [6. Close the session](#6-close-the-session) | Claude, on your go-ahead; then the maintainer | The Global Rules change goes to QA; the document's record goes into a `DESIGN:` PR | A rule change awaiting `approved`; a PR to merge |
| [7. Build the features](#7-get-the-features-built) | You | Each feature from step 3 of [new-feature.md](new-feature.md) | Features in `main` |

## Before you start

- **The Global Rules issue is filed.** Find it by the `global rules` label. If there is
  none, file it from **Issues → New issue → Global rules** first. It does not have to
  be approved yet: porting the first document is often how its rules get written.
- **You can answer for the document**, or have the person who can to hand. Claude asks
  about what it means; a document nobody can speak for ports badly.
- **Claude Code is running on your machine**, in the repository, as
  [new-feature.md](new-feature.md#starting-claude-code) describes.

---

## 1. Add the document

Copy the document into `Documentation/Planning/` in your checkout — Markdown, Word or
PDF. Do not commit it to `main` yourself: the session commits it on its own design
branch, so every request can link to it at a fixed commit.

## 2. Run the port

```
/grill-with-docs Documentation/Planning/bookings-spec.docx
```

For a long document, say which part to port this time — "sections 3 to 5" — and run
the skill again for the rest later. It finds the feature requests already ported from
the document, by their **Ported from** link, and leaves those out.

## 3. Agree what it holds

Before any questions, Claude reads the document and lists what it found:

```
Features
  1. Members see the status of their bookings — so they stop asking an organiser
  2. Organisers move a booking to another event — so plans can change without a re-booking
Rules for every feature
  3. A member sees only their own bookings                              → Security
  4. Every status change records who made it and when                   → Domain
Terms
  5. Booking, Member, Organiser
Left behind
  - Section 7, the first options appraisal: history, superseded by section 8
```

Correct it now. Merge two features, split one, move a "rule" that only one feature
needs into that feature, drop what nobody wants any more. A feature is something a
person can use on its own once it ships; the planner will propose a split later if one
is still too big.

## 4. Answer the rounds

Then Claude interviews you in **rounds**: only the questions that can be answered now,
numbered, each with its recommended answer. "Yes" accepts a recommendation, so you only
write out what you would decide differently:

```
1 yes. 2 no — organisers of the same group only. 3 yes.
```

It asks about what the document leaves open, and about everything it says that the
code, the Global Rules issue or an open feature contradicts. **The document is old
intent, not a decision**: where it and the code disagree, you choose which is right,
and the code is existing behaviour until you do. Anything the code or the issues can
answer, Claude looks up instead of asking.

As each **rule or term** settles, Claude writes it into a draft of the Global Rules
issue — kept outside the repository until the end — and shows you the change.

## 5. File each feature through the form

For each feature, Claude drafts the request field by field, in the form's order:
**Summary, Who uses it, Behaviour** (as Given/When/Then), **What must not happen,
Mockups, Data, Done when, Out of scope, Ported from, Deadline**. Everything the planner
will need is written into those fields, because the planner never reads the document.

You file it:

1. **Issues → New issue → Feature request.**
2. Paste each field from the draft.
3. Drag in any image the feature needs — Claude names which ones; it cannot upload them
   for you.
4. Leave the **Ported from** link exactly as drafted: it points at the document at the
   commit Claude pushed.
5. Submit, and tell Claude the issue number.

Claude never files a feature request itself. Every request is raised through the form by
a person who owns it, so it is logged the same way as one typed from scratch.

## 6. Close the session

When nothing is left open, Claude asks you to confirm you share one understanding, then
shows you each step before taking it:

1. **The Global Rules change**, if the port produced rules or terms. It shows you the
   whole change; on your go-ahead it takes `approved` off the issue, applies the change,
   posts a `Change <n>:` comment listing every section and term changed and every open
   feature that cites one, and runs QA's **global rules review**. On a pass, **the
   maintainer** reads the issue and adds `approved` — a person, never an agent.
2. **The files**, on a `users/<you>/design-…` branch: the document itself if it was new,
   and — once every feature in it is filed — its row in the **Ported** table of
   `Documentation/Planning/README.md`, naming the issues it became. Any decision record
   the session produced goes there too. Claude opens an issue recording the session —
   what was settled and where each thing went — and a `DESIGN:` PR into `main` that
   closes it. The maintainer reviews and merges it.

A document ported only in part gets no row in the **Ported** table yet. Everything
filed so far is a real feature request; run the skill again for the rest.

## 7. Get the features built

Each ported feature is now an ordinary feature request at `status: needs-design`, and
follows [new-feature.md](new-feature.md) from **step 3**:

```
/plan-feature <n>
```

- **Wait for the rules first.** If any feature relies on a rule or term from this port,
  `/plan-feature` refuses until the maintainer has approved the Global Rules issue
  again.
- **Choose the order.** Plan first the feature others build on — the one that brings a
  shared entity or service into being.
- **Plan just in time.** `/plan-feature` will not start while two other features wait
  between planning and building, so plan one or two at a time, not the whole document
  at once.
- **Sharpen one further** with `/grill-with-docs <n>` (step 2 of
  [new-feature.md](new-feature.md)) if its draft still felt thin.

Then each goes through the maintainer's approval, `/build-feature <n>`, and a merge,
as any other feature.

---

## Common questions

**Can I keep editing the document after it is ported?** Leave it as it was: it is the
record of what was ported. A change to what was agreed goes through the issues — a new
request, a change to an approved plan, or a Global Rules change.

**The document has diagrams.** A finished diagram a feature needs goes into that
feature request's Mockups field, so it is on the issue — which is what the releaser
documents from. A draft goes in `Documentation/Diagrams/`, which no agent reads.

**Why can't Claude just file the requests?** So that every feature is raised through
the same form, by a person who owns it, and logged the same way as every other. The
draft makes filing a matter of pasting.

**What if I stop half-way?** What you filed stays filed, and a Global Rules change only
happens at the end of a session, with your go-ahead. Run the skill again on the same
document; it skips the features already ported.

**Does the planner ever see the document?** No. If a request is missing something the
document said, the planner asks for it in a question round — and the answer goes on the
issue, where the next agent can read it.

**A feature in the document already has tasks signed `ready for development`.** Do not
port it as a new request. It goes through the legacy lane in
[migrating-an-existing-repository.md](migrating-an-existing-repository.md): its tasks
keep their bodies and are built against the document, read at its Planning path.
