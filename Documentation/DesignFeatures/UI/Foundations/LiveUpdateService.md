# Live update service (React)
Parent: [LiveUpdates.md](../../LiveUpdates.md)
Level: foundation service — `liveUpdateService` (`Websites/Glory2Him.WebApp.React/src/services/foundations/liveUpdateService.ts`), new
Inherits: §UI20.10, §UI20.9.1 departure 4, §UI20.9.2, §SEC14.8 rule 2, §ARC16.8 (*Which rows are counted*, the rule 6 row), `UI/Brokers/LiveUpdateBroker.md`, `UI/Foundations/AssociationService.md §3` rule 1, LiveUpdates.md rules 4–6 and 10

What turns a message into a fresh page. It keeps the tab's one connection open, and makes stale the reads a message says may have changed. TanStack Query then reads them again wherever a page is showing them. It has one member, which `Root` calls once (§UI20.10 rule 1). It takes the app's foundation-service shape: an exported object whose member is a hook that constructs the broker it calls (§UI20.9.1 departure 4).

## 1. useLiveUpdates (#T17)

```ts
useLiveUpdates: () => void
```

1. **While it is mounted the tab holds one connection.** It starts the connection through `LiveUpdateBroker.startLiveUpdatesAsync` when it mounts, and stops it through `stopLiveUpdatesAsync` when it unmounts. It reads no sign-in state, because every reader's connection is the same (§SEC14.8 rule 2).
2. **A setting message makes every content item setting read stale, and every reaction summary read.** The setting reads are the five families a settings write already makes stale: `invalidateContentItemSettingReads` in `contentItemSettingService.ts` is exported for this, so that set has one home. The summary reads are included because a summary answers a host's `ShowReactions` inside its count (§ARC16.8, the rule 6 row of *Which rows are counted*), so a changed setting can change what a summary says.
3. **A count message makes stale only the reaction summary reads whose page of ids holds its content item.** A summary read is keyed `['ReactionSummaries', <readerId>, <that page's ids>]` (`UI/Foundations/AssociationService.md §3` rule 1), so the reads for other cards, and every other read, are left alone.
4. **A message it cannot read changes nothing**: a type it does not know, and a count message with no content item. A host newer than the page may send a type the page predates.
5. **A connection that comes back makes every read rules 2 and 3 cover stale.** That covers a reconnection, and a start that succeeds after an earlier attempt failed. Either may follow messages the tab never heard. A first start that succeeds at once makes nothing stale, because the pages it serves have just read (`LiveUpdates.md`, *Risks*).
6. **A connection that will not start, or that closes for good, is started again** after 0, 2, 10 and 30 seconds, and then every 30 seconds for as long as the tab is open. That is SignalR's own reconnection schedule, its documented default, continued without end. The service hands the same schedule to the broker for SignalR's automatic reconnection, so a lost connection is never given up.
7. **Nothing is announced.** A failed start, a lost connection and a reconnection show the reader nothing and raise no toast (§UI20.10 rule 4). A read that a message prompts and that then fails is announced as any failed read is, by the app's global handler (`apiBroker.globals.ts`).
