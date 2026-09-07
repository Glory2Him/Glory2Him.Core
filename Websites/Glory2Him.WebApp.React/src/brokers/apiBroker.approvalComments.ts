import ApiBroker from './apiBroker';

import {
    ApprovalComment,
    ApprovalCommentAddRequest
} from '../models/foundations/approvals/approvalComment';

// The review thread's reads and writes, against api/ApprovalComments — the plain foundation
// collection, OData-filtered like the other foundation reads in this folder, and written to like
// them: a comment is a row.
//
// EVERY CALL IS KEYED BY THE APPROVAL, never by the content item. A comment names the approval it
// hangs off and nothing about the post being judged, so a caller reads the verdict first to learn
// the approval's id — the same chain the reviews take.
//
// NOTHING HERE DECIDES WHO MAY DO WHAT. The controller is §14.7 posture D: the collection read
// degrades to the caller's own comments rather than refusing, the writes are the author's alone
// and the resolution is the author's or the publisher tier's, all re-decided against the stored
// row (§14.6).
class ApprovalCommentBroker {
    relativeApprovalCommentsUrl = '/api/approvalcomments';
    private apiBroker: ApiBroker = new ApiBroker();

    // The thread as it stands. Soft-deleted rows are filtered SERVER-side rather than thrown away
    // here: a withdrawn comment is not part of the conversation, and a page of them would waste
    // the round trip it took to fetch.
    async GetApprovalCommentsAsync(approvalId: string): Promise<ApprovalComment[]> {
        const filter = `approvalId eq ${approvalId} and isDeleted eq false`;
        const url = `${this.relativeApprovalCommentsUrl}?$filter=${encodeURIComponent(filter)}`;
        const result = await this.apiBroker.GetAsync(url);

        return result.data as ApprovalComment[];
    }

    // A COMMENT IS A ROW. The id travels in the body, minted by the caller — the foundation
    // refuses an empty Guid and never generates one — and the audit fields do NOT travel at all:
    // the server stamps them from the caller's own identity before it validates anything, and an
    // empty string in a DateTimeOffset is refused in model binding with a body that names no
    // message.
    async PostApprovalCommentAsync(
        approvalComment: ApprovalCommentAddRequest): Promise<ApprovalComment> {
        const result = await this.apiBroker.PostAsync(
            this.relativeApprovalCommentsUrl, approvalComment);

        return result.data as ApprovalComment;
    }

    // AN EDIT IS A PUT OF THE ROW THAT WAS READ. The foundation pins CreatedBy, CreatedWhen,
    // ApprovalId and UpdatedWhen against storage before it accepts the write, so the whole row
    // goes rather than the changed fields alone.
    async PutApprovalCommentAsync(approvalComment: ApprovalComment): Promise<ApprovalComment> {
        const result = await this.apiBroker.PutAsync(
            this.relativeApprovalCommentsUrl, approvalComment);

        return result.data as ApprovalComment;
    }

    // SOFT removal, never DELETE {id}/Hard. A withdrawn comment leaves the §8.5 block — the
    // evaluation counts rows where IsDeleted is false && IsResolved is false — so the row going
    // quiet is the whole effect, and the record of what was said stays. The reason rides the query
    // string because it is a scalar the operation owns, not a body the caller composes.
    async DeleteApprovalCommentByIdAsync(
        approvalCommentId: string,
        deletionReason: string): Promise<ApprovalComment> {
        const url = `${this.relativeApprovalCommentsUrl}/${approvalCommentId}`
            + `?deletionReason=${encodeURIComponent(deletionReason)}`;

        const result = await this.apiBroker.DeleteAsync(url);

        return result.data as ApprovalComment;
    }

    // THE SETTLED FLAG, and nothing else (§14.7 rule 5). The value is ALWAYS sent: the endpoint
    // binds it [BindRequired] precisely because an absent bool would bind to false with a valid
    // model state and quietly UN-resolve a comment that had been settled — a 200 on a route
    // reading /Resolve, re-blocking an approval that had been cleared.
    async PostApprovalCommentResolveAsync(
        approvalCommentId: string,
        isResolved: boolean): Promise<ApprovalComment> {
        const url = `${this.relativeApprovalCommentsUrl}/${approvalCommentId}`
            + `/Resolve?isResolved=${String(isResolved)}`;

        const result = await this.apiBroker.PostAsync(url, {});

        return result.data as ApprovalComment;
    }
}

export default ApprovalCommentBroker;
