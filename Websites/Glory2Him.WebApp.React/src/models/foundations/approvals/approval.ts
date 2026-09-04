import { ApprovalStatus } from '../../components/associations/associationItem';

// Wire shapes of the approval endpoints, camelCased by the host's default System.Text.Json
// policy. Only what the review surface reads is typed; the audit members ride along untyped
// exactly as the other wire models leave what they do not use.
//
// EVERY ONE OF THESE IS KEYED BY THE APPROVAL, NOT BY THE ENTITY. An ApprovalReview carries an
// approvalId and nothing that names the content item it judges, so the verdict has to be read
// FIRST — it is what turns "this post" into the approval id the reviews hang off.

// Mirrors Glory2Him.Core.Models.Enums.EntityType. The host binds a route enum from either its
// member name or its number; the NAME is used on the wire here, so a URL in a network log says
// what it is about.
export const EntityTypeName = {
    ContentItem: 'ContentItem',
    Tag: 'Tag'
} as const;

export type EntityTypeName = typeof EntityTypeName[keyof typeof EntityTypeName];

// GET api/Approvals/{entityType}/{entityId}/Verdict — the per-caller answer to "what may happen
// to this approval now" (design §16.7.2). Numeric `code` is the AccessDenialReason and survives
// rewording; `message` is what a moderator reads.
export type ApprovalBlockReason = {
    code: number;
    message: string;
};

export type ApprovalVerdict = {
    approvalId: string;
    entityType: number;
    entityId: string;
    approvalStatus: ApprovalStatus;
    blockReasons: ReadonlyArray<ApprovalBlockReason>;
    isBlocked: boolean;
    isBypassAllowedForCurrentUser: boolean;
    canApprove: boolean;
    approvalCount: number;
    requiredNumberOfApprovals: number;
    unresolvedApprovalCommentCount: number;
};

// GET api/ApprovalReviews — one reviewer's recorded verdict. CreatedBy is the reviewer's ACCOUNT
// ID and the row carries no name for them, which is what ReviewerDisplayName exists to answer.
//
// THE WHOLE ROW GOES BACK ON AN AMEND. A reviewer holds at most one active review on a round
// (§7.7 rule 1), so changing a vote is a PUT of the row that was read, with the verdict
// changed — never a second POST — and the foundation compares the audit fields against
// storage before it accepts the write, so they travel too.
export type ApprovalReview = {
    id: string;
    approvalId: string;
    statusId: ApprovalStatus;
    comment: string;
    createdBy: string;
    createdWhen: string;
    updatedBy: string;
    updatedWhen: string;
    isDeleted: boolean;
};

// POST api/ApprovalReviews — a vote as the client composes it. NO AUDIT FIELDS, and that is
// the contract rather than a convenience: the server stamps CreatedBy/When and UpdatedBy/When
// from the caller's own identity (ApplyAddAuditValuesAsync) before it validates anything, so a
// client has nothing true to put there — and an empty string in a DateTimeOffset is refused in
// model binding, before the service ever sees the row, with a body that names no message. The
// id IS the client's: the foundation refuses an empty Guid and never mints one.
export type ApprovalReviewAddRequest = Pick<
    ApprovalReview,
    'id' | 'approvalId' | 'statusId' | 'comment' | 'isDeleted'>;

// POST api/Approvals/{entityType}/{entityId}/Decision — what the round became. The entity's
// own status follows through the workflow's transition command rather than in this response,
// which is why a decision invalidates the item's reads as well as the round's.
export type ApprovalOutcome = {
    approvalId: string;
    entityType: number;
    entityId: string;
    approvalStatus: ApprovalStatus;
    isApprovedByBypass: boolean;
    approvedByBypassReason: string | null;
    isEntitySyncRequested: boolean;
};

// GET api/Approvals/{entityType}/{entityId}/ReviewerCandidates — the minimum a picker needs
// (§16.7.4): an account id and a display name, and nothing else.
export type ReviewerCandidate = {
    userId: string;
    displayName: string;
};

// GET api/Approvals/{entityType}/{entityId}/ReviewRequests — somebody invited to review who has
// not answered yet. Unlike a review, this one DOES carry the name it was addressed to.
export type ApprovalReviewRequest = {
    id: string;
    approvalId: string;
    requestedUserId: string;
    requestedUserDisplayName: string;
    isDeleted: boolean;
};

// GET api/Approvals/ReviewerDisplayNames?userIds=… — the names behind the account ids a review
// row carries. Asked in one round trip for the whole round rather than one per reviewer.
export type ReviewerDisplayName = {
    userId: string;
    displayName: string;
};
