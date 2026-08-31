import { ApprovalStatus } from '../associations/associationItem';
import { ContentItemSetting } from '../../foundations/contentItemSettings/contentItemSetting';
import { ContentType } from '../../foundations/contentItemSettings/contentType';

export { ApprovalStatus };

// Mirrors Glory2Him.Core.Models.Enums.ShareabilityBasis. The host serializes enums as their
// numeric value (no JsonStringEnumConverter is registered), so the numbers are the wire contract
// of POST / PUT api/ContentItems and must not be renumbered — the C# enum is append-only for the
// same reason.
//
// TWO QUESTIONS CROSSED. Who wrote it, and what the site may do with it. Neither answer implies
// the other, which is why the live members are the pair: "it's my own" says nothing about whether
// the contributor is releasing it, and "public domain" says nothing about who wrote it.
export const ShareabilityBasis = {
    // RETIRED, and still valid. Every item contributed before the split carries it, and
    // reclassifying those rows would put a licence claim on somebody's work that they never made.
    // It is not offered in the picker (see shareabilityBasisMembers) and appears only on an item
    // that already holds it.
    Owned: 0,

    PermissionGranted: 1,
    PublicDomain: 2,
    OwnedPermissionGranted: 3,
    OwnedPublicDomain: 4
} as const;

export type ShareabilityBasis = typeof ShareabilityBasis[keyof typeof ShareabilityBasis];

// How each basis is PUT TO A CONTRIBUTOR, under the question "How are you permitted to share
// this?". Short enough to sit in a dropdown without wrapping, and each one a complete answer to
// that question on its own.
export const shareabilityBasisLabels: Readonly<Record<ShareabilityBasis, string>> = {
    [ShareabilityBasis.PublicDomain]: "It's public domain",
    [ShareabilityBasis.PermissionGranted]: 'I have permission to share',
    [ShareabilityBasis.OwnedPublicDomain]: "It's my own, released as public domain",
    [ShareabilityBasis.OwnedPermissionGranted]: "It's my own, I grant permission to share",

    // The retired member never appears in the picker, so this is only ever read where a stored
    // item is being edited. It states what the row actually says and claims nothing further.
    [ShareabilityBasis.Owned]: "It's my own"
};

// WHAT A READER IS TOLD, which is a different question from what a contributor is asked. A
// reader wants the LICENCE — may this be passed on, and under what terms — and who wrote it is
// already answered by the Submitted by and Author fields sitting beside this one. So the two
// public-domain members read alike, and so do the two permission members: the ownership half of
// the answer is not repeated here, and the label stays short enough for a meta row.
export const shareabilityBasisReadLabels: Readonly<Record<ShareabilityBasis, string>> = {
    [ShareabilityBasis.PublicDomain]: 'Public Domain',
    [ShareabilityBasis.OwnedPublicDomain]: 'Public Domain',
    [ShareabilityBasis.PermissionGranted]: 'Shared by Permission',
    [ShareabilityBasis.OwnedPermissionGranted]: 'Shared by Permission',

    // The retired member states ownership and no licence, because that is all it ever recorded.
    [ShareabilityBasis.Owned]: 'Own Work'
};

// Declaration order for the dropdown, and the OFFERABLE set: the retired Owned member is absent,
// so a contributor can never newly file an item under it. Stating the list rather than deriving
// it from Object.keys is what makes that exclusion a decision instead of an accident.
export const shareabilityBasisMembers: ReadonlyArray<ShareabilityBasis> = [
    ShareabilityBasis.PublicDomain,
    ShareabilityBasis.PermissionGranted,
    ShareabilityBasis.OwnedPublicDomain,
    ShareabilityBasis.OwnedPermissionGranted
];

// THE BASIS AN UNTOUCHED FORM CARRIES. It has to be a member of the offerable list above — the
// retired Owned it used to be would leave the <select> showing an option it does not hold — and
// of the four it is deliberately the NARROWEST: a contributor who never opens the dropdown has
// licensed this use and given nothing away.
export const defaultShareabilityBasis: ShareabilityBasis =
    ShareabilityBasis.OwnedPermissionGranted;

// Whether the basis says THE CONTRIBUTOR WROTE IT. Where it does, the Author field is the
// submitter over again — the read surface already names them under "Submitted by" — so the field
// is neither asked for nor rendered. The retired member counts: it said ownership and nothing
// else, which is precisely this question.
export const isOwnedShareabilityBasis = (basis: ShareabilityBasis): boolean =>
    basis === ShareabilityBasis.Owned
    || basis === ShareabilityBasis.OwnedPermissionGranted
    || basis === ShareabilityBasis.OwnedPublicDomain;

// Whether the basis rests on somebody's PERMISSION, and so has a permission worth detailing.
// Both members qualify: the contributor either holds the owner's permission or is granting their
// own, and either way the detail field is what records it.
export const isPermissionShareabilityBasis = (basis: ShareabilityBasis): boolean =>
    basis === ShareabilityBasis.PermissionGranted
    || basis === ShareabilityBasis.OwnedPermissionGranted;

// The three surfaces of one content item. `add` has no item behind it yet; `read` renders the
// item; `edit` renders the same fields the add surface does, over an item that already exists.
export type ContentItemPanelMode = 'add' | 'read' | 'edit';

// The minimum the ContentItemPanel form faces need to render one content item and decide who
// may act on it.
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

    // THE WINNING SETTING, when the projection resolved one (§6.4: this item's own override
    // beats its type default) — carried ON the item so a list surface can hand its element
    // straight to a detail view with no server round trip. When present it WINS over the
    // panel's contentItemSettingCollection for this item's shaping; absent, the panel
    // resolves from the collection exactly as it always has, and `add` — which has no item
    // yet — always shapes from the collection and stamps the winner onto what it emits.
    contentItemSetting?: ContentItemSetting;

    title?: string;
    author?: string;
    content: string;

    shareabilityBasis: ShareabilityBasis;

    // Free text detailing the permission. Rendered only for a permission basis, as the bespoke
    // form did.
    sharePermission?: string;

    // Who contributed it, for the [OWNER] rule. The ACCOUNT ID — the value the audit trail
    // records — never a display name: two accounts can share one.
    createdBy?: string;

    // WHEN it was contributed, as the ISO string the wire carries. Read-only, and rendered in the
    // byline; the panel never sends it back, because the foundation stamps it from the envelope.
    createdWhen?: string;

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
