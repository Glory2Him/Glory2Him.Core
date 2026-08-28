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
    createdBy: string;
    createdWhen: string;
    updatedBy: string;
    updatedWhen: string;
    deletedBy: string | null;
    deletedWhen: string | null;
    isDeleted: boolean;
    deletionReason: string | null;
};
