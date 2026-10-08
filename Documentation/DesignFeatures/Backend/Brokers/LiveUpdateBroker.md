# Live update broker
Parent: [LiveUpdates.md](../../LiveUpdates.md)
Level: broker — `ILiveUpdateBroker`, declared in Core (`Glory2Him.Core/Brokers/LiveUpdates/`) and implemented in the host over SignalR (`Websites/Glory2Him.WebApp/Brokers/LiveUpdates/`), new
Inherits: §ARC12.2, §ARC12.12 rules 2–4, §SEC14.6.1, `the-standard-brokers`

Sends a live update to every connection the hub holds. It holds no logic: what is sent, and whether, is decided above it (`Backend/Foundations/LiveUpdateService.md`, `Backend/Orchestrations/LiveUpdateOrchestrationService.md`). Like every broker it gets no unit tests. The hub's acceptance tests prove over real HTTP that what it sends arrives, and that a reader who stops reading holds no writer (`Backend/Hubs/LiveUpdatesHub.md §1`).

## 1. SendLiveUpdateAsync (#894)

```csharp
ValueTask SendLiveUpdateAsync(
    LiveUpdate liveUpdate,
    CancellationToken cancellationToken = default);
```

1. **It sends the update to every connection** as the client method `ReceiveLiveUpdate`, the update its one argument, through the hub context's `Clients.All`, passing the caller's `CancellationToken`. `ReceiveLiveUpdate` is the name the React broker listens for (`UI/Brokers/LiveUpdateBroker.md`).
2. **It does not wait for any connection to take the update.** It starts SignalR's send and returns without awaiting it. SignalR starts the write to every connection before its send returns, so a reader who is reading has the update at once. The send's task completes only once every connection has taken the update, and a connection that has stopped reading would hold it until SignalR closes that connection, after `TransportSendTimeout`, 10 seconds by default. The send runs inside the request of the write that caused it (§EVN11), and anyone may open a connection (§SEC14.8 rule 2), so a broker that waited would let any reader hold every writer. Not waiting is how it calls SignalR, not a decision: it has no branch, catches nothing, and returns nothing, as SignalR's send answers nothing (ts-brokers-007). What happens to a connection after the send has started is SignalR's: it logs a write that fails and closes that connection. A token already cancelled sends nothing.
3. **The interface is Core's and names no technology** (ts-brokers-002). The implementation is the host's, because Core references nothing of ASP.NET Core's shared framework (§ARC12.12 rule 3). The interface takes a native model and returns nothing (ts-brokers-006).
4. **The hub it sends through is created with it.** `LiveUpdatesHub` is an empty SignalR `Hub` in `Websites/Glory2Him.WebApp/Hubs/`, the type the hub context is for. `Backend/Hubs/LiveUpdatesHub.md §1` maps it and opens it to readers. Until then the broker sends to no one.
5. **The host registers SignalR and the broker** in `CoreRegistration.AddCoreServices`: `AddSignalR()`, and `ILiveUpdateBroker` scoped, beside the other request-bound brokers. The broker captures no caller, so its lifetime decides nothing about identity (§SEC14.6.1). It is scoped all the same, which leaves §SEC14.6.1's list of the host's singleton brokers as it stands.
