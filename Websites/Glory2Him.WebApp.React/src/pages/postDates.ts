import { PostView } from '../models/coreUI/postView';

// The API serialises publishedDate as an ISO string; the CoreUI components expect a Date.
// Revive it once at the page boundary so everything below works with real dates.
export const withParsedDate = (post: PostView): PostView => ({
    ...post,
    publishedDate: new Date(post.publishedDate as unknown as string),
});

export const withParsedDates = (posts: ReadonlyArray<PostView>): PostView[] =>
    posts.map(withParsedDate);
