export interface CommentEntry {
    authorName: string;
    authorImageUrl?: string;
    postedAt: Date;
    body: string;
    reactions: number;
    isReply?: boolean;
}
