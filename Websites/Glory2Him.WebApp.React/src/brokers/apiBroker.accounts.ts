import { CurrentUser } from "../models/accounts/currentUser";
import { LoginRequest } from "../models/accounts/loginRequest";
import { LoginResult, TwoFactorLoginResult } from "../models/accounts/loginResult";
import ApiBroker from "./apiBroker";

class AccountBroker {
    relativeAccountsUrl = '/api/accounts';
    private apiBroker: ApiBroker = new ApiBroker();

    async GetCurrentUserAsync(): Promise<CurrentUser> {
        const url = `${this.relativeAccountsUrl}/me`;
        const result = await this.apiBroker.GetAsync(url);
        return new CurrentUser(result.data);
    }

    async LoginAsync(loginRequest: LoginRequest): Promise<LoginResult> {
        const url = `${this.relativeAccountsUrl}/login`;
        const result = await this.apiBroker.PostAsync(url, loginRequest);
        return new LoginResult(result.data);
    }

    async LoginWith2faAsync(
        twoFactorCode: string,
        rememberMachine: boolean,
        rememberMe: boolean): Promise<TwoFactorLoginResult> {
        const url = `${this.relativeAccountsUrl}/login-2fa`;

        const result = await this.apiBroker.PostAsync(
            url, { twoFactorCode, rememberMachine, rememberMe });

        return new TwoFactorLoginResult(result.data);
    }

    async LoginWithRecoveryCodeAsync(recoveryCode: string): Promise<TwoFactorLoginResult> {
        const url = `${this.relativeAccountsUrl}/login-recovery-code`;
        const result = await this.apiBroker.PostAsync(url, { recoveryCode });
        return new TwoFactorLoginResult(result.data);
    }

    async ResendEmailConfirmationAsync(email: string): Promise<void> {
        const url = `${this.relativeAccountsUrl}/resend-email-confirmation`;
        await this.apiBroker.PostAsync(url, { email });
    }

    async ConfirmEmailAsync(userId: string, code: string): Promise<{ message: string }> {
        const url = `${this.relativeAccountsUrl}/confirm-email`;
        const result = await this.apiBroker.PostAsync(url, { userId, code });
        return result.data as { message: string };
    }

    async ConfirmEmailChangeAsync(
        userId: string,
        email: string,
        code: string): Promise<{ message: string }> {
        const url = `${this.relativeAccountsUrl}/confirm-email-change`;
        const result = await this.apiBroker.PostAsync(url, { userId, email, code });
        return result.data as { message: string };
    }

    async GetRegisterConfirmationAsync(
        email: string,
        returnUrl: string | null): Promise<{ emailConfirmationLink: string | null }> {
        const query = returnUrl != null
            ? `?email=${encodeURIComponent(email)}&returnUrl=${encodeURIComponent(returnUrl)}`
            : `?email=${encodeURIComponent(email)}`;

        const url = `${this.relativeAccountsUrl}/register-confirmation${query}`;
        const result = await this.apiBroker.GetAsync(url);
        return result.data as { emailConfirmationLink: string | null };
    }

    async LogoutAsync(): Promise<void> {
        const url = `${this.relativeAccountsUrl}/logout`;
        await this.apiBroker.PostAsync(url, {});
    }

    async ChangePasswordAsync(oldPassword: string, newPassword: string): Promise<void> {
        const url = `${this.relativeAccountsUrl}/change-password`;
        await this.apiBroker.PostAsync(url, { oldPassword, newPassword });
    }

    async ForgotPasswordAsync(email: string): Promise<void> {
        const url = `${this.relativeAccountsUrl}/forgot-password`;
        await this.apiBroker.PostAsync(url, { email });
    }

    async ResetPasswordAsync(email: string, code: string, password: string): Promise<void> {
        const url = `${this.relativeAccountsUrl}/reset-password`;
        await this.apiBroker.PostAsync(url, { email, code, password });
    }
}

export default AccountBroker;
