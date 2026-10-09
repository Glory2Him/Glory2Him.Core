import { useEffect, useMemo, useRef, useState } from 'react';
import { toastSuccess } from '../brokers/toastBroker.success';
import { useAuth } from '../components/securitys/authProvider';
import { EntityType } from '../models/foundations/approvalSettings/approvalSetting';
import { associationService } from '../services/foundations/associationService';
import { reactionService } from '../services/foundations/reactionService';

import {
    toChosenReactionSummary
} from '../services/views/contentItems/toChosenReactionSummary';

import {
    toContentItemReactionOption
} from '../services/views/contentItems/toContentItemReactionOption';

import {
    ContentItemReactionCount,
    ContentItemReactionOption,
    ContentItemSearchItem
} from '../models/components/contentItems/contentItemSearchItem';

type ReactionOverlay = {
    viewerReactionLabel: string | undefined;
    reactionSummary: ReadonlyArray<ContentItemReactionCount>;
};

type ReactionWrite = {
    send: () => Promise<unknown>;
    whenSucceeded: () => Promise<void>;
};

type ItemWrites = {
    waiting: ReactionWrite[];
};

const withoutOverlay = (
    overlays: Readonly<Record<string, ReactionOverlay>>,
    contentItemId: string): Readonly<Record<string, ReactionOverlay>> =>
    Object.fromEntries(Object.entries(overlays).filter(([laidOn]) => laidOn !== contentItemId));

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

    // What the reader has just chosen, per item, laid over the item's summary until the server
    // answers: the reaction pressed, or none after a withdrawal, and the counts moved.
    const [overlays, setOverlays] =
        useState<Readonly<Record<string, ReactionOverlay>>>({});

    // Each item with a write in flight, and the writes waiting their turn behind it.
    const itemWrites = useRef(new Map<string, ItemWrites>());

    // Each item's latest write, by identity: only the read that follows it ends the overlay.
    const latestWrites = useRef(new Map<string, object>());

    const upsertAssociation = associationService.useUpsertAssociation();
    const removeAssociation = associationService.useRemoveAssociationByPair();
    const readReactionSummariesAgain = associationService.useReadReactionSummariesAgain();

    const reactionOptions = useMemo(
        () => (reactions ?? []).map(toContentItemReactionOption),
        [reactions]);

    // A page going into the back-forward cache drops the writes still waiting, as if the write
    // before them had failed. The write in flight keeps its place.
    useEffect(() => {
        const dropWaitingWrites = (event: PageTransitionEvent) => {
            if (event.persisted === false) {
                return;
            }

            itemWrites.current.forEach((writes, contentItemId) => {
                if (writes.waiting.length > 0) {
                    writes.waiting.splice(0);
                    setOverlays((laid) => withoutOverlay(laid, contentItemId));
                }
            });
        };

        window.addEventListener('pagehide', dropWaitingWrites);

        return () => window.removeEventListener('pagehide', dropWaitingWrites);
    }, []);

    // One item's writes are sent one at a time, in the order chosen. A failed write drops those
    // waiting behind it, which started from it, and holds up nothing after.
    const sendNextWrite = (contentItemId: string, writes: ItemWrites) => {
        const write = writes.waiting.shift();

        if (write === undefined) {
            itemWrites.current.delete(contentItemId);

            return;
        }

        // Each write's outcome comes from its own call: the mutations' shared state reports
        // only their latest call, across every card.
        write.send().then(
            () => {
                void write.whenSucceeded();
                sendNextWrite(contentItemId, writes);
            },
            () => {
                itemWrites.current.delete(contentItemId);
                setOverlays((laid) => withoutOverlay(laid, contentItemId));
            });
    };

    const onReactionSelected = (
        item: ContentItemSearchItem,
        reaction: ContentItemReactionOption) => {
        const heldReactionLabel = item.viewerReactionLabel;

        // The same choice again is a change of mind — withdrawn, not doubled.
        const isWithdrawal = heldReactionLabel === reaction.label;
        const chosenReactionLabel = isWithdrawal ? undefined : reaction.label;

        setOverlays((laid) => ({
            ...laid,
            [item.id]: {
                viewerReactionLabel: chosenReactionLabel,
                reactionSummary: toChosenReactionSummary(
                    item.reactionSummary,
                    heldReactionLabel,
                    chosenReactionLabel,
                    reactionOptions)
            }
        }));

        const association = {
            entityAType: EntityType.ContentItem,
            entityAKeyId: item.id,
            entityBType: EntityType.Reaction,
            entityBKeyId: reaction.id
        };

        const writeToken = {};
        latestWrites.current.set(item.id, writeToken);

        const write: ReactionWrite = {
            send: () => isWithdrawal
                ? removeAssociation.mutateAsync(association)
                : upsertAssociation.mutateAsync(association),

            whenSucceeded: async () => {
                await readReactionSummariesAgain();

                if (latestWrites.current.get(item.id) === writeToken) {
                    setOverlays((laid) => withoutOverlay(laid, item.id));
                }
            }
        };

        const writes = itemWrites.current.get(item.id);

        if (writes === undefined) {
            const firstWrites: ItemWrites = { waiting: [write] };
            itemWrites.current.set(item.id, firstWrites);
            sendNextWrite(item.id, firstWrites);
        } else {
            writes.waiting.push(write);
        }
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

            const overlay = overlays[contentItem.id];

            return overlay === undefined
                ? summarisedItem
                : {
                    ...summarisedItem,
                    viewerReactionLabel: overlay.viewerReactionLabel,
                    reactionSummary: overlay.reactionSummary
                };
        });

    return { reactionOptions, onReactionSelected, onShareClick, onSaveClick, withReactions };
};
