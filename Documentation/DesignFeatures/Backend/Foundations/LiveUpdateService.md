# Live update service
Parent: [LiveUpdates.md](../../LiveUpdates.md)
Level: foundation — `ILiveUpdateService` (`Glory2Him.Core/Services/Foundations/LiveUpdates/`), new
Inherits: §SEC14.8 rules 1, 3 and 5, §ARC12.12, `Backend/Models/LiveUpdate.md §1`, `the-standard-foundations`

The one way into the live connection. It refuses an update whose shape could tell more than its type says, and sends the rest through `ILiveUpdateBroker` (`Backend/Brokers/LiveUpdateBroker.md §1`). It is single-entity — `LiveUpdate` — and reads nothing.

## 1. SendLiveUpdateAsync (#895)

```csharp
ValueTask SendLiveUpdateAsync(
    LiveUpdate liveUpdate,
    CancellationToken cancellationToken = default);
```

1. **A valid update is sent through `ILiveUpdateBroker.SendLiveUpdateAsync` unchanged**, with the caller's `CancellationToken`.
2. **An update is refused when its shape could carry more than its type says, or names what it does not:** a null update; a `Type` that is not a member of `LiveUpdateType`; a `ContentItemSettingChanged` that names a content item; and a `ContentItemReactionsChanged` that names none, or names `Guid.Empty`. Each refusal is a validation failure naming the field, and nothing is sent.
3. **It has no caller gate, and reads no caller.** It publishes no fact and answers no request address, no route serves it, and its one caller is `LiveUpdateOrchestrationService`. What it sends tells nobody anything a public read does not (§SEC14.8 rule 1). Whether a count message may be sent is decided by that caller, which can read the content item the message names (§SEC14.8 rule 3), where this single-entity service cannot. So it mints no envelope and resolves no `SecurityContext`, as the content item foundation's caller-independent reads mint none (`IContentItemService.RetrieveContentItemFeedAsync`). A design that gives it another caller decides that caller's visibility rule first (§SEC14.8 rule 5).
4. **Its failures are mapped as the foundation services map theirs** (`ReactionService.Exceptions.cs`): a cancelled token is rethrown unwrapped, a cancellation the caller did not ask for is a timeout and this service's dependency failure, and any other failure is its service failure. Each is logged before it is thrown.
