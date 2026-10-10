# Online status hook
Parent: [BrokersHoldNoLogic.md](../../BrokersHoldNoLogic.md)
Level: hook — `useOnlineStatus` (`Websites/Glory2Him.WebApp.React/src/hooks/useOnlineStatus.ts`), existing
Inherits: §UI20.9.3 rule 7, `UI/Foundations/NetworkStatusService.md`

Whether the reader is online, so the installed app can show a clear offline state (`offlineBanner.tsx`) rather than let a cached page look live. It hears the browser's `online` and `offline` events, and today it also hears two window events that `ApiBroker`'s interceptor raises from real requests (`apiBroker.ts`). Under §UI20.9.3 rule 7 it hears the API through `networkStatusService` instead, and the window events go.

## 1. useOnlineStatus (#960)

```ts
useOnlineStatus: () => boolean
```

1. **It starts from `navigator.onLine`.**
2. **The browser's `offline` event makes it `false`, and its `online` event `true`**, as today.
3. **It hears the API through `networkStatusService.useApiReachability`** (`UI/Foundations/NetworkStatusService.md §1`): the API reached makes it `true`, and the API found unreachable makes it `false`. It no longer listens for `g2h-network-reachable` or `g2h-network-unreachable`, or imports anything from `apiBroker.ts`.
4. **The latest signal wins**, from the browser or the API.
