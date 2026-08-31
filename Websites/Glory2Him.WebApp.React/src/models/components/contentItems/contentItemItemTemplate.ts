import { ContentItemSetting } from '../../foundations/contentItemSettings/contentItemSetting';

import { shareabilityBasisReadLabels } from './contentItemFormItem';

import {
    ApprovalStatus,
    ContentItemReactionOption,
    ContentItemSearchItem,
    ShareabilityBasis
} from './contentItemSearchItem';

// The contract between ContentItemItemPanel and its templates. In a module of its own so the
// templates can state it without importing the panel that dispatches to them — the panel imports
// every template, and a template importing it back would be a cycle.

// Every event hook a card raises. Filter hooks are handled by ContentItemSearchPanel (they
// rewrite the committed criteria); navigation hooks bubble through it to the page, which owns
// the redirect — and stamps the origin into router state so the destination can offer a true
// way back.
export interface ContentItemItemEvents {
    // Toggles the Category criterion: set if clear, cleared if already this item's type.
    onContentTypeClick?: (item: ContentItemSearchItem) => void;

    // The way into the detail surface — WHICH detail surface is the page's decision: public
    // detail, my-content detail, or moderation detail.
    onTitleClick?: (item: ContentItemSearchItem) => void;

    // Sets the submitted-by criterion to this item's submitter.
    onSubmittedByClick?: (item: ContentItemSearchItem) => void;

    // Sets the author criterion to this item's author.
    onAuthorClick?: (item: ContentItemSearchItem) => void;

    // Sets the tag criterion.
    onTagClick?: (item: ContentItemSearchItem, tag: string) => void;

    // Bubbles: the page decides where a reference leads.
    onBibleReferenceClick?: (item: ContentItemSearchItem, bibleReference: string) => void;

    // The reader chose a reaction from the choices. The CONSUMER posts the create — or the
    // remove, when it is the one they already hold — and hands back a refreshed collection;
    // the panel holds no optimistic state.
    onReactionSelected?: (item: ContentItemSearchItem, reaction: ContentItemReactionOption) => void;

    // Bubbles: the page routes into the detail's comment section.
    onCommentsClick?: (item: ContentItemSearchItem) => void;

    // Same destination as onTitleClick, from the read-more affordance.
    onReadMoreClick?: (item: ContentItemSearchItem) => void;

    // Bubbles: the page routes to the detail surface where the item can be modified. The
    // control renders ONLY for the person who submitted the item (submittedById is the
    // viewer's own account id) — and rendering is all this decides: the server re-decides
    // authorization against the stored row on every write.
    onEditClick?: (item: ContentItemSearchItem) => void;

    // Bubbles: the page routes to the moderation detail surface. The control renders only
    // for the moderation tier — Administrators, Reviewers and Publishers at every §18.6
    // scope: global, ContentItem-, and ContentItem-{ContentType}- for the item's own type.
    onModerateClick?: (item: ContentItemSearchItem) => void;

    // The design's cards carry these too; rendered only when wired, like Edit.
    onShareClick?: (item: ContentItemSearchItem) => void;
    onSaveClick?: (item: ContentItemSearchItem) => void;
}

// The text every template renders from — threaded once so an override never drifts from the
// default's wording.
export interface ContentItemItemText {
    submittedByLabelText?: string;
    authorLabelText?: string;
    shareabilityLabelText?: string;
    dateLabelText?: string;
    likeButtonText?: string;
    commentsText?: string;
    commentsNoCountText?: string;
    shareButtonText?: string;
    saveButtonText?: string;
    editButtonText?: string;
    readMoreText?: string;
    allReactionsText?: string;
    shareabilityBasisLabels?: Readonly<Record<ShareabilityBasis, string>>;
}

// Everything a template receives: the item, its RESOLVED policy, and the dispatching panel's
// own state and toggles. Templates render from this and decide nothing — which is what lets an
// override derive from the default by replacing only its content slot.
export interface ContentItemItemTemplateProps extends ContentItemItemEvents, ContentItemItemText {
    contentItem: ContentItemSearchItem;

    // The item's OWN effective setting, resolved by the panel (§6.4 / §12.5.2 rules 1-2), so a
    // template gates ShowTags, ShowBibleReferences, ShowComments and the title/author shaping
    // off one authoritative row.
    contentItemSetting?: ContentItemSetting;

    contentTypeName: string;

    // Already gated against the setting and love-narrowed — empty means render no Like control.
    offeredReactions: ReadonlyArray<ContentItemReactionOption>;

    // The action-button decisions, made ONCE in the dispatching panel — ownership for Edit,
    // the moderation tier for Moderate, and the isModeratedView styling — so a template only
    // ever renders them. moderateButtonIconCss / moderateButtonLabel arrive resolved: the
    // shield and 'Moderate' on an ordinary surface, the pencil and 'Edit' on a moderated one
    // where Moderate stands alone wearing Edit's clothes.
    showsEditButton: boolean;
    showsModerateButton: boolean;
    moderateButtonIconCss: string;
    moderateButtonLabel: string;

    // The two render toggles the dispatching panel owns.
    areReactionCountsExpanded: boolean;
    onAssignedReactionsClick: () => void;
    isReactionPickerOpen: boolean;
    onReactionClick: () => void;
}

// THE TYPE CHIP CARRIES NO COLOUR TABLE HERE. The colour of a content type lives in
// contentItems.css and nowhere else — the chip renders the enum member name into
// data-content-type and the measured palette does the rest, pinned by
// contentItemChipPalette.test.ts. A second table in TypeScript is exactly the drift that
// rule exists to prevent.

// A row that is not yet public wears its status; the colour says which kind of not-yet. Approved
// is absent on purpose — it is the ordinary case, and a badge on every card would say nothing.
export const approvalStatusBadgeLabels: Readonly<Record<number, string>> = {
    [ApprovalStatus.Draft]: 'Draft',
    [ApprovalStatus.Submitted]: 'In review',
    [ApprovalStatus.Rejected]: 'Rejected',
    [ApprovalStatus.Dismissed]: 'Dismissed'
};

export const approvalStatusBadgeCssClasses: Readonly<Record<number, string>> = {
    [ApprovalStatus.Draft]: 'text-bg-secondary',
    [ApprovalStatus.Submitted]: 'text-bg-warning',
    [ApprovalStatus.Rejected]: 'text-bg-danger',
    [ApprovalStatus.Dismissed]: 'text-bg-secondary'
};

// How each basis reads on the meta row: the READ labels the shareability split defined — a
// reader wants the LICENCE, and who wrote it is already answered by the Submitted by and Author
// segments beside it. One table, shared with the detail panel, so the two surfaces cannot drift.
export const defaultShareabilityBasisLabels: Readonly<Record<ShareabilityBasis, string>> =
    shareabilityBasisReadLabels;
