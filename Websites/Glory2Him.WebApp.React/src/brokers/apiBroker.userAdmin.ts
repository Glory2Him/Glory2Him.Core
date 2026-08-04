import { UpdateUserRequest, UserView } from "../models/admin/userView";
import ApiBroker from "./apiBroker";

class UserAdminBroker {
    relativeUsersUrl = '/api/admin/users';
    private apiBroker: ApiBroker = new ApiBroker();

    async GetAllUsersAsync(): Promise<UserView[]> {
        const result = await this.apiBroker.GetAsync(this.relativeUsersUrl);

        return result.data as UserView[];
    }

    async GetAllRolesAsync(): Promise<string[]> {
        const result = await this.apiBroker.GetAsync(`${this.relativeUsersUrl}/roles`);

        return result.data as string[];
    }

    async GetUserByIdAsync(userId: string): Promise<UserView> {
        const result = await this.apiBroker.GetAsync(`${this.relativeUsersUrl}/${userId}`);

        return result.data as UserView;
    }

    async UpdateUserAsync(userId: string, updateUserRequest: UpdateUserRequest): Promise<void> {
        await this.apiBroker.PutAsync(`${this.relativeUsersUrl}/${userId}`, updateUserRequest);
    }

    async SetUserRoleAsync(userId: string, roleName: string, isInRole: boolean): Promise<void> {
        await this.apiBroker.PostAsync(`${this.relativeUsersUrl}/${userId}/roles`, { roleName, isInRole });
    }

    async ConfirmEmailAsync(userId: string): Promise<void> {
        await this.apiBroker.PostAsync(`${this.relativeUsersUrl}/${userId}/confirm-email`, {});
    }

    async SetLockedOutAsync(userId: string, isLockedOut: boolean): Promise<void> {
        await this.apiBroker.PostAsync(`${this.relativeUsersUrl}/${userId}/locked-out`, { isLockedOut });
    }

    async ResetFailedCountAsync(userId: string): Promise<void> {
        await this.apiBroker.PostAsync(`${this.relativeUsersUrl}/${userId}/reset-failed-count`, {});
    }

    async SetTwoFactorAsync(userId: string, isEnabled: boolean): Promise<void> {
        await this.apiBroker.PostAsync(`${this.relativeUsersUrl}/${userId}/two-factor`, { isEnabled });
    }

    async SetDisabledAsync(userId: string, isDisabled: boolean): Promise<void> {
        await this.apiBroker.PostAsync(`${this.relativeUsersUrl}/${userId}/disabled`, { isDisabled });
    }

    async GetConfirmationLinkAsync(userId: string): Promise<string> {
        const result = await this.apiBroker.PostAsync(`${this.relativeUsersUrl}/${userId}/confirmation-link`, {});

        return (result.data as { link: string }).link;
    }

    async GetPasswordResetLinkAsync(userId: string): Promise<string> {
        const result = await this.apiBroker.PostAsync(`${this.relativeUsersUrl}/${userId}/password-reset-link`, {});

        return (result.data as { link: string }).link;
    }

    async DeleteUserAsync(userId: string): Promise<void> {
        await this.apiBroker.DeleteAsync(`${this.relativeUsersUrl}/${userId}`);
    }
}

export default UserAdminBroker;
