import { CurrentUser } from "../models/accounts/currentUser";
import { LoginRequest } from "../models/accounts/loginRequest";
import ApiBroker from "./apiBroker";

class AccountBroker {
    relativeAccountsUrl = '/api/accounts';
    private apiBroker: ApiBroker = new ApiBroker();

    async GetCurrentUserAsync(): Promise<CurrentUser> {
        const url = `${this.relativeAccountsUrl}/me`;
        const result = await this.apiBroker.GetAsync(url);
        return new CurrentUser(result.data);
    }

    async LoginAsync(loginRequest: LoginRequest): Promise<CurrentUser> {
        const url = `${this.relativeAccountsUrl}/login`;
        const result = await this.apiBroker.PostAsync(url, loginRequest);
        return new CurrentUser(result.data);
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
