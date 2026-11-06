# Passkey broker
Parent: [BrokersHoldNoLogic.md](../../BrokersHoldNoLogic.md)
Level: broker — `PasskeyBroker` (`Websites/Glory2Him.WebApp.React/src/brokers/apiBroker.passkeys.ts`), existing
Inherits: §UI20.9.1, §UI20.9.3 rules 4 and 5, `the-standard-reacttypescript-brokers` as §UI20.9.1 departs from it

The React app's door to `api/Passkeys` and the account's external logins. One member holds logic today (`BrokersHoldNoLogic.md` rule 2): `GetRequestOptionsAsync` leaves the username out when it is an empty string, which decides that an empty string is no username. It gains a replacement that sends what it is handed, `passkeyService.usePasskeySignIn` makes the decision (`UI/Foundations/PasskeyService.md §1`; §UI20.9.3 rule 5), and the old member goes (§UI20.9.3 rule 6).

## 1. PostRequestOptionsAsync (#943)

```ts
PostRequestOptionsAsync(username?: string): Promise<unknown>
```

1. **It sends `POST /api/passkeys/request-options` with an empty body, and `username` on the query string, encoded, when it is handed one.** A username that is `undefined` is left out (§UI20.9.3 rule 4). It sends the username it is handed as it is.
2. **It returns the request options as they came**, untyped, for the WebAuthn ceremony to parse, as today.
3. **It is named for the verb it sends**, as departure 2 names every new member, so it does not collide with `GetRequestOptionsAsync` (§UI20.9.3 rule 3).

## 2. GetRequestOptionsAsync — deleted (#973)

Deleted once `requestPasskeyAndSignInAsync` (`src/hooks/usePasskeys.ts`) calls §1 instead (`UI/Foundations/PasskeyService.md §1`) and nothing calls it. No broker test covers it.
