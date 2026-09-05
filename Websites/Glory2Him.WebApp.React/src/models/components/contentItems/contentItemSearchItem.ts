import { ApprovalStatus } from '../associations/associationItem';
import { ShareabilityBasis } from './contentItemFormItem';
import { ContentItemSetting } from '../../foundations/contentItemSettings/contentItemSetting';
import { ContentType } from '../../foundations/contentItemSettings/contentType';

export { ApprovalStatus, ShareabilityBasis };

// One row in a list of content items, projected down to what a CARD renders. The sibling of
// ContentItemFormItem, and separate from it for the same reason that one is separate from the
// wire entity: a card needs an image, the contributor's face and the engagement figures that no
// ContentItem column carries, and it does not need the permission note only a form has a use for.
//
// A page projects whatever it holds down to this shape, so the ContentItemListPanel family
// never depends on the wire entity — the same split AssociationItem and ApprovalReviewItem take.
//
// EACH ELEMENT IS SELF-CONTAINED: it carries the item AND its winning setting (and, as the
// association reads arrive, its tags, references and counts), so a card renders from its one
// element and consults nothing else. That is what makes an update CHEAP — a reaction given, a
// tag approved, a setting changed — the page swaps that ONE element in the collection and only
// that card re-renders; nothing is refetched wholesale.
export type ContentItemSearchItem = {
    // The template's React key, and what the page routes its detail redirects on.
    id: string;

    // Decides WHICH template renders the item — an override where one is registered, the default
    // otherwise. The numeric member, never a display name, exactly as ContentItemFormItem
    // carries it.
    contentType: ContentType;

    // THE WINNING SETTING, resolved by the PROJECTION (§6.4 / §12.5.2 rules 1-2: this item's
    // own override beats its type default, soft-deleted rows excluded §6.6) — so the card
    // gates every feature (ShowTags, ShowReactions, LimitReactionsToLoveOnly, ShowComments,
    // HasTitle, HasAuthor) off the one row that actually governs THIS item. Absent, the card
    // shapes itself from what the item carries, exactly as the detail panel falls back.
    contentItemSetting?: ContentItemSetting;

    // Absent on a type whose effective setting carries no title (a quote). The templates do not
    // invent one: a card with no title leads with its content instead.
    title?: string;

    // The AUTHOR OF THE WORDS — "William Temple" — which is a different person from whoever
    // submitted the row. Absent on the types whose setting carries no author.
    author?: string;

    // The item's own text, WHOLE. The quote template shows it entire — a quote is short
    // enough to form an opinion on — and the default template cuts it at the surface's
    // truncateAt with the read-more affordance, so no second excerpt field exists to drift
    // from the content it summarises.
    content: string;

    // The card's imagery — the quote hero's background, the default template's thumbnail.
    // NOTHING FETCHES THIS: ContentItem carries no image column and Attachment has no exposer, so
    // the consumer supplies whatever it has, today a per-content-type placeholder. A card with no
    // image drops the imagery rather than rendering a broken one.
    imageUrl?: string;

    // WHO SUBMITTED IT, for the "Submitted by" segment of the meta row. The NAME is what renders
    // and the ID is what onSubmittedByClick filters on — deliberately two members, because the id
    // is the account identifier the audit trail records and must never itself be rendered, while
    // two accounts can share one display name and a name is therefore nothing to filter on.
    submittedById?: string;
    submittedByName?: string;
    submittedByImageUrl?: string;

    // How the contributor is permitted to share it, for the meta row's Shareability segment.
    shareabilityBasis?: ShareabilityBasis;

    // The permission note behind a permission basis. A card never renders it, but the element
    // seeds ContentItemPanel's edit face, and an editor opened without it would silently drop
    // the note on the first save.
    sharePermission?: string;

    publishedDate?: Date;

    // Tags and bible references as the pills read them. Supplied by the consumer, which today has
    // no way to read them — Associations have no HTTP exposer (#318) — so they are normally
    // absent and the pill row simply does not render.
    tags?: ReadonlyArray<string>;
    bibleReferences?: ReadonlyArray<string>;

    // The reactions this item has already been given, per option. Drives both faces of the
    // assigned-reactions cluster: compact shows the glyphs and the summed total, expanded shows
    // each count. Absent or empty renders no cluster at all — a card claims no figure it does not
    // have, the rule EngagementMeta already follows.
    reactionSummary?: ReadonlyArray<ContentItemReactionCount>;

    // The reaction this viewer has already given, by its option label. Renders the matching
    // choice as pressed, so a second click is visibly a change of mind rather than a second vote.
    viewerReactionLabel?: string;

    // Comment count for the engagement row. Absent leaves the comments control out entirely.
    commentCount?: number;

    // Drives the status badge on a non-public row. A public feed leaves it unset and no badge
    // appears; a moderation surface or a "my posts" page sets it, because a draft that looks
    // published is the one thing a contributor must never be shown.
    approvalStatus?: ApprovalStatus;
};

// One glyph of the assigned-reactions cluster.
export type ContentItemReactionCount = {
    label: string;
    glyph: string;
    count: number;
};

// One reaction a reader may give — the choices behind the Like control. The page pulls these from
// GET api/Reactions (approved rows only) and hands them over; the panel invents none.
export type ContentItemReactionOption = {
    // Identity as well as label: what comes back on onReactionSelected and what
    // viewerReactionLabel is matched against, so it must be stable.
    label: string;

    // Rendered as it stands — the reaction row's UnicodeEmoji.
    glyph: string;

    // Marks the one option a love-only surface keeps. A content type whose effective setting
    // carries LimitReactionsToLoveOnly (§6.5) offers this and nothing else — a flag rather than a
    // match on the label text, so renaming "Love" cannot silently empty the row.
    isLove?: boolean;
};

// Who a submitted-by filter points at. The ID is what the read filters on; the NAME is what the
// filter chip shows — carried together because a criteria chip reading a bare account id would be
// rendering the one thing the id must never do.
export type ContentItemSubmittedByCriterion = {
    id: string;
    name: string;
};

// What the search stood at when it was last committed. The bar holds the half-typed version
// itself and commits on Search; the pill-click hooks commit immediately, because a reader
// clicking a tag has already said what they want.
export type ContentItemSearchCriteria = {
    // Free text. What it is matched against is the CONSUMER's decision: the panel neither filters
    // nor knows what read is behind the collection it renders.
    query: string;

    // The Category box, and what onContentTypeClick toggles. Null is "any category".
    contentType: ContentType | null;

    // The Author box — free text against the author of the WORDS, not the submitter.
    author: string;

    // The Submitted by box and onSubmittedByClick both land here. A PILL CLICK carries both
    // halves — the id the read filters on, the name the chip shows. A TYPED name carries an
    // empty id: the panel has no resolver from a display name to an account, so the page
    // filters on the id only when one is present (§16.7.4 keeps the resolver reviewer-gated).
    submittedBy: ContentItemSubmittedByCriterion | null;

    // The Tags box and onTagClick both land here: typed-and-entered or clicked on a card,
    // each tag wears a removable chip. Carried in the criteria so the family does not change
    // shape when #318 makes it servable; until then the page simply cannot act on it.
    tags: ReadonlyArray<string>;

    // Whether a row must carry ANY of the tags or ALL of them — the Any/All toggle beside
    // the Tags box. Meaningless while tags is empty.
    tagMatchMode: ContentItemTagMatchMode;

    // The Bible references box and onBibleReferenceClick both land here, behaving exactly
    // as the tags do — typed-and-entered or clicked on a card, each wearing a removable
    // chip — and equally blocked on #318 for the actual narrowing.
    bibleReferences: ReadonlyArray<string>;
    bibleReferenceMatchMode: ContentItemTagMatchMode;

    // The Shareability box. Null is "any shareability".
    shareabilityBasis: ShareabilityBasis | null;

    // The Approval status boxes — the checkbox group the bar renders only where a surface
    // opted in with showApprovalStatusSearchOptions. EMPTY IS "ANY STATUS", exactly as a
    // null contentType is "any category": a reader who has ticked nothing is asking for
    // everything the surface already shows them, not for nothing at all.
    //
    // A NARROWING, NEVER A WIDENING. The statuses a page PINS (the moderation queue's Draft
    // + Submitted) and the roles the foundation enforces (§14.5) both still stand — what is
    // ticked here narrows within them, so a reader cannot reach a row by ticking a box that
    // their surface was never showing.
    approvalStatuses: ReadonlyArray<ApprovalStatus>;
};

export type ContentItemTagMatchMode = 'any' | 'all';

// The statuses the search checkbox group offers, in the order they are rendered — the
// workflow's own order, which is also the numeric one. Dismissed is deliberately absent: no
// shipped surface lists dismissed rows, so a box that narrows to them would return nothing
// wherever it was ticked.
export const contentItemSearchApprovalStatusMembers: ReadonlyArray<ApprovalStatus> = [
    ApprovalStatus.Draft,
    ApprovalStatus.Submitted,
    ApprovalStatus.Approved,
    ApprovalStatus.Rejected
];

export const emptyContentItemSearchCriteria: ContentItemSearchCriteria = {
    query: '',
    contentType: null,
    author: '',
    submittedBy: null,
    tags: [],
    tagMatchMode: 'any',
    bibleReferences: [],
    bibleReferenceMatchMode: 'any',
    shareabilityBasis: null,
    approvalStatuses: []
};
