# Events

Unifies `G2H Design.md` §10 "Event Design" and the standalone `EventSubstrate.md`
into one authoritative document, removing the duplication between them.

Section numbers below carry an **`EVN` prefix** (`EVN1`, `EVN2`, ...) — flat,
not restarted-with-decimals — so a bare `§EVN4` is unambiguous once other
`Documentation/Design/*.md` files exist with their own prefixes (`ARC`, `DOM`,
`SEC`, `UI`) and their own local numbering. This repository's C# comments cite
design sections extensively (61 files cite `§10.X` alone from this section's
former life in `G2H Design.md`), so every section also carries a
*(formerly §10.X)* annotation — the literal string `§10.X` still appears on the
right heading, so an old citation resolves by grep even though the citable
number itself is now prefixed and did not survive verbatim.

## 0. What changed in this unification

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
   `EnvelopeIntegrityBrokerTests`. §10 below carries the corrected, current
   description; §10's reasoning for *why* the signature must bind the destination
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
once the work is done. Because the subject identifies the service, the verbs stay
the standard CRUD set at every layer and never have to be reinvented to avoid
collisions:

| Service | Request addresses | Fact addresses |
| --- | --- | --- |
| `ContentItemService` (foundation) | `ContentItem-Adding`, `ContentItem-Modifying`, `ContentItem-RemovingById`, `ContentItem-HardRemovingById`, `ContentItem-RetrievingById` | `ContentItem-Added`, `ContentItem-Modified`, `ContentItem-Removed` |
| `ContentItemProcessingService` | `ContentItemProcessing-Adding`, `ContentItemProcessing-Modifying`, `ContentItemProcessing-RemovingById` | `ContentItemProcessing-Added`, `ContentItemProcessing-Modified`, `ContentItemProcessing-Removed` |
| `LinkProcessingService` | `LinkProcessing-Adding`, `LinkProcessing-Modifying`, `LinkProcessing-RemovingById`, `LinkProcessing-RetrievingById` | `LinkProcessing-Added`, `LinkProcessing-Modified`, `LinkProcessing-Removed` |

1. Create operations emit an `-Added` fact.
2. Update operations emit a `-Modified` fact.
3. Soft delete operations emit a `-Removed` fact.
4. No hard delete facts are required because hard deletes are not planned.
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
   `Approving`/`Approved` or `Publishing`/`Published` owns a narrower field scope
   than a general modify, so it is a separate method and therefore a separate
   verb.
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

Hard deletes are not planned.

Soft delete should be implemented through:

```csharp
public string? DeletedBy { get; set; }
public DateTimeOffset? DeletedWhen { get; set; }
public string? DeletionReason { get; set; }
```

An entity is considered deleted when `DeletedWhen` is not null.

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
payload alongside security, request, and event metadata.

```csharp
public sealed class EventEnvelope<T>
{
    public T Content { get; init; }

    public SecurityContext SecurityContext { get; init; }

    public RequestContext RequestContext { get; init; }

    public EventMetadata Metadata { get; init; }
}
```

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
    public IReadOnlyList<string> Roles { get; init; }

    public IReadOnlyList<string> Scopes { get; init; }

    public IReadOnlyList<string> Permissions { get; init; }

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

`SecurityContext` should be built from the `ClaimsPrincipal` provided by ASP.NET
Core Identity and OpenIddict. A `securityContextFactory` at the entry point is
responsible for this normalization. The rest of the application must not depend
on `ClaimsPrincipal` directly.

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

### 7.1 Authentication Flow Examples

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

    public string EventType { get; init; }

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

Example causation chain:

```text
API Request
CorrelationId: A

StudentCreated
EventId: 1
CorrelationId: A

AddressCreated
EventId: 2
CorrelationId: A
CausationId: 1

AuditLogged
EventId: 3
CorrelationId: A
CausationId: 2
```

## EVN10. Envelope Integrity — Signing *(new; corrects EventSubstrate.md §5.10)*

**Implemented.** `IEnvelopeIntegrityBroker.SignAsync` / `VerifyAsync` are real,
used by every `EventBroker` publish and every substrate handler's verification,
and covered by `EnvelopeIntegrityBrokerTests`. This corrects `EventSubstrate.md`
§5.10, which described this as "Not implemented" — that was accurate when
written and is no longer accurate. What follows is `EventSubstrate.md` §5.10's
reasoning, carried forward because the implementation that was actually built
follows it: the "why" survived even though the surrounding scheme did not.

The signature covers:

1. `SecurityContext`
2. `RequestContext`
3. A hash of `Content`
4. `Metadata` — in full
5. The **composed event name** (`$"{entityName}{operation}"`), supplied by the
   caller
6. A **direction discriminator** — `request` or `reply`

**Signing only `SecurityContext` and `RequestContext` does not work.** A
signature over identity alone is a transplantable bearer token: capture any one
legitimately signed envelope, lift its signed `SecurityContext` onto different
`Content` at a different destination, and it still verifies. The attacker then
authors any request they like as that actor — which is the whole property the
signature was supposed to deny them.

Four things about the list above are easy to get wrong:

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
schema selector — forgeable over hashed content, which is a downgrade attack
against the hash itself.

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
    public string Algorithm { get; init; } = "HMACSHA256";

    public string Signature { get; init; } = string.Empty;

    public DateTimeOffset SignedDate { get; init; }
}
```

```csharp
public enum EnvelopeDirection
{
    Request = 0,
    Reply = 1
}

public interface IEnvelopeIntegrityBroker
{
    ValueTask<EnvelopeIntegrity> SignAsync<T>(
        EventEnvelope<T> envelope,
        string eventName,
        EnvelopeDirection direction,
        CancellationToken cancellationToken = default);

    // Returns false for a bad signature, a missing one, a name mismatch,
    // or a direction mismatch. The caller supplies the name it expects and
    // the direction it expects; neither is read off the envelope.
    ValueTask<bool> VerifyAsync<T>(
        EventEnvelope<T> envelope,
        string expectedEventName,
        EnvelopeDirection expectedDirection,
        CancellationToken cancellationToken = default);
}
```

The signed input must be canonical — a single, deterministic byte rendering
agreed by signer and verifier. Do not sign "the JSON," because property order,
culture and null handling are all free to vary between serializer versions;
define the canonical form explicitly, and version it.

## EVN11. Current Implementation — EventHighway Substrate *(formerly §10.10)*

Events are published through the `EventBroker`, which wraps
[EventHighway](https://github.com/The-Standard-Organization/EventHighway) — a
durable, SQL-backed pub/sub substrate. Each service owns a set of event addresses
named `<Subject>-<Verb>` (§EVN2), split into two families: **requests** in the
present tense (`ContentItem-Adding`, `-Modifying`, `-RemovingById`,
`-RetrievingById`), answered by responder handlers on the owning service, and
**facts** in the past tense (`ContentItem-Added`, `-Modified`, `-Removed`),
published by the service after its work is done for observers to react to. The
subject is the service rather than the entity, so a higher-level service
announcing completion of its own unit of work sits on its own addresses —
`ContentItemProcessing-Adding` is handled by `ContentItemProcessingService`,
which publishes `ContentItemProcessing-Added` once the processed add has
completed. Receiver handler methods are always named `On<Verb><Entity>Async`
(`OnAddingContentItemAsync`); the `On` prefix marks the receiver and never
appears in the address itself. The address is selected by a strongly typed
per-service operation enum passed on publish (for example
`ContentItemEventOperation.Adding`, `ContentItemProcessingEventOperation.Added`)
— no magic strings, and operations can be added per service without affecting
the others. The broker composes the stored event name from the subject and
operation (for example `"ContentItemAdding"`, `"ContentItemProcessingAdded"`),
so the subject must be distinct per service or the stored names would collide.
Every publish persists the event and dispatches it inline to the in-process
delegate handlers subscribed to that address; handler failures are recorded per
listener (with retry support) instead of failing the publisher. Subscriptions
bind to exactly one operation. Handlers may optionally return a reply envelope
(`ValueTask<EventEnvelope<T>?>`), which the broker serializes onto the
delivery's `ListenerEventV2` row — the observable reply channel for
request-style events such as `RetrievedById`, carrying the same
security-context and metadata discipline as the request.

Publishing returns an `EventPublishResult<T>`: the persisted event id plus one
`EventDelivery<T>` per subscription, each with its dispatch-time status and —
for responders — the reply envelope deserialized back to `EventEnvelope<T>`.
This is a dispatch-time snapshot: failed deliveries may still succeed later via
retries, and the durable truth remains the event store. Notification-style
publishers simply ignore the result.

Foundation services follow a dual-path shape (see `ContentItemService` as the
template):

- **Non-event path**: receive the object → convert to a request envelope via
  `IEventEnvelopeFactory.CreateAsync` (captures the caller's `SecurityContext`,
  stamps event/correlation identifiers) → call the shared private `DoXAsync`
  method.
- **Event path** (the `.Substrate` partial): one `On<Operation><Entity>Async`
  handler per request address (`OnAdding…`, `OnModifying…`, `OnRemoving…ById`,
  `OnRetrieving…ById`) → validate the envelope → dedup mutating handlers via the
  `ProcessedEvents` table (unique on EventId + ReceiverName; a deduplicated
  delivery replies `null`) → converge on the same `DoXAsync` methods → reply
  with the outcome envelope on the delivery.

The `DoXAsync` methods own auditing, validation, storage, and publishing the
past-tense fact, so the two paths cannot diverge; §EVN19 rules where the storage
half ends and the publishing half begins, because nothing binds them and a
failed publish strands the row it was announcing; every hop chains causation
through `IEventEnvelopeFactory.CreateNextAsync` (fresh `EventId`, `CausationId`
= source event, security/request context carried forward). Substrate handlers
categorize failures into the service's typed exceptions and rethrow —
deliveries record `Error` and retry; failures are never swallowed. Hard removal
is deliberately not event-invokable, and reads publish no fact — a retrieve's
reply rides the delivery's response.

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
Create EventEnvelope<T> via IEventEnvelopeFactory
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
Create EventEnvelope<T> via IEventEnvelopeFactory
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
orchestrations, processings, foundations) via `IEventEnvelopeFactory`.

The controller should:

1. Rely on authentication middleware to authenticate the caller.
2. Accept the request model and `CancellationToken`.
3. Call the relevant orchestration service.
4. Map the result and domain exceptions to HTTP responses.

Example:

```csharp
[HttpPost]
public async ValueTask<IActionResult> PostStudentAsync(
    Student student,
    CancellationToken cancellationToken)
{
    Student createdStudent =
        await this.studentOrchestrationService
            .OrchestrateStudentCreationAsync(
                student,
                cancellationToken);

    return Ok(createdStudent);
}
```

## EVN14. Event Handler Pattern *(formerly §10.13)*

Event handlers should accept the envelope and pass it to the relevant
orchestration service.

```csharp
public sealed class StudentCreatedEventHandler
{
    private readonly IStudentOrchestrationService studentOrchestrationService;

    public StudentCreatedEventHandler(
        IStudentOrchestrationService studentOrchestrationService)
    {
        this.studentOrchestrationService = studentOrchestrationService;
    }

    public async ValueTask HandleAsync(
        EventEnvelope<Student> envelope,
        CancellationToken cancellationToken)
    {
        await this.studentOrchestrationService
            .OrchestrateStudentCreationAsync(
                envelope,
                cancellationToken);
    }
}
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

Example validation:

```csharp
private static void ValidateEnvelope<T>(EventEnvelope<T> envelope)
{
    if (envelope is null)
    {
        throw new InvalidEventEnvelopeException("Event envelope is required.");
    }

    if (envelope.Content is null)
    {
        throw new InvalidEventEnvelopeException("Event content is required.");
    }

    if (envelope.SecurityContext is null)
    {
        throw new InvalidEventEnvelopeException("Security context is required.");
    }

    if (envelope.RequestContext is null)
    {
        throw new InvalidEventEnvelopeException("Request context is required.");
    }

    if (envelope.Metadata is null)
    {
        throw new InvalidEventEnvelopeException("Event metadata is required.");
    }
}
```

## EVN16. Anti-Patterns *(formerly §10.15)*

Avoid passing `HttpContext` into orchestration services:

```csharp
// AVOID
public ValueTask<Student> OrchestrateAsync(Student student, HttpContext httpContext)
```

Avoid using `IHttpContextAccessor` inside orchestration services:

```csharp
// AVOID
this.httpContextAccessor.HttpContext.User
```

Avoid serializing raw `ClaimsPrincipal` into events.

Avoid passing raw JWT tokens through the domain or event pipeline unless there is
a specific and justified reason.

Avoid placing authorization decisions only in controllers when orchestration
services are responsible for business workflow decisions.

Avoid scattering magic-string role and scope names throughout orchestration
services. Keep role and claim names in a central constants class and perform
checks through `ISecurityBroker`.

## EVN17. Authorization in Orchestration Services *(formerly §10.16)*

Authorization is performed where the business decision is required — inside the
orchestration service — using `ISecurityBroker` directly. A separate
permission/authorization service is not used.

`ISecurityBroker` provides the required primitives:

```csharp
public interface ISecurityBroker
{
    ValueTask<User> GetCurrentUserAsync();
    ValueTask<bool> IsCurrentUserAuthenticatedAsync();
    ValueTask<bool> IsInRoleAsync(string roleName);
    ValueTask<bool> UserHasClaimAsync(string claimType, string claimValue);
    ValueTask<bool> UserHasClaimAsync(string claimType);
    ValueTask<SecurityContext> GetCurrentSecurityContextAsync();
}
```

Example usage in an orchestration service:

```csharp
public ValueTask<ContentItem> AddContentItemAsync(
    ContentItem contentItem,
    CancellationToken cancellationToken) =>
TryCatch(async () =>
{
    bool isAuthenticated =
        await this.securityBroker.IsCurrentUserAuthenticatedAsync();

    // all three tiers of the veto, and the narrow one is composed from the row's own
    // content type — a block at any of them bars the write (§18.6 rule 2)
    bool isBlocked =
        await this.securityBroker.IsInRoleAsync(Roles.ReadOnly)
            || await this.securityBroker.IsInRoleAsync(Roles.ContentItemReadOnly)
            || await this.securityBroker.IsInRoleAsync(
                Roles.ReadOnlyFor(EntityType.ContentItem, contentItem.ContentType));

    ValidateUserIsAllowedToContribute(isAuthenticated, isBlocked);

    ContentItem createdContentItem =
        await this.contentItemService.AddContentItemAsync(
            contentItem,
            cancellationToken);

    return createdContentItem;
});
```

Rules:

1. Role and claim names must live in a central constants class (e.g. `Roles`) —
   no magic strings scattered through orchestration services.
2. Controllers must not perform business authorization; they rely on
   authentication middleware and standard policy attributes for coarse access
   only.
3. The `SecurityContext` for event envelopes is obtained via
   `ISecurityBroker.GetCurrentSecurityContextAsync()` inside the service that
   creates the envelope (`IEventEnvelopeFactory`).

## EVN18. Approval Workflow Wiring *(formerly §10.17)*

The approval workflow both **consumes** entity lifecycle facts and **causes**
entity writes. Wired naively that cycle does not terminate, so the wiring is
specified here rather than left to the implementation.

**Inbound — subscribe to the entity's top-layer fact, never the foundation
fact.**

An entity's **top-layer service** is the highest business layer that owns its
write flows — its orchestration service if it has one, otherwise its processing
service, otherwise the foundation itself. The tier matters; which of the two
upper layers it happens to be does not.

1. The approval orchestration subscribes to the top-layer `-Added` and
   `-Modified` facts **where a layer above the foundation exists** — for
   `ContentItem` that is `ContentItemProcessing-Added` / `-Modified`, and for
   `Link` that is `LinkProcessing-Added` / `-Modified`. It does not subscribe to
   those entities' `-Removed` at all; the workflow records' removals are the
   documented exception (§EVN18(a)). Per §EVN2 rule 6 it must not also subscribe to
   the foundation facts for the same reaction.

   Where an approvable entity has nothing above its foundation — today that is
   every one except `ContentItem` and `Link` — it subscribes to the
   **foundation** facts instead. That is safe for a Single-Row entity: the loop
   is broken by rule 4 below rather than by the subscription tier, and with no
   version fork there is no multi-row bookkeeping write to misread. A
   **Versioned** entity must have a service above its foundation before it can
   participate in approval, for the reason in rule 2.
2. The reason is §EVN2 rule 5. A version fork used to write two foundation rows and
   therefore emit two foundation facts. Reacting to the second — the demotion of
   the previous latest — would have reset the still-published previous version's
   approval and dismissed its review history, for a write that changed only a
   bookkeeping flag.

   **There is no demotion fact, because there is no demotion.** The tip is
   derived rather than stored, so a fork writes one row and emits one `-Added`.
   The misreading this rule guards against is therefore impossible rather than
   merely unsubscribed — stricter than the interim shape, which gave the
   demotion its own `<Entity>-Demoted` address so it could not be mistaken for a
   content amendment. The rule stands anyway: rule 1's "one fact per completed
   amend" and rule 3's "a direct foundation write bypasses invalidation" are
   independent of it, and a `Versioned` entity still needs a layer above its
   foundation for those. The top-layer service emits exactly one fact per
   completed amend, which is the unit of work the approval workflow actually
   cares about — and it is the fork that makes this a *layer* question rather
   than an *orchestration* question, since the fork is single-entity processing
   work.
3. The consequence to accept deliberately: a write made directly against a
   foundation service bypasses approval invalidation. Approvable entities are
   therefore written through their top-layer service, and an exposer must bind
   to that service rather than the foundation for any approvable entity.

**Inbound — the workflow's own records.** `ApprovalReview` and `ApprovalComment`
are a second inbound channel, and a different one: their facts do not
*invalidate* an approval, they prompt the workflow to **re-test the §8.5
conditions** on an approval that may have been blocked. Both are foundation-tier
subscriptions — neither is an approvable entity and neither has a layer above its
foundation, so rules 1 and 2 do not apply and there is no fork to misread.
Lettered here so the numbered rules above keep their cross-references.

- (a) **Subscribe to every fact address on both records — not a subset.** The
  §8.5 evaluation reads comments through `IsDeleted is false && IsResolved is
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
  — it means the inputs changed. The handler re-runs the whole §8.5 evaluation.
  Facts that move the gate *shut* matter as much as those that open it: a
  comment born outstanding, or a withdrawn approving review, can re-block an
  approval that was clear, which is exactly the case
  `AutoApproveIfAllApprovalRequirementsMet` would otherwise get wrong. Equally,
  a fact may move nothing at all — a comment born settled is the common case —
  which is why the handler re-evaluates instead of inferring a direction from
  the address.
- (b1) **The entity under review is a fourth inbound source, and it is the one
  that causes dismissal.** When an item subject to approval is added or
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
- (b3) **The one fact this service causes itself is suppressed while it causes
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
  genuine property, pinned by a test that publishes from outside any window,
  which is how a second publisher would arrive. A repair pass or an
  administrative tool that dismissed outside the reset loop would need exactly
  this subscription, and would find it already correct.

  Recorded here rather than left implicit so that a later reader finding an
  unreachable handler does not mistake it for an oversight.

- (b2) **`Approval` itself is a fifth.** Its own `-Added` / `-Modified` facts
  re-enter the same evaluation, because a status or setting change can move the
  outcome without any review or comment changing.
- (b3) **The decision is not the orchestration's to compute.** It receives a
  fact, gathers what the evaluation needs, and asks; the answer — block,
  permit, or auto-approve — comes back from the decision function. The
  orchestration owns the *reaction*, never the *rule*.
- (c) **`-Dismissed` is a distinct address precisely so this reaction can tell
  a withdrawn verdict from an amended one**, and `-Resolved` serves the same
  purpose for a comment.
- (d) **The cycle rule still binds.** Re-testing may cause an approval
  decision, and that decision must go out through the transition verb of rules
  4–5, never as a `-Modified` on the workflow record that triggered it.

**Outbound — approval-caused writes use a transition verb, never
`-Modifying`.**

4. Every write the approval workflow causes on an entity's approval state goes
   through `Transition<Entity>ApprovalAsync` on the owning foundation service,
   published as `<Entity>-Approving` / `-Approved`. §EVN2 rule 7 already
   establishes this vocabulary — a transition owning a narrower field scope
   than a general modify is a separate method and therefore a separate verb.
   Its scope is the whole of `IApproval`, so no separate publish verb is
   required.
5. This operation validates only the `IApproval` members — plus the
   first-publish `ShortCode` derivation on `ContentItem` — and **must not**
   publish `<Entity>-Modified`. This is what breaks the cycle: the workflow
   subscribes to `-Modified` and causes only `-Approved`, `-Rejected` or
   `-Submitted`.

   One approval-caused write originates from another entity's approval: when a
   host completes approval and publication, its purposefully-placed and
   inline-referenced attachments are approved through the attachment
   submit-then-approve transitions, bypass-audited. The derived write uses
   transition verbs, so rules 4–5 and the cycle-breaker hold unchanged.

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

8. `ApprovalOrchestrationService` performs the entity write itself. It does not
   publish an approval fact for the owning entity's orchestration to react to.
   This resolves a contradiction in earlier drafts, which would have required
   every approvable entity's orchestration to subscribe to approval facts and
   would have reintroduced the cycle at one remove.

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

1. **One Core transaction covers the row, both `ProcessedEvent` records, and an
   outbox row.** The entity write, the inbound envelope's `ProcessedEvent`, the
   outbound envelope's `ProcessedEvent`, and a durable outbox row carrying the
   fact about to be announced all commit together or not at all. They are all
   in `Glory2Him.Core`, which is what makes one transaction sufficient. Nothing
   in the transaction touches the event store.

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
   it.** `ProcessedEvents` is unique on `EventId` + `ReceiverName` and a
   deduplicated delivery replies `null`, so a redelivered envelope is a no-op.
   That existing dedup is the precondition this ruling depends on; it is not
   new work.

5. **The outbox stores the envelope minted before the commit, verbatim, and a
   retry republishes that same envelope.** The relay must never re-mint. A
   re-minted envelope carries a fresh `EventId` and would defeat rule 4's
   dedup, turning one fact into many. Re-signing on each attempt is correct and
   required: the signature is computed at publish time and binds the composed
   event name, the direction, and the carried sections, so signing the same
   stored envelope later yields the same envelope with a valid signature. This
   is also what lets a host that could not sign at write time dispatch the fact
   once a key is configured, rather than losing it.

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
    the work is *committed*, with the fact following. Reads publish no fact and
    hard removal publishes none, so neither is affected.

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

> **Service calls are for intent. Events are for reaction.**

If something is part of the required business transaction, call a service
directly. If something is a reaction to what happened, publish an event.

Avoiding event spaghetti:

1. Required business flow belongs in orchestration services.
2. Reactions belong in event receivers.
3. Events should describe facts, not commands.
4. Receivers should be idempotent.
5. Do not rely on receiver execution order unless explicitly designed.
6. Do not use events to avoid proper service boundaries.
7. Persist events before external delivery.
8. Keep event contracts stable.
9. Use correlation and causation IDs everywhere.
10. Treat replay as a first-class design concern.

## EVN21. Future Pattern: Intentional Dispatch Events *(new; from EventSubstrate.md §34)*

**Not yet used anywhere in this codebase.** Documented as a considered pattern
for if and when it is needed, not as current design.

The rule "service calls are for intent, events are for reaction" (§EVN20) describes
the common case. There is a legitimate exception where **intent itself is
triggered by an incoming external signal** — in that case, an orchestration
service may publish an event as a deliberate dispatch mechanism, not as a normal
reaction. Use carefully.

**The scenario this answers:** an orchestration service polls an external API
and receives a graph of related objects (e.g. `{ Department, [Course],
[Student], [Enrollment] }`) and needs to create each locally, in the correct
order. It could call each foundation service directly in sequence — but if the
graph is large, the object types are variable, or the creation logic needs to be
owned by each domain service independently, the orchestration can instead
**publish a scoped import event per object** and let the appropriate foundation
service receive it internally.

The event in this pattern is still **intent**, not reaction — the orchestration
is making a deliberate routing decision.

| Characteristic | Reaction event | Intentional dispatch event |
| --- | --- | --- |
| Emitted because | Something already happened | A deliberate routing decision is being made |
| Handler/receiver is | Optional / loosely coupled | Expected and required |
| Order matters | Usually not | Often yes |
| Who owns the receiver | The reacting service | The domain service responsible for that object type |

Naming reflects intent, not a past-tense fact, since the work has not happened
yet: `StudentImportRequestedEvent` (intent), not `ImportStudentEvent`
(ambiguous) or `CreateStudentEvent` (command style).

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
signing rationale (§10), the intent-vs-reaction principle and the
event-spaghetti-avoidance rules (§EVN20), and the intentional-dispatch pattern
(§EVN21) — none of these depended on the discarded scheme.

To recover the original document in full: `git log --follow -- Documentation/EventSubstrate.md`
finds the commits; the file existed at that path up to the commit that unified
it into this one.
