# Live update broker (React)
Parent: [LiveUpdates.md](../../LiveUpdates.md)
Level: broker — `LiveUpdateBroker` (`Websites/Glory2Him.WebApp.React/src/brokers/hubBroker.liveUpdates.ts`), new
Inherits: §ARC12.12 rules 4 and 6, §UI20.9.1 departures 4 and 5, `the-standard-reacttypescript-brokers`, `Backend/Hubs/LiveUpdatesHub.md`

The React app's door to the live connection. It holds the tab's one SignalR connection to `/api/LiveUpdates` and hands up what the hub sends. It holds no logic: when to connect, how soon to try again, and what a message makes stale all belong to the live update service (`UI/Foundations/LiveUpdateService.md`). It is the app's first hub broker. Its file name carries the dependency type as its prefix, `hubBroker.`, and its class is named for its entity, as §UI20.9.1 departure 5 has every broker. The owner ruled on 2026-10-07 that the hub broker follows the app's layout, choosing *Follow the app's layout* over the skill's `src/brokers/hubs/liveUpdateHubBroker.ts` and `LiveUpdateHubBroker`, with departure 5 worded for three kinds of broker, which the owner approves with the rest of §UI20.9.1. Its members follow the skill's names, `{verb}{Entity}Async` (tsr-brokers-005); §UI20.9.1 departure 3's PascalCase is the API brokers' and does not reach it. It calls no other broker (tsr-brokers-019).

**The dependency it grants.** `@microsoft/signalr`, at the major version of the host's ASP.NET Core (`^10.0.0`), is added to the React app's `dependencies` (§ARC12.12 rule 4). Only this broker imports it.

**What crosses the wire is the server's shape.** The hub calls `ReceiveLiveUpdate` with one argument, `{ type, contentItemId }`. `type` is a number mirroring `LiveUpdateType`, `1` for a setting changed and `2` for a content item's reactions changed. `contentItemId` is a string or `null` (`Backend/Models/LiveUpdate.md §1`). The model is new, in `src/models/foundations/liveUpdates/liveUpdate.ts`.

**In development** the dev server forwards the connection to the host as it forwards the rest of `/api`, with WebSockets enabled on that proxy rule (`vite.config.ts`, `server.proxy`; §ARC12.12 rule 6).

## 1. startLiveUpdatesAsync (#907)

```ts
startLiveUpdatesAsync(
    listeners: LiveUpdateListeners,
    nextRetryDelayInMilliseconds: (previousRetryCount: number) => number | null
): Promise<void>
```

`LiveUpdateListeners` is `{ onLiveUpdate: (liveUpdate: LiveUpdate) => void; onReconnected: () => void; onClosed: () => void }`.

1. **On its first call it builds the connection to `/api/LiveUpdates`**, with SignalR's automatic reconnection asking the broker for each retry's delay.
2. **Every call hands the connection the listeners and the retry delay function it was given, in place of an earlier call's, and then starts it.** A later call starts the same connection again. From then on the connection calls only the latest call's listeners and asks only its retry delay function, each listener once for each event: an earlier call's are never called again, and none is called twice. React's strict mode starts the connection again with new listeners after a stop, and a handler from the unmounted effect left live, or one registered twice, would start a second retry.
3. **It calls `onLiveUpdate` with each `ReceiveLiveUpdate` message as it arrived, `onReconnected` when SignalR has reconnected, and `onClosed` when the connection has closed for good.**
4. **A start that fails rejects with SignalR's own error, unchanged** (*Deviations*). The listeners it was given are the connection's all the same.

## 2. stopLiveUpdatesAsync (#908)

```ts
stopLiveUpdatesAsync(): Promise<void>
```

1. **It stops the connection, if one was started, and resolves once it has stopped.** A stop that fails rejects with SignalR's own error, unchanged (*Deviations*).

## Deviations

One, from `the-standard-reacttypescript-brokers`, approved by the owner on 2026-10-06, who chose *Approve it* when it was put to them:

1. **tsr-brokers-003 — *"Brokers MUST map external exceptions to broker-layer exceptions with meaningful messages."* — and tsr-brokers-015 — *"Brokers MUST catch external exceptions and wrap them in broker-specific exceptions."*** A start or a stop that fails rejects with SignalR's own error, unchanged. **Why:** its one consumer, the live update service, answers every failure the same way: it tries again later and shows the reader nothing (§UI20.10 rule 4). A broker error type would carry nothing anyone reads. It also keeps the app's one rule for brokers, that an error leaves a broker unchanged (§UI20.9.1 departure 1). That departure is written for the API brokers' `AxiosError`, and does not reach this broker. **Instead:** the error leaves the broker unchanged.
