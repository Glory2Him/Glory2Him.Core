import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import AccountBroker from "../../brokers/apiBroker.accounts";
import { CurrentUser } from "../../models/accounts/currentUser";
import { LoginRequest } from "../../models/accounts/loginRequest";

export const accountService = {
    useGetCurrentUser: () => {
        const accountBroker = new AccountBroker();

        return useQuery<CurrentUser>({
            queryKey: ["AccountsGetCurrentUser"],
            queryFn: async () => await accountBroker.GetCurrentUserAsync(),
            staleTime: 5 * 60 * 1000
        });
    },

    useLogin: () => {
        const accountBroker = new AccountBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async (loginRequest: LoginRequest) =>
                await accountBroker.LoginAsync(loginRequest),
            onSuccess: (currentUser: CurrentUser) => {
                queryClient.setQueryData(["AccountsGetCurrentUser"], currentUser);
            }
        });
    },

    useChangePassword: () => {
        const accountBroker = new AccountBroker();

        return useMutation({
            mutationFn: async (input: { oldPassword: string, newPassword: string }) =>
                await accountBroker.ChangePasswordAsync(input.oldPassword, input.newPassword)
        });
    },

    useForgotPassword: () => {
        const accountBroker = new AccountBroker();

        return useMutation({
            mutationFn: async (email: string) => await accountBroker.ForgotPasswordAsync(email)
        });
    },

    useResetPassword: () => {
        const accountBroker = new AccountBroker();

        return useMutation({
            mutationFn: async (input: { email: string, code: string, password: string }) =>
                await accountBroker.ResetPasswordAsync(input.email, input.code, input.password)
        });
    },

    useLogout: () => {
        const accountBroker = new AccountBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async () => await accountBroker.LogoutAsync(),
            onSuccess: () => {
                queryClient.invalidateQueries({ queryKey: ["AccountsGetCurrentUser"] });
            }
        });
    }
};
