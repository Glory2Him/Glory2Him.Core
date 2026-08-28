// Wire shape of GET /api/contentitemsettings — the Core foundation entity, camelCased by the
// host's default System.Text.Json policy. ContentType serializes as its numeric enum value (no
// JsonStringEnumConverter is registered on the host), so it is typed as a number here rather
// than a string union.
export type ContentItemSetting = {
    id: string;
    contentType: number;
    contentItemId: string | null;
    contentTypeName: string;
    contentTypeDescription: string;
    contentTypeIconCssClass: string;
    hasTitle: boolean;
    hasAuthor: boolean;
    isAvailableAsGeneralUserContribution: boolean;
    tagsAllowed: boolean;
    showTags: boolean;
    reactionsAllowed: boolean;
    showReactions: boolean;
    linksAllowed: boolean;
    showLinks: boolean;
    attachmentsAllowed: boolean;
    showAttachments: boolean;
    commentsAllowed: boolean;
    showComments: boolean;
    bibleReferenceAllowed: boolean;
    showBibleReferences: boolean;
    limitReactionsToLoveOnly: boolean;
};
