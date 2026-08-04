import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import PasskeyBroker from '../../brokers/apiBroker.passkeys';
import { CurrentUser } from '../../models/accounts/currentUser';
import { ExternalLoginsView, ExternalProvider } from '../../models/accounts/externalLogins';
import { PasskeyInfo } from '../../models/passkeys/passkeyInfo';
import {
    createAndRegisterPasskeyAsync,
    requestPasskeyAndSignInAsync
} from '../../hooks/usePasskeys';

export const passkeyService = {
    useGetPasskeys: () => {
        const passkeyBroker = new PasskeyBroker();

        return useQuery<PasskeyInfo[]>({
            queryKey: ["PasskeysGetAll"],
            queryFn: async () => await passkeyBroker.GetPasskeysAsync()
        });
    },

    // Runs the full "Add a new passkey" flow: creation options → WebAuthn
    // create ceremony → register. Resolves to the new passkey's credential id.
    useAddPasskey: () => {
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async () => await createAndRegisterPasskeyAsync(),
            onSuccess: () => {
                queryClient.invalidateQueries({ queryKey: ["PasskeysGetAll"] });
            }
        });
    },

    useRenamePasskey: () => {
        const passkeyBroker = new PasskeyBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async (input: { credentialId: string, name: string }) =>
                await passkeyBroker.RenamePasskeyAsync(input.credentialId, input.name),
            onSuccess: () => {
                queryClient.invalidateQueries({ queryKey: ["PasskeysGetAll"] });
            }
        });
    },

    useDeletePasskey: () => {
        const passkeyBroker = new PasskeyBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async (credentialId: string) =>
                await passkeyBroker.DeletePasskeyAsync(credentialId),
            onSuccess: () => {
                queryClient.invalidateQueries({ queryKey: ["PasskeysGetAll"] });
            }
        });
    },

    // Runs the full passkey sign-in flow: request options → WebAuthn get
    // ceremony → login. Mirrors accountService.useLogin's cache update.
    usePasskeySignIn: () => {
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async (email: string) => await requestPasskeyAndSignInAsync(email),
            onSuccess: (currentUser: CurrentUser) => {
                queryClient.setQueryData(["AccountsGetCurrentUser"], currentUser);
                queryClient.invalidateQueries({ queryKey: ["AccountsGetCurrentUser"] });
            }
        });
    },

    useGetExternalProviders: () => {
        const passkeyBroker = new PasskeyBroker();

        return useQuery<ExternalProvider[]>({
            queryKey: ["AccountsGetExternalProviders"],
            queryFn: async () => await passkeyBroker.GetExternalProvidersAsync(),
            staleTime: 5 * 60 * 1000
        });
    },

    useGetExternalLogins: () => {
        const passkeyBroker = new PasskeyBroker();

        return useQuery<ExternalLoginsView>({
            queryKey: ["AccountsGetExternalLogins"],
            queryFn: async () => await passkeyBroker.GetExternalLoginsAsync()
        });
    },

    useRemoveExternalLogin: () => {
        const passkeyBroker = new PasskeyBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async (input: { loginProvider: string, providerKey: string }) =>
                await passkeyBroker.RemoveExternalLoginAsync(
                    input.loginProvider, input.providerKey),
            onSuccess: () => {
                queryClient.invalidateQueries({ queryKey: ["AccountsGetExternalLogins"] });
            }
        });
    }
};
