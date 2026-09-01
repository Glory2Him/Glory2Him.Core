import { useId } from 'react';
import { ContentItemResultsPanel } from './contentItemResultsPanel';
import { ContentItemSearchBarPanel } from './contentItemSearchBarPanel';
import { ContentItemSetting } from '../../models/foundations/contentItemSettings/contentItemSetting';

import {
    ContentItemEvents,
    ContentItemSectionToggles,
    ContentItemText
} from '../../models/components/contentItems/contentItemTemplate';

import {
    ContentItemReactionOption,
    ContentItemSearchCriteria,
    ContentItemSearchItem,
    emptyContentItemSearchCriteria
} from '../../models/components/contentItems/contentItemSearchItem';

import './contentItems.css';

// MANY content items, searched and scrolled — the composition of the family:
//
//   ContentItemListPanel
//   ├── ContentItemSearchBarPanel     the search bar and its advanced fold-out
//   └── ContentItemResultsPanel       the results, infinite scroll
//       └── ContentItemPanel      one result, dispatched to a template by content type
//           ├── ContentItemDefaultPanel
//           └── ContentItemQuotesPanel (and future ContentItem{ContentType}Panel overrides)
//
// EVERY COMPONENT IN THE FAMILY IS PRESENTATION ONLY: props in, events out, no fetching, no
// mutation, no sockets — the contract ContentItemFormPanel, AssociationPanel and ReviewPanel
// already share. The panel does not know what read is behind the collection it renders, and must
// not: the same family serves the public feed, "my posts" and the moderation queue, and the
// server decides what each caller may see against the stored row.
//
// WHAT THIS LEVEL ADDS is the filter semantics of the card hooks. A card raises
// onContentTypeClick, onSubmittedByClick, onAuthorClick or onTagClick; THIS panel rewrites the
// committed criteria accordingly and raises onSearch — so the consumer sees exactly one search
// signal however the reader asked. The navigation hooks (title, read-more, comments, bible
// reference, edit) pass straight through: where they lead is the page's decision, and the page
// stamps the origin into router state so the destination can offer a true way back.
// The six SECTION SWITCHES thread through too (ContentItemSectionToggles) — like
// showModerationSection and showApprovalStatusRibbon they are per-SURFACE decisions a page makes
// once for every card, and ContentItemPanel owns what each means. The form-face props
// (showEditSection, onModified, validationIssues…) are deliberately NOT here: they
// carry ONE item's write lifecycle, which no single list-level value can express — a
// list row's edit is a navigation (onEditClick).
export interface ContentItemListPanelProps
    extends ContentItemEvents, ContentItemText, ContentItemSectionToggles {
    // ── Subject ───────────────────────────────────────────────────────────────
    // The ACCUMULATED results — the consumer's infinite query keeps the pages. Each element
    // is SELF-CONTAINED: it carries the item and its winning setting, resolved by the
    // projection, so a card consults nothing beyond its own element — and updating one item
    // is one element swapped by the consumer, never a refetch of the list.
    contentItemCollection?: ReadonlyArray<ContentItemSearchItem>;

    // FOR THE CATEGORY BOX ALONE: the per-type default rows the bar offers as choices. The
    // cards never read this — their settings ride on their own elements.
    categorySettingCollection?: ReadonlyArray<ContentItemSetting>;

    // ── Search ────────────────────────────────────────────────────────────────
    // Off leaves the list alone — right for a surface that has already decided what it shows.
    showSearchBar?: boolean;

    criteria?: ContentItemSearchCriteria;
    onSearch?: (criteria: ContentItemSearchCriteria) => void;

    // ── Paging ────────────────────────────────────────────────────────────────
    isLoading?: boolean;
    isLoadingMore?: boolean;
    hasMore?: boolean;
    onLoadMore?: () => void;

    // ── Engagement ────────────────────────────────────────────────────────────
    reactionOptions?: ReadonlyArray<ContentItemReactionOption>;

    // ── Surface ───────────────────────────────────────────────────────────────
    // Whether this whole panel is a MODERATED surface. Off — the default — every card
    // offers Edit to its own submitter and Moderate (the shield) to the moderation tier.
    // On, Moderate stands alone on every card, wearing Edit's pencil and label.
    showModerationSection?: boolean;

    // Whether every card wears a corner ribbon naming its approval status: grey Draft,
    // yellow Submitted, green Approved, red Rejected — the colours in contentItems.css,
    // keyed by data-approval-status. Off by default.
    showApprovalStatusRibbon?: boolean;

    // The ribbon's sibling, threaded the same way: whether every card wears its
    // approval-status PILL beside the type chip. Off by default; on, every status shows.
    showApprovalStatus?: boolean;

    // The content-length trio, threaded to every card — ContentItemPanel owns what each
    // means: cut at truncateAt with the read-more affordance unless showContentExpanded,
    // and allowInPlaceExpansion turns read-more into an in-place expand/collapse toggle.
    showContentExpanded?: boolean;
    truncateAt?: number;
    allowInPlaceExpansion?: boolean;

    // ── Presentation ──────────────────────────────────────────────────────────
    cssClass?: string;
    ariaLabel?: string;
    titleText?: string;

    // ── Text ──────────────────────────────────────────────────────────────────
    searchPlaceholderText?: string;
    categoryLabelText?: string;
    anyCategoryText?: string;
    searchAuthorLabelText?: string;
    searchAuthorPlaceholderText?: string;
    loadingText?: string;
    loadingMoreText?: string;
    loadMoreButtonText?: string;
    emptyText?: string;
}

export function ContentItemListPanel({
    contentItemCollection = [],
    categorySettingCollection = [],
    showSearchBar = true,
    criteria,
    onSearch,
    isLoading = false,
    isLoadingMore = false,
    hasMore = false,
    onLoadMore,
    reactionOptions = [],
    showModerationSection = false,
    showApprovalStatusRibbon = false,
    showApprovalStatus = false,
    cssClass = '',
    ariaLabel = 'Content items',
    titleText = '',
    searchPlaceholderText = 'Search posts, authors and topics',
    categoryLabelText = 'Category',
    anyCategoryText = 'Any category',
    searchAuthorLabelText = 'Author',
    searchAuthorPlaceholderText = 'Any author',
    loadingText = 'Loading…',
    loadingMoreText = 'Loading more…',
    loadMoreButtonText = 'Load more',
    emptyText = 'Nothing matched that search.',
    onContentTypeClick,
    onSubmittedByClick,
    onAuthorClick,
    onTagClick,
    onBibleReferenceClick,
    ...itemEventsAndText
}: ContentItemListPanelProps) {
    const headingId = useId();

    const committedCriteria = criteria ?? emptyContentItemSearchCriteria;

    // THE FILTER HOOKS, given their meaning. Each rewrites the criteria and commits immediately
    // — a reader clicking a pill has already said what they want, so there is no Search press to
    // wait for. The consumer-supplied hook of the same name still fires afterwards, for a page
    // that wants to know (analytics, a scroll reset); it does not replace the behaviour.
    const contentTypeClicked = (item: ContentItemSearchItem) => {
        onSearch?.({
            ...committedCriteria,

            // A toggle: set if clear, cleared if the committed criterion is already this type.
            contentType: committedCriteria.contentType === item.contentType
                ? null
                : item.contentType
        });

        onContentTypeClick?.(item);
    };

    const submittedByClicked = (item: ContentItemSearchItem) => {
        if ((item.submittedById ?? '').length > 0) {
            onSearch?.({
                ...committedCriteria,
                submittedBy: {
                    id: item.submittedById ?? '',
                    name: item.submittedByName ?? ''
                }
            });
        }

        onSubmittedByClick?.(item);
    };

    const authorClicked = (item: ContentItemSearchItem) => {
        onSearch?.({ ...committedCriteria, author: item.author ?? '' });
        onAuthorClick?.(item);
    };

    // Toggles membership, the same register the Category toggle keeps: clicking a tag the
    // criteria already carry takes it back off.
    const tagClicked = (item: ContentItemSearchItem, tag: string) => {
        const alreadyListed = committedCriteria.tags.some(
            (listed) => listed.toLowerCase() === tag.toLowerCase());

        onSearch?.({
            ...committedCriteria,
            tags: alreadyListed
                ? committedCriteria.tags.filter(
                    (listed) => listed.toLowerCase() !== tag.toLowerCase())
                : [...committedCriteria.tags, tag]
        });

        onTagClick?.(item, tag);
    };

    const bibleReferenceClicked = (item: ContentItemSearchItem, bibleReference: string) => {
        const alreadyListed = committedCriteria.bibleReferences.some(
            (listed) => listed.toLowerCase() === bibleReference.toLowerCase());

        onSearch?.({
            ...committedCriteria,
            bibleReferences: alreadyListed
                ? committedCriteria.bibleReferences.filter(
                    (listed) => listed.toLowerCase() !== bibleReference.toLowerCase())
                : [...committedCriteria.bibleReferences, bibleReference]
        });

        onBibleReferenceClick?.(item, bibleReference);
    };

    return (
        <section
            className={`g2h-content-item-list-panel ${cssClass}`}
            aria-label={titleText.length > 0 ? undefined : ariaLabel}
            aria-labelledby={titleText.length > 0 ? headingId : undefined}>

            {titleText.length > 0 && (
                <h2 className="h5 mb-3" id={headingId}>{titleText}</h2>
            )}

            {showSearchBar && (
                <div className="mb-4">
                    <ContentItemSearchBarPanel
                        criteria={committedCriteria}
                        onSearch={onSearch}
                        contentItemSettingCollection={categorySettingCollection}
                        placeholderText={searchPlaceholderText}
                        categoryLabelText={categoryLabelText}
                        anyCategoryText={anyCategoryText}
                        authorLabelText={searchAuthorLabelText}
                        authorPlaceholderText={searchAuthorPlaceholderText} />
                </div>
            )}

            <ContentItemResultsPanel
                contentItemCollection={contentItemCollection}
                reactionOptions={reactionOptions}
                showModerationSection={showModerationSection}
                showApprovalStatusRibbon={showApprovalStatusRibbon}
                showApprovalStatus={showApprovalStatus}
                isLoading={isLoading}
                isLoadingMore={isLoadingMore}
                hasMore={hasMore}
                onLoadMore={onLoadMore}
                loadingText={loadingText}
                loadingMoreText={loadingMoreText}
                loadMoreButtonText={loadMoreButtonText}
                emptyText={emptyText}
                onContentTypeClick={contentTypeClicked}
                onSubmittedByClick={submittedByClicked}
                onAuthorClick={authorClicked}
                onTagClick={tagClicked}
                onBibleReferenceClick={bibleReferenceClicked}
                {...itemEventsAndText} />
        </section>
    );
}
