# Return after sign-in
Parent: [UI.md §UI20.8](../../../Design/UI.md), authentication, which was designed before features had documents of their own. §UI20.8.1 lists this user story.
Level: hook — `useSignInReturn` (`Websites/Glory2Him.WebApp.React/src/hooks/useSignInReturn.ts`), new
Inherits: §SEC18.7.1 rule 7, §UI20.8 rule 2, §UI20.8.1

§UI20.8.1 has every way of signing in send the reader on through one shared return, so that §SEC18.7.1 rule 7 is applied in one place. This user story builds that return. The sign-in action, `UI/Hooks/SignIn.md`, sends a reader to sign in carrying their return address, and this hook is where the address is followed. The four places that follow it do so through this hook, built under their own gaps in one pull request, PR #832 (§UI20.8.1 items 1–4).

## 1. useSignInReturn (#780)

```ts
useSignInReturn(): (returnUrl: string | null | undefined) => void
```

1. **Calling what it returns sends the signed-in reader on.** The reader goes to the return address when §SEC18.7.1 rule 7 allows it to be followed, whole, with its path, query and fragment. Otherwise they go to the home page, `/`.
2. **A refused address is not an error.** The hook throws nothing and hands no error to its caller. The reader goes to the home page.
3. **It decides nothing about who is sent on.** It is called once the reader has signed in. Whether they have, and what happens instead when a second factor is needed or the account is locked out, is the calling page's decision (§UI20.8.1).
