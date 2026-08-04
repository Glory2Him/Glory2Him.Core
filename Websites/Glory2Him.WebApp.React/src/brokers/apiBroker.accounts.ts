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
}

export default AccountBroker;
