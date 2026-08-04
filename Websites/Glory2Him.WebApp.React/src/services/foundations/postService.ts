import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import PostBroker from "../../brokers/apiBroker.posts";
import { PostView } from "../../models/coreUI/postView";
import { PagedPosts, PostQuery } from "../../models/posts/pagedPosts";

export const postService = {
    useGetPosts: (query: PostQuery) => {
        const postBroker = new PostBroker();

        return useQuery<PagedPosts>({
            queryKey: ["PostsGetAll", query],
            queryFn: async () => await postBroker.GetPostsAsync(query),
            staleTime: 60 * 1000
        });
    },

    useGetPostBySlug: (slug: string) => {
        const postBroker = new PostBroker();

        return useQuery<PostView>({
            queryKey: ["PostsGetBySlug", slug],
            queryFn: async () => await postBroker.GetPostBySlugAsync(slug),
            staleTime: 60 * 1000
        });
    },

    useGetPostById: (id: string, enabled = true) => {
        const postBroker = new PostBroker();

        return useQuery<PostView>({
            queryKey: ["PostsGetById", id],
            queryFn: async () => await postBroker.GetPostByIdAsync(id),
            enabled,
            staleTime: 60 * 1000
        });
    },

    useCreatePost: () => {
        const postBroker = new PostBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async (post: PostView) => await postBroker.CreatePostAsync(post),
            onSuccess: () => queryClient.invalidateQueries({ queryKey: ["PostsGetAll"] })
        });
    },

    useUpdatePost: () => {
        const postBroker = new PostBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async (post: PostView) => await postBroker.UpdatePostAsync(post),
            onSuccess: (_, post) => {
                queryClient.invalidateQueries({ queryKey: ["PostsGetAll"] });
                queryClient.invalidateQueries({ queryKey: ["PostsGetById", post.id] });
            }
        });
    },

    useDeletePost: () => {
        const postBroker = new PostBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async (id: string) => await postBroker.DeletePostAsync(id),
            onSuccess: () => queryClient.invalidateQueries({ queryKey: ["PostsGetAll"] })
        });
    }
};
