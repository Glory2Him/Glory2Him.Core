---
name: update-dependency-graph
description: Re-scan the solution and regenerate Documentation/DependencyGraph/graph-data.js so the interactive dependency graph matches the current source. Use when services, events, brokers, client libraries, or cross-project wiring have changed, or when the user asks to refresh/rebuild the dependency graph.
version: 0.1.0
---

# Update Solution Dependency Graph

Regenerate `Documentation/DependencyGraph/graph-data.js` from the current
source. `index.html` is the renderer — do not change it unless a new
concept cannot be expressed in data (new edge kind, new layer).

## 1/ Load the current model

Read `Documentation/DependencyGraph/README.md` and `graph-data.js` first.
The data file is the previous scan's snapshot; your job is a diff-and-update,
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
- Column map (0–11) is documented at the top of `graph-data.js` — keep new
  components consistent with it.

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

## 3/ Update graph-data.js

- New/changed foundation service → edit the `entities` config (usually one
  line: entity name + variant). A structural deviation from the template →
  extend the generator, don't special-case with hand-written edges.
- Everything else → explicit declarations: `C({...})` components,
  `D(from, to)` direct edges (`null` method = header-level link),
  `P(comp, method, event)` publishes, `S(event, comp, handler)` subscribes.
- Add new roots to the `roots` list in project order (it controls layout).
- Circular-event detection is automatic from P/S pairs — never hand-color.

## 4/ Verify in the browser

Both views read the same data: `index.html` (per-consumer duplicated) and
`index2.html` (single copy per component). Verify `index.html` fully, then
load `index2.html` once to confirm it renders without console errors.
In `index.html` confirm:

- No console errors; the header count roughly matches expectations.
- Purple edge count equals the number of subscriptions wired in
  `EventSubscriptionRegistration` (74 at last scan).
- No node-rect overlaps and no project-box overlaps (query the SVG rects
  with `javascript_tool` and intersect pairwise).
- Click one foundation service, the orchestration, and one shared client
  exposer: flows in/out in the side panel must match the scan results.
- Red edges appear ONLY if a real publish/subscribe cycle now exists — if
  one shows up, verify it against the source before accepting it.

## 5/ Finish

Update the "Current truths" section and scan date in
`Documentation/DependencyGraph/README.md`, and summarize what changed since
the previous snapshot (new components, new flows, anything that became
circular).
