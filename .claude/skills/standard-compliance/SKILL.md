---
name: standard-compliance
description: Survey the code for where it breaks The Standard and the Global Rules — a skipped layer, a mixed dependency, an orchestration holding a broker other than a utility broker, a query condition in a storage broker, an orchestration outside the Florance two-to-three, logic in a broker or exposer, identity from an ambient accessor, missing tests for a layer — present the findings as a visual HTML report, then grill through the one you pick and turn it into a feature request or a /tweak. Never changes code. Run as /standard-compliance [area] on your own machine.
disable-model-invocation: true
---

You survey this codebase for where it departs from The Standard and the Global Rules,
show the findings, and help the user turn one into work. You never change code: the
output of a run is one HTML report outside the repository, a conversation, and at most
one feature request drafted for the user to submit through the form. The fix goes through the normal workflow — `/plan-feature` and
`/build-feature`, or `/tweak`.

Every GitHub call goes through `gh api` (REST). This skill is interactive: never run it in
a `claude -p` session.

**The rules are The Standard's and this repository's, nothing else.** Many layers that
each do one small thing are the design here, not a smell: a broker is a thin wrapper on
purpose, an exposer has one dependency on purpose, and every layer has its own tests on
purpose. Never propose merging layers, deleting a layer's tests, or "simplifying" by
removing a broker or a foundation service.

## 1. Scope

- If the user named an area — a feature, an entity, a layer, a folder — take it.
- Otherwise look where the work is: walk back a good stretch of
  `git log --first-parent --name-only` and let the paths that keep changing take your
  attention first. A breach in code nobody touches costs little; one in a hot spot is
  copied into every change. If the changes are scattered, widen the net to the whole
  solution.

Then read:

- the Global Rules issue —
  `gh api "repos/{owner}/{repo}/issues?labels=global%20rules&state=open"` — its rules,
  which you check against, and its **Terms**, which name things in the report. A
  **Legacy rules** index there lists old rule sections; an unstruck line binds legacy
  features only, so you never check against one or open its document. Where a breach's
  area has an unstruck line there, say so: a feature request for it waits until that
  line is settled;
- the decision records in `Documentation/Decisions/`, which you do not re-litigate;
- the recorded deviations: `scripts/list-deviations.sh > <file outside the repository>`.
  It reads both kinds: the **Deviations** sections of user story issues, and the
  decision records in `Documentation/Decisions/` that carry a
  `**Deviation:** <ClassName> — <the rule>` line — the deviations of services designed
  before this repository moved its design into issues, which have no user story issue.

`Documentation/Design/` and `Documentation/DependencyGraph/architecture.md` are the
releaser's output, not a source. `Documentation/Diagrams/` is the user's scratchpad, and
`Documentation/Planning/` holds documents from before the issue-based workflow — never
open either.

## 2. The breaches a script can find

```bash
python3 scripts/architecture-graph.py --check --deviations <deviations file>
```

(On Windows, run it with `python`: `python3` there is often the Microsoft Store stub.)

It prints the layer graph as Mermaid — keep it for the report — and a `VIOLATION:` line
for each skipped layer, mixed orchestration, orchestration holding a broker that is not a
utility broker, orchestration outside two to three service dependencies without a
recorded deviation, exposer without exactly one dependency, broker depending on a
service, and storage broker composing a query operator or a lambda predicate. It knows
the utility brokers from `scripts/utility-brokers.txt`, the Template's seed, and
`Documentation/DependencyGraph/utility-brokers.txt`, the repository's own — one
`<ClassName> <kind>` per line, the kind one of the six below — and stops with an error on
a kind it does not know. A utility broker the code has but neither list names shows up as
a false violation: name the line to add to the repository's own list as a fix. The seed
is never edited outside the Template. It follows names, not a compiler, so open
the code behind every violation before you report it. One that is wrong is dropped, and
the reason it fooled the script goes in the report as a fix for the script.

**The utility brokers.** Six kinds of broker carry no entity's data, and any service — an
orchestration included — may hold them without breaking the same-kind or skip rules:
**Logging**; **DateTime**; **Identifier**; **Identity**, which puts the caller's identity
on a signed envelope; **Events**, which publishes events or verifies the ones received;
and **Access**, which answers "may this person do this?" with a verdict, gathering only
the facts that question needs and never handing an entity's data back. Every other
broker is off limits to an orchestration: a **storage** broker first, and any other
broker that hands an entity's data back — that goes through its foundation service. No
broker counts toward the Florance two-to-three. These deliberately override
`the-standard-orchestrations` 1.1/Don'ts#1, `ts-orchestrations-001`,
`ts-orchestrations-002` and `the-standard-core` `ts-core-003`; the override is this
repository's rule, not a breach.

## 3. The breaches that need judgement

Spawn read-only sub-agents in parallel, one per family of layers the scope touches. Give
each the files in scope, the Global Rules sections that apply, and the skills to load,
and ask for findings with `file:line`, the rule broken by name, and the evidence:

| Family | Load | Look for |
| --- | --- | --- |
| Brokers | `the-standard-brokers` | a decision in a broker — a branch, a mapping, a validation; a storage broker composing a query operator — `Where`, `Select`, `OrderBy` or any other — calling a predicate-taking terminal operator, or querying through its `DbContext`; a broker calling a service; retry anywhere but the broker (`§ARC3`); a utility broker doing entity work or handing an entity's data back |
| Foundations | `the-standard-foundations`, `the-standard-exceptions` | more than one entity in a service; structural or logical validation missing; exceptions not wrapped into the layer's own categories; identity read from an ambient accessor; a security decision that is not in the service that owns it; a query-shaping function that uses more than `System.Linq`, or a list materialised and then filtered instead of a query that runs in SQL |
| Processings and above | `the-standard-processings`, `the-standard-orchestrations`, `the-standard-aggregations` | a processing depending on anything but its own foundation; an orchestration deciding what one entity's service should; a service answering a question by running a terminal operator over the unfiltered collection read; same-kind, utility brokers and Florance where the script could not resolve a dependency |
| Exposers | `the-standard-exposers` | business logic; more than one dependency; status codes that do not follow the exception categories; an endpoint without a security test |
| Tests | `the-standard-testing` | a service missing the happy, validation, dependency or service path; a keyed read whose test does not execute its query-shaping function against a near-miss set; tests on a broker; acceptance tests that do not go through an exposer, or mock what the solution owns; data left behind |
| React, where there is an app | `the-standard-reacttypescript-review` | its own checklist |

Also hold every finding to the non-negotiables in `CLAUDE.md`: identity on the signed
envelope; migrations only ever added, each a single batch inside `§ARC1`. Naming and file
layout under `the-standard-csharp-*` are reported as drift, never as the headline.

## 4. Shape the findings

- **One finding per root cause, not per file.** Ten controllers with no security test is
  one finding with ten files.
- **Skip what is already decided.** A breach an approved deviation covers is listed in
  the report's deviations section, not as a finding. One a decision record settled is
  left out — unless it causes real friction now, and then the card says
  *"contradicts decision NNNN, worth reopening because …"*.
- **Name the remedy from the catalogue below,** in the Terms' words.
- **Give it a tier,** as the planner would: **1** if the remedy adds, moves or splits a
  service or layer, or touches schema, an event or the security boundary; **2** if it is
  new behaviour inside the existing design, such as missing tests; **3** if it is one
  file with no schema, no event and no boundary.
- **Give it a strength:** `Clear breach` when the script or the code shows it beyond
  argument; `Likely breach` when a person must confirm what was meant; `Drift` for naming
  and layout.

### Remedies

| Breach | Remedy |
| --- | --- |
| An orchestration with one service dependency | It is not an orchestration. Move it to the processing or foundation layer it really is, and rename it. |
| An orchestration with four or more | Split it into two orchestrations of two or three dependencies, each one coherent flow, under a coordination service. Never widen it. |
| An orchestration mixing processing and foundation | One kind: lift the foundation calls behind their processing services, or, if every call could be foundation, drop the processing ones. |
| An orchestration or above holding a broker other than the six utility brokers — a storage broker first | Go through that entity's foundation service. |
| A skipped layer | Go through the layer between, adding it if it does not exist. |
| A decision in a broker | Move it to the foundation service of that entity. |
| A query condition in a storage broker | Move the condition into the foundation service as a query-shaping function, using `System.Linq` only, passed down for the broker to apply and await. The query still runs in SQL, and the read's test executes the function against a near-miss set. |
| An exposer with logic or several dependencies | Push the logic into the service it fronts. Several dependencies call for one service above them. |
| Identity from an ambient accessor | Identity travels on the signed envelope; the service that owns the decision checks it. |
| A missing test path | The paths `the-standard-testing` requires, test first, one task per operation. |
| An applied migration edited, or one outside `§ARC1` | A new migration that brings the schema to where it should be. |

## 5. The report

Write one self-contained HTML file outside the repository —
`<temp dir>/standard-compliance-<timestamp>.html`, the temp dir from `$TMPDIR`, else
`/tmp`, else `%TEMP%` — following [REPORT.md](REPORT.md). If the machine has a desktop,
open it; give the absolute path either way. Also put a short table in the conversation —
title, strength, tier, files — so the user can choose without opening the file.

Do not design the fix yet. End by asking: **"Which of these would you like to work
through?"** and stop.

## 6. Grill the chosen one

Call the Skill tool with `grilling`, and walk the tree with the user:

- Is it really a breach, or what was meant? If there is a good reason, it may be a
  deviation instead — see step 7.
- Which remedy, and its shape: which services split where, what the coordination service
  is called, what moves to which layer. New names come from the Terms; a concept the
  Terms lack is noted for `/grill-with-docs`, not invented here.
- What each layer's tests become: tests move with the code they test, new layers get
  their own, and nothing loses coverage.
- What it touches: migrations, events, the security boundary, other features' code.
- The order to build it in, from the bottom up, and the tier.

## 7. Turn it into work

Every feature request goes through the form, so it is logged like any other:

- **Tier 1 or 2:** a feature request drafted field by field in the **Feature request**
  form's order — Summary, Who uses it, Behaviour, What must not happen, Data, Done when,
  Out of scope. Its Behaviour says what must keep working exactly as it does; its Done
  when says the breach no longer appears in this survey or the layer check. Hand it to
  the user to submit through **Issues → New issue → Feature request**; never file it
  yourself. The next step is `/plan-feature <n>`.
- **Tier 3:** the request for `/tweak`, worded for the user to run.
- **Rejected for a lasting reason:** record it so the next survey does not raise it
  again. A layer-rule breach in a service is a deviation — the line format is in
  `scripts/list-deviations.sh`. Where the service has a user story, the planner adds it
  there through its change process; where it has none, it goes in a decision record in
  `Documentation/Decisions/` on a `DESIGN:` PR, which the maintainer merges. Skip
  reasons that will not last ("not now").

One finding per session. The others stay in the report for another run.

## Never

- Never edit code, tests or configuration, and never open a PR that does.
- Never propose fewer layers, a merged layer, or deleting a layer's tests.
- Never add `approved` or `ready for development` to anything.
