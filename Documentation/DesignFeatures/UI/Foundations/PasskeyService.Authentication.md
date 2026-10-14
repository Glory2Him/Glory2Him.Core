# Passkey service — authentication
Parent: [UI.md §UI20.8](../../../Design/UI.md), authentication, which was designed before features had documents of their own. §UI20.8 rule 7 lists this user story.
Level: foundation service — `passkeyService` (`Websites/Glory2Him.WebApp.React/src/services/foundations/passkeyService.ts`), existing. The services-and-brokers sub-feature's work on the same service is `UI/Foundations/PasskeyService.md` (`design.md`, *How the work breaks down*).
Inherits: §UI20.8 rule 7

§UI20.8 rule 7 has a passkey's failure announced once, by the page that ran it. This user story builds that in the two hooks that run the browser's passkey ceremony, `useAddPasskey` and `usePasskeySignIn`: each turns the app's global error toast off, so that what the reader is told of a failure is the page's alone. The shapes in which the ceremony's failures reach the two hooks are `UI/Foundations/PasskeyService.md` *Deviations* 2's, a cancelled ceremony among them, and this user story changes none of them. Nor does it change either page, what either hook does on success, or what the ceremony does (`src/hooks/usePasskeys.ts`).

## 1. useAddPasskey (#1101)

```ts
useAddPasskey: () => UseMutationResult<string, Error, void>
```

1. **It turns the app's global error toast off.** It declares `meta: { suppressGlobalErrorToast: true }`, the opt-out the app's global handler reads (`src/brokers/apiBroker.globals.ts`), so the handler raises no toast for any failure of this hook, whatever its shape.
2. **While the passkeys page is shown, the page alone tells the reader of each failure.** The page that runs this hook is the passkeys page, `/Account/Manage/Passkeys` (`src/pages/account/manage/passkeys.tsx`). It clears its status message (`src/pages/account/statusMessage.tsx`) when the reader asks to add a passkey, and shows each failure there:
   1. a `PasskeyCeremonyError` that carries a message, as `Error: ` followed by that message;
   2. any other failure, a failed `PasskeyBroker` request among them, as `Error: ` followed by the server's message where its answer carries one (`extractApiErrorMessage`), and `Error: The passkey could not be added to your account.` where it carries none.

   This user story changes neither message.
3. **A cancelled ceremony is told as a failed one.** A reader who cancels the browser's passkey prompt is told `Error: No passkey was provided by the authenticator.` by the page (rule 2, its first case), and the toast is off. The browser reports a cancel as a `NotAllowedError`, as it reports a prompt that timed out or that the authenticator refused (W3C Web Authentication, §5.1.3 and §5.1.4.1), so the ceremony cannot tell them apart and gives each the same message (`UI/Foundations/PasskeyService.md` *Deviations* 2). A `PasskeyCeremonyError` with no message, for which the page shows nothing, is raised on an aborted call alone, and nothing in the app aborts one (*Deviations* 2). The ceremony's comments that say a cancel is an `AbortError` (`usePasskeys.ts`, above `toPasskeyErrorMessage` and in `rethrowCeremonyError`) say instead what *Deviations* 2 says.
4. **A failure that lands after the reader has left the passkeys page is announced by nothing.** A reader can follow a link to another page of the app while the ceremony is in flight, such as while one of its requests to the server is. TanStack Query runs the callbacks the page hands to `mutate` only while the page is still rendered (`@tanstack/query-core`, `MutationObserver`), so once the reader has left, the page and its message are gone, and the toast is off. The failure leaves nothing the reader must act on. A failed ceremony, a cancel and a failed request for the creation options add no passkey. A failed registration request may have added one, as one whose answer never arrives, since the endpoint adds the passkey before it answers (`/api/passkeys/register`, `Websites/Glory2Him.WebApp/Infrastructure/PasskeyApiEndpoints.cs`). The passkeys page reads the list afresh each time it is shown (`useGetPasskeys` sets no `staleTime`, so TanStack Query reads it again when the page mounts), so a reader who comes back sees whether it was added.
5. **What it does on success is unchanged.** It invalidates the passkeys list (`PasskeysGetAll`).

## 2. usePasskeySignIn (#1102)

Its signature is `UI/Foundations/PasskeyService.md §1`'s, unchanged.

1. **It turns the app's global error toast off.** It declares `meta: { suppressGlobalErrorToast: true }`, the opt-out the app's global handler reads (`src/brokers/apiBroker.globals.ts`), so the handler raises no toast for any failure of this hook, whatever its shape.
2. **While the sign-in page is shown, the page alone tells the reader of each failure.** The hook runs in the passkey button (`src/pages/account/passkeySignInButton.tsx`), which hands each failure's message to the sign-in page, `/Account/Login` (`src/pages/account/login.tsx`), to show in its status message (`src/pages/account/statusMessage.tsx`):
   1. a `PasskeyCeremonyError` that carries a message, as `Error: ` followed by that message;
   2. any other failure, a failed `PasskeyBroker` request among them, as the server's message where its answer carries one (`extractApiErrorMessage`), and `Error: Invalid login attempt.` where it carries none. Neither of the sign-in's endpoints, `/api/passkeys/request-options` and `/api/passkeys/login`, refuses with a message of its own (`PasskeyApiEndpoints.cs`).

   This user story changes neither message.
3. **A cancelled ceremony is told as a failed one.** A reader who cancels the browser's passkey prompt is told `Error: No passkey was provided by the authenticator.` by the sign-in page (rule 2, its first case), and the toast is off. The browser reports a cancel as it reports a prompt that timed out or was refused, so the ceremony gives each the same message (§1 rule 3). A `PasskeyCeremonyError` with no message, for which the button hands the page no message, so that the page leaves its status message as it was, is raised on an aborted call alone, and nothing in the app aborts one (`UI/Foundations/PasskeyService.md` *Deviations* 2).
4. **A failure that lands after the reader has left the sign-in page is announced by nothing.** A reader can follow a link to another page of the app while the ceremony is in flight, such as while one of its requests to the server is. TanStack Query runs the callbacks the button hands to `mutate` only while the button is still rendered (`@tanstack/query-core`, `MutationObserver`), and the button is rendered only on the sign-in page, so once the reader has left, the page and its message are gone, and the toast is off. The failure leaves nothing the reader must act on: the hook writes who is signed in to the cache on success alone (`UI/Foundations/PasskeyService.md §1` rule 4), so what the app shows of who is signed in is what it showed before the reader tried.
5. **What it does with the email and on success is unchanged** (`UI/Foundations/PasskeyService.md §1`).
