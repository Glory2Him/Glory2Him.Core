import { PostView } from "../models/coreUI/postView";
import { PagedPosts, PostQuery } from "../models/posts/pagedPosts";
import ApiBroker from "./apiBroker";

class PostBroker {
    relativePostsUrl = '/api/posts';
    private apiBroker: ApiBroker = new ApiBroker();

    async GetPostsAsync(query: PostQuery): Promise<PagedPosts> {
        const parameters = new URLSearchParams();

        if (query.q) parameters.set('q', query.q);
        if (query.category) parameters.set('category', query.category);
        if (query.tag) parameters.set('tag', query.tag);
        if (query.author) parameters.set('author', query.author);
        if (query.page) parameters.set('page', String(query.page));
        if (query.pageSize) parameters.set('pageSize', String(query.pageSize));

        const queryString = parameters.toString();
        const url = queryString ? `${this.relativePostsUrl}?${queryString}` : this.relativePostsUrl;
        const result = await this.apiBroker.GetAsync(url);

        return result.data as PagedPosts;
    }

    async GetPostBySlugAsync(slug: string): Promise<PostView> {
        const url = `${this.relativePostsUrl}/slug/${encodeURIComponent(slug)}`;
        const result = await this.apiBroker.GetAsync(url);

        return result.data as PostView;
    }

    async GetPostByIdAsync(id: string): Promise<PostView> {
        const url = `${this.relativePostsUrl}/${id}`;
        const result = await this.apiBroker.GetAsync(url);

        return result.data as PostView;
    }

    async CreatePostAsync(post: PostView): Promise<PostView> {
        const result = await this.apiBroker.PostAsync(this.relativePostsUrl, post);

        return result.data as PostView;
    }

    async UpdatePostAsync(post: PostView): Promise<PostView> {
        const url = `${this.relativePostsUrl}/${post.id}`;
        const result = await this.apiBroker.PutAsync(url, post);

        return result.data as PostView;
    }

    async DeletePostAsync(id: string): Promise<void> {
        const url = `${this.relativePostsUrl}/${id}`;
        await this.apiBroker.DeleteAsync(url);
    }
}

export default PostBroker;
