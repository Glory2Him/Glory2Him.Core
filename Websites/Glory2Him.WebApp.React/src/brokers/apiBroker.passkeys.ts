import { CurrentUser } from "../models/accounts/currentUser";
import { ExternalLoginsView, ExternalProvider } from "../models/accounts/externalLogins";
import { PasskeyInfo } from "../models/passkeys/passkeyInfo";
import ApiBroker from "./apiBroker";

class PasskeyBroker {
    relativePasskeysUrl = '/api/passkeys';
    relativeAccountsUrl = '/api/accounts';
    private apiBroker: ApiBroker = new ApiBroker();

    // Mirrors Blazor's /Account/PasskeyCreationOptions endpoint.
    async GetCreationOptionsAsync(): Promise<unknown> {
        const url = `${this.relativePasskeysUrl}/creation-options`;
        const result = await this.apiBroker.PostAsync(url, {});
        return result.data;
    }

    // Mirrors Blazor's /Account/PasskeyRequestOptions endpoint.
    async GetRequestOptionsAsync(username: string): Promise<unknown> {
        const url = `${this.relativePasskeysUrl}/request-options` +
            (username.length > 0 ? `?username=${encodeURIComponent(username)}` : '');

        const result = await this.apiBroker.PostAsync(url, {});
        return result.data;
    }

    async RegisterPasskeyAsync(credentialJson: string): Promise<string> {
        const url = `${this.relativePasskeysUrl}/register`;
        const result = await this.apiBroker.PostAsync(url, { credentialJson });
        return (result.data as { credentialId: string }).credentialId;
    }

    async PasskeyLoginAsync(credentialJson: string): Promise<CurrentUser> {
        const url = `${this.relativePasskeysUrl}/login`;
        const result = await this.apiBroker.PostAsync(url, { credentialJson });
        return new CurrentUser(result.data);
    }

    async GetPasskeysAsync(): Promise<PasskeyInfo[]> {
        const result = await this.apiBroker.GetAsync(this.relativePasskeysUrl);
        return result.data as PasskeyInfo[];
    }

    async RenamePasskeyAsync(credentialId: string, name: string): Promise<void> {
        const url = `${this.relativePasskeysUrl}/${encodeURIComponent(credentialId)}`;
        await this.apiBroker.PutAsync(url, { name });
    }

    async DeletePasskeyAsync(credentialId: string): Promise<void> {
        const url = `${this.relativePasskeysUrl}/${encodeURIComponent(credentialId)}`;
        await this.apiBroker.DeleteAsync(url);
    }

    async GetExternalProvidersAsync(): Promise<ExternalProvider[]> {
        const url = `${this.relativeAccountsUrl}/external-providers`;
        const result = await this.apiBroker.GetAsync(url);
        return result.data as ExternalProvider[];
    }

    async GetExternalLoginsAsync(): Promise<ExternalLoginsView> {
        const url = `${this.relativeAccountsUrl}/external-logins`;
        const result = await this.apiBroker.GetAsync(url);
        return result.data as ExternalLoginsView;
    }

    async RemoveExternalLoginAsync(loginProvider: string, providerKey: string): Promise<void> {
        const url = `${this.relativeAccountsUrl}/external-logins/remove`;
        await this.apiBroker.PostAsync(url, { loginProvider, providerKey });
    }
}

export default PasskeyBroker;
