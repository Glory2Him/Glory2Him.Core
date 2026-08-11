# Event Substrate Pattern

## 1. Purpose

This document describes an event substrate pattern for applications that follow **The Standard** style of layered architecture.

The intent is to allow every service to act as both:

1. **An event emitter** - a service can publish events when something meaningful has happened.
2. **An event receiver** - a service can react to events published by other services.

The substrate acts as a shared messaging layer underneath the service layer. It allows local in-process event handling, durable event recording, replay, and optional external fan-out through mechanisms such as REST APIs, queues, webhooks, or service bus topics.

The pattern is useful for modular monoliths, distributed systems, integrations, audit trails, and systems that may need to rehydrate downstream projections or external consumers from a known point in time.

---

## 2. Core Idea

Traditional service interaction often looks like this:

```text
Controller
    ↓
Orchestration Service
    ↓
Foundation Service
    ↓
Broker
```

With an event substrate, services still keep their direct service methods, but they also emit and receive events:

```text
Controller
    ↓
Orchestration Service
    ↓
Foundation Services
    ↓
Event Substrate
    ├── Local Subscribers
    ├── Event Store
    ├── External REST Fan-out
    ├── Queue / Service Bus Fan-out
    └── Replay / Rehydration
```

The important distinction is:

> **Service calls are for intent. Events are for reaction.**

If something is part of the required business transaction, call a service directly.

If something is a reaction to what happened, publish an event.

---

## 3. Why Every Service Is an Emitter and Receiver

Every service should be capable of emitting and receiving events because most business modules eventually need both behaviours.

For example, in a student application:

| Service | Emits | Receives |
|---|---|---|
| StudentService | StudentCreatedEvent | StudentEnrolledEvent |
| EnrollmentService | StudentEnrolledEvent | StudentCreatedEvent |
| TimetableService | TimetableGeneratedEvent | StudentEnrolledEvent |
| NotificationService | NotificationSentEvent | StudentCreatedEvent, TimetableGeneratedEvent |
| AuditService | AuditRecordedEvent | Any event |
| SearchIndexService | StudentIndexedEvent | StudentCreatedEvent, StudentUpdatedEvent |

### Event Chain Timeline

The following shows the full event propagation chain, starting from the moment a student is created:

```
[1] StudentService
    │  Action: Student record created (e.g. via API)
    │  Emits: StudentCreatedEvent
    │
    ├──▶ [2a] EnrollmentService
    │         Receives: StudentCreatedEvent
    │         Action: Creates enrollment record
    │         Emits: StudentEnrolledEvent
    │         │
    │         ├──▶ [3a] TimetableService
    │         │         Receives: StudentEnrolledEvent
    │         │         Action: Generates timetable for student
    │         │         Emits: TimetableGeneratedEvent
    │         │         │
    │         │         └──▶ [4a] NotificationService
    │         │                   Receives: TimetableGeneratedEvent
    │         │                   Action: Sends timetable notification to student
    │         │                   Emits: NotificationSentEvent
    │         │
    │         └──▶ [3b] AuditService
    │                   Receives: StudentEnrolledEvent (via Any event)
    │                   Action: Records enrollment audit entry
    │                   Emits: AuditRecordedEvent
    │
    ├──▶ [2b] NotificationService
    │         Receives: StudentCreatedEvent
    │         Action: Sends welcome notification to student
    │         Emits: NotificationSentEvent
    │
    ├──▶ [2c] AuditService
    │         Receives: StudentCreatedEvent (via Any event)
    │         Action: Records creation audit entry
    │         Emits: AuditRecordedEvent
    │
    └──▶ [2d] SearchIndexService
              Receives: StudentCreatedEvent
              Action: Indexes new student record for search
              Emits: StudentIndexedEvent
```

> **Note:** `AuditService` subscribes to **any** event, so it records an audit entry at every stage of the chain. `NotificationService` fires at two independent points — on student creation and on timetable generation.

This creates a consistent service model:

```text
All services can be called directly.
All services can publish events.
All services can subscribe to events.
```

This is especially useful in a modular monolith because each module can remain loosely coupled without needing to know about every other module.

---


---

## 3.1 Standard-Compliant Foundation Service Progression

The event substrate must not change the public shape of a Standard Foundation Service.

A Foundation Service remains a public capability contract that exposes CRUD and business operations. Event awareness is added to the implementation, not to the public interface.

### 3.1.1 Standard Foundation Service Interface

A normal Standard-compliant Foundation Service interface should continue to look like this:

```csharp
public interface IStudentService
{
    ValueTask<Student> AddStudentAsync(
        Student student,
        CancellationToken cancellationToken = default);

    IQueryable<Student> RetrieveAllStudents();

    ValueTask<Student> RetrieveStudentByIdAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    ValueTask<Student> ModifyStudentAsync(
        Student student,
        CancellationToken cancellationToken = default);

    ValueTask<Student> RemoveStudentByIdAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);
}
```

Consumers should only depend on this public interface.

They should not know whether the service emits events, receives events, participates in replay, or is connected to the substrate.

### 3.1.2 Event-Aware Foundation Service Implementation

The implementation may additionally implement internal substrate receiver contracts:

```csharp
public sealed partial class StudentService :
    IStudentService,
    IEventReceiver<StudentEnrolledEvent>,
    IEventReceiver<StudentGraduatedEvent>
{
}
```

The important distinction is:

```text
Public API
-----------
IStudentService
    AddStudentAsync(...)
    RetrieveAllStudents()
    RetrieveStudentByIdAsync(...)
    ModifyStudentAsync(...)
    RemoveStudentByIdAsync(...)

Internal Substrate API
----------------------
IEventReceiver<StudentEnrolledEvent>
IEventReceiver<StudentGraduatedEvent>
```

The substrate extends the service implementation, but it does not change the service contract.

### 3.1.3 Foundation Service as a Capability Owner

With the substrate, a Foundation Service should be understood as the owner of a business capability, not merely as a CRUD wrapper around storage.

For example, `StudentService` owns student behaviour regardless of the trigger:

- a controller calls `AddStudentAsync`
- an orchestration service calls `ModifyStudentAsync`
- the substrate delivers `StudentEnrolledEvent`
- replay delivers an old event to rebuild downstream state

The source of the trigger changes, but the business capability remains owned by the same service.

### 3.1.4 Public Intent, Internal Reaction

The service supports both models:

```text
Intent-driven behaviour
-----------------------
Controller / Orchestration Service
    ↓
IStudentService.AddStudentAsync(...)

Event-driven behaviour
----------------------
Event Substrate
    ↓
IEventReceiver<StudentEnrolledEvent>.ReceiveAsync(...)
```

The public interface remains the entry point for intent.

The internal receiver interface is the entry point for reaction.

### 3.1.5 Partial File Convention

Following the Standard partial-file style, all substrate receiver implementations should live in a dedicated partial file:

```text
StudentService.cs
StudentService.Validations.cs
StudentService.Exceptions.cs
StudentService.Substrate.cs
```

`StudentService.cs` contains the normal public Foundation Service behaviour.

`StudentService.Substrate.cs` contains the internal event receiver behaviour.


## 4. Architecture Overview

```text
┌─────────────────────────────┐
│          Controller          │
└──────────────┬──────────────┘
               │
               ↓
┌─────────────────────────────┐
│   Student Orchestration      │
└──────────────┬──────────────┘
               │
               ↓
┌─────────────────────────────┐
│      Foundation Services     │
│                             │
│  StudentService              │
│  EnrollmentService           │
│  TimetableService            │
│  NotificationService         │
└──────────────┬──────────────┘
               │
               ↓
┌─────────────────────────────┐
│       Event Substrate        │
│                             │
│  Local Dispatch              │
│  Event Store                 │
│  External Dispatch           │
│  Replay Engine               │
│  Dead Letter Store           │
└─────────────────────────────┘
```

The event substrate is not just an in-memory event bus. It is a durable event layer that can:

1. Publish local events.
2. Dispatch events to in-process handlers.
3. Record events in a database.
4. Fan events out to external systems.
5. Retry failed external deliveries.
6. Store failed events in a dead-letter table.
7. Replay events from a point in time.
8. Rehydrate projections, search indexes, caches, or external systems.

---

## 5. The Event Envelope

Events should not be published as raw payloads. They should be wrapped in an envelope.

The envelope carries operational, security, traceability, and integrity information.

```csharp
public sealed class EventEnvelope<T>
{
    public Guid EventId { get; init; }

    public string EventName { get; init; } = typeof(T).Name;

    public int EventVersion { get; init; } = 1;

    public Guid CorrelationId { get; init; }

    public Guid CausationId { get; init; }

    public DateTimeOffset OccurredDate { get; init; }

    public SecurityContext SecurityContext { get; init; } = default!;

    public RequestContext RequestContext { get; init; } = default!;

    public EnvelopeIntegrity Integrity { get; init; } = default!;

    public Dictionary<string, object> Metadata { get; init; } = new();

    public T Content { get; init; } = default!;
}
```

### 5.1 EventId

Uniquely identifies the event instance.

This is used for:

- idempotency
- replay tracking
- external delivery tracking
- duplicate detection

### 5.2 EventName

The logical event name.

Example:

```text
StudentCreatedEvent
StudentEnrolledEvent
TimetableGeneratedEvent
```

### 5.3 EventVersion

The schema version of the event.

This allows event contracts to evolve without breaking old consumers.

### 5.4 CorrelationId

Identifies the overall business operation.

Example:

```text
Register student operation
    ├── StudentCreatedEvent
    ├── StudentEnrolledEvent
    ├── TimetableGeneratedEvent
    └── WelcomeEmailSentEvent
```

All these events should share the same `CorrelationId`.

### 5.5 CausationId

Identifies the event that caused this event.

Example:

```text
StudentCreatedEvent
    ↓ causes
StudentEnrolledEvent
    ↓ causes
TimetableGeneratedEvent
```

The `CausationId` of `StudentEnrolledEvent` would be the `EventId` of `StudentCreatedEvent`.

### 5.6 OccurredDate

The UTC time when the business event occurred.

This should not be overwritten during retries or replay.

### 5.7 SecurityContext

Carries the authenticated user or system identity responsible for the action.

Authentication should happen at the entry point, such as the API controller or message receiver.

Authorization should happen where it is needed, using the authorization service.

```csharp
public sealed class SecurityContext
{
    public Guid? UserId { get; init; }

    public string? UserName { get; init; }

    public IReadOnlyCollection<string> Roles { get; init; } = [];

    public bool IsAuthenticated { get; init; }
}
```

### 5.8 RequestContext

Carries request-level metadata.

```csharp
public sealed class RequestContext
{
    public string? SourceSystem { get; init; }

    public string? TenantId { get; init; }

    public string? RequestPath { get; init; }

    public string? UserAgent { get; init; }

    public string? IpAddress { get; init; }
}
```

### 5.9 Metadata

Metadata is for enrichment that can safely change over time.

Examples:

```text
RetryCount
ExternalDeliveryAttempt
ExternalEndpoint
ProcessingNode
```

Metadata should not be covered by the envelope integrity signature if you want the substrate to enrich it during retries and dispatch.

### 5.10 EnvelopeIntegrity

The integrity section protects the parts of the envelope that should not be tampered with.

**Not implemented.** No signer and no verifier exists in the repository; `Integrity` is `null` on every envelope built today. What follows is the specification, not a description of current behaviour.

The signature must cover:

1. `SecurityContext`
2. `RequestContext`
3. A hash of `Content`
4. `Metadata.EventId`
5. The target address / operation

**Signing only `SecurityContext` and `RequestContext` does not work**, and an earlier revision of this document recommended exactly that. A signature over identity alone is a transplantable bearer token: capture any one legitimately signed envelope, lift its signed `SecurityContext` onto different `Content` at a different address, and it still verifies. The attacker then authors any request they like as that actor — which is the whole property the signature was supposed to deny them. Binding the content hash, the event id and the destination is what ties a signature to *one* request; without all three the control is decorative.

`Metadata` as a whole stays outside the signature so the substrate can enrich it during retries and dispatch — `Metadata.RetryCount` in particular changes legitimately in flight. `Metadata.EventId` is pulled in individually because it is the replay key and is fixed at creation.

```csharp
public sealed class EnvelopeIntegrity
{
    public string Algorithm { get; init; } = "HMACSHA256";

    public string Signature { get; init; } = string.Empty;

    public DateTimeOffset SignedDate { get; init; }
}
```

Integrity should be handled by a broker:

```csharp
public interface IEnvelopeIntegrityBroker
{
    ValueTask<EnvelopeIntegrity> SignAsync(
        SecurityContext securityContext,
        RequestContext requestContext,
        CancellationToken cancellationToken = default);

    ValueTask<bool> VerifyAsync(
        SecurityContext securityContext,
        RequestContext requestContext,
        EnvelopeIntegrity integrity,
        CancellationToken cancellationToken = default);
}
```

---

## 6. Event Contracts

Events should represent things that already happened.

Use past-tense names.

Good:

```text
StudentCreatedEvent
StudentEnrolledEvent
TimetableGeneratedEvent
WelcomeEmailSentEvent
```

Avoid command-style names:

```text
CreateStudentEvent
EnrollStudentEvent
GenerateTimetableEvent
SendWelcomeEmailEvent
```

A command asks for something to happen.

An event says something already happened.

---

## 7. Internal Event Receiver Contract

Event reception is an internal substrate concern.

A Foundation Service may receive events, but the receiver methods must not be part of the public business API.

The public service interface should remain focused on Standard CRUD and business operations. For example, `IStudentService` should not inherit from any event receiver interface.

### 7.1 Receiver Contract

Use an internal receiver contract:

```csharp
internal interface IEventReceiver<TEvent>
{
    ValueTask ReceiveAsync(
        EventEnvelope<TEvent> envelope,
        CancellationToken cancellationToken = default);
}
```

The receiver contract should be internal to the application or substrate assembly boundary.

Only the Event Substrate may resolve and invoke receivers.

### 7.2 Why Receivers Are Internal

If event receivers are public, application code could bypass the substrate:

```csharp
await studentService.ReceiveAsync(envelope);
```

That must not be allowed because it bypasses:

- event persistence
- integrity verification
- correlation and causation tracking
- retry behaviour
- replay controls
- local dispatch rules
- external fan-out records
- idempotency safeguards
- audit and diagnostics

Therefore, receiver methods are not public business operations.

### 7.3 Explicit Interface Implementation

Receivers should be implemented explicitly:

```csharp
public sealed partial class StudentService :
    IStudentService,
    IEventReceiver<StudentEnrolledEvent>
{
    async ValueTask
        IEventReceiver<StudentEnrolledEvent>.ReceiveAsync(
            EventEnvelope<StudentEnrolledEvent> envelope,
            CancellationToken cancellationToken)
    {
        // Reaction logic goes here.
    }
}
```

This prevents normal consumers from calling `ReceiveAsync` directly on `StudentService`.

Only code that resolves the service as `IEventReceiver<StudentEnrolledEvent>` can invoke the receiver.

In this pattern, that code must be the Event Substrate only.

### 7.4 Multiple Receivers on One Service

A service may receive multiple event types:

```csharp
public sealed partial class StudentService :
    IStudentService,
    IEventReceiver<StudentEnrolledEvent>,
    IEventReceiver<StudentGraduatedEvent>,
    IEventReceiver<StudentSuspendedEvent>
{
    async ValueTask
        IEventReceiver<StudentEnrolledEvent>.ReceiveAsync(
            EventEnvelope<StudentEnrolledEvent> envelope,
            CancellationToken cancellationToken)
    {
        // Mark student as enrolled.
    }

    async ValueTask
        IEventReceiver<StudentGraduatedEvent>.ReceiveAsync(
            EventEnvelope<StudentGraduatedEvent> envelope,
            CancellationToken cancellationToken)
    {
        // Mark student as graduated.
    }

    async ValueTask
        IEventReceiver<StudentSuspendedEvent>.ReceiveAsync(
            EventEnvelope<StudentSuspendedEvent> envelope,
            CancellationToken cancellationToken)
    {
        // Mark student as suspended.
    }
}
```

### 7.5 Receiver Location

All receiver implementations should live in the `*.Substrate.cs` partial file.

Example:

```text
StudentService.Substrate.cs
EnrollmentService.Substrate.cs
TimetableService.Substrate.cs
NotificationService.Substrate.cs
```

This keeps public service behaviour and substrate behaviour clearly separated.

### 7.6 Receiver Rule

The rule is strict:

> Event receivers are internal implementation details. Only the Event Substrate may invoke them.

## 8. Event Substrate Contract

The substrate publishes events.

```csharp
public interface IEventSubstrate
{
    ValueTask PublishAsync<TEvent>(
        EventEnvelope<TEvent> envelope,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<EventEnvelope<TEvent>> ReplayAsync<TEvent>(
        DateTimeOffset fromDate,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default);
}
```

The substrate is responsible for:

1. Validating the envelope.
2. Verifying integrity.
3. Persisting the event.
4. Dispatching local handlers.
5. Scheduling external fan-out.
6. Recording delivery outcomes.
7. Supporting replay.

---

## 9. Durable Event Store

Even when an event is handled immediately, it should still be recorded in a database.

This gives the system:

- auditability
- replay capability
- external fan-out reliability
- diagnostics
- recovery from downstream failures
- rehydration of read models or external systems

### 9.1 Event Table

```csharp
public sealed class StoredEvent
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public int EventVersion { get; set; }

    public Guid CorrelationId { get; set; }

    public Guid CausationId { get; set; }

    public DateTimeOffset OccurredDate { get; set; }

    public DateTimeOffset StoredDate { get; set; }

    public string SecurityContextJson { get; set; } = string.Empty;

    public string RequestContextJson { get; set; } = string.Empty;

    public string IntegrityJson { get; set; } = string.Empty;

    public string MetadataJson { get; set; } = string.Empty;

    public string ContentJson { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;
}
```

### 9.2 External Delivery Table

Each event may need to be sent to one or more external targets.

```csharp
public sealed class EventDelivery
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public string DestinationName { get; set; } = string.Empty;

    public string DestinationType { get; set; } = string.Empty;

    public string DestinationAddress { get; set; } = string.Empty;

    public int AttemptCount { get; set; }

    public string Status { get; set; } = "Pending";

    public DateTimeOffset? LastAttemptDate { get; set; }

    public DateTimeOffset? NextAttemptDate { get; set; }

    public string? LastError { get; set; }
}
```

Possible statuses:

```text
Pending
Delivered
Failed
DeadLettered
Skipped
```

### 9.3 Dead Letter Table

Events that cannot be delivered after the retry policy should be placed in a dead-letter store.

```csharp
public sealed class DeadLetteredEvent
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public string DestinationName { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string EventJson { get; set; } = string.Empty;

    public DateTimeOffset DeadLetteredDate { get; set; }
}
```

---

## 10. Event Storage Broker

The broker owns database interaction.

```csharp
public interface IEventStorageBroker
{
    ValueTask<StoredEvent> InsertEventAsync(
        StoredEvent storedEvent,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<StoredEvent>> SelectEventsAsync(
        DateTimeOffset fromDate,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default);

    ValueTask<EventDelivery> InsertDeliveryAsync(
        EventDelivery delivery,
        CancellationToken cancellationToken = default);

    ValueTask<EventDelivery> UpdateDeliveryAsync(
        EventDelivery delivery,
        CancellationToken cancellationToken = default);
}
```

The storage broker should not contain business logic. It only stores and retrieves event records.

---

## 11. Local Event Dispatch

Local dispatch means calling in-process substrate receivers.

Example:

```text
StudentCreatedEvent
    ↓
EnrollmentService.ReceiveAsync(...)
NotificationService.ReceiveAsync(...)
AuditService.ReceiveAsync(...)
SearchIndexService.ReceiveAsync(...)
```

The dispatcher resolves registered `IEventReceiver<TEvent>` implementations through dependency injection or through an internal receiver registry owned by the substrate.

```csharp
public interface ILocalEventDispatcher
{
    ValueTask DispatchAsync<TEvent>(
        EventEnvelope<TEvent> envelope,
        CancellationToken cancellationToken = default);
}
```

Sample implementation:

```csharp
public sealed class LocalEventDispatcher : ILocalEventDispatcher
{
    private readonly IServiceProvider serviceProvider;

    public LocalEventDispatcher(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public async ValueTask DispatchAsync<TEvent>(
        EventEnvelope<TEvent> envelope,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<IEventReceiver<TEvent>> receivers =
            this.serviceProvider.GetServices<IEventReceiver<TEvent>>();

        foreach (IEventReceiver<TEvent> receiver in receivers)
        {
            await receiver.ReceiveAsync(envelope, cancellationToken);
        }
    }
}
```

For high-throughput use cases, local receivers may be executed in parallel. For deterministic workflows, sequential dispatch may be safer.

The local dispatcher must remain internal to the substrate.

Application code should never resolve or call `IEventReceiver<TEvent>` directly.

## 12. External Event Fan-Out

The substrate must be able to send events outside the process.

External fan-out can target:

1. REST APIs
2. Webhooks
3. Azure Service Bus
4. Storage Queues
5. Event Grid
6. Kafka
7. RabbitMQ
8. Other internal systems

The substrate should not hard-code destinations in the service that emits the event.

Instead, use event routing configuration.

```csharp
public sealed class EventRoute
{
    public string EventName { get; init; } = string.Empty;

    public string DestinationName { get; init; } = string.Empty;

    public string DestinationType { get; init; } = string.Empty;

    public string DestinationAddress { get; init; } = string.Empty;

    public bool IsEnabled { get; init; } = true;
}
```

Example route configuration:

```json
[
  {
    "eventName": "StudentCreatedEvent",
    "destinationName": "ExternalStudentPortal",
    "destinationType": "REST",
    "destinationAddress": "https://external-system.example.com/api/events/student-created",
    "isEnabled": true
  },
  {
    "eventName": "TimetableGeneratedEvent",
    "destinationName": "ParentNotificationSystem",
    "destinationType": "REST",
    "destinationAddress": "https://parents.example.com/api/timetable-events",
    "isEnabled": true
  }
]
```

### 12.1 External Dispatcher Contract

```csharp
public interface IExternalEventDispatcher
{
    ValueTask DispatchAsync(
        StoredEvent storedEvent,
        EventRoute route,
        CancellationToken cancellationToken = default);
}
```

### 12.2 REST External Dispatcher

```csharp
public sealed class RestExternalEventDispatcher : IExternalEventDispatcher
{
    private readonly HttpClient httpClient;

    public RestExternalEventDispatcher(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async ValueTask DispatchAsync(
        StoredEvent storedEvent,
        EventRoute route,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response =
            await this.httpClient.PostAsJsonAsync(
                route.DestinationAddress,
                storedEvent,
                cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}
```

In a production system, avoid sending the database shape externally. Instead, send a stable integration contract.

```csharp
public sealed class ExternalEventMessage
{
    public Guid EventId { get; init; }

    public string EventName { get; init; } = string.Empty;

    public int EventVersion { get; init; }

    public Guid CorrelationId { get; init; }

    public Guid CausationId { get; init; }

    public DateTimeOffset OccurredDate { get; init; }

    public string PayloadJson { get; init; } = string.Empty;
}
```

---

## 13. Outbox-Like Processing

The substrate should persist the event before external delivery.

Recommended flow:

```text
1. Service publishes event.
2. Substrate validates and verifies event.
3. Substrate stores event in database.
4. Substrate dispatches local handlers.
5. Substrate creates external delivery records.
6. Background worker sends pending external deliveries.
7. Failed deliveries are retried.
8. Exhausted deliveries are dead-lettered.
```

This means an external system can be offline without losing events.

The application can continue to operate locally while the substrate retries external dispatch later.

---

## 14. Retry Model

External dispatch should support retries.

Example policy:

| Attempt | Delay |
|---|---:|
| 1 | immediate |
| 2 | 1 minute |
| 3 | 5 minutes |
| 4 | 15 minutes |
| 5 | 1 hour |
| 6 | dead-letter |

Sample retry calculation:

```csharp
public static TimeSpan GetRetryDelay(int attemptCount) =>
    attemptCount switch
    {
        0 => TimeSpan.Zero,
        1 => TimeSpan.FromMinutes(1),
        2 => TimeSpan.FromMinutes(5),
        3 => TimeSpan.FromMinutes(15),
        4 => TimeSpan.FromHours(1),
        _ => TimeSpan.FromHours(6)
    };
```

---

## 15. Replay and Rehydration

Because events are stored, they can be replayed.

Replay is useful for:

- rebuilding a search index
- recreating read models
- resending events to a new external system
- recovering from downstream corruption
- backfilling analytics
- debugging historical behaviour

Example replay method:

```csharp
public sealed class EventReplayService
{
    private readonly IEventStorageBroker storageBroker;
    private readonly IExternalEventDispatcher externalDispatcher;

    public EventReplayService(
        IEventStorageBroker storageBroker,
        IExternalEventDispatcher externalDispatcher)
    {
        this.storageBroker = storageBroker;
        this.externalDispatcher = externalDispatcher;
    }

    public async ValueTask ReplayToExternalDestinationAsync(
        DateTimeOffset fromDate,
        DateTimeOffset? toDate,
        EventRoute route,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<StoredEvent> events =
            await this.storageBroker.SelectEventsAsync(
                fromDate,
                toDate,
                cancellationToken);

        foreach (StoredEvent storedEvent in events)
        {
            if (storedEvent.EventName == route.EventName)
            {
                await this.externalDispatcher.DispatchAsync(
                    storedEvent,
                    route,
                    cancellationToken);
            }
        }
    }
}
```

Replay should normally be explicit and controlled. It should not accidentally re-trigger side effects such as duplicate emails unless the handler is designed to be idempotent.

---

## 16. Idempotency

Event receivers must be idempotent.

This means handling the same event more than once should not create incorrect duplicate state.

Use `EventId` to detect duplicates.

Example:

```csharp
public sealed class ProcessedEvent
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public string HandlerName { get; set; } = string.Empty;

    public DateTimeOffset ProcessedDate { get; set; }
}
```

Example handler guard:

```csharp
async ValueTask(
    EventEnvelope<StudentCreatedEvent> envelope,
    CancellationToken cancellationToken = default)
{
    bool alreadyProcessed =
        await this.eventProcessingBroker.HasProcessedAsync(
            envelope.EventId,
            nameof(SearchIndexService),
            cancellationToken);

    if (alreadyProcessed)
    {
        return;
    }

    await this.searchBroker.IndexStudentAsync(
        envelope.Content.StudentId,
        cancellationToken);

    await this.eventProcessingBroker.MarkProcessedAsync(
        envelope.EventId,
        nameof(SearchIndexService),
        cancellationToken);
}
```

---

## 17. Student Application Example

### 17.1 Student Entity

```csharp
public sealed class Student
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public string Email { get; set; } = string.Empty;
}
```

### 17.2 Events

```csharp
public sealed class StudentCreatedEvent
{
    public Guid StudentId { get; init; }

    public string FullName { get; init; } = string.Empty;
}
```


```csharp
public sealed class StudentUpdatedEvent
{
    public Guid StudentId { get; init; }
}
```

```csharp
public sealed class StudentDeletedEvent
{
    public Guid StudentId { get; init; }
}
```

```csharp
public sealed class StudentEnrolledEvent
{
    public Guid StudentId { get; init; }

    public string CourseCode { get; init; } = string.Empty;
}
```

```csharp
public sealed class TimetableGeneratedEvent
{
    public Guid StudentId { get; init; }
}
```

```csharp
public sealed class WelcomeEmailSentEvent
{
    public Guid StudentId { get; init; }

    public string EmailAddress { get; init; } = string.Empty;
}
```

---

## 18. Student Orchestration Service

The orchestration service controls required business flow.

It should call foundation services directly where those actions are required.

```csharp
public interface IStudentOrchestrationService
{
    ValueTask<Student> RegisterStudentAsync(
        Student student,
        CancellationToken cancellationToken = default);
}
```

```csharp
public sealed class StudentOrchestrationService : IStudentOrchestrationService
{
    private readonly IStudentService studentService;
    private readonly IEnrollmentService enrollmentService;
    private readonly ITimetableService timetableService;

    public StudentOrchestrationService(
        IStudentService studentService,
        IEnrollmentService enrollmentService,
        ITimetableService timetableService)
    {
        this.studentService = studentService;
        this.enrollmentService = enrollmentService;
        this.timetableService = timetableService;
    }

    public async ValueTask<Student> RegisterStudentAsync(
        Student student,
        CancellationToken cancellationToken = default)
    {
        Student savedStudent =
            await this.studentService.AddStudentAsync(
                student,
                cancellationToken);

        await this.enrollmentService.EnrollStudentAsync(
            savedStudent.Id,
            "MATH-101",
            cancellationToken);

        await this.timetableService.GenerateTimetableAsync(
            savedStudent.Id,
            cancellationToken);

        return savedStudent;
    }
}
```

In this example, enrollment and timetable generation are required parts of registration, so they are called directly.

The services may still emit events after completing their work.

---

## 19. Student Foundation Service

The Student Foundation Service remains a normal Standard-compliant service from the perspective of its consumers.

The public interface exposes CRUD and business operations only.

It does not expose event receiver methods.

### 19.1 Public Service Interface

```csharp
public interface IStudentService
{
    ValueTask<Student> AddStudentAsync(
        Student student,
        CancellationToken cancellationToken = default);

    IQueryable<Student> RetrieveAllStudents();

    ValueTask<Student> RetrieveStudentByIdAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    ValueTask<Student> ModifyStudentAsync(
        Student student,
        CancellationToken cancellationToken = default);

    ValueTask<Student> RemoveStudentByIdAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);
}
```

### 19.2 StudentService.cs

`StudentService.cs` contains the normal Foundation Service behaviour.

This is where public CRUD and business operations live.

The service may emit events after successful operations.

```csharp
public sealed partial class StudentService : IStudentService
{
    private readonly IStorageBroker storageBroker;
    private readonly IEventSubstrate eventSubstrate;
    private readonly IEventEnvelopeFactory envelopeFactory;

    public StudentService(
        IStorageBroker storageBroker,
        IEventSubstrate eventSubstrate,
        IEventEnvelopeFactory envelopeFactory)
    {
        this.storageBroker = storageBroker;
        this.eventSubstrate = eventSubstrate;
        this.envelopeFactory = envelopeFactory;
    }

    public async ValueTask<Student> AddStudentAsync(
        Student student,
        CancellationToken cancellationToken = default)
    {
        Student savedStudent =
            await this.storageBroker.InsertAsync(
                student,
                cancellationToken);

        EventEnvelope<StudentCreatedEvent> envelope =
            await this.envelopeFactory.CreateAsync(
                new StudentCreatedEvent
                {
                    StudentId = savedStudent.Id,
                    FullName = $"{savedStudent.FirstName} {savedStudent.LastName}"
                },
                cancellationToken);

        await this.eventSubstrate.PublishAsync(
            envelope,
            cancellationToken);

        return savedStudent;
    }

    public IQueryable<Student> RetrieveAllStudents() =>
        this.storageBroker.SelectAllStudents();

    public async ValueTask<Student> RetrieveStudentByIdAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        return await this.storageBroker.SelectStudentByIdAsync(
            studentId,
            cancellationToken);
    }

    public async ValueTask<Student> ModifyStudentAsync(
        Student student,
        CancellationToken cancellationToken = default)
    {
        Student modifiedStudent =
            await this.storageBroker.UpdateAsync(
                student,
                cancellationToken);

        EventEnvelope<StudentUpdatedEvent> envelope =
            await this.envelopeFactory.CreateAsync(
                new StudentUpdatedEvent
                {
                    StudentId = modifiedStudent.Id
                },
                cancellationToken);

        await this.eventSubstrate.PublishAsync(
            envelope,
            cancellationToken);

        return modifiedStudent;
    }

    public async ValueTask<Student> RemoveStudentByIdAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        Student student =
            await this.RetrieveStudentByIdAsync(
                studentId,
                cancellationToken);

        Student deletedStudent =
            await this.storageBroker.DeleteAsync(
                student,
                cancellationToken);

        EventEnvelope<StudentDeletedEvent> envelope =
            await this.envelopeFactory.CreateAsync(
                new StudentDeletedEvent
                {
                    StudentId = deletedStudent.Id
                },
                cancellationToken);

        await this.eventSubstrate.PublishAsync(
            envelope,
            cancellationToken);

        return deletedStudent;
    }
}
```

### 19.3 StudentService.Substrate.cs

`StudentService.Substrate.cs` contains the internal event receiver behaviour.

The receiver is explicitly implemented so normal consumers cannot call it directly.

```csharp
public sealed partial class StudentService :
    IEventReceiver<StudentEnrolledEvent>
{
    async ValueTask
        IEventReceiver<StudentEnrolledEvent>.ReceiveAsync(
            EventEnvelope<StudentEnrolledEvent> envelope,
            CancellationToken cancellationToken)
    {
        Student student =
            await this.RetrieveStudentByIdAsync(
                envelope.Content.StudentId,
                cancellationToken);

        student.Status = "Enrolled";

        await this.ModifyStudentAsync(
            student,
            cancellationToken);
    }
}
```

The Student Service is therefore both:

```text
Emitter
-------
AddStudentAsync(...) emits StudentCreatedEvent
ModifyStudentAsync(...) emits StudentUpdatedEvent
RemoveStudentByIdAsync(...) emits StudentDeletedEvent

Receiver
--------
StudentService.Substrate.cs receives StudentEnrolledEvent
```

The public Foundation Service contract remains clean and Standard-compliant.

## 20. Enrollment Foundation Service

The Enrollment Foundation Service owns enrollment behaviour.

It exposes public business operations and internally receives substrate events.

### 20.1 Public Service Interface

```csharp
public interface IEnrollmentService
{
    ValueTask EnrollStudentAsync(
        Guid studentId,
        string courseCode,
        CancellationToken cancellationToken = default);
}
```

### 20.2 EnrollmentService.cs

```csharp
public sealed partial class EnrollmentService : IEnrollmentService
{
    private readonly IEventSubstrate eventSubstrate;
    private readonly IEventEnvelopeFactory envelopeFactory;

    public EnrollmentService(
        IEventSubstrate eventSubstrate,
        IEventEnvelopeFactory envelopeFactory)
    {
        this.eventSubstrate = eventSubstrate;
        this.envelopeFactory = envelopeFactory;
    }

    public async ValueTask EnrollStudentAsync(
        Guid studentId,
        string courseCode,
        CancellationToken cancellationToken = default)
    {
        // Save enrollment record here.

        EventEnvelope<StudentEnrolledEvent> envelope =
            await this.envelopeFactory.CreateAsync(
                new StudentEnrolledEvent
                {
                    StudentId = studentId,
                    CourseCode = courseCode
                },
                cancellationToken);

        await this.eventSubstrate.PublishAsync(
            envelope,
            cancellationToken);
    }
}
```

### 20.3 EnrollmentService.Substrate.cs

```csharp
public sealed partial class EnrollmentService :
    IEventReceiver<StudentCreatedEvent>
{
    async ValueTask
        IEventReceiver<StudentCreatedEvent>.ReceiveAsync(
            EventEnvelope<StudentCreatedEvent> envelope,
            CancellationToken cancellationToken)
    {
        // Optional automatic reaction.
        // Only do this if enrollment is reactive, not part of the required orchestration flow.

        await this.EnrollStudentAsync(
            envelope.Content.StudentId,
            "MATH-101",
            cancellationToken);
    }
}
```

Important: if enrollment is required in the registration transaction, the orchestration service should call it directly. If enrollment is optional, default, reactive, or eventually consistent, it can happen from the substrate receiver.

## 21. Timetable Foundation Service

The Timetable Foundation Service owns timetable generation.

It can be called directly by orchestration when timetable generation is required, and it can also react to enrollment events when timetable generation is reactive.

### 21.1 Public Service Interface

```csharp
public interface ITimetableService
{
    ValueTask GenerateTimetableAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);
}
```

### 21.2 TimetableService.cs

```csharp
public sealed partial class TimetableService : ITimetableService
{
    private readonly IEventSubstrate eventSubstrate;
    private readonly IEventEnvelopeFactory envelopeFactory;

    public TimetableService(
        IEventSubstrate eventSubstrate,
        IEventEnvelopeFactory envelopeFactory)
    {
        this.eventSubstrate = eventSubstrate;
        this.envelopeFactory = envelopeFactory;
    }

    public async ValueTask GenerateTimetableAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        // Generate timetable here.

        EventEnvelope<TimetableGeneratedEvent> envelope =
            await this.envelopeFactory.CreateAsync(
                new TimetableGeneratedEvent
                {
                    StudentId = studentId
                },
                cancellationToken);

        await this.eventSubstrate.PublishAsync(
            envelope,
            cancellationToken);
    }
}
```

### 21.3 TimetableService.Substrate.cs

```csharp
public sealed partial class TimetableService :
    IEventReceiver<StudentEnrolledEvent>
{
    async ValueTask
        IEventReceiver<StudentEnrolledEvent>.ReceiveAsync(
            EventEnvelope<StudentEnrolledEvent> envelope,
            CancellationToken cancellationToken)
    {
        await this.GenerateTimetableAsync(
            envelope.Content.StudentId,
            cancellationToken);
    }
}
```

## 22. Notification Service

The Notification Service can be a Foundation Service or a processing service depending on the application's architecture.

It still follows the same substrate rule: public methods are normal service operations; event receivers are internal and explicitly implemented.

### 22.1 Public Service Interface

```csharp
public interface INotificationService
{
    ValueTask SendWelcomeEmailAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    ValueTask SendTimetableEmailAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);
}
```

### 22.2 NotificationService.cs

```csharp
public sealed partial class NotificationService : INotificationService
{
    private readonly IEmailBroker emailBroker;
    private readonly IEventSubstrate eventSubstrate;
    private readonly IEventEnvelopeFactory envelopeFactory;

    public NotificationService(
        IEmailBroker emailBroker,
        IEventSubstrate eventSubstrate,
        IEventEnvelopeFactory envelopeFactory)
    {
        this.emailBroker = emailBroker;
        this.eventSubstrate = eventSubstrate;
        this.envelopeFactory = envelopeFactory;
    }

    public async ValueTask SendWelcomeEmailAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        await this.emailBroker.SendWelcomeEmailAsync(
            studentId,
            cancellationToken);

        EventEnvelope<WelcomeEmailSentEvent> envelope =
            await this.envelopeFactory.CreateAsync(
                new WelcomeEmailSentEvent
                {
                    StudentId = studentId,
                    EmailAddress = "student@example.com"
                },
                cancellationToken);

        await this.eventSubstrate.PublishAsync(
            envelope,
            cancellationToken);
    }

    public async ValueTask SendTimetableEmailAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        await this.emailBroker.SendTimetableEmailAsync(
            studentId,
            cancellationToken);
    }
}
```

### 22.3 NotificationService.Substrate.cs

```csharp
public sealed partial class NotificationService :
    IEventReceiver<StudentCreatedEvent>,
    IEventReceiver<TimetableGeneratedEvent>
{
    async ValueTask
        IEventReceiver<StudentCreatedEvent>.ReceiveAsync(
            EventEnvelope<StudentCreatedEvent> envelope,
            CancellationToken cancellationToken)
    {
        await this.SendWelcomeEmailAsync(
            envelope.Content.StudentId,
            cancellationToken);
    }

    async ValueTask
        IEventReceiver<TimetableGeneratedEvent>.ReceiveAsync(
            EventEnvelope<TimetableGeneratedEvent> envelope,
            CancellationToken cancellationToken)
    {
        await this.SendTimetableEmailAsync(
            envelope.Content.StudentId,
            cancellationToken);
    }
}
```

## 23. Event Envelope Factory

A factory keeps envelope creation consistent.

```csharp
public interface IEventEnvelopeFactory
{
    ValueTask<EventEnvelope<TEvent>> CreateAsync<TEvent>(
        TEvent content,
        CancellationToken cancellationToken = default);
}
```

```csharp
public sealed class EventEnvelopeFactory : IEventEnvelopeFactory
{
    private readonly ISecurityContextBroker securityContextBroker;
    private readonly IRequestContextBroker requestContextBroker;
    private readonly IEnvelopeIntegrityBroker integrityBroker;

    public EventEnvelopeFactory(
        ISecurityContextBroker securityContextBroker,
        IRequestContextBroker requestContextBroker,
        IEnvelopeIntegrityBroker integrityBroker)
    {
        this.securityContextBroker = securityContextBroker;
        this.requestContextBroker = requestContextBroker;
        this.integrityBroker = integrityBroker;
    }

    public async ValueTask<EventEnvelope<TEvent>> CreateAsync<TEvent>(
        TEvent content,
        CancellationToken cancellationToken = default)
    {
        SecurityContext securityContext =
            await this.securityContextBroker.GetCurrentSecurityContextAsync(
                cancellationToken);

        RequestContext requestContext =
            await this.requestContextBroker.GetCurrentRequestContextAsync(
                cancellationToken);

        EnvelopeIntegrity integrity =
            await this.integrityBroker.SignAsync(
                securityContext,
                requestContext,
                cancellationToken);

        return new EventEnvelope<TEvent>
        {
            EventId = Guid.NewGuid(),
            EventName = typeof(TEvent).Name,
            EventVersion = 1,
            CorrelationId = Guid.NewGuid(),
            CausationId = Guid.Empty,
            OccurredDate = DateTimeOffset.UtcNow,
            SecurityContext = securityContext,
            RequestContext = requestContext,
            Integrity = integrity,
            Content = content
        };
    }
}
```

In a real implementation, the factory should reuse an existing `CorrelationId` from the request context when available.

---

## 24. Event Substrate Implementation Sketch

```csharp
public sealed class EventSubstrate : IEventSubstrate
{
    private readonly IEventStorageBroker storageBroker;
    private readonly ILocalEventDispatcher localDispatcher;
    private readonly IEventRouteProvider routeProvider;
    private readonly IEnvelopeIntegrityBroker integrityBroker;

    public EventSubstrate(
        IEventStorageBroker storageBroker,
        ILocalEventDispatcher localDispatcher,
        IEventRouteProvider routeProvider,
        IEnvelopeIntegrityBroker integrityBroker)
    {
        this.storageBroker = storageBroker;
        this.localDispatcher = localDispatcher;
        this.routeProvider = routeProvider;
        this.integrityBroker = integrityBroker;
    }

    public async ValueTask PublishAsync<TEvent>(
        EventEnvelope<TEvent> envelope,
        CancellationToken cancellationToken = default)
    {
        bool isValid = await this.integrityBroker.VerifyAsync(
            envelope.SecurityContext,
            envelope.RequestContext,
            envelope.Integrity,
            cancellationToken);

        if (!isValid)
        {
            throw new InvalidOperationException(
                "Event envelope integrity verification failed.");
        }

        StoredEvent storedEvent = MapToStoredEvent(envelope);

        await this.storageBroker.InsertEventAsync(
            storedEvent,
            cancellationToken);

        await this.localDispatcher.DispatchAsync(
            envelope,
            cancellationToken);

        IReadOnlyList<EventRoute> routes =
            await this.routeProvider.GetRoutesAsync(
                envelope.EventName,
                cancellationToken);

        foreach (EventRoute route in routes.Where(route => route.IsEnabled))
        {
            EventDelivery delivery = new()
            {
                Id = Guid.NewGuid(),
                EventId = envelope.EventId,
                DestinationName = route.DestinationName,
                DestinationType = route.DestinationType,
                DestinationAddress = route.DestinationAddress,
                Status = "Pending",
                AttemptCount = 0
            };

            await this.storageBroker.InsertDeliveryAsync(
                delivery,
                cancellationToken);
        }
    }

    public async IAsyncEnumerable<EventEnvelope<TEvent>> ReplayAsync<TEvent>(
        DateTimeOffset fromDate,
        DateTimeOffset? toDate = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<StoredEvent> events =
            await this.storageBroker.SelectEventsAsync(
                fromDate,
                toDate,
                cancellationToken);

        foreach (StoredEvent storedEvent in events)
        {
            if (storedEvent.EventName == typeof(TEvent).Name)
            {
                yield return MapFromStoredEvent<TEvent>(storedEvent);
            }
        }
    }

    private static StoredEvent MapToStoredEvent<TEvent>(
        EventEnvelope<TEvent> envelope)
    {
        return new StoredEvent
        {
            Id = Guid.NewGuid(),
            EventId = envelope.EventId,
            EventName = envelope.EventName,
            EventVersion = envelope.EventVersion,
            CorrelationId = envelope.CorrelationId,
            CausationId = envelope.CausationId,
            OccurredDate = envelope.OccurredDate,
            StoredDate = DateTimeOffset.UtcNow,
            SecurityContextJson = JsonSerializer.Serialize(envelope.SecurityContext),
            RequestContextJson = JsonSerializer.Serialize(envelope.RequestContext),
            IntegrityJson = JsonSerializer.Serialize(envelope.Integrity),
            MetadataJson = JsonSerializer.Serialize(envelope.Metadata),
            ContentJson = JsonSerializer.Serialize(envelope.Content),
            ContentType = typeof(TEvent).AssemblyQualifiedName ?? typeof(TEvent).Name
        };
    }

    private static EventEnvelope<TEvent> MapFromStoredEvent<TEvent>(
        StoredEvent storedEvent)
    {
        return new EventEnvelope<TEvent>
        {
            EventId = storedEvent.EventId,
            EventName = storedEvent.EventName,
            EventVersion = storedEvent.EventVersion,
            CorrelationId = storedEvent.CorrelationId,
            CausationId = storedEvent.CausationId,
            OccurredDate = storedEvent.OccurredDate,
            SecurityContext = JsonSerializer.Deserialize<SecurityContext>(
                storedEvent.SecurityContextJson)!,
            RequestContext = JsonSerializer.Deserialize<RequestContext>(
                storedEvent.RequestContextJson)!,
            Integrity = JsonSerializer.Deserialize<EnvelopeIntegrity>(
                storedEvent.IntegrityJson)!,
            Metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(
                storedEvent.MetadataJson) ?? new(),
            Content = JsonSerializer.Deserialize<TEvent>(
                storedEvent.ContentJson)!
        };
    }
}
```

---

## 25. External Delivery Worker

External delivery should usually happen in a background worker.

```csharp
public sealed class ExternalEventDeliveryWorker : BackgroundService
{
    private readonly IEventDeliveryService deliveryService;

    public ExternalEventDeliveryWorker(IEventDeliveryService deliveryService)
    {
        this.deliveryService = deliveryService;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await this.deliveryService.ProcessPendingDeliveriesAsync(
                stoppingToken);

            await Task.Delay(
                TimeSpan.FromSeconds(30),
                stoppingToken);
        }
    }
}
```

```csharp
public interface IEventDeliveryService
{
    ValueTask ProcessPendingDeliveriesAsync(
        CancellationToken cancellationToken = default);
}
```

The delivery service should:

1. Select pending deliveries.
2. Load the related stored event.
3. Send the event externally.
4. Mark delivery as delivered on success.
5. Increment attempt count on failure.
6. Schedule the next retry.
7. Dead-letter after retry exhaustion.

---

## 26. Dependency Injection Example

Public service registration remains normal and Standard-compliant.

```csharp
services.AddScoped<IEventSubstrate, EventSubstrate>();
services.AddScoped<IEventEnvelopeFactory, EventEnvelopeFactory>();
services.AddScoped<ILocalEventDispatcher, LocalEventDispatcher>();
services.AddScoped<IEventStorageBroker, EventStorageBroker>();
services.AddScoped<IEnvelopeIntegrityBroker, EnvelopeIntegrityBroker>();
services.AddScoped<IEventRouteProvider, EventRouteProvider>();

services.AddScoped<IStudentOrchestrationService, StudentOrchestrationService>();
services.AddScoped<IStudentService, StudentService>();
services.AddScoped<IEnrollmentService, EnrollmentService>();
services.AddScoped<ITimetableService, TimetableService>();
services.AddScoped<INotificationService, NotificationService>();

services.AddHttpClient<RestExternalEventDispatcher>();
services.AddHostedService<ExternalEventDeliveryWorker>();
```

Receiver registration should be treated as substrate infrastructure.

The application should not encourage normal code to resolve `IEventReceiver<T>` directly.

One option is to have the substrate scan assemblies and register internal receivers:

```csharp
services.AddEventSubstrateReceiversFromAssembly(
    typeof(StudentService).Assembly);
```

The substrate registration can internally map:

```csharp
IEventReceiver<StudentEnrolledEvent>      -> StudentService
IEventReceiver<StudentCreatedEvent>       -> EnrollmentService
IEventReceiver<StudentEnrolledEvent>      -> TimetableService
IEventReceiver<StudentCreatedEvent>       -> NotificationService
IEventReceiver<TimetableGeneratedEvent>   -> NotificationService
```

The important rule is not the exact registration mechanism.

The important rule is:

> Application code may call `IStudentService`, `IEnrollmentService`, and `ITimetableService`. Only the substrate may call `IEventReceiver<T>`.

For larger applications, receiver registration should be automated through assembly scanning.

## 27. Ordering Considerations

Not all events require strict ordering.

Where ordering matters, include ordering information:

```csharp
public sealed class EventOrdering
{
    public string StreamName { get; init; } = string.Empty;

    public long SequenceNumber { get; init; }
}
```

Example stream names:

```text
Student:{studentId}
Course:{courseId}
Tenant:{tenantId}
```

Ordering should normally be guaranteed per stream, not globally.

---

## 28. Versioning Events

Events should be versioned.

Example:

```csharp
public sealed class StudentCreatedEventV1
{
    public Guid StudentId { get; init; }

    public string FullName { get; init; } = string.Empty;
}
```

```csharp
public sealed class StudentCreatedEventV2
{
    public Guid StudentId { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;
}
```

The envelope should include:

```csharp
public int EventVersion { get; init; }
```

External contracts should be stable and backward compatible where possible.

---

## 29. Security Considerations

Authentication happens at the entry point.

Examples:

- Controller
- Azure Function trigger
- Message receiver
- External webhook receiver

Authorization happens where needed.

For example, a controller may authenticate the user, but a foundation service or orchestration service may still call an authorization service before performing sensitive work.

```csharp
await this.authorizationService.AuthorizeAsync(
    envelope.SecurityContext,
    "Students.Enroll",
    cancellationToken);
```

Envelope integrity should protect the security and request context.

External fan-out should also consider:

- signed messages
- API keys
- OAuth client credentials
- mutual TLS
- event HMAC signatures
- endpoint-specific secrets
- replay protection

---

## 30. Avoiding Event Spaghetti

The main risk with event-driven systems is hiding business flow inside event chains.

Use these rules:

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

---

## 31. Recommended Folder Structure

```text
Services
├── Foundations
│   ├── Students
│   │   ├── IStudentService.cs
│   │   ├── StudentService.cs
│   │   ├── StudentService.Validations.cs
│   │   ├── StudentService.Exceptions.cs
│   │   └── StudentService.Substrate.cs
│   ├── Enrollments
│   │   ├── IEnrollmentService.cs
│   │   ├── EnrollmentService.cs
│   │   ├── EnrollmentService.Validations.cs
│   │   ├── EnrollmentService.Exceptions.cs
│   │   └── EnrollmentService.Substrate.cs
│   └── Timetables
│       ├── ITimetableService.cs
│       ├── TimetableService.cs
│       ├── TimetableService.Validations.cs
│       ├── TimetableService.Exceptions.cs
│       └── TimetableService.Substrate.cs
│
├── Orchestrations
│   └── Students
│       ├── IStudentOrchestrationService.cs
│       └── StudentOrchestrationService.cs
│
├── Events
│   ├── Abstractions
│   │   ├── IEventSubstrate.cs
│   │   ├── IEventReceiver.cs
│   │   └── IEventEnvelopeFactory.cs
│   ├── Models
│   │   ├── EventEnvelope.cs
│   │   ├── EnvelopeIntegrity.cs
│   │   ├── SecurityContext.cs
│   │   └── RequestContext.cs
│   ├── StudentEvents
│   │   ├── StudentCreatedEvent.cs
│   │   ├── StudentUpdatedEvent.cs
│   │   ├── StudentDeletedEvent.cs
│   │   ├── StudentEnrolledEvent.cs
│   │   └── TimetableGeneratedEvent.cs
│   └── Substrate
│       ├── EventSubstrate.cs
│       ├── LocalEventDispatcher.cs
│       ├── EventReceiverRegistrationExtensions.cs
│       └── ExternalEventDeliveryWorker.cs
│
Brokers
├── Storages
│   └── EventStorageBroker.cs
├── Securities
│   └── SecurityContextBroker.cs
├── Requests
│   └── RequestContextBroker.cs
└── Integrities
    └── EnvelopeIntegrityBroker.cs
```

The `*.Substrate.cs` convention is important because it makes the substrate boundary visible in the codebase.

A reviewer should be able to open a Foundation Service and immediately see:

```text
StudentService.cs              public intent-driven operations
StudentService.Substrate.cs    internal event-driven reactions
```

## 32. Migration Path

This pattern can start simple and evolve over time.

### Stage 1: In-memory local events

```text
Publish event → resolve local handlers → call handlers
```

### Stage 2: Durable local events

```text
Publish event → store event → dispatch local handlers
```

### Stage 3: External fan-out

```text
Publish event → store event → local dispatch → create external deliveries → background worker sends
```

### Stage 4: Replay and rehydration

```text
Select stored events from date → replay to local/external target
```

### Stage 5: Distributed substrate

```text
Local substrate → Service Bus / Kafka / Event Grid → external systems
```

The service code should not need to change when moving between these stages.

Only the substrate implementation changes.

---

## 33. Summary

The event substrate pattern creates a consistent way for services to emit and receive events while keeping orchestration explicit and preserving Standard-compliant service contracts.

It gives the application:

- local event dispatch
- durable event storage
- external fan-out
- replay
- rehydration
- auditability
- traceability
- loose coupling
- future migration to distributed architecture

The most important design rule is:

> **Use orchestration for required business flow. Use events for reactions.**

The most important Standard-compliance rule is:

> **The substrate extends the service implementation, not the public Foundation Service interface.**

Public consumers should depend on normal service interfaces such as:

```csharp
IStudentService
IEnrollmentService
ITimetableService
```

The substrate may internally resolve:

```csharp
IEventReceiver<StudentCreatedEvent>
IEventReceiver<StudentEnrolledEvent>
IEventReceiver<TimetableGeneratedEvent>
```

but normal application code must not call these receivers directly.

This keeps the public service model clean while still allowing services to act as event-aware capability owners.

---

## 33.1 Architectural Rules

1. Foundation Services remain Standard-compliant.
2. CRUD and business methods remain the primary public API.
3. The substrate extends the implementation, not the public interface.
4. Events are normally facts, not commands.
5. Required business flow belongs in orchestration services.
6. Reactive behaviour belongs in substrate receivers.
7. Event receivers must be internal.
8. Only the Event Substrate may invoke receivers.
9. Receivers should use explicit interface implementation.
10. Receiver implementations belong in `*.Substrate.cs` partial files.
11. Events must be persisted before external fan-out.
12. External systems never receive events directly from Foundation Services.
13. Replay operates from the persisted event store.
14. Handlers/receivers must be idempotent.
15. Correlation and causation identifiers must be preserved across event chains.
16. Replay must be explicit and controlled.

## 34. When Intent Starts With an Event

The rule "service calls are for intent, events are for reaction" describes the common case.

However, there is a legitimate pattern where **intent itself is triggered by an incoming external signal** — and in that case, the orchestration service publishes an event as a deliberate dispatch mechanism, not as a normal reaction.

This is an exception and should be used carefully.

### 34.1 The Graph Import Scenario

Consider an orchestration service that polls an external API and receives a graph of related objects:

```text
ExternalApiOrchestrationService
    │
    ├── Calls external API
    ├── Receives graph: { Department, [Course], [Student], [Enrollment] }
    │
    └── Needs to create each object locally in the correct order
```

The orchestration service could call each foundation service directly in sequence.

But if the graph is large, the object types are variable, or the creation logic needs to be owned by each domain service independently, the orchestration service can instead **publish a scoped import event per object** and let the appropriate Foundation Service receive the event internally through the substrate.

```text
ExternalApiOrchestrationService
    │
    ├── Calls external API → receives graph
    │
    ├── Publishes: DepartmentImportRequestedEvent  ──▶ DepartmentService.Substrate.cs
    ├── Publishes: CourseImportRequestedEvent       ──▶ CourseService.Substrate.cs
    ├── Publishes: StudentImportRequestedEvent      ──▶ StudentService.Substrate.cs
    └── Publishes: EnrollmentImportRequestedEvent   ──▶ EnrollmentService.Substrate.cs
```

### 34.2 Is This Intent or Reaction?

The event in this pattern is still **intent** — not reaction.

The orchestration service is making a deliberate decision to dispatch work to the correct domain owners.

The distinction from a pure reaction event is:

| Characteristic | Reaction Event | Intentional Dispatch Event |
|---|---|---|
| Emitted because | Something already happened | A deliberate routing decision is being made |
| Handler/receiver is | Optional / loosely coupled | Expected and required |
| Order matters | Usually not | Often yes |
| Who owns the receiver | The reacting service | The domain service responsible for that object type |
| Example | `StudentCreatedEvent` → AuditService reacts | `StudentImportRequestedEvent` → StudentService is the required receiver |

### 34.3 Naming Convention

Use a name that reflects the intent, not a past-tense fact, because the work has not happened yet:

```text
StudentImportRequestedEvent      ✅  (intent — work is being dispatched)
StudentCreatedEvent              ✅  (fact — work already happened)
CreateStudentEvent               ❌  (command style — avoid)
ImportStudentEvent               ❌  (ambiguous — past or future?)
```

### 34.4 Example: Graph Import Orchestration

```csharp
public sealed class ExternalApiOrchestrationService : IExternalApiOrchestrationService
{
    private readonly IExternalApiBroker externalApiBroker;
    private readonly IEventSubstrate eventSubstrate;
    private readonly IEventEnvelopeFactory envelopeFactory;

    public ExternalApiOrchestrationService(
        IExternalApiBroker externalApiBroker,
        IEventSubstrate eventSubstrate,
        IEventEnvelopeFactory envelopeFactory)
    {
        this.externalApiBroker = externalApiBroker;
        this.eventSubstrate = eventSubstrate;
        this.envelopeFactory = envelopeFactory;
    }

    public async ValueTask SyncFromExternalAsync(
        CancellationToken cancellationToken = default)
    {
        ExternalGraph graph =
            await this.externalApiBroker.GetGraphAsync(cancellationToken);

        foreach (ExternalDepartment department in graph.Departments)
        {
            EventEnvelope<DepartmentImportRequestedEvent> envelope =
                await this.envelopeFactory.CreateAsync(
                    new DepartmentImportRequestedEvent
                    {
                        ExternalId = department.Id,
                        Name = department.Name
                    },
                    cancellationToken);

            await this.eventSubstrate.PublishAsync(envelope, cancellationToken);
        }

        foreach (ExternalStudent student in graph.Students)
        {
            EventEnvelope<StudentImportRequestedEvent> envelope =
                await this.envelopeFactory.CreateAsync(
                    new StudentImportRequestedEvent
                    {
                        ExternalId = student.Id,
                        FirstName = student.FirstName,
                        LastName = student.LastName,
                        Email = student.Email
                    },
                    cancellationToken);

            await this.eventSubstrate.PublishAsync(envelope, cancellationToken);
        }
    }
}
```

The `StudentService` handles the import event internally in `StudentService.Substrate.cs` and owns the creation logic:

```csharp
public sealed partial class StudentService :
    IEventReceiver<StudentImportRequestedEvent>
{
    async ValueTask
        IEventReceiver<StudentImportRequestedEvent>.ReceiveAsync(
            EventEnvelope<StudentImportRequestedEvent> envelope,
            CancellationToken cancellationToken)
    {
        bool alreadyExists =
            await this.storageBroker.ExistsStudentByExternalIdAsync(
                envelope.Content.ExternalId,
                cancellationToken);

        if (alreadyExists)
        {
            return;
        }

        Student student = new()
        {
            Id = Guid.NewGuid(),
            FirstName = envelope.Content.FirstName,
            LastName = envelope.Content.LastName,
            Email = envelope.Content.Email
        };

        await this.AddStudentAsync(student, cancellationToken);
    }
}
```

### 34.5 When To Use This Pattern

Use intentional dispatch events when:

- The graph is large and each object type has its own domain service.
- You want each domain service to own its own creation and idempotency logic.
- The orchestration service should not know the internal details of each domain service.
- Objects from the same import need to be replayed or retried independently.
- The import itself needs to be auditable per object.

Do not use this pattern when:

- The object graph is small and fixed — call foundation services directly from the orchestration service.
- Order is strictly enforced and cannot be guaranteed by event dispatch.
- The receiver is not guaranteed to exist — intentional dispatch events require a known, registered receiver.

### 34.6 Order of Dependent Objects

If objects in the graph depend on each other, the orchestration service must either:

1. Publish events in dependency order and dispatch sequentially.
2. Or call dependent foundation services directly first, then publish events for the rest.

```text
// Safe ordering for dependent graph import:

[1] Publish DepartmentImportRequestedEvent  → await receiver completes
[2] Publish CourseImportRequestedEvent      → await receiver completes (depends on department)
[3] Publish StudentImportRequestedEvent     → await receiver completes
[4] Publish EnrollmentImportRequestedEvent  → await receiver completes (depends on student + course)
```

This is still event-driven dispatch, but the orchestration service controls sequencing by awaiting each publish before proceeding to the next.

Even in this exceptional pattern, receiver methods remain internal and should live in `*.Substrate.cs` partial files.

