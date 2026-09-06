import {
    ApprovalComment
} from '../../../models/foundations/approvals/approvalComment';

import {
    ReviewerDisplayName
} from '../../../models/foundations/approvals/approval';

import {
    ReviewCommentItem
} from '../../../models/components/approvals/reviewCommentItem';

// The wire shape projected down to what ReviewCommentPanel renders. The panel takes no wire model
// on purpose — it renders a conversation, whatever produced one — so the mapping lives here
// rather than in the panel or in the page, exactly as toReviewPanelItems does for the round.

// A comment row names its author only by ACCOUNT ID. The names arrive from the round's own name
// read, so resolution is a lookup with an honest fallback: an account that has gone leaves the
// words standing and the name absent, which is the right shape for both — what was said is a
// fact about the round, not about whoever is still on the system.
const authorOf = (
    userId: string,
    reviewerDisplayNameCollection: ReadonlyArray<ReviewerDisplayName>):
    { displayName: string; userName: string } => {
    const resolved = reviewerDisplayNameCollection
        .find((reviewerDisplayName) => reviewerDisplayName.userId === userId);

    // BOTH FIELDS ARE ONE DECISION. A resolved account always carries both — §18.3.1 makes a
    // username mandatory, and §16.7.4 carries it beside the display name — so there is no branch
    // where a row has a name and no handle. The only fork is whether the id resolved AT ALL: an
    // account that has since been deleted is named by nothing (§16.7.4 leaves it out rather than
    // erroring), and then neither field has a value to give.
    if (resolved == null) {
        return { displayName: 'Unknown author', userName: '' };
    }

    return { displayName: resolved.displayName, userName: resolved.userName };
};

export const toReviewCommentItem = (
    approvalComment: ApprovalComment,
    reviewerDisplayNameCollection: ReadonlyArray<ReviewerDisplayName> = []
): ReviewCommentItem => {
    const author = authorOf(approvalComment.createdBy, reviewerDisplayNameCollection);

    return {
        id: approvalComment.id,
        approvalId: approvalComment.approvalId,
        comment: approvalComment.comment,
        commentType: approvalComment.commentType,
        isResolved: approvalComment.isResolved,

        // CreatedBy, not some author column: the audit trail records who wrote it, and that is
        // the id the panel compares against the signed-in user to decide whether Edit and Delete
        // render.
        authorId: approvalComment.createdBy,
        authorDisplayName: author.displayName,
        authorUserName: author.userName,
        createdWhen: approvalComment.createdWhen,
        updatedWhen: approvalComment.updatedWhen
    };
};

// SOFT-DELETED ROWS ARE DROPPED HERE TOO, not only in the OData filter. The read asks the server
// to leave them out, but this projection is also handed collections a test or a doc page
// composed by hand — and a withdrawn comment must never render, whichever route it arrived by.
export const toReviewCommentItems = (
    approvalCommentCollection: ReadonlyArray<ApprovalComment>,
    reviewerDisplayNameCollection: ReadonlyArray<ReviewerDisplayName> = []
): ReadonlyArray<ReviewCommentItem> =>
    approvalCommentCollection
        .filter((approvalComment) => approvalComment.isDeleted !== true)
        .map((approvalComment) =>
            toReviewCommentItem(approvalComment, reviewerDisplayNameCollection));
