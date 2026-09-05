import { Button } from '../coreUI/button';
import { Card } from '../coreUI/card';
import { FormSwitch } from '../coreUI/formSwitch';
import { Spinner } from '../coreUI/spinner';

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

import '../coreUI/coreUI.css';
import './contentItemSettings.css';

// THE READ FACE. The settings that actually govern this content item, shown as they are — the
// same rows in the same order as the modify face, drawn with the same switches, differing only
// in that none of them moves. A reader who takes Modify sees the values stay put and the
// switches come alive, rather than one layout replaced by another.
//
// WHICH ROW IS BEING SHOWN is said out loud, because the two answers lead to different actions:
// a type default governs every item of its type and is edited elsewhere, an override governs
// this item alone and can be removed here.

// WHAT THIS OVERRIDE ACTUALLY CHANGES. A row is marked only where the value in force differs
// from the content type default — so a reader sees the two switches somebody moved rather than
// having to diff nineteen of them against a page they would have to open in another tab.
//
// Nothing is marked when the default is what won: there is then no override, and every value IS
// the default. Nothing is marked either when no default resolves — with nothing to compare
// against, silence is honest and red would be an assertion nobody can check.
const differsFromDefault = (
    contentTypeDefault: ContentItemSetting | undefined,
    isOverride: boolean,
    current: ContentItemSetting,
    field: ContentItemSettingFlag): boolean =>
    isOverride
        && contentTypeDefault != null
        && contentTypeDefault[field] !== current[field];

const overriddenCssClass = (isOverridden: boolean): string =>
    isOverridden ? 'g2h-settings-overridden' : '';

export interface ContentItemSettingsViewPanelProps
    extends ContentItemSettingsTemplateProps, ContentItemSettingsEvents {
    // The corner ribbon naming which policy row is in force — Default or Override. ON BY
    // DEFAULT, unlike the content item card's approval ribbon: that card sits in a feed where
    // most items share a status and the ribbon would be noise, while this panel exists to answer
    // exactly the question the ribbon answers.
    //
    // Turned off, the scope moves to an inline badge rather than disappearing — a reader must
    // never have to guess whether they are looking at the type default.
    showRibbon?: boolean;
}

export function ContentItemSettingsViewPanel({
    contentItemSetting,
    contentTypeDefault,
    isOverride,
    canAdministerSettings,
    isLoading = false,
    isSubmitting = false,
    showRibbon = true,
    showBorder = true,
    cssClass = '',
    titleText = 'Content Settings',
    ariaLabel = 'Content settings',
    onModify,
    onOverrideRemoved
}: ContentItemSettingsViewPanelProps) {
    // SPELLED OUT IN BOTH DIRECTIONS. The theme's .card has no border of its own, so
    // leaving the class off does not produce one — the switch has to add it. The content
    // item card family spells it the same way, for the same reason.
    const borderCss = showBorder ? 'border' : 'border-0';

    // The ribbon needs a resolved row to name — with nothing in force there is no scope to
    // announce, and an empty strip would be worse than none.
    const wearsRibbon = showRibbon && contentItemSetting != null;
    const scope = isOverride ? 'Override' : 'Default';

    // Whether ANY row is marked, so the legend appears only when there is something to explain.
    const hasMarkedRows =
        contentItemSetting != null
        && [
            ...contentItemSettingFeatureFields.flatMap(
                (feature) => [feature.shown, feature.allowed]),
            'limitReactionsToLoveOnly' as ContentItemSettingFlag
        ].some((field) => differsFromDefault(
            contentTypeDefault, isOverride, contentItemSetting, field));

    const hostCssClass =
        `${borderCss} ${cssClass}${wearsRibbon ? ' g2h-has-corner-ribbon' : ''}`;

    return (
        <Card cssClass={hostCssClass}>
            {wearsRibbon && (
                <span
                    className="g2h-corner-ribbon g2h-settings-ribbon"
                    data-setting-scope={scope}>
                    {scope}
                </span>
            )}

            <div aria-label={ariaLabel}>
                <h5 className="mb-1">{titleText}</h5>

                {isLoading ? (
                    <div className="text-center py-4">
                        <Spinner />
                    </div>
                ) : contentItemSetting == null ? (
                    /* NO ROW RESOLVED — neither an override nor a type default. The panel says so
                       rather than drawing every switch off, which would read as a policy somebody
                       chose instead of an answer that has not arrived. */
                    <p className="text-body-secondary mb-0 mt-3">
                        No content settings apply to this item yet.
                    </p>
                ) : (
                    <>
                        <p className="text-body-secondary small mb-3">Features</p>

                        {/* WITHOUT THE RIBBON the scope still has to be visible, so it falls back
                            to a badge. With it, one card would otherwise say the same word
                            twice. */}
                        {wearsRibbon === false && (
                            <div className="d-flex flex-wrap gap-2 mb-3">
                                <span className={`badge ${isOverride
                                    ? 'text-bg-primary'
                                    : 'text-bg-secondary'}`}>
                                    {isOverride ? 'Override' : 'Default'}
                                </span>
                            </div>
                        )}

                        {contentItemSettingFeatureFields.map((feature) => (
                            <div className="border-top pt-2" key={feature.title}>
                                <div className="fw-semibold mb-2">{feature.title}</div>

                                <div className={overriddenCssClass(differsFromDefault(
                                    contentTypeDefault, isOverride,
                                    contentItemSetting, feature.shown))}>
                                    <FormSwitch
                                        label={feature.shownLabel}
                                        value={contentItemSetting[feature.shown]}
                                        disabled />
                                </div>

                                <div className={overriddenCssClass(differsFromDefault(
                                    contentTypeDefault, isOverride,
                                    contentItemSetting, feature.allowed))}>
                                    <FormSwitch
                                        label={feature.allowedLabel}
                                        value={contentItemSetting[feature.allowed]}
                                        disabled />
                                </div>
                            </div>
                        ))}

                        <div className="border-top pt-3">
                            <div className={overriddenCssClass(differsFromDefault(
                                contentTypeDefault, isOverride,
                                contentItemSetting, 'limitReactionsToLoveOnly'))}>
                                <FormSwitch
                                    label={limitReactionsToLoveOnlyLabel}
                                    value={contentItemSetting.limitReactionsToLoveOnly}
                                    disabled />
                            </div>

                            <p className="text-body-secondary small mb-0">
                                {limitReactionsToLoveOnlyDescription}
                            </p>
                        </div>

                        {hasMarkedRows && (
                            <p className="small mt-3 mb-0 text-danger">
                                Highlighted settings differ from the content type default.
                            </p>
                        )}

                        <p className="text-body-secondary small mt-3">
                            {isOverride
                                ? 'These settings apply to this content item alone, overriding '
                                + 'the content type default.'
                                : 'These are the content type defaults. This item has no '
                                + 'settings of its own.'}
                        </p>

                        {/* THE WRITES, offered only to who may make them. Every write on this
                            controller is Administrators only, so a reviewer working the same
                            moderation surface reads the settings and is offered nothing —
                            a control whose request the server refuses is worse than no control.

                            REMOVE OVERRIDE IS ABSENT, NOT DISABLED, against a type default:
                            there is nothing to remove, the server refuses one (§12.5.2 business
                            rule 5 — every type must always have a live default), and a greyed
                            button would suggest a permission problem instead. */}
                        {canAdministerSettings && (
                            <div className="d-flex flex-wrap gap-2 mt-3">
                                <Button
                                    color="primary"
                                    disabled={isSubmitting}
                                    onClick={onModify}>
                                    Modify
                                </Button>

                                {isOverride && (
                                    <Button
                                        color="outline-danger"
                                        disabled={isSubmitting}
                                        onClick={() => onOverrideRemoved?.(contentItemSetting)}>
                                        Remove Override
                                    </Button>
                                )}
                            </div>
                        )}
                    </>
                )}
            </div>
        </Card>
    );
}
