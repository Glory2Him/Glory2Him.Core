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
