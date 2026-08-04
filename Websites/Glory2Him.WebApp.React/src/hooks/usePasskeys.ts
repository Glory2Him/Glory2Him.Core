import PasskeyBroker from '../brokers/apiBroker.passkeys';
import { CurrentUser } from '../models/accounts/currentUser';

// The WebAuthn browser ceremony, ported from Blazor's PasskeySubmit.razor.js.
// The server supplies the options JSON (SignInManager.MakePasskey*OptionsAsync)
// and consumes the credential JSON the browser produces; this module only runs
// the navigator.credentials.create/get ceremony between the two.

// PublicKeyCredential's static JSON helpers are newer than some TS lib.dom
// versions, so they are described locally and feature-detected at runtime.
interface PublicKeyCredentialStatics {
    parseCreationOptionsFromJSON?: (optionsJson: unknown) => PublicKeyCredentialCreationOptions;
    parseRequestOptionsFromJSON?: (optionsJson: unknown) => PublicKeyCredentialRequestOptions;
    isConditionalMediationAvailable?: () => Promise<boolean>;
}

const publicKeyCredentialStatics = (): PublicKeyCredentialStatics | undefined =>
    (window as { PublicKeyCredential?: PublicKeyCredentialStatics }).PublicKeyCredential;

export const browserSupportsPasskeys = (): boolean => {
    const statics = publicKeyCredentialStatics();

    return typeof navigator.credentials !== 'undefined'
        && statics != null
        && typeof statics.parseCreationOptionsFromJSON === 'function'
        && typeof statics.parseRequestOptionsFromJSON === 'function';
};

const unsupportedBrowserMessage =
    'Some passkey features are missing. Please update your browser.';

// Mirrors PasskeySubmit.razor.js: a NotAllowedError becomes the same friendly
// message Blazor showed; a user-cancelled ceremony (AbortError) returns null.
const toPasskeyErrorMessage = (error: unknown): string => {
    if (error instanceof DOMException && error.name === 'NotAllowedError') {
        return 'No passkey was provided by the authenticator.';
    }

    if (error instanceof Error && error.message.length > 0) {
        return error.message;
    }

    return 'The passkey operation failed.';
};

export class PasskeyCeremonyError extends Error { }

const rethrowCeremonyError = (error: unknown): never => {
    if (error instanceof DOMException && error.name === 'AbortError') {
        // The user explicitly canceled the operation.
        throw new PasskeyCeremonyError('');
    }

    throw new PasskeyCeremonyError(toPasskeyErrorMessage(error));
};

// Runs the attestation (create) ceremony and registers the resulting
// credential — the Blazor Manage/Passkeys "Add a new passkey" flow. Returns
// the Base64Url credential id of the new passkey.
export async function createAndRegisterPasskeyAsync(): Promise<string> {
    if (!browserSupportsPasskeys()) {
        throw new PasskeyCeremonyError(unsupportedBrowserMessage);
    }

    const passkeyBroker = new PasskeyBroker();
    const optionsJson = await passkeyBroker.GetCreationOptionsAsync();

    const options = publicKeyCredentialStatics()!
        .parseCreationOptionsFromJSON!(optionsJson);

    let credential: Credential | null;

    try {
        credential = await navigator.credentials.create({ publicKey: options });
    } catch (error) {
        return rethrowCeremonyError(error);
    }

    if (credential == null) {
        throw new PasskeyCeremonyError('The browser did not provide a passkey.');
    }

    return await passkeyBroker.RegisterPasskeyAsync(JSON.stringify(credential));
}

// Runs the assertion (get) ceremony and signs in — the Blazor Login page's
// PasskeySubmit Request flow.
export async function requestPasskeyAndSignInAsync(email: string): Promise<CurrentUser> {
    if (!browserSupportsPasskeys()) {
        throw new PasskeyCeremonyError(unsupportedBrowserMessage);
    }

    const passkeyBroker = new PasskeyBroker();
    const optionsJson = await passkeyBroker.GetRequestOptionsAsync(email.trim());

    const options = publicKeyCredentialStatics()!
        .parseRequestOptionsFromJSON!(optionsJson);

    let credential: Credential | null;

    try {
        credential = await navigator.credentials.get({ publicKey: options });
    } catch (error) {
        return rethrowCeremonyError(error);
    }

    if (credential == null) {
        throw new PasskeyCeremonyError('No passkey was provided by the authenticator.');
    }

    return await passkeyBroker.PasskeyLoginAsync(JSON.stringify(credential));
}
