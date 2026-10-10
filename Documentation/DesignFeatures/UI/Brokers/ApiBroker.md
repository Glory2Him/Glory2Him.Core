# API broker
Parent: [BrokersHoldNoLogic.md](../../BrokersHoldNoLogic.md)
Level: broker — `ApiBroker` (`Websites/Glory2Him.WebApp.React/src/brokers/apiBroker.ts`), existing
Inherits: §UI20.9.1 (*Scope*, departure 6), §UI20.9.3 rule 7, `the-standard-reacttypescript-brokers` as §UI20.9.1 departs from it

The app's one wrapper over axios, which every entity broker calls (§UI20.9.1 departure 6). Today the same file also registers a response interceptor on the shared axios module that decides whether this origin is reachable and raises two window events, `g2h-network-reachable` and `g2h-network-unreachable`, which `useOnlineStatus` listens for (`apiBroker.ts:3-52`). That decision is logic (§UI20.9.3 rule 7). `ApiBroker` keeps only a way for a service to hear the responses, and the decision moves to `networkStatusService` (`UI/Foundations/NetworkStatusService.md §1`).

## 1. ListenToResponses (#945)

```ts
ListenToResponses(listeners: ResponseListeners): () => void
```

`ResponseListeners` is `{ onResponse: (response: AxiosResponse) => void; onFailure: (error: unknown) => void }`, in `src/models/foundations/networkStatuses/responseListeners.ts`, new.

1. **It adds one response interceptor to the shared axios module**, as the interceptor today is added, so it hears every request the app makes, through `ApiBroker` or not (`apiBroker.ts:11-17`).
2. **The interceptor calls `onResponse` with each response, and `onFailure` with each failure, as axios raised them.**
3. **It passes each response on, and rejects with each failure, unchanged.** Listening changes no request's outcome.
4. **It returns a function that takes that interceptor out again**, and no other.
5. **It is named for what it does, not for a verb it sends**, because it sends no request (§UI20.9.3 rule 7). `networkStatusService` alone calls it (§UI20.9.1 departure 6).

## 2. The connectivity interceptor — deleted (#975)

Deleted once `useOnlineStatus` hears the API through `networkStatusService` (`UI/Hooks/OnlineStatus.md §1`) and nothing imports what it exports. It is `isSameOriginRequest`, `markNetworkReachable`, `markNetworkUnreachableIfUnreachable`, the two event names `NETWORK_REACHABLE_EVENT` and `NETWORK_UNREACHABLE_EVENT`, and the line that adds them to axios (`apiBroker.ts:3-52`). `apiBroker.networkStatus.test.ts` goes with them: its first seven cases have moved to `networkStatusService`'s tests, and its eighth tests the registration being deleted (`BrokersHoldNoLogic.md` rule 5).
