# Events

Unifies `G2H Design.md` §10 "Event Design" and the standalone `EventSubstrate.md`
into one authoritative document, removing the duplication between them.

Section numbers below carry an **`EVN` prefix** (`EVN1`, `EVN2`, ...) — flat,
not restarted-with-decimals — so a bare `§EVN4` is unambiguous once other
`Documentation/Design/*.md` files exist with their own prefixes (`ARC`, `DOM`,
`SEC`, `UI`) and their own local numbering. This repository's C# comments cite
design sections extensively — dozens of files outside `Documentation/` cite
`§10.X` from this section's former life in `G2H Design.md` — so every section
relocated from that former
`§10.X` life also carries a *(formerly §10.X)* annotation — the literal string
`§10.X` still appears on the right heading, so an old citation resolves by grep
even though the citable number itself is now prefixed and did not survive
verbatim. Sections with no former life in `G2H Design.md` (`EVN0`, `EVN10`,
`EVN20`, `EVN21`, `EVN22`) carry no such annotation, since there is no old
citation for
them to remain discoverable under.

## EVN0. What changed in this unification

`EventSubstrate.md` was an early, **generic pattern sketch** — a fictional
Student/Enrollment/Timetable domain, `IEventReceiver<TEvent>` DI-resolved
receivers, generic `StoredEvent`/`EventDelivery`/`DeadLetteredEvent` tables, REST
fan-out, replay, and a background delivery worker. The system actually built
diverged from that sketch on every one of those points: it is EventHighway-backed,
subscriptions are delegate-based (`SubscribeToTagEventAsync`, and so on for every
entity), addressing follows `<Subject>-<Verb>`, and envelope signing is real HMAC
over the composed event name plus direction — not the generic scheme sketched.

Two corrections made in this merge, stated explicitly rather than silently:

1. `EventSubstrate.md` §5.10 said envelope integrity was "**Not implemented**." It
   is now implemented — `IEnvelopeIntegrityBroker.SignAsync`/`VerifyAsync`, real
   HMAC, exercised throughout `EventBroker` and covered by
   `EnvelopeIntegrityBrokerTests`. §EVN10 below carries the corrected, current
   description; §EVN10's reasoning for *why* the signature must bind the destination
   name and direction (not just identity) is `EventSubstrate.md` §5.10's
   contribution, preserved because the current implementation follows it exactly.
2. Nothing describing the discarded generic scheme (`IEventReceiver<T>`,
   `StoredEvent`, REST fan-out, replay, the background worker, the Student
   example) is carried forward as current design — presenting it as such would be
   actively misleading. It is not deleted either: §EVN22 names every discarded piece
   and why, and the original text remains fully recoverable from git history (see
   §EVN22 for the exact pointer).

---

## EVN1. Purpose *(formerly §10.1)*

The component design uses events to decouple entity creation and update
operations from approval record creation, approval reset behaviour, and
denormalized read state updates.

## EVN2. Naming and Addressing *(formerly §10.2)*

Every service publishes consistent lifecycle events on its own event addresses.
An address is named `<Subject>-<Verb>`, where the **subject is the service** —
its class name minus the `Service` suffix — and the **verb** is the operation.
Tense encodes direction: the present participle (`-ing`) is a **request** the
owning service receives, and the past tense (`-ed`) is a **fact** it publishes
once the work is done. Because the subject identifies the service, a verb never
has to be reinvented to avoid a collision: the CRUD verbs carry the same meaning
at every layer, and a service adds a verb of its own only for an operation CRUD
cannot express (rule 7):

| Service | Request addresses | Fact addresses |
| --- | --- | --- |
| `ContentItemService` (foundation) | `ContentItem-Adding`, `ContentItem-Modifying`, `ContentItem-RemovingById`, `ContentItem-HardRemovingById`, `ContentItem-RetrievingById`, `ContentItem-Submitting`, `ContentItem-Approving` | `ContentItem-Added`, `ContentItem-Modified`, `ContentItem-Removed`, `ContentItem-Submitted`, `ContentItem-Approved`, `ContentItem-Rejected`, `ContentItem-Unpublished` |
| `ContentItemProcessingService` | `ContentItemProcessing-Adding`, `ContentItemProcessing-Modifying`, `ContentItemProcessing-RemovingById`, `ContentItemProcessing-RetrievingById`, `ContentItemProcessing-Approving` | `ContentItemProcessing-Added`, `ContentItemProcessing-Modified`, `ContentItemProcessing-Removed`, `ContentItemProcessing-Approved` |
| `LinkProcessingService` | `LinkProcessing-Adding`, `LinkProcessing-Modifying`, `LinkProcessing-RemovingById`, `LinkProcessing-RetrievingById`, `LinkProcessing-Approving` | `LinkProcessing-Added`, `LinkProcessing-Modified`, `LinkProcessing-Removed`, `LinkProcessing-Approved` |

Those are the complete registered sets for the three services rather than a
sample — `EventBrokerIdentifiers.ContentItem.cs`, `.ContentItemProcessing.cs`
and `.LinkProcessing.cs` read straight through. `LinkService` mirrors
`ContentItemService` address for address; other entities carry the same CRUD
core with their own subset of the extra verbs.

Requests and facts do not pair one to one, and each mismatch is deliberate.
`ContentItem-RetrievingById` publishes no fact, because a read's reply rides the
delivery rather than an address (§EVN11). `ContentItem-HardRemovingById`
publishes onto `ContentItem-Removed` rather than an address of its own (rule 4).
`ContentItem-Approving` publishes `ContentItem-Approved`,
`ContentItem-Rejected` or `ContentItem-Submitted` according to the decision
reached — the third when an administrator's override re-opens a decided row —
so a subscriber keying on the fact name is never told the opposite of what
happened, and one that wants every decision binds all three. And
`ContentItem-Unpublished` has no request address at all: it is the publication
swap clearing the outgoing row, and taking a live row dark with no replacement
is not an operation a caller has a reason to invoke, so no address exists that
would let one ask for it.

`Approving`/`Approved` appears on both tiers for `ContentItem` and `Link`
without either publisher duplicating the other, which is rule 5 at work. The
foundation fact says one row was decided. The processing fact says this tier
finished everything the decision required of the group — which is a promotion
only when the decision promotes. A command carrying `Approved` together with
`IsPublished` clears the incumbent's publication before the decided row takes
the group's published slot; a rejection, an administrator's reset back to
`Submitted`, and an `Approved` that does not carry `IsPublished` all take
nothing into that slot, so no incumbent is looked for and none is cleared. The
fact goes out on all of them, because what it announces is this tier's unit of
work finished and not a promotion — a subscriber that needs to know a promotion
happened reads the decided row on the envelope rather than keying on the
address.

The request address exists here as well as on the foundation because the
promoting case has two writes in it and only this tier can order them: the
foundation decides one row per call, and the orchestration holds no entity
services. Every versioned approval is therefore addressed here, promoting or
not, and the tier that owns the ordering is the one that reports the operation
finished.

1. Create operations emit an `-Added` fact.
2. Update operations emit a `-Modified` fact.
3. Soft delete operations emit a `-Removed` fact.
4. Hard delete operations emit a `-Removed` fact too — `HardRemoved` is
   published to the **same** address as `Removed`, deliberately, and the two
   are told apart by the composed event name, which is bound into the
   envelope's signature, rather than by a separate address. Hard delete is
   implemented and event-invokable wherever it is declared: every entity whose
   event operation enum carries `HardRemovingById` has a request handler for it
   registered in `EventSubscriptionRegistration`, and `EventBrokerIdentifiers`
   maps that entity's `HardRemoved` onto its `Removed` address. No entity
   declares the operation and leaves it unwired, and none publishes
   `HardRemoved` on an address of its own. §EVN18(a) documents the same shape
   for the workflow records.

   Hard remove is **opt-in, not a floor.** An entity declares it only when a
   caller needs it, because a capability nobody calls is untested surface
   carried for its own sake. Thirteen of the fifteen foundation entities
   declare it. The two that do not are `Attachment`, which has no foundation
   service at all yet and only reserved addresses, and `AIReviewerAssignment`,
   where a soft delete already frees the round's one-live-assignment slot so
   nothing needs the row gone. Neither processing service declares it either —
   `ContentItemProcessing` and `LinkProcessing` carry the plain CRUD set. Every
   other statement about hard removal in this document is scoped to the
   entities that declare the operation; the exceptions are named here and
   nowhere else, so the list has one place to go stale.
5. A service publishes a fact only about its **own** unit of work. A foundation
   `-Added` means a row was written; an orchestration `-Added` means that
   orchestrated process completed with its gates passed and its invariants
   restored. They are different facts about different units of work, never two
   publishers of the same fact, so an orchestration must not republish the
   foundation's fact.
6. Subscribers choose accordingly. A foundation fact fires for **every** write to
   that entity regardless of the path that produced it, which suits projections
   and indexes that only need current row state. A layer fact fires only when
   that process completed, which is what a subscriber needs when its reaction
   depends on the guarantees that layer added, or when the process makes several
   foundation writes and the intermediate states must not be observed. Never
   subscribe to both for one reaction — it would double-fire.
7. A verb outside the CRUD set is introduced only when one service has two
   operations that CRUD cannot tell apart — a state transition such as
   `Submitting`/`Submitted` or `Approving`/`Approved` owns a narrower field
   scope than a general modify, so it is a separate method and therefore a
   separate verb. A transition's fact need not echo its request: `Approving`
   publishes `-Approved`, `-Rejected` or `-Submitted` according to the decision,
   because a subscriber keying on the fact name must never be told the opposite
   of what happened. The transition refuses any other target, so those three are
   the whole set and a subscriber that wants every decision binds all of them.
8. Approval services subscribe to relevant lifecycle facts.
9. Event handlers determine whether approval must be created, retained,
   dismissed, reset, or updated.
10. Event handlers can update the denormalized `ApprovalStatus` field where
    appropriate, for example setting `ApprovalStatus = ApprovalStatus.Approved`
    when the threshold is met.

## EVN3. Recommended Domain Events *(formerly §10.3)*

Recommended domain events. The names below identify each event's **intent**; the
address actually registered for it follows the `<Subject>-<Verb>` scheme in §EVN2 —
for example `ContentItemCreatedEvent` is published on the `ContentItem-Added`
address by `ContentItemService`.

| Event | Purpose |
| --- | --- |
| `ContentItemCreatedEvent` | Create approval record for new content. |
| `ContentItemUpdatedEvent` | Dismiss or retain approval based on approval settings and entity-scoped rules. |
| `ContentItemDeletedEvent` | Record soft delete and remove from visibility. |
| `AssociationCreatedEvent` | Create approval record for association. |
| `AssociationUpdatedEvent` | Dismiss or retain association approval. |
| `AssociationDeletedEvent` | Record soft delete and remove association from visibility. |
| `TagCreatedEvent` | Create approval record for tag. |
| `TagUpdatedEvent` | Dismiss or retain tag approval. |
| `TagDeletedEvent` | Record soft delete and remove tag from visibility. |
| `ReactionCreatedEvent` | Create approval record for reaction. |
| `ReactionUpdatedEvent` | Dismiss or retain reaction approval. |
| `ReactionDeletedEvent` | Record soft delete and remove reaction from visibility. |
| `CommentCreatedEvent` | Create approval record for comment. |
| `CommentUpdatedEvent` | Dismiss or retain comment approval. |
| `CommentDeletedEvent` | Record soft delete and remove comment from visibility. |
| `BibleReferenceCreatedEvent` | Create approval record for Bible reference. |
| `BibleReferenceUpdatedEvent` | Dismiss or retain Bible reference approval. |
| `BibleReferenceDeletedEvent` | Record soft delete and remove Bible reference from visibility. |
| `LinkCreatedEvent` | Create approval record for link. |
| `LinkUpdatedEvent` | Dismiss or retain link approval. |
| `LinkDeletedEvent` | Record soft delete and remove link from visibility. |
| `AttachmentCreatedEvent` | Create approval record for attachment. |
| `AttachmentUpdatedEvent` | Dismiss or retain attachment approval. |
| `AttachmentDeletedEvent` | Record soft delete and remove attachment from visibility. |
| `ApprovalCreatedEvent` | Notify subscribers that a new approval record has been created. |
| `ApprovalUpdatedEvent` | Propagate approval status changes to denormalized fields such as `ApprovalStatus`. |
| `ApprovalDeletedEvent` | Record soft delete and remove approval record from active workflow evaluation. |
| `ApprovalReviewCreatedEvent` | Trigger threshold evaluation after a reviewer submits a decision. |
| `ApprovalReviewUpdatedEvent` | Dismiss or retain review based on entity-scoped change rules. |
| `ApprovalReviewDeletedEvent` | Record soft delete and exclude review from threshold calculations. |
| `ApprovalCommentCreatedEvent` | Notify relevant parties that a comment has been added to an approval record. |
| `ApprovalCommentUpdatedEvent` | Propagate comment update to audit history. |
| `ApprovalCommentDeletedEvent` | Record soft delete and remove comment from public visibility. |

## EVN4. Soft Delete Behaviour *(formerly §10.4)*

> May belong under `Domain.md` once #481 continues — it is about entity deletion
> semantics as much as it is about the fact that announces it. Left here, where
> it was already colocated, rather than deciding that boundary now.

Hard delete is implemented and event-invokable for every entity that declares
the operation (§EVN2 rule 4). What follows concerns soft delete specifically,
which is the default and the far more common path; hard delete does not go
through the fields below.

Soft delete is implemented through four fields on `IAudit`:

```csharp
public bool IsDeleted { get; set; }
public string? DeletedBy { get; set; }
public DateTimeOffset? DeletedWhen { get; set; }
public string? DeletionReason { get; set; }
```

`IsDeleted` is the predicate. Every guard and every read filter asks it, and
nothing asks `DeletedWhen`. The flag is also what the database itself keys on:
the filtered unique indexes that decide whether a row may exist at all carry
`[IsDeleted] = 0`, so a predicate reading the timestamp instead would be asking
a different question from the constraint enforcing the rule. `DeletedBy`,
`DeletedWhen` and `DeletionReason` record who, when and why, and answer nothing
about visibility.

All four move together. `ApplyRemoveAuditValuesAsync` stamps `DeletedBy`,
`DeletedWhen` and `IsDeleted = true` on removal, and writes `DeletionReason`
only when the caller supplied one — a null there means "the caller gave no
reason", not "clear the reason already on the row". The approval workflow's
in-place reinstatement clears all four rather than only the flag, so a restored
row carries no residue of the removal that was undone.

Soft-deleted entities:

1. Must not be visible in public UI.
2. Must not appear in feed projections.
3. Must not appear in topic child lists.
4. Must remain available for audit.
5. Must remain available for administrative review.

## EVN5. Delete Approval Direction *(formerly §10.5)*

Deletion is not part of `ApprovalStatus`.

`ApprovalStatus` must remain focused on moderation workflow.

If delete approval is needed in future, introduce a separate pending-deletion
workflow, for example:

```csharp
public bool PendingDeletion { get; set; }
```

or a separate delete-request entity that itself participates in approval.

## EVN6. The Event Envelope *(formerly §10.6)*

All events should be wrapped in an `EventEnvelope<T>` that carries the business
payload alongside security, request, event metadata, and — since §EVN10 —
integrity. All five sections appear below. An envelope that reaches a receiver
without `Integrity` is refused rather than believed, so a sample omitting it
would teach a reader to construct one that no handler will accept.

```csharp
public sealed class EventEnvelope<T>
{
    public T Content { get; init; } = default!;

    public SecurityContext SecurityContext { get; init; } = new SecurityContext();

    public RequestContext RequestContext { get; init; } = new RequestContext();

    public EventMetadata Metadata { get; init; } = new EventMetadata();

    // No initialiser: there is nothing to sign with until publish. See below.
    public EnvelopeIntegrity Integrity { get; init; }
}
```

The initialisers are part of the contract rather than noise. The project
compiles under `<Nullable>enable</Nullable>`, and a section declared without one
is a section every reader downstream has to treat as possibly absent.

`Integrity` carries no initialiser because there is nothing to initialise it
with. The envelope factory does not know the destination and therefore cannot
sign, so the signature is attached at publish, where the event name is known
(§EVN10). An envelope in hand before that point genuinely has none:
`IEventEnvelopeBroker.CreateAsync` returns one whose `Integrity` is null, and
`EventBroker` fills it in on the way out.

**Open defect — the declaration does not say so.** `Integrity` is declared
non-nullable while the value is legitimately absent for the whole of an
envelope's life before publish, so `EventEnvelope.cs` raises `CS8618` and every
consumer works around the declaration rather than with it:
`EnvelopeIntegrityBroker.VerifyAsync` reads `envelope?.Integrity` into a local
and branches on `null`, and `EventEnvelopeBroker` assigns `null` through
`Integrity` at both ends of the client round trip. `EnvelopeIntegrity?` is the
declaration that matches the value's actual lifetime, and
`G2H.EventEnvelope.Client`'s own envelope carries the identical defect.
Correcting either is a code change; the sample above is the model as it stands.

The word `Envelope` is intentional. The event content is the business payload,
while the envelope carries the contextual information required to process the
event safely and consistently.

This design ensures that orchestration services and event handlers do not depend
directly on `HttpContext`, `IHttpContextAccessor`, `ClaimsPrincipal`, or raw JWT
tokens.

## EVN7. Security Context *(formerly §10.7)*

`SecurityContext` is a normalized representation of the authenticated caller
extracted at the application entry point.

```csharp
public sealed class SecurityContext
{
    // Identity
    public string? SubjectId { get; init; }

    public string? Username { get; init; }

    public string? TenantId { get; init; }

    // Authorization
    public IReadOnlyList<string> Roles { get; init; } = [];

    public IReadOnlyList<string> Scopes { get; init; } = [];

    public IReadOnlyList<string> Permissions { get; init; } = [];

    // Authentication state
    public bool IsAuthenticated { get; init; }

    public AuthenticationType AuthenticationType { get; init; }

    // Client / application identity
    public string? ClientId { get; init; }

    public string? ClientApplicationName { get; init; }

    // Delegated/system access
    public bool IsSystemIdentity { get; init; }

    public string? DelegatedBySubjectId { get; init; }
}
```

`Roles`, `Scopes` and `Permissions` are never null. The veto checks in §EVN17
call `.Contains` on `Roles` immediately after establishing that the context
itself is present, and the empty list is what makes that safe — a caller
holding no roles has an empty list, not an absent one.

Recommended enum:

```csharp
public enum AuthenticationType
{
    Unknown = 0,
    User = 1,
    Machine = 2,
    Delegated = 3,
    System = 4
}
```

`SubjectId` is used instead of `UserId` because OAuth 2.0 and OpenID Connect use
the `sub` claim to represent the authenticated subject. For machine-to-machine
flows there may be no human user, and using `SubjectId` avoids forcing every
authenticated caller into a user-only model.

`SecurityContext` is built from the `ClaimsPrincipal` that ASP.NET Core Identity
and OpenIddict put on the request. The normalization happens once, inside
`G2H.EventEnvelope.Client`, at the moment an envelope is created — not in a
factory of its own and not in a controller (§EVN13, §EVN17 rule 3). The rest of
the application must not depend on `ClaimsPrincipal` directly.

**`Username` is the account's login name, never its email address.** It is read
from the username claim — `ClaimTypes.Name`, where ASP.NET Core Identity puts
`UserName` — and nothing that names the caller falls back to `Email` to fill it.
The reason is that this field does not stay in memory: the envelope carrying it
is signed and then serialised whole into the stored event, so whatever `Username`
holds is written into every event that caller ever causes. **That makes the rule
forward-only.** The signature binds the payload, so an email already written into
a stored event cannot be scrubbed without destroying the integrity proof the
event path depends on; correcting the field corrects new events, and existing
ones are a retention question rather than an edit. The same reasoning bars any
other personal data from the security context — it is an authorisation record,
not a profile.

`Username` is carried for diagnostics and for a human-readable actor on the event
path. **No rule is ever decided on it.** Authorisation compares `SubjectId`, and
audit stamps `CreatedBy`/`UpdatedBy` from the subject claim — two accounts can
share a display name, so a rule matching on a name is a privilege escalation.

### Authentication Flow Examples *(formerly §10.7.1)*

Not given its own `EVN` number — it is a subsection of §EVN7 rather than a
citable top-level section, so a decimal suffix here (`EVN7.1`) would be the
one place this document contradicts its own flat, non-decimal numbering rule.
`SystemIdentity` cites `§10.7.1` directly. That citation still resolves: the
*(formerly §10.7.1)* annotation on the heading above is its grep anchor, the
same mechanism every relocated `§10.X` section relies on — so the citation
costs this subsection no number of its own.

**OpenID Connect user login:**

```csharp
new SecurityContext
{
    SubjectId = subjectId,
    Username = username,
    TenantId = tenantId,
    Roles = roles,
    Scopes = scopes,
    Permissions = permissions,
    IsAuthenticated = true,
    AuthenticationType = AuthenticationType.User,
    ClientId = clientId,
    ClientApplicationName = clientApplicationName,
    IsSystemIdentity = false
};
```

**Client credentials / machine-to-machine:**

`SubjectId` is **never blank on a context that will write**. `CreatedBy`,
`UpdatedBy` and `DeletedBy` are all resolved from it, and the audit client
refuses a null or whitespace user id outright — so a context minted with
`SubjectId = null` throws on the first audited write rather than recording a
machine act. A machine that only reads may leave it null; one that writes carries
`SystemIdentity.UserId`.

```csharp
new SecurityContext
{
    SubjectId = SystemIdentity.UserId,
    Username = SystemIdentity.Username,
    Roles = [],
    Scopes = scopes,
    Permissions = permissions,
    IsAuthenticated = true,
    AuthenticationType = AuthenticationType.Machine,
    ClientId = clientId,
    ClientApplicationName = clientApplicationName,
    IsSystemIdentity = true
};
```

**Delegated access:**

```csharp
new SecurityContext
{
    SubjectId = actingSubjectId,
    DelegatedBySubjectId = delegatingSubjectId,
    Username = username,
    Roles = roles,
    Scopes = scopes,
    Permissions = permissions,
    IsAuthenticated = true,
    AuthenticationType = AuthenticationType.Delegated,
    ClientId = clientId,
    IsSystemIdentity = false
};
```

## EVN8. Request Context *(formerly §10.8)*

`RequestContext` contains operational information about the original request or
process that triggered the event.

```csharp
public sealed class RequestContext
{
    public Guid CorrelationId { get; init; }

    public DateTimeOffset RequestedDate { get; init; }

    public string? RequestId { get; init; }

    public string? SourceSystem { get; init; }

    public string? ClientApplicationId { get; init; }
}
```

`CorrelationId` represents the wider business operation or request chain and is
useful for audit trails, diagnostics, tracing, distributed workflow correlation,
support investigations, and replay analysis.

## EVN9. Event Metadata *(formerly §10.9)*

`EventMetadata` contains information about the event instance itself.

```csharp
public sealed class EventMetadata
{
    public Guid EventId { get; init; }

    public string EventType { get; init; } = string.Empty;

    public int Version { get; init; }

    public int RetryCount { get; init; }

    public string? CausationId { get; init; }

    public Guid? ParentCorrelationId { get; init; }
}
```

This metadata becomes more important when moving from in-process event handling
to asynchronous or distributed event processing. It supports retries, replays,
event versioning, diagnostics, idempotency, causation tracking, and parent/child
event relationships.

Every property is `init`-only, and deliberately so: `Metadata` sits inside the
integrity signature (§EVN10), so a field changed after signing would invalidate
the envelope's own signature. `RetryCount` is the one that invites the mistake —
it is set once when the envelope is created and never incremented, because a
redelivery is the substrate replaying the stored event rather than a fresh
envelope carrying a higher count.

Example causation chain, from a single `POST api/ContentItems`:

```text
POST api/ContentItems
  ContentItemProcessingService.AddContentItemAsync mints the root envelope
  EventId: 1
  CorrelationId: A
  CausationId: none — a root envelope has no cause

ContentItemProcessing-Added
  published on an envelope built from the one above
  EventId: 2
  CorrelationId: A
  CausationId: 1
```

The chain runs exactly as far as an envelope is carried and no further.
`IEventEnvelopeBroker.CreateNextAsync` copies the source's `RequestContext` —
correlation id included — and points `CausationId` at the source's `EventId`,
while `CreateAsync` mints a fresh correlation id and leaves `CausationId` null.
A service entered by a direct method call rather than by a delivered envelope
therefore starts a chain of its own: the foundation's `ContentItem-Added` and
the approval round's `Approval-Added` each carry their own correlation id,
because `ContentItemService.AddContentItemAsync` and
`ApprovalService.AddApprovalAsync` each mint a root envelope before doing any
work. `ParentCorrelationId` is only ever copied forward and never originated, so
it is null on every envelope this system mints.

## EVN10. Envelope Integrity — Signing *(new; corrects EventSubstrate.md §5.10)*

**Implemented.** `IEnvelopeIntegrityBroker.SignAsync` / `VerifyAsync` are real,
used by every `EventBroker` publish and every substrate handler's verification,
and covered by `EnvelopeIntegrityBrokerTests`. This corrects `EventSubstrate.md`
§5.10, which described this as "Not implemented" — that was accurate when
written and is no longer accurate. What follows is `EventSubstrate.md` §5.10's
reasoning, carried forward because the implementation that was actually built
follows it: the "why" survived even though the surrounding scheme did not.

The signature covers, serialized together into one `SignedPayload<T>` object and
HMAC'd whole — there is no separate content-hash step; `Content` is bound by
being part of that one signed object, the same as every other section below. The
sections are listed in the order `SignedPayload<T>` declares them, because that
declaration order is the order the serializer writes and therefore part of what
the signer and the verifier have to agree on:

1. The **composed event name** (`$"{entityName}{operation}"`), supplied by the
   caller
2. A **direction discriminator** — the `EnvelopeDirection` value rendered by
   `ToString()`, so `Request` or `Reply`
3. `Content`, in full
4. `SecurityContext`
5. `RequestContext`
6. `Metadata` — in full

`Integrity` is the one part of the envelope left out, and necessarily so: it
carries the signature, which cannot be an input to itself.

**Signing only `SecurityContext` and `RequestContext` does not work.** A
signature over identity alone is a transplantable bearer token: capture any one
legitimately signed envelope, lift its signed `SecurityContext` onto different
`Content` at a different destination, and it still verifies. The attacker then
authors any request they like as that actor — which is the whole property the
signature was supposed to deny them.

Five things about the list above are easy to get wrong:

**The destination is the event NAME, not the address.** Addresses are
deliberately many-to-one: `ContentItemEventOperation.Removed` and `HardRemoved`
map to the same `ContentItemRemovedEventAddressId` on purpose, so consumers
subscribe to one removal address and tell the two apart by the composed name.
Bind the address and a signed soft delete is interchangeable with a signed hard
delete — the destructive one — under the same signature. Bind
`$"{entityName}{operation}"`, which is what `EventBroker` composes and writes to
`EventV2.EventName`.

**Nothing on the envelope carries the destination, and nothing should.**
`EventEnvelope<T>` is `Content` + `SecurityContext` + `RequestContext` +
`Metadata` + `Integrity`; there is no destination field, and adding one would
mean signing a value the attacker supplies — a self-check, worth nothing. The
event name is therefore an **out-of-band parameter to both operations**: the
publisher passes the name it is about to publish to, and the receiver passes the
name it is subscribed to. The signature verifies only if the two agree. That is
what makes it a binding rather than an assertion. This has a consequence for who
can sign: the envelope factory does not know the destination, so it cannot be
the signer. Signing happens at publish, where the name is known.

**`Metadata` is signed in full.** Excluding it would leave `Version` — the
schema selector — forgeable alongside an otherwise-genuine signature, which is
a downgrade attack against the signed payload itself. Signing it in full also
fixes every other field on it, `RetryCount` included, and that is why the whole
of `EventMetadata` is `init`-only. Nothing increments `RetryCount` after an
envelope is created. A substrate retry is EventHighway redelivering the stored
`EventV2` row, which replays the identical signed bytes rather than re-minting
the envelope; an outbox retry (§EVN19 rule 5) republishes the stored envelope
under a fresh signature and likewise mints nothing. A mutable counter inside the signed payload would not be a
convenience but a contradiction — bumping it would invalidate the very
signature that makes the envelope believable. A count that has to change belongs
on the delivery record, outside the signature.

**Replies are envelopes too.** A handler's reply carries the original caller's
`SecurityContext` verbatim and is minted with a fresh `EventId` that no dedupe
table has seen. Without the direction discriminator, a signed reply is a
structurally valid signed *request* for the same event name. Sign replies as
`reply`, and refuse a `reply` where a `request` is expected.

**Signing the `EventId` is necessary but not sufficient for replay protection.**
It stops an attacker *changing* the id; it does not stop them re-submitting the
bytes unchanged. That requires the receiver to check the id against
`ProcessedEvents`, which the foundation substrate handlers do.

```csharp
public sealed class EnvelopeIntegrity
{
    // Recorded for documentation only. A verifier NEVER reads this to decide
    // how to check the signature — it computes with its own configured
    // algorithm — so an envelope rewritten to claim "none" cannot downgrade
    // the check.
    public string Algorithm { get; init; } = "HMACSHA256";

    // Which signing key produced this signature — lets the key be rotated
    // without invalidating envelopes already signed under the previous one:
    // VerifyAsync resolves the key by KeyId rather than assuming there is
    // only ever one, and ignores that key's active window so a historic event
    // still verifies after its key has retired. It is unsigned selection
    // metadata rather than a signed claim: an id resolving to nothing fails
    // closed, and an id rewritten to another configured id is caught only
    // because that id resolves to different secret material. Two ids sharing
    // one secret are one key under two names, and startup validation does
    // not refuse that.
    public string KeyId { get; init; } = string.Empty;

    public string Signature { get; init; } = string.Empty;

    public DateTimeOffset SignedDate { get; init; }
}
```

Neither `Algorithm` nor `KeyId` is inside the signature, and the asymmetry
between the two is deliberate. They are unsigned in different senses, and only
one of them is tamper-evident at all.

`Algorithm` is never read. `VerifyAsync` computes through `ComputeSignature`,
which calls `HMACSHA256.HashData` directly, so an envelope rewritten to claim
`"none"` cannot downgrade the check. Nor is that rewrite *detected*: an envelope
with a forged `Algorithm` and an otherwise genuine signature verifies as valid,
because the field is inert —
`ShouldNotConsultTheAlgorithmFieldWhenVerifyingAsync` asserts exactly that. The
field records what was used; it is not a claim the verifier honours.

`KeyId` is read, but only to select which configured secret to recompute
against. An id that resolves to no configured key fails closed. An id rewritten
to another *configured* id is caught only because that id resolves to different
secret material, so the recomputed HMAC differs and the comparison fails — and
that is a property of the configuration rather than of the verifier.
`ValidateSigningKeys` refuses two entries under one `KeyId`, because
verification resolves by id and duplicates would make which key checks a
signature indeterminate; it does not refuse two ids carrying the same `Key`.
Under that configuration the two ids are one key wearing two names, and
rewriting `KeyId` from either to the other recomputes the identical signature
and verifies.

That concedes no forgery power. The payload is still bound, so rewriting `KeyId`
alone changes nothing an attacker wants changed. What it costs is rotation:
retiring an id removes it from signing, not the secret it carried, and while a
second id still carries that secret the rotation has renamed the key rather than
replaced it. Configure distinct secret material per id and the guarantee holds
as stated; share material and `KeyId` stops distinguishing anything.

The rule underneath both fields is that nothing an attacker can rewrite may
decide *how* a signature is checked. An unsigned field on the envelope may at
most select among things the verifier already trusts — and for that selection to
carry any weight, the things selected among have to actually differ.

```csharp
public enum EnvelopeDirection
{
    Request = 0,
    Reply = 1
}

// Internal, like every broker: the envelope never leaves the process, so there
// is no external signer or verifier to expose this to.
internal interface IEnvelopeIntegrityBroker
{
    ValueTask<EnvelopeIntegrity> SignAsync<T>(
        EventEnvelope<T> envelope,
        string eventName,
        EnvelopeDirection direction);

    // Returns false for a bad signature, a missing one, a name mismatch,
    // or a direction mismatch. The caller supplies the name it expects and
    // the direction it expects; neither is read off the envelope.
    ValueTask<bool> VerifyAsync<T>(
        EventEnvelope<T> envelope,
        string expectedEventName,
        EnvelopeDirection expectedDirection);
}
```

The signed input is not a defined canonical form. `ComputeSignature` builds the
`SignedPayload<T>` above and hands it to `JsonSerializer.SerializeToUtf8Bytes`
with default options, so the signed bytes are whatever `System.Text.Json`
produces for that type: properties in declaration order, names as declared,
nulls written rather than omitted, enums as numbers. That is deterministic
enough today for a narrow reason — signer and verifier are the same method in
the same assembly in the same process. `SignAsync` and `VerifyAsync` both call
`ComputeSignature`, so there is no second implementation to drift from the
first.

**Open risk — the signed rendering is unversioned.** The bytes depend on the
declared shape and order of `SignedPayload<T>`, of `SecurityContext`,
`RequestContext` and `EventMetadata`, and of whatever `T` is, and nothing on the
envelope records which rendering a stored signature was produced under. Signed
envelopes are persisted and verified later — the request in `EventV2.Content`,
the reply in `ListenerEventV2.Response` — so a deploy that reorders a property,
renames one, or changes a serialized type makes every already-stored envelope
recompute to different bytes and fail verification, which at the receiver is
indistinguishable from tampering. Closing this means defining the rendering
explicitly instead of inheriting it from the serializer, and carrying a version
on the signed payload so an old signature can be checked under the rendering it
was made with. Until that exists, the serialized shape of those types is part of
the signing contract: reordering a property in any of them is a breaking change,
not a tidy-up.

## EVN11. Current Implementation — EventHighway Substrate *(formerly §10.10)*

Events are published through the `EventBroker`, which wraps
[EventHighway](https://github.com/The-Standard-Organization/EventHighway) — a
durable, SQL-backed pub/sub substrate. Each service owns a set of event addresses
named `<Subject>-<Verb>` (§EVN2), split into two families: **requests** in the
present tense (`ContentItem-Adding`, `-Modifying`, `-RemovingById`,
`-HardRemovingById`, `-RetrievingById`, `-Submitting`, `-Approving`), answered
by responder handlers on the owning service, and **facts** in the past tense
(`ContentItem-Added`, `-Modified`, `-Removed`, `-Submitted`, `-Approved`,
`-Rejected`, `-Unpublished`), published by the service after its work is done
for observers to react to. The
subject is the service rather than the entity, so a higher-level service
announcing completion of its own unit of work sits on its own addresses —
`ContentItemProcessing-Adding` is handled by `ContentItemProcessingService`,
which publishes `ContentItemProcessing-Added` once the processed add has
completed. A receiver handler carries an `On` prefix, which marks the receiver
and never appears in the address itself; the rest of the name follows the
direction of the event. A request responder mirrors the address verb with the
entity set inside it — `Adding` gives `OnAddingContentItemAsync`,
`RemovingById` gives `OnRemovingContentItemByIdAsync`, and a two-part verb such
as `SettingConfidence` gives `OnSettingAssociationConfidenceAsync` — so the
method reads as the operation it has been asked to perform and stays in step
with the address it owns. A fact subscriber reverses the order, naming the
entity first and the completed verb second: `OnContentItemAddedAsync`,
`OnLinkAddedAsync`, `OnApprovalCommentResolvedAsync`. It reads as a reaction to
a completed entity fact rather than as a request, and it deliberately omits the
publishing subject — `OnContentItemAddedAsync` is bound to
`ContentItemProcessing-Added`, not to `ContentItem-Added` — because a
subscriber does not own the address it listens on, and which tier's fact it
reacts to is a routing decision belonging to `EventSubscriptionRegistration`
rather than to the method name. Which service is listening is recorded on the
subscription instead: `"ApprovalOrchestrationService.OnContentItemAdded"`
against the responder's `"ContentItemService.OnAddingContentItem"`. The address
is selected by a strongly typed
per-service operation enum passed on publish (for example
`ContentItemEventOperation.Adding`, `ContentItemProcessingEventOperation.Added`)
— no magic strings, and operations can be added per service without affecting
the others. The broker composes the stored event name from the subject and
operation (for example `"ContentItemAdding"`, `"ContentItemProcessingAdded"`),
so the subject must be distinct per service or the stored names would collide.
Every publish persists the event and dispatches it inline to the in-process
delegate handlers subscribed to that address; handler failures are recorded per
listener rather than failing the publisher, and nothing in this host retries
them. Subscriptions
bind to exactly one operation. Handlers may optionally return a reply envelope
(`ValueTask<EventEnvelope<T>?>`), which the broker serializes onto the
delivery's `ListenerEventV2` row — the observable reply channel for
request-style events such as `RetrievingById`, carrying the same
security-context and metadata discipline as the request.

Publishing returns an `EventPublishResult<T>`: the persisted event id plus one
`EventDelivery<T>` per subscription, each with its dispatch-time status and —
for responders — the reply envelope deserialized back to `EventEnvelope<T>`.
This is a dispatch-time snapshot, and in this host it is also the final word. A
delivery that records `Error` stays failed. EventHighway can retry listener
deliveries — listener-level budgets with incremental backoff — but only when a
caller drives `ListenerEventV2Client.RetryFailedListenerEventV2sAsync`. The
substrate runs no timer and no background worker of its own, and `IEventBroker`
does not expose that entry point at all.
`IEventBroker.FireScheduledPendingEventsAsync` is a different mechanism and not
a substitute: it fires time-deferred events whose `ScheduledDate` has passed,
nothing in this repository calls it, and no hosted service, timer or scheduler
exists that could. `EventBroker` never sets a `ScheduledDate` either, so every
publish is immediate and there would be nothing for it to fire. Recovering a
failed delivery is therefore an operator action against the event store, where
the durable truth remains. Notification-style publishers simply ignore the
result, and in doing so give up the only signal that a subscriber never ran.

Foundation services follow a dual-path shape (see `ContentItemService` as the
template):

- **Non-event path**: receive the object → convert to a request envelope via
  `IEventEnvelopeBroker.CreateAsync` (captures the caller's `SecurityContext`,
  stamps event/correlation identifiers) → call the shared private `DoXAsync`
  method.
- **Event path** (the `.Substrate` partial): one `On<Verb><Entity>Async`
  handler per request address, with any tail of the verb following the entity
  (`OnAdding…`, `OnModifying…`, `OnRemoving…ById`, `OnRetrieving…ById`) → validate the envelope → dedup mutating handlers via the
  `ProcessedEvents` table (unique on EventId + ReceiverName; a deduplicated
  delivery replies `null`) → converge on the same `DoXAsync` methods → reply
  with the outcome envelope on the delivery.

The `DoXAsync` methods own auditing, validation, storage, and publishing the
past-tense fact, so the two paths cannot diverge; §EVN19 rules where the storage
half ends and the publishing half begins, because nothing binds them and a
failed publish strands the row it was announcing; every hop chains causation
through `IEventEnvelopeBroker.CreateNextAsync` (fresh `EventId`, `CausationId`
= source event, security/request context carried forward). Substrate handlers
categorize failures into the service's typed exceptions and rethrow — the
delivery records `Error` and stays there; the failure is never swallowed, and
nothing in this host retries it. An entity
that declares `HardRemovingById` is hard-removable through the substrate on the
same shape as any other write: the request arrives on that entity's own request
address and the service publishes `HardRemoved` onto its `Removed` address
(§EVN2 rule 4, §EVN18(a)). An entity that declares no hard-remove operation has
no such handler and no such address; §EVN2 rule 4 names the ones that do not.
Reads publish no fact — a retrieve's reply rides the delivery's response.

The broker keeps per-entity pub/sub methods (`PublishContentItemAsync`,
`SubscribeToContentItemEventAsync`, and so on), so publishing and subscribing
always go through the broker — never directly against foundation services. All
subscriptions are configured in one central place, `EventSubscriptionRegistration`,
which also registers the participant and event addresses at startup.

The event handler must receive an `EventEnvelope<T>` rather than depending
directly on `HttpContext`.

Current flow:

```text
HTTP Request
    ↓
Controller (thin pass-through)
    ↓
Orchestration / Foundation Service
    ↓
Create EventEnvelope<T> via IEventEnvelopeBroker
    ↓
Publish using EventBroker (EventHighway)
    ↓
Event persisted + dispatched inline
    ↓
Subscribed handler (registered in EventSubscriptionRegistration)
    ↓
Orchestration Service
```

## EVN12. Future Disconnected Processing *(formerly §10.11)*

If the application later moves to background workers, queues, Azure Service Bus,
RabbitMQ, Kafka, or another distributed event mechanism, the same envelope can be
serialized and processed outside the original HTTP request.

Future flow:

```text
HTTP Request
    ↓
Controller (thin pass-through)
    ↓
Orchestration / Foundation Service
    ↓
Create EventEnvelope<T> via IEventEnvelopeBroker
    ↓
Serialize envelope
    ↓
Queue/message broker
    ↓
Background worker
    ↓
Deserialize envelope
    ↓
Orchestration Service
```

At that point there is no active `HttpContext`, no original request scope, and
the original token may have expired. The `EventEnvelope<T>` prevents the
architecture from depending on request-specific state.

## EVN13. Controller Pattern *(formerly §10.12)*

Controllers are thin exposure points. Like brokers, they exist only to let
requests into the business domain — they carry no business logic and must not
build `SecurityContext`, `RequestContext`, `EventMetadata`, or `EventEnvelope<T>`.
Envelopes and events are created only by internal services (coordinations,
orchestrations, processings, foundations) via `IEventEnvelopeBroker`.

The controller should:

1. Rely on authentication middleware to authenticate the caller.
2. Accept the request model and `CancellationToken`.
3. Call the entity's **top-layer service**. Which layer that is varies by entity
   and is not the controller's choice: §EVN18 rule 3 requires the two Versioned
   types to be exposed above their foundation, so `ContentItemsController` binds
   `IContentItemProcessingService`, while `Tag` has nothing above its foundation
   and `TagsController` binds `ITagService` directly. The publication model does
   not decide that — `ContentItemSetting` and `Association` are both Single-Row
   and both carry an orchestration above their foundation (§EVN18). Of the
   twelve controllers, three bind an orchestration, two a processing service and
   seven a foundation.
4. Map the result and domain exceptions to HTTP responses.

`TagsController.PostTagAsync`, whole — the exception mapping is most of what a
controller is, so an abridged one would show the least of it:

```csharp
[HttpPost]
[Authorize]
public async ValueTask<ActionResult<Tag>> PostTagAsync(
    [FromBody] Tag tag,
    CancellationToken cancellationToken)
{
    try
    {
        Tag addedTag =
            await this.tagService.AddTagAsync(tag, cancellationToken);

        return Created(addedTag);
    }
    catch (TagValidationException tagValidationException)
        when (tagValidationException.InnerException is UnauthorizedTagException)
    {
        return Unauthorized(tagValidationException.InnerException);
    }
    catch (TagValidationException tagValidationException)
    {
        return BadRequest(tagValidationException.InnerException);
    }
    catch (TagDependencyValidationException tagDependencyValidationException)
        when (tagDependencyValidationException.InnerException is AlreadyExistsTagException)
    {
        return Conflict(tagDependencyValidationException.InnerException);
    }
    catch (TagDependencyValidationException tagDependencyValidationException)
    {
        return BadRequest(tagDependencyValidationException.InnerException);
    }
    catch (TagDependencyException tagDependencyException)
    {
        return FailedDependency(tagDependencyException.InnerException);
    }
    catch (TagServiceException tagServiceException)
    {
        return InternalServerError(tagServiceException);
    }
}
```

`[Authorize]` is a coarse gate and nothing more (§EVN17): it establishes that
somebody is signed in. The row-level rule this operation actually turns on — may
this caller contribute at all — is decided inside `AddTagAsync`, by
`ValidateUserIsAllowedToContribute` reading the envelope's own
`SecurityContext`, because a layer boundary is not a trust boundary and no
service assumes an upstream one gated the caller. The base class is
`RESTFulSense.Controllers.RESTFulController`, which supplies the single-argument
`Created` as well as `FailedDependency` and `InternalServerError`; a POST answers
`Created`, not `Ok`.

## EVN14. Event Handler Pattern *(formerly §10.13)*

There is no standalone handler class. §EVN11 already states this — subscriptions
are delegate-based, not `IEventReceiver<T>`-style DI-resolved receiver classes
(§EVN22 names that discarded shape). The receiving
service implements the handler as part of its own `.Substrate` partial —
`On<Verb><Entity>Async` for a request address, `On<Entity><Verb>Async` for a
fact address (§EVN11) — and `EventSubscriptionRegistration` binds it as a
delegate at startup:

```csharp
// IContentItemService.Substrate.cs
ValueTask<EventEnvelope<ContentItem>?> OnAddingContentItemAsync(
    EventEnvelope<ContentItem> envelope,
    CancellationToken cancellationToken = default);
```

```csharp
// EventSubscriptionRegistration.cs
await this.eventBroker.SubscribeToContentItemEventAsync(
    subscription: new EventSubscription
    {
        Id = EventBrokerIdentifiers.ContentItemOnAddingContentItemSubscriptionId,
        Name = EventBrokerIdentifiers.ContentItemOnAddingContentItemSubscriptionName,

        Description = "Handles add requests: stores the content item, publishes " +
            "ContentItem-Added, and replies with the added entity."
    },
    operation: ContentItemEventOperation.Adding,
    contentItemEventHandler: Scoped<IContentItemService, ContentItem>(
            service => service.OnAddingContentItemAsync),
    cancellationToken: cancellationToken);
```

## EVN15. Envelope Validation *(formerly §10.14)*

The envelope should be validated before orchestration proceeds. Validation
should confirm:

1. Envelope is not null.
2. Content is not null.
3. Security context is present.
4. Request context is present.
5. Metadata is present.
6. Correlation id is present.
7. Event id is present.
8. Authenticated operations have valid identity details.
9. Machine operations have valid client details.
10. **The signature verifies** — `IEnvelopeIntegrityBroker.VerifyAsync` against
    the event name this handler serves and the expected direction (§EVN10). It
    sits alongside items 1-9 rather than standing in for them: it settles who
    signed the envelope, for which event name and in which direction, and that
    nothing signed has changed since — not that any signed section is present
    or means anything.

`ApprovalOrchestrationService.Validations.cs` is the real implementation. It is
written in the shared-address form — see §EVN18 for the accepted-name-set
reasoning behind the `acceptedEventNames` array, needed because a removal
address carries both `Removed` and `HardRemoved`:

```csharp
private ValueTask ValidateEntityFactEnvelopeAsync<TEntity>(
    EventEnvelope<TEntity> envelope,
    string eventName) =>
    ValidateEntityFactEnvelopeAsync(envelope, new[] { eventName });

private async ValueTask ValidateEntityFactEnvelopeAsync<TEntity>(
    EventEnvelope<TEntity> envelope,
    string[] acceptedEventNames)
{
    if (envelope is null || envelope.Content is null || envelope.Metadata is null)
    {
        throw new InvalidApprovalOrchestrationException(
            message: "Approval is invalid, fix the errors and try again.");
    }

    foreach (string eventName in acceptedEventNames ?? Array.Empty<string>())
    {
        bool isSignatureValid = await this.envelopeIntegrityBroker.VerifyAsync(
            envelope, eventName, EnvelopeDirection.Request);

        if (isSignatureValid)
        {
            return;
        }
    }

    throw new InvalidApprovalOrchestrationException(
        message: "Approval event is invalid. Integrity verification failed.");
}
```

Three things about that shape are deliberate.

**The exception is the service's own.** There is no envelope exception type in
this codebase, and there should not be one: every service categorizes into its
own `Invalid<Entity>…Exception`, and a separate family raised from the event
path would have to be taught to every substrate `TryCatch` that maps failures
onto a delivery.

**The structural checks are folded into one condition.** A malformed envelope
names no row and no operation, so there is nothing for a per-field message to
be about, and the caller gets one verdict either way.

**The null check is short, and the signature does not lengthen it.** `Content`
and `Metadata` are guarded here because the handler dereferences them
immediately. The signature does not cover the remaining items. It authenticates
the values that were signed and nothing more: `ComputeSignature` copies
`SecurityContext`, `RequestContext` and `Metadata` into `SignedPayload<T>` with
no guard of any kind, and `System.Text.Json` writes a null as `null` rather than
omitting it (§EVN10), so an envelope signed over an absent `SecurityContext`, or
over a `Guid.Empty` correlation or event id, verifies exactly as one signed over
real values does. `Guid.Empty` is a value like any other to an HMAC. What
verification rules out is a *different* context being substituted for the signed
one — the property the event path actually depends on, and the reason a service
may act on a role or on `IsSystemIdentity` read off a verified envelope (§EVN17).
It is not a presence check, and reading it as one would leave items 3, 4, 6
and 7 unasked by anybody.

The remaining items are answered, where they are answered at all, outside this
method:

- **Items 3 and 8 — security context present, identity valid.** At the
  authorization gate. Every `ValidateUserIsAllowedToContribute`, and each
  read-side gate, opens with `securityContext is null ||
  securityContext.IsAuthenticated is false` and refuses, so a null or default
  context fails closed wherever a gate runs (§EVN17). A read that answers from
  the row never reaches one — `DoRetrieveAssociationByIdAsync` returns a
  publicly visible row before it looks at `SecurityContext` at all — and that is
  right, because the decision there is made from the row rather than from the
  caller.
- **Items 4 and 6 — request context, correlation id.** Not checked. No service
  reads `RequestContext`; it is carried for tracing and audit and nothing
  decides on it, so an absent one costs a trace rather than an invariant.
- **Item 7 — event id.** Not checked for presence. The substrate dedupe passes
  `Metadata.EventId` to `ProcessedEvents` as it finds it, so an envelope signed
  with `Guid.Empty` would collide with every other such envelope rather than be
  refused.
- **Item 9 — machine client details.** `ClientId` and `ClientApplicationName`
  are read by no service. `IsSystemIdentity` is the machine-identity fact that
  is actually decided on, and the transition gates ask it directly.

Items 4, 6, 7 and 9 are therefore contract rather than current behaviour. A
receiver that starts deciding on any of them owns the check that makes doing so
safe, because verification will not have made it on that receiver's behalf.

A foundation receiver is the same shape with a single name rather than a set,
composed from the operation it serves: `ValidateContentItemEventEnvelopeAsync`
builds `$"{nameof(ContentItem)}{operation}"`, verifies against that, and throws
`InvalidContentItemEventException`.

## EVN16. Anti-Patterns *(formerly §10.15)*

Avoid passing `HttpContext` into orchestration services:

```csharp
// AVOID
public ValueTask<ApprovalOutcome> DecideApprovalAsync(
    EntityType entityType,
    Guid entityId,
    ApprovalDecision decision,
    HttpContext httpContext)
```

Avoid using `IHttpContextAccessor` inside orchestration services:

```csharp
// AVOID
this.httpContextAccessor.HttpContext.User
```

Avoid serializing raw `ClaimsPrincipal` into events.

Avoid passing raw JWT tokens through the domain or event pipeline unless there is
a specific and justified reason.

Avoid placing authorization decisions only in controllers, and equally avoid
placing them only in the topmost service a request happens to pass through —
every service that exposes the operation decides for itself (§EVN17).

Avoid scattering magic-string role and scope names throughout services. Keep
role and claim names in `Roles` (`Glory2Him.Core/Models/Securities/Roles.cs`) —
a typed façade over the spellings `G2H.Security.Client` composes its own
decisions from, so the composer and the decision that depends on it cannot drift
apart — and perform ordinary role checks against the envelope's own
`SecurityContext.Roles`.

Avoid resolving the caller a second time inside a service. The envelope already
carries the actor, and a service that fetched its own would hold two identity
sources that disagree precisely on the unauthenticated path. §EVN17 names the one
place a caller is resolved from a principal.

## EVN17. Authorization in Every Layer *(formerly §10.16)*

A generic `ISecurityBroker` — `IsInRoleAsync`, `GetCurrentUserAsync`,
`GetCurrentSecurityContextAsync` and the rest — does exist, but not here. It
lives in `G2H.EventEnvelope.Client`, it is `internal` to that assembly, and that
assembly's `InternalsVisibleTo` names only its own test projects. Core cannot
call it and must not try: its single job is to resolve the ambient principal
once, at the moment an envelope is minted (rule 3 below traces that chain).
Asking it again from a service would produce a second identity source that
disagrees with the envelope's on exactly the path where it matters.

Inside Core there are therefore two patterns and only two: ordinary role and
veto checks read the envelope's own `SecurityContext.Roles`, and cross-table
approval decisions go through `IAccessBroker`.

**Ordinary role and veto checks read `SecurityContext.Roles` directly**, in the
`.Validations` partial of **every** service that exposes the write — not in one
chosen layer. A caller reaches a service in one of two ways, an exposer binding
to it directly or an envelope arriving on its own event address, and both happen
at more than one altitude: `EventSubscriptionRegistration` registers
`IContentItemService` and `IContentItemProcessingService` as substrate handlers
side by side, and among the controllers `ContentItemsController` binds the
processing service while `TagsController` binds a foundation service. A layer
boundary is therefore not a trust boundary, and no service assumes an upstream
layer already gated the caller.

The duplication that follows is deliberate defence in depth — `G2H Design.md`
§14.6 rule 2, either service must be safe when called alone — so
`ContentItemService` and `ContentItemProcessingService` both ask the contribution
gate, as do `LinkService` and `LinkProcessingService`, and `AssociationService`
and `AssociationOrchestrationService`. A service that is exposed but that
nothing routes to today gates all the same, because a check that leans on
current routing stops checking the moment a route is added, and stops silently.

What varies by altitude is which rules are asked, not whether they are asked
(`G2H Design.md` §14.6 rule 3). A foundation asks the row-level rules: authenticated, not blocked
by a `ReadOnly` role, permitted to write this row, permitted to see it. A
processing or orchestration service asks those again and adds the rules spanning
rows or states that a single-table service cannot see to ask. The gate sits at
each service's own entry points, once each, and is not restated down its private
helpers. No broker call is involved:

```csharp
// the foundation enforces the same security rules as the orchestration (design
// §14.6): an exposer may bind to either service directly, so no layer may assume
// an upstream layer already gated the caller

private static void ValidateUserIsAllowedToContribute(SecurityContext securityContext)
{
    if (securityContext is null || securityContext.IsAuthenticated is false)
    {
        throw new UnauthorizedContentItemException(
            message: "The current user is not authenticated.");
    }

    bool isBlocked =
        securityContext.Roles.Contains(Roles.ReadOnly)
            || securityContext.Roles.Contains(Roles.ContentItemReadOnly);

    if (isBlocked)
    {
        throw new UnauthorizedContentItemException(
            message: "The current user is blocked from contributing content items.");
    }
}

// The same gate asked of a KNOWN content type, so the narrow third tier of the block
// can be composed (design §18.6 rule 2). The two coarse names above cover every row
// whatever its type; this one has to be told which row is being written.
private static void ValidateUserIsAllowedToContribute(
    SecurityContext securityContext,
    ContentType contentType)
{
    ValidateUserIsAllowedToContribute(securityContext);
    ValidateUserIsNotBlockedFromContentType(securityContext, contentType);
}
```

`ContentItemProcessingService.Validations.cs` defines the same two gates over
again, against the same three role names, differing only in the exception type it
throws.

**Cross-table approval decisions go through `IAccessBroker` instead**, because
they depend on rows a single-entity service cannot see for itself — the
approval, its settings, its reviews and its comments. `IAccessBroker` gathers
those rows and hands them to the security client, which decides; it returns a
verdict, never the settings, so the decision logic has exactly one home rather
than seven (one per approvable entity). `ApprovalOrchestrationService.Decisions.cs`
asks it once per decision:

```csharp
// The ONE authorisation. Everything after this is bookkeeping and a sync — the
// question of whether this person may decide this approval is asked here, once,
// against the row rather than against anything the caller supplied (§16.7.1).
AccessVerdict verdict = await this.accessBroker.MayDecideApprovalByIdAsync(
    approvalId: approvalMatch.Id,
    decision: decision,
    isBypassRequested: isBypassRequested,
    bypassReason: bypassReason,
    securityContext: envelope.SecurityContext,
    cancellationToken: cancellationToken);

ValidateUserMayDecideApproval(verdict);
```

Rules:

1. Role and claim names must live in a central constants class (`Roles`) — no
   magic strings scattered through services.
2. Controllers must not perform business authorization; they rely on
   authentication middleware and standard policy attributes for coarse access
   only.
3. A caller is resolved from a principal in exactly one place, and Core reaches
   that resolution through exactly one call. A service calls `IEventEnvelopeBroker.CreateAsync`;
   `EventEnvelopeBroker` delegates to the public `IEventEnvelopeClient.CreateAsync`,
   which delegates to the envelope client's internal `EventEnvelopeService`, and
   that is where `ISecurityBroker.GetCurrentSecurityContextAsync()` is called. The
   security broker resolved the caller in its own constructor, from
   `IHttpContextAccessor.HttpContext?.User`, and normalises it through
   `ISecurityClient.Users`; the result returns as the client's
   `EventSecurityContext`, and `EventEnvelopeBroker` maps it onto Core's
   `SecurityContext`. Every layer above that call sees a context already made.

   Core mints no context from scratch, and derives two.
   `IEventEnvelopeBroker.CreateSystemAsync` and `CreateElevatedAsync` are both
   built on `CreateAsync` and then replace the `SecurityContext` it returned:
   the system mint records `SystemIdentity.UserId` as the actor, the elevated
   mint keeps the caller as the actor, and both set `IsSystemIdentity`, keep the
   triggering person on `DelegatedBySubjectId`, and drop `Roles` — the flag
   stands in for the publisher tier by itself, and carried roles would leave a
   context that looks authorised two ways. That is the system identity §EVN18(c)
   and §EVN18 rule 8 write under.

   Two consequences follow from where the principal is captured.
   `IEventEnvelopeBroker` is registered **scoped**, never singleton: the broker
   constructs its client eagerly and the security broker underneath reads the
   principal in its constructor, so a singleton would stamp every envelope in the
   process with whichever caller happened to be current the first time it was
   built. And on the event path there is no `HttpContext` to read at all, which is
   why an inbound envelope's context is carried and verified (§EVN15) rather than
   resolved again — `CreateNextAsync` copies the source envelope's
   `SecurityContext` forward unchanged.
4. Duplicate enforcement across layers is intended, never redundancy to be
   refactored away. Deleting a check because the layer above already makes it is
   a regression (`G2H Design.md` §14.6 rules 1–3).

## EVN18. Approval Workflow Wiring *(formerly §10.17)*

The approval workflow both **consumes** entity lifecycle facts and **causes**
entity writes. Wired naively that cycle does not terminate, so the wiring is
specified here rather than left to the implementation.

**Inbound — the subscription tier follows the entity's publication model, not
the layers it happens to have.**

Every approvable `EntityType` declares one publication model in
`EntityTypeVersioning`, mirroring `G2H Design.md` §7.5.1: **Versioned** if an amendment to a
terminal row forks a new row, **Single-Row** if the row that is edited is the
published row. That declaration decides which tier the workflow listens on, and
it is a lookup rather than a probe of the entity's runtime shape.

Whether a layer exists above the foundation is a different question with a
different answer, and conflating the two gets `Association` wrong.
`Association` is Single-Row and has `AssociationOrchestrationService` above its
foundation, and the workflow still binds to its foundation facts.

1. For the two **Versioned** types the orchestration subscribes to the
   **processing** service's `-Added` and `-Modified` facts — for `ContentItem`
   that is `ContentItemProcessing-Added` / `-Modified`, and for `Link`
   `LinkProcessing-Added` / `-Modified`. It subscribes to no approvable
   entity's `-Removed` at all; the workflow records' removals are the
   documented exception (§EVN18(a)). Per §EVN2 rule 6 it must not also subscribe to
   the foundation facts for the same reaction.

   For the five **Single-Row** types that participate today — `Tag`, `Comment`,
   `Reaction`, `BibleReference` and `Association` — it subscribes to the
   **foundation** facts. Nothing forks, so a foundation write and a completed
   amend are the same event, and the loop is broken by rule 4 below rather than
   by the subscription tier. `Association`'s orchestration changes nothing
   here: it resolves endpoints and runs the retrieve-or-add probe, holds no
   event broker and publishes no facts of its own, so the foundation's fact is
   the only one an amend produces.

   `-Submitted` is on the **foundation** address for all seven, `ContentItem`
   and `Link` included. Six of them reach it through a foundation submit
   transition. `Association` has no submit verb at all —
   `AssociationEventOperation` carries no `Submitting` and `IAssociationService`
   no submit method — so its `-Submitted` is published by the approve transition
   alone, when an administrator's override re-opens a decided row. Nothing above
   the foundation takes part in either route, so there is no processing fact to
   prefer.

   A **Versioned** type must have a processing service before it can
   participate in approval, for the reason in rule 2. `Attachment` is Versioned
   and has neither a processing service nor a foundation one, which is why it
   does not participate yet.
2. The reason is §EVN2 rule 5. A version fork used to write two foundation rows and
   therefore emit two foundation facts. Reacting to the second — the demotion of
   the previous latest — would have reset the still-published previous version's
   approval and dismissed its review history, for a write that changed only a
   bookkeeping flag.

   **The version tip has no demotion fact, because it has no demotion.** The
   tip is derived from the group's highest live `Version` rather than stored, so
   a fork writes one row and emits one foundation `-Added`. The misreading this
   rule guards against is therefore impossible rather than merely unsubscribed —
   stricter than the interim shape, which gave the tip demotion its own
   `<Entity>-Demoted` address so it could not be mistaken for a content
   amendment.

   **The published slot is a different demotion, and it does have a fact.**
   Granting approval to a new version has to clear the group's published row
   first, and `UnpublishContentItemByIdAsync` / `UnpublishLinkByIdAsync`
   publish `<Entity>-Unpublished` when they do. That address carries no request
   address and no subscriber: unpublishing is a step inside the swap rather than
   something a caller may ask for, and nothing reacts to it.

   The rule stands regardless of the tip demotion having gone. Rule 1's
   preference for the processing service's fact and rule 3's "a direct
   foundation write bypasses invalidation" are independent of it, and a
   `Versioned` entity still needs a processing service for those. On the foundation addresses a fork arrives as
   `-Added`, because the fork's write *is* an add: an amend is there
   indistinguishable from a first contribution, so a workflow bound to the
   foundation would open a fresh round for an edit and never hear the edit as an
   edit. The processing service emits exactly one fact per completed amend,
   named for the operation the caller asked for, which is the unit of work the
   approval workflow cares about — and it is the fork that makes this a *layer*
   question rather than an *orchestration* question, since the fork is
   single-entity processing work.
3. The consequence to accept deliberately, and it bites only on the Versioned
   pair: a write made directly against the foundation of a `ContentItem` or a
   `Link` bypasses approval invalidation, because the workflow is listening one
   tier up. Both are therefore written through their processing service, and an
   exposer must bind to that service rather than the foundation for either. For
   the Single-Row types the subscribed tier *is* the foundation, so no such gap
   exists and a foundation binding is correct.

**Inbound — the workflow's own records.** `ApprovalReview` and `ApprovalComment`
are a second inbound channel, and a different one: their facts do not
*invalidate* an approval, they prompt the workflow to **re-test the `G2H Design.md` §8.5
conditions** on an approval that may have been blocked. Both are foundation-tier
subscriptions — neither is an approvable entity, neither is an `EntityType`, and
`EntityTypeVersioning` has no row for either, so rules 1 and 2 do not apply and
there is no fork to misread.
Lettered here so the numbered rules above keep their cross-references, and the
letters are cited in their own right — `(a)` in service and test comments, `(b)`
in a service comment — so neither of those two is ever reassigned to a different
item.

**Legacy letter aliases — `§10.17(a)` is `§EVN18(a)`, and `§10.17(b)` is
`§EVN18(b)`.** The *(formerly §10.17)* annotation on the heading above carries
the unlettered number only, so it anchors the section but not a citation that
names a letter. The lettered forms code actually writes are spelled out here to
be that anchor: `§10.17(a)`, `§10.17 (a)`, `§10.17(b)`,
`§10.17 inbound (a)` and `§10.17 inbound item (a)`.

The rest of the run carries no alias and must not be read as one. This list is
lettered continuously where the earlier one interleaved `(b1)`–`(b3)`, so the
earlier `(c)` and `(d)` are `(g)` and `(h)` here — a bare `§10.17(c)` or
`§10.17(d)` names neither item reliably. Nothing outside this document cites
either.

- (a) **Subscribe to every fact address on both records — not a subset.** The
  `G2H Design.md` §8.5 evaluation reads comments through `IsDeleted is false && IsResolved is
  false`, and reviews through `IsDeleted is false && Verdict != Dismissed`.
  Every published fact can move one of those predicates, so all of them
  re-test:

  | Fact | How it moves the gate |
  | --- | --- |
  | `ApprovalComment-Added` | a comment born **outstanding** blocks an approval that was clear; one born settled moves nothing, which the re-test establishes rather than assumes |
  | `ApprovalComment-Modified` | the owner flipped `IsResolved` through the general modify |
  | `ApprovalComment-Resolved` | the owner **or** an administrator flipped it through the resolve transition |
  | `ApprovalComment-Removed` | soft-deleting an outstanding comment **unblocks**; `-HardRemoved` shares this address |
  | `ApprovalReview-Added` / `-Modified` | moves the approval count or raises a blocking rejection |
  | `ApprovalReview-Removed` | withdrawing an approving review drops the count; withdrawing a rejection unblocks |
  | `ApprovalReview-Dismissed` | a dismissed verdict leaves the active set |

  **Both comment resolution addresses are required.** `IsResolved` has two
  writers by design: the owner through modify, the owner or an administrator
  through the transition. Which one carried a given change depends on nothing
  more than which UI control was clicked, so watching one address would leave
  the gate movable unnoticed.

  **All eight are wired.** That contract is enforced by publishing rather than
  by a list: the integration suite derives its cases from the two operation
  enums — by excluding requests, never by matching a past-tense suffix, since a
  fact need not end in "ed" — and publishes every one through the real broker,
  asserting each is accepted and re-tests its round. A fact operation added
  later arrives with a case already attached, and that case fails until it is
  both subscribed and given an accepted name.

  Two of the eight addresses carry **two** event names apiece: `HardRemoved` is
  published to the `Removed` address on purpose, and the event name is bound
  into the envelope's signature. A handler on a shared address therefore
  verifies against the **set** of names that address can legitimately carry —
  the publisher's composition inverted — rather than a single name, which would
  refuse half its traffic silently.
- (b) **Re-test, do not assume.** No fact means "the approval may now complete"
  — it means the inputs changed. The handler re-runs the whole `G2H Design.md` §8.5 evaluation.
  Facts that move the gate *shut* matter as much as those that open it: a
  comment born outstanding, or a withdrawn approving review, can re-block an
  approval that was clear, which is exactly the case
  `AutoApproveIfAllApprovalRequirementsMet` would otherwise get wrong. Equally,
  a fact may move nothing at all — a comment born settled is the common case —
  which is why the handler re-evaluates instead of inferring a direction from
  the address.
- (c) **The entity under review is the inbound source that causes dismissal.** When an item subject to approval is added or
  amended, the orchestration receives that fact (rules 1–3 above decide at
  which tier) and, from the effective `ApprovalSetting`, determines that the
  existing verdicts no longer describe the current content. It then sets
  **every active `ApprovalReview` on that approval to `Dismissed`**. The re-file
  route depends entirely on it, and **that route is now reachable**: the
  service exists, the subscription is wired, and a superseded reviewer's slot is
  cleared automatically by the content change that superseded it.

  The dismissal runs under the **system identity**, not the editor's. No role
  carries authority to dismiss, so the workflow mints its own context in
  process rather than borrowing an authority that exists for nobody.

  This is now the ONLY thing that dismisses a review. No user action does, and
  none can: the public verb and the request address a person could once have
  used are both gone, and the gate refuses any caller that is not the workflow.
- (d) **The one fact this service causes itself is suppressed while it causes
  it.** The stale-review reset dismisses in a loop, and each dismissal
  publishes `ApprovalReview-Dismissed` — an address (a) requires a subscriber
  for. Substrate delivery is synchronous, so an unguarded handler would re-test
  the round *inside* that loop, once per review, each time against a
  population still being torn down; with
  `AutoApproveIfAllApprovalRequirementsMet` on it can approve off a review set
  that never existed in storage as a settled state. The loop therefore
  announces the approval it is dismissing and the handler stands down **for
  that approval only** — suppressing the re-test, never the signature check,
  and restoring in a `finally` so a throw inside the loop cannot leak the
  suppression. The dismissing flow re-evaluates once at the end, which is the
  correct single evaluation for the whole act.

  This is a third line of defence alongside rules 6-7 below, and it is
  narrower than either: it is scoped to one approval, for the duration of one
  loop, on one address. No human route to a `Dismissed` verdict exists, so
  every dismissal this address carries is the workflow's own.

  **The subscription is currently unreachable, and is retained deliberately.**
  A concurrent *different* round does not reach it either. `ApprovalReview-Dismissed`
  has exactly one publisher, reached by exactly one caller, and that caller sets
  the suppression before it publishes; delivery is synchronous on the
  publisher's execution context and the guard is an `AsyncLocal`, so every
  production publish lands inside its own window. Measured on the real
  substrate: two overlapping resets, four deliveries, zero re-tests.

  **Settled: it is kept.** Rule (a) above requires a subscriber on **every**
  fact address, and that universal is enforced by a test derived from the
  operation enum precisely so it cannot be hand-carved. Removing this one
  subscription would carve the first exception into that invariant — and the
  invariant is the thing worth protecting, because it is what stops a fact
  going unheard by accident. The cost of keeping is one suppressed delivery per
  dismissal; the cost of removing is a weaker rule for every address.

  The guard's *scoping* — one approval rather than all — also remains a
  genuine property, pinned by
  `ApprovalOrchestrationServiceTests.DismissalReEntrancy`, which re-enters the
  handler from inside the reset loop with an unrelated approval id and asserts
  that round is still re-tested. Inside the window is the only place a second
  publisher can arrive from, because the window stands open for the whole loop. A repair pass or an
  administrative tool that dismissed outside the reset loop would need exactly
  this subscription, and would find it already correct.

  Recorded here rather than left implicit so that a later reader finding an
  unreachable handler does not mistake it for an oversight.

- (e) **`Approval`'s own facts are not a further inbound source.**
  `Approval-Added` and `Approval-Modified` are published by the foundation, but
  no workflow handler is bound to either. A change made directly to an approval
  record therefore does not itself re-test the round; the re-test is driven by
  the entity's facts and the workflow records' facts above, and by the flows
  that write the approval and evaluate within the same call.
- (f) **The decision is not the orchestration's to compute.** It receives a
  fact, gathers what the evaluation needs, and asks; the answer — block,
  permit, or auto-approve — comes back from the decision function. The
  orchestration owns the *reaction*, never the *rule*.
- (g) **`-Dismissed` is a distinct address precisely so this reaction can tell
  a withdrawn verdict from an amended one**, and `-Resolved` serves the same
  purpose for a comment.
- (h) **The cycle rule still binds.** Re-testing may cause an approval
  decision, and that decision must go out through the transition verb of rules
  4–5, never as a `-Modified` on the workflow record that triggered it.

**Outbound — approval-caused writes use a transition verb, never
`-Modifying`.**

4. Every write the approval workflow causes on an entity's approval state goes
   through `Transition<Entity>ApprovalAsync` on the owning foundation service.
   The **request** is `<Entity>-Approving`; the **fact** is `<Entity>-Approved`,
   `<Entity>-Rejected` or `<Entity>-Submitted`, chosen by the decision the
   command carried rather than by the verb that was asked for. Those three are
   the whole set — the transition refuses any other target — so every decision
   lands on exactly one of them, and a subscriber that needs all of the
   workflow's decisions binds all three. Each is its own address: `-Approved` on
   its own hears the approvals and hears nothing of the rejections or the
   administrator resets, and a rejection broadcast on the `-Approved` address
   would tell a subscriber the opposite of what happened. §EVN2 rule 7 already
   establishes this vocabulary — a transition owning a narrower field scope than
   a general modify is a separate method and therefore a separate verb, and its
   fact need not echo its request. The transition's scope is the whole of
   `IApproval`, so no separate publish verb is required.
5. This operation writes only the `IApproval` members, and **must not** publish
   `<Entity>-Modified` or `<Entity>-Added`. Those two are the addresses rule 1's
   invalidation subscriptions bind to, so an approval-caused write cannot
   re-enter the handler that caused it. Within that field scope `ApprovalStatus`
   is copied from the command; `IsPublished` and `PublishDate` are gated on the
   decision, so publication survives only where the target is `Approved` and the
   date is cleared with the flag; and the bypass pair is taken from the access
   verdict rather than from the caller. Nothing outside `IApproval` is widened
   into — the `ShortCode` derivation `G2H Design.md` §19.7 places at a group's first publish is
   designed and not built, and no column exists for it.

   `<Entity>-Submitted` is the one subscribed address the workflow can also
   cause, and it is deliberate. An administrator reset drives the entity back to
   `Submitted` through this same verb, so the fact lands on the address the
   submit ear listens to. The re-entry is a single hop: the handler re-tests a
   round whose active reviews the reset has already dismissed, and whatever it
   decides leaves on `-Approved` or `-Rejected`, which nothing subscribes to.

   Two further facts follow a Versioned approval, and neither closes a loop. The
   publication swap publishes `<Entity>-Unpublished` when it clears the
   incumbent (rule 2), and the processing service publishes
   `<Entity>Processing-Approved` once the decision has landed — after the swap
   as well, on the decisions that run one. That second fact goes out for every
   decision the processing tier handles, rejections and resets included, because
   it reports that tier's unit of work finished rather than reporting a
   promotion (§EVN2). Both addresses exist so that a subscriber *could* be told
   the group-level work is done; neither has one today.

   **An attachment does not yet ride on its host's approval.** `G2H Design.md` §5.6.5 rules that
   an attachment's approval derives from the host that displays it — the host's
   purposefully-placed and inline-referenced attachments submitted and then
   approved with it, bypass-audited — and because that flow would use transition
   verbs, rules 4–5 and the cycle-breaker would hold unchanged. None of it is
   built: `Attachment` has no foundation service, its event operations stop at
   `Added` / `Modified` / `Removed` with no submit or approve among them, and
   the association endpoint arm for an `Attachment` throws until that service
   exists.

**Why `ProcessedEvents` is not sufficient on its own.**

6. `ProcessedEvents` is unique on `(EventId, ReceiverName)` and stops
   *redeliveries of one event*. It does not stop *new events caused by a
   handler's own write*: a write-back publishes on an envelope minted by
   `CreateNextAsync` with a **fresh** `EventId`, which the receiver has never
   seen. Under the inline dispatch of §EVN11 the repetition would be synchronous
   re-entry inside the original request.
7. The changed-field gate is the second line of defence. Rules 1 and 4 above
   are the first.

**Ownership of the entity write.**

8. `ApprovalOrchestrationService` holds no entity services, so it does not
   perform the entity write itself. It publishes an `-Approving` **command** on
   the entity's own request address — the foundation's for the five Single-Row
   types, the processing service's for `ContentItem` and `Link`, which have to
   clear the group's published slot before they promote — under the system
   identity, and the entity's own request handler performs the write.

   The distinction that matters is request against fact. The command goes to a
   request address the entity already owns; it is not an approval fact published
   for the entity's own layers to react to. An approval fact in its place would
   require every approvable entity's orchestration to subscribe to approval
   facts, which reintroduces the cycle at one remove. The `Approval` row is written first and the entity
   follows, because `G2H Design.md` §9.8 names the approval the source of truth — so a repair
   pass can only ever mean "drive the entity to match the approval".

## EVN19. Write and Publish Atomicity — ruled, not built *(formerly §10.18)*

Every write in the §EVN11 foundation shape commits its row and **then** publishes
the fact announcing it, with nothing binding the two. If the publish throws —
an unreachable event store, or every configured signing key's validity window
having lapsed or left a gap over *now* — the row stays and the fact never goes
out. A *wholly unconfigured* host no longer reaches this: `EnvelopeIntegrityBroker`
refuses at construction, so every Core endpoint fails before it can write. That
closed the case that was actually observed; it did not close the shape, because
a lapsed window still throws at signing time, which is after the write. The
caller receives a dependency error and cannot tell an add that failed outright
from one that half-succeeded; neither can the next request.

For `ContentItem` that is not merely untidy. The duplicate-content probe is
global and unfiltered **by design**, so a row stranded this way is
indistinguishable from a genuine earlier contribution: every retry of the same
content is permanently barred, and the contributor can neither resubmit nor see
the row that is blocking them. The retry is not even refused out loud — the
service thanks them and writes nothing — so the failure mode is silent from the
contributor's side, which makes the stranding worth catching on the write path
rather than on a complaint. Entities without a content-uniqueness rule degrade
more quietly — a row, no fact, and a subscriber whose state never advanced.

The two databases make a truly atomic pair impossible: the row lives in
`Glory2Him.Core` and the event in `Glory2Him.Events`, and no transaction spans
them. What is ruled here is therefore not how to make the pair atomic, but
**which half is allowed to be late**.

**The ruling: the write is atomic with the _intent_ to publish, and the publish
itself is at-least-once.**

1. **One Core transaction covers the row, the outbox row, and whatever
   `ProcessedEvent` records the path writes.** The entity write, a durable
   outbox row carrying the fact about to be announced, and — where the write is
   reached through a request address — the inbound envelope's `ProcessedEvent`
   and the outbound envelope's `ProcessedEvent` all commit together or not at
   all. They are all in `Glory2Him.Core`, which is what makes one transaction
   sufficient. Nothing in the transaction touches the event store.

   **The `ProcessedEvent` pair belongs to the substrate path rather than to the
   write.** Both records are keyed on `(EventId, ReceiverName)` where the
   receiver is a request handler: the inbound record marks the delivery that
   asked for this write so a redelivery of it is skipped, and the outbound
   record pre-claims the published fact's id against that same handler so the
   fact cannot loop back into it. A transition with no request address of its
   own has no handler to name and therefore no receiver to key either record on.
   Three publish a fact and write neither —
   `ApprovalReviewService.DismissStaleApprovalReviewAsync` onto
   `ApprovalReview-Dismissed`,
   `ApprovalReviewRequestService.RetireAnsweredApprovalReviewRequestAsync` onto
   `ApprovalReviewRequest-Removed`, and
   `AIReviewerAssignmentService.ReturnStaleAIReviewerAssignmentToPendingAsync`
   onto `AIReviewerAssignment-Modified`. Each is reached only by a direct
   in-process call from `ApprovalOrchestrationService`, on an envelope the
   service mints for itself under the system identity, so there is no inbound
   delivery to deduplicate and no request handler of its own to key the outbound
   record against. Two of the three facts carry no subscriber at all;
   `ApprovalReview-Dismissed` carries the one §EVN18(a) requires, and re-entry
   there is held off by the reset loop's suppression window rather than by a
   `ProcessedEvent` row (§EVN18(d)).
   Under this ruling they take the row and the outbox row and nothing else, and
   rules 2 and 5-7 hold for them unchanged: what makes a failed publish
   recoverable is the outbox row, never the dedup pair.

   Three transitions sit the other way round, and are named here so the shape is
   not read as a rule. `UnpublishContentItemByIdAsync`,
   `UnpublishLinkByIdAsync` and `SortAssociationAsync` have no request address
   either (§EVN2), yet they record the pair against
   `"ContentItem.OnContentItemUnpublished"`, `"Link.OnLinkUnpublished"` and
   `"AssociationService.OnSortingAssociation"` — receiver names that no handler
   and no subscription owns. Those rows have no reader rather than a wrong one, and the
   transaction covers them as it covers any other row the path writes. Which is
   why this rule is stated in terms of what the path writes rather than in terms
   of a fixed pair: the pair follows the handler, and the atomicity being ruled
   here follows the row and the outbox.

2. **The publish happens after that commit, and can no longer strand the row**
   — the intent to publish committed with it, so a failed publish is a fact
   that has not gone out *yet*, not a fact that is lost. A row with a pending
   outbox entry is a completed write, and is treated as one everywhere.

3. **Rolling the row back on a failed publish is refused on mechanism, not
   preference.** It is the obvious alternative and it does not work here. Per
   §EVN11 every publish persists the event and then dispatches it **inline** to
   the in-process handlers subscribed to that address, and
   `EventSubscriptionRegistration` opens a fresh DI scope **per delivery** — so
   each handler gets its own `StorageBroker`, its own `DbContext`, and
   therefore its own connection, deliberately and for thread-safety. A
   transaction held open across the publish would hide the uncommitted row
   from the very handlers that must read it, and they cannot enlist in it. The
   window also cannot be closed from the other side: a publish that succeeds
   and a commit that then fails would announce a row that does not exist,
   which is worse than a row whose fact is late — subscribers acting on a
   phantom cannot be undone, whereas a late fact converges.

4. **The guarantee becomes at-least-once, and receivers are already safe for
   it.** A foundation request handler checks `ProcessedEvents`, unique on
   `EventId` + `ReceiverName`, and a deduplicated delivery replies `null`, so a
   redelivered envelope is a no-op there. Above the foundation nothing checks
   that table: a redelivered fact is handled again, and safety rests on the
   handler re-evaluating the round rather than applying a delta. Both properties
   exist already; neither is new work.

5. **The outbox stores the envelope minted before the commit and the
   destination it is owed to; a retry republishes exactly that, and only the
   integrity block is produced fresh.** Held verbatim is everything the
   signature is computed over — `Metadata` in full, `EventId` included, plus
   `Content`, `SecurityContext` and `RequestContext` — together with the
   destination, which has to be stored because nothing on the envelope carries
   it (§EVN10): `EventBroker` composes the signed event name from the entity
   name and the operation, and resolves the address from the operation, so the
   row records those alongside the envelope. The direction needs no column —
   the outbox holds facts, a fact publishes as a `Request`, and a reply is
   returned inline on its own delivery record and never reaches an outbox row.
   The relay must never re-mint any of it: a re-minted envelope carries a
   fresh `EventId` and would defeat rule 4's dedup, turning one fact into
   many.

   `Integrity` is the deliberate exception, because an envelope minted before
   the commit has no signature to store — the factory does not know the
   destination, so signing happens at publish (§EVN6, §EVN10), and
   `IEventBroker.Publish*Async` takes an unsigned envelope and signs inside
   the broker, so a stored signature could not survive the publish seam even
   if the row held one. Each attempt therefore produces a new `Signature`, a
   new `SignedDate`, and whichever `KeyId` is active at that moment. Those
   three are the only fields that may differ between attempts, and none of
   them is inside the signature, so a retried envelope is identical to the
   first in everything verification reads: `VerifyAsync` never looks at
   `SignedDate`, and reads `KeyId` only to choose which configured secret to
   recompute against. A fact that waits across a rotation therefore goes out
   under the new key, and the retired key stays configured for the facts
   already dispatched under it. `SignedDate` records when an attempt was
   signed rather than when the write committed, which leaves
   `RequestContext.RequestedDate` the time the fact belongs to. Re-signing is
   also what lets a host whose signing keys had all lapsed at write time
   dispatch the fact once an active key covers the moment of the attempt,
   rather than losing it.

6. **Dispatch is attempted inline immediately after the commit; the outbox is
   the fallback, not the normal path.** On success the outbox row is marked
   dispatched. On failure it stays pending and **the caller still sees
   success** — the write completed, which is what the caller asked for, and
   this is the behaviour change the ruling deliberately makes. If the
   mark-dispatched write itself fails, the row stays pending and the relay
   republishes; rule 4 makes that a no-op.

7. **Pending rows dispatch in commit order, and a stuck fact delays later
   facts rather than reordering them.** A sweep dispatches pending rows oldest
   first and stops at the first one that fails, so a fact never overtakes an
   earlier fact about the same row. Blocking is bounded rather than permanent:
   once a row has failed a set number of attempts it moves to a terminal
   state, and subsequent sweeps step over it so one poison fact cannot hold the
   queue for ever. A terminal row is never deleted and never silently dropped.
   Terminal rows are an operational signal and must surface as one. Retention
   of *dispatched* rows is a housekeeping decision, not a correctness one.

8. **The seam belongs to the broker; the outbox belongs to Core.** Transaction
   scope is a broker concern, and `IStorageBroker` today exposes transactions
   only through the storage client's bulk operations — single-entity writes
   have no such seam, so one is designed for them rather than borrowed from the
   bulk path. The outbox table is a Core table, because Core is the only
   database the row and the transaction share. It is not part of the event
   store: `Glory2Him.Events` remains the durable truth of *published* events,
   and the outbox is the durable truth of *owed* ones. The relay is a
   Core-side sweep; until background-job infrastructure exists it runs on the
   inline path of rule 6 plus a manually invoked operation.

9. **The duplicate probe does not change, and that is a decision rather than
   an omission.** The content-exists check stays global and unfiltered and
   does **not** discount rows whose fact is still pending. A row whose fact
   has not gone out is still a row; making a content rule conditional on event
   state would put it at the mercy of the event store, and would hand a caller
   a way to manufacture a row that does not count. Under this ruling the
   question is moot in practice — the fact is owed, not lost — but the rule is
   stated so that it stays true if the guarantee is ever revisited.

10. **This is the service template, not one service.** Every
    `Do<Verb><Entity>Async` that writes and then publishes takes this shape; a
    service that opts out reintroduces the defect for its entity. §EVN2 rule 5 is
    unchanged in substance — a service still publishes exactly one fact about
    its own completed unit of work — but "once the work is done" now means once
    the work is *committed*, with the fact following. Reads publish no fact, so
    this rule does not touch them. Hard removal is not exempt either: where an
    entity declares the operation, `DoHardRemove<Entity>ByIdAsync` writes and
    then publishes `HardRemoved` (§EVN2 rule 4, §EVN11), so it follows this
    template exactly as any other write does.

11. **A composing layer gets the weaker half of this, deliberately.** Rules 1–2
    bind a fact to the write it announces, which a foundation owns. A
    processing or orchestration service composes lower-layer writes that have
    each already committed — and each already announced its own foundation
    fact — so there is no single row for its own fact to be atomic with, and
    The Standard gives it no unit of work spanning the services it called. Its
    outbox row is therefore written in its own transaction as the last step of
    the process: the layer fact becomes durable and retryable (rules 4–7), but
    not atomic with the process it reports. That residual gap is named here
    rather than papered over — a process that fails after its last foundation
    write and before its outbox row still owes a layer fact that will never be
    sent, and closing it would need a unit of work across services that does
    not exist today.

## EVN20. Design Principles *(new; from EventSubstrate.md §2-3, §30)*

> **A fact address announces what happened. A request address carries what is
> being asked for.**

The substrate carries both families, and §EVN2 tells them apart by tense: a
past-tense address is a **fact**, published once a service's own unit of work is
done, for any number of subscribers — including none — to react to; a
present-participle address is a **request**, delivered to the one service that
owns the operation. Almost every rule below depends on which family an address
belongs to, so that is settled first.

A request is not always a command. The family holds two kinds, and only one of
them changes anything: a **command** asks the owning service to do something
(`-Adding`, `-Modifying`, `-RemovingById`, `-Approving`), and a **query** asks it
for data and leaves the system as it found it (`-RetrievingById`). Neither is a
reaction to something that happened — that is what separates the whole request
family from the fact family — so an event on this substrate is not, by nature,
either a notification or a reaction. It is whichever of the three its address
says it is.

**Facts are announcements.** The publisher describes its own completed unit of
work (§EVN2 rule 5) and does not know what is bound to the address; the set can
change without it. A fact therefore never names a receiver and never says what
to do next. Nothing replies to a fact either — a handler returning the inbound
envelope would put its own name on a fact another service published.

**Requests are addressed, and that is deliberate.** A request address belongs to
the service that owns the operation, is bound to exactly one handler — the
`On<Verb><Entity>Async` method on that service (§EVN11, §EVN14) — and carries
the data to act on. Three in four of the subscriptions in
`EventSubscriptionRegistration` are request handlers, so the request family is
the larger half of the wiring rather than a corner of it. What is published onto
it is narrower: the only request this system publishes today is the `-Approving`
command the approval workflow sends on each decision (§EVN18 rule 8). A handler may return a reply, which
the broker signs with `EnvelopeDirection.Reply`, stores on the delivery row and
hands back in `EventPublishResult.Deliveries`.

**A query is a request that asks and does not tell.** `-RetrievingById` is the
joint-largest request operation on the substrate — fifteen subscriptions, level
with `-Adding` and `-RemovingById` — so reading is wired on every entity that
answers requests at all, not on a chosen few. It publishes no fact, because nothing happened worth announcing; the
answer comes back on the reply channel above, which is the whole reason that
channel exists. It also skips the `ProcessedEvents` bookkeeping a foundation's
mutating handlers perform: a read is naturally idempotent, so a redelivered
query costs a second read and nothing else. That exemption is the practical test for which
kind of request an address carries — if delivering it twice would be wrong, it
is a command.

Sending a **command** over the substrate is bounded rather than open. A query is
bound only by the first of these, since it changes nothing for the others to
protect. A command holds only when:

- **The address already belongs to the receiving service**, which owns the
  operation and the rules that govern it. A publisher never invents an address
  on another service in order to instruct it.
- **Exactly one handler owns it.** A command delivered twice is two writes.
- **There is a real reason not to call the service directly.** Today there is
  one: `ApprovalOrchestrationService` deliberately depends on none of the seven
  entity services, because taking all seven to write one decision is a shape
  already on record as breaking, so the decision leaves as an
  `<Entity>-Approving` command on the entity's own request address and the
  entity performs its own write (§EVN18 rule 8).
- **Order that must hold is held by the call stack, not by delivery.** Handler
  failures are recorded per listener instead of failing the publisher, so a
  sequence expressed as two publishes can half-happen. The publication swap is
  two sequential awaits inside one processing method for exactly that reason.

Avoiding event spaghetti:

1. Required business flow belongs in a call chain owned by one service — an
   orchestration, or a processing service where the work is on a single entity —
   not in a chain of services reacting to each other's facts.
2. A reaction lives in the subscribing service's own `.Substrate` partial. There
   is no handler class to put one in (§EVN14).
3. A fact describes what happened, never what to do next. Telling a service to
   act, and asking it for data, both belong on its own request address — the
   first under the conditions above, the second freely — and neither belongs on
   a fact.
4. A foundation's mutating handlers are made idempotent explicitly:
   `ProcessedEvents` is unique on `EventId` + `ReceiverName` and a deduplicated
   delivery replies `null`, so a redelivered envelope is a no-op there (§EVN19
   rule 4). Read handlers keep no such record and are exempt by nature, which is
   the distinction rule 3 turns on. Above the foundation nothing consults that
   table: the two processing services' request handlers and every
   `ApprovalOrchestrationService` fact handler run again on a redelivery. What
   makes that tolerable for the fact handlers is that they re-evaluate the round
   rather than apply a delta — idempotence by construction, not deduplication —
   and a handler added above the foundation that applies a delta owns the check
   that makes it safe.
5. Do not rely on the relative order of two subscribers on one address, or on
   the order of two publishes. No address carries two subscriptions today, so
   the first half constrains future wiring; the second bites now.
6. Do not use events to avoid a service boundary. An event whose only purpose is
   to let a service reach past its declared dependencies is the boundary being
   dodged rather than honoured, which is why a command over the substrate has to
   name its reason out loud.
7. The event is persisted before it is dispatched: the substrate writes the
   stored event row and then delivers inline, so a delivered event is always a
   stored one. There is no external delivery to persist ahead of — nothing fans
   events out beyond this process (§EVN22).
8. Keep event contracts stable. There is no event schema versioning (§EVN22):
   `Metadata.Version` records a version and nothing negotiates one, so a changed
   payload shape has no second version to fall back on.
9. Correlation and causation travel on every hop. `CreateNextAsync` mints a
   fresh `EventId`, sets `CausationId` to the source event, and carries the
   request and security contexts forward (§EVN11).
10. Every handler must be safe against redelivery. The substrate can redeliver a
    stored event, and it replays the identical signed bytes rather than minting a
    fresh envelope — which is what makes rule 4's dedup on `EventId` work, and
    why `RetryCount` never increments (§EVN9). There is no replay operator that
    re-runs history (§EVN22); replay-safety is a property every handler carries,
    not a feature something offers.

## EVN21. Future Pattern: Intentional Dispatch Events *(new; from EventSubstrate.md §34)*

**Not yet used anywhere in this codebase.** Documented as a considered pattern
for if and when it is needed, not as current design.

Publishing a command onto a service's own request address is current design, not
an exception to it — §EVN20 sets the conditions and
`ApprovalOrchestrationService` meets them on every decision. What is not built
is this particular shape of it: a command **fanned out per object across a
graph** whose size and membership are unknown until an incoming external signal
arrives, so the publisher cannot enumerate the calls it would otherwise make.
That is the case this section answers. Use carefully.

**The scenario this answers:** an orchestration service takes a graph of related
objects in from outside — a syndicated contribution arriving as a `ContentItem`
with its `Tag`, `BibleReference` and `Association` rows, say — and needs to
create each locally, in the correct order. It could call each foundation service
directly in sequence, but if the graph is large, the object types are variable,
or the creation logic needs to be owned by each domain service independently,
the orchestration can instead **publish one command per object onto that
object's owning service** and let that service receive it internally. Nothing in
this system imports a graph this way; the entities are named so the shape can be
weighed against real ownership boundaries rather than invented ones.

The event in this pattern is still **intent**, not reaction — the orchestration
is making a deliberate routing decision.

| Characteristic | Reaction event | Intentional dispatch event |
| --- | --- | --- |
| Emitted because | Something already happened | A deliberate routing decision is being made |
| Handler/receiver is | Optional / loosely coupled | Expected and required |
| Order matters | Usually not | Often yes |
| Who owns the receiver | The reacting service | The domain service responsible for that object type |

Naming needs no rule of its own here, and inventing one is the mistake §EVN2
already prevents. The command goes onto the receiving service's own request
address in the present participle — `ContentItem-Adding`, `Tag-Adding`,
`BibleReference-Adding` — one delivery per object. Tense carries the meaning, so
a past-tense address would be wrong however deliberate the dispatch: `-Added`
announces a row that exists, and this one does not yet. Nor does the import earn
an address of its own, because creating a row is CRUD and §EVN2 rule 7 admits a
new verb only for an operation CRUD cannot express.

Use this pattern when the graph is large with each object type owned by its own
domain service, each service should own its own creation/idempotency logic, the
orchestration should not know each domain service's internals, or objects need
independent replay/retry and per-object auditability. Do not use it when the
graph is small and fixed (call foundation services directly), order is strictly
enforced and cannot be guaranteed by event dispatch, or the receiver is not
guaranteed to exist.

If objects in the graph depend on each other, the orchestration must either
publish events in dependency order and await each receiver before the next, or
call dependent foundation services directly first and publish events for the
rest.

## EVN22. Superseded Draft — what `EventSubstrate.md` originally sketched

The full original text is fully recoverable from git history — this section
exists so a reader does not have to go looking for it to know what was there and
why it isn't presented as current design.

`EventSubstrate.md` (2591 lines) was an early pattern sketch, written before the
real implementation existed, using a fictional Student/Enrollment/Timetable
domain throughout. Concepts it proposed that were **not** what got built:

- **`IEventReceiver<TEvent>`**, a DI-resolved generic interface per event type,
  explicitly implemented on foundation services. The real system uses
  delegate-based subscription instead — `EventBroker.SubscribeToTagEventAsync`
  and one such method per entity, each taking a
  `Func<EventEnvelope<T>, CancellationToken, ValueTask>` handler.
- **Generic `StoredEvent` / `EventDelivery` / `DeadLetteredEvent` tables** and an
  `IEventStorageBroker` owning them directly. The real system delegates all of
  this to EventHighway (§EVN11) rather than building custom storage.
- **External REST fan-out** (`IExternalEventDispatcher`, `EventRoute`,
  `RestExternalEventDispatcher`) and a **retry/dead-letter policy** with a
  background `ExternalEventDeliveryWorker`. None of this exists; nothing in the
  current system fans events out externally.
- **Explicit `ReplayAsync`** against the stored event table. Not implemented;
  EventHighway's own durability is what the real system relies on.
- **`EventOrdering` / stream-based sequencing**, and **event schema
  versioning** (`StudentCreatedEventV1`/`V2`) as a first-class concept. Neither
  exists today.
- The entire worked example in the original document's §17–§26 (Student,
  Enrollment, Timetable, Notification services, their DI registration) was
  illustrative scaffolding, never real code in this repository.

What did survive, corrected and carried forward into this document: the
signing rationale (§EVN10), the event-spaghetti-avoidance rules (§EVN20), and
the intentional-dispatch pattern (§EVN21) — none of these depended on the
discarded scheme. The sketch's intent-versus-reaction maxim is **not** among
them: it drew the line between a service call and an event, whereas this system
draws it between a request address and a fact address and carries commands on
the first (§EVN20, §EVN18 rule 8).

To recover the original document in full: `git log --follow -- Documentation/EventSubstrate.md`
finds the commits; the file existed at that path up to the commit that unified
it into this one.
