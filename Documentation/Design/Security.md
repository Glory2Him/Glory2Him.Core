# Security

Carries `G2H Design.md` §14 "Visibility Rules" and §18 "Authentication and
Authorisation" — the read-visibility rules, the enforcement posture applied at
every layer, and the authentication, identity and role design that posture is
built on.

Section numbers below carry a **`SEC` prefix** and are otherwise the numbers
these sections already had: an old `§14.N` is now `§SEC14.N` and an old `§18.N`
is now `§SEC18.N`, and nothing was renumbered, reordered, merged or split in
the move. That is the prefix-preserving rule of §1.5. It means this file jumps
from `SEC14` straight to `SEC18` with nothing between, so its numbering is not
contiguous with the other area files, and the contents list below is what makes
that gap read as a table of contents rather than as missing content.
**§SEC14.6.1 carries a gap of its own kind, at a different scale**: it stood as
`### 14.6.1` — a third-level number at heading level 3 — in `G2H Design.md`,
and it stays at heading level 3 as `### SEC14.6.1` rather than being promoted
to match its number; the anomaly is preserved, not tidied (§1.5, the same
precedent already applied to §ARC16.7.5). The other `Documentation/Design/*.md`
files carry their own prefixes — `ARC`, `APR`, `DOM`, `EVN`, `UI` — so a bare
`§SEC14.7` is unambiguous once they exist. Where this file cites one of them
ahead of its own existence, the map at the top of
[`G2H Design.md`](../G2H%20Design.md) is what resolves it.

**`Events.md` is the one file a citation into it cannot be derived for.** It
renumbered rather than prefix-preserved, so an into-Events citation is looked
up in that file's own *(formerly §10.X)* annotations instead of having a
prefix applied to the number it already had. The citations this file carries
into it — `§EVN18` and its lettered form `§EVN18(a)`, in §SEC14.6 and
§SEC14.7 — were resolved by looking up `Events.md`'s `## EVN18.` heading,
which carries *(formerly §10.17)*, and `§EVN10.17` would have been the wrong
answer.

This repository's C# and TypeScript comments cite design sections
extensively, so every relocated section also carries a *(formerly §14.X)* or
*(formerly §18.X)* annotation naming its old position. The literal old string
still appears on the right heading, so an old citation resolves by grep even
though the citable number is now prefixed.

**Contents**

- [SEC14. Visibility Rules](#sec14-visibility-rules-formerly-14)
  - [SEC14.1 Canonical Content Visibility](#sec141-canonical-content-visibility-formerly-141)
  - [SEC14.2 Feed Visibility](#sec142-feed-visibility-formerly-142)
  - [SEC14.3 Association Visibility](#sec143-association-visibility-formerly-143)
  - [SEC14.4 Topic Visibility](#sec144-topic-visibility-formerly-144)
  - [SEC14.5 Denial Posture and Audit Logging](#sec145-denial-posture-and-audit-logging-formerly-145)
  - [SEC14.6 Security Enforcement in Every Layer](#sec146-security-enforcement-in-every-layer-formerly-146)
    - [SEC14.6.1 Dependency Lifetimes Are a Security Control](#sec1461-dependency-lifetimes-are-a-security-control-formerly-1461)
  - [SEC14.7 Per-Entity Security Rules](#sec147-per-entity-security-rules-formerly-147)
- [SEC18. Authentication and Authorisation](#sec18-authentication-and-authorisation-formerly-18)
  - [SEC18.1 Purpose](#sec181-purpose-formerly-181)
  - [SEC18.2 Technology Selection](#sec182-technology-selection-formerly-182)
  - [SEC18.3 ASP.NET Core Identity](#sec183-aspnet-core-identity-formerly-183)
    - [SEC18.3.1 A Username Is Never an Email Address](#sec1831-a-username-is-never-an-email-address-formerly-1831)
  - [SEC18.4 OpenIddict](#sec184-openiddict-formerly-184)
  - [SEC18.5 Scope Design](#sec185-scope-design-formerly-185)
  - [SEC18.6 Role Design](#sec186-role-design-formerly-186)
  - [SEC18.7 Authentication Flow](#sec187-authentication-flow-formerly-187)
    - [SEC18.7.1 Web App (Cookie Auth)](#sec1871-web-app-cookie-auth-formerly-1871)
    - [SEC18.7.2 API (JWT Bearer)](#sec1872-api-jwt-bearer-formerly-1872)
    - [SEC18.7.3 Two-Factor Authentication](#sec1873-two-factor-authentication-formerly-1873)
    - [SEC18.7.4 External Login Providers](#sec1874-external-login-providers-formerly-1874)
  - [SEC18.8 Authorisation Policies](#sec188-authorisation-policies-formerly-188)
  - [SEC18.9 Phased Adoption](#sec189-phased-adoption-formerly-189)
  - [SEC18.10 Future Token Claims Example](#sec1810-future-token-claims-example-formerly-1810)
  - [SEC18.11 Architecture](#sec1811-architecture-formerly-1811)

---

## SEC14. Visibility Rules *(formerly §14)*

### SEC14.1 Canonical Content Visibility *(formerly §14.1)*

A content item is visible only when:

```csharp
contentItem.DeletedWhen is null
&& contentItem.ApprovalStatus == ApprovalStatus.Approved
&& contentItem.IsPublished
&& (
    contentItem.PublishDate is null
    || contentItem.PublishDate <= utcNow
)
```

### SEC14.2 Feed Visibility *(formerly §14.2)*

The feed is a projection of visible content items.

A content item appears in the feed only when:

1. The content item is visible according to canonical content visibility.
2. The content item `ContentType` is not `Topic`.

The feed is ordered by:

1. `PublishDate DESC`, if present.
2. `CreatedWhen DESC` as fallback.

### SEC14.3 Association Visibility *(formerly §14.3)*

An association is visible only when:

1. The association is not soft deleted.
2. The association approval status is `Approved`, if approval is required.
3. **Both** endpoints are not soft deleted.
4. **Both** endpoints are visible under their own entity's §SEC14.1 rule — not deleted, approved if approval is required, and published if their publish date has passed.
5. `Association.PublishDate` is null or has passed.
6. The effective settings for each host endpoint allow the association to be shown (§DOM6.10).

Rules 3 and 4 replace the earlier "the associated entity" and "the parent content item is visible", both of which assumed one endpoint was always a `ContentItem`. Under symmetric endpoints there is no parent, and the driving case — `BibleReference` ↔ `BibleReference` — has no content item at all.

**Layer.** Rules 3, 4 and 6 span more than one entity, so they cannot be evaluated by the association's foundation service, whose reads touch only its own table. The foundation keeps a self-only filter covering rules 1, 2 and 5; the composite rule belongs to an orchestration or aggregation service that can resolve both endpoints. A public read surface must therefore bind to that service, not to the foundation's collection read.

### SEC14.4 Topic Visibility *(formerly §14.4)*

A topic page is visible only when:

1. The topic content item is visible according to canonical content visibility.
2. The topic content item has `ContentType = Topic`.

Topic children are visible only when:

1. The topic is visible.
2. The child content item is visible.
3. The topic-child association is visible.

### SEC14.5 Denial Posture and Audit Logging *(formerly §14.5)*

When a caller requests an entity they are not allowed to see, the system uses a **no-existence-leak** posture:

1. A non-visible entity is reported as **not found — never as unauthorized**. An unprivileged probe must not be able to distinguish a non-public entity (draft, submitted, rejected, unpublished, future-scheduled) from an entity that does not exist.
2. The caller-facing error carries **no reason**: exception messages and the exception `Data` dictionary surface outward to callers, so neither may ever contain the denial reason, the entity's state, or the caller's identity.
3. A soft-deleted entity is not found for **every** caller, including `Administrators` — review and audit reads cover the approval workflow, not takedowns.
4. Collection reads apply the same posture by **filtering**: rows the caller may not see silently drop out of the set instead of producing an error, so a collection read never reveals how many non-public rows exist.

So that debugging and audit remain correct despite the deliberately opaque outward answer, **the true denial reason must always be logged server-side, immediately before the generic error is thrown** — and only there:

1. Privilege denials (an anonymous caller, or an authenticated caller who is neither the owner nor in a review role, requesting a non-public entity) are logged as **warnings**, including the entity id and — when resolved — the denied user's id. These are the security-relevant events: repeated warnings for one caller indicate probing.
2. State-based misses (soft-deleted entity requested; a group with no non-deleted latest or published version) are logged as **information**, including the entity or group id.
3. The log message states the real reason and notes that the caller was answered with not-found, e.g. `Content item read denied. Content item {id} is not publicly visible and user "{userId}" is neither the owner nor in a review role; reported to the caller as not found.`

This posture and its logging rule apply to every read surface — by id, latest/published per group, and collection reads — and to both the direct and event (substrate) paths, which converge on the same do-work methods.

### SEC14.6 Security Enforcement in Every Layer *(formerly §14.6)*

An exposer (controller, page, or any other host) may bind to a foundation service, a processing service, or an orchestration service directly — there is no guarantee that a request passes through any particular layer. Therefore:

1. **Every service enforces security itself.** Each service — foundation, processing, and orchestration — applies authentication, role, ownership, and visibility rules against the ambient `SecurityContext` (captured on its own inbound envelope) for every operation it exposes. No service ever assumes an upstream layer already gated the caller.
2. **Duplicate enforcement across layers is intended** (defense in depth). An orchestration re-checking a rule its foundation also checks is correct, not redundant: either service must be safe when called alone.
3. **Each layer enforces the rules appropriate to its altitude.** Foundations enforce row-level rules — the contribution gate (authenticated, not blocked by a `ReadOnly` role), row write permission (owner or moderation role; removal by owner or `Administrators`; hard removal by `Administrators` only), and read visibility (§SEC14.1, §SEC14.5). Orchestrations additionally enforce process rules that span rows or states — for example that an `Approved` content item is amended only by its owner and only by forking a new version.
4. **The same rules apply on both entry paths, and on the event path a verifying signature is what makes the carried `SecurityContext` admissible.** The direct method path and the event (substrate) path converge on the same do-work methods, so every rule above is enforced on both. What differs is the provenance of the context they are enforced against. `EnvelopeIntegrityBroker` computes an HMAC-SHA256 over the event name, the direction, and `Content`, `SecurityContext`, `RequestContext` and `Metadata`, excluding `Integrity` itself because it holds the result. `EventBroker.PublishEventAsync` signs every request under the `Request` direction before submitting it and every handler reply under `Reply`, and `VerifiedReplyOrNullAsync` drops a reply that does not verify rather than returning it as authentic. Each receiving service verifies in its own `Validate*EventEnvelope` method — against the event name that handler serves and the request direction — before it does anything else. **Verification sits in the receiver, not in the transport**, because a handler is reachable without going through the broker; that is also why `EventBroker.DeserializeEnvelope` is still a bare `JsonSerializer.Deserialize<EventEnvelope<T>>(content)!`. Deserializing is not the trust decision, and a check placed there is one a direct handler call walks past.

   **Be precise about what a valid signature establishes.** It proves provenance and integrity of what was signed: this system minted this envelope, for this event name, in this direction, and no signed field has changed since. It establishes nothing about whether a signed section is present or sensible — a verifying envelope may still carry a null or empty `SecurityContext`. So the receivers null-check it and then run the ordinary contribution, role, ownership and visibility gates against it exactly as the direct path does. The `Validate*EventEnvelope` methods themselves require only that `Content` and `Metadata` are present and that the signature verifies; they decide no identity question at all.

   The participant is still registered with `IsSecretRequired = false`, so the substrate does not authenticate whoever submits an event to the store. That is not what the system rests on. The trust decision is made by the receiver, on the envelope inside the event content, and a submitter without the signing key cannot produce an envelope that verifies.

   **Replay is answered in two places, and the cover is uneven.** `Metadata` is inside the signed payload, so a replay cannot be handed a fresh `EventId` without breaking the signature. Behind that, the write-effecting foundation substrate handlers deduplicate on `Metadata.EventId` and receiver name through `ProcessedEvents`, so a re-delivered envelope is a no-op for them. Not every receiver keeps that record. Read-only handlers deliberately keep none, a read being naturally idempotent, and `ContentItemSettingOrchestrationService` probes for a duplicate itself before handing the same envelope down to the foundation handler that keeps the record. The processing services and `ApprovalOrchestrationService` keep none at all: the approval handlers re-derive the round from stored state, so a redelivery re-reaches the same conclusion, and a replayed processing add is absorbed by the §DOM3.4.2 duplicate-content probe. The remaining processing commands rest on that re-derivation argument rather than on a record of their own, which is the weaker half of this and the first place to look if a redelivery is ever seen to double an effect.

**That is the tier rule rather than a tally of services, and two further subscriber sets are built on it.** Above the foundation nothing writes or checks `ProcessedEvents` at all — the reasons are `Documentation/Design/Events.md` §EVN19 rules 1 and 4's, and they are structural rather than per-service: an orchestration holds no storage broker to write the record with (§ARC12.5), and it has no transaction of its own to commit it in. `ApprovalReviewerOrchestrationService`'s two subscriptions (§ARC12.5.4 business rule 4) and `AIReviewerOrchestrationService`'s two (§APR8.6.2.1) therefore keep no record, and neither is exempt from anything by doing so. Both pairs are **built** (#522 and #532) and both rulings are already recorded. What stands in the record's place in each case is a signed status gate read before any gather plus a gather or presence check that finds nothing left to do on a second delivery. That is the same footing §EVN18(d) already rests on, stated once here so the list above is not read as an inventory somebody must extend per service.

   **`ApprovalOrchestrationService`'s fact receivers are guarded by the signature alone.** Its handlers route through two shared methods that verify and then hand the row's identity to the flow; `ProcessEntityAddedAsync` validates the shape of its arguments and nothing else, and `ProcessApprovalInputsChangedAsync` only reacts. No role check, no ownership check and no `ProcessedEvents` record stands behind either. The two things that look like guards there are not caller checks: the entity type is supplied by the handler rather than read off the payload, so a `Tag` fact cannot drive a `ContentItem`'s approval, and a fact carrying the system identity is dropped so the workflow does not react to its own writes. Verification is the load-bearing guard on that path, not one of several.

   **What a lost key would cost, through a mechanism that is otherwise correct.** A single `SecurityContextPrincipalFactory` feeds both the actor `AccessBroker` sends to `IAccessClient` and the `CreatedBy` that `SecurityAuditBroker` stamps — deliberately, because HR-1 and HR-2 are `actor == CreatedBy` comparisons and two conversions would disagree in the permissive direction (§APR8.6.1). One subject therefore both authors the row and answers the self-review and self-approval comparisons *against itself*. That is right while the subject is the real caller, and it is why the envelope's context has to be established before any rule reads it: anyone able to mint a verifying envelope would not be weakening the rules, they would be having them evaluated against a subject of their own choosing, with the audit trail agreeing. This is the concrete cost of the signing key leaking, and the reason the key is the asset to protect rather than any single gate.

   **The signature is the whole of the guard now, because the circumstantial ones are gone.** Two of them used to stand behind it and neither survives. `Glory2Him.WebApp` wires the substrate at startup: `Program.RegisterCoreEventSubstrateAsync` resolves `IEventSubscriptionRegistration` and registers the participant, every event address and every subscription, so the handlers that were dormant are live in the only deployment there is. And published traffic is no longer past-tense notifications only: `ApprovalOrchestrationService.PublishEntityApprovalCommandAsync` publishes `-Approving` **commands** to request addresses, which is how the workflow's decision reaches the entity at all. Request addresses are live and carry commands, and a receiver's only evidence that a command came from this system is that its envelope verifies. Read every enforcement claim on the event path as resting on that, and treat anything that would let a second party sign — a shared key, a federated publisher — as a change to the security boundary rather than to configuration. **The remediation is built.** `IEnvelopeIntegrityBroker` signs on publish and verifies on receive, and each receiving handler verifies before it does anything else — in the receiver rather than the transport, because a handler is reachable without going through the broker. The signature binds the event name, the direction, and the three carried sections plus content, so an envelope cannot be lifted onto another address, replayed as a reply, or edited in any part the rules read.

   Because only this system holds a signing key, a verified envelope is one this system produced, which is what lets the event path be trusted with the same claims as the direct path. Signing happens internally, after an API has authorised the caller; verification is there to detect tampering. That equivalence is a property of there being exactly one key holder, and it is the assumption to revisit first if that ever stops being true.
5. **Denials follow §SEC14.5**: reads answer not-found with the true reason logged server-side; writes answer unauthorized (revealing a write denial leaks nothing the caller did not already assert).

Cross-row rules under visibility filtering: because the entity-returning collection reads are visibility-filtered per caller, a cross-row rule must never be computed over them. Instead the foundation exposes a **boolean probe** for such a rule — `CheckContentItemContentExistsAsync(contentTypeId, contentHash, excludedGroupId)` for the duplicate-content rule (§DOM3.4.2) — which queries the unfiltered store but returns only a yes/no answer. A boolean reveals no row data: the caller must already possess the exact content to probe it, and the duplicate rule already reveals "identical content exists" to submitters. The probe still carries the contribution gate (it exists to support contribution flows), and this is the pattern for any future global rule: filtered reads for entities, gated boolean probes for cross-row facts.

The media surface carries its own security rules: upload constraints — refused SVG, magic-bytes sniffing over the declared MIME, mandatory re-encode stripping EXIF/GPS, and a per-user quota — are defined in §DOM5.6.3, and the `/media` visibility gate, which follows the §SEC14.5 posture, in §DOM5.6.2.

### SEC14.6.1 Dependency Lifetimes Are a Security Control *(formerly §14.6.1)*

Every rule in §SEC14.6 is evaluated against a `SecurityContext` derived from the caller's
`ClaimsPrincipal`, and every audit field is stamped from the same subject (§APR8.6.1). **One broker
in that chain still resolves that principal in its constructor, not per call; the other now takes
it as an explicit per-call argument.** Where a broker still captures it in its constructor, that
broker's registered lifetime decides *whose* identity the rules run against, which makes DI
lifetime a security control rather than a performance choice for it.

Two brokers sit in that chain:

1. `SecurityAuditBroker` takes the actor as an explicit `SecurityContext` argument on the calls
   that need one (via `SecurityContextPrincipalFactory`) rather than capturing a principal in its
   constructor — it stamps `CreatedBy`, `UpdatedBy`, `DeletedBy` and their timestamps that way. The
   one member that needs no actor at all, `EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync`,
   only copies fields between two entity instances. Its registration stays
   `Scoped` regardless, as a deliberate security margin rather than a strict requirement: it holds
   no per-request state today, but a future constructor addition that captured one would
   reintroduce the hazard described below, and a `Scoped` lifetime keeps that margin in place
   should that happen (`CoreRegistration.AddCoreServices`).

   **RULE — the audit columns name the actor, and the system is an actor.** They are resolved from
   `SecurityContext.SubjectId`, so whatever that holds is what the row says happened. An act the
   system performs on its own account — an approval opened because content was submitted, a round
   re-approved because its conditions came to be met, an invitation retired because the person
   answered it (§APR7.9 rule 6), a review dismissed because the content moved under it (§APR9.5) — is
   minted through `CreateSystemAsync` and records `SystemIdentity.UserId`. An act the workflow
   carries out *for* a person, which is the manual approve or reject and nothing else, is minted
   through `CreateElevatedAsync` and records the person. Both drop roles; the difference is only
   whose name the row carries. The triggering person is kept on `DelegatedBySubjectId` either way,
   so the causal trail survives without the audit column claiming somebody acted who did not.

   The caller names the **act**, never an identity, so it can only ever elect to be recorded as
   itself — the system flag stays unforgeable by construction rather than by validation (§ARC16.7.1).

   `Approval` is the one entity the system owns outright: it opens the row itself, so
   `Approval.CreatedBy` records the system and never a person. Ownership questions about an
   approval therefore anchor on the **entity's** author, which is what §SEC14.7 posture D rule 3 means
   by the submitter — anchoring them on `Approval.CreatedBy` would refuse every author their own
   resubmission, silently, since a submitter holds no role to fall back on.
2. `EventEnvelopeBroker` constructs an `EventEnvelopeClient`, which builds its own service
   provider and resolves `IEventEnvelopeService` **once**, and the `SecurityBroker` beneath that
   reads `HttpContext.User` in *its* constructor. This one is easy to miss: the capture is an
   assembly away, in `G2H.EventEnvelope.Client/Brokers/Securities/SecurityBroker.cs`, behind a
   parameterless constructor that looks stateless — `EventEnvelopeClient` builds its own
   `ServiceCollection` and resolves `IEventEnvelopeService` once, which pins that broker and the
   principal it captured for the lifetime of the client. It supplies the `SecurityContext` on every envelope, which is what the foundation
   authorises against.

**Registering a broker that still captures ambient identity in its constructor — `EventEnvelopeBroker`'s
chain today — as a singleton freezes the first principal the process ever saw.** The failure is
silent and total: the service keeps enforcing every rule correctly, but against the wrong subject.
Every subsequent caller's row is authored by that first user; ownership checks, the §APR8.6.1
`actor == CreatedBy` comparisons, the no-self-approval rule (HR-2) and the whole audit trail are
all decided for someone who is not the caller. Nothing throws, and no test that excludes the audit
fields from its assertions will notice.

**The rule:** any broker in the identity chain — and any service that composes one — is `Scoped`
or `Transient`, never `Singleton`. A longer-lived consumer of a scoped identity broker is the
same defect wearing a different hat.

This collides with `ServiceRegistration.Add*Service()`, which registers foundation services as
**singletons** deliberately, so `EventSubscriptionRegistration` can bind substrate handlers into
the singleton `IEventBroker` as method groups. That trade is only sound in a host that actually
wires those subscriptions. **A host that exposes a service over HTTP and wires no subscriptions
must not use those helpers** — it registers the service and its request-bound brokers scoped
itself, as `CoreRegistration.AddCoreServices` does. Only the genuinely stateless brokers
(`IDateTimeBroker`, `IIdentifierBroker`, `IHashBroker`, `IEnvelopeIntegrityBroker`,
`IEventBroker`) stay singletons there.

Because the failure is invisible to behavioural tests, **the guard is a registration test that
asserts the lifetime directly** — see `CoreRegistrationTests.ShouldRegisterRequestBoundServicesAsScoped`.

### SEC14.7 Per-Entity Security Rules *(formerly §14.7)*

The §SEC14.6 mandate is applied per entity according to what the entity is. Four postures cover every foundation entity; each service documents its posture in its class XML doc and enforces it on all six CRUD surfaces (Add, RetrieveAll, RetrieveById, Modify, RemoveById, HardRemoveById), on both entry paths.

**A. User-contributed approvable content** — `ContentItem`, `Association`, `Tag`, `Reaction`, `Comment`, `BibleReference`, `Link` (and `Attachment` when implemented):

1. Contribution gate on writes: authenticated and not blocked by `ReadOnly`, by `%EntityType%-ReadOnly`, or — for `ContentItem`, the one entity type carrying a content type — by `ContentItem-%ContentType%-ReadOnly`. The three are a **veto**, asked before any grant and overridden by none of them, `Administrators` included (§SEC18.6 rule 2). On an add the content type comes off the incoming row, which is safe because `ContentType` is create-only; on every other write path — modify, submit, the approval transition, unpublish, remove and hard remove — it comes off the **stored** row. Unpublish is in that list and asked no block role at any tier before #366: it is the write that takes published content off the site, so a sanction stopping the reversible acts and not that one would be the wrong way round. The publication swap's own system identity is exempt, holding no roles by construction (§APR9.7.7 rule 7).
2. Review roles: global `Reviewers` / `Publishers` / `Administrators` plus `%EntityType%-Reviewers` / `%EntityType%-Publishers` (§SEC18.6).
3. **Modify: owner (`CreatedBy`) or the `Publishers` tier** — the global `Publishers` / `Administrators`, `%EntityType%-Publishers`, and on `ContentItem` `ContentItem-%ContentType%-Publishers`. **The `Reviewers` tier is not in it, at any scope.** A reviewer reviews: they cast approval reviews and write approval comments, and that is the whole of their authority over somebody else's row. Rewriting the text underneath the verdict they are about to cast is HR-3 on the other surface — and this gate used to permit exactly that while HR-3 refused them the status one field away. *(This replaces "owner or review role", which described the behaviour before this ruling.)* The review tier keeps every **read** it had: rule 4 and §SEC14.5 still admit it to non-public rows and to audit, because a reviewer must be able to see the draft they are reviewing. Only the modify gate moves off the review role.

   **The non-owner branch is bounded by status, and the owner branch by §DOM3.4 rule 16.** The publisher tier and `Administrators` may amend only while the row is `Draft` or `Submitted` — the in-flight window §SEC18.6 already granted for `Submitted`, now widened to `Draft`. Once a row is **`Approved` or `Rejected` it is terminal to them**: no in-place amendment, whatever the role. It is terminal to its **owner** too — a `Versioned` entity forks a new version instead (§APR9.2, §EVN18), a non-versioned one is refused outright and the only route back is the §APR8.6 HR-4 override. The status is read from the **stored** row, never from the caller's copy: a caller who could supply it would self-certify their way past the terminal bar.

   The `Draft` ↔ `Submitted` carve-out (§APR9.2 rules 4–6) is untouched by all of this. It is a status transition rather than an amendment, and it already runs through the owner or the publisher tier — which is now the same set the modify gate admits.

   Remove: owner or `Administrators` (a takedown, not a moderation step — checked before the idempotent already-deleted short-circuit). Hard remove: `Administrators` only.
4. Reads: the §SEC14.1 public-visibility rule; non-public rows answer not-found to everyone but the owner and the review roles (§SEC14.5). Collections: review roles see all non-deleted rows; authenticated callers see public plus their own; anonymous callers see public only.

**A′. `Association` — the endpoint-derived variant of posture A.** An association has no scoped roles of its own; every scoped question is answered from its two endpoints, using only the columns on the row (§SEC18.6):

1. **Contribution gate.** Blocked by the global `ReadOnly`, **or** by either endpoint's `%EntityType%-ReadOnly`, **or** by either endpoint's narrow `ContentItem-%ContentType%-ReadOnly` composed from its denormalised content type — four scoped names in all, two per end, and all four compose from the row alone. **The `OR` is load-bearing.** Under an `AND`, a user holding `Tag-ReadOnly` alongside `BibleReference-Reviewers` could pair a tag with an entity type they are not banned from and land it on a public scripture page — exactly what `Tag-ReadOnly` exists to prevent. A block on one end blocks the association.

   **On ADD the endpoint content type is the caller's, and the veto fails closed rather than trusting it.** The value is derived from the resolved endpoint by the orchestration — §SEC18.6 says it is "derived on write and never accepted from a caller" precisely because it is an authorization input — but this service is single-entity and may not resolve an endpoint to derive it for itself (§SEC14.3), so add validation admits a null. A null on a `ContentItem` endpoint therefore means the narrow tier cannot be *decided*, not that it does not apply, and anyone that tier covers is refused. Without it, omitting the field on the public `Association-Adding` address would step around every narrow block there is — no lie needed, and no knowledge of which content types the sanction covers. **What remains open is a declared but FALSE content type on that same address**, which needs the endpoint resolved to detect; it is the same exposure the narrow *grant* already carries on both read paths (rule 6), and it is recorded here rather than papered over. The orchestration path is unaffected either way — it overwrites both values from the resolved endpoints before the foundation gate runs again beneath it.

   **The `OR` runs in both directions, and a `Series`–`Quote` row is the case to reason from** — both ends are content items carrying different content types, so all four narrow names are in play at once. On the **grant** side one end is enough to admit (rule 2): requiring both would leave every cross-type association unreviewable by anyone short of a global role. On the **block** side one end is enough to bar: `ContentItem-Series-ReadOnly` refuses the holder that association even though they hold `ContentItem-Quote-Reviewers`, and the reverse refuses them just the same. One end admits; one end bars.
2. **Review roles.** A global `Reviewers` / `Publishers` / `Administrators`, **or** a scoped role matching *at least one* endpoint. Each endpoint is checked at both tiers: the coarse `%EntityType%-Reviewers` / `-Publishers`, and the narrow `%EntityType%-%ContentType%-Reviewers` / `-Publishers` from the denormalised endpoint content type. One endpoint is enough because the pairing is the thing under review and the reviewer can see both ends of it; requiring both would leave every cross-type association unreviewable by anyone short of a global role.

   **Modify takes posture A.3's wording, composed from the endpoints.** The gate is the owner or the endpoint-derived **`Publishers` tier** — a global `Publishers` / `Administrators`, or a `%EntityType%-Publishers` / `%EntityType%-%ContentType%-Publishers` matching at least one endpoint. The review tier is absent from it and keeps its reads, exactly as in posture A. The terminal bar binds here too: `Approved` and `Rejected` admit no in-place amendment from anyone, and an association never forks, so refusing the write **is** the enforcement.
3. **The veto is scoped to writes — and the approval OUTCOME is one of them.** `Approval`, `ApprovalReview`, `ApprovalComment` and `ApprovalReviewRequest` have no role vocabulary of their own — there is no `Approval-Reviewers` — so their scope is derived from the attached entity, which for an association means both endpoints. A block in scope stops the holder **casting or changing a review, deciding the approval, and amending the approval record**, and it drops them from the reviewer candidates so nobody can invite a person who cannot answer (§APR7.9 rule 3, §ARC16.7.4). It does **not** reach the comment thread's **words** — adding, amending and withdrawing a comment are stopped by the global `ReadOnly` alone; §SEC18.6 rule 2 records why. It **does** reach `IsResolved`, which is the one comment field that moves a §APR8.5 gate: `ResolveApprovalCommentRequest` carries the subjects, and the veto is asked ahead of the author branch (§SEC14.7 posture D rule 5).

   **Reads stay exempt, and that is the one thing the veto still does not touch.** §SEC18.6 defines `ReadOnly` as a contribution block, so a moderator holding a scoped `ReadOnly` keeps **audit visibility** — they can still see the row and its approval history; they simply cannot write it, vote on it, or decide it. The review-role check and both read paths never consult it.
4. **The gate splits on the remove and hard-remove paths.** Removal is handed an id, not an association, so the endpoint half of the veto cannot run until the row is loaded. Authentication and the global block still run first, so an anonymous or globally blocked caller never reaches the `Associations` table and cannot use these surfaces to probe which association ids exist. (On the event path a deduplication lookup against `ProcessedEvents` precedes the gate; it is keyed on the event id, not the association id, so it reveals nothing about which rows exist.) Hard removal is `Administrators` only **and** subject to the same endpoint veto — a block that stopped the reversible takedown but not the irreversible one would be the wrong way round.
5. **The collection read filter resolves its sets in memory first.** It composes an expression tree and has no row to inspect, so the caller's reviewable entity types and content types are resolved in C# and the resulting sets are closed over; `Contains` over a local collection translates to `IN (...)`, and both enums persist as strings so the converted values are parameterised. A caller with no scoped roles gets two empty sets and the query degrades to exactly the public-plus-own predicate.
6. **The narrow tier tests the endpoint type as well as the content type — on both read paths.** Only `ContentItem` carries a content type (§SEC18.6 rule 5), and the foundation refuses one on any other endpoint, so it is tempting to match the content type alone. That rule lives in the service, not the schema: no check constraint ties the column to an `EntityType` of `ContentItem`, so a row arriving by migration, backfill or direct SQL is not bound by it. Matching on the content type alone would hand a `ContentItem-Testimony-Reviewers` a `Tag` endpoint carrying `Testimony`, while the single read — which composes the role from both halves of the endpoint, and so asks for the never-granted `Tag-Testimony-Reviewers` — refuses the same row. The bulk path must not be the more permissive of the two.

**Approval and publication now have a code path.** `TransitionAssociationApprovalAsync` owns the whole of `IApproval` — `ApprovalStatus`, `IsPublished` and `PublishDate` move together, so approve and publish are one operation and there is no separate publish verb. It is the **only** path that writes the three fields: add still refuses a caller-supplied `IsPublished`, `PublishDate` or non-`Draft`/`Submitted` status, and the general modify still pins all three against storage. The public clause on both read paths is therefore reachable, and rules 3 and 5 above describe live behaviour rather than a caveat.

It requires the endpoint-derived `Publishers` tier and refuses a row that is `Draft` or `Dismissed`, so a `Draft` cannot skip the submission the workflow is built around — the bypass included, because what a bypass waives are the §APR8.5 approval *conditions*, never the requirement that there be a submission to decide on. A row that is already `Approved` or `Rejected` is admitted but only as an **override**, which needs `Administrators` or the workflow's system identity (§APR8.6 HR-4). The bypass is narrower than the verb that carries it: it may only accompany a target of `Approved`, because there is no bypass-reject and no bypass-reopen, and it is the one request that ever *sets* the pair `IsApprovedByBypass` / `ApprovedByBypassReason` — which is written from the verdict, never from the caller (§APR9.7.5).

**The five state transitions and who may call them.** The general modify is content-only; every other field group has its own narrow operation that owns exactly its own fields and publishes its own fact. That separation is the approval workflow's cycle-breaker — the workflow subscribes to `-Modified` and causes `-Approved`, so a transition publishing `-Modified` would re-enter the handler that caused it. `ProcessedEvents` cannot break it: that table is keyed on the event id and a write-back mints a fresh one, so under inline dispatch the repetition is synchronous re-entry inside the originating request.

| Operation | Field scope | Who may call it | Publishes |
| --- | --- | --- | --- |
| `TransitionAssociationApprovalAsync` | all of `IApproval`, plus `IsApprovedByBypass` / `ApprovedByBypassReason` as a request | the **`Publishers` tier** — global `Publishers`/`Administrators` or `PublishersFor(endpoint)` — and never the row's own `CreatedBy` (HR-2), unless an `Administrators` is requesting a bypass; **or** a system identity minted in process. Out of a stored `Approved`/`Rejected` it is an override, and then `Administrators` or the system identity only (HR-4). A bypass request additionally needs an access decision (§APR8.6.1) that permits it, which repeats the tier check, re-applies HR-3, re-applies HR-2 to everyone but an `Administrators` (whose bypass over their own row is HR-2's one exception), and refuses outright when `DoNotAllowBypassingSettings = true` | `Association-Approved` on approval — including a bypass, never a fact of its own (§APR9.7.5) — `Association-Rejected` on rejection, `Association-Submitted` on an override that re-opens the round |
| `SortAssociationAsync` | `SortOrder` only | owner, `Administrators` | `Association-Sorted` |
| `SetAssociationConfidenceAsync` | all four `IConfidence` fields, as one unit | `Publishers`, `Administrators` — **never the owner** | `Association-ConfidenceSet` |
| `SetAssociationScopeAsync` | `EntityAScope` / `EntityBScope` | `Publishers`, `Administrators` | `Association-Scoped` |
| `SetAssociationDefaultAsync` *(designed, not built — §DOM4.9)* | `IsDefault` only | not yet ruled — `Administrators` until ruled, the conservative reading (§DOM4.9 rule 4); refuses any target not `Approved` | `Association-DefaultSet` |

Submission is deliberately absent: it is the `Draft` ↔ `Submitted` carve-out on the general modify (§APR9.2 rules 4–6), not an operation of its own. Five things about the table are load-bearing rather than incidental. **Every transition is a write**, so the whole of rule 1's veto applies to all of them before anything is read — the global `ReadOnly`, each endpoint's `%EntityType%-ReadOnly`, and each endpoint's narrow `ContentItem-%ContentType%-ReadOnly`. **Authorization is decided against the STORED endpoints**, never the caller's copy — the endpoint content type is an authorization input, so trusting the caller's would be self-certification. **Set-confidence excludes the owner**, and that exclusion is the operation's whole point: a contributor who could score their own association defeats scoring. **Set-scope's `Publishers`/`Administrators` restriction is what justifies scope changes not re-opening approval** — only the people who would be re-approving it can make one — so widening that gate would invalidate the no-reapproval rule, not merely loosen a policy. **And approve admits neither a reviewer nor — barring an `Administrators` bypass — the author** — HR-3 keeps the decision out of reviewers' hands entirely, and HR-2 keeps it out of the author's; together they are what stop this, the first path by which an association becomes publicly visible, from being a path a contributor can walk end to end alone. A third exclusion joins them, and it is now live: §APR8.6 regardless-rule 1 also bars anyone but an `Administrators` holding an active `ApprovalReview` on the row, which is HR-3 restated by act rather than by role and catches the `Publishers` who files the single required review and then decides on it. It arrives through `IAccessBroker` — §APR8.6.1 records why it cannot be answered row-locally.

Sort takes an anchor and a side rather than a target index, because a pairwise swap cannot express a drag. Values are sparse (100, 200, 300 …) and landing beside an anchor is a half-step away, which at the default spacing is the midpoint between the anchor and its neighbour — so exactly one row is written and the operation stays single-entity. Ties are legal and fall through the §DOM11.7 tie-break chain. Sort is the one transition with no request address: its signature needs a second entity and an envelope carries one, so it is direct-call only and publishes its fact like the others. Set-scope re-runs the same duplicate check an add does, because a scope toggle recomputes the effective id and can move the row onto a key `UX_Associations_Pair` already holds.

**Known gap — now closed on the write paths, still open on the reads.** `ApprovalService`, `ApprovalReviewService` and `ApprovalCommentService` identify a reviewer **row-locally** by generic suffix match (`role.EndsWith("-Reviewers")`), so on that check alone a bare `Tag-Reviewers` would reach the *approval record* of a `ContentItem` ↔ `BibleReference` association that rule 2 above refuses them on the association itself. Every write path **that admits a scoped review role** now re-asks that question through `IAccessBroker` against the entity behind the approval — `MayRecordApprovalReviewAsync` (add/modify/remove of a review), `MayAmendApprovalAsync` (the approval record), and the three comment gates. The write paths that are **not** routed through it admit no scoped role for the endpoint rule to narrow: `Approval` add is the contribution gate, its remove is owner-or-`Administrators`, and every hard remove is `Administrators`-only — and `Administrators` clears every tier. The paragraph below singles out `ApprovalReviewService`'s hard remove as un-routed, which is true — but by the same reasoning it costs nothing, because that path is `Administrators`-only too and no scoped role can reach it. What remains open is the **read** posture: rule 1's owner-or-review-role visibility is still decided row-locally, so a `Tag-Reviewers` can still *see* an association's approval, its reviews and its comment thread. Narrowing reads is a separate question from narrowing writes, and is not covered by the work above.

**`ApprovalReview` has since closed this on its own paths**: tier 2 resolves the entity behind the approval and matches the exact composed role for it, so the suffix match is now the coarse first half of a two-tier check rather than the whole of it (§APR8.6.1, §ARC12.3.1). The gap survives wherever a write is **not** routed through `IAccessBroker` — including `ApprovalReviewService`'s own hard-remove path, which takes no access decision at all. Recorded here rather than fixed with the endpoint rules.

**A″. `Attachment` — the referencing-host variant of posture A** *(designed, not built — §DOM5.6)*. Writes follow posture A unchanged. Reads widen rule 4 by one admit: a non-public attachment additionally answers to reviewers or publishers of an entity whose row references it — through a §DOM4.9 purposeful association or a §DOM5.6.6 inline body reference — so a host's reviewer sees its draft images in context (§DOM5.6.2 rule 2). The referencing host's state is read directly from the host row, never inferred from an association row's existence or approval — §SEC14.3's composite rule is implemented nowhere yet (§ARC12.5 entry 1), and this gate must not repeat that gap.

**B. Reference data** — `ContentType`:

1. All writes, including hard removal: `Administrators` only. No owner branch — only admins author reference data.
2. Reads: §SEC14.1 public visibility for everyone; non-public rows are visible to `Administrators` only. Collections: `Administrators` sees all non-deleted rows; everyone else sees public rows only.

**C. Configuration** — `ApprovalSetting`, `ContentItemSetting`:

1. All writes, including hard removal: `Administrators` only.
2. Reads of the approval-policy entities require an authenticated caller (any signed-in user may see the rules their submissions run under); anonymous callers get not-found / an empty set. `ContentItemSetting` is public-read (effective settings drive rendering for anonymous visitors). In both cases only non-deleted rows are visible; there is no §SEC14.1 approval-visibility concept.

**D. Approval workflow records** — `Approval`, `ApprovalReview`, `ApprovalComment`:

1. These records are never public. Reads: owner (`CreatedBy`) or a review role; everyone else gets not-found (§SEC14.5). Collections: review roles see all non-deleted rows; authenticated callers see their own; anonymous callers see an empty set.
2. Because these entities carry no entity-type scoping row-locally, the **row-local** check accepts the global review roles plus any granular role following the `%EntityType%-Reviewers` / `%EntityType%-Publishers` convention. Enforcing that the granular role matches the approval's target `EntityType` **lives in the foundation, one tier down, through `IAccessBroker`** — which can read the entity behind the approval where a row-local check cannot; for an `Association`, that means either endpoint (posture A′ rule 2). This was previously described as an orchestration (process-level) rule, which is withdrawn: §ARC12.3.1 gives `ApprovalReview` and `ApprovalComment` no orchestration to defer to, and §SEC14.6 rule 1 requires every service to gate its own callers. Both tiers run, and §SEC14.6 rule 2 makes the duplicate intentional — a defect in the gathering can only ever make the pair stricter.

   **The `ReadOnly` veto splits across the same two tiers, for the same reason.** An `Approval` carries an `EntityType` and an `EntityId` but **no content type**, and a foundation may not resolve the entity behind it (§SEC14.3) — so tier 1 keeps the global `ReadOnly` check it has always had, and the **scoped** block belongs at tier 2, in `IAccessClient` behind `IAccessBroker`, where the `RoleSubject` list is already resolved. The subject list serves both readings: holding a matching role for any one subject **admits** on the grant side, and holding a block for any one subject **bars**. Unlike the tier checks, the veto is evaluated *before* eligibility and cannot be satisfied by a wider role.
3. `Approval`: add/modify/remove gate is the global contribution gate; modify by owner or review role (resubmission by the submitter, status transitions by reviewers); remove by owner or `Administrators`; hard remove `Administrators` only. **"Status transitions by reviewers" excludes the two outcome statuses.** Moving an approval *into* `Approved` or `Rejected` is applying the §APR8.6.1 decision, and that additionally requires the **`Publishers` tier** (HR-3: reviewing is vouching, deciding is deciding), asked through `IAccessBroker.MayDecideApprovalByIdAsync` on top of this gate. Everything else the gate admits stays open to the review tier and to the submitter — resubmitting, for instance — because no transition matrix constrains this verb beyond the outcome gate and the standing refusal of `Dismissed`; it is authorization, not a state machine, that narrows it. **Reopening a decided round is NOT among them.** This sentence previously named it, which contradicted §APR9.4 and §APR9.6 ("only an administrator re-opens a decided one") and would have let the submitter undo a rejection of their own work. Moving a round out of `Approved` or `Rejected` is the §APR8.6 HR-4 override and is `Administrators` alone, whether it is reached through the entity's transition verb or through `ResetApprovalAsync` (§ARC16.7.5). This sentence previously read as though a reviewer could decide through the general modify, which was the behaviour before that gate existed (§APR9.7.5).
4. `ApprovalReview`: adding requires a review role (§APR8.9 — only reviewers review); a review is its reviewer's own verdict, so modify and remove are **by the owner alone** — not `Publishers`, not `Administrators`. An administrator who needs past a standing rejection **bypasses** (§APR8.6.1) rather than editing the review out of the way, which keeps the record of what was actually said intact. Hard remove is `Administrators` only. *(This replaces "owner-or-`Administrators`", which predates the owner-only narrowing and was contradicted by the code it described.)*
5. `ApprovalComment`: adding requires only the contribution gate (submitters converse in review threads); **modify and remove by the owner alone**; hard remove `Administrators` only. No role widens the amend gate — a comment belongs to whoever wrote it, and somebody who needs past an unresolved one resolves it or bypasses the block rather than editing another person's words. The single exception is `IsResolved`, which the owner **or the publisher tier for the entity behind the approval** may set through the dedicated resolve operation, because resolving records that a comment is **settled** — that it no longer requires anything before the approval can proceed — and changes no wording. This replaces "modify by owner or review role (reviewers resolve comments); remove by owner or `Administrators`", which predates that decision and is the same reviewers-flip-IsResolved model withdrawn from `ApprovalCommentService` (§ARC12.3.1).

**The resolve operation is built.** `ResolveApprovalCommentAsync` owns `IsResolved` and nothing else, answers on `ApprovalComment-Resolving` and publishes `ApprovalComment-Resolved`.

**It is not the only route to the field, and is not meant to be.** Modify is owner-only, so the owner may flip `IsResolved` there too **on a remark**; `IsResolved` is therefore not pinned against storage the way `ApprovalId` and `CreatedBy` are (§ARC12.3.1). Settling an **ask** is this operation's alone — the amend gate refuses a write that would leave one settled (§APR7.8) — which is what stops modify being a way around the publisher tier below. What the operation adds is the **route past the author**: modify cannot express somebody else acting on another person's row without also handing them the author's words, which rule 5 above withdraws. It also gives the UI a single action to target for a resolve control regardless of who is acting.

**Two write paths, two facts, and that costs nothing.** The approval workflow subscribes to **both** `ApprovalComment-Modified` and `ApprovalComment-Resolved` and re-tests, on either, whether an approval previously blocked by `RequireReviewCommentResolutionBeforeApprovals` can now complete. A gate move is announced on whichever address carried it, so neither path can move the gate silently. *(Wired (#276) — both addresses are subscribed by `ApprovalOrchestrationService`; §EVN18(a) records the full set and §EVN18 governs which tier each subscribes at.)*

Four further things are load-bearing rather than incidental.

1. **The subject is `ApprovalComment`, never `Comment`.** `CommentService` owns a separate entity, and the broker composes the stored event name as subject + operation, so `Comment-Resolving` would attribute this service's facts to the wrong entity.
2. **The tier beside the author is the PUBLISHER tier, not the review tier** — the global `Publishers` and `Administrators`, and the entity-scoped `%EntityType%-Publishers` / `%EntityType%-%ContentType%-Publishers` composed from the `RoleSubject` list. An outstanding comment holds the **approval** shut under `RequireReviewCommentResolutionBeforeApprovals`, and the people that block stops are exactly the people who decide the approval (§APR8.6.1). A reviewer is never held by it — they vouch, they do not decide — so settling somebody else's ask is not theirs to do; one who wants to respond writes a comment of their own. `Administrators` clears every tier and so is still admitted, which is what preserves the route rule 5 opened. *(This replaces "`Administrators` is the global role alone — not an entity-scoped `%EntityType%-Publishers`", which read the block as an administrative override rather than as part of deciding the approval, and which left a publisher offered a control the server refused.)*

   **The scoped `ReadOnly` veto reaches this operation, and only this one.** §SEC18.6 rule 3 exempts the comment thread from a scoped block — a comment is speech about the content, not a write to it — and records `IsResolved` as the place that reasoning strains, because settling a comment clears a §APR8.5 gate. `ResolveApprovalCommentRequest` therefore carries the `RoleSubjects` the gatherer already resolves, and the veto is asked **first**, ahead of the author branch, because a block covers the holder's own rows (§SEC18.6 rule 2). Adding, amending and withdrawing a comment are unchanged: the global `ReadOnly` alone.
3. **It is two-way**, and not merely as error-correction. A comment recorded as an observation may later turn out to need action, and one settled prematurely must be able to block again. Without the reverse direction a single mistaken resolve would permanently defeat the setting for that comment — the setting that exists to hold approval shut on outstanding ones.
4. **A no-op is refused, not absorbed.** Resolving an already-resolved comment errors rather than silently re-stamping the audit values and re-publishing the fact. That matters more here than for a display flag: a spurious `-Resolved` announces to anything watching the setting that a gate moved when it did not.

Both gates run, per §SEC14.6 rule 2: the row-local owner-or-`Administrators` check, and an `IAccessBroker` decision that adds what a single-entity service may not read for itself — the round must still be open and the parent approval must not be soft-deleted. Permission is decided before the resolution state is looked at, so a caller who may not act cannot use the "already resolved" response to probe whether a comment on a thread is still outstanding.

Soft-deleted rows follow §SEC14.5 for every posture: not found for every caller including `Administrators`, with the state-based miss logged as information.

## SEC18. Authentication and Authorisation *(formerly §18)*

### SEC18.1 Purpose *(formerly §18.1)*

Authentication and authorisation ensures that G2H users are correctly identified, that access to content and actions is controlled by role and permission, and that the system is ready to support future client applications, mobile apps, and machine-to-machine integrations without requiring a rewrite.

### SEC18.2 Technology Selection *(formerly §18.2)*

G2H uses the following stack for authentication and authorisation:

| Component | Purpose |
| --- | --- |
| ASP.NET Core Identity | User management, password hashing, roles, claims, 2FA, and external login providers. |
| OpenIddict | OAuth 2.0 and OpenID Connect token issuance, scopes, client app registration, and machine-to-machine auth. |
| EF Core | Identity and OpenIddict data persisted to the same SQL database as the domain model. |

This combination gives full ownership of users and data with no vendor lock-in and no external auth service costs.

### SEC18.3 ASP.NET Core Identity *(formerly §18.3)*

ASP.NET Core Identity provides:

1. Full control over users, roles, claims, passwords, and lockout policies.
2. Natural integration with EF Core — Identity tables live in the same database.
3. Role-based and claims-based authorisation for API endpoints and UI routes.
4. Two-factor authentication using TOTP, compatible with Microsoft Authenticator and Google Authenticator.
5. External login provider support including Google, Microsoft, GitHub, and Facebook.
6. Cookie-based authentication for the React frontend hosted within the same ASP.NET app.
7. JWT bearer token support for API consumers.

#### SEC18.3.1 A Username Is Never an Email Address *(formerly §18.3.1)*

**A username and an email address are two different values, and a username may never contain `@`.**

The reason is a leak, not tidiness. Every display name in the system is composed the same way — preferred name, else "Name Surname", else **the username** — so an account that has set no personal details is shown to other people by its username wherever the site names who submitted or reviewed something. `ApprovalReviewRequest.RequestedUserDisplayName` (§APR7.9) even stores the result, so a name composed once outlives the account it came from. If a username may be an address, that whole chain publishes addresses.

The rule is the broad form deliberately. "A username may not equal *this account's own* email" would still let somebody register with a colleague's address as their username and leak it just as effectively, so the constraint is on the shape of the value, not on a comparison against a second field.

**What this buys is the fallback itself.** The chain is allowed to end at the username, and does — an unnamed account keeps a name a moderator can recognise in a reviewer picker, which is what §ARC16.7.4 requires. It is safe because of this rule, and only because of it: the guarantee lives in the data, not in the composer.

**Enforced in three places, one of which is the data:**

| Where | What it does |
| --- | --- |
| Registration, and the administrator's user edit | The two paths that write a username. Both refuse `@` through one shared rule, so they cannot drift, and both explain why rather than reporting a name as "taken". |
| Identity's `User.AllowedUserNameCharacters` | The framework's own default list, narrowed by removing `@`. Nothing routed through `UserManager` can write one past the services above. |
| The stored rows | Enforcing the rule going forward does not clean what is already there, and the fallback holds whatever the row holds. Existing identity rows and existing stored display names are both remediated by migration. |

The identity migration tests each row against **the same character set Identity itself will apply**, not against `@` alone — a guard that asked the narrower question would certify a row the application cannot write to. Where it cannot remediate an account safely it **stops the deploy** rather than choosing for you, because both of its refusals are questions about who a person is rather than about data: an account with no address left to sign in with once its username is taken away, and an address shared by two accounts, which cannot identify either of them afterwards. The second is the one place the unsettled `RequireUniqueEmail` question below has teeth — the migration refuses only the rows whose safety would depend on the answer, and leaves every other duplicate alone.

Signing in is unaffected: sign-in resolves a username first and falls back to the email address, so a person may still sign in with either. That fallback is also why the confirmed-email-change flow does **not** rewrite the username — changing an address changes the address and nothing else.

The related concern of whether two accounts may share an email address (`RequireUniqueEmail`, and the uniqueness of `EmailIndex`) is **not settled here** — it is a question about the email column, and this section is about the username one.


### SEC18.4 OpenIddict *(formerly §18.4)*

OpenIddict layers OAuth 2.0 and OpenID Connect on top of ASP.NET Core Identity.

It enables:

1. OAuth 2.0 authorisation code flow with PKCE for mobile and public clients.
2. OpenID Connect for identity token issuance and userinfo endpoints.
3. Client credentials flow for machine-to-machine integrations such as background jobs and AI workers.
4. Scope-based permission control for fine-grained API access.
5. Client application registration for web, mobile, CLI, and partner integrations.

OpenIddict integrates directly with ASP.NET Core Identity and persists its data to EF Core, meaning no separate identity server infrastructure is required.

### SEC18.5 Scope Design *(formerly §18.5)*

OAuth 2.0 scopes define what a client application is permitted to access.

Recommended initial scopes for G2H:

| Scope | Purpose |
| --- | --- |
| `content.read` | Read published content items, feed, topics, tags, and reactions. |
| `content.write` | Submit, edit, and soft-delete content items and associations. |
| `topics.read` | Read topic landing pages and child content. |
| `notes.read` | Read approval comments and review notes. |
| `notes.write` | Add approval comments. |
| `admin.users` | Manage users, roles, and approval settings. |

Client apps request only the scopes they need.

Example scope assignments by client type:

| Client | Requested Scopes |
| --- | --- |
| Web app (React, cookie auth) | All scopes based on user role. |
| Mobile app | `content.read` |
| Admin portal | `content.read`, `content.write`, `admin.users` |
| AI background worker | `content.read` via client credentials |
| Partner/ministry API consumer | `content.read` via client credentials |

### SEC18.6 Role Design *(formerly §18.6)*

ASP.NET Core Identity roles control access within the G2H application. Roles are stored in the standard Identity roles table and assigned through admin user management.

There is **no `Contributor` role** — every authenticated user may contribute by default.

Global roles:

| Role | Purpose |
| --- | --- |
| `ReadOnly` | **The block role.** If present — even alongside any other roles — the user cannot contribute anywhere. Assigned to users who misbehave. Takes precedence over every other role. **Singular deliberately**, at every tier — see the naming paragraph below. |
| `Reviewers` | Can submit approval reviews and approval comments for any entity type, and can read the non-public rows they are reviewing (§SEC14.5, §SEC14.7 posture A.4). **Never amends content.** A reviewer reviews; rewriting the text underneath the verdict they are about to cast is not part of the job, and the modify gate excludes the review tier at every scope — `Reviewers`, `%EntityType%-Reviewers` and `ContentItem-%ContentType%-Reviewers` alike (§SEC14.7 posture A.3). This is HR-3 applied to content rather than to status. |
| `Publishers` | Can approve and reject content for any entity type, may amend the text of a `Draft` or `Submitted` item during review, and gains the option to bypass approval criteria by being in the role. **Not in-place amendment of an `Approved` or `Rejected` record** — those are terminal to the publisher tier exactly as they are to `Administrators` below: the row is withdrawn, forked or overridden, never edited where it stands (§DOM3.4 rule 16, §SEC14.7 posture A.3). |
| `Administrators` | Full access including user management, approval settings, bypass approval, and the status override that re-opens a terminal record (§APR8.6 HR-4). **Not** in-place amendment of an `Approved` record — that is withdrawn (§DOM3.4 rule 16); an administrator editing terminal content forks like anyone else. |

Granular (entity-type-scoped) roles follow the `%EntityType%-ReadOnly`, `%EntityType%-Reviewers`, and `%EntityType%-Publishers` convention, created for each approvable entity type:

```text
ContentItem-ReadOnly,            ContentItem-Reviewers,            ContentItem-Publishers,
Tag-ReadOnly,                    Tag-Reviewers,                    Tag-Publishers,
BibleReference-ReadOnly,         BibleReference-Reviewers,         BibleReference-Publishers,
Comment-ReadOnly,                Comment-Reviewers,                Comment-Publishers,
Link-ReadOnly,                   Link-Reviewers,                   Link-Publishers,
Attachment-ReadOnly,             Attachment-Reviewers,             Attachment-Publishers,
Association-ReadOnly,            Association-Reviewers,            Association-Publishers
```

The same convention applies to any further approvable entity types (e.g. `Reaction`, `ContentItemSetting`).

**Content-type-scoped roles.** `ContentItem` has a further granularity: `%EntityType%-%ContentType%-Reviewers`, `-Publishers` and `-ReadOnly`, so a reviewer can be trusted with stories but not testimonies, and a contributor can be sanctioned on quotes alone and left free on everything else.

```text
ContentItem-Story-Reviewers,       ContentItem-Story-Publishers,       ContentItem-Story-ReadOnly,
ContentItem-Series-Reviewers,      ContentItem-Series-Publishers,      ContentItem-Series-ReadOnly,
ContentItem-Testimony-Reviewers,   ContentItem-Testimony-Publishers,   ContentItem-Testimony-ReadOnly
```

Read the whole vocabulary as a grid: three tiers by three capabilities, with no gaps. The narrow block was the one cell missing, and its absence made the matrix asymmetric — two tiers could block a user and the third could only grant.

**The capability must stay last in the name.** `ContentItem-Blog-Reviewers`, not `ContentItem-Reviewers-Blog`. `ApprovalService`, `ApprovalReviewService` and `ApprovalCommentService` all identify a reviewer by suffix — `role.EndsWith("-Reviewers")` — so a name ending in the content type would not be recognised as a review role at all, and a content-type-scoped reviewer would silently lose every capability the suffix check grants. Capability-last keeps those three checks working untouched.

**The capability segment is plural**, at every tier — `-Reviewers`, `-Publishers`, and the global `Reviewers` / `Publishers` / `Administrators`. A role name names the *group of people* who hold it, and a group takes the plural. The suffix checks are unaffected by the choice: they are ordinal `EndsWith` matches against whatever this section says the suffix is, so what matters is that the constants, the seed and the checks all spell it one way — which is why the spelling lives in exactly one place — `RoleNames` in `G2H.Security.Client`, the same assembly as the `IAccessClient` decision that depends on it.

**`ReadOnly` is the exception and stays singular**, wherever it appears — the global `ReadOnly`, the entity-scoped `%EntityType%-ReadOnly` and the content-type-scoped `ContentItem-%ContentType%-ReadOnly` alike. It does not name a group of people, it names the *state its holder is in*, and it has no sensible plural. That is a decision, written here so it is not later read as an oversight and "corrected" into line with the other two.

**There is no `Admin` role.** Somebody may be *called* an admin; what they hold is `Administrators`. Until issue #368 there were two administrator roles seeded side by side — the portal's `Administrators`, which opened `/api/admin`, and Core's own `Admin`, which opened the moderation tier — the two-vocabulary split issue #193 describes. They are now one name governing both surfaces, which **widens what a grant of `Administrators` confers**: granting it through the user-admin UI now hands over Core's moderation authority (approve, hard delete, the status override, bypass) as well as the portal's. The widening runs **both ways, and the migration performs the second one at upgrade time**: because `Admin`'s members are moved onto the `Administrators` row, anybody who held only Core's `Admin` — a moderator who was never a portal administrator — comes out holding `Administrators`, and so gains `/api/admin` and user management — and the blog post create, update and delete endpoints under `/api/posts`, which are gated on the same role rather than on `/api/admin`. Where the two roles were always granted together, as the seeded site administrators had them, nobody's authority moves. Where they were not, this migration is a privilege grant in both directions and should be reviewed against the actual `Admin` and `Administrators` membership of each environment before it is applied.

Existing role rows are **renamed in place** by migration rather than re-seeded. `AspNetUserRoles` keys on `RoleId`, so rewriting a row's `Name` and `NormalizedName` carries every existing assignment across untouched, where re-seeding under the new spelling would leave every current holder pointing at a row nothing checks any more. `Admin` is the one row that cannot simply be renamed — `Administrators` already exists and `NormalizedName` is unique — so its members are moved onto the `Administrators` row and the `Admin` row is dropped.

Granular role rules:

1. A granular role grants its capability only for its own entity type. A user in `ContentItem-Reviewers` who is not in `Administrators`, not in a global role, and not in `Tag-Reviewers` cannot review tags.

   **One deliberate exception: a `ContentItem`-scoped publisher role admits a write on a `ContentItemSetting` OVERRIDE row.** An override is not configuration in its own right — it is configuration *of one content item*, keyed on that item's id, and it governs nothing else. The authority that governs the item therefore governs its narrowing, and requiring a second entity-scoped grant to switch off one post's comments would separate two decisions nobody makes separately. The per-type **default** takes no such exception and stays where this rule leaves it (§ARC12.5.2 business rule 6).

   **The exception runs both ways, and must.** Wherever a `ContentItem`-scoped role can GRANT, the matching `ContentItem`-scoped block must be able to REFUSE — so `ContentItem-ReadOnly` and `ContentItem-%ContentType%-ReadOnly` bar an override write exactly as they bar a write to the item itself. A tier that can grant and cannot block is the asymmetry the narrow block was added to close, and an exception that widened only the grants would re-open it.
2. **Any `ReadOnly` variation trumps every other role within its scope — `Administrators` included.** There is no role that escapes a block that applies to the row being written. Two questions, asked in this order:

   1. **Does the block's scope cover this row?** `ReadOnly` covers everything; `%EntityType%-ReadOnly` covers every row of that entity type; `ContentItem-%ContentType%-ReadOnly` covers that content type only. A block whose scope does not cover the row is **silent** — not weakened, not outvoted, simply not asked.
   2. **If it does, it wins.** No grant at any tier overrides it, however wide: not `ContentItem-Quote-Publishers`, not `ContentItem-Publishers`, not `Publishers`, not `Administrators`, and not being the row's own author — the owner admit is a grant like any other.

   Worked, on two content types:

   | Row being written | Holder | Outcome |
   | --- | --- | --- |
   | a **Quote** | `ContentItem-Quote-ReadOnly` + `ContentItem-Quote-Reviewers` | **blocked** |
   | a **Quote** | `ContentItem-Quote-ReadOnly` + `ContentItem-Quote-Publishers` | **blocked** |
   | a **Quote** | `ContentItem-Quote-ReadOnly` + `Administrators` | **blocked** |
   | a **Story** | `ContentItem-Quote-ReadOnly` + `ContentItem-Story-Reviewers` | allowed — the block does not cover stories |
   | a **Story** | `ContentItem-Quote-ReadOnly` + `ContentItem-Story-Publishers` | allowed |
   | a **Story** | `ContentItem-Quote-ReadOnly` + `Administrators` | allowed |
   | **any** content item | `ContentItem-ReadOnly` + any role at any tier | **blocked** |
   | **anything at all** | `ReadOnly` + any role at any tier | **blocked** |

   A wider grant never rescues a narrower block. That is the **mirror image of rule 4**, not a contradiction of it: grants widen *upward*, so a wider grant satisfies a narrower check; blocks are absolute *downward*, and silent outside their scope.

   **Two edges, ruled.** The block covers the holder's **own** rows — somebody who contributed quotes and is then given `ContentItem-Quote-ReadOnly` may no longer edit, withdraw or delete their own existing quotes. The consequence to accept deliberately is that **a sanctioned contributor cannot take their own content down**; removing it needs an unblocked owner-or-`Administrators` path. That keeps the rule total within its scope and leaves no branch where a block is negotiable. And **a vote already cast stands**: blocking somebody is not retroactive, so a review they filed while eligible remains a fact of that round and keeps counting toward its required reviews. The veto governs what they may do **next** — no new vote, no change to the existing one, no decision. Nothing recomputes when a role is assigned, so no approval in flight silently re-opens and there is no sweep to build.

   **Implementation consequence.** The block cannot be expressed as "is the caller in the allowed set" — it is a veto evaluated **before** any grant is considered, and the row's own content type is what selects which narrow block to compose. Each gate asks the block question first and returns unauthorized without ever reaching the grant check. On every modify path the content type is read from the **stored** row rather than the caller's copy: `ContentType` is create-only (§ARC12.4.1 rule 7a), so a blocked contributor relabelling their edit as a type they are free on would otherwise walk straight past it.

   **Which writes it reaches, exactly.** Every write to the CONTENT and to its approval *outcome*: add, modify, submit, the approval transition, unpublish, remove and hard remove on the entity itself; and, through `IAccessClient`, casting or changing a review, deciding an approval, and amending the approval record. It also drops a blocked user from the reviewer candidates and refuses an invitation aimed at them (§APR7.9 rule 3).

   **The approval COMMENT thread's WORDS are outside it, and that is a decision rather than an omission.** `RecordApprovalCommentRequest` and `AmendApprovalCommentRequest` carry no role subjects, so only the global `ReadOnly` reaches them: a scoped block does not stop its holder writing in a review thread. The reasoning is that a comment carries no verdict and moves no outcome — it is speech about the content, not a write to it — and §SEC14.5 keeps the thread readable to them either way.

   **`IsResolved` was the one place that reasoning strained, and it is now closed.** Resolving clears a `RequireReviewCommentResolutionBeforeApprovals` block, so a scoped-blocked holder could still move a §APR8.5 gate. `ResolveApprovalCommentRequest` therefore carries the `RoleSubjects` the gatherer already resolves — `AccessBroker` reads them through the same `ResolveEntityAsync` the amend-approval gate uses — and `DecideMayResolveApprovalComment` asks the veto **first**, ahead of the author branch, because a block covers the holder's own rows (rule 2). The same subjects compose the publisher tier that operation now admits beside the author (§SEC14.7 posture D rule 5).

   **Removing the approval RECORD is outside it too, and sits less comfortably.** `ValidateUserCanRemoveStorageApprovalAsync` is owner-or-`Administrators` (§SEC14.7 posture D rule 3) and takes no access decision, so no scoped block reaches it — the list above says "remove and hard remove **on the entity itself**" for exactly that reason. Unlike a comment, retracting an approval takes down the round, so the case for covering it is stronger: what it needs is an `IAccessBroker.MayRemoveApprovalAsync` mirroring the amend decision, which is where the scoped subjects already are. Not built here; written down so the gap is a decision rather than a discovery.
3. The global `Publishers` role gains the option to bypass approval criteria for any entity type. `%EntityType%-Publishers` gains the bypass option only for that entity type.
4. The three tiers widen from narrow to broad — `ContentItem-Blog-Reviewers` ⊂ `ContentItem-Reviewers` ⊂ `Reviewers`. Holding any one of them satisfies a check for that content type; the narrow role never satisfies a check for a different content type.
5. Content-type-scoped roles apply to `ContentItem` only, and carry **all three capabilities** — `-Reviewers`, `-Publishers` and `-ReadOnly`. No other entity type has a sub-classification, and none should be invented to make the pattern uniform.

**The role segment is the `ContentType` enum member name** (`Quote`, `Story`, `Testimony`, `Topic`, `Series`) — there is no `Slug` any more (§DOM3.7). Every member is already a single PascalCase word with no whitespace or hyphens by construction, so no derivation step is needed and no two members can ever collide on the composed role name.

**Role lifecycle is fixed, not driven by any content-type lifecycle** — there is none (§ARC12.5.1). `ContentType` is a compile-time enum, so the full set of content-type-scoped roles is known at compile time and can be enumerated and seeded once, at application startup, for every member: `ContentItem-Quote-Reviewers`, `ContentItem-Quote-Publishers`, `ContentItem-Quote-ReadOnly`, `ContentItem-Story-Reviewers`, and so on for every member — three names per member, not two. The block is the one of the three that cannot afford to be missed: an unseeded grant fails visibly, because nobody can be scoped to it and the coarse tier still admits them, while an **unseeded block is a sanction that can never be applied** — the composed name is simply never found among an actor's roles, every gate falls through to the coarser question, nothing throws and nothing is logged. Adding a `ContentType` member is a code change and a release; the corresponding roles are seeded on that release's startup, the same as any other fixed role. No *content type* forces a rename or a removal, which is the property this rule is about.

That is not the same as saying a role name can never change: #368 renamed every one of them at once and dropped `Admin`, and the paragraphs above describe how. The difference is that a vocabulary change is a deliberate, migrated, once-off act, where a data-driven lifecycle would make renames routine and unbudgeted. A vocabulary change also leaves a **stale-claim window**: role claims are baked into the auth cookie and refreshed on `SecurityStampValidator`'s interval, so between the migration committing and each signed-in user's next revalidation their principal still carries the old names. That window fails closed — an unrecognised name grants nothing — and closes on its own.

**This capability does not exist yet.** Core's `ISecurityBroker` is read-only on roles — `IsInRoleAsync` and nothing more — and `IIdentityBroker` in the web app manages *user-to-role assignment* (`InsertUserToRoleAsync`, `DeleteUserFromRoleAsync`, `SelectAllRoles`) but cannot create, rename or delete a role. Since Identity is owned by the web app and the `ContentType` enum is owned by Core, the startup seed belongs on the web-app side, reading the fixed set of Core enum members, not on a new Core dependency into the Identity store.

Because these role names now depend on a **fixed enum** rather than on data, they can be enumerated at compile time, and a test can assert the full set exists.

**Composing an association's role check.** An `Association` is authorised from its two endpoints (§SEC14.7), so the check must be able to name both role tiers for each end. The entity type is on the row, but the content type is not — it lives on the endpoint. Rather than resolve the endpoint (which the foundation may not do, §SEC14.3, and which an `IQueryable` filter cannot do at all), the association **denormalises each endpoint's `ContentType` onto its own row.** A `Story` content item's association therefore satisfies `ContentItem-Reviewers` *or* `ContentItem-Story-Reviewers` from the row alone.

The enum member name is stored — as a string, via the same `HasConversion<string>()` used everywhere else `ContentType` is persisted — because the role name needs the member name and there is no separate identifier to join through any more. It is **derived on write and never accepted from a caller** — it is an input to an authorization decision, so a caller who could set it could claim authority over a content type they do not hold a role for.

**The denormalised value can never go stale.** `ContentType` members never change identity once released (§DOM3.6), so there is no rename to cascade; and a content item's `ContentType` is create-only (§DOM3.8 rule 4), so there is no reclassification to chase either. The value is written once, at association creation, from an endpoint whose type can never change — which is what makes denormalising it safe rather than a maintenance liability.

Role claims from the identity token must be used to control visibility of role-restricted navigation items in the React frontend and to enforce API-level authorisation.

### SEC18.7 Authentication Flow *(formerly §18.7)*

#### SEC18.7.1 Web App (Cookie Auth) *(formerly §18.7.1)*

1. The React frontend is hosted within the same ASP.NET Core application.
2. Login submits credentials to the ASP.NET Core Identity sign-in endpoint.
3. On success, an HttpOnly cookie is issued and the user is redirected.
4. The cookie is sent automatically on subsequent requests.
5. Logout clears the cookie and redirects to the home page.
6. Role claims from the cookie identity are used for route guards and UI state.

#### SEC18.7.2 API (JWT Bearer) *(formerly §18.7.2)*

1. API consumers authenticate using OAuth 2.0 via OpenIddict.
2. The authorisation code + PKCE flow is used for interactive clients such as mobile apps.
3. The client credentials flow is used for non-interactive clients such as background jobs.
4. Access tokens are issued as JWTs containing user identity, roles, and scopes.
5. APIs validate the JWT bearer token on each request.
6. API endpoints declare required scopes and roles using standard ASP.NET Core policy attributes.

Example:

```csharp
[Authorize(Policy = "content.write")]
[HttpPost("/api/content-items")]
public IActionResult CreateContentItem(...) { ... }
```

#### SEC18.7.3 Two-Factor Authentication *(formerly §18.7.3)*

1. TOTP-based 2FA is supported via ASP.NET Core Identity.
2. Users can enable 2FA from their profile and scan a QR code with Microsoft Authenticator or Google Authenticator.
3. 2FA is enforced for `Administrators` and `Publishers` roles by policy.

#### SEC18.7.4 External Login Providers *(formerly §18.7.4)*

1. Google, Microsoft, GitHub, and Facebook external login providers can be configured.
2. External login users are linked to ASP.NET Core Identity accounts.
3. Role assignment for external login users follows the same rules as internal users.

### SEC18.8 Authorisation Policies *(formerly §18.8)*

API authorisation is enforced using ASP.NET Core policy-based authorisation.

Recommended policies:

| Policy | Requirement |
| --- | --- |
| `content.read` | Authenticated user or valid access token with `content.read` scope. |
| `content.write` | Authenticated user not in the `ReadOnly` role, nor in the relevant `%EntityType%-ReadOnly`, nor — for the content type being written — in `ContentItem-%ContentType%-ReadOnly`; or an access token with `content.write` scope. Any of the three bars the write whatever else the user holds (§SEC18.6 rule 2). |
| `review` | Authenticated user with `Reviewers` or `Publishers` role. |
| `publish` | Authenticated user with `Publishers` role. |
| `admin` | Authenticated user with `Administrators` role or access token with `admin.users` scope. |

### SEC18.9 Phased Adoption *(formerly §18.9)*

The recommended adoption path is:

**Phase 1 — Current**

1. ASP.NET Core Identity for user management, roles, and claims.
2. Cookie authentication for the React frontend.
3. JWT bearer token support for API consumers.
4. Role-based authorisation for all API endpoints.
5. 2FA with TOTP.
6. External login providers.

**Phase 2 — When Mobile or Public API is Required**

1. Add OpenIddict on top of the existing Identity setup.
2. No rewrite of Identity or domain model required.
3. Register client applications in OpenIddict.
4. Introduce scope-based authorisation alongside role-based authorisation.
5. Enable authorisation code + PKCE for mobile clients.
6. Enable client credentials for machine-to-machine integrations.

### SEC18.10 Future Token Claims Example *(formerly §18.10)*

When OpenIddict is active, access tokens will carry structured claims:

```json
{
  "sub": "user-guid",
  "name": "Jane Doe",
  "role": ["Reviewers", "ContentItem-Publishers"],
  "plan": "premium",
  "scope": "content.read content.write notes.read notes.write"
}
```

APIs enforce access using:

```csharp
[Authorize(Policy = "content.write")]
```

This allows fine-grained permission control per client type without changing the domain model.

### SEC18.11 Architecture *(formerly §18.11)*

The authentication and authorisation architecture follows the same layered pattern as the rest of the system:

```text
React Frontend (cookie auth)
Mobile App / Partner API (OAuth 2.0 + PKCE)
AI Worker / CLI (client credentials)
        │
        ▼
ASP.NET Core Identity + OpenIddict
        │
        ▼
G2H APIs (scope + role policy enforcement)
        │
        ▼
EF Core → SQL (Identity + OpenIddict + domain tables)
```

This keeps all users, tokens, roles, clients, and domain data in a single owned SQL database with no external dependency on a third-party identity provider.

