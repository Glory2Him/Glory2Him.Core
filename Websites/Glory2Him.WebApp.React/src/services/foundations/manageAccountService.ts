import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import ManageAccountBroker from "../../brokers/apiBroker.manageAccounts";
import {
    AuthenticatorSetup,
    EmailInfo,
    PersonalDataInfo,
    TwoFactorInfo
} from "../../models/accounts/manageAccount";

export const manageAccountService = {
    useGetEmailInfo: () => {
        const manageAccountBroker = new ManageAccountBroker();

        return useQuery<EmailInfo>({
            queryKey: ["ManageGetEmailInfo"],
            queryFn: async () => await manageAccountBroker.GetEmailInfoAsync()
        });
    },

    useChangeEmail: () => {
        const manageAccountBroker = new ManageAccountBroker();

        return useMutation({
            mutationFn: async (newEmail: string) =>
                await manageAccountBroker.ChangeEmailAsync(newEmail)
        });
    },

    useSendVerificationEmail: () => {
        const manageAccountBroker = new ManageAccountBroker();

        return useMutation({
            mutationFn: async () =>
                await manageAccountBroker.SendVerificationEmailAsync()
        });
    },

    useGetTwoFactorInfo: () => {
        const manageAccountBroker = new ManageAccountBroker();

        return useQuery<TwoFactorInfo>({
            queryKey: ["ManageGetTwoFactorInfo"],
            queryFn: async () => await manageAccountBroker.GetTwoFactorInfoAsync()
        });
    },

    useGetAuthenticatorSetup: () => {
        const manageAccountBroker = new ManageAccountBroker();

        return useQuery<AuthenticatorSetup>({
            queryKey: ["ManageGetAuthenticatorSetup"],
            queryFn: async () => await manageAccountBroker.GetAuthenticatorSetupAsync()
        });
    },

    useGetAuthenticatorQrCode: () => {
        const manageAccountBroker = new ManageAccountBroker();

        return useQuery<string>({
            queryKey: ["ManageGetAuthenticatorQrCode"],
            queryFn: async () =>
                await manageAccountBroker.GetAuthenticatorQrCodeSvgAsync()
        });
    },

    useVerifyAuthenticator: () => {
        const manageAccountBroker = new ManageAccountBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async (code: string) =>
                await manageAccountBroker.VerifyAuthenticatorAsync(code),
            onSuccess: () => {
                queryClient.invalidateQueries({ queryKey: ["ManageGetTwoFactorInfo"] });
            }
        });
    },

    useDisable2fa: () => {
        const manageAccountBroker = new ManageAccountBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async () => await manageAccountBroker.Disable2faAsync(),
            onSuccess: () => {
                queryClient.invalidateQueries({ queryKey: ["ManageGetTwoFactorInfo"] });
            }
        });
    },

    useGenerateRecoveryCodes: () => {
        const manageAccountBroker = new ManageAccountBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async () =>
                await manageAccountBroker.GenerateRecoveryCodesAsync(),
            onSuccess: () => {
                queryClient.invalidateQueries({ queryKey: ["ManageGetTwoFactorInfo"] });
            }
        });
    },

    useResetAuthenticator: () => {
        const manageAccountBroker = new ManageAccountBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async () => await manageAccountBroker.ResetAuthenticatorAsync(),
            onSuccess: () => {
                queryClient.invalidateQueries({ queryKey: ["ManageGetTwoFactorInfo"] });

                queryClient.invalidateQueries({
                    queryKey: ["ManageGetAuthenticatorSetup"]
                });

                queryClient.invalidateQueries({
                    queryKey: ["ManageGetAuthenticatorQrCode"]
                });
            }
        });
    },

    useForgetBrowser: () => {
        const manageAccountBroker = new ManageAccountBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async () => await manageAccountBroker.ForgetBrowserAsync(),
            onSuccess: () => {
                queryClient.invalidateQueries({ queryKey: ["ManageGetTwoFactorInfo"] });
            }
        });
    },

    useGetPersonalDataInfo: () => {
        const manageAccountBroker = new ManageAccountBroker();

        return useQuery<PersonalDataInfo>({
            queryKey: ["ManageGetPersonalDataInfo"],
            queryFn: async () => await manageAccountBroker.GetPersonalDataInfoAsync()
        });
    },

    useDeletePersonalData: () => {
        const manageAccountBroker = new ManageAccountBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async (password: string) =>
                await manageAccountBroker.DeletePersonalDataAsync(password),
            onSuccess: () => {
                queryClient.invalidateQueries({ queryKey: ["AccountsGetCurrentUser"] });
            }
        });
    }
};
