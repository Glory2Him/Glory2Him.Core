import { useState } from 'react';
import { Button } from '../coreUI/button';
import { Card } from '../coreUI/card';
import { FormSwitch } from '../coreUI/formSwitch';

import {
    ContentItemSetting
} from '../../models/foundations/contentItemSettings/contentItemSetting';

import {
    ContentItemSettingFlag,
    contentItemSettingFeatureFields,
    limitReactionsToLoveOnlyDescription,
    limitReactionsToLoveOnlyLabel
} from '../../models/components/contentItemSettings/contentItemSettingFeature';

import {
    ContentItemSettingsEvents,
    ContentItemSettingsTemplateProps
} from '../../models/components/contentItemSettings/contentItemSettingsTemplate';

// THE MODIFY FACE. The same feature rows the read face shows, now live, over a DRAFT COPY — an
// abandoned edit never leaves the resolved row half-changed, and Reset is simply that copy taken
// again.
//
// WHAT IT EDITS is the feature pairs and the love-only narrowing, and nothing else. The type's
// presentation (name, icon, sort order, description) and its contribution shaping (title, author,
// the length ceilings) are type IDENTITY: a per-item icon would drift from the type's own, so
// those stay on /Admin/ContentItemSettings and ride out on the save untouched — which the
// foundation requires anyway, since it refuses an add with no ContentTypeName.
//
// WHAT IT SAVES is always an OVERRIDE. Seeded from the type default, the row raised here carries
// the item's ContentItemId with an empty id, and the consumer creates it; seeded from an existing
// override, it carries that override's id and the consumer updates it. Either way the type
// default is left alone — a sidebar must not silently re-shape every item of a type.
export interface ContentItemSettingsModifyPanelProps
    extends ContentItemSettingsTemplateProps, ContentItemSettingsEvents {
    // The item these settings are being narrowed for. It is stamped onto the saved row, which is
    // what makes the write an override rather than an edit of the default.
    contentItemId: string;
}

export function ContentItemSettingsModifyPanel({
    contentItemSetting,
    contentTypeDefault,
    contentItemId,
    isOverride,
    isSubmitting = false,
    showBorder = true,
    cssClass = '',
    titleText = 'Content Settings',
    ariaLabel = 'Content settings',
    onReset,
    onModified
}: ContentItemSettingsModifyPanelProps) {
    // SPELLED OUT IN BOTH DIRECTIONS. The theme's .card has no border of its own, so
    // leaving the class off does not produce one — the switch has to add it. The content
    // item card family spells it the same way, for the same reason.
    const borderCss = showBorder ? 'border' : 'border-0';

    // THE DRAFT, seeded once from exactly what the read face displayed. Seeded in the
    // initialiser rather than through an effect: the DISPATCHER keys this template on the
    // resolved row's identity, so a different winning row is a different component instance
    // with a fresh draft, and no effect can wipe uncommitted edits on an unrelated re-render.
    const [draft, setDraft] = useState<ContentItemSetting | null>(
        contentItemSetting == null ? null : { ...contentItemSetting });

    const setFlag = (field: ContentItemSettingFlag, value: boolean) =>
        setDraft((current) => current == null ? current : { ...current, [field]: value });

    // WHAT THIS SAVE WOULD MAKE DIFFERENT, marked against the content type default and read off
    // the LIVE draft — so a switch moved away from the default lights up as it is moved, and one
    // moved back goes quiet again.
    //
    // Unlike the read face this does not ask whether an override already exists: saving from
    // here always writes one, so a divergence from the default is a divergence whether or not
    // the row has been created yet.
    const differsFromDefault = (
        current: ContentItemSetting,
        field: ContentItemSettingFlag): boolean =>
        contentTypeDefault != null && contentTypeDefault[field] !== current[field];

    const overriddenCssClass = (isOverridden: boolean): string =>
        isOverridden ? 'g2h-settings-overridden' : '';

    if (draft == null) {
        return (
            <Card cssClass={`${borderCss} ${cssClass}`}>
                <h5 className="mb-1">{titleText}</h5>

                <p className="text-body-secondary mb-0 mt-3">
                    There are no content settings to modify for this item.
                </p>
            </Card>
        );
    }

    // THE ROW THAT GOES OUT. Every field the form does not edit rides along verbatim from the row
    // it was seeded from — the foundation's add validation requires a ContentTypeName, a
    // description within its ceiling and a SortOrder of zero or more, so a create that dropped
    // them would be a 400 rather than a narrower policy.
    //
    // The id is EMPTIED when the seed was the type default: that row belongs to the whole type,
    // and sending its id back would modify the default instead of creating the override. The
    // consumer mints one — it owns the write, so it owns the identity of what it writes.
    const toSavedSetting = (): ContentItemSetting => ({
        ...draft,
        id: isOverride ? draft.id : '',
        contentItemId
    });

    return (
        <Card cssClass={`${borderCss} ${cssClass}`}>
            <div aria-label={ariaLabel}>
                {/* NO RIBBON ON THIS FACE, deliberately. The read face's ribbon names what is IN
                    FORCE; here the scope is about to change, and a strip reading "Default" over
                    a form whose Save creates an override would be announcing the opposite of
                    what the button does. The sentence below says what the save will do instead. */}
                <h5 className="mb-1">{titleText}</h5>

                <p className="text-body-secondary small mb-3">Features</p>

                <p className="text-body-secondary small">
                    {isOverride
                        ? 'Saving updates the settings for this content item alone.'
                        : 'Saving creates settings for this content item alone. The content '
                        + 'type default is left unchanged.'}
                </p>

                {contentItemSettingFeatureFields.map((feature) => (
                    <div className="border-top pt-2" key={feature.title}>
                        <div className="fw-semibold mb-2">{feature.title}</div>

                        <div className={overriddenCssClass(
                            differsFromDefault(draft, feature.shown))}>
                            <FormSwitch
                                label={feature.shownLabel}
                                value={draft[feature.shown]}
                                onValueChange={(value) => setFlag(feature.shown, value)} />
                        </div>

                        <div className={overriddenCssClass(
                            differsFromDefault(draft, feature.allowed))}>
                            <FormSwitch
                                label={feature.allowedLabel}
                                value={draft[feature.allowed]}
                                onValueChange={(value) => setFlag(feature.allowed, value)} />
                        </div>
                    </div>
                ))}

                <div className="border-top pt-3">
                    <div className={overriddenCssClass(
                        differsFromDefault(draft, 'limitReactionsToLoveOnly'))}>
                        <FormSwitch
                            label={limitReactionsToLoveOnlyLabel}
                            value={draft.limitReactionsToLoveOnly}
                            onValueChange={(value) =>
                                setFlag('limitReactionsToLoveOnly', value)} />
                    </div>

                    <p className="text-body-secondary small mb-0">
                        {limitReactionsToLoveOnlyDescription}
                    </p>
                </div>

                <div className="d-flex gap-2 mt-4">
                    <Button
                        color="primary"
                        disabled={isSubmitting}
                        onClick={() => onModified?.(toSavedSetting())}>
                        {isSubmitting ? 'Saving...' : 'Save settings'}
                    </Button>

                    {/* RESET goes back to the values the READ FACE showed — the resolved winner —
                        not to the type default and not to the last save. Uncommitted work is
                        dropped here and nowhere else. */}
                    <Button
                        color="outline-secondary"
                        disabled={isSubmitting}
                        onClick={() => {
                            setDraft(contentItemSetting == null
                                ? null
                                : { ...contentItemSetting });

                            onReset?.();
                        }}>
                        Reset
                    </Button>
                </div>
            </div>
        </Card>
    );
}
