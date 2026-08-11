# Solution Dependency Graph

An interactive, self-contained dependency graph of the Glory2Him solution:
project boundaries, per-component method blocks, and colour-coded data /
event flows. No build step and no server — open either view directly in a
browser (they cross-link and share the same data and interactions):

- [index.html](./index.html) — **duplicated view**: dependencies are drawn
  once per consumer, showing only the method rows that consumer uses.
- [index2.html](./index2.html) — **single-copy view**: every component
  appears exactly once with its full method surface, and all consumers'
  flows converge on it (e.g. the one StorageBroker shows all 72 per-entity
  method rows).

## Reading the graph

- **Left → right layering**: exposers → view services → orchestrations →
  foundations → brokers → client libraries → external services.
- **Dashed boxes** are project / library boundaries. In-solution libraries
  (G2H.Security.Client, G2H.StorageClient, G2H.EventEnvelope.Client) show
  their internal components; external packages show only the public surface
  that this solution calls.
- **Edge colours**:
  - **blue** — direct method call
  - **green** — event publish (service → its EventBroker copy)
  - **purple** — event subscribe (EventBroker copy → the handler it invokes)
  - **red** — a publish/subscribe pair that participates in a circular event
    flow (none exist today: services subscribe to request events —
    `…ing` — and publish fact events — `…ed` — and the fact addresses have
    no subscribers)
- **Duplication over line-spaghetti** (matching `Glory 2 Him.drawio`): a
  dependency is drawn once *per consumer*, showing only the method rows that
  consumer uses, instead of many lines converging on one shared node. The
  exception is client-library / external exposers (marked "shared" in the
  side panel): all consumers link to a single copy.
- **Click a method row** to trace that single method's path — the full
  upstream + downstream slice (through duplicated copies, publishes and
  subscriptions) lights up and everything else dims.
- Every flow is method-to-method: arrows land on the target's method row,
  including on shared library/external exposers, whose boxes list exactly
  the public surface this solution calls (derived from the edges, verified
  against source).
- **Click a component header** to see the whole block's direct flows
  in / out instead. Click the background or Reset to clear. Search finds
  components and methods. The **utility brokers** toggle reveals the
  DateTime / Identifier / Logging / Hash broker copies that are hidden by
  default for readability.

## Current truths captured in the data (scanned 2026-08-11)

- **Glory2Him.WebApp is standalone** — it has no project reference to
  Glory2Him.Core. Its minimal-API endpoint groups use its own view services,
  brokers, Identity database, and in-memory sample data.
- **Glory2Him.Core has no controllers** — its consumption surface is the DI
  registrations plus `IEventSubscriptionRegistration.RegisterAsync`, which
  wires all 74 event subscriptions (every purple line). No production host in
  this repo calls it yet.
- Core's `StorageBroker` derives from `EFxceptionsContext` (EF Core
  DbContext) and passes **itself** into G2H.StorageClient's `EFCoreClient`.
- `EventBroker` wraps EventHighway (SQL Server): one
  `Publish<Entity>Async` / `SubscribeTo<Entity>EventAsync` pair per entity;
  the operation enum selects the event address GUID. 68 subscriptions are
  wired in total.
- Approval policy is a pure decision function: `AccessClient`
  (`ISecurityClient.Access`) decides, and Core's `AccessBroker` does all the
  gathering from storage. `AssociationService`'s approval verdicts and
  `ApprovalReviewService` are its only consumers.
- `AssociationService` carries five approval state-transition verbs
  (approve, bypass-approve, sort, set-confidence, set-scope), each
  publishing its own fact; `Sort` is call-only with no request event.

## Updating the graph

The data is a scanned snapshot of the source, not a build artifact — refresh
it whenever services, events, or cross-project wiring change by running the
`/update-dependency-graph` skill in Claude Code (defined in
`.claude/skills/update-dependency-graph/SKILL.md`). It re-scans the solution,
diffs against the current data, updates `graph-data.js`, and re-verifies the
rendered graph.

For small changes you can also edit by hand: all data lives in
[graph-data.js](./graph-data.js) (`window.G2H_DATA`);
[index.html](./index.html) is the renderer and should rarely need changes.

- The 14 foundation services follow one template and are generated from the
  `entities` config (entity name + read-path variant A/B/C/D). A new
  foundation service is usually one added line.
- Everything else (WebApp, orchestration, client libraries) is declared
  explicitly with `C(...)` components, `D(from, to)` direct edges,
  `P(component, method, event)` publishes and `S(event, component, handler)`
  subscribes.
- Component options: `col` (layout column), `utility: true` (hidden behind
  the toggle), `shared: true` (consumers link to one copy instead of
  duplicating).
- Circular-event detection is automatic: if a publish and a subscribe ever
  meet on the same event id across a component cycle, those lines turn red.
