# Live update
Parent: [LiveUpdates.md](../../LiveUpdates.md)
Level: model — `LiveUpdate` and `LiveUpdateType` (`Glory2Him.Core/Models/Foundations/LiveUpdates/`), new
Inherits: §SEC14.8 rule 1, LiveUpdates.md rules 4, 5 and 8

What the server tells an open page: that a read it may be holding is stale. It carries nothing a page would show (§SEC14.8 rule 1), and the page reads the rest again itself (LiveUpdates.md rule 4). No table stores one and no fact announces one: it is sent and forgotten (§EVN26 rule 4).

## 1. LiveUpdate and LiveUpdateType (#T01)

```csharp
public class LiveUpdate
{
    public LiveUpdateType Type { get; set; }
    public Guid? ContentItemId { get; set; }
}

public enum LiveUpdateType
{
    ContentItemSettingChanged = 1,
    ContentItemReactionsChanged = 2
}
```

1. **`ContentItemSettingChanged` names nothing else.** Some content item setting row changed, and which one does not matter to a page, which reads every setting it shows again (LiveUpdates.md rule 5). Its `ContentItemId` is null.
2. **`ContentItemReactionsChanged` names the version whose counts may have moved**: the canonically visible version of the reaction's host (LiveUpdates.md rule 8). Its `ContentItemId` is that version's id, never the group's, because a card holds a version's id (§ARC16.8, *The projection*, `ContentItemId`).
3. **No member is `0`.** A `LiveUpdate` whose `Type` was never set therefore holds no member, and the foundation refuses it (`Backend/Foundations/LiveUpdateService.md §1`) rather than sending it as a setting message.
4. **It crosses the wire as JSON, its type as a number**, which is SignalR's default: the host registers no string-enum converter, and §ARC16.8.1's suggestion result travels the same way (`UI/Brokers/AssociationBroker.md`). So the members are appended and never renumbered, and the React app mirrors the two numbers (`UI/Brokers/LiveUpdateBroker.md`).
5. **The model holds no rule.** What a message must carry is the foundation's to refuse (`Backend/Foundations/LiveUpdateService.md §1`).
