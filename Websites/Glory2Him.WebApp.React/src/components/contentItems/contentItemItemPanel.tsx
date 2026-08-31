import { ComponentType, useState } from 'react';
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
    onReactionSelected,
    ...eventsAndText
}: ContentItemItemPanelProps) {
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

    const Template =
        templateOverrides[contentItem.contentType] ?? ContentItemItemDefaultPanel;

    return (
        <Template
            contentItem={contentItem}
            contentItemSetting={contentItemSetting}
            contentTypeName={contentTypeName}
            offeredReactions={offeredReactions}
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
