// The wire shape of api/ApprovalComments, camelCased by the host's default System.Text.Json
// policy. Only what the review-comment surface reads is typed; the members it does not use ride
// along untyped exactly as the other wire models leave what they do not need.
//
// KEYED BY THE APPROVAL, not by the entity. A comment carries an approvalId and nothing that
// names the content item the round is about, so the verdict has to be read FIRST — it is what
// turns "this post" into the approval id the thread hangs off, the same chain the reviews take.

// Mirrors Glory2Him.Core.Models.Enums.ApprovalCommentType. The host registers no
// JsonStringEnumConverter, so the NUMBERS are the wire contract and this mirror is numbered to
// match — the column stores the member NAME, which is a storage decision the client never sees.
//
// WHAT A COMMENT IS, beside where it stands. §7.8 draws the line in IsResolved — an observation
// asks for nothing and is born settled, an ask holds the approval shut — but that field MOVES:
// the moment a question is settled it carries the same flag an informational comment was born
// with. This says which it was, so a thread can tell them apart and the resolve control can be
// offered on questions alone.
export const ApprovalCommentType = {
    Comment: 0,
    Question: 1
} as const;

export type ApprovalCommentType =
    typeof ApprovalCommentType[keyof typeof ApprovalCommentType];

// GET api/ApprovalComments — one row of the thread. CreatedBy is the author's ACCOUNT ID and the
// row carries no name for them, which is what the round's ReviewerDisplayNames read answers.
//
// THE WHOLE ROW GOES BACK ON AN AMEND. The foundation pins CreatedBy, CreatedWhen, ApprovalId and
// UpdatedWhen against storage before it accepts the write, so they travel too — an edit is a PUT
// of the row that was read, never a fresh object carrying only the new words.
export type ApprovalComment = {
    id: string;
    approvalId: string;
    comment: string;
    commentType: ApprovalCommentType;
    isResolved: boolean;
    createdBy: string;
    createdWhen: string;
    updatedBy: string;
    updatedWhen: string;
    isDeleted: boolean;
};

// POST api/ApprovalComments — a comment as the client composes it. NO AUDIT FIELDS, and that is
// the contract rather than a convenience: the server stamps CreatedBy/When and UpdatedBy/When
// from the caller's own identity (ApplyAddAuditValuesAsync) before it validates anything, so a
// client has nothing true to put there — and an empty string in a DateTimeOffset is refused in
// model binding, before the service ever sees the row. The id IS the client's: the foundation
// refuses an empty Guid and never mints one.
//
// isResolved travels because BOTH birth values are legitimate for a remark (§7.8 rule 1): a
// Question is born outstanding and holds the approval shut, a Comment is born settled and never
// blocks. The field carries no shape rule, so saying nothing would silently make every remark a
// blocker. The one pairing the server refuses is a settled ask.
export type ApprovalCommentAddRequest = Pick<
    ApprovalComment,
    'id' | 'approvalId' | 'comment' | 'commentType' | 'isResolved' | 'isDeleted'>;
