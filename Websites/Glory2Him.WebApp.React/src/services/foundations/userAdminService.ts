import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import UserAdminBroker from "../../brokers/apiBroker.userAdmin";
import { UpdateUserRequest, UserView } from "../../models/admin/userView";

const invalidateUser = (queryClient: ReturnType<typeof useQueryClient>, userId: string) => {
    queryClient.invalidateQueries({ queryKey: ["AdminUsersGetAll"] });
    queryClient.invalidateQueries({ queryKey: ["AdminUsersGetById", userId] });
};

export const userAdminService = {
    useGetAllUsers: () => {
        const userAdminBroker = new UserAdminBroker();

        return useQuery<UserView[]>({
            queryKey: ["AdminUsersGetAll"],
            queryFn: async () => await userAdminBroker.GetAllUsersAsync()
        });
    },

    useGetAllRoles: () => {
        const userAdminBroker = new UserAdminBroker();

        return useQuery<string[]>({
            queryKey: ["AdminRolesGetAll"],
            queryFn: async () => await userAdminBroker.GetAllRolesAsync(),
            staleTime: Infinity
        });
    },

    useGetUserById: (userId: string) => {
        const userAdminBroker = new UserAdminBroker();

        return useQuery<UserView>({
            queryKey: ["AdminUsersGetById", userId],
            queryFn: async () => await userAdminBroker.GetUserByIdAsync(userId)
        });
    },

    useUpdateUser: () => {
        const userAdminBroker = new UserAdminBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async (input: { userId: string, request: UpdateUserRequest }) =>
                await userAdminBroker.UpdateUserAsync(input.userId, input.request),
            onSuccess: (_, input) => invalidateUser(queryClient, input.userId)
        });
    },

    useSetUserRole: () => {
        const userAdminBroker = new UserAdminBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async (input: { userId: string, roleName: string, isInRole: boolean }) =>
                await userAdminBroker.SetUserRoleAsync(input.userId, input.roleName, input.isInRole),
            onSuccess: (_, input) => invalidateUser(queryClient, input.userId)
        });
    },

    useConfirmEmail: () => {
        const userAdminBroker = new UserAdminBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async (userId: string) => await userAdminBroker.ConfirmEmailAsync(userId),
            onSuccess: (_, userId) => invalidateUser(queryClient, userId)
        });
    },

    useSetLockedOut: () => {
        const userAdminBroker = new UserAdminBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async (input: { userId: string, isLockedOut: boolean }) =>
                await userAdminBroker.SetLockedOutAsync(input.userId, input.isLockedOut),
            onSuccess: (_, input) => invalidateUser(queryClient, input.userId)
        });
    },

    useResetFailedCount: () => {
        const userAdminBroker = new UserAdminBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async (userId: string) => await userAdminBroker.ResetFailedCountAsync(userId),
            onSuccess: (_, userId) => invalidateUser(queryClient, userId)
        });
    },

    useSetTwoFactor: () => {
        const userAdminBroker = new UserAdminBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async (input: { userId: string, isEnabled: boolean }) =>
                await userAdminBroker.SetTwoFactorAsync(input.userId, input.isEnabled),
            onSuccess: (_, input) => invalidateUser(queryClient, input.userId)
        });
    },

    useSetDisabled: () => {
        const userAdminBroker = new UserAdminBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async (input: { userId: string, isDisabled: boolean }) =>
                await userAdminBroker.SetDisabledAsync(input.userId, input.isDisabled),
            onSuccess: (_, input) => invalidateUser(queryClient, input.userId)
        });
    },

    useGetConfirmationLink: () => {
        const userAdminBroker = new UserAdminBroker();

        return useMutation({
            mutationFn: async (userId: string) => await userAdminBroker.GetConfirmationLinkAsync(userId)
        });
    },

    useGetPasswordResetLink: () => {
        const userAdminBroker = new UserAdminBroker();

        return useMutation({
            mutationFn: async (userId: string) => await userAdminBroker.GetPasswordResetLinkAsync(userId)
        });
    },

    useDeleteUser: () => {
        const userAdminBroker = new UserAdminBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async (userId: string) => await userAdminBroker.DeleteUserAsync(userId),
            onSuccess: () => queryClient.invalidateQueries({ queryKey: ["AdminUsersGetAll"] })
        });
    }
};
