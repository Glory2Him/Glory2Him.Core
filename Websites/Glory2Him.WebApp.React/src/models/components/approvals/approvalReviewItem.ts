import { ApprovalStatus } from '../associations/associationItem';

export { ApprovalStatus };

// Mirrors G2H.Security.Client.Models.Foundations.Access.ApprovalDecision. The host serializes
// enums as their numeric value, so the numbers are the wire contract of
// POST api/Approvals/{entityType}/{entityId}/Decision and must not be renumbered.
export const ApprovalDecision = {
    Approve: 0,
    Reject: 1
} as const;

export type ApprovalDecision = typeof ApprovalDecision[keyof typeof ApprovalDecision];

// One reviewer's recorded verdict, projected from an ApprovalReview row. The panel renders a row
// per item plus, for the signed-in viewer, a synthesized placeholder row when no item carries
// their userId yet.
export type ApprovalReviewItem = {
    // The reviewer's account id — the value the audit trail records on CreatedBy, and the ONLY
    // thing compared against the signed-in user's id to find "my" row. Never a display name:
    // two accounts can share one.
    reviewerUserId: string;

    // What the row reads. The viewer's own row is labelled with their display name too, taken
    // from this item rather than useAuth() so a freshly changed profile name and the recorded
    // review cannot disagree on screen.
    reviewerDisplayName: string;

    // The handle under the display name, resolved from the same §16.7.4 names read the display
    // name comes from. OPTIONAL because an id that named no account resolves to neither field,
    // and because a consumer may be projecting from somewhere that has no username to give — the
    // row then renders the name alone. Never an email address (§18.3.1).
    reviewerUserName?: string;

    // The verdict: Approved or Rejected. Reviews are only ever recorded with one of the two —
    // an uncast vote has no row, which is why the placeholder is synthesized instead.
    vote: ApprovalStatus;

    // Soft deletion, orthogonal to the verdict - a withdrawn review keeps its row. Excluded from
    // the panel like a dismissed one, so a projection that has not already filtered deleted rows
    // away can hand them over safely.
    isDeleted?: boolean;

    // Optional stable key. Prefer it over reviewerUserId whenever the source row has an id.
    id?: string;
};

// One person the panel can name outside the cast votes: a reviewer candidate offered in the
// request picker, or the target of a pending ApprovalReviewRequest, which renders in the main
// list wearing a "Requested" chip (design §7.9). userId is the identity — displayName is
// presentation only and is never compared.
export type ReviewerCandidateItem = {
    userId: string;
    displayName: string;
    // OPTIONAL by design, not by oversight. The §16.7.4 reads now carry a username, so the usual
    // consumer supplies one — but an id that named no account resolves to none, and a consumer
    // projecting from somewhere else may have none to give. The row then renders the display name
    // alone and the filter matches on it alone, which is why the filter placeholder promises
    // "name" rather than anything a caller may not be able to give.
    userName?: string;

    // Why the picker is suggesting this person - "Recently reviewed this type", and the like.
    // Presentation only, and the CONSUMER decides it: the panel has no basis for ranking people
    // and must not invent one. Present only on entries passed as suggestions.
    suggestionReason?: string;
};

// THE AI REVIEWER IDENTITY (design §8.6.2, "Berean" — Acts 17:11). Offered to ReviewPanel as
// aiReviewerCandidate, which is the ONLY thing that puts it in the picker: a panel handed no
// candidate offers no AI reviewer, which is the fail-closed posture §8.4 asks for expressed as
// the absence of a prop rather than as a flag somebody has to remember to set.
//
// THE ID IS A PLACEHOLDER AND NOTHING READS IT AS AN ACCOUNT. Berean acts under a system
// identity (§8.6.2), and that account does not exist yet — so this is deliberately NOT a GUID.
// Anything that posted it to the review-request endpoints would be refused, which is the right
// outcome while the backend half of #354 is unbuilt: the panel raises onAIReviewerRequested and
// the consumer decides what that means. Replace this with the system account's id when the
// identity lands, and nothing else here has to move.
export const BereanAIReviewerUserId = 'ai-reviewer-berean';

export const BereanAIReviewer: ReviewerCandidateItem = {
    userId: BereanAIReviewerUserId,
    displayName: 'Berean'
};

// One reason approval cannot be granted right now — the client-side shape of
// Glory2Him.Core.Models.Orchestrations.Approvals.ApprovalBlockReason. Render `message`;
// branch (if ever needed) on `code`, which is the numeric AccessDenialReason and survives
// rewording and translation.
export type ApprovalBlockReasonItem = {
    code: number;
    message: string;
};

// The per-caller answer to "what may happen to this approval now", mirroring the
// GET api/Approvals/{entityType}/{entityId}/Verdict response (ApprovalVerdict, design §16.7.2).
// Every outcome gate on the panel reads off this rather than re-deriving from roles:
// isBypassAllowedForCurrentUser folds the caller's tier and DoNotAllowBypassingSettings, and
// canApprove folds HR-2 self-approval and the reviewer-carried-it rule — none of which the
// browser could decide from role names alone.
export type ApprovalVerdictItem = {
    approvalId: string;
    approvalStatus: ApprovalStatus;
    blockReasons: ReadonlyArray<ApprovalBlockReasonItem>;
    isBlocked: boolean;
    isBypassAllowedForCurrentUser: boolean;
    canApprove: boolean;
    approvalCount: number;
    requiredNumberOfApprovals: number;
    unresolvedApprovalCommentCount: number;
};
