import { ApprovalCommentType } from '../../foundations/approvals/approvalComment';

export { ApprovalCommentType };

// ONE ROW OF A REVIEW THREAD, projected from an ApprovalComment. The panel takes no wire model
// on purpose — it renders a conversation, whatever produced one — so the mapping lives in
// services/views/approvals/toReviewCommentItems.ts rather than in the panel or the page.
//
// THE AUTHOR IS RESOLVED BEFORE IT GETS HERE. An ApprovalComment names its author by ACCOUNT ID
// and carries no name at all, and resolving one is a separate read keyed on the round. Doing it
// in the projection rather than in the panel is what keeps the panel pure and what stops a second
// surface inventing its own lookup and rendering one person under two names.
export type ReviewCommentItem = {
    id: string;

    // The round this belongs to. Carried so a row can be written back without the panel having
    // to reach for the panel-level prop — an edit is a PUT of the row that was read, and the
    // foundation pins this field against storage.
    approvalId: string;

    comment: string;

    // What the row IS. Decides the chip, and decides whether the resolve control appears at all:
    // "Is resolved — I got my answer" is meaningless against a remark that asked for nothing.
    commentType: ApprovalCommentType;

    // Whether the row is SETTLED — whether it still requires something before the approval can
    // proceed (§7.8). Never "has the question been answered": a remark is born settled and never
    // blocks, and a settled question can be re-opened.
    isResolved: boolean;

    // The author's ACCOUNT ID — the value the audit trail records on CreatedBy, and the ONLY
    // thing compared against the signed-in user to decide whether Edit and Delete render. Never a
    // display name: two accounts can share one.
    authorId: string;

    // What the row reads. Resolved from the round's own name read; an account that has gone
    // leaves the words standing and the name absent, which is the right shape for both.
    authorDisplayName: string;

    // The author's username, rendered muted in brackets beside the display name. OPTIONAL: an id
    // that resolves to no account has neither, and a consumer wired to a name source that carries
    // no username supplies none — the row then renders the display name alone.
    authorUserName?: string;

    // ISO 8601, straight off the row. The panel formats and sorts on it, so it must be the
    // stored value rather than anything already rendered.
    createdWhen: string;

    updatedWhen?: string;
};

// A NEW COMMENT as the add face composes it — the words, what it is, and the round it belongs
// to. No id and no audit fields: the consumer mints the id (the foundation refuses an empty Guid
// and never mints one) and the server stamps the audit trail from the caller's own identity.
export type ReviewCommentDraft = {
    approvalId: string;
    comment: string;
    commentType: ApprovalCommentType;
};

// What the family raises, gathered so every panel in it declares the same events and a consumer
// wires them once. Every one of them is a NOTIFICATION of what the reader decided — nothing here
// persists anything, and the consumer owns every write.
export type ReviewCommentEvents = {
    // A new comment. The panel has already refused a blank one and cleared its box by the time
    // this lands.
    onSave?: (draft: ReviewCommentDraft) => void;

    // The box emptied and the radios put back to defaultType. Notification only — the reset is
    // internal, the way ContentItemPanel closes its own editor.
    onClear?: () => void;

    // An edited row committed. Carries the WHOLE row so the consumer can PUT what it read with
    // only the changed fields moved.
    onModified?: (item: ReviewCommentItem) => void;

    // Delete, already CONFIRMED by the consumer. The panel raises the intent through
    // onRemoveRequested and this fires only once the consumer has said yes — the same split
    // ContentItemSettingsPanel makes for Remove Override.
    onRemoved?: (item: ReviewCommentItem) => void;

    // The reader took Delete. The CONSUMER asks "Are you sure?" — a panel that owned the dialog
    // would own a piece of page chrome it cannot place.
    onRemoveRequested?: (item: ReviewCommentItem) => void;

    // The resolve tick, both ways. Settling and re-opening ride one event because the transition
    // is symmetric by design: a comment settled prematurely must be able to block again.
    onResolvedChanged?: (item: ReviewCommentItem, isResolved: boolean) => void;
};

// The presentation props every face in the family shares.
export type ReviewCommentText = {
    cssClass?: string;
    titleText?: string;
    ariaLabel?: string;
};
