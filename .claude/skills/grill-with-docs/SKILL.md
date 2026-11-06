---
name: grill-with-docs
description: A relentless interview to sharpen a feature request, an area of the design, a new repository's intent, an existing document in Documentation/Planning/ to port, or a section from the Global Rules issue's Legacy rules index, that writes down what it settles as it goes — terms and rules into a draft of the Global Rules issue, decision records into Documentation/Decisions/, INTENT.md for a new repository, feature requests drafted for the form — then sends the rule change through QA for the maintainer to approve. Run as /grill-with-docs <feature-issue | area | Documentation/Planning/<document> | §legacy-section | nothing> on your own machine.
disable-model-invocation: true
---

You interview the user until you share one understanding, and you write down what it
settles while you go. Call the Skill tool with `grilling` before the first question and
follow it for the interview: a design tree worked in rounds, the whole frontier asked
each round, every question numbered with your recommended answer, facts looked up rather
than asked. This skill adds what to challenge, and where each settled thing is written.

Every GitHub call goes through `gh api` (REST). This skill is interactive: never run it in
a `claude -p` session, where nobody can answer.

## Before the first round

1. **Find the Global Rules issue:**
   `gh api "repos/{owner}/{repo}/issues?labels=global%20rules&state=open"`.
   - None open: the repository is new, or has just moved from file-based design. Ask the
     user to file one from the **Global rules** issue form — it carries the template's
     pre-filled `§ARC1`–`§ARC5` — and stop until they have.
     You do not file it for them.
   - More than one: stop and ask which is real.
2. **Read** that issue, `INTENT.md` if it exists, and the decision records in
   `Documentation/Decisions/`. A decision already recorded is not re-opened unless the
   user says so, knowing it is recorded.
3. **Name the scope** in one line and confirm it:
   - a feature issue (`/grill-with-docs 500`) — sharpen the request before
     `/plan-feature`, without editing what the requester wrote;
   - an area of the design (`/grill-with-docs eventing`) — the rules and terms for it;
   - a document in `Documentation/Planning/`
     (`/grill-with-docs Documentation/Planning/Design/Architecture.md`, or
     `/grill-with-docs Documentation/Planning/saved-searches-spec.docx`) — port it, as
     **Porting a planning document** below says;
   - a section, or an area, on an unstruck line of the Global Rules issue's **Legacy
     rules** (`/grill-with-docs §ARC12.5`, or `/grill-with-docs eventing`) — settle it, as
     **Porting a legacy rule** below says;
   - nothing, on a repository with no `INTENT.md` — what the system is for, then its
     rules, then its terms.
4. **Load `the-standard-core`.** Every rule you write down must sit inside The Standard,
   or say plainly that it overrides it and why.

`Documentation/Design/` and `Documentation/DependencyGraph/architecture.md` are the
releaser's output, not a source. `Documentation/Diagrams/` is the user's scratchpad —
never open it. Open
`Documentation/Planning/` only for the one document the user names to port, or for the
legacy rule sections in scope.

## Porting a planning document

A document in `Documentation/Planning/` is old intent, not a decision. Nothing in it
counts until it has been ported through an issue form, so every feature and rule is
logged the same way whatever it started as.

**A repository moved from file-based design ports its global documents first.** Its old
design documents moved, unchanged, under `Documentation/Planning/` —
`Documentation/Design/X` to `Documentation/Planning/Design/X`,
`Documentation/DesignFeatures/X` to `Documentation/Planning/DesignFeatures/X`, and a
loose design document `Documentation/<name>` to `Documentation/Planning/<name>`;
mockups, prompts and images outside the moved folders stayed where they were. Before any
new feature is planned, port the old global documents
(`Documentation/Planning/Design/Architecture.md`, `Security.md`, `Events.md`,
`Domain.md` and any like them) into the Global Rules issue, one session each, so there
are approved rules to plan and take legacy features in against. Rules too large for one
issue, or for one sitting, are filed lean instead: the old sections are indexed under
**Legacy rules** and ported area by area, as **Porting a legacy rule** below says.
`Documentation/Procedures/migrating-an-existing-repository.md` walks through the move.
Tasks signed off before the move become **legacy** features through that procedure, not
through you: never draft a feature request for work already carried by a legacy task,
and never edit the old document a legacy task cites — a change to its design is new
work, through this skill and `/plan-feature`.

1. **Read all of it** — a Word or PDF file through the skill for that format — unless
   the user names a part to port this session. Find what earlier sessions already
   ported: the feature requests whose **Ported from** field links this document, and
   the legacy features whose body links it
   (`gh api -X GET search/issues -f q='repo:<owner>/<repo> in:body "<document file name>"'`).
   List what is left before the first round: each feature it describes (something a
   person can use on its own once it ships), each rule more than one feature would
   follow, and each term. Name what is neither — history, meeting notes, superseded
   options — and leave it behind. The user may merge, split or drop items on the list;
   a feature still has to pass the planner's size check.
2. **Grill on it.** What it leaves open, and everything it says that the Global Rules
   issue, the code or an open feature contradicts. The user confirms each statement
   before it is ported; the document's say-so is not enough.
3. **Rules and terms** go into the draft of the Global Rules issue, like any other
   settled rule. If the Global Rules issue has not been filed yet, ask the user to file
   it from its form first.
4. **Each feature** becomes a Feature request, drafted field by field in the form's
   order — Summary, Who uses it, Behaviour as Given/When/Then, What must not happen,
   Mockups, Data, Done when, Out of scope, Ported from, Deadline — in the words the user
   agreed. Everything the planner needs goes in the fields: the planner never reads the
   document. **Ported from** links the document by a URL pinned to its commit; if it is
   not committed yet, commit it on your design branch and push the branch first, so the
   link resolves. Images in the document cannot go through you: name each one the
   feature needs, for the user to drag into the form's Mockups field. Hand each draft to
   the user to submit through **Issues → New issue → Feature request**, and ask for the
   issue number it got. Never file it yourself. A feature the document describes that
   already exists as a legacy feature is not drafted again.
5. **Record it.** A document gets its row in the **Ported** table in
   `Documentation/Planning/README.md` — the issues it became, who ported it, the date —
   only when everything in it is in issues: its rules and terms in the Global Rules
   issue, with the change at QA, and every feature it describes as a feature issue,
   legacy or new. Add the row on your design branch. A document ported only in part
   gets no row yet.
6. **Hand over.** Tell the user each new feature is an ordinary feature request now,
   at `status: needs-design`: the next step for each is `/plan-feature <n>`, once any
   rule change it relies on is approved, and no more than two planned ahead of the
   build.

## Porting a legacy rule

A repository moved from file-based design may hold its old rules as an index: the
Global Rules issue's **Legacy rules** section has a line for every old section, each
with its number, title and a link pinned to its commit. An unstruck line binds legacy
features only. A new feature that needs one waits until it is settled, and settling it
is your scope.

1. **Read the sections in scope** — the one named (`§ARC12.5`), or every unstruck line
   in the area named — at the `Documentation/Planning/` path each index line links. A
   section's sub-sections go with it.
2. **Grill on them** as on any document: what is still true, and everything the code,
   the approved rules or an open feature contradicts. The user confirms each rule, and
   which of three ends each line takes.
3. **Ported.** Distil the section into the draft, in the section that owns its area, as
   a `####` heading under **its own number and title**, its sub-sections keeping their
   numbers (`§ARC12.3.1`), as numbered rules a test or a reviewer can check. Never
   renumber it: legacy tasks and code comments cite those numbers. The one exception is
   `§ARC1`–`§ARC5`, reserved for the template's rules: a section using one takes the next
   free number, marked `(formerly §ARCn)`. Leave the history, the explanation and the
   superseded options in the old document. Strike the line —
   `- ~~§ARC12.5 — Feature flags — [Architecture.md](<pinned link>)~~ (ported)`.
4. **Retired.** History, a map or conventions that no longer bind anything: strike the
   line with `(retired: <reason>)`.
5. **Feature-level.** A section only one legacy feature's tasks cite is not a global
   rule: once that feature is filed, strike the line with `(feature: #<n>)`. It binds
   those tasks alone and is re-planned with them. A section tasks of two features cite is
   global: port it.
6. **Mind the size.** An issue body holds 65,536 characters. If the draft would pass
   that, stop and put it to the user — the maintainer — before any change is posted.
7. **End the session** as below: the Global Rules change, QA's global rules review, and
   the maintainer's `approved`. The feature that waited plans after that.

A line is never deleted. A rule of the repository's own that says the same thing as one
of `§ARC1`–`§ARC5` stands beside it; one that conflicts with it is a question for the
user, never a reason to drop the template's rule.

## During the session

- **Challenge against the terms.** When the user uses a word that conflicts with the
  **Terms**, say so at once: "The terms define a Bookmark as X, but you seem to mean Y.
  Which is it?"
- **Sharpen fuzzy language.** When a word is vague or overloaded, propose one precise
  term: "You're saying 'account' — do you mean the Member or the sign-in? Those are
  different things."
- **Stress-test with scenarios.** Invent concrete cases that probe the edges between
  concepts, and make the user say where each falls.
- **Cross-reference the code and the issues.** When the user states how something
  works, check whether the code and the open features agree. Surface a contradiction:
  "Your code deletes whole Collections, but you just said part of one can be removed.
  Which is right?"
- **Hold rules to The Standard.** A proposed rule that would skip a layer, mix an
  orchestration's dependencies, have an orchestration hold a broker other than the six
  utility brokers, put a query condition in a storage broker, or break the Florance
  limit is put back to the user with the rule it breaks. They may still decide it — then
  it is an explicit override, with its reason. This repository's own overrides of The
  Standard are already settled, and the Global Rules form carries them pre-filled: an
  orchestration's service dependencies are one kind, all processing or all foundation,
  never mixed; the six utility brokers — **Logging**, **DateTime**, **Identifier**,
  **Identity** (puts the caller's identity on a signed envelope), **Events** (publishes
  events or verifies the ones received) and **Access** (answers "may this person do
  this?" with a verdict, never an entity's data) — may be held by any service, an
  orchestration included, and no broker counts toward the two-to-three; and a storage
  broker authors no query condition — the foundation service writes it as a
  query-shaping function and passes it down. Do not put those back to the user as
  breaches.
- **Global or feature.** A rule only one feature needs is not a global rule. Say so and
  keep it for that feature.

## Where each settled thing goes

| What settled | Where it goes | When |
| --- | --- | --- |
| A term — this system's word for a thing | **Terms**, in the draft of the Global Rules issue | The moment it resolves |
| A rule more than one feature must follow | Architecture, Security, Events or Domain, in the draft, as the next free `§ARC`/`§SEC`/`§EVN`/`§DOM` number — a ported legacy section under its own | The moment it resolves |
| What the system is for | `INTENT.md` | The moment it resolves |
| A decision that is hard to reverse, surprising without context, and the result of a real trade-off | A decision record in `Documentation/Decisions/` | Offer it; write it when the user agrees |
| Anything about one feature only | One comment on that feature issue, at the end — or, when porting, the feature request drafted for the form | At the end |
| Everything else | The conversation, and nowhere else | — |

**The draft of the Global Rules issue.** At the start, copy the issue's body to a file
outside the repository. Edit the draft as each term or rule resolves — never save them
up for the end — and show the user each change. The issue itself changes once, at the
end, through its change process, so it stays approved and features keep planning
against it until then.

- **A term** is one or two sentences on what the thing *is*, never how it is built, with
  the words to avoid. Only this system's own concepts; general programming terms do not
  belong. Group terms under a `####` heading when clusters emerge.

  ```
  **Bookmark**: A post a member has saved to come back to later.
  _Avoid_: favourite, save
  ```

- **A rule** is a `####` heading with its `§` number and title, then numbered rules a
  test or a reviewer can check. A number is never reused; a withdrawn rule is struck
  through, not deleted; a moved rule carries `(formerly §X)`. A new repository numbers
  flat; a ported legacy section keeps the repository's own number, nested sub-section
  numbers included.

**Decision records.** Offer one only when all three hold: it is hard to reverse, a future
reader would be surprised by it without context, and there was a real alternative. Most
sessions produce none, and that is the skill working. Name it
`Documentation/Decisions/NNNN-<slug>.md`, `NNNN` one past the highest number there
(`0001` first). One to three sentences — the context, what was decided, and why — is the
whole record. Add **Status**, **Considered options** or **Consequences** only when they
earn their place. When the user turns down a proposal for a lasting reason, offer a
record of that too, so the next session does not propose it again.

When porting an old architecture document whose register recorded a deviation for a
service designed before the move — a service with no user story issue to carry it —
write that deviation as a decision record carrying a line of its own,
`**Deviation:** <ClassName> — <the rule>`, so `scripts/list-deviations.sh` finds it.

`INTENT.md` and decision records are files: write them on
`users/<handle>/design-<area>-<action>`, from `main`, committing as
`DESIGN: Description In Pascal Case`, never with AI attribution.

## Ending the session

The session is done when the frontier is empty and the user confirms you share one
understanding. Then, showing the user each step first:

1. **The Global Rules issue**, if the draft differs from it. Show the whole diff. On the
   user's go-ahead: take `approved` off the issue, `PATCH` its body from the draft, and
   post one comment — `Change <n>:` — listing every section and term added, changed or
   withdrawn, and every open feature that cites a changed section (search the open
   issues for its `§` number). Then launch a fresh `qa` at max effort with exactly
   `Act as QA, reviewing the global rules on issue #<n>.` Relay its verdict in at most
   three lines. On FAIL, settle each finding with the user in the draft and run QA
   again; stop after the third failed round. On PASS, tell the user that they — the
   maintainer — read the issue and add `approved` themselves, and that any feature
   waiting on the change plans after that.
2. **The files**, if there are any. Open one issue recording the session — what was
   settled and where each thing went — and a `DESIGN:` PR into `main` whose body says
   `Closes #<that issue>` and names the Global Rules change, if there was one. The
   maintainer merges it.
3. **A feature in scope.** Post one comment on the feature issue headed
   `Settled before planning`: each decision as a numbered line, in the words the user
   agreed. Do not edit the request itself. Then the next step is
   `/plan-feature <n>`.

## Never

- Never answer your own question, or let one "yes" stand for questions the user did not
  see.
- Never put implementation detail in a term, or a feature's rule among the global rules.
- Never add `approved` to anything — only a person adds it — and never edit a feature
  request someone else wrote.
- Never file a feature request: a person submits it through the form.
- Never add `legacy` to anything: that label belongs to the move, not to you.
