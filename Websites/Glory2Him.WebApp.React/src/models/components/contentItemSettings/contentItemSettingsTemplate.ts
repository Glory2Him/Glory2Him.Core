import {
    ContentItemSetting
} from '../../foundations/contentItemSettings/contentItemSetting';

// Which face the panel is showing. Mirrors ContentItemPanelMode — `view` reads the winning
// settings, `modify` edits them into an override.
export type ContentItemSettingsPanelMode = 'view' | 'modify';

// What both faces are told and what both faces raise. The templates are handed a RESOLVED
// setting rather than the collection: the dispatcher owns §6.4 resolution so the two faces
// cannot answer "which row wins" differently.
export interface ContentItemSettingsTemplateProps {
    // The row that governs this item — the override where one exists, the content type default
    // otherwise. Absent when neither resolves, which the view face reports honestly and the
    // modify face cannot be reached from.
    contentItemSetting?: ContentItemSetting;

    // Whether the resolved row is this item's OWN override rather than the type default. Decides
    // the scope badge on both faces, and whether Remove Override renders at all.
    isOverride: boolean;

    // Whether the reader may write. Every write on the settings controller is Administrators
    // only, so a reviewer sees the settings and no buttons. Render-only — the foundation
    // re-decides against the stored row (§14.6).
    canAdministerSettings: boolean;

    isLoading?: boolean;

    // Freezes the buttons while the consumer is persisting, so one click is one write.
    isSubmitting?: boolean;

    // Whether the panel is drawn as a bordered card — on by default, and spelled out in both
    // directions because the theme's own .card carries no border to leave alone.
    showBorder?: boolean;

    cssClass?: string;
    titleText?: string;
    ariaLabel?: string;
}

// The events the panel raises. Everything here is the CONSUMER's to persist: this family does no
// fetching and no mutation, exactly like the content item panels beside it.
export interface ContentItemSettingsEvents {
    // Notification only — the face switch itself is internal, the way ContentItemPanel opens its
    // editor in place. A surface that wants to route somewhere else listens here.
    onModify?: () => void;

    // Notification only — the revert to the displayed values is internal.
    onReset?: () => void;

    // SAVE SETTINGS. Carries the complete row to persist, with contentItemId already stamped:
    // saving from this panel always writes an OVERRIDE, never the type default. A row that
    // carries no id yet is a create; one that carries the override's id is an update.
    onModified?: (contentItemSetting: ContentItemSetting) => void;

    // REMOVE OVERRIDE. Carries the override row, id and all, to hard delete. Never raised
    // against a type default — both the button and the server refuse one.
    onOverrideRemoved?: (contentItemSetting: ContentItemSetting) => void;
}
