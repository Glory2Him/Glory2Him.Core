import { ApprovalStatus } from '../associations/associationItem';
import { ContentType } from '../../foundations/contentItemSettings/contentType';

export { ApprovalStatus };

// Mirrors Glory2Him.Core.Models.Enums.ShareabilityBasis. The host serializes enums as their
// numeric value (no JsonStringEnumConverter is registered), so the numbers are the wire contract
// of POST / PUT api/ContentItems and must not be renumbered — the C# enum is append-only for the
// same reason.
export const ShareabilityBasis = {
    Owned: 0,
    PermissionGranted: 1,
    PublicDomain: 2
} as const;

export type ShareabilityBasis = typeof ShareabilityBasis[keyof typeof ShareabilityBasis];

// How each basis is put to a contributor. Wording carried over from the bespoke contribute form
// this panel replaces, so the question reads the same as it always did.
export const shareabilityBasisLabels: Readonly<Record<ShareabilityBasis, string>> = {
    [ShareabilityBasis.Owned]: "It's my own",
    [ShareabilityBasis.PermissionGranted]: 'I have permission from the owner to share it',
    [ShareabilityBasis.PublicDomain]: "It's public domain"
};

// Declaration order for the dropdown. Object.keys over the const object would yield the keys in
// insertion order too, but stating the list keeps the order a decision rather than an accident.
export const shareabilityBasisMembers: ReadonlyArray<ShareabilityBasis> = [
    ShareabilityBasis.Owned,
    ShareabilityBasis.PermissionGranted,
    ShareabilityBasis.PublicDomain
];

// The three surfaces of one content item. `add` has no item behind it yet; `read` renders the
// item; `edit` renders the same fields the add surface does, over an item that already exists.
export type ContentItemPanelMode = 'add' | 'read' | 'edit';

// The minimum a ContentItemPanel needs to render one content item and decide who may act on it.
// A page projects whatever it holds — a ContentItem row off the wire, a draft it is composing —
// down to this shape, so the panel never depends on the wire entity (the same split
// AssociationItem and ApprovalReviewItem take).
export type ContentItemFormItem = {
    // Absent on an item that has not been persisted yet. Present, it is the panel's React key
    // material and what the consumer routes its PUT / DELETE on.
    id?: string;

    // Create-only (§12.4.1 rule 7a): an item may not be relabelled into a type its content was
    // never checked against, so the panel offers the picker in `add` and a frozen label in
    // `edit`. It also composes the content-type-scoped role names, which is why the numeric
    // member — never a display name — is what is carried.
    contentType: ContentType;

    title?: string;
    author?: string;
    content: string;

    shareabilityBasis: ShareabilityBasis;

    // Free text detailing the permission. Rendered only for PermissionGranted, as the bespoke
    // form did.
    sharePermission?: string;

    // Who contributed it, for the [OWNER] rule. The ACCOUNT ID — the value the audit trail
    // records — never a display name: two accounts can share one.
    createdBy?: string;

    // Drives the edit gate for the non-owner tier, which may amend a Draft or a Submitted item
    // and never a terminal one (§3.4 rule 16 — amending a decided item forks a new version).
    approvalStatus?: ApprovalStatus;

    // Soft deletion, orthogonal to approval. A projection that already filters removed rows away
    // can leave this unset.
    isDeleted?: boolean;
};

// Field-keyed validation messages exactly as the API returned them — the `errors` dictionary of
// the ValidationProblemDetails a RESTFulSense controller builds from the service's Xeption.
//
// The KEYS ARE THE SERVER'S parameter names (`Content`, `ContentType`, `SharePermission`), so the
// panel matches them case-insensitively against its own fields and shows anything it cannot place
// in a summary rather than dropping it. The panel never invents an entry: it renders what the
// server said and nothing more.
export type ContentItemValidationIssues = Readonly<Record<string, ReadonlyArray<string>>>;
