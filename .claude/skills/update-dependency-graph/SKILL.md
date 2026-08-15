---
name: update-dependency-graph
description: Re-scan the solution and regenerate the dependency graph data files (Documentation/DependencyGraph/graph.yml + projects/*.yml) so the interactive dependency graph matches the current source. Use when services, events, brokers, client libraries, or cross-project wiring have changed, or when the user asks to refresh/rebuild the dependency graph.
version: 0.1.0
---

# Update Solution Dependency Graph

Regenerate the data files — `Documentation/DependencyGraph/graph.yml` and
`projects/*.yml` — from the current source. `index.html` is the renderer — do not change it unless a new
concept cannot be expressed in data (new edge kind, new layer). It carries
BOTH views behind `state.view`: `buildSingleCopyInstances` + `layoutBands`
(the default) and `buildDuplicatedInstances` + `layoutTrees`. Anything you
change in one builder usually needs the mirror change in the other.

## 1/ Load the current model

Read `Documentation/DependencyGraph/README.md`, then `graph.yml` and the
`projects/*.yml` files it lists. The data files are the previous scan's
snapshot; your job is a diff-and-update,
not a rewrite. Preserve its modeling rules:

- Per-consumer duplication is done by the renderer — declare each component
  ONCE; never hand-duplicate.
- `shared: true` only on client-library / external exposers (consumers link
  to one copy). `utility: true` on DateTime / Identifier / Logging / Hash
  brokers (hidden behind a toggle).
- Happy-path calls and read-path denial logging are drawn; exception-path
  (TryCatch) logging is NOT.
- Substrate `On*Async` handlers get only their ProcessedEvents-dedupe edge +
  purple subscribe edges; they delegate to the same `Do*Async` path as the
  public methods (say so in the component description).
- Publishes attach to public mutating methods; events use ids like
  `<Entity>.<Operation>` (`ContentItem.Added`). Request ops = `Adding`,
  `Modifying`, `RemovingById`, `HardRemovingById`, `RetrievingById`;
  fact ops = `Added`, `Modified`, `Removed`, `HardRemoved`.
- Column map (0–11) is documented in `graph.yml` — keep new components
  consistent with it.
- The foundation-service template knowledge (variants A–D below) guides the
  SCAN; the data itself is fully expanded YAML with no generators. When a
  change touches many components the same way (a new broker call in every
  templated service, a new entity's full CRUD block), write a throwaway
  script against the YAML rather than hand-editing dozens of entries.

## 2/ Re-scan the source (parallel Explore agents)

Fan out read-only exploration; each agent returns compact JSON:

1. **Foundation services** — `Glory2Him.Core\Services\Foundations\*`:
   folder list, public interface methods, per-method broker calls
   (Storage / SecurityAudit / EventEnvelope / Event / DateTime / Identifier /
   Logging), substrate handlers, publishes. Services are templated: classify
   each into read-path variant A (publish-date visibility + ownership),
   B (ownership only), C (admin-gated, storage-only reads), D (fully public
   reads, no envelope) — or flag a genuine deviation.
2. **Orchestrations / processings / coordinations + events** —
   `Services\Orchestrations\*` (deps, per-method calls, publishes),
   `Brokers\Events\EventBroker*.cs` (entities, operations),
   `Registrations\EventSubscriptionRegistration.cs` (every subscription:
   operation → handler). Also grep the solution for any new `SubscribeTo` /
   `Publish` call sites outside these.
3. **Cross-project surface** — every `.csproj` ProjectReference/PackageReference;
   public surfaces + internals of `G2H.Security.Client`, `G2H.StorageClient`,
   `G2H.EventEnvelope.Client`; which Core brokers consume them;
   `Websites\Glory2Him.WebApp` endpoint groups → view services → brokers →
   externals; whether WebApp now references Glory2Him.Core (today it does NOT
   — if that changed, it is the headline update).

## 3/ Update the data files

The YAML schema is documented in the README's "The data files" section —
components live in `projects/<project>.yml`, each with `methods`, outbound
`calls` (`from: null` = header-level link), `publishes` (method + event) and
`subscribes` (event + handler); manifest-level lists (`projects`, `roots`,
`events`) live in `graph.yml`.

- A new component → add it to its project's file AND to `roots` in
  `graph.yml` (project order; `shared` components must be roots).
- A new templated foundation service → replicate an existing sibling of the
  same variant (its full block: methods, calls, publishes, subscribes), and
  register the entity's events in `graph.yml`'s `events` list.
- Externals with `deriveMethods: true` get their rows derived from inbound
  edges at load time — never hand-list rows on them.
- Circular-event detection is automatic from publish/subscribe pairs — never
  hand-color.
- Strings with characters beyond letters, digits, spaces and `_.-/()` must be
  double-quoted JSON strings; the renderer parses a small YAML subset
  (single-line scalars only, no anchors, no multi-line blocks).

## 4/ Verify in the browser

`index.html` carries both views. Serve the folder over HTTP first — the page
fetches `graph.yml` and the project files, and browsers block those fetches
from `file://` pages. Verify BOTH views — the header toggle, or
`window.__graph.setView("single")` / `window.__graph.setView("duplicated")`
from `javascript_tool` (the renderer exposes `window.__graph` = { state,
setView, select, selectRow, clearSelection, rebuild, fit, tracePath }).
Confirm:

- No console errors; the header count roughly matches expectations (last
  scan: 60 components · 1033 flows single-copy; 119 nodes · 1226 flows per
  consumer).
- Purple edge count equals the number of subscriptions wired in
  `EventSubscriptionRegistration` (74 at last scan) — in both views.
- No node-rect overlaps and no project-box overlaps (query the SVG rects
  with `javascript_tool` and intersect pairwise), in each view.
- Switching view preserves the selection (by component id).
- Click one foundation service, the orchestration, and one shared client
  exposer: flows in/out in the side panel must match the scan results.
- Selecting a header must light the component's whole fan-out (the same
  upstream + downstream slice a method row gets, seeded from every row), not
  just its first hop, and the selection must be outlined in amber. Clearing
  the selection must restore the graph exactly — snapshot every node's
  attributes before and after and compare.
- Red edges appear ONLY if a real publish/subscribe cycle now exists — if
  one shows up, verify it against the source before accepting it.

## 5/ Finish

Update the "Current truths" section and scan date in
`Documentation/DependencyGraph/README.md`, and summarize what changed since
the previous snapshot (new components, new flows, anything that became
circular).
