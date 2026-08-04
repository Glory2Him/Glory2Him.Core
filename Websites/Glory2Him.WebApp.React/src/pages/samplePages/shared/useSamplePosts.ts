import { PostView } from '../../../models/coreUI/postView';
import { postService } from '../../../services/foundations/postService';
import { withParsedDates } from '../../postDates';

// Shared loader for the layout demos, ported from the Blazor SamplePageBase: the usual posts
// fetch plus the slicing helpers the layouts need, so each sample page stays markup and
// nothing else.
export interface SamplePosts {
    posts: ReadonlyArray<PostView>;
    lead: PostView | null;
    afterLead: ReadonlyArray<PostView>;
    isLoading: boolean;
    isError: boolean;

    // The demo store holds only a handful of posts, but a masonry or four-across grid needs
    // more tiles than that to read as a real layout — repeat the set to fill the shape rather
    // than shipping a half-empty grid.
    fill: (count: number) => ReadonlyArray<PostView>;
    take: (count: number) => PostView[];
}

export const useSamplePosts = (): SamplePosts => {
    const { data, isLoading, isError } = postService.useGetPosts({});
    const posts = data == null ? [] : withParsedDates(data.items);

    const fill = (count: number): ReadonlyArray<PostView> => {
        if (posts.length === 0 || count <= 0) {
            return [];
        }

        return Array.from(
            { length: count },
            (_, index) => posts[index % posts.length]);
    };

    const take = (count: number): PostView[] =>
        posts.slice(0, count);

    return {
        posts,
        lead: posts.length > 0 ? posts[0] : null,
        afterLead: posts.slice(1),
        isLoading,
        isError,
        fill,
        take,
    };
};
