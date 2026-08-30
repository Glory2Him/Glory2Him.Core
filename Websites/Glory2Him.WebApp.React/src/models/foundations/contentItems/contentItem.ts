import { ApprovalStatus } from '../../components/associations/associationItem';
import { ShareabilityBasis } from '../../components/contentItems/contentItemFormItem';
import { ContentType } from '../contentItemSettings/contentType';

// Wire shape of api/ContentItems — the Core foundation entity, camelCased by the host's default
// System.Text.Json policy. ContentType, ShareabilityBasis and ApprovalStatus each serialize as
// their numeric enum value (no JsonStringEnumConverter is registered on the host), which is what
// those enums are numbered to match.
export type ContentItem = {
    id: string;
    contentType: ContentType;
    title: string | null;
    author: string | null;
    content: string;
    shareabilityBasis: ShareabilityBasis;
    sharePermission: string | null;

    // Control fields, all DERIVED on write and never accepted from a caller: the hash the
    // duplicate check runs on, the version group, and the approval and publication state the
    // workflow owns.
    contentHash: string;
    groupId: string;
    version: number;
    publishDate: string | null;
    isPublished: boolean;
    approvalStatus: ApprovalStatus;
    isApprovedByBypass: boolean;
    approvedByBypassReason: string | null;

    isDeleted: boolean;
    createdBy: string;
    createdWhen: string;
    updatedBy: string;
    updatedWhen: string;
    deletedBy: string | null;
    deletedWhen: string | null;
    deletionReason: string | null;
};

// EVERYTHING A CALLER MAY SEND on POST api/ContentItems, and nothing else. The processing service
// composes the row it stores from these six members alone — it mints the Id and GroupId, computes
// the ContentHash, and lands the row as an unpublished Draft; the foundation beneath it stamps the
// audit fields from the envelope's SecurityContext. Sending more is not rejected, it is simply
// discarded, so the type states what actually travels rather than inviting a caller to believe
// otherwise.
export type ContentItemAddRequest = {
    contentType: ContentType;
    title: string | null;
    author: string | null;
    content: string;
    shareabilityBasis: ShareabilityBasis;
    sharePermission: string | null;
};
