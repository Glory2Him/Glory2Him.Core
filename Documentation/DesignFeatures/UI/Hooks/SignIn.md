# Sign-in action
Parent: [Likes.md](../../Likes.md)
Level: hook — `useSignIn` (`Websites/Glory2Him.WebApp.React/src/hooks/useSignIn.ts`), new
Inherits: §UI20.6.6 rule 2, `UI/Pages/Home.md §6 item 3`, Likes.md rule 4

§UI20.6.6 rule 2 calls for one reusable action that sends a reader to sign in and returns them to exactly where they were. `UI/Pages/Home.md §6 item 3` records the five places that compose the sign-in route from the path alone. The Likes feature needs it first, for a signed-out reader who chooses a reaction (Likes.md rule 4), so this user story builds the action and its plain return. The invitation's return on to `/posts/contribute` is built with its first user (`UI/Pages/Home.md §6 item 10`), and the five existing places move onto the action under their own gaps.

## 1. useSignIn (#737)

```ts
useSignIn(): () => void
```

1. **Calling what it returns sends the reader to sign in**: it navigates to `/Account/Login?returnUrl=<path + search + hash>`, with the path, query and fragment they left encoded as one query value.
2. **The return is the sign-in page's own, relied on as it stands.** The page navigates to `returnUrl` where it begins with `/` (`src/pages/account/login.tsx`, line 70), hands it on as `ReturnUrl` when a second factor is asked (line 62), and external sign-in posts it as `ReturnUrl` (`src/pages/account/externalLoginPicker.tsx`, line 38). No test covers that return today, and pinning it is the sign-in page's work, not this hook's.
3. **It decides nothing about who is sent.** Never sending a reader whose sign-in state is still being read is the caller's rule (§UI20.6.6 rule 2; `UI/Hooks/ContentItemEngagement.md §2`).
