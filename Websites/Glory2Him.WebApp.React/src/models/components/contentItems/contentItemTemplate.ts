import { ContentItemSetting } from '../../foundations/contentItemSettings/contentItemSetting';

import { shareabilityBasisReadLabels } from './contentItemFormItem';

import {
    ApprovalStatus,
    ContentItemReactionOption,
    ContentItemSearchItem,
    ShareabilityBasis
} from './contentItemSearchItem';

// The contract between ContentItemPanel and its templates. In a module of its own so the
// templates can state it without importing the panel that dispatches to them — the panel imports
// every template, and a template importing it back would be a cycle.

// Every event hook a card raises. Filter hooks are handled by ContentItemListPanel (they
// rewrite the committed criteria); navigation hooks bubble through it to the page, which owns
// the redirect — and stamps the origin into router state so the destination can offer a true
// way back.
export interface ContentItemEvents {
    // Toggles the Category criterion: set if clear, cleared if already this item's type.
    onContentTypeClick?: (item: ContentItemSearchItem) => void;

    // The way into the detail surface — WHICH detail surface is the page's decision: public
    // detail, my-content detail, or moderation detail. Whether the title is a way in AT ALL
    // is the surface's decision (allowTitleClick): a list allows it, a detail view does not.
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

    // THE REDIRECT read-more: raised by readMoreLinkButton, which renders only while
    // allowInPlaceExpansion is OFF and the content is cut — the page routes to the detail
    // surface, same destination as onTitleClick. Never shared with the in-place toggle.
    onReadMore?: (item: ContentItemSearchItem) => void;

    // THE IN-PLACE toggle: raised by expandCollapseLinkButton, which renders only while
    // allowInPlaceExpansion is ON and the content overruns the cut. ContentItemPanel owns
    // the expansion state and toggles it on this event; the hook still bubbles so a page
    // can observe it.
    onExpandCollapse?: (item: ContentItemSearchItem) => void;

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
export interface ContentItemText {
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
    expandLinkText?: string;
    showLessText?: string;
    allReactionsText?: string;
    shareabilityBasisLabels?: Readonly<Record<ShareabilityBasis, string>>;
}

// Everything a template receives: the item, its RESOLVED policy, and the dispatching panel's
// own state and toggles. Templates render from this and decide nothing — which is what lets an
// override derive from the default by replacing only its content slot.
// SECTION SWITCHES, separate from what the ContentItemSettings allow: the setting says what
// this item's TYPE shows, these say what this SURFACE has room for — a page standing tags
// and bible references in side panels turns the in-card sections off, so the same facts are
// never shown twice on one screen. All default TRUE, so the setting on the projection stays
// the deciding factor unless a surface specifically overrides it: a section renders only
// when BOTH agree.
export interface ContentItemSectionToggles {
    showTagSection?: boolean;
    showBibleReferenceSection?: boolean;
    showReactionSection?: boolean;
    showCommentsSection?: boolean;
    showShareSection?: boolean;
    showSaveSection?: boolean;
}

export interface ContentItemTemplateProps
    extends ContentItemEvents, ContentItemText, ContentItemSectionToggles {
    contentItem: ContentItemSearchItem;

    // The item's OWN effective setting, resolved by the panel (§6.4 / §12.5.2 rules 1-2), so a
    // template gates ShowTags, ShowBibleReferences, ShowComments and the title/author shaping
    // off one authoritative row.
    contentItemSetting?: ContentItemSetting;

    contentTypeName: string;

    // Already gated against the setting and love-narrowed — empty means render no Like control.
    offeredReactions: ReadonlyArray<ContentItemReactionOption>;

    // The action-button decisions, made ONCE in the dispatching panel — ownership for Edit,
    // the moderation tier for Moderate, and the showModerationSection styling — so a template only
    // ever renders them. moderateButtonIconCss / moderateButtonLabel arrive resolved: the
    // shield and 'Moderate' on an ordinary surface, the pencil and 'Edit' on a moderated one
    // where Moderate stands alone wearing Edit's clothes.
    showsEditButton: boolean;
    showsModerateButton: boolean;
    moderateButtonIconCss: string;
    moderateButtonLabel: string;

    // Whether the title (the quote or verse, on the faces that have no title) is a CONTROL:
    // resolved by the dispatching panel from its allowTitleClick prop. A template renders
    // the button only when this is on AND onTitleClick is listening; otherwise plain heading
    // text, with no underline — a detail surface's title leads nowhere.
    allowTitleClick: boolean;

    // Whether the card wears its approval-status corner ribbon — the surface's opt-in,
    // threaded down from ContentItemListPanel.
    showApprovalStatusRibbon: boolean;

    // Whether the card wears its approval-status PILL beside the type chip — the ribbon's
    // sibling opt-in, threaded the same way. Off (the default) shows no pill at all; on
    // shows the status on EVERY row, the ordinary Approved included, because a surface that
    // asks for statuses is asking for all of them.
    showApprovalStatus?: boolean;

    // THE CONTENT LENGTH DECISIONS, decided once in the dispatching panel. truncateAt is
    // the character position the content is cut at (with an ellipsis and the read-more
    // affordance) while isContentExpanded is off; allowInPlaceExpansion says whether the
    // read-more affordance TOGGLES the expansion in place — when it is off, read-more is
    // the page's onReadMore, the way into the detail surface.
    truncateAt: number;
    allowInPlaceExpansion: boolean;
    isContentExpanded: boolean;

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

// The status PILL's vocabulary, rendered only where a surface opted in with
// showApprovalStatus — and there EVERY status has an entry, Approved included: a surface
// that asks for statuses is asking for all of them. The Submitted pill and the Submitted
// ribbon say the same thing, so they wear the same warning yellow.
export const approvalStatusBadgeLabels: Readonly<Record<number, string>> = {
    [ApprovalStatus.Draft]: 'Draft',
    [ApprovalStatus.Submitted]: 'In review',
    [ApprovalStatus.Approved]: 'Approved',
    [ApprovalStatus.Rejected]: 'Rejected',
    [ApprovalStatus.Dismissed]: 'Dismissed'
};

export const approvalStatusBadgeCssClasses: Readonly<Record<number, string>> = {
    [ApprovalStatus.Draft]: 'text-bg-secondary',
    [ApprovalStatus.Submitted]: 'text-bg-warning',
    [ApprovalStatus.Approved]: 'text-bg-success',
    [ApprovalStatus.Rejected]: 'text-bg-danger',
    [ApprovalStatus.Dismissed]: 'text-bg-secondary'
};

// THE CORNER RIBBON'S vocabulary, rendered only where a surface opted in with
// showApprovalStatusRibbon. Unlike the badge, Approved IS present: a surface that asks for ribbons
// is asking for the status on every card, the ordinary case included. Dismissed stays
// absent — no shipped surface lists dismissed rows, and a colour for a state nobody shows
// would be dead vocabulary. The colours live in contentItems.css keyed by
// data-approval-status — the member NAME — exactly as the type chip's palette does.
//
// The member NAMES ride in their own map: ApprovalStatus here is a const object, not a
// TypeScript enum, so there is no reverse mapping to lean on the way the type chip leans
// on ContentType[value].
export const approvalStatusMemberNames: Readonly<Record<number, string>> = {
    [ApprovalStatus.Draft]: 'Draft',
    [ApprovalStatus.Submitted]: 'Submitted',
    [ApprovalStatus.Approved]: 'Approved',
    [ApprovalStatus.Rejected]: 'Rejected',
    [ApprovalStatus.Dismissed]: 'Dismissed'
};

export const approvalStatusRibbonLabels: Readonly<Record<number, string>> = {
    [ApprovalStatus.Draft]: 'Draft',
    [ApprovalStatus.Submitted]: 'Submitted',
    [ApprovalStatus.Approved]: 'Approved',
    [ApprovalStatus.Rejected]: 'Rejected'
};

// How each basis reads on the meta row: the READ labels the shareability split defined — a
// reader wants the LICENCE, and who wrote it is already answered by the Submitted by and Author
// segments beside it. One table, shared with the detail panel, so the two surfaces cannot drift.
export const defaultShareabilityBasisLabels: Readonly<Record<ShareabilityBasis, string>> =
    shareabilityBasisReadLabels;
