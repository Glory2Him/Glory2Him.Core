# Solution Dependency Graph

An interactive dependency graph of the Glory2Him solution: project
boundaries, per-component method blocks, and colour-coded data / event flows.

Data and renderer are separate files, all in this folder:

- [graph.yml](./graph.yml) — the manifest: solution name, project list (which
  also names each project's data file), root order, and the event registry
  (all 170 `<Entity>.<Operation>` events with their publish/subscribe row
  labels).
- `projects/*.yml` — one file per project / package boundary, each declaring
  that project's components with their methods, outbound calls, publishes and
  subscriptions.
- [index.html](./index.html) — the renderer. It fetches the manifest and the
  project files, assembles them, and draws. No build step, but because the
  data is fetched the page must be **served** rather than double-clicked:

```bash
python -m http.server 8731 --bind 127.0.0.1
```

then open `http://127.0.0.1:8731/`.

It carries two ways of drawing the same data, switched from the segmented
control in the header:

- **single copy** *(default)* — every component appears exactly once with its
  full method surface, and all consumers' flows converge on it (the one
  StorageBroker shows all 72 per-entity method rows). Best for "who touches
  this?".
- **per consumer** — dependencies are duplicated once per consumer, each copy
  showing only the method rows that consumer uses. Best for "what does this
  one call path actually do?".

The choice lands in the URL (`#single` / `#duplicated`), so a link keeps the
view you were on, and switching carries your current selection across.

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
- **Duplication over line-spaghetti** (the *per consumer* view only, matching
  `Glory 2 Him.drawio`): a dependency is drawn once per consumer, showing only
  the method rows that consumer uses, instead of many lines converging on one
  shared node. The exception is client-library / external exposers (marked
  "shared" in the side panel): all consumers link to a single copy. In the
  *single copy* view nothing is duplicated, so the `shared` flag makes no
  difference there.
- **Click a method row** to trace that single method's path — the full
  upstream + downstream slice (through duplicated copies, publishes and
  subscriptions) lights up and everything else dims.
- Every flow is method-to-method: arrows land on the target's method row,
  including on shared library/external exposers, whose boxes list exactly
  the public surface this solution calls (derived from the edges, verified
  against source).
- **Click a component header** for the same slice seeded from *every* row of
  that copy at once: the component's whole fan-out, not just its first hop.
  Other copies of the same component stay half-lit so you can find them.
- Whatever is selected is outlined and lettered in **amber**; rows the traced
  path passes through carry a faint blue tint. Click the background or Reset
  to clear. Search finds components and methods. The **utility brokers**
  toggle reveals the DateTime / Identifier / Logging / Hash broker copies
  that are hidden by default for readability.

## Current truths captured in the data (full re-scan 2026-08-21)

- **All 108 subscriptions are now drawn.** `EventSubscriptionRegistration`
  wires 108 and the data files carry 108 — the first time these have matched
  since the 2026-08-11 scan, which drew 71 against 85. The gap closed in two
  halves: the six approvable entities gained their `Submitting` / `Approving`
  subscriptions and their submit and approval-transition verbs, and
  `ApprovalOrchestrationService` was added with its 22 handlers.
- **`ApprovalOrchestrationService` (`AO`) is the approval workflow**, added on
  this branch (PR #289 and the workflow-record subscriptions that followed).
  It records human approve/reject decisions on the `Approval` row and
  re-evaluates a round whenever its inputs change. It deliberately holds none
  of the seven entity services: the decided state reaches its entity as an
  `<Entity>-Approving` command event published under the system identity,
  addressed to the PROCESSING tier for the two versioned types and to the
  foundation for the other five. It has no `IStorageBroker`, so no
  ProcessedEvents dedupe — its substrate guard is `IEnvelopeIntegrityBroker`
  instead. `IApprovalCommentService` is injected but currently unused.
- **Circular event flows now exist, and the red edges are correct.** 14 of the
  108 subscriptions are on fact addresses, all handled by `AO`. `AO` publishes
  `<Entity>-Approving`, each entity publishes `<Entity>-Added` / `-Modified`
  back, and Tarjan finds one cyclic component: `AO`, `CIP`, `LP`,
  `FS.Tag`, `FS.Comment`, `FS.Reaction`, `FS.BibleReference`,
  `FS.Association`. 21 pub/sub pairs — 42 lines — render red. `FS.ContentItem`
  and `FS.Link` stay out of it because `AO` addresses their processing tier.
  The `ApprovalReview` and `ApprovalComment` fact subscriptions stay purple:
  nothing `AO` publishes reaches those two services.
- **`EnvelopeIntegrityBroker` is new to the data.** Symmetric HMAC signing and
  verification of every envelope. It takes only `IConfiguration`, so it is a
  leaf with no outbound edges — but 16 components call it: `EventBroker` signs
  on publish and verifies on reply, and all 12 foundations, both processing
  services and the orchestration verify inside their substrate handlers.
- **`Demote<Entity>VersionAsync` is gone** — reversed 2026-08-19 by
  `4d674b7d` (#265), which derives the version tip instead of storing it.
  There is no `Demote` verb, no `<Entity>-Demoted` address and no
  `IsLatestVersion` column anywhere in `Glory2Him.Core/`. The fork edge from
  each processing service now points at `FindHighestVersionInGroupAsync`.
- **The publication swap lives in the processing tier.** `CIP` and `LP` each
  gained `OnApproving<Entity>Async`, which clears the group's published slot
  through `FindPublished<Entity>IdByGroupAsync` + `Unpublish<Entity>ByIdAsync`
  before forwarding the promote to `Transition<Entity>ApprovalAsync`, then
  publishes its own `<Entity>Processing-Approved` fact. That handler has no
  public counterpart on the interface, so — uniquely — its publish and its
  foundation calls hang off the handler row rather than a public method.
- **`Glory2Him.WebApp` is no longer standalone.** It gained a project
  reference to `Glory2Him.Core` on 2026-08-13 (`1780e2bc`) and
  `Infrastructure/CoreRegistration.cs` registers ten Core brokers (the tenth,
  `IHashBroker`, was missing until `7a0d559a` — see below) plus all fifteen
  foundation, processing and orchestration services, the internal
  `IApprovalReviewWorkflowService` seam, and `IEventSubscriptionRegistration`.
  Four OData controllers (`Tags`, `ApprovalComments`, `ApprovalReviews`,
  `Approvals`) call them directly. **None of those four is modelled yet** —
  they would be the first webapp→core edges in the graph, and adding them is
  the next scan's job.
- **The substrate is live, and `RegisterAsync` is no longer test-only.**
  `Program.Configurations.cs` calls it at startup (`RegisterCoreEventSubstrateAsync`),
  so the 108 listeners and 166 addresses are registered in the running host
  rather than only under test. Handlers resolve **per delivery** through an
  `IServiceScopeFactory` — not as method groups captured by the singleton
  broker, which is how they were bound before. Any service the substrate
  reaches must therefore be resolvable from a scope, and a service that is not
  fails mid-delivery rather than at boot: `IHashBroker` was unregistered while
  `ContentItemProcessingService` carried five subscriptions.
- Core's `StorageBroker` derives from `EFxceptionsContext` (EF Core
  DbContext) and passes **itself** into G2H.StorageClient's `EFCoreClient`.
- `EventBroker` wraps EventHighway (SQL Server): one
  `Publish<Entity>Async` / `SubscribeTo<Entity>EventAsync` pair per entity;
  the operation enum selects the event address GUID.
- Approval policy is a pure decision function: `AccessClient`
  (`ISecurityClient.Access`) decides, and Core's `AccessBroker` does all the
  gathering from storage. `IAccessBroker` now carries 9 methods and has eight
  foundation consumers plus the orchestration — not the two the previous
  snapshot named.
- `AssociationService` carries four approval state-transition verbs
  (transition, sort, set-confidence, set-scope), each publishing its own fact;
  `Sort` is call-only with no request event. The bypass folded into
  `TransitionAssociationApprovalAsync` on 2026-08-17 (#198).

### Known gaps in this snapshot

- **`AssociationOrchestrationService`** (`Services/Orchestrations/Associations/`,
  added 2026-08-12) is not modelled. It has no events, so it does not affect
  the subscription count.
- **The four WebApp controllers** above are not modelled.
- **7 of 177 event addresses are absent from the manifest** — the whole
  `Attachment` family. They are declared on `IEventBroker` but no service
  publishes or subscribes them, so nothing would be drawn. The manifest
  otherwise carries 170 of 177, exactly the set with a producer or consumer.
- The `Attachment` storage family (`IStorageBroker.Attachment.cs`, 11
  operations) is likewise unmodelled; only `SelectAttachmentByIdAsync` has a
  caller today, and it is drawn.

## The data files

All data is declarative YAML — no code runs to produce the model, and
[index.html](./index.html) is a pure renderer (it holds both views,
`buildSingleCopyInstances` / `layoutBands` and `buildDuplicatedInstances` /
`layoutTrees`, dispatched on `state.view`, and should rarely need changes).

**`graph.yml`** is the manifest: `projects` (id, name, kind, data file — list
order controls the single-copy band order), `roots` (per-consumer layout
order; `shared` components **must** appear here or their inbound edges are
dropped), `events` (every event id with its publish/subscribe row labels)
and `eventBroker` (the EventBroker component id).

**`projects/<name>.yml`** declares one project's components:

```yaml
- id: FS.ContentItem
  name: ContentItemService
  layer: foundation
  col: 5                  # layout column — map documented in graph.yml
  shared: true            # optional: consumers link to ONE copy
  utility: true           # optional: hidden behind the header toggle
  deriveMethods: true     # optional: rows derived from inbound edges
  description: "..."
  methods: [...]
  calls:
    - from: <method or null>   # null = header-level link
      to: <component id>
      method: <method or null>
  publishes:
    - method: AddContentItemAsync
      event: ContentItem.Added
  subscribes:
    - event: ContentItem.Adding
      handler: OnAddingContentItemAsync
```

Strings containing anything beyond letters, digits, spaces, `_.-/()` are
double-quoted JSON strings — the renderer parses a deliberately small YAML
subset, so stick to the shapes above (single-line scalars, no anchors, no
multi-line blocks). Circular-event detection stays automatic: if a publish
and a subscribe ever meet on the same event id across a component cycle,
those lines turn red.

## Updating the graph

The data is a scanned snapshot of the source, not a build artifact — refresh
it whenever services, events, or cross-project wiring change by running the
`/update-dependency-graph` skill in Claude Code (defined in
`.claude/skills/update-dependency-graph/SKILL.md`). It re-scans the solution,
diffs against the current data files, updates them, and re-verifies the
rendered graph. The 14 templated foundation services are now fully expanded
in the data — a new one is a replicated sibling block plus its events in the
manifest, and bulk template-wide changes are a throwaway script over the
YAML.
