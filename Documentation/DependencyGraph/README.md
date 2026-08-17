# Solution Dependency Graph

An interactive dependency graph of the Glory2Him solution: project
boundaries, per-component method blocks, and colour-coded data / event flows.

Data and renderer are separate files, all in this folder:

- [graph.yml](./graph.yml) — the manifest: solution name, project list (which
  also names each project's data file), root order, and the event registry
  (all 125 `<Entity>.<Operation>` events with their publish/subscribe row
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

## Current truths captured in the data (scanned 2026-08-11; LinkProcessing and the widened approval transition added 2026-08-17)

- **Glory2Him.WebApp is standalone** — it has no project reference to
  Glory2Him.Core. Its minimal-API endpoint groups use its own view services,
  brokers, Identity database, and in-memory sample data.
- **Glory2Him.Core has no controllers** — its consumption surface is the DI
  registrations plus `IEventSubscriptionRegistration.RegisterAsync`, which
  wires every event subscription (each purple line). No production host in
  this repo calls it yet.
- **The subscription counts below are stale and this snapshot is partial.**
  `EventSubscriptionRegistration` wires 85 subscriptions today; the data
  files carry 71. The gap of 14 predates the LinkProcessing addition and is
  almost entirely the state transitions rolled out to `Tag`, `Reaction`,
  `Comment`, `BibleReference`, `Link` and `ContentItem`: those six are modelled
  with CRUD and their substrate handlers only, so they carry no `Submitting` /
  `Approving` subscription and no submit or approval-transition method.
  `Association` is the one entity whose transitions are modelled. A full
  re-scan is needed to reconcile them — until then, read the counts here as a
  floor, not a total.
- **The version fork gained its own foundation operation 2026-08-17** (issue #263).
  `Demote<Entity>VersionAsync` on `ContentItem` and `Link` owns `IsLatestVersion`
  and publishes `<Entity>-Demoted`; the fork edge from each processing service
  now points at it instead of the general modify. These two are the only
  transitions the snapshot models on those entities — submit and the approval
  transition are still missing, per the note above.
- **The approval transition verb was widened 2026-08-17** (issue #198).
  `Approve<Entity>Async` became `Transition<Entity>ApprovalAsync` on all seven
  approvable entities, carrying the ordinary verdict, the `Admin` override out
  of a terminal row, and the bypass. On `Association` that folded
  `BypassApproveAssociationAsync` away: its verb, substrate handler and the
  `Association-BypassApproving` request address are gone, and
  `Association-Submitted` was added as a fact address so an override that
  re-opens a round has something to announce. Both changes are reflected here;
  the six unmodelled entities are unaffected because their transitions were
  never in the data.
- **`LinkProcessingService` (`LP`) is the second processing service**, added
  2026-08-17 alongside `ContentItemProcessingService`. Same shape minus the
  dedupe-by-hash rule and the content-type role tier, so it takes no
  `IHashBroker`. Its component block was added by hand rather than by a full
  re-scan; everything else in the data still reflects the 2026-08-11 scan.
- Core's `StorageBroker` derives from `EFxceptionsContext` (EF Core
  DbContext) and passes **itself** into G2H.StorageClient's `EFCoreClient`.
- `EventBroker` wraps EventHighway (SQL Server): one
  `Publish<Entity>Async` / `SubscribeTo<Entity>EventAsync` pair per entity;
  the operation enum selects the event address GUID. 71 subscriptions are
  drawn here, against 85 wired in the source — see the partial-snapshot note
  above.
- Approval policy is a pure decision function: `AccessClient`
  (`ISecurityClient.Access`) decides, and Core's `AccessBroker` does all the
  gathering from storage. `AssociationService`'s approval verdicts and
  `ApprovalReviewService` are its only consumers.
- `AssociationService` carries five approval state-transition verbs
  (approve, bypass-approve, sort, set-confidence, set-scope), each
  publishing its own fact; `Sort` is call-only with no request event.

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
