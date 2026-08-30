import { ChangeEvent, useEffect, useId, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { Avatar } from '../coreUI/avatar';
import { formatDate } from '../coreUI/dateFormats';
import SearchBarComponent from '../coreUI/searchBar';
import { Spinner } from '../coreUI/spinner';
import { TagPillList } from '../coreUI/tagPillList';
import { ContentItemSetting } from '../../models/foundations/contentItemSettings/contentItemSetting';
import { ContentType } from '../../models/foundations/contentItemSettings/contentType';

import {
    contentTypeNameOf,
    resolveContentItemSetting
} from '../../services/views/contentItems/resolveContentItemSetting';

import {
    ApprovalStatus,
    ContentItemReactionOption,
    ContentItemSearchCriteria,
    ContentItemSearchItem
} from '../../models/components/contentItems/contentItemSearchItem';

import './contentItems.css';

// MANY content items, searched and scrolled. The sibling of ContentItemDetailPanel, which renders
// ONE — and the reason that one was renamed.
//
// A PURE PRESENTATION COMPONENT, on the same contract as ContentItemDetailPanel, AssociationPanel
// and ReviewPanel: props in, events out, no fetching, no mutation, no sockets. That contract is
// what decides most of what follows, and it is the reason the panel takes a collection rather
// than a query.
//
// IT DOES NOT KNOW WHAT IS BEHIND THE COLLECTION, and must not. The same panel serves a public
// feed (GET api/ContentItems/Public), a contributor's own rows and a moderation queue — three
// pages over one component. So it never filters, never decides visibility, and never turns a
// search into a request: it raises onSearch and renders whatever comes back. A panel that filtered
// what it was handed would be deciding, badly and twice, something the server has already decided
// against the stored row.
//
// TWO RENDERS, CHOSEN BY CONTENT TYPE. A Quote gets the full-width hero card, showing the quote
// WHOLE, because a quote is short enough to fit and to form an opinion on. Every other type gets
// the horizontal row: thumbnail, title, excerpt, pills, byline. The split is on the type rather
// than on position — the hero is what a quote LOOKS like, so a page of quotes is a page of heroes
// and not one hero above a list.
//
// WHERE AN OPINION MAY BE GIVEN follows from what the reader can actually see. A quote may be
// reacted to in place. Everything else routes into the detail view first, because you cannot form
// an opinion on an excerpt, and a like offered beside three sentences of a six-part study invites
// exactly that. Commenting always routes into the detail view, on both renders: there is no room
// for a thread here and no honest way to show one.
//
// FRESHNESS AND PERSISTENCE BELONG TO THE CONSUMER. onReacted is raised, never posted; onLoadMore
// is raised, never fetched. The panel shows the world as of the last props it was handed.
export interface ContentItemSearchPanelProps {
    // ── Subject ───────────────────────────────────────────────────────────────
    // The page of results as they stand. On an infinite scroll this is the ACCUMULATED list, not
    // the last page — the panel appends nothing of its own and holds no results of its own.
    contentItemCollection?: ReadonlyArray<ContentItemSearchItem>;

    // The ContentItemSetting rows the consumer already holds. Each card resolves ITS OWN effective
    // row through the shared §6.4 / §12.5.2 resolver, so a mixed collection is safe: one item's
    // override is never applied to another's, and a soft-deleted row is excluded entirely (§6.6).
    //
    // What is read here is the facet pairs that decide which surfaces a card offers — ShowTags,
    // ShowReactions / ReactionsAllowed, ShowComments, ShowBibleReferences,
    // LimitReactionsToLoveOnly (§6.5) — and the type's presentation: ContentTypeName and
    // ContentTypeIconCssClass. The Category box is built from the DEFAULT rows among them.
    contentItemSettingCollection?: ReadonlyArray<ContentItemSetting>;

    // ── Search ────────────────────────────────────────────────────────────────
    // Off leaves the list alone, which is right for a surface that has already decided what it is
    // showing — a topic's children, a contributor's own rows.
    showSearchBar?: boolean;

    // The criteria as they stood when Search was last pressed. Seeds the boxes and RESEEDS them
    // when it changes, so a page landing from ?q= shows what it searched for. The half-typed
    // version lives here in the panel; changing an advanced option does not re-run the search
    // until the button is pressed, matching the search page this bar came from.
    criteria?: ContentItemSearchCriteria;
    onSearch?: (criteria: ContentItemSearchCriteria) => void;

    // ── Paging ────────────────────────────────────────────────────────────────
    // The FIRST page. While it is on, the list is replaced by a spinner rather than being emptied,
    // so a re-search does not flash "nothing found" on its way to results.
    isLoading?: boolean;

    // A further page, on its way. Renders beneath the results and holds the sentinel back, so one
    // scroll is one fetch.
    isLoadingMore?: boolean;

    // Whether anything is left. The consumer knows this from its own paging — the OData reads
    // answer with a plain array and no total, so a page asks for one row beyond the page and
    // drops it, which is the only thing that separates a full last page from a page with more
    // behind it.
    hasMore?: boolean;

    // Raised when the foot of the list comes into view, and by the fallback button where
    // IntersectionObserver is not available. The panel never calls it while isLoadingMore is on.
    onLoadMore?: () => void;

    // ── Engagement ────────────────────────────────────────────────────────────
    // The reactions a reader may give. Empty — the default — means no card offers one, whatever
    // the settings say: a surface that cannot persist a reaction must not appear to accept one.
    reactionOptions?: ReadonlyArray<ContentItemReactionOption>;

    // Raised with the item and the option chosen. The CONSUMER posts it, decides whether a repeat
    // click is a retraction, and hands back a new collection — the panel re-renders from props and
    // holds no optimistic state of its own.
    onReacted?: (item: ContentItemSearchItem, reaction: ContentItemReactionOption) => void;

    // ── Presentation ──────────────────────────────────────────────────────────
    cssClass?: string;
    ariaLabel?: string;
    titleText?: string;

    // ── Text ──────────────────────────────────────────────────────────────────
    searchPlaceholderText?: string;
    categoryLabelText?: string;
    anyCategoryText?: string;
    authorLabelText?: string;
    authorPlaceholderText?: string;
    loadingText?: string;
    loadingMoreText?: string;
    loadMoreButtonText?: string;
    emptyText?: string;
    commentsLinkText?: string;
    readMoreText?: string;
    authorByText?: string;
}

// What a card's badge is coloured with. A presentation decision the panel owns: the setting
// carries an icon and a name but no colour, and picking one per card at random would make the
// same type look different on two pages. Stated as a Record so a new ContentType member fails to
// compile here rather than falling back to something unreadable.
const contentTypeBadgeCssClasses: Readonly<Record<ContentType, string>> = {
    [ContentType.Quote]: 'text-bg-success',
    [ContentType.Story]: 'text-bg-primary',
    [ContentType.Testimony]: 'text-bg-warning',
    [ContentType.Devotional]: 'text-bg-danger',
    [ContentType.BibleStudy]: 'text-bg-info',
    [ContentType.BlogPost]: 'text-bg-secondary',
    [ContentType.Series]: 'text-bg-dark',
    [ContentType.Topic]: 'text-bg-dark'
};

// A row that is not yet public wears its status, and the colour says which kind of not-yet it is.
// Approved is absent on purpose: an approved row is the ordinary case, and a badge on every card
// would say nothing.
const approvalStatusLabels: Readonly<Record<number, string>> = {
    [ApprovalStatus.Draft]: 'Draft',
    [ApprovalStatus.Submitted]: 'In review',
    [ApprovalStatus.Rejected]: 'Rejected',
    [ApprovalStatus.Dismissed]: 'Dismissed'
};

const approvalStatusBadgeCssClasses: Readonly<Record<number, string>> = {
    [ApprovalStatus.Draft]: 'text-bg-secondary',
    [ApprovalStatus.Submitted]: 'text-bg-warning',
    [ApprovalStatus.Rejected]: 'text-bg-danger',
    [ApprovalStatus.Dismissed]: 'text-bg-secondary'
};

export function ContentItemSearchPanel({
    contentItemCollection = [],
    contentItemSettingCollection = [],
    showSearchBar = true,
    criteria,
    onSearch,
    isLoading = false,
    isLoadingMore = false,
    hasMore = false,
    onLoadMore,
    reactionOptions = [],
    onReacted,
    cssClass = '',
    ariaLabel = 'Content items',
    titleText = '',
    searchPlaceholderText = 'Search posts, authors and topics',
    categoryLabelText = 'Category',
    anyCategoryText = 'Any category',
    authorLabelText = 'Author',
    authorPlaceholderText = 'Any author',
    loadingText = 'Loading…',
    loadingMoreText = 'Loading more…',
    loadMoreButtonText = 'Load more',
    emptyText = 'Nothing matched that search.',
    commentsLinkText = 'comments',
    readMoreText = 'Read and react',
    authorByText = 'by'
}: ContentItemSearchPanelProps) {
    const headingId = useId();
    const fieldId = useId();
    const sentinelRef = useRef<HTMLDivElement | null>(null);

    const [draftQuery, setDraftQuery] = useState(criteria?.query ?? '');
    const [draftAuthor, setDraftAuthor] = useState(criteria?.author ?? '');

    const [draftContentType, setDraftContentType] =
        useState<ContentType | null>(criteria?.contentType ?? null);

    // Keyed on the MEMBERS rather than on the object, so a consumer building the criteria inline
    // — which is the natural thing to do when they live in the URL — does not wipe what is being
    // typed on every render.
    useEffect(() => {
        setDraftQuery(criteria?.query ?? '');
        setDraftAuthor(criteria?.author ?? '');
        setDraftContentType(criteria?.contentType ?? null);
    }, [criteria?.query, criteria?.author, criteria?.contentType]);

    // Held in a ref so the observer below depends only on the paging state. Without it, a consumer
    // passing an inline arrow — again, the natural thing — would tear the observer down and build
    // it again on every render.
    const onLoadMoreRef = useRef(onLoadMore);

    useEffect(() => {
        onLoadMoreRef.current = onLoadMore;
    });

    // Progressive enhancement, read at render rather than at module load so a test (and a browser
    // without it) takes the same path the fallback button is rendered for.
    const supportsAutoLoad = typeof IntersectionObserver === 'function';

    // DEPENDS ON isLoadingMore ON PURPOSE. The observer is torn down while a page is in flight and
    // rebuilt when it lands, and observing fires an immediate callback — so a sentinel that is
    // still on screen after the new rows arrive asks for the next page. Reading the flag inside
    // the callback instead would stall the list: the sentinel never moves, so nothing would fire
    // again to un-stick it.
    useEffect(() => {
        const sentinel = sentinelRef.current;

        if (sentinel == null || hasMore === false || isLoadingMore || supportsAutoLoad === false) {
            return;
        }

        const observer = new IntersectionObserver(
            (entries) => {
                if (entries.some((entry) => entry.isIntersecting)) {
                    onLoadMoreRef.current?.();
                }
            },
            // Asks a screen early, so the next page is usually there before the reader arrives.
            { rootMargin: '200px 0px' });

        observer.observe(sentinel);

        return () => observer.disconnect();
    }, [hasMore, isLoadingMore, supportsAutoLoad, contentItemCollection.length]);

    // A soft-deleted row is excluded from active policy resolution (§6.6), so it never reaches any
    // resolution below — filtered once here rather than remembered at each use.
    const activeSettings =
        contentItemSettingCollection.filter((setting) => setting.isDeleted !== true);

    // The Category box offers the per-type DEFAULTS, ordered by the administrator's own SortOrder.
    // Deliberately NOT filtered by IsAvailableAsGeneralUserContribution the way the contribution
    // picker is: searching is not contributing, and a reader must be able to narrow to blog posts
    // whether or not they may write one.
    const filterableSettings = activeSettings
        .filter((setting) => setting.contentItemId == null)
        .sort((first, second) => first.sortOrder - second.sortOrder);

    const settingFor = (item: ContentItemSearchItem): ContentItemSetting | undefined =>
        resolveContentItemSetting(contentItemSettingCollection, item.contentType, item.id);

    const typeNameOf = (item: ContentItemSearchItem): string =>
        contentTypeNameOf(contentItemSettingCollection, item.contentType, item.id);

    const hrefOf = (item: ContentItemSearchItem): string => item.href ?? `/posts/${item.id}`;

    const search = () =>
        onSearch?.({
            query: draftQuery,
            contentType: draftContentType,
            author: draftAuthor
        });

    const onCategoryChanged = (event: ChangeEvent<HTMLSelectElement>) =>
        setDraftContentType(
            event.target.value.length === 0 ? null : Number(event.target.value) as ContentType);

    // What a card may offer, decided against ITS OWN effective row. Both halves of the §6.5 pair
    // are asked: ReactionsAllowed says the type accepts them at all, ShowReactions says this
    // surface renders them — and the panel adds a third condition of its own, that somebody is
    // listening, because a button whose event goes nowhere is worse than no button.
    const reactionsFor = (item: ContentItemSearchItem): ReadonlyArray<ContentItemReactionOption> => {
        const setting = settingFor(item);

        if (onReacted == null
            || reactionOptions.length === 0
            || setting?.reactionsAllowed === false
            || setting?.showReactions === false) {
            return [];
        }

        return setting?.limitReactionsToLoveOnly === true
            ? reactionOptions.filter((reaction) => reaction.isLove === true)
            : reactionOptions;
    };

    const showsComments = (item: ContentItemSearchItem): boolean =>
        settingFor(item)?.showComments !== false && item.commentCount != null;

    const showsTags = (item: ContentItemSearchItem): boolean =>
        settingFor(item)?.showTags !== false && (item.tags?.length ?? 0) > 0;

    const showsBibleReferences = (item: ContentItemSearchItem): boolean =>
        settingFor(item)?.showBibleReferences !== false
        && (item.bibleReferences?.length ?? 0) > 0;

    const renderStatusBadge = (item: ContentItemSearchItem) => {
        const status = item.approvalStatus;

        if (status == null || status === ApprovalStatus.Approved) {
            return null;
        }

        return (
            <span className={`badge ${approvalStatusBadgeCssClasses[status]} ms-2`}>
                {approvalStatusLabels[status]}
            </span>
        );
    };

    const renderPills = (item: ContentItemSearchItem) => {
        if (showsTags(item) === false && showsBibleReferences(item) === false) {
            return null;
        }

        return (
            <TagPillList
                tags={showsTags(item) ? item.tags : []}
                bibleReferences={
                    showsBibleReferences(item) ? item.bibleReferences : []} />
        );
    };

    const renderByline = (item: ContentItemSearchItem, isOverImage: boolean) => (
        <ul
            className={`nav nav-divider align-items-center small mb-0${isOverImage
                ? ' text-white-force'
                : ''}`}>

            {(item.contributorName ?? '').length > 0 && (
                <li className="nav-item">
                    <span className="d-inline-flex align-items-center">
                        <Avatar
                            name={item.contributorName ?? ''}
                            imageUrl={item.contributorImageUrl}
                            sizePx={24} />

                        <span className="ms-2">{authorByText} {item.contributorName}</span>
                    </span>
                </li>
            )}

            {item.publishedDate != null && (
                <li className="nav-item">{formatDate(item.publishedDate)}</li>
            )}

            {item.reactionCount != null && (
                <li className="nav-item">
                    <i className="far fa-heart me-1" aria-hidden="true"></i>{item.reactionCount}
                </li>
            )}
        </ul>
    );

    // THE ONLY PLACE A READER MAY ACT WITHOUT LEAVING THE LIST, and only on a quote. aria-pressed
    // rather than a disabled state, so a reader who has already reacted can see which one they
    // chose and change their mind.
    const renderReactions = (item: ContentItemSearchItem) => {
        const reactions = reactionsFor(item);

        if (reactions.length === 0) {
            return null;
        }

        return (
            <div
                className="d-flex flex-wrap align-items-center gap-2 mt-3"
                role="group"
                aria-label={`React to ${typeNameOf(item)}`}>

                {reactions.map((reaction) => (
                    <button
                        key={reaction.label}
                        type="button"
                        className={`g2h-content-item-reaction${item.viewerReactionLabel === reaction.label
                            ? ' g2h-content-item-reaction-given'
                            : ''}`}
                        aria-pressed={item.viewerReactionLabel === reaction.label}
                        aria-label={reaction.label}
                        title={reaction.label}
                        onClick={() => onReacted?.(item, reaction)}>

                        <span aria-hidden="true">{reaction.glyph}</span>
                    </button>
                ))}
            </div>
        );
    };

    // The comment count is a LINK INTO THE DETAIL VIEW on both renders. There is no room for a
    // thread beside a card and no honest way to show one, so the count says how many there are
    // and the page that can show them is one click away.
    const renderCommentsLink = (item: ContentItemSearchItem) => {
        if (showsComments(item) === false) {
            return null;
        }

        return (
            <Link to={hrefOf(item)} className="btn-link text-reset small">
                <i className="far fa-comment me-1" aria-hidden="true"></i>
                {item.commentCount} {commentsLinkText}
            </Link>
        );
    };

    // THE QUOTE RENDER. The quote itself is the card's heading — there is no title on a quote, and
    // showing the words whole is what makes reacting in place fair.
    //
    // A card-body IN NORMAL FLOW rather than the theme's absolutely positioned card-img-overlay,
    // and neither is an accident. The overlay only has a height where a .card-grid ancestor gives
    // the card one, which a scrolling list does not — and even given one, a long quote would
    // overflow a fixed height rather than grow the card. This body sets the card's height from its
    // own content and lifts itself over the gradient the theme draws behind it.
    //
    // No stretched-link either, unlike the theme's own hero card: this one carries its own
    // buttons, and a link covering the whole card would swallow every one of them.
    const renderQuoteCard = (item: ContentItemSearchItem) => (
        <article
            key={item.id}
            className="card card-overlay-bottom card-bg-scale g2h-content-item-hero mb-4"
            style={{
                backgroundImage:
                    (item.imageUrl ?? '').length > 0 ? `url(${item.imageUrl})` : undefined
            }}>

            <div className="card-body d-flex flex-column justify-content-end p-3 p-sm-4">
                <div className="w-100 mt-auto">
                    <span className={`badge ${contentTypeBadgeCssClasses[item.contentType]} mb-2`}>
                        <i
                            className={`${settingFor(item)?.contentTypeIconCssClass ?? 'bi-quote'} me-2`}
                            aria-hidden="true"></i>
                        {typeNameOf(item)}
                    </span>

                    {renderStatusBadge(item)}

                    <h3 className="text-white h4">
                        <Link to={hrefOf(item)} className="btn-link text-reset">
                            {item.content}
                        </Link>

                        {/* text-white rather than the theme's text-white-force, which colours
                            DESCENDANTS (`.text-white-force *`) and so does nothing to the element
                            carrying it. The h6 class sets its own heading colour, which is what
                            has to be beaten here. */}
                        {(item.author ?? '').length > 0 && (
                            <span className="d-block h6 mt-2 text-white">
                                — {item.author}
                            </span>
                        )}
                    </h3>

                    {renderPills(item)}
                    {renderByline(item, true)}

                    <div className="d-flex flex-wrap align-items-center gap-3 mt-2">
                        {renderCommentsLink(item)}
                    </div>

                    {renderReactions(item)}
                </div>
            </div>
        </article>
    );

    // EVERY OTHER TYPE. Thumbnail on the left, the item on the right, and no reaction control:
    // what is on screen is an excerpt, and an opinion on an excerpt is not an opinion on the item.
    // The way in is the title and the "read and react" link beneath it.
    const renderListCard = (item: ContentItemSearchItem) => (
        <article key={item.id} className="card mb-4">
            <div className="row g-0">
                {(item.imageUrl ?? '').length > 0 && (
                    <div className="col-md-4 position-relative">
                        <img
                            className="rounded-3 h-100 w-100 object-fit-cover"
                            src={item.imageUrl}
                            alt="" />

                        <span
                            className={`badge ${contentTypeBadgeCssClasses[item.contentType]}
                                position-absolute bottom-0 start-0 m-3`}>
                            <i
                                className={`${settingFor(item)?.contentTypeIconCssClass ?? 'bi-file-text'} me-2`}
                                aria-hidden="true"></i>
                            {typeNameOf(item)}
                        </span>
                    </div>
                )}

                <div className={(item.imageUrl ?? '').length > 0 ? 'col-md-8' : 'col-12'}>
                    <div className="card-body h-100 d-flex flex-column">
                        {(item.imageUrl ?? '').length === 0 && (
                            <span
                                className={`badge ${contentTypeBadgeCssClasses[item.contentType]}
                                    align-self-start mb-2`}>
                                {typeNameOf(item)}
                            </span>
                        )}

                        <h3 className="card-title h5">
                            <Link to={hrefOf(item)} className="btn-link text-reset fw-bold">
                                {(item.title ?? '').length > 0 ? item.title : typeNameOf(item)}
                            </Link>

                            {renderStatusBadge(item)}
                        </h3>

                        {/* The author of the WORDS, when the type carries one. Its own line
                            rather than a place in the byline below, which names the contributor
                            — on a story those are two different people, and running them
                            together would credit the wrong one. */}
                        {(item.author ?? '').length > 0
                            && settingFor(item)?.hasAuthor !== false && (
                                <p className="small mb-2">{authorByText} {item.author}</p>
                            )}

                        <p className="card-text g2h-content-item-excerpt">
                            {(item.excerpt ?? '').length > 0 ? item.excerpt : item.content}
                        </p>

                        {renderPills(item)}

                        <div className="mt-auto">
                            {renderByline(item, false)}

                            <div className="d-flex flex-wrap align-items-center gap-3 mt-2">
                                <Link
                                    to={hrefOf(item)}
                                    className="btn btn-xs btn-primary-soft mb-0">
                                    {readMoreText}
                                </Link>

                                {renderCommentsLink(item)}
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </article>
    );

    return (
        <section
            className={`g2h-content-item-search-panel ${cssClass}`}
            aria-label={titleText.length > 0 ? undefined : ariaLabel}
            aria-labelledby={titleText.length > 0 ? headingId : undefined}>

            {titleText.length > 0 && (
                <h2 className="h5 mb-3" id={headingId}>{titleText}</h2>
            )}

            {showSearchBar && (
                <div className="mb-4">
                    <SearchBarComponent
                        query={draftQuery}
                        onQueryChange={setDraftQuery}
                        onSearch={search}
                        placeholder={searchPlaceholderText}
                        advanced={
                            <div className="row g-3">
                                <div className="col-sm-6">
                                    <label
                                        className="form-label"
                                        htmlFor={`${fieldId}-category`}>
                                        {categoryLabelText}
                                    </label>

                                    <select
                                        className="form-select"
                                        id={`${fieldId}-category`}
                                        value={draftContentType == null
                                            ? ''
                                            : String(draftContentType)}
                                        onChange={onCategoryChanged}>

                                        <option value="">{anyCategoryText}</option>

                                        {filterableSettings.map((setting) => (
                                            <option
                                                key={setting.contentType}
                                                value={String(setting.contentType)}>
                                                {setting.contentTypeName}
                                            </option>
                                        ))}
                                    </select>
                                </div>

                                {/* Free text rather than a list, exactly as the search page has
                                    it: there is no useful upper bound on the number of authors,
                                    and this box asks about the AUTHOR OF THE WORDS rather than
                                    whoever contributed the row.

                                    The Tags box the search page carries is deliberately absent.
                                    Associations have no HTTP exposer yet (#318), so a tag filter
                                    would be a control that does nothing — and one that could only
                                    ever narrow the pages already loaded, which on an infinite
                                    scroll is a filter that quietly lies. */}
                                <div className="col-sm-6">
                                    <label
                                        className="form-label"
                                        htmlFor={`${fieldId}-author`}>
                                        {authorLabelText}
                                    </label>

                                    <input
                                        className="form-control"
                                        type="text"
                                        id={`${fieldId}-author`}
                                        placeholder={authorPlaceholderText}
                                        value={draftAuthor}
                                        onChange={(event) => setDraftAuthor(event.target.value)} />
                                </div>
                            </div>
                        } />
                </div>
            )}

            {isLoading ? (
                <div className="text-center py-5">
                    <Spinner />
                    <p className="mt-2 mb-0">{loadingText}</p>
                </div>
            ) : contentItemCollection.length === 0 ? (
                <div className="alert alert-info mb-0" role="status">{emptyText}</div>
            ) : (
                <>
                    {contentItemCollection.map((item) =>
                        item.contentType === ContentType.Quote
                            ? renderQuoteCard(item)
                            : renderListCard(item))}

                    {/* The sentinel is rendered whenever there is more, even where the observer
                        cannot be built — it costs nothing, and rendering it conditionally on
                        support would mean two different trees to reason about.

                        IT CARRIES A HEIGHT, and that is the whole point of the class. A bare
                        <div> here is zero-area, and an observer over a zero-area target is
                        unreliable — engines differ on whether an empty intersection rectangle
                        counts as intersecting at all, which is a list that silently stops
                        loading rather than an error anyone would see. One pixel is enough. */}
                    {hasMore && (
                        <div
                            ref={sentinelRef}
                            className="g2h-content-item-sentinel"
                            aria-hidden="true"></div>
                    )}

                    {isLoadingMore && (
                        <div className="text-center py-3" role="status">
                            <Spinner />
                            <p className="mt-2 mb-0">{loadingMoreText}</p>
                        </div>
                    )}

                    {/* The way out of a dead end. Without IntersectionObserver nothing would ever
                        ask for the next page, and the list would simply stop with no explanation. */}
                    {hasMore && isLoadingMore === false && supportsAutoLoad === false && (
                        <div className="text-center">
                            <button
                                type="button"
                                className="btn btn-outline-primary mb-0"
                                onClick={() => onLoadMore?.()}>
                                {loadMoreButtonText}
                            </button>
                        </div>
                    )}
                </>
            )}
        </section>
    );
}
