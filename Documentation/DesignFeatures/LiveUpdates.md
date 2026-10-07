# Live updates
Epic: [INTENT.md](../../INTENT.md)
Inherits: §ARC12.1, §ARC12.5, §ARC12.5.2 business rules 1, 2 and 12, §ARC12.12, §ARC16.2.2, §ARC16.8, §ARC16.8.1, §APR8.6.1 rule 3, §DOM4.10 rule 8, §EVN2 rule 6, §EVN11, §EVN18 rule 1, §EVN20 rules 5 and 10, §EVN23, §EVN26, §SEC14.1, §SEC14.3, §SEC14.5, §SEC14.6 rule 4, §SEC14.7 posture A′ rule 7, §SEC14.7 posture C rule 2, §SEC14.8, §UI20.8 rule 3, §UI20.10, Likes.md rule 12
Mockups: none. A page shows what it already shows, only sooner.
Design task: #702

An open page learns, without a reload, that something it shows has changed. The server tells every open page when a change happens that may move what it shows, and each page reads that part again through the reads it already makes. The owner ruled on 2026-10-06 that this first design covers two changes, a content item's setting and its Like counts, over a SignalR connection, with the host on one instance.

## Problem

An open page shows the world as it stood at the page's last read. A setting another person changes, and a reaction another reader gives, reach it only on a reload, or when the query library reads again on its own: when the tab regains focus, when the connection returns, or once a read has gone stale. Seven pages carry a rule that a changed setting reaches them without a reload, and the Likes feature carries one that counts update live. When this feature was designed (2026-10-06), nothing told an open page that anything had changed: the only freshness was the writer's own browser re-reading after its own write, and the review round's 15-second refresh on the moderation page.

## Business rules

1. **A changed setting reaches every open page that shows an item it governs, without a reload** (§ARC12.5.2 business rule 12). That covers an item's override added, amended or removed, and a content type's default amended while the item has no override (§ARC12.5.2 business rules 1–2). The rule is each page's own: `UI/Pages/Home.md rule 2.24`, `UI/Pages/Posts.md rule 2.25`, `UI/Pages/PostDetail.md rule 2.22`, `UI/Pages/MyPosts.md rule 2.19`, `UI/Pages/MyPostDetail.md rule 2.22`, `UI/Pages/ContentItemModerationPage.md rule 2.16` and `UI/Pages/ContentItemModerationDetailPage.md rule 2.29`.
2. **Another reader's reaction reaches every open page that shows the item's counts, without a reload** (Likes.md rule 12).
3. **Those two changes are the whole of what is pushed** (owner ruling 2026-10-06: the owner chose *Settings and Like counts* over adding the review round, and over adding posts appearing, changing and leaving). The review round keeps its 15-second refresh (`UI/Pages/ContentItemModerationDetailPage.md rule 2.12`). An item that is approved, amended, unpublished or removed, and a list that gains or loses an item, reach an open page as they did before: on a reload, or on the query library's own triggers. Each would tell readers about something most of them may not read, so each is a later design under §SEC14.8 rule 5.
4. **A message says only that a read is stale, and the page reads it again.** A setting message carries its type alone. A count message carries its type and the id of the content item version whose counts may have moved. Neither carries a row (§SEC14.8 rule 1). This is the owner's *"so the UI can refresh accordingly as needed"* (§ARC12.5.2 business rule 12): the page refreshes through the reads it already makes, each with its own gate (§SEC14.8 rule 4).
5. **A setting message is sent for every `ContentItemSetting-Added`, `-Modified` and `-Removed`, `-HardRemoved` included**, whether the row is a content type's default or an item's override: either can change what an item renders under (§ARC12.5.2 business rules 1–2). Every setting row is public-read (§SEC14.7 posture C rule 2), so a message about one tells nobody anything they could not read.
6. **A count message is sent for every `Association` fact that can move a row into or out of a reaction count, or from one reaction to another, on a row pairing a content item with a reaction** — endpoint A a `ContentItem` and endpoint B a `Reaction`, the pair §ARC16.8's predicate counts. A row is counted only once it is `Approved` (§ARC16.8, *Which rows are counted*, rule 2), so six facts can move one:
   - `-Approved` brings a row in.
   - `-Rejected` and `-Submitted` take an approved row out: an administrator's override, or the approval workflow returning a changed reaction's round (§EVN18 rule 1).
   - `-Removed`, `-HardRemoved` included, takes a row out.
   - `-Restored` brings a withdrawn row back at the status it was withdrawn at (§DOM4.10 rule 8), with no `-Approved` behind it (§ARC16.2.2).
   - `-Repointed` moves a row from one reaction to another (§ARC16.2.2).
7. **The other `Association` facts send nothing, because none can move a count.** `-Added` creates a row at `Submitted` (§ARC16.8.1), which no count includes. Under the seeded tier the row's approval runs inside that same publish (§EVN11) and publishes the `-Approved` of rule 6 once it has committed, so no message goes out before the row can be counted. `-Modified` moves a row only between `Draft` and `Submitted` (§ARC12.3.1 shared rule 9's note on `Association`). `-Sorted` and `-ConfidenceSet` change no term a count reads. `-Scoped` would change which group a row counts under, but nothing reaches set-scope: no route serves it, and nothing publishes `Association-SettingScope`. Whether set-scope may change a personal row at all is still open (§DOM4.6), and the design that opens it adds its ear.
8. **A count message names the canonically visible version of the reaction's host, and only when one exists and its winning setting shows reactions.** The host is the row's `EntityAGroupId`, the content item's version group. Its canonically visible version is the one §SEC14.1 admits. A group has at most one, because it has at most one published, non-deleted version (`IX_ContentItem_IsPublished`). That version's winning setting is §DOM6.4's, asked through the `IAccessBroker` arm the reaction summary asks (§ARC16.8, *Where each shared rule lives*, the §DOM6.10 row). A group with no such version, a version whose setting turns `ShowReactions` off, and a setting that cannot be resolved all send nothing: no card shows counts for that group, and no reader may read the change (§SEC14.3 rules 4 and 6, §SEC14.8 rules 1 and 3). That version is the only id a card can show counts for, because the summary answers only for a canonically visible version (§ARC16.8, the near-end row of *Which rows are counted*). So naming it names exactly the cards whose counts may have moved.
9. **The connection is SignalR, and the host runs one instance** (owner rulings 2026-10-06; §ARC12.12 rules 1 and 5).
10. **Every open page reads again what a message makes stale, and a connection that drops and comes back makes it read again everything a message could have covered** (§UI20.10 rules 2 and 4). The reader is told nothing about the connection (owner ruling 2026-10-06).
11. **A live update adds to a page's refresh, and replaces none of it** (§UI20.10 rule 3). The writer's own browser still reads again after its own write, and the engagement hook's rule 8 still decides which read ends a reader's overlay (`UI/Hooks/ContentItemEngagement.md §2` rule 8).

## User stories

The feature's own user stories, bottom up. Each names this document as its parent.

| User story | Level |
| --- | --- |
| [Backend/Models/LiveUpdate.md](Backend/Models/LiveUpdate.md) | model — `LiveUpdate` and `LiveUpdateType` |
| [Backend/Brokers/LiveUpdateBroker.md](Backend/Brokers/LiveUpdateBroker.md) | broker — `ILiveUpdateBroker`, declared in Core and implemented in the host |
| [Backend/Foundations/LiveUpdateService.md](Backend/Foundations/LiveUpdateService.md) | foundation — `ILiveUpdateService` |
| [Backend/Foundations/ContentItemService.LiveUpdates.md](Backend/Foundations/ContentItemService.LiveUpdates.md) | foundation — `IContentItemService`, the canonically visible version of a group |
| [Backend/Orchestrations/LiveUpdateOrchestrationService.md](Backend/Orchestrations/LiveUpdateOrchestrationService.md) | orchestration — `ILiveUpdateOrchestrationService`, the forwarder |
| [Backend/Hubs/LiveUpdatesHub.md](Backend/Hubs/LiveUpdatesHub.md) | exposer — `LiveUpdatesHub` |
| [UI/Brokers/LiveUpdateBroker.md](UI/Brokers/LiveUpdateBroker.md) | broker — `LiveUpdateBroker` (React) |
| [UI/Foundations/LiveUpdateService.md](UI/Foundations/LiveUpdateService.md) | foundation service — `liveUpdateService` (React) |

The rest of the work is recorded in the documents that own it:

| Where it is recorded | What it is |
| --- | --- |
| `UI.md` §UI20.10 item 1 | `Root` opens the tab's one connection |
| `UI/Pages/Home.md §6 item 11`, `UI/Pages/Posts.md §6 item 11`, `UI/Pages/PostDetail.md §6 item 14`, `UI/Pages/MyPosts.md §6 item 10`, `UI/Pages/MyPostDetail.md §6 item 16`, `UI/Pages/ContentItemModerationPage.md §6 item 10`, `UI/Pages/ContentItemModerationDetailPage.md §6 item 16` | each page's share of rule 1, which needs no page code (§UI20.10 rule 2) |
| `Likes.md` rule 12 | rule 2, which needs no page code either |
| `Architecture.md` §ARC12.12 rules 5 and 6 | the deploy job switches Web sockets on for `g2h-dev` and refuses to deploy to more than one instance (#914) |

**Why the forwarder sits on an orchestration.** A count message spans two entities: the `LiveUpdate` it sends, and the `ContentItem` whose canonically visible version it names. That is an orchestration's definition (§ARC12.1 rule 2). The `ContentItemSetting` it asks for the host's winning setting arrives through `IAccessBroker`, and leaves the count where it is (§APR8.6.1 rule 3), as it does for the reaction summary read (§ARC16.8, *Why the read sits on the orchestration*). Its two service dependencies are both foundation services, `IContentItemService` and `ILiveUpdateService`, so they are the same kind and within two-to-three.

## Entity count, events and storage

**Entity count.** Two, `LiveUpdate` and `ContentItem`, with `ContentItemSetting` gathered through a broker and left out of the count, as above.

**Events.** No new event address and no new fact: a live update is caused only by a fact a service already publishes (§EVN26 rule 1). Nine new subscriptions, all on `LiveUpdateOrchestrationService`:

| Address | Its subscribers before this feature |
| --- | --- |
| `ContentItemSetting-Added`, `-Modified`, `-Removed` | none |
| `Association-Approved`, `-Rejected`, `-Removed`, `-Restored` | none; the approval workflow deliberately does not hear `-Restored` (§ARC16.2.2) |
| `Association-Submitted`, `-Repointed` | `ApprovalOrchestrationService`'s ears, beside which the forwarder relies on no order (§EVN26 rule 5) |

**Storage and migration.** None: no table, column, index, migration or seed, and no role.

**Dependencies.** Two packages, `@microsoft/signalr` in the React app, granted in `UI/Brokers/LiveUpdateBroker.md`, and `Microsoft.AspNetCore.SignalR.Client` in the WebApp acceptance suite, granted in `Backend/Hubs/LiveUpdatesHub.md`. The host takes none, and no hosted service is added (§ARC12.12 rule 4).

## Risks

**Reversible.** Everything here is code. The hub's route, its client method and the message's shape are the app's own contract, and the React app ships with the host. `LiveUpdateType` crosses the wire as a number, so its members are appended and never renumbered. A page older than the host ignores a type it does not know (`UI/Foundations/LiveUpdateService.md §1`).

**Not reversible.** Nothing: no event name, no schema and no stored data.

**The send is part of the writer's request, and no reader can hold it.** A publish dispatches inline (§EVN11), so the forwarder's reads and its send run inside the request that wrote the row. A count message costs one content item read and one settings gather. SignalR's send completes only once every connection has taken the message, and a connection that has stopped reading holds it until SignalR closes that connection, after `TransportSendTimeout`, 10 seconds by default. Anyone may open such a connection. So the broker starts the send and does not wait for it (`Backend/Brokers/LiveUpdateBroker.md §1` rule 2): a reader who is reading has the message at once, the writer's request goes on, and the stalled connection is SignalR's to close. The hub's acceptance tests prove it (`Backend/Hubs/LiveUpdatesHub.md §1` rule 5).

**A message can be lost.** A forward that fails is contained (§EVN11), and so is a host restart or a dropped connection. Nothing retries a message (§EVN23 rule 6). The page catches up when its connection comes back (rule 10), or on the query library's own triggers.

**A change between a page's first reads and its tab's first connection is missed** until the page's next trigger, because a first start that succeeds at once makes nothing stale (`UI/Foundations/LiveUpdateService.md §1`). Navigating between pages keeps the tab's connection, so this window opens only when the app loads.

**An open connection holds memory on the host,** and nothing limits how many one client opens. The connection accepts nothing from a reader (§SEC14.8 rule 6), so a connection can only listen. Limiting connections per client is out of scope.

**A second instance would split the audience without saying so** (§ARC12.12 rule 5), so the deploy job refuses to deploy to more than one (#914). An app scaled out between two deploys is caught by the next.

**Failure midway.** The forwarder writes nothing, so there is no midway. A failure after its reads and before its send loses that one message and nothing else.

## Out of scope

- Pushing the review round. Its 15-second refresh stays (owner ruling 2026-10-06), and the comment in `src/hooks/useApprovalRoundChanges.ts` that keeps its seam for a later SignalR channel stays true.
- Pushing an item's own changes and a list's membership: approval, amendment, unpublication and removal, and a new item appearing.
- A change no fact announces, such as a `PublishDate` passing (§EVN26 rule 1).
- Comments, Save and Share. None is built (§UI20.6.6 rule 4), and their live updates come with them.
- The reaction vocabulary changing — a reaction approved, renamed or removed — which reaches a page on a reload.
- `Association-Scoped` (rule 7).
- Any identity on the connection (§SEC14.8 rule 5).
- A backplane between instances (§ARC12.12 rule 5).
- Limiting connections per client.
- Telling the reader that the connection is down (owner ruling 2026-10-06).
- Any client other than the React app, such as the Expo client (#465).

## Open questions

None. The owner ruled on all five on 2026-10-06, choosing in each case the option put to them as recommended: settings and Like counts as what is pushed, SignalR as the transport, one instance for the host, the React broker's departure approved, and nothing shown to the reader while the connection is down. QA's first round on the design PR raised two more, and the owner ruled on both on 2026-10-07, again choosing the option recommended: the React hub broker follows the app's layout (`UI/Brokers/LiveUpdateBroker.md`), and the deploy job switches Web sockets on and refuses a second instance (§ARC12.12 rules 5 and 6).

## Deviations

None at the feature level. `UI/Brokers/LiveUpdateBroker.md` records one, approved by the owner on 2026-10-06.
