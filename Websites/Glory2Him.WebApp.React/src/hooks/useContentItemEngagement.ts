import { useMemo, useState } from 'react';
import { toastSuccess } from '../brokers/toastBroker.success';
import { useAuth } from '../components/securitys/authProvider';
import { EntityType } from '../models/foundations/approvalSettings/approvalSetting';
import { associationService } from '../services/foundations/associationService';
import { reactionService } from '../services/foundations/reactionService';

import {
    toContentItemReactionOption
} from '../services/views/contentItems/toContentItemReactionOption';

import {
    ContentItemReactionOption,
    ContentItemSearchItem
} from '../models/components/contentItems/contentItemSearchItem';

// The engagement wiring every page that renders the card shares, so each card RENDERS its full
// row — Like with the real reaction vocabulary, Share, Save — and every page decides it the
// same way.
//
// THE COUNTS ARE THE SERVER'S. A page hands the hook the ids of each page of cards it has
// delivered, and withReactions puts each card's reaction summary on it: the reactions its item
// has been given, and the reader's own. A card the read has no summary for carries neither and
// still offers Like; a page that hands no pages reads nothing.
//
// Choosing writes nothing yet: a chosen reaction lives in page state for this visit, laid over
// the summary (the picker marks it, a second click withdraws it), and Save says so honestly.
// Share is real: it copies the item's address.
export const useContentItemEngagement = (
    contentItemIdPages?: ReadonlyArray<ReadonlyArray<string>>) => {
    const { data: reactions } = reactionService.useGetApprovedReactions();

    const { user } = useAuth();

    // The reader the summaries are read for: not yet known while there is no current user,
    // whether its read is still loading or failed, and signed out only once it has been read.
    const readerId = user === undefined
        ? undefined
        : user.isAuthenticated ? user.userId : null;

    const { summaries } =
        associationService.useGetReactionSummaries(contentItemIdPages ?? [], readerId);

    // What this visitor has chosen, per item, for THIS VISIT. Merged into the projection below
    // so the picker shows the choice; nothing is persisted yet.
    const [viewerReactions, setViewerReactions] =
        useState<Readonly<Record<string, string>>>({});

    const upsertAssociation = associationService.useUpsertAssociation();
    const removeAssociation = associationService.useRemoveAssociationByPair();

    const reactionOptions = useMemo(
        () => (reactions ?? []).map(toContentItemReactionOption),
        [reactions]);

    const onReactionSelected = (
        item: ContentItemSearchItem,
        reaction: ContentItemReactionOption) => {
        setViewerReactions((given) => ({
            ...given,

            // The same choice again is a change of mind — withdrawn, not doubled.
            [item.id]: given[item.id] === reaction.label ? '' : reaction.label
        }));

        const association = {
            entityAType: EntityType.ContentItem,
            entityAKeyId: item.id,
            entityBType: EntityType.Reaction,
            entityBKeyId: reaction.id
        };

        void (item.viewerReactionLabel === reaction.label
            ? removeAssociation.mutateAsync(association)
            : upsertAssociation.mutateAsync(association));
    };

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
                    })),
                    viewerReactionLabel: summary.viewerReactionName ?? undefined
                };

            return (viewerReactions[contentItem.id] ?? '').length > 0
                ? { ...summarisedItem, viewerReactionLabel: viewerReactions[contentItem.id] }
                : summarisedItem;
        });

    return { reactionOptions, onReactionSelected, onShareClick, onSaveClick, withReactions };
};
