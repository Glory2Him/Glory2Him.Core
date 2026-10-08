import { useMemo, useState } from 'react';
import { toastSuccess } from '../brokers/toastBroker.success';
import { associationService } from '../services/foundations/associationService';
import { reactionService } from '../services/foundations/reactionService';

import {
    toContentItemReactionOption
} from '../services/views/contentItems/toContentItemReactionOption';

import {
    ContentItemReactionOption,
    ContentItemSearchItem
} from '../models/components/contentItems/contentItemSearchItem';

// The engagement wiring the feed pages share, so the cards RENDER their full row — Like with the
// real reaction vocabulary, Share, Save — while the writes behind them are still to come.
//
// DELIBERATELY THIN. Persisting a reaction or a saved post is a ContentItem association, and
// associations have no HTTP exposer yet (#318) — so a chosen reaction lives in page state for
// this visit (the picker marks it, a second click withdraws it) and Save says so honestly.
// Share is real: it copies the item's address. When #318 lands, the handlers here grow a write
// each and no page or component changes shape.
export const useContentItemEngagement = (
    contentItemIdPages?: ReadonlyArray<ReadonlyArray<string>>) => {
    const { data: reactions } = reactionService.useGetApprovedReactions();

    const { summaries } =
        associationService.useGetReactionSummaries(contentItemIdPages ?? [], undefined);

    // What this visitor has chosen, per item, for THIS VISIT. Merged into the projection below
    // so the picker shows the choice; nothing is persisted yet.
    const [viewerReactions, setViewerReactions] =
        useState<Readonly<Record<string, string>>>({});

    const reactionOptions = useMemo(
        () => (reactions ?? []).map(toContentItemReactionOption),
        [reactions]);

    const onReactionSelected = (
        item: ContentItemSearchItem,
        reaction: ContentItemReactionOption) =>
        setViewerReactions((given) => ({
            ...given,

            // The same choice again is a change of mind — withdrawn, not doubled.
            [item.id]: given[item.id] === reaction.label ? '' : reaction.label
        }));

    const onShareClick = (item: ContentItemSearchItem) => {
        navigator.clipboard
            ?.writeText(`${window.location.origin}/posts/${item.id}`)
            .then(() => toastSuccess('Link copied.'))
            .catch(() => { /* a blocked clipboard is not worth an error toast */ });
    };

    const onSaveClick = () => toastSuccess('Saving posts is coming soon.');

    const withReactions = (
        contentItems: ReadonlyArray<ContentItemSearchItem>): ReadonlyArray<ContentItemSearchItem> =>
        contentItems.map((contentItem) => {
            const summary = summaries[contentItem.id];

            const summarisedItem = summary === undefined
                ? contentItem
                : {
                    ...contentItem,
                    reactionSummary: summary.reactions.map((reaction) => ({
                        label: reaction.name,
                        glyph: reaction.unicodeEmoji,
                        count: reaction.count
                    }))
                };

            return (viewerReactions[contentItem.id] ?? '').length > 0
                ? { ...summarisedItem, viewerReactionLabel: viewerReactions[contentItem.id] }
                : summarisedItem;
        });

    return { reactionOptions, onReactionSelected, onShareClick, onSaveClick, withReactions };
};
