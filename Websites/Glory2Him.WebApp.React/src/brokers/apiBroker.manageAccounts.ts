import {
    AuthenticatorSetup,
    EmailInfo,
    GenerateRecoveryCodesResult,
    PersonalDataInfo,
    TwoFactorInfo,
    VerifyAuthenticatorResult
} from "../models/accounts/manageAccount";
import ApiBroker from "./apiBroker";

class ManageAccountBroker {
    relativeManageUrl = '/api/manage';
    relativeAccountsUrl = '/api/accounts';
    private apiBroker: ApiBroker = new ApiBroker();

    // The personal-data download is a plain browser navigation so the JSON file
    // arrives with its Content-Disposition attachment header, like the Blazor form post.
    personalDataDownloadUrl = `${this.relativeManageUrl}/personal-data/download`;

    async GetEmailInfoAsync(): Promise<EmailInfo> {
        const url = `${this.relativeManageUrl}/email`;
        const result = await this.apiBroker.GetAsync(url);
        return result.data as EmailInfo;
    }

    async ChangeEmailAsync(newEmail: string): Promise<{ message: string }> {
        const url = `${this.relativeManageUrl}/email/change`;
        const result = await this.apiBroker.PostAsync(url, { newEmail });
        return result.data as { message: string };
    }

    async SendVerificationEmailAsync(): Promise<{ message: string }> {
        const url = `${this.relativeManageUrl}/email/send-verification`;
        const result = await this.apiBroker.PostAsync(url, {});
        return result.data as { message: string };
    }

    async GetTwoFactorInfoAsync(): Promise<TwoFactorInfo> {
        const url = `${this.relativeManageUrl}/two-factor`;
        const result = await this.apiBroker.GetAsync(url);
        return result.data as TwoFactorInfo;
    }

    async GetAuthenticatorSetupAsync(): Promise<AuthenticatorSetup> {
        const url = `${this.relativeManageUrl}/two-factor/authenticator`;
        const result = await this.apiBroker.GetAsync(url);
        return result.data as AuthenticatorSetup;
    }

    // The endpoint answers with the same SVG markup QRCoder rendered for the
    // Blazor page; the caller inlines it, so the QR looks identical.
    async GetAuthenticatorQrCodeSvgAsync(): Promise<string> {
        const url = `${this.relativeManageUrl}/two-factor/qr-code`;
        const result = await this.apiBroker.GetAsync(url);
        return result.data as string;
    }

    async VerifyAuthenticatorAsync(code: string): Promise<VerifyAuthenticatorResult> {
        const url = `${this.relativeManageUrl}/two-factor/verify`;
        const result = await this.apiBroker.PostAsync(url, { code });
        return result.data as VerifyAuthenticatorResult;
    }

    async Disable2faAsync(): Promise<void> {
        const url = `${this.relativeManageUrl}/two-factor/disable`;
        await this.apiBroker.PostAsync(url, {});
    }

    async GenerateRecoveryCodesAsync(): Promise<GenerateRecoveryCodesResult> {
        const url = `${this.relativeManageUrl}/two-factor/generate-recovery-codes`;
        const result = await this.apiBroker.PostAsync(url, {});
        return result.data as GenerateRecoveryCodesResult;
    }

    async ResetAuthenticatorAsync(): Promise<void> {
        const url = `${this.relativeManageUrl}/two-factor/reset-authenticator`;
        await this.apiBroker.PostAsync(url, {});
    }

    async ForgetBrowserAsync(): Promise<void> {
        const url = `${this.relativeManageUrl}/two-factor/forget-browser`;
        await this.apiBroker.PostAsync(url, {});
    }

    async GetPersonalDataInfoAsync(): Promise<PersonalDataInfo> {
        const url = `${this.relativeManageUrl}/personal-data`;
        const result = await this.apiBroker.GetAsync(url);
        return result.data as PersonalDataInfo;
    }

    async DeletePersonalDataAsync(password: string): Promise<void> {
        const url = `${this.relativeManageUrl}/delete-personal-data`;
        await this.apiBroker.PostAsync(url, { password });
    }
}

export default ManageAccountBroker;
