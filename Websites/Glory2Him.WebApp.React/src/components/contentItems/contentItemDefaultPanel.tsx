import { ReactNode } from 'react';
import { Avatar } from '../coreUI/avatar';
import { formatDate } from '../coreUI/dateFormats';

import { ContentType } from '../../models/foundations/contentItemSettings/contentType';

import {
    approvalStatusBadgeCssClasses,
    approvalStatusBadgeLabels,
    approvalStatusMemberNames,
    approvalStatusRibbonLabels,
    ContentItemTemplateProps,
    defaultShareabilityBasisLabels
} from '../../models/components/contentItems/contentItemTemplate';

import {
    ContentItemSearchItem
} from '../../models/components/contentItems/contentItemSearchItem';

import './contentItems.css';

// THE PINK BLOCKS: the template most content types render through. Type badge, the content
// block, the meta row (Submitted by | Author | Shareability | Date), the tag and reference
// pills, and the engagement row.
//
// AN OVERRIDE DERIVES FROM THIS TEMPLATE by rendering it with `contentSlot` replaced — the React
// register of inheritance — so the meta row, the pills and the engagement row are written once
// and every ContentItem{ContentType}Panel carries them identically. What an override may
// change is how the CONTENT reads; what it may not change is what the card offers.
//
// Every affordance here is an event, not a link: which surface a title, a comment count or a
// reference leads to is the PAGE's decision — the same card serves the public feed, "my posts"
// and the moderation queue — and a filter click is the criteria's business, not the router's.
export interface ContentItemDefaultPanelProps extends ContentItemTemplateProps {
    // The derivation point. Absent, the default content block renders: thumbnail, title,
    // the truncated content, read-more.
    contentSlot?: ReactNode;
}

export function ContentItemDefaultPanel({
    contentItem,
    contentItemSetting,
    contentTypeName,
    offeredReactions,
    areReactionCountsExpanded,
    onAssignedReactionsClick,
    isReactionPickerOpen,
    onReactionClick,
    onContentTypeClick,
    onTitleClick,
    onSubmittedByClick,
    onAuthorClick,
    onTagClick,
    onBibleReferenceClick,
    onReactionSelected,
    onCommentsClick,
    onReadMore,
    onExpandCollapse,
    onEditClick,
    onModerateClick,
    onShareClick,
    onSaveClick,
    showsEditButton,
    showsModerateButton,
    moderateButtonIconCss,
    moderateButtonLabel,
    contentSlot,
    showApprovalStatusRibbon,
    showApprovalStatus = false,
    truncateAt,
    allowInPlaceExpansion,
    isContentExpanded,
    showTagSection = true,
    showBibleReferenceSection = true,
    showReactionSection = true,
    showCommentsSection = true,
    showShareSection = true,
    showSaveSection = true,
    submittedByLabelText = 'Submitted by',
    authorLabelText = 'Author',
    shareabilityLabelText = 'Shareability',
    dateLabelText = 'Date',
    likeButtonText = 'Like',
    commentsText = 'comments',
    commentsNoCountText = 'Comments',
    shareButtonText = 'Share',
    saveButtonText = 'Save',
    editButtonText = 'Edit',
    // Four dots on the redirect, an ellipsis on the toggle — deliberately different, so
    // which affordance a card carries is visible at a glance.
    readMoreText = 'read more....',
    expandLinkText = 'read more…',
    showLessText = 'show less',
    allReactionsText = 'All',
    shareabilityBasisLabels = defaultShareabilityBasisLabels
}: ContentItemDefaultPanelProps) {
    const showsTitle =
        contentItemSetting?.hasTitle !== false && (contentItem.title ?? '').length > 0;

    const showsAuthor =
        contentItemSetting?.hasAuthor !== false && (contentItem.author ?? '').length > 0;

    // Every section asks BOTH questions: the surface switch (this page's room) AND the
    // setting on the projection (this type's policy). Both default open, so the setting
    // stays the deciding factor unless a surface specifically overrides it.
    const showsTags =
        showTagSection
        && contentItemSetting?.showTags !== false
        && (contentItem.tags?.length ?? 0) > 0;

    const showsBibleReferences =
        showBibleReferenceSection
        && contentItemSetting?.showBibleReferences !== false
        && (contentItem.bibleReferences?.length ?? 0) > 0;

    const reactionSummary = contentItem.reactionSummary ?? [];

    const showsAssignedReactions =
        showReactionSection
        && contentItemSetting?.showReactions !== false
        && reactionSummary.length > 0;

    // The count is optional — the comment reads have no exposer yet (#318) — but the way INTO
    // the comments is not: the control renders whenever the surface shows comments and somebody
    // is listening, counted or not.
    const showsComments =
        showCommentsSection
        && contentItemSetting?.showComments !== false
        && onCommentsClick != null;

    const totalReactions =
        reactionSummary.reduce((total, reaction) => total + reaction.count, 0);

    // The content-length pair the dispatcher's trio resolves to on THIS card: whether
    // there is anything beyond the cut at all, and whether the cut is currently applied.
    const isContentOverTruncateAt = contentItem.content.length > truncateAt;
    const isContentTruncated = isContentExpanded === false && isContentOverTruncateAt;

    // Whether the engagement row has anything to say. Today's shipped pages often have nothing —
    // no summaries or counts until #318, no wired Edit — and an empty flex row still spends its
    // margin, which reads as dead space at the foot of every card.
    const showsShare = showShareSection && onShareClick != null;
    const showsSave = showSaveSection && onSaveClick != null;

    const showsEngagementRow =
        showsAssignedReactions
        || offeredReactions.length > 0
        || showsComments
        || showsShare
        || showsSave
        || showsEditButton
        || showsModerateButton;

    // The corner ribbon: rendered on the card ROOT so every derived template wears it
    // identically, coloured by the stylesheet off the status member NAME — the same
    // colour-lives-in-CSS contract the type chip keeps. Dismissed has no ribbon entry, so
    // an unmapped status simply renders none rather than an unlabelled strip.
    const ribbonLabel =
        showApprovalStatusRibbon && contentItem.approvalStatus != null
            ? approvalStatusRibbonLabels[contentItem.approvalStatus]
            : undefined;

    // The status PILL: the surface's opt-in, like the ribbon — and where a surface opted
    // in, EVERY status wears one, the ordinary Approved included.
    const renderStatusBadge = (item: ContentItemSearchItem) => {
        const status = item.approvalStatus;

        if (showApprovalStatus === false || status == null) {
            return null;
        }

        return (
            <span className={`badge ${approvalStatusBadgeCssClasses[status]} ms-2`}>
                {approvalStatusBadgeLabels[status]}
            </span>
        );
    };

    // The type badge is the type FILTER — set if clear, cleared if already this type — and the
    // status badge beside it never leaves a draft looking published. WHERE the pair stands
    // depends on the template face: an override (the quote) wears it above its content block,
    // while the default block stands it beside the thumbnail, over the title.
    const badgeRow = (
        <div className="d-flex align-items-center mb-2">
            {/* THE COLOUR LIVES IN THE STYLESHEET AND NOWHERE ELSE: the chip renders the
                enum MEMBER NAME into data-content-type and the palette in contentItems.css
                does the rest — the same contract the detail panel's chip keeps, so one type
                is one colour on every surface. */}
            <button
                type="button"
                className="g2h-content-item-chip border-0"
                data-content-type={ContentType[contentItem.contentType]}
                onClick={() => onContentTypeClick?.(contentItem)}>
                <i className="fas fa-circle me-2 small" aria-hidden="true"></i>
                {contentTypeName}
            </button>

            {renderStatusBadge(contentItem)}
        </div>
    );

    // The default content block: the thumbnail on the left with the badge and the title stacked
    // BESIDE it — the chip belongs to the title column, not to a row of its own above the image —
    // then the content beneath, cut at truncateAt, and the way in at the end of it.
    const defaultContentSlot = (
        <>
            <div className="d-flex align-items-start gap-3">
                {(contentItem.imageUrl ?? '').length > 0 && (
                    <img
                        className="g2h-content-item-thumb rounded-3 object-fit-cover"
                        src={contentItem.imageUrl}
                        alt="" />
                )}

                <div className="align-self-center">
                    {badgeRow}

                    {/* A control only where somebody is listening: on a detail surface the
                        title leads nowhere, so it stands as plain heading text. */}
                    {showsTitle && (
                        <h3 className="h5 mb-0">
                            {onTitleClick != null ? (
                                <button
                                    type="button"
                                    className="btn btn-link text-reset fw-bold p-0 mb-0 text-start"
                                    onClick={() => onTitleClick(contentItem)}>
                                    {contentItem.title}
                                </button>
                            ) : contentItem.title}
                        </h3>
                    )}
                </div>
            </div>

            {/* TRUNCATED BY CHARACTERS, decided by the dispatcher's trio — and TWO link
                buttons that are never shared: allowInPlaceExpansion picks exactly one.
                OFF → readMoreLinkButton ('read more....') raises onReadMore and the page
                routes to the detail surface. ON → expandCollapseLinkButton ('read more…' /
                'show less') raises onExpandCollapse and the dispatcher toggles in place. */}
            <p className="card-text mt-2 mb-0">
                {isContentTruncated
                    ? `${contentItem.content.slice(0, truncateAt).trimEnd()}…`
                    : contentItem.content}

                {isContentTruncated && allowInPlaceExpansion === false && (
                    <>
                        {' '}
                        <button
                            type="button"
                            className="btn btn-link fw-bold p-0 mb-0 align-baseline"
                            onClick={() => onReadMore?.(contentItem)}>
                            {readMoreText}
                        </button>
                    </>
                )}

                {allowInPlaceExpansion && isContentOverTruncateAt && (
                    <>
                        {' '}
                        <button
                            type="button"
                            className="btn btn-link fw-bold p-0 mb-0 align-baseline"
                            onClick={() => onExpandCollapse?.(contentItem)}>
                            {isContentExpanded ? showLessText : expandLinkText}
                        </button>
                    </>
                )}
            </p>
        </>
    );

    return (
        <article
            className={`card border p-3 mb-3 g2h-content-item-card${
                ribbonLabel != null ? ' g2h-has-approval-ribbon' : ''}`}>
            {ribbonLabel != null && (
                <span
                    className="g2h-approval-ribbon"
                    data-approval-status={approvalStatusMemberNames[contentItem.approvalStatus!]}>
                    {ribbonLabel}
                </span>
            )}

            {contentSlot != null ? (
                <>
                    {badgeRow}
                    {contentSlot}
                </>
            ) : defaultContentSlot}

            {/* The meta row. Each segment renders only when its member is present, and the two
                PEOPLE are two different filters: the submitter contributed the row, the author
                said the words. */}
            <ul className="g2h-content-item-meta list-unstyled d-flex flex-wrap align-items-center mt-3 mb-0">
                {(contentItem.submittedByName ?? '').length > 0 && (
                    <li>
                        <button
                            type="button"
                            className="btn btn-link text-reset p-0 mb-0 d-inline-flex align-items-center"
                            onClick={() => onSubmittedByClick?.(contentItem)}>
                            <Avatar
                                name={contentItem.submittedByName ?? ''}
                                imageUrl={contentItem.submittedByImageUrl}
                                sizePx={32} />
                            <span className="ms-2">
                                {submittedByLabelText}{' '}
                                <strong>{contentItem.submittedByName}</strong>
                            </span>
                        </button>
                    </li>
                )}

                {showsAuthor && (
                    <li>
                        <button
                            type="button"
                            className="btn btn-link text-reset p-0 mb-0"
                            onClick={() => onAuthorClick?.(contentItem)}>
                            {authorLabelText} <strong>{contentItem.author}</strong>
                        </button>
                    </li>
                )}

                {contentItem.shareabilityBasis != null && (
                    <li>
                        {shareabilityLabelText}{' '}
                        <strong>{shareabilityBasisLabels[contentItem.shareabilityBasis]}</strong>
                    </li>
                )}

                {contentItem.publishedDate != null && (
                    <li>
                        {dateLabelText} <strong>{formatDate(contentItem.publishedDate)}</strong>
                    </li>
                )}
            </ul>

            {/* The pills, as filters and as references. A tag narrows THIS list; a reference
                leads somewhere, and where is the page's call. */}
            {(showsTags || showsBibleReferences) && (
                <div className="d-flex flex-wrap align-items-center gap-2 mt-3">
                    {showsTags && (contentItem.tags ?? []).map((tag) => (
                        <button
                            key={tag}
                            type="button"
                            className="btn btn-xs btn-success-soft mb-0"
                            onClick={() => onTagClick?.(contentItem, tag)}>
                            #{tag}
                        </button>
                    ))}

                    {showsBibleReferences && (contentItem.bibleReferences ?? []).map((reference) => (
                        <button
                            key={reference}
                            type="button"
                            className="btn btn-xs btn-primary-soft mb-0"
                            onClick={() => onBibleReferenceClick?.(contentItem, reference)}>
                            <i className="bi bi-book me-1" aria-hidden="true"></i>{reference}
                        </button>
                    ))}
                </div>
            )}

            {/* The engagement row — only when it has anything to offer. */}
            {showsEngagementRow && (
            <div className="d-flex flex-wrap align-items-center gap-3 mt-3">
                {showsAssignedReactions && (
                    <button
                        type="button"
                        className="btn btn-link text-reset p-0 mb-0 d-inline-flex align-items-center gap-2"
                        aria-expanded={areReactionCountsExpanded}
                        aria-label="Reaction counts"
                        onClick={onAssignedReactionsClick}>

                        {areReactionCountsExpanded ? (
                            <>
                                <strong className="text-primary">
                                    {allReactionsText} {totalReactions}
                                </strong>

                                {reactionSummary.map((reaction) => (
                                    <span key={reaction.label} title={reaction.label}>
                                        <span aria-hidden="true">{reaction.glyph}</span>
                                        {' '}{reaction.count}
                                    </span>
                                ))}
                            </>
                        ) : (
                            <>
                                <span>
                                    {reactionSummary.map((reaction) => (
                                        <span key={reaction.label} title={reaction.label}>
                                            {reaction.glyph}
                                        </span>
                                    ))}
                                </span>
                                <span>{totalReactions}</span>
                            </>
                        )}
                    </button>
                )}

                {offeredReactions.length > 0 && (
                    <span className="position-relative">
                        <button
                            type="button"
                            className="btn btn-link text-reset p-0 mb-0"
                            aria-expanded={isReactionPickerOpen}
                            aria-haspopup="true"
                            onClick={onReactionClick}>
                            <i className="bi bi-hand-thumbs-up me-1" aria-hidden="true"></i>
                            {likeButtonText}
                        </button>

                        {isReactionPickerOpen && (
                            <span
                                className="g2h-content-item-reaction-picker shadow rounded-pill bg-body"
                                role="menu"
                                aria-label="Choose a reaction">
                                {offeredReactions.map((reaction) => (
                                    <button
                                        key={reaction.label}
                                        type="button"
                                        role="menuitem"
                                        className={`g2h-content-item-reaction-choice${contentItem.viewerReactionLabel === reaction.label
                                            ? ' g2h-content-item-reaction-given'
                                            : ''}`}
                                        aria-pressed={
                                            contentItem.viewerReactionLabel === reaction.label}
                                        aria-label={reaction.label}
                                        title={reaction.label}
                                        onClick={() => onReactionSelected?.(contentItem, reaction)}>
                                        <span aria-hidden="true">{reaction.glyph}</span>
                                    </button>
                                ))}
                            </span>
                        )}
                    </span>
                )}

                {showsComments && (
                    <button
                        type="button"
                        className="btn btn-link text-reset p-0 mb-0"
                        onClick={() => onCommentsClick?.(contentItem)}>
                        <i className="far fa-comment me-1" aria-hidden="true"></i>
                        {contentItem.commentCount != null
                            ? `${contentItem.commentCount} ${commentsText}`
                            : commentsNoCountText}
                    </button>
                )}

                {showsShare && (
                    <button
                        type="button"
                        className="btn btn-link text-reset p-0 mb-0"
                        onClick={() => onShareClick(contentItem)}>
                        <i className="bi bi-share me-1" aria-hidden="true"></i>{shareButtonText}
                    </button>
                )}

                {showsSave && (
                    <button
                        type="button"
                        className="btn btn-link text-reset p-0 mb-0"
                        onClick={() => onSaveClick(contentItem)}>
                        <i className="bi bi-bookmark me-1" aria-hidden="true"></i>{saveButtonText}
                    </button>
                )}

                {/* The actions on the right — Edit for the item's own submitter, Moderate
                    for the moderation tier, both DECIDED in ContentItemPanel and only
                    rendered here. On a moderated surface Moderate arrives alone, wearing
                    Edit's icon and label. */}
                {(showsEditButton || showsModerateButton) && (
                    <span className="ms-auto d-inline-flex align-items-center gap-3">
                        {showsEditButton && (
                            <button
                                type="button"
                                className="btn btn-link text-reset p-0 mb-0"
                                onClick={() => onEditClick?.(contentItem)}>
                                <i className="bi bi-pencil me-1" aria-hidden="true"></i>
                                {editButtonText}
                            </button>
                        )}

                        {showsModerateButton && (
                            <button
                                type="button"
                                className="btn btn-link text-reset p-0 mb-0"
                                onClick={() => onModerateClick?.(contentItem)}>
                                <i className={`${moderateButtonIconCss} me-1`} aria-hidden="true"></i>
                                {moderateButtonLabel}
                            </button>
                        )}
                    </span>
                )}
            </div>
            )}
        </article>
    );
}
