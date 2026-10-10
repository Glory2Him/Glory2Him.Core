# Content item setting broker
Parent: [BrokersHoldNoLogic.md](../../BrokersHoldNoLogic.md)
Level: broker — `ContentItemSettingBroker` (`Websites/Glory2Him.WebApp.React/src/brokers/apiBroker.contentItemSettings.ts`), existing
Inherits: §UI20.9.1, §UI20.9.3, `the-standard-reacttypescript-brokers` as §UI20.9.1 departs from it

The React app's door to `api/ContentItemSettings`. Four of its reads hold logic today, and all four read the same `[EnableQuery]` route (`BrokersHoldNoLogic.md` rule 2): two write a fixed `$filter`, one splits a list of ids into chunks and builds a filter for each, and one builds a filter from a query's fields, orders and pages the read, and cuts the answer to the page. They become one member that sends the query it is handed. `contentItemSettingService` writes each read's query (`UI/Foundations/ContentItemSettingService.md`), and the four old members go (§UI20.9.3 rule 6).

## 1. GetContentItemSettingsByQueryAsync (#937)

```ts
GetContentItemSettingsByQueryAsync(query: ODataQuery): Promise<ContentItemSetting[]>
```

1. **It sends `GET /api/contentitemsettings` with each option of the query it is handed**, under its `$` name, encoded, and none it is not handed (§UI20.9.3 rules 2 and 4).
2. **It returns the rows as they came**, typed as `ContentItemSetting[]`.
3. **Its name carries `ByQuery`** because `GetContentItemSettingsAsync`, the member §5 deletes, holds `GetContentItemSettingsAsync` (§UI20.9.3 rule 3).

## 2. GetAvailableForContributionAsync — deleted (#964)

Deleted once `contentItemSettingService.useGetAvailableForContribution` calls §1 instead (`UI/Foundations/ContentItemSettingService.md §1`) and nothing calls it. No broker test covers it.

## 3. GetDefaultsAsync — deleted (#965)

Deleted once `contentItemSettingService.useGetDefaults` and `useGetEffectiveSettingsFor` both call §1 instead (`UI/Foundations/ContentItemSettingService.md §2` and §3) and nothing calls it. No broker test covers it.

## 4. GetOverridesForContentItemsAsync — deleted (#966)

Deleted once `contentItemSettingService.useGetEffectiveSettingsFor` calls §1 instead (`UI/Foundations/ContentItemSettingService.md §3`) and nothing calls it. `apiBroker.contentItemSettings.test.ts` goes with it: its every case has moved to `contentItemSettingService.test.tsx` (`BrokersHoldNoLogic.md` rule 5).

## 5. GetContentItemSettingsAsync — deleted (#967)

Deleted once `contentItemSettingService.useGetContentItemSettings` calls §1 instead (`UI/Foundations/ContentItemSettingService.md §4`) and nothing calls it. The file's `toODataLiteral`, which only it uses, goes with it. No broker test covers it.
