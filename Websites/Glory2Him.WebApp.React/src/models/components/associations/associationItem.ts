// Mirrors Glory2Him.Core.Models.Enums.ApprovalStatus. The host serializes enums as their numeric
// value (no JsonStringEnumConverter is registered), so the numbers are the wire contract and must
// not be renumbered — the C# enum is append-only for the same reason.
export const ApprovalStatus = {
    Draft: 0,
    Submitted: 1,
    Approved: 2,
    Rejected: 3,
    Dismissed: 4
} as const;

export type ApprovalStatus = typeof ApprovalStatus[keyof typeof ApprovalStatus];

// The minimum an AssociationPanel needs to render one chip and decide who may act on it. A page
// projects whatever it holds — a Tag, a BibleReference, an Association row — down to this shape,
// so the panel never depends on any one entity.
export type AssociationItem = {
    // What the chip reads. Also the panel's fallback React key, so `id` is worth supplying when
    // two items can legitimately carry the same text.
    value: string;

    // Who contributed it, for the [OWNER] rule. Compared against the signed-in user's id AND
    // username, because the audit trail records whichever the security context yields.
    createdBy?: string;

    // Drives both the pending affordance and, with createdBy, whether the owner may withdraw it.
    approvalStatus?: ApprovalStatus;

    // Soft deletion, which is orthogonal to approval — a row can be Approved AND removed. A
    // removed row is never rendered to anyone, whatever their role, so it outranks every other
    // gate. Projections that already filter deleted rows away can leave this unset.
    isDeleted?: boolean;

    // Optional stable key. Prefer it over `value` whenever the source row has an id.
    id?: string;
};
