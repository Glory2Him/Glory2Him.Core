import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import AccountBroker from "../../brokers/apiBroker.accounts";
import { CurrentUser } from "../../models/accounts/currentUser";
import { LoginRequest } from "../../models/accounts/loginRequest";
import { LoginResult, TwoFactorLoginResult } from "../../models/accounts/loginResult";

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
            onSuccess: (loginResult: LoginResult) => {
                if (loginResult.currentUser != null) {
                    queryClient.setQueryData(
                        ["AccountsGetCurrentUser"], loginResult.currentUser);
                }
            }
        });
    },

    useLoginWith2fa: () => {
        const accountBroker = new AccountBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async (input: {
                twoFactorCode: string,
                rememberMachine: boolean,
                rememberMe: boolean
            }) =>
                await accountBroker.LoginWith2faAsync(
                    input.twoFactorCode, input.rememberMachine, input.rememberMe),
            onSuccess: (loginResult: TwoFactorLoginResult) => {
                if (loginResult.currentUser != null) {
                    queryClient.setQueryData(
                        ["AccountsGetCurrentUser"], loginResult.currentUser);
                }
            }
        });
    },

    useLoginWithRecoveryCode: () => {
        const accountBroker = new AccountBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async (recoveryCode: string) =>
                await accountBroker.LoginWithRecoveryCodeAsync(recoveryCode),
            onSuccess: (loginResult: TwoFactorLoginResult) => {
                if (loginResult.currentUser != null) {
                    queryClient.setQueryData(
                        ["AccountsGetCurrentUser"], loginResult.currentUser);
                }
            }
        });
    },

    useResendEmailConfirmation: () => {
        const accountBroker = new AccountBroker();

        return useMutation({
            mutationFn: async (email: string) =>
                await accountBroker.ResendEmailConfirmationAsync(email)
        });
    },

    useConfirmEmail: () => {
        const accountBroker = new AccountBroker();

        return useMutation({
            mutationFn: async (input: { userId: string, code: string }) =>
                await accountBroker.ConfirmEmailAsync(input.userId, input.code)
        });
    },

    useConfirmEmailChange: () => {
        const accountBroker = new AccountBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async (input: { userId: string, email: string, code: string }) =>
                await accountBroker.ConfirmEmailChangeAsync(
                    input.userId, input.email, input.code),
            onSuccess: () => {
                queryClient.invalidateQueries({ queryKey: ["AccountsGetCurrentUser"] });
            }
        });
    },

    useGetRegisterConfirmation: (email: string | null, returnUrl: string | null) => {
        const accountBroker = new AccountBroker();

        return useQuery({
            queryKey: ["AccountsGetRegisterConfirmation", email, returnUrl],
            queryFn: async () =>
                await accountBroker.GetRegisterConfirmationAsync(email!, returnUrl),
            enabled: email != null,
            retry: false
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
