import {
    ApprovalStatus,
    AssociationItem
} from '../../../models/components/associations/associationItem';

// Projects the bare strings a page holds onto the shape AssociationPanel renders. Shared rather
// than repeated per page: the post detail and the bible reference page both do exactly this, and
// a projection that drifted between them would show the same tag as approved on one and pending
// on the other.

// Anything already carried by a published post or passage is approved — it is live content, so
// it projects to an approved chip with no contributor.
export const asApprovedAssociations = (
    values: ReadonlyArray<string>
): ReadonlyArray<AssociationItem> =>
    values.map((value) => ({ value, approvalStatus: ApprovalStatus.Approved }));

// A suggestion is the reader's own and still awaiting a decision, which is what earns it the
// hourglass and the single action a read-only panel offers: withdrawing it again. Without
// createdBy the owner rule cannot fire, so the reader would be unable to take back what they
// just typed.
export const asSuggestedAssociation = (
    value: string,
    createdBy: string | undefined
): AssociationItem => ({
    value,
    createdBy,
    approvalStatus: ApprovalStatus.Submitted
});

// Matched on value rather than id: a suggestion made on the page has no id until it reaches the
// server, and the panel's own duplicate check already guarantees the value is unique.
export const withoutAssociationValue = (
    items: ReadonlyArray<AssociationItem>,
    removed: AssociationItem
): ReadonlyArray<AssociationItem> =>
    items.filter((item) => item.value !== removed.value);
