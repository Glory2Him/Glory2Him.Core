# Network status service (React)
Parent: [BrokersHoldNoLogic.md](../../BrokersHoldNoLogic.md)
Level: foundation service — `networkStatusService` (`Websites/Glory2Him.WebApp.React/src/services/foundations/networkStatusService.ts`), new
Inherits: §UI20.9.1 departure 6, §UI20.9.2, §UI20.9.3 rule 7, `UI/Brokers/ApiBroker.md`, `the-standard-reacttypescript-services` as §UI20.9.2 departs from it

Whether the API can be reached, from what really happens to the app's requests. A browser's `navigator.onLine` says only whether a network adapter is connected, so a captive portal or a downed host leaves it at `true`. A response reaching the app at all, even an error, proves the API is reachable, and a request that got no response is the real sign it is not. Today `ApiBroker`'s interceptor decides this and raises two window events (`apiBroker.ts`). Under §UI20.9.3 rule 7 the decision is this service's. It wraps `ApiBroker` alone (tsr-services-001), the one broker that hears every response, through `ListenToResponses` (`UI/Brokers/ApiBroker.md §1`). It holds no query: nothing it decides is read from the server.

**Same origin**, below, means a request whose `url`, resolved against its `baseURL`, or against the page's own origin when it has none, has the page's origin. A request with no `url`, or one that cannot be resolved, is not same origin. A cross-origin request, such as one through `GetAsyncAbsolute`, says nothing about whether this app's API can be reached.

## 1. useApiReachability (#959)

```ts
useApiReachability: (listeners: { onReachable: () => void; onUnreachable: () => void }) => void
```

1. **While it is mounted, it listens to every response and failure** through `ApiBroker.ListenToResponses`, once, and stops listening when it unmounts. It calls the listeners its caller handed it most recently.
2. **A response to a same-origin request calls `onReachable`**, whatever its status: the server answered.
3. **A failure calls `onUnreachable` when the request was same origin, got no response, and was not cancelled.** A cancelled request was superseded, not lost to the network (`axios.isCancel`).
4. **Nothing else calls either listener**: a response or a failure for a cross-origin request, a failure the server answered with a status, a cancelled request, or a failure that is not an axios error.
5. **It changes no request's outcome.** A response is passed on, and a failure rejects as it did (`UI/Brokers/ApiBroker.md §1`).
