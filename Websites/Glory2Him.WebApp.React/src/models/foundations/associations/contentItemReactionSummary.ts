// §ARC16.8's projection in the entity's names, as the server sends it. Renaming into the view's
// label and glyph is the view's job (§ARC16.8, The projection), so these are not the view's
// ContentItemReactionCount.
export interface ContentItemReactionCountRow {
    reactionId: string;
    name: string;
    unicodeEmoji: string;
    count: number;
}

export interface ContentItemReactionSummary {
    contentItemId: string;
    reactions: ContentItemReactionCountRow[];
    viewerReactionId: string | null;
    viewerReactionName: string | null;
}
