import { ApprovalStatus } from '../associations/associationItem';
import { ContentType } from '../../foundations/contentItemSettings/contentType';

export { ApprovalStatus };

// One row in a list of content items, projected down to what a CARD renders. The sibling of
// ContentItemFormItem, and separate from it for the same reason that one is separate from the
// wire entity: a list card needs an image, a contributor's face and two engagement counts that no
// ContentItem column carries, and it does not need the shareability basis or the permission note
// that only a form has any use for.
//
// A page projects whatever it holds down to this shape, so ContentItemSearchPanel never depends
// on the wire entity — the same split AssociationItem and ApprovalReviewItem take.
export type ContentItemSearchItem = {
    // The panel's React key, and what the default detail href is built from.
    id: string;

    // Decides WHICH render the item gets — the hero treatment for a quote, the horizontal row for
    // everything else — and which effective ContentItemSetting is resolved for it. The numeric
    // member, never a display name, exactly as ContentItemFormItem carries it.
    contentType: ContentType;

    // Absent on a type whose effective setting carries no title (a quote). The panel does not
    // invent one: a card with no title leads with its content instead.
    title?: string;

    // The AUTHOR OF THE WORDS — "D. L. Moody" — which is a different person from whoever
    // contributed the row. Absent on the types whose setting carries no author.
    author?: string;

    // The item's own text. The hero render shows a quote WHOLE, because a quote is short enough
    // to form an opinion on; the row render shows `excerpt` instead.
    content: string;

    // What a row shows in place of the full content. Left to the consumer rather than truncated
    // here: a summary somebody wrote beats the first 140 characters of anything, and when it is
    // absent the panel falls back to the content itself.
    excerpt?: string;

    // The card's thumbnail — the hero's background, the row's left column. NOTHING FETCHES THIS:
    // ContentItem carries no image column and Attachment has no exposer yet, so the consumer
    // supplies whatever it has, today a per-content-type placeholder. A card with no image drops
    // the thumbnail rather than rendering a broken one.
    imageUrl?: string;

    // WHO CONTRIBUTED IT, for the byline — a display name, unlike ContentItemFormItem.createdBy,
    // which is the account id the [OWNER] rule is decided on. Nothing here is an authorization
    // input, so a name is the right thing to carry.
    contributorName?: string;
    contributorImageUrl?: string;

    publishedDate?: Date;

    // Tags and bible references as the pills read them. Supplied by the consumer, which today has
    // no way to read them — Associations have no HTTP exposer (#318) — so they are normally
    // absent and the pill row simply does not render.
    tags?: ReadonlyArray<string>;
    bibleReferences?: ReadonlyArray<string>;

    // Each count is optional and an omitted one is LEFT OUT rather than shown as a zero, the rule
    // EngagementMeta already follows: a card must not claim a figure it does not have.
    reactionCount?: number;
    commentCount?: number;

    // The reaction this viewer has already given, by its ReactionOption label. Renders the
    // matching button as pressed, so a second click is visibly a change of mind rather than a
    // second vote.
    viewerReactionLabel?: string;

    // Drives the status badge on a non-public row. A public feed leaves it unset and no badge
    // appears; a moderation surface or a "my contributions" page sets it, because a draft that
    // looks published is the one thing a contributor must never be shown.
    approvalStatus?: ApprovalStatus;

    // Where the card leads. Defaults to the detail route the app already answers on, so a
    // consumer that routes items normally passes nothing.
    href?: string;
};

// What the search bar was set to when Search was last pressed. The panel holds the half-typed
// version itself and raises this on submit — an advanced option changed does not re-run the
// search until the button is pressed, matching the search page this bar came from.
export type ContentItemSearchCriteria = {
    // Free text. What it is matched against is the CONSUMER's decision: the panel neither filters
    // nor knows what read is behind the collection it renders.
    query: string;

    // The Category box. Null is "any category".
    contentType: ContentType | null;

    // The Author box — free text against the item's author, not its contributor.
    author: string;
};

export const emptyContentItemSearchCriteria: ContentItemSearchCriteria = {
    query: '',
    contentType: null,
    author: ''
};

// One reaction a reader may give from the list. Deliberately NOT coreUI's ReactionOption, which
// carries a count: a count belongs to ONE item, and this list is shared by every card the panel
// renders. The per-item total lives on ContentItemSearchItem.reactionCount.
export type ContentItemReactionOption = {
    // Identity as well as label. It is what comes back on onReacted and what
    // ContentItemSearchItem.viewerReactionLabel is matched against, so it must be stable.
    label: string;

    // Rendered as it stands. An emoji reads at any size, needs no icon font, and is what the
    // reaction row has always shown.
    glyph: string;

    // Marks the one option a love-only surface keeps. A content type whose effective setting
    // carries LimitReactionsToLoveOnly (§6.5) offers this and nothing else — matched on a flag
    // rather than on the label text, so renaming "Love" cannot silently empty the row.
    isLove?: boolean;
};
