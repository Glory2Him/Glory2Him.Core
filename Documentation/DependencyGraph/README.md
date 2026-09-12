# Solution Dependency Graph

An interactive dependency graph of the Glory2Him solution: project
boundaries, per-component method blocks, and colour-coded data / event flows.

Data and renderer are separate files, all in this folder:

- [graph.yml](./graph.yml) — the manifest: solution name, project list (which
  also names each project's data file), root order, and the event registry
  (all 177 `<Entity>.<Operation>` events with their publish/subscribe row
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

or, where Python is not installed:

```bash
npx --yes http-server -p 8731 -a 127.0.0.1
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

## Current truths captured in the data (full re-scan 2026-08-21; targeted updates 2026-08-28, 2026-09-07, 2026-09-10 and 2026-09-12)

- **The collection reads are no longer the answer to every question**
  (2026-09-07). Where a caller used to take `RetrieveAll<Entity>Async`'s live
  `IQueryable`, compose a predicate onto it and run a synchronous terminal
  operator, the question is now asked of storage as a KEYED read. Fifteen
  broker members carry it — `SelectContentItemsByGroupIdAsync`,
  `SelectContentItemVersionsInGroupAsync`, `SelectContentItemInGroupAsync`,
  `SelectPublishedContentItemInGroupAsync`,
  `ExistsHigherLiveContentItemVersionInGroupAsync`,
  `ExistsContentItemContentAsync`, the four `Link` counterparts,
  `SelectAssociationByPairAsync`, `SelectOverlappingAssociationAsync`,
  `ExistsLiveAssociationOnPairAsync`, `SelectApprovalByEntityAsync` and
  `SelectApprovalReviewRequestsByApprovalIdAsync`. They are the **only**
  `StorageBroker` members whose edges land on `EXT.EFCore` rather than on
  `STC.EFCoreClient`: each queries its DbSet directly with an EF async terminal
  operator (`ToListAsync` / `FirstOrDefaultAsync` / `AnyAsync`), which is what
  carries the cancellation token to the database. `EFCoreClient` has no
  keyed-read surface, so there was nothing there to route through.
- **The processing tier's three group reads go through the foundation's
  group-keyed read** (2026-09-07). `CIP`'s `RetrieveContentItemsByGroupIdAsync`,
  `RetrieveLatestContentItemByGroupIdAsync` and
  `RetrievePublishedContentItemByGroupIdAsync` — and `LP`'s three — now call
  `FS.ContentItem.RetrieveContentItemsByGroupIdAsync` /
  `FS.Link.RetrieveLinksByGroupIdAsync`, not the foundation's collection read.
  The public group read also **mints no envelope of its own** any more: the
  group-keyed foundation read mints one to capture the ambient security
  context, and a second here would re-run the same §14.7 filter, against the
  same context, over the set that filter already produced. That is why its
  `EventEnvelopeBroker`, `SecurityAuditBroker` and `DateTimeBroker` edges are
  gone from `CIP` and `LP` while the latest/published reads keep theirs — those
  two still apply the single-row posture themselves.
- **All 114 subscriptions the data declares are drawn**, and two of them are
  `ARO`'s, added by issue #522. `EventSubscriptionRegistration` wires 121, so
  this bullet no longer reports a match — the seven-subscription drift is the
  one the closing bullet records, and it predates both orchestration splits.
  The data files first matched the registration at 108 in the 2026-08-21
  scan (the 2026-08-11 scan drew 71 against 85); the four added since are
  `ApprovalReviewRequest`'s, below. The original gap closed in two halves: the
  six approvable entities gained their `Submitting` / `Approving`
  subscriptions and their submit and approval-transition verbs, and
  `ApprovalOrchestrationService` was added with its 22 handlers.
- **`FS.ApprovalReviewRequest` is new** (2026-08-28, design §7.9 / §16.7.4) —
  the review INVITATIONS that let a moderation surface show who has been asked
  and has not yet answered. Three things make it unlike every other approval
  foundation, and all three are visible in the data: it has **no
  `IAccessBroker`** edge, because an invitation grants no eligibility and
  enters no §8.5 condition, so there is no cross-entity invariant to defend;
  it has **no Modify** method or `-Modifying` address, because `ApprovalId` and
  `RequestedUserId` are the halves of its uniqueness index and are fixed at
  creation; and its **remove path takes no `GetUserIdAsync`**, because
  withdrawal is open to the whole review tier rather than to the requester
  alone (§7.9 rule 5). Four subscriptions, four publishes, 37 direct calls.
  Its facts have no subscribers, so none of its edges are circular.
- **`FS.IdentityUser` and `IdentityCoreStorageBroker` are new** (2026-08-28, design §12.7.1) —
  Core's first read into the SECURITY database, and the first time it has had two
  DbContexts. They exist because §7.9 rule 3 and the reviewer-candidates read both ask
  about ROLE MEMBERSHIP, which lives in the ASP.NET Identity store and nowhere else:
  `ISecurityClient.Users` reads a `ClaimsPrincipal`, so it only ever describes the
  current caller. The broker is read-only by interface (Select members only, no design-time
  factory, no migrations), and `FS.IdentityUser` is the one foundation with **no**
  `EventEnvelopeBroker` and **no** `SecurityAuditBroker` edge — it writes nothing, publishes
  nothing, and who may enumerate users is decided by `ARO` before the call is made.
- **`ARO` owns the invitation flow, and `AO` no longer does** (PR #535, design §12.5.4).
  `RetrieveReviewerCandidatesAsync`, `RetrieveReviewerDisplayNamesAsync`,
  `RequestApprovalReviewAsync`, `RetrieveApprovalReviewRequestsAsync` and
  `WithdrawApprovalReviewRequestAsync` are `ApprovalReviewerOrchestrationService`'s. These are
  the operations needing BOTH stores, which is why they sit in an orchestration rather than a
  foundation, and `FS.IdentityUser` now has exactly one consumer. That refactor is what closed
  the eleven-dependency problem this section used to record as tracked separately: `AO` is back
  to three arm-bearing seams and twelve catch blocks.
- **`ARO` resolves the round in ONE read, keyed on the entity** —
  `AccessBroker.RetrieveApprovalReviewerScopeByEntityAsync`. The two-read shape the invitation
  flow used to draw (`FS.Approval.FindApprovalByEntityAsync` then
  `AccessBroker.RetrieveApprovalReviewerScopeByIdAsync`) is made by no method on any service
  now. On a null it runs the §9.7.2 rule 1 repair — hence its `RetrieveEntityApprovalStatusAsync`,
  `FindApprovalByEntityAsync` and `AddApprovalAsync` edges — and asks the gather again. The BY-ID
  gather is still drawn, and by this same component: §7.9 rule 6's subscription resolves its round
  by approval id off the envelope, so it needs no entity lookup ahead of it. It is the PAIR that
  is gone, not the read.
- **`ARO` binds TWO subscriptions and publishes nothing** (issue #522, design §12.5.4 business
  rule 4). `ApprovalReview.Added` carries §7.9 rule 6's retirement and `Approval.Modified`
  carries rule 8's — the first subscription in the solution on any of the `Approval` entity's
  own FACT addresses, the five existing ones all binding command addresses. `ApprovalReview.Added`
  therefore has two subscribers, `AO`'s re-test and this retirement: two reactions on one
  address in two services, with `Deliveries` recorded per subscription, which is not the
  double-fire §EVN2 rule 6 forbids. `AO` lost its `RetrieveApprovalReviewerScopeByIdAsync` and
  both `Retire*ApprovalReviewRequestAsync` edges with them, and with those the
  `IApprovalReviewRequestWorkflowService` seam entirely. Issue #523 splits `ApprovalsController`,
  which binds `ARO` for its five reviewer routes meanwhile.
- **`RetireAnsweredApprovalReviewRequestAsync` is the second workflow seam in
  the graph**, after `ApprovalReviewService.DismissStaleApprovalReviewAsync`,
  and it is drawn the same way: a `CreateSystemAsync` edge instead of
  `CreateAsync`, and no `InsertProcessedEventAsync` pair. It exists because
  §7.9 rule 6 retires an answered invitation under the SYSTEM identity, and
  `CreateSystemAsync` mints a context with no roles — so the public withdraw
  verb, whose gate asks for a review-tier role, cannot serve that rule. It
  publishes the ordinary `ApprovalReviewRequest.Removed` fact; what
  distinguishes a retirement from a withdrawal is recorded on the row, not on
  a separate address.
- **`RetireClosedRoundApprovalReviewRequestAsync` sits beside it** (§7.9 rule
  8): a round that closes on an outcome retires every invitation it never
  answered, because a review can no longer be recorded against a decided round
  and the row would go on rendering an ask nobody can answer. It is drawn
  identically — same `CreateSystemAsync`, same `Removed` fact, same absent
  `InsertProcessedEventAsync` pair. **Exactly ONE method reaches it, and it is
  `ARO.OnApprovalModifiedAsync`** (issue #522). The graph used to show FOUR `AO`
  methods reaching it — one per route a round can close by — and that
  enumeration is what the subscription replaced: all three closing routes write
  the outcome through `ModifyApprovalAsync`, so one subscriber on the fact that
  write publishes hears all of them and no list of call sites has to be kept in
  step. Its read is `AccessBroker`'s
  `FindRetirableApprovalReviewRequestIdsAsync` rather than the foundation's own
  round-keyed read, for the reason the dismissal's gather already carries: two
  of those routes run under the editor's or reviewer's identity, and the
  caller-facing read is filtered by §14.7 posture D.
- **`ApprovalOrchestrationService` (`AO`) is the approval workflow**, added on
  this branch (PR #289 and the workflow-record subscriptions that followed).
  It records human approve/reject decisions on the `Approval` row and
  re-evaluates a round whenever its inputs change. It deliberately holds none
  of the seven entity services: the decided state reaches its entity as an
  `<Entity>-Approving` command event published under the system identity,
  addressed to the PROCESSING tier for the two versioned types and to the
  foundation for the other five. It has no `IStorageBroker`, so no
  ProcessedEvents dedupe — its substrate guard is `IEnvelopeIntegrityBroker`
  instead. It no longer holds `IApprovalCommentService` at all — that left with
  §12.5.4's reviewer coordination in PR #535, and the only thing this service
  reads a comment for is the §8.5 count, which arrives as a verdict.
- **Circular event flows now exist, and the red edges are correct.** 14 of the
  114 subscriptions are on ENTITY fact addresses, all handled by `AO` — the two
  `ARO` gained in issue #522 are on WORKFLOW-RECORD and `Approval` fact
  addresses and are not among them, so they take no part in the cycle. `AO` publishes
  `<Entity>-Approving`, each entity publishes `<Entity>-Added` / `-Modified`
  back, and Tarjan finds one cyclic component: `AO`, `CIP`, `LP`,
  `FS.Tag`, `FS.Comment`, `FS.Reaction`, `FS.BibleReference`,
  `FS.Association`. 63 lines render red. `FS.ContentItem`
  and `FS.Link` stay out of it because `AO` addresses their processing tier.
  The `ApprovalReview` and `ApprovalComment` fact subscriptions stay purple:
  nothing `AO` publishes reaches those two services. `ARO`'s two stay purple for
  a second reason as well — it publishes nothing at all, so it can close no
  loop. The two
  `<Entity>Processing-Approved` facts added on 2026-09-07 stay purple-free
  entirely — nothing subscribes to them.
- **`EnvelopeIntegrityBroker` is new to the data.** Symmetric HMAC signing and
  verification of every envelope. It takes only `IConfiguration`, so it is a
  leaf with no outbound edges — but 17 components call it: `EventBroker` signs
  on publish and verifies on reply, and all 12 foundations, both processing
  services and BOTH orchestrations with substrate handlers verify inside them.
  The second orchestration is `ApprovalReviewerOrchestrationService`, which
  gained its two handlers in #522; the count was 16 before that.
- **`Demote<Entity>VersionAsync` is gone, and the data finally agrees**
  (removed from the YAML 2026-09-07; reversed in source 2026-08-19 by
  `4d674b7d`, #265, which derives the version tip instead of storing it).
  There is no `Demote` verb, no `<Entity>-Demoted` address and no
  `IsLatestVersion` column anywhere in `Glory2Him.Core/`. The prose above said
  so from 2026-08-21 while the data still carried the method row, seven call
  edges and a `<Entity>.Demoted` publish per versioned foundation; the publish
  drew nothing only because the manifest has no such event id, which is how it
  survived unnoticed. Modify's branch now shows what actually runs:
  `CheckHigher<Entity>VersionExistsAsync` decides whether the row is the tip,
  and the fork asks `FindHighestVersionInGroupAsync` for the next number.
- **The publication swap lives in the processing tier**, and its edges are
  drawn as of 2026-09-07. `CIP` and `LP` each carry `OnApproving<Entity>Async`,
  which clears the group's published slot through
  `FindPublishedSibling<Entity>IdAsync` + `Unpublish<Entity>ByIdAsync` before
  forwarding the promote to `Transition<Entity>ApprovalAsync`, then publishes
  its own `<Entity>Processing-Approved` fact. That handler has no public
  counterpart on the interface, so — uniquely — its publish and its foundation
  calls hang off the handler row rather than a public method.
- **`Glory2Him.WebApp` is no longer standalone.** It gained a project
  reference to `Glory2Him.Core` on 2026-08-13 (`1780e2bc`) and
  `Infrastructure/CoreRegistration.cs` registers ten Core brokers (the tenth,
  `IHashBroker`, was missing until `7a0d559a` — see below) plus all fifteen
  foundation, processing and orchestration services, the internal
  `IApprovalReviewWorkflowService` seam, and `IEventSubscriptionRegistration`.
  **Twelve** controller folders now call them directly — `AIReviewers`, `Tags`,
  `ApprovalComments`, `ApprovalReviews`, `Approvals`, `ApprovalSettings`,
  `BibleReferences`, `Comments`, `ContentItems`, `ContentItemSettings`,
  `Links` and `Reactions`; the count was four at the 2026-08-28 update.
  **None of them is modelled yet** — they would be the first webapp→core edges
  in the graph, and adding them is the next full scan's job.
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
  DbContext) and passes **itself** into G2H.StorageClient's `EFCoreClient` —
  except for the fifteen keyed reads above, which go to EF directly.
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
- **The eleven WebApp controllers** above are not modelled.
- **`AO`'s `IAccessBroker.IsEntityVisibleAsync` edges are still not drawn** — six
  live call sites (`.Decisions.cs`, `.Flows.cs`, `.Reactions.cs` twice, `.Resets.cs`
  and the verdict read in `ApprovalOrchestrationService.cs`) and zero edges in the
  data. This entry used to be scoped to the invitation reads, and PR #535 closed
  only that part of it by moving those operations to `ARO` and drawing the
  resolver's whole call set per method, the way `AIRO`'s narrow resolver already
  was. **The gap itself did not leave with them.** Drawing the six is a change to
  `AO`'s own modelling with no connection to the split that exposed it, and it
  would move the header counts above, so it is left for a pass that owns `AO`
  rather than ridden in on a reviewer-orchestration PR.
- **7 of 184 event addresses are absent from the manifest** — the whole
  `Attachment` family. They are declared on `IEventBroker` but no service
  publishes or subscribes them, so nothing would be drawn. The manifest
  otherwise carries 177 of 184, exactly the set with a producer or consumer
  (170 before `ApprovalReviewRequest` added its seven).
- The `Attachment` storage family (`IStorageBroker.Attachment.cs`, 11
  operations) is likewise unmodelled; only `SelectAttachmentByIdAsync` has a
  caller today, and it is drawn.
- **No foundation draws its `IEnvelopeIntegrityBroker` edge.** The body text
  above is right that every substrate handler verifies the envelope signature
  there, but only the two ORCHESTRATIONS declare those calls in the data —
  `ApprovalOrchestrationService`'s 22 and, since #522,
  `ApprovalReviewerOrchestrationService`'s 2. No foundation and neither
  processing service does.
  `FS.ApprovalReviewRequest` follows its siblings rather than fixing this for
  one service alone, which would make the picture less consistent, not more.
  Correcting it is a template-wide edit and belongs to a full re-scan.
- **The header counts moved again on 2026-09-12.** After #521 single copy read
  **68 components · 1406 flows**, per consumer **193 nodes · 1883 flows**.
  Purple edges were **112** in both views and **63** lines still render red in
  both — the AI reviewer and the #521 reviewer split added neither, for the
  reasons in the bullets below.

  **Issue #522 leaves single copy at 68 components · 1404 flows**, measured the
  same way rather than inferred. It moves no component. Its edge delta is **six
  direct edges added and ten removed** — the four `ARO` retirement edges plus a
  `VerifyAsync` edge for each of its two handlers, against the rule 6 hook's two
  and the rule 8 sweep's two at each of *four* `AO` call sites — and **two
  subscribe edges added**, taking purple from 112 to 114. Net two fewer flows.
  Red is unmoved, because `ARO` publishes nothing and so can close no loop.

  Counting the `AO` call sites as three is the mistake to avoid here, and an
  earlier version of this bullet made it: `ProcessApprovalInputsChangedAsync`
  drew the pair as well as the three closing routes.

  **Per consumer, #522 reads 191 nodes · 1884 flows** — measured the same way,
  by running `buildDuplicatedInstances` rather than deriving it. It cannot be
  derived: that view duplicates a dependency per consumer, so removing `AO`'s
  retirement edges removed two whole per-consumer COPIES while `ARO`'s handlers
  added edges mostly onto instances that already existed. Nodes therefore fall
  by two while flows rise by one, which is the opposite direction from the
  single-copy total and the reason this number has to be run rather than
  reasoned about. An earlier version of this bullet gave the underivability as
  grounds for leaving it unmeasured; it is grounds for measuring it, and the
  function sits in the same file as the one already being run.

  The `/update-dependency-graph` skill's own verification numbers are
  stale by three generations now and should be read from here instead.

  *Measured by running the page's own `buildSingleCopyInstances` and
  `buildDuplicatedInstances` over the data rather than read off a screenshot, so
  the two view numbers are directly comparable.*

  **The figures in this bullet go stale between full re-scans, and the mechanism
  is worth naming because it has now happened twice in two days.** A targeted
  update adds edges to `projects/*.yml` and does not touch this bullet, so the
  count drifts silently until the next update measures it. Measured against each
  revision's own renderer, single copy read 1368 flows at `29c1eb09`
  (2026-09-10), 1383 at `a94fecbf` and 1384 at `b8710b57` — both 2026-09-12,
  neither of which refreshed the header. Per consumer: 1774, 1796, 1798.
  **Every one of those records was exact when written**, including the
  2026-09-10 pair, which an earlier draft of this bullet wrongly called low by
  measuring the 2026-09-12 data against the 2026-09-10 entry. If you are checking
  these numbers, measure the revision that wrote them.
- **The AI reviewer (Berean) is modelled as of 2026-09-10** — issue #354 Track A,
  PR #475. Two new components: `FS.AIReviewerAssignment` (the foundation, whose
  `ReturnStaleAIReviewerAssignmentToPendingAsync` row is the
  `IAIReviewerAssignmentWorkflowService` seam on the same implementation, exactly
  as `FS.ApprovalReviewRequest` carries its retirement) and `AIRO`
  (`AIReviewerOrchestrationService`). `AIRO` exists because the PR review
  rejected hanging Berean off `IApprovalOrchestrationService` — that gave one
  contract two subjects — so the three invitation operations moved to their own
  service and their own controller. `AO` keeps only the PRIVATE return-to-pending
  step, drawn from `ProcessEntityModifiedAsync` and `ResetApprovalAsync`, which
  is why `ResetApprovalAsync` finally appears in `AO`'s method list.
  **`AIReviewerAssignment` draws no purple edges**: its four `On*Async` handlers
  exist for structural consistency with every sibling foundation, but
  `EventSubscriptionRegistration` has no entry for the entity, so the broker seam
  is declared and unwired. Its `EnvelopeIntegrityBroker` calls are deliberately
  NOT declared, following the 14 sibling foundations rather than fixing that
  inconsistency for one service alone (see the bullet above).
- **The reviewer orchestration is modelled as of 2026-09-12** — issue #521,
  PR #535. One new component, `ARO` (`ApprovalReviewerOrchestrationService`),
  and it is the same shape of split as `AIRO` for the same reason: one contract
  had two subjects. The five reviewer-coordination operations moved off `AO`
  with `IApprovalReviewRequestService`, `IApprovalCommentService` and
  `IIdentityUserService`, so `AO` lost sixteen call edges and `FS.IdentityUser`
  changed consumer. `ARO` holds a fourth service dependency,
  `IApprovalWorkflowService`, drawn as its `FindApprovalByEntityAsync` and
  `AddApprovalAsync` edges — that is the §9.7.2 rule 1 repair, and it is the
  approved Florance deviation §12.5's register records.
  **`ARO` draws two purple edges and no red ones** since issue #522: it binds
  the §7.9 rule 6 and rule 8 retirements as subscriptions and still publishes
  nothing of its own, because both cause their write through the foundation's
  workflow seam, which publishes for itself — which is why it holds
  `IEnvelopeIntegrityBroker` but not `IEventBroker`. `AO`'s
  `RetireAnsweredApprovalReviewRequestAsync`, `RetireClosedRoundApprovalReviewRequestAsync`,
  `RetrieveApprovalReviewerScopeByIdAsync` and
  `FindRetirableApprovalReviewRequestIdsAsync` edges left with them. Issue #523
  splits `ApprovalsController`. Like `AIRO`, it renders with zero inbound flows
  because the controller folders are still unmodelled (see the bullet below).
- **Three gaps are still open and none is this update's doing.**
  `EventSubscriptionRegistration` now wires **121** subscriptions while the data
  declares **114** — a drift of seven that predates the AI reviewer and wants a
  targeted pass of its own. And the twelve controller folders (the eleven listed
  above plus `AIReviewers`) remain unmodelled, so `AIRO` renders with zero
  inbound flows and `WA.*` still has no edge into Core. Adding them is still the
  next full scan's job; doing it for the one new controller alone would make the
  picture less consistent, not more.

  The third is **two more undrawn `AO` calls beside the `IsEntityVisibleAsync`
  ones already recorded above** — `AccessBroker.MayAmendApprovalAsync` and
  `AccessBroker.RetrieveEntityApprovalStatusAsync`. Both are in `AO`'s source on
  `main` and absent from its `calls` on `main`, so they predate this change and
  are left for the pass that owns `AO`, exactly as its sibling bullet leaves the
  six visibility reads.

  **How they were found is the transferable part.** The graph had only ever been
  checked in one direction — *does every drawn edge still exist in the code* —
  and that direction is structurally blind to an omission. Running it the other
  way, *is every code call drawn*, is what turned these up, and it is also what
  caught `ARO`'s two missing `VerifyAsync` edges in #522: the forward check
  passed on `ARO` while its own description asserted a verification the data did
  not draw. `ARO` and `AIRO` are clean in both directions now. Worth repeating on
  any component a change touches.

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
