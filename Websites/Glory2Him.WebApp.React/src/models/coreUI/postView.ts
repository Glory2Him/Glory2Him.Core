export interface PostView {
    id: string;
    title: string;
    slug: string;
    excerpt: string;
    imageUrl: string;
    category: string;
    categoryBadgeCss: string;
    authorName: string;
    authorImageUrl: string;
    publishedDate: Date;
    readMinutes: number;
    isFeatured: boolean;
    tags: ReadonlyArray<string>;
}
