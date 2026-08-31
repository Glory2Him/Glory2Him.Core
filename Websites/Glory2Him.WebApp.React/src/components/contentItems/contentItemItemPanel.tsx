import { ComponentType, useState } from 'react';
import { useAuth } from '../securitys/authProvider';
import { ContentItemItemDefaultPanel } from './contentItemItemDefaultPanel';
import { ContentItemItemQuotesPanel } from './contentItemItemQuotesPanel';
import { ContentItemSetting } from '../../models/foundations/contentItemSettings/contentItemSetting';
import { ContentType } from '../../models/foundations/contentItemSettings/contentType';

import {
    contentTypeNameOf,
    resolveContentItemSetting
} from '../../services/views/contentItems/resolveContentItemSetting';

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
    contentItem: ContentItemSearchItem;

    // The rows the consumer holds. THIS panel resolves the item's OWN effective row (§6.4,
    // §12.5.2 rules 1-2: item override beats type default, soft-deleted rows excluded §6.6), so
    // a mixed collection is safe and every card gates its features individually — ShowTags,
    // ShowBibleReferences, ShowReactions, LimitReactionsToLoveOnly, ShowComments, HasTitle,
    // HasAuthor.
    contentItemSettingCollection?: ReadonlyArray<ContentItemSetting>;

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
// default renders otherwise. ContentItemItemVerseImagePanel belongs here too, but there is no
// ContentType.VerseImage member yet — the enum is append-only (§3.6) and a new member drags the
// settings, role and demo seeds with it, so that is its own change; this registry is why it will
// then be one line here.
const templateOverrides:
    Partial<Record<ContentType, ComponentType<ContentItemItemTemplateProps>>> = {
    [ContentType.Quote]: ContentItemItemQuotesPanel
};

export function ContentItemItemPanel({
    contentItem,
    contentItemSettingCollection = [],
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

    const contentItemSetting = resolveContentItemSetting(
        contentItemSettingCollection, contentItem.contentType, contentItem.id);

    const contentTypeName = contentTypeNameOf(
        contentItemSettingCollection, contentItem.contentType, contentItem.id);

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
