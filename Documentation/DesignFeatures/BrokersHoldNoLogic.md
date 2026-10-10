# Brokers hold no logic
Parent: [UI.md §UI20.9](../Design/UI.md), services and brokers, designed before feature documents
Inherits: §UI20.3 rule 5, §UI20.9.1 (*Scope*, departures 2 and 6), §UI20.9.2 departure 2, §UI20.9.3, §ARC12.2.1, `the-standard-reacttypescript-brokers` tsr-brokers-008
Mockups: none. A reader sees one change: the Like control offers the reactions in the vocabulary's order (rule 6). Every other page asks the server for what it asks for today.
Design task: #814

No React broker holds logic. Each one sends what it is handed, converted to the wire's format, and hands back what came (tsr-brokers-008; §UI20.9.3 rule 1). This sub-feature moves the logic some brokers hold today into the foundation services above them, and into one new foundation service for the connectivity check, under the owner's ruling of 2026-10-01 that logic in a broker is a defect (§UI20.9.1, *Scope*).

## Problem

When #814 designed this (2026-10-10, at `a00746ad`), fourteen broker members and `ApiBroker`'s connectivity interceptor held logic. They wrote query conditions, orders and pages of their own, built filter clauses from what they were handed, split a read into chunks, trimmed what a reader typed and decided when it counted as nothing, and worked out from an answer whether another page followed. None of it is where the repository's rule puts it (CLAUDE.md, *Brokers hold no logic and get no unit tests*). Some of it is tested in broker unit tests that the rule says a broker does not have, and some of it is not tested at all, because a broker test is only written where someone chose to write one. The Like control's order (Likes.md rule 5a) was about to add one more condition to a broker (#752). This is the state the sub-feature set out from, and it is not kept current as tasks merge.

## Business rules

1. **No React broker member holds logic, by §UI20.9.3 rule 1's test.** What each member sends is fixed by its route and what it is handed, and what it returns is the answer as the wire carries it.
2. **Every member that held logic at `a00746ad` is named here, and so is where its logic goes.** Each was measured against §UI20.9.3 rules 1 and 4, member by member, across every file in `src/brokers/` (`Websites/Glory2Him.WebApp.React/src/brokers/`):

   | Broker and member | Its logic | Where the logic goes |
   | --- | --- | --- |
   | `ReactionBroker.GetApprovedReactionsAsync` (`apiBroker.reactions.ts:13-19`) | a fixed `$filter` | `reactionService.useGetApprovedReactions` |
   | `ContentItemBroker.SearchContentItemsAsync`, with its private `GetFeedPageAsync` and `GetSearchPageAsync` (`apiBroker.contentItems.ts:104-210`) | the route chosen by the query's scope; a `$filter` clause built from each field it is handed, the term and the submitter trimmed, the term and the author lower-cased, quotes doubled, the type and the statuses written by member name; a fixed `$orderby`; `$skip` and `$top`, or the feed's `skip` and `take`, worked out from the page, one row beyond it; the answer cut to the page, and whether another page follows | `contentItemService.useSearchContentItems` |
   | `ContentItemBroker.DeleteContentItemByIdAsync` (`apiBroker.contentItems.ts:65-78`) | trims the reason, and leaves a blank one out | `contentItemService.useRemoveContentItem` |
   | `ContentItemSettingBroker.GetAvailableForContributionAsync` (`apiBroker.contentItemSettings.ts:25-31`) | a fixed `$filter` | `contentItemSettingService.useGetAvailableForContribution` |
   | `ContentItemSettingBroker.GetDefaultsAsync` (`apiBroker.contentItemSettings.ts:37-43`) | a fixed `$filter` | `contentItemSettingService.useGetDefaults` and `useGetEffectiveSettingsFor` |
   | `ContentItemSettingBroker.GetOverridesForContentItemsAsync` (`apiBroker.contentItemSettings.ts:56-81`) | asks nothing for no ids; splits the ids into chunks of 12, one request each; a `$filter` built from each chunk; the answers merged | `contentItemSettingService.useGetEffectiveSettingsFor` |
   | `ContentItemSettingBroker.GetContentItemSettingsAsync` (`apiBroker.contentItemSettings.ts:90-133`) | a `$filter` clause built from each field it is handed, the term trimmed and lower-cased, quotes doubled; a fixed `$orderby`; `$skip` and `$top` worked out from the page, one row beyond it; the answer cut to the page, and whether another page follows | `contentItemSettingService.useGetContentItemSettings` |
   | `ApprovalBroker.GetApprovalReviewsAsync` (`apiBroker.approvals.ts:46-52`) | a `$filter` built from the approval id, with a fixed clause | `approvalService.useGetApprovalReviews` |
   | `ApprovalBroker.PostApprovalDecisionAsync` (`apiBroker.approvals.ts:129-152`) | trims the bypass reason, and leaves a blank one out | `approvalService.useDecideApproval` |
   | `ApprovalCommentBroker.GetApprovalCommentsAsync` (`apiBroker.approvalComments.ts:27-33`) | a `$filter` built from the approval id, with a fixed clause | `approvalCommentService.useGetApprovalComments` |
   | `ApprovalSettingBroker.GetApprovalSettingsAsync` (`apiBroker.approvalSettings.ts:20-33`) | a fixed `$filter` and a fixed `$orderby` | `approvalSettingService.useGetApprovalSettings` |
   | `ApprovalSettingBroker.RemoveApprovalSettingByIdAsync` (`apiBroker.approvalSettings.ts:65-78`) | trims the reason, and leaves a blank one out | `approvalSettingService.useRemoveApprovalSetting` |
   | `PasskeyBroker.GetRequestOptionsAsync` (`apiBroker.passkeys.ts:19-25`) | leaves an empty username out | `passkeyService.usePasskeySignIn` (§UI20.9.3 rule 5) |
   | `PostBroker.GetPostsAsync` (`apiBroker.posts.ts:9-24`) | leaves out each field that is an empty string or `0`, not only one that is absent | `postService.useGetPosts` |
   | `ApiBroker`'s connectivity interceptor (`apiBroker.ts:18-52`) | which responses speak for this origin, and what a failure means | `networkStatusService.useApiReachability`, new (§UI20.9.3 rule 7) |

3. **Every other member holds no logic, by the same test.** The ones nearest the line were measured and cleared under §UI20.9.3 rule 4: `AccountBroker.GetRegisterConfirmationAsync` leaves `returnUrl` out only when it is `null`; `AIReviewerBroker.DeleteAIReviewerAsync` reads the empty answer to a `204` as `null`; `RegistrationBroker.GetEmailInUseAsync` and `GetUsernameSuggestionsAsync`, `PasskeyBroker.RegisterPasskeyAsync`, and `UserAdminBroker.GetConfirmationLinkAsync` and `GetPasswordResetLinkAsync` return the one field the wire wraps their answer in; `AssociationBroker.DeleteAssociationPairAsync` and `GetReactionSummariesAsync`, and `ProfileBroker.UploadProfileImageAsync`, write what they are handed in the wire's form. `AccountBroker`'s members and `PasskeyBroker.PasskeyLoginAsync` hand the body to a wire model's constructor, which is the model's (§UI20.9.3 rule 1). The toast broker's functions hand their message on, and `ToastBroker.Container` sets the options of the component it renders, as `ApiBroker` sets `withCredentials` on every request: neither is a value on a request.
4. **What reaches the server does not change, except the vocabulary's order.** Each read asks the same route for the same parameters, but for the reaction vocabulary, which also asks for `$orderby=sortOrder,name` (rule 6), and each parameter decodes to the value it decodes to today. The text on the wire may differ where a new member encodes with `URLSearchParams` and the old one with `encodeURIComponent` (a space as `+` rather than `%20`), which the server decodes alike. Each service hook keeps its query key, its stale time and its other options, so nothing that invalidates or reads those keys, such as the live update service (`UI/Foundations/LiveUpdateService.md`), sees a change.
5. **Every test file under `src/brokers/` has a fate** (§UI20.9.3 rule 8). A case moves to the service's tests with the service task that takes its logic over, and leaves the broker's file with the task that deletes the old member. A case that pins format conversion is deleted with its broker's clean-up task. Whichever task removes a file's last case deletes the file.

   | Test file | Its cases and their fate |
   | --- | --- |
   | `apiBroker.aiReviewers.test.ts` | all five, the `it.each` case included, pin format conversion: deleted (`UI/Brokers/AIReviewerBroker.md §1`) |
   | `apiBroker.approvalComments.test.ts` | *the thread by approval* moves to `approvalCommentService.test.tsx` (`UI/Foundations/ApprovalCommentService.md §1`) and leaves with the old member (`UI/Brokers/ApprovalCommentBroker.md §2`); the other four, the `it.each` case included, pin format conversion: deleted (`UI/Brokers/ApprovalCommentBroker.md §3`) |
   | `apiBroker.approvalSettings.test.ts` | *reading the set*, two cases, moves to `approvalSettingService.test.tsx` (`UI/Foundations/ApprovalSettingService.md §1`) and leaves with its old member (`UI/Brokers/ApprovalSettingBroker.md §3`); *closing one*, two cases, the same (`UI/Foundations/ApprovalSettingService.md §2`, `UI/Brokers/ApprovalSettingBroker.md §4`); *creating one* and *amending one*, three cases, pin format conversion: deleted (`UI/Brokers/ApprovalSettingBroker.md §5`) |
   | `apiBroker.approvals.test.ts` | *the reviews by approval* moves to `approvalService.test.tsx` (`UI/Foundations/ApprovalService.md §1`) and leaves with its old member (`UI/Brokers/ApprovalBroker.md §3`); the two decision cases the same (`UI/Foundations/ApprovalService.md §2`, `UI/Brokers/ApprovalBroker.md §4`); the other six pin format conversion: deleted (`UI/Brokers/ApprovalBroker.md §5`) |
   | `apiBroker.contentItemSettings.test.ts` | all four pin `GetOverridesForContentItemsAsync`'s logic: they move to `contentItemSettingService.test.tsx` (`UI/Foundations/ContentItemSettingService.md §3`) and leave with the old member (`UI/Brokers/ContentItemSettingBroker.md §4`) |
   | `apiBroker.contentItems.test.ts` | every case pins `SearchContentItemsAsync`'s logic: they move to `contentItemService.test.tsx` (`UI/Foundations/ContentItemService.md §1`) and leave with the old member (`UI/Brokers/ContentItemBroker.md §5`) |
   | `apiBroker.networkStatus.test.ts` | the seven cases on what a response or a failure means move to `networkStatusService.test.tsx` (`UI/Foundations/NetworkStatusService.md §1`); the eighth, *should wire both functions into axios as its response interceptor*, pins the way in that `ApiBroker` keeps, and is deleted; the file goes with the interceptor (`UI/Brokers/ApiBroker.md §2`) |
   | `apiBroker.globals.test.ts` | stays: `apiBroker.globals.ts` is not a broker (§UI20.9.1, *Scope*), and nothing here renames it |

6. **The Like control's order is asked for where the vocabulary's condition now lives.** Likes.md rule 5a orders the vocabulary by `sortOrder`, then `name`. #752 was to add that order to `ReactionBroker`. Under §UI20.9.3 rule 2 it is `reactionService.useGetApprovedReactions`'s, beside the filter that moves there (`UI/Foundations/ReactionService.md §1`), and #752 is that task.

## User stories

This sub-feature's own user stories, bottom up. Each names this document as its parent.

| User story | Level |
| --- | --- |
| [UI/Brokers/ApiBroker.md](UI/Brokers/ApiBroker.md) | broker — `ApiBroker` |
| [UI/Brokers/ReactionBroker.md](UI/Brokers/ReactionBroker.md) | broker — `ReactionBroker` |
| [UI/Brokers/ContentItemBroker.md](UI/Brokers/ContentItemBroker.md) | broker — `ContentItemBroker` |
| [UI/Brokers/ContentItemSettingBroker.md](UI/Brokers/ContentItemSettingBroker.md) | broker — `ContentItemSettingBroker` |
| [UI/Brokers/ApprovalBroker.md](UI/Brokers/ApprovalBroker.md) | broker — `ApprovalBroker` |
| [UI/Brokers/ApprovalCommentBroker.md](UI/Brokers/ApprovalCommentBroker.md) | broker — `ApprovalCommentBroker` |
| [UI/Brokers/ApprovalSettingBroker.md](UI/Brokers/ApprovalSettingBroker.md) | broker — `ApprovalSettingBroker` |
| [UI/Brokers/AIReviewerBroker.md](UI/Brokers/AIReviewerBroker.md) | broker — `AIReviewerBroker` |
| [UI/Brokers/PasskeyBroker.md](UI/Brokers/PasskeyBroker.md) | broker — `PasskeyBroker` |
| [UI/Brokers/PostBroker.md](UI/Brokers/PostBroker.md) | broker — `PostBroker` |
| [UI/Foundations/NetworkStatusService.md](UI/Foundations/NetworkStatusService.md) | foundation service — `networkStatusService`, new |
| [UI/Foundations/ReactionService.md](UI/Foundations/ReactionService.md) | foundation service — `reactionService` |
| [UI/Foundations/ContentItemService.md](UI/Foundations/ContentItemService.md) | foundation service — `contentItemService` |
| [UI/Foundations/ContentItemSettingService.md](UI/Foundations/ContentItemSettingService.md) | foundation service — `contentItemSettingService` |
| [UI/Foundations/ApprovalService.md](UI/Foundations/ApprovalService.md) | foundation service — `approvalService` |
| [UI/Foundations/ApprovalCommentService.md](UI/Foundations/ApprovalCommentService.md) | foundation service — `approvalCommentService` |
| [UI/Foundations/ApprovalSettingService.md](UI/Foundations/ApprovalSettingService.md) | foundation service — `approvalSettingService` |
| [UI/Foundations/PasskeyService.md](UI/Foundations/PasskeyService.md) | foundation service — `passkeyService` |
| [UI/Foundations/PostService.md](UI/Foundations/PostService.md) | foundation service — `postService` |
| [UI/Hooks/OnlineStatus.md](UI/Hooks/OnlineStatus.md) | hook — `useOnlineStatus` |

**Build order.** Each move goes broker member, then service, then the old member's deletion (§UI20.9.3 rule 6), and the connectivity check broker member, service, hook, deletion (§UI20.9.3 rule 7). The moves are independent of one another, except that two service tasks that change the same service file, or two broker tasks that change the same broker file, are best built one after the other. A broker's clean-up task depends on nothing.

**Why the logic sits in the foundation services** is §UI20.9.3 rule 2's, and is not restated. **Why the connectivity check gets a service of its own:** no existing service wraps `ApiBroker`, and each foundation service wraps exactly one broker (tsr-services-001), so the check cannot join one.

## Entity count, events and storage

**Entity count.** None changes. Each service already reads or writes its one entity through its one broker, and the move changes where a request is decided, not what it asks for. The new `networkStatusService` reads no entity: it hears the responses `ApiBroker` already receives.

**Events.** None. The window events `apiBroker.ts` raises today, `g2h-network-reachable` and `g2h-network-unreachable`, are not domain events, and they go (§UI20.9.3 rule 7).

**Storage.** None.

## Risks

- **Everything is reversible.** No migration, no schema, no event and no route changes. Each task changes the React app alone.
- **A caller of an old member left behind fails the build** at the deletion task, which the type check catches (`npm run build` type-checks both `tsconfig` projects). The deletion task names every caller it expects to find gone.
- **A test that mocks a broker module asserts on its members.** `useApprovalRound.test.tsx` and `approvalCommentService.test.tsx` mock `ApprovalBroker` and `ApprovalCommentBroker`, and assert that the old read members were asked for an approval id, or were not asked at all while the read is gated. Once the service calls the new member, an assertion that the old member was asked fails, and one that it was not asked passes whatever the hook does, so a read sent with no approval id would go unnoticed. A service task that calls a new member moves every such assertion to the new member: the filter for the id, and nothing asked while gated. Each service task names the assertions it moves.
- **The wire text may change, its values may not** (rule 4). A test that compared a URL character for character moves to the service, where it compares the `ODataQuery` the service hands the broker, not a URL.

## Out of scope

- **Logic in a wire model's constructor** (`CurrentUser`, `LoginResult`, `TwoFactorLoginResult`), which is the model's (§UI20.9.3 rule 1).
- **Renaming an existing member.** Departure 2 keeps every existing name, and §UI20.9.3 rule 3 names only the new members.
- **Where `src/hooks/usePasskeys.ts` stands, and how the passkey hooks depart from the services skill** (#833).
- **Modules outside `src/services/` that import a broker** for something other than the passkey request and the connectivity events: the toast broker's functions, which pages and hooks call, and `ManageAccountBroker`'s download address, which `personalData.tsx` reads. Neither is logic in a broker, and where those modules may call a broker is not this sub-feature's question.
- **The server.** Every route keeps its contract.

## Open questions

None.
