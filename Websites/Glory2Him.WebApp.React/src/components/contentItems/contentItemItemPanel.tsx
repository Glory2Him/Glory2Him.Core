import { ComponentType, useState } from 'react';
import { useAuth } from '../securitys/authProvider';
import { ContentItemItemDefaultPanel } from './contentItemItemDefaultPanel';
import { ContentItemItemQuotesPanel } from './contentItemItemQuotesPanel';
import { ContentItemItemVersesPanel } from './contentItemItemVersesPanel';
import {
    ContentType,
    contentTypeLabels
} from '../../models/foundations/contentItemSettings/contentType';

import {
    ContentItemItemEvents,
    ContentItemItemTemplateProps,
    ContentItemItemText
} from '../../models/components/contentItems/contentItemItemTemplate';

import {
    ContentItemReactionOption,
    ContentItemSearchItem
} from '../../models/components/contentItems/contentItemSearchItem';

import './contentItems.css';

// THE BLUE BLOCK: one result, rendered through a TEMPLATE chosen by its content type. This panel
// owns everything that is the same for every template — the effective-setting resolution, the
// per-card UI state, the reaction gating — and hands the template a fully decided bundle, so an
// override renders differently without ever deciding differently.
//
// A pure presentation component, like everything in this family: props in, events out, no
// fetching, no mutation. Every On{X} either adjusts what is RENDERED (the two toggles it keeps
// for itself) or bubbles to the consumer, which owns filters, redirects and persistence.
export interface ContentItemItemPanelProps extends ContentItemItemEvents, ContentItemItemText {
    // SELF-CONTAINED: the element carries the item AND its winning setting, resolved by the
    // projection (§6.4). This panel consults no collection — every gate below reads the one
    // row that governs this item, so a mixed page is safe by construction and updating one
    // item is one element swapped by the consumer.
    contentItem: ContentItemSearchItem;

    // The reaction choices behind the Like control — pulled by the page from GET api/Reactions
    // (approved rows only) and handed over. Empty means no card offers one, whatever the
    // settings say: a surface that cannot persist a reaction must not appear to accept one.
    reactionOptions?: ReadonlyArray<ContentItemReactionOption>;

    // Whether this card sits on a MODERATED surface (the admin queue). Off — the default —
    // the card offers Edit to its submitter and Moderate (shield) to the moderation tier,
    // side by side. On, only Moderate renders, wearing Edit's pencil and label: on a surface
    // that IS moderation, the moderation action is simply what editing means.
    isModeratedView?: boolean;
}

// THE TEMPLATE REGISTRY. An override renders when one is registered for the item's type; the
// default renders otherwise. Adding one is exactly this one line — Verses arrived that way
// when ContentType.Verses landed, seeds and all.
const templateOverrides:
    Partial<Record<ContentType, ComponentType<ContentItemItemTemplateProps>>> = {
    [ContentType.Quote]: ContentItemItemQuotesPanel,
    [ContentType.Verses]: ContentItemItemVersesPanel
};

export function ContentItemItemPanel({
    contentItem,
    reactionOptions = [],
    isModeratedView = false,
    onReactionSelected,
    onEditClick,
    onModerateClick,
    ...eventsAndText
}: ContentItemItemPanelProps) {
    const { isAuthenticated, user, userRoles } = useAuth();
    // The two per-card render toggles. Local state is right even in a presentation component:
    // which face of a cluster is showing is nothing the consumer persists.
    const [areReactionCountsExpanded, setAreReactionCountsExpanded] = useState(false);
    const [isReactionPickerOpen, setIsReactionPickerOpen] = useState(false);

    // The winner rode in on the element. The name falls back to the fixed enum label, which
    // exists for every member and so is never empty — the same rule contentTypeNameOf keeps.
    const contentItemSetting = contentItem.contentItemSetting;

    const contentTypeName =
        contentItemSetting?.contentTypeName
        ?? contentTypeLabels[contentItem.contentType]
        ?? '';

    // What this card may OFFER, decided against its own effective row. Both halves of the §6.5
    // pair are asked — ReactionsAllowed says the type accepts them, ShowReactions says this
    // surface renders them — plus the panel's own condition that somebody is listening, because
    // a control whose event goes nowhere is worse than no control.
    const offeredReactions = (() => {
        if (onReactionSelected == null
            || reactionOptions.length === 0
            || contentItemSetting?.reactionsAllowed === false
            || contentItemSetting?.showReactions === false) {
            return [] as ReadonlyArray<ContentItemReactionOption>;
        }

        return contentItemSetting?.limitReactionsToLoveOnly === true
            ? reactionOptions.filter((reaction) => reaction.isLove === true)
            : reactionOptions;
    })();

    // WHO SUBMITTED IT is an account-id comparison, exactly the [OWNER] rule
    // ContentItemDetailPanel decides on — never a display name, which two accounts can share.
    const viewerOwnsItem =
        isAuthenticated
        && (contentItem.submittedById ?? '').length > 0
        && contentItem.submittedById === user?.userId;

    // The moderation tier, at every §18.6 scope the item's type composes — and the ReadOnly
    // veto asked FIRST, at its three scopes, because a sanction outranks every grant (#366).
    // RENDER decisions only: the server re-decides both actions against the stored row.
    const contentTypeSegment = ContentType[contentItem.contentType] ?? '';

    const holdsAnyRole = (roles: ReadonlyArray<string>): boolean =>
        roles.some((role) => userRoles.includes(role));

    const isBlocked = holdsAnyRole([
        'ReadOnly',
        'ContentItem-ReadOnly',
        `ContentItem-${contentTypeSegment}-ReadOnly`
    ]);

    const viewerModerates =
        isAuthenticated
        && isBlocked === false
        && holdsAnyRole([
            'Administrators',
            'Reviewers',
            'Publishers',
            'ContentItem-Reviewers',
            'ContentItem-Publishers',
            `ContentItem-${contentTypeSegment}-Reviewers`,
            `ContentItem-${contentTypeSegment}-Publishers`
        ]);

    // The isModeratedView matrix: an ordinary surface offers both, each to its own people; a
    // moderated surface offers Moderate alone, wearing Edit's pencil and label — on a surface
    // that IS moderation, the moderation action is simply what editing means there.
    const showsEditButton =
        isModeratedView === false
        && viewerOwnsItem
        && isBlocked === false
        && onEditClick != null;

    const showsModerateButton = viewerModerates && onModerateClick != null;

    const Template =
        templateOverrides[contentItem.contentType] ?? ContentItemItemDefaultPanel;

    return (
        <Template
            contentItem={contentItem}
            contentItemSetting={contentItemSetting}
            contentTypeName={contentTypeName}
            offeredReactions={offeredReactions}
            showsEditButton={showsEditButton}
            showsModerateButton={showsModerateButton}
            moderateButtonIconCss={isModeratedView ? 'bi bi-pencil' : 'bi bi-shield'}
            moderateButtonLabel={isModeratedView ? 'Edit' : 'Moderate'}
            onEditClick={onEditClick}
            onModerateClick={onModerateClick}
            areReactionCountsExpanded={areReactionCountsExpanded}
            onAssignedReactionsClick={
                () => setAreReactionCountsExpanded(!areReactionCountsExpanded)}
            isReactionPickerOpen={isReactionPickerOpen}
            onReactionClick={() => setIsReactionPickerOpen(!isReactionPickerOpen)}
            onReactionSelected={(item, reaction) => {
                setIsReactionPickerOpen(false);
                onReactionSelected?.(item, reaction);
            }}
            {...eventsAndText} />
    );
}
