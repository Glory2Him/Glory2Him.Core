import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import ProfileBroker from "../../brokers/apiBroker.profile";
import { ProfileView, UpdateProfileRequest } from "../../models/profile/profileView";

const invalidateProfile = (queryClient: ReturnType<typeof useQueryClient>) => {
    queryClient.invalidateQueries({ queryKey: ["ProfileGetMine"] });
    queryClient.invalidateQueries({ queryKey: ["AccountsGetCurrentUser"] });
};

export const profileService = {
    useGetMyProfile: () => {
        const profileBroker = new ProfileBroker();

        return useQuery<ProfileView>({
            queryKey: ["ProfileGetMine"],
            queryFn: async () => await profileBroker.GetMyProfileAsync()
        });
    },

    useUpdateMyProfile: () => {
        const profileBroker = new ProfileBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async (request: UpdateProfileRequest) =>
                await profileBroker.UpdateMyProfileAsync(request),
            onSuccess: () => invalidateProfile(queryClient)
        });
    },

    useUploadProfileImage: () => {
        const profileBroker = new ProfileBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async (file: File) => await profileBroker.UploadProfileImageAsync(file),
            onSuccess: () => invalidateProfile(queryClient)
        });
    },

    useDeleteProfileImage: () => {
        const profileBroker = new ProfileBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async () => await profileBroker.DeleteProfileImageAsync(),
            onSuccess: () => invalidateProfile(queryClient)
        });
    }
};
