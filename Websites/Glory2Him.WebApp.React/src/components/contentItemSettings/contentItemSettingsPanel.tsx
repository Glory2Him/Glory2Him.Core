import { useEffect, useState } from 'react';
import { useAuth } from '../securitys/authProvider';
import { ContentItemSettingsModifyPanel } from './contentItemSettingsModifyPanel';
import { ContentItemSettingsViewPanel } from './contentItemSettingsViewPanel';

import {
    ContentItemSetting
} from '../../models/foundations/contentItemSettings/contentItemSetting';

import {
    ContentType
} from '../../models/foundations/contentItemSettings/contentType';

import {
    resolveContentItemSetting
} from '../../services/views/contentItems/resolveContentItemSetting';

import {
    ContentItemSettingsEvents,
    ContentItemSettingsPanelMode
} from '../../models/components/contentItemSettings/contentItemSettingsTemplate';

// THE SETTINGS THAT GOVERN ONE CONTENT ITEM, on whichever face the moment asks for. This panel
// owns everything the two faces share — the §6.4 resolution, whether the winner is an override,
// whether the reader may write, and which face is showing — and DISPATCHES:
//
//   view (the default)   → ContentItemSettingsViewPanel    the winner, read-only, + the buttons
//   modify               → ContentItemSettingsModifyPanel  the feature form, + Save / Reset
//
// Built like ContentItemPanel and for the same reason: one component resolves the facts, the
// templates render them, and adding a third face is one more branch here rather than a second
// resolution somewhere else that drifts.
//
// A pure presentation component, like the content item family beside it: props in, events out,
// no fetching and no mutation. The CONSUMER owns persistence — onModified says what the
// administrator decided, and whether that is a POST or a PUT is the page's business (today the
// client branches on the row's id; when #209 lands it is one call).
export interface ContentItemSettingsPanelProps extends ContentItemSettingsEvents {
    // Lands the panel straight on a surface. Absent, it opens on `view` — a sidebar that opened
    // mid-edit would be a surprise, and reading is what a moderator came for.
    mode?: ContentItemSettingsPanelMode;

    // The item being governed — from the URL, or from the ContentItemPanel beside it. ABSENT,
    // ONLY A DEFAULT CAN RESOLVE: with no item there is nothing to override, so the panel reads
    // the type default and offers no writes.
    contentItemId?: string;

    // The item's content type. Together with contentItemId it is the whole of what §6.4 needs.
    contentType: ContentType;

    // The candidate rows — the type DEFAULTS and this item's OVERRIDE, which is exactly what
    // contentItemSettingService.useGetEffectiveSettingsFor returns. Named for the family, and a
    // collection rather than one row on purpose: handing the defaults alone silently
    // un-overrides an overridden item, which is the drift the shared resolver exists to prevent.
    contentItemSettingCollection: ReadonlyArray<ContentItemSetting>;

    isLoading?: boolean;

    // Freezes the buttons while the consumer is persisting, so one click is one write.
    isSubmitting?: boolean;

    // The read face's corner ribbon naming which policy row is in force — Default or Override.
    // ON BY DEFAULT: this panel exists to answer that question, so its answer leads. The modify
    // face never wears one, whatever this says — see the note there.
    showRibbon?: boolean;

    showBorder?: boolean;
    cssClass?: string;
    titleText?: string;
    ariaLabel?: string;
}

export function ContentItemSettingsPanel({
    mode,
    contentItemId,
    contentType,
    contentItemSettingCollection,
    isLoading = false,
    isSubmitting = false,
    showRibbon = true,
    showBorder = false,
    cssClass,
    titleText,
    ariaLabel,
    onModify,
    onReset,
    onModified,
    onOverrideRemoved
}: ContentItemSettingsPanelProps) {
    const { isAuthenticated, userRoles } = useAuth();

    // Which face is showing is nothing the consumer persists, so it is local state even here —
    // the same call ContentItemPanel makes about its editor.
    const [isModifyTaken, setIsModifyTaken] = useState(false);

    // A different item is a different surface, and a changed mode prop overrules a Modify the
    // reader took earlier.
    useEffect(() => {
        setIsModifyTaken(false);
    }, [contentItemId, mode]);

    // THE WINNER, by the one shared resolver: this item's override where one exists, the content
    // type default otherwise, soft-deleted rows excluded. Never a per-flag merge — §6.4 is full
    // precedence, so "the most specific row" and "the settings in force" are the same sentence.
    const contentItemSetting = resolveContentItemSetting(
        contentItemSettingCollection, contentType, contentItemId);

    // Whether the winner is this item's OWN row. Matched on the item as well as being non-null,
    // because a collection carrying another item's override must never make this one look
    // overridden.
    const isOverride =
        contentItemSetting != null
        && contentItemId != null
        && contentItemSetting.contentItemId === contentItemId;

    // WHO MAY WRITE. Every write on this controller is Administrators only, and the ReadOnly
    // sanction is asked first because a sanction outranks every grant (#366). With no item there
    // is nothing to override, so the writes are withheld from everyone.
    //
    // RENDER decisions only: the foundation re-decides both the save and the removal against the
    // stored row (§14.6).
    const isBlocked = userRoles.includes('ReadOnly');

    const canAdministerSettings =
        isAuthenticated
        && isBlocked === false
        && userRoles.includes('Administrators')
        && contentItemId != null;

    if ((mode === 'modify' || isModifyTaken) && canAdministerSettings && contentItemId != null) {
        return (
            <ContentItemSettingsModifyPanel
                // The resolved row's identity keys the draft: a save landing, or an override
                // removed under the form, is a fresh instance seeded from the new winner rather
                // than an old draft edited on top of a row that no longer governs.
                key={contentItemSetting?.id ?? 'none'}
                contentItemSetting={contentItemSetting}
                contentItemId={contentItemId}
                isOverride={isOverride}
                canAdministerSettings={canAdministerSettings}
                isLoading={isLoading}
                isSubmitting={isSubmitting}
                showBorder={showBorder}
                cssClass={cssClass}
                titleText={titleText}
                ariaLabel={ariaLabel}
                onReset={onReset}
                onModified={(setting) => {
                    // A committed save CLOSES the form back to the read face. What it then shows
                    // is the CONSUMER's collection: the page persists and re-reads, so the saved
                    // override appears; a page that has not re-read yet honestly shows the row
                    // it still holds.
                    setIsModifyTaken(false);
                    onModified?.(setting);
                }} />
        );
    }

    return (
        <ContentItemSettingsViewPanel
            contentItemSetting={contentItemSetting}
            isOverride={isOverride}
            canAdministerSettings={canAdministerSettings}
            isLoading={isLoading}
            isSubmitting={isSubmitting}
            showRibbon={showRibbon}
            showBorder={showBorder}
            cssClass={cssClass}
            titleText={titleText}
            ariaLabel={ariaLabel}
            onModify={() => {
                setIsModifyTaken(true);
                onModify?.();
            }}
            onOverrideRemoved={onOverrideRemoved} />
    );
}
