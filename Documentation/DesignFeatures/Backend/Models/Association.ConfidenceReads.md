# Association model — confidence reads
Parent: [ConfidenceReads.md](../../ConfidenceReads.md)
Level: model — `Association` (`Glory2Him.Core/Models/Foundations/Associations/Association.cs`). Its other user story is `Backend/Models/Association.md`, the Likes feature's.
Inherits: §DOM4.6, ConfidenceReads.md rule 9, *What ships together* step 5

An association's withheld answer is a copy of the row storage returned, or, on the collection read, a projection of it (ConfidenceReads.md rule 9), and it must carry every property but the four as stored. Two of them cannot be carried today: the endpoints' computed effective ids, whose setters are private to the model, so neither a copy nor a projection written in `AssociationService` can set them. This section makes them `internal`, ahead of every section of `Backend/Foundations/AssociationService.ConfidenceReads.md`.

## 1. EntityAEffectiveId and EntityBEffectiveId — set by Core on a copy (#1275)

```csharp
[JsonInclude]
public Guid EntityAEffectiveId { get; internal set; }

[JsonInclude]
public Guid EntityBEffectiveId { get; internal set; }
```

1. **Both setters are `internal` in place of `private`**, so that `AssociationService` can set them on a copy of a row it has read, or in a collection read's projection, and a withheld association carries both as the database computed them. `internal` opens them to Core and to the assemblies Core names as its friends (`InternalsVisibleTo`, `Glory2Him.Core.csproj`): its three test projects, `Glory2Him.WebApp` and its two test projects, and `DynamicProxyGenAssembly2`. Each keeps its `[JsonInclude]`, which the substrate's round trip needs (its comment in `Association.cs`), and the round trip still carries both (`EventPayloadWireRoundTripTests`).
2. **The database still computes both, and no write sends them.** Each is a persisted computed column (`StorageBroker.Association.Configurations.cs`, `HasComputedColumnSql`), so storage never writes a value Core sets, and a value a caller sends is ignored as it is today: `[JsonInclude]` already reads both from a payload. Making the setters `internal` moves no boundary: the computed column is the guarantee, and the private setter was not. **§DOM4.6 holds of the column**, read-only to application code: whoever sets a value on a row in memory, no write sends it.
3. **No migration.** Both are configured explicitly, so the model the storage broker builds does not change, and neither does its snapshot.
4. **Their summary stays true, in words its task fixes.** *"Computed and persisted by the database, never written by application code"* still holds of every write, and says that storage ignores whatever a row in memory holds, and that the setter is `internal` so that a copy of a row Core has read can carry the value the database computed.
