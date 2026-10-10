# Passkey service (React)
Parent: [BrokersHoldNoLogic.md](../../BrokersHoldNoLogic.md)
Level: foundation service — `passkeyService` (`Websites/Glory2Him.WebApp.React/src/services/foundations/passkeyService.ts`), existing
Inherits: §UI20.9.2 (*Outside this section*), §UI20.9.3 rules 4 and 5, `UI/Brokers/PasskeyBroker.md`

The React hooks over `PasskeyBroker` and the browser's WebAuthn ceremony. `usePasskeySignIn` runs the sign-in ceremony through `requestPasskeyAndSignInAsync` in `src/hooks/usePasskeys.ts`, which asks the broker for the request options. Today the ceremony trims the email it is handed, and the broker leaves an empty one out (`BrokersHoldNoLogic.md` rule 2). Under §UI20.9.3 rule 5 that decision is this hook's, and the ceremony hands on what it is given. How this hook and `useAddPasskey` depart from the services skill is #833's, and nothing here records or approves it (§UI20.9.2, *Outside this section*).

## 1. usePasskeySignIn (#957)

```ts
usePasskeySignIn: () => UseMutationResult<CurrentUser, unknown, string>
```

1. **It asks for the request options for the email the reader typed, trimmed.**
2. **An email that is blank once trimmed is no username**, and it hands none on, so the request carries no `username`, as today when the reader typed nothing.
3. **The ceremony hands the username it is given to `PostRequestOptionsAsync` unchanged** (`UI/Brokers/PasskeyBroker.md §1`). `requestPasskeyAndSignInAsync` takes `username: string | undefined` in place of the email, and neither trims nor tests it (§UI20.9.3 rule 5). The rest of the ceremony, and its errors, are unchanged.
4. **What it writes to the cache on success is unchanged.**
