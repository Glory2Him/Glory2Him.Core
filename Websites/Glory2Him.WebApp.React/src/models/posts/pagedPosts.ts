import { PostView } from "../coreUI/postView";

export interface PagedPosts {
    items: PostView[];
    totalCount: number;
    page: number;
    pageSize: number;
    totalPages: number;
}

export interface PostQuery {
    q?: string;
    category?: string;
    tag?: string;
    author?: string;
    page?: number;
    pageSize?: number;
}
