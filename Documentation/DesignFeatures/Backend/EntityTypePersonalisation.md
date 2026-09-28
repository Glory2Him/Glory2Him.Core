# Entity type personalisation
Parent: [Likes.md](../Likes.md)
Level: model — `EntityTypePersonalisation` (`Glory2Him.Core/Models/Configurations/`), a declared lookup shaped like `EntityTypeVersioning`
Inherits: §DOM4.2, §DOM4.10 rules 1, 4 and 5

§DOM4.10 rule 4 declares which entity types make an association personal, and forbids answering it any other way: never an inline test against `EntityType.Reaction`, and never a silent default for a member nobody declared. An association is personal where an endpoint's type is personal — today a `Reaction` endpoint (§DOM4.2) — and rule 5's chain starts here: this lookup decides whether `UserId` is set, `UserId` decides `IsPersonal`, and `IsPersonal` decides the approval tier and the unique index that governs the row.

## 1. IsPersonal (#715)

```csharp
public static bool IsPersonal(EntityType entityType);
```

1. **`Reaction` is personal; every other member of `EntityType` is not** (§DOM4.10 rule 4).
2. **A member the lookup does not declare is a hard error** — `NotSupportedException`, naming the member and this lookup — never a `false` default, exactly as `EntityTypeVersioning.IsVersioned` answers an undeclared member (§DOM4.10 rule 4: a silent `false` files a forgotten member as editorial, under the wrong index and the wrong approval tier).
3. **Every member of `EntityType` is declared**, and a test walks the enum to say so, as `EntityTypeVersioningTests` does for its own lookup — so adding a member without a decision fails a test rather than a request.
