# Live update orchestration service
Parent: [LiveUpdates.md](../../LiveUpdates.md)
Level: orchestration — `ILiveUpdateOrchestrationService` (`Glory2Him.Core/Services/Orchestrations/LiveUpdates/`), new
Inherits: §ARC12.1 rule 2, §ARC12.5, §APR8.6.1 rule 3, §EVN2 rule 4, §EVN11, §EVN15, §EVN26, §SEC14.6 rule 4, §SEC14.6.1, §SEC14.8 rules 1–3, §ARC16.8 (*Which rows are counted*; *Where each shared rule lives*, the §DOM6.10 row), LiveUpdates.md rules 5–8

The forwarder. It hears the facts that change what an open page shows, and sends the live update that tells the page (§EVN26). A count message spans two entities, the `LiveUpdate` it sends and the `ContentItem` whose canonically visible version it names, so the forwarder is an orchestration (§ARC12.1 rule 2). Its two service dependencies are foundation services, the same kind: `IContentItemService` (`Backend/Foundations/ContentItemService.LiveUpdates.md §1`) and `ILiveUpdateService` (`Backend/Foundations/LiveUpdateService.md §1`). The host's winning setting arrives through `IAccessBroker`, and leaves the count where it is (§APR8.6.1 rule 3), as it does for the reaction summary read (§ARC16.8). Its brokers are the logging broker, `IEnvelopeIntegrityBroker` to verify each fact it hears, and `IAccessBroker` to gather the winning setting. It holds no `IEventBroker`, because it publishes nothing, and no `IEventEnvelopeBroker`, because it mints nothing and reads no caller.

**Every ear has the same shape.** Each is `ValueTask<EventEnvelope<T>?> On<Entity><Verb>Async(EventEnvelope<T> envelope, CancellationToken cancellationToken = default)` and answers `null`, since nothing replies to a fact (§EVN20). Each verifies the fact's signature against the event name its address carries, in the request direction (§EVN15 item 10). Each refuses a null envelope, an envelope with no content and an envelope that does not verify, before it reads anything. It does not drop a fact carrying the system identity (§EVN26 rule 3). Then it hands the verified fact to one of two bodies:

- **The settings body** sends `ContentItemSettingChanged` through `ILiveUpdateService.SendLiveUpdateAsync`. It reads nothing first: every setting row is public-read (§SEC14.7 posture C rule 2).
- **The reactions body** first tests the row. Unless endpoint A is a `ContentItem` and endpoint B a `Reaction` — the pair §ARC16.8's predicate counts — it sends nothing. For that pair it asks `IContentItemService.FindPublicContentItemGroupAsync` for the canonically visible version of the row's `EntityAGroupId`. It then asks `IAccessBroker.RetrieveEffectiveContentItemSettingsAsync` for that version's winning setting, keyed on the version's content type and id. It sends `ContentItemReactionsChanged` naming that version only when the version exists and its winning setting has `ShowReactions` on. A group with no such version, `ShowReactions` off, and a key the gather leaves unanswered all send nothing (§SEC14.8 rules 1 and 3).

**Failures** are mapped, logged and rethrown as the solution's other ears map theirs (`ApprovalOrchestrationService.Substrate.cs`), so the substrate records a failed delivery (§EVN11). Nothing retries it (§EVN23 rule 6). A cancelled token is rethrown unwrapped before anything is read.

**Wiring.** Each ear's subscription is registered in `EventSubscriptionRegistration` under a stable identifier and name (`EventBrokerIdentifiers.LiveUpdateOrchestration.cs`). The service is registered scoped, by hand, in the host's `CoreRegistration.AddCoreServices`. It composes `IContentItemService`, whose envelope broker captures identity, so a singleton would be the §SEC14.6.1 defect, and no `ServiceRegistration.Add*Service()` helper is added for it.

## 1. OnContentItemSettingAddedAsync (#897)

Hears `ContentItemSetting-Added` and runs the settings body. This ear builds the settings body, on the service §4 builds.

## 2. OnContentItemSettingModifiedAsync (#898)

Hears `ContentItemSetting-Modified` and runs the settings body.

## 3. OnContentItemSettingRemovedAsync (#899)

Hears `ContentItemSetting-Removed`, and accepts both names its address carries, `ContentItemSettingRemoved` and `ContentItemSettingHardRemoved` (§EVN2 rule 4). Runs the settings body.

## 4. OnAssociationApprovedAsync (#900)

Hears `Association-Approved` and runs the reactions body. This ear builds the service, with both its foundation services, its wiring and the reactions body, so the forwarder spans its two entities from the task that creates it (§ARC12.1 rule 2). The other ears follow it.

## 5. OnAssociationRejectedAsync (#901)

Hears `Association-Rejected` and runs the reactions body.

## 6. OnAssociationSubmittedAsync (#902)

Hears `Association-Submitted`, the address the approval workflow's ear also hears, and runs the reactions body. Neither relies on which runs first (§EVN26 rule 5).

## 7. OnAssociationRemovedAsync (#903)

Hears `Association-Removed`, and accepts both names its address carries, `AssociationRemoved` and `AssociationHardRemoved` (§EVN2 rule 4). Runs the reactions body.

## 8. OnAssociationRestoredAsync (#904)

Hears `Association-Restored`, which the approval workflow deliberately does not hear (§ARC16.2.2), and runs the reactions body.

## 9. OnAssociationRepointedAsync (#905)

Hears `Association-Repointed`, the address the approval workflow's ear also hears, and runs the reactions body. Neither relies on which runs first (§EVN26 rule 5).

## Deviations

None.
