import { ContentType } from "./contentType";

// Wire shape of GET /api/contentitemsettings — the Core foundation entity, camelCased by the
// host's default System.Text.Json policy. ContentType serializes as its numeric enum value (no
// JsonStringEnumConverter is registered on the host), which is what the ContentType enum here
// is numbered to match.
//
// The audit fields round-trip on a PUT but are not caller-editable: the foundation stamps
// UpdatedBy/UpdatedWhen from the envelope's SecurityContext and restores CreatedBy/CreatedWhen
// from the stored row before it validates, so whatever a caller sends in them is overwritten.
// They are carried on the model because the admin detail page shows them and because a PUT
// sends the whole entity back.
export type ContentItemSetting = {
    id: string;
    contentType: ContentType;
    contentItemId: string | null;
    contentTypeName: string;
    contentTypeDescription: string;
    contentTypeIconCssClass: string;

    // The order this type is presented in wherever the types are listed — the contribute page's
    // type picker above all. Lower first, and the server rejects a negative. New rows default to
    // 1000, past every value the seed curates, so an unordered type lands after the ordered ones.
    sortOrder: number;

    hasTitle: boolean;
    hasAuthor: boolean;
    isAvailableAsGeneralUserContribution: boolean;

    // The per-type field ceilings, each optional — null or absent means no limit. The form
    // enforces them client-side (maxLength on the input, and a refusal for a seeded value
    // already over a lowered ceiling); the server re-validates as always.
    maxTitleLength?: number | null;
    maxAuthorLength?: number | null;
    maxContentLength?: number | null;
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
    createdBy: string;
    createdWhen: string;
    updatedBy: string;
    updatedWhen: string;
    deletedBy: string | null;
    deletedWhen: string | null;
    isDeleted: boolean;
    deletionReason: string | null;
};
