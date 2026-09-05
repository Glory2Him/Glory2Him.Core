import { useState } from 'react';

import {
    ContentItemSettingsPanel
} from '../../../components/contentItemSettings/contentItemSettingsPanel';

import {
    ContentItemSetting
} from '../../../models/foundations/contentItemSettings/contentItemSetting';

import { ContentType } from '../../../models/foundations/contentItemSettings/contentType';
import { useDocumentTitle } from '../../useDocumentTitle';

import {
    DemoSecurityContext,
    SecurityContextOption
} from './shared/securityContextDemo';

import {
    CodeSample,
    ComponentDoc,
    ComponentPropRow,
    DemoControls,
    DemoRadioGroup,
    DocSection,
    LiveDemo,
    PropsTable
} from './shared/componentDoc';

const familySample = `
ContentItemSettingsPanel              ONE item's settings, on whichever face is asked for
├── ContentItemSettingsViewPanel        VIEW: the winner read-only + Modify / Remove Override
└── ContentItemSettingsModifyPanel      MODIFY: the feature form + Save settings / Reset
`;

const minimalSample = `
import {
    ContentItemSettingsPanel
} from '../../components/contentItemSettings/contentItemSettingsPanel';

// The collection is the SAME one the cards resolve against — the type defaults plus
// this item's own override, which useGetEffectiveSettingsFor returns in one read.
const { data: contentItemSettings } =
    contentItemSettingService.useGetEffectiveSettingsFor([contentItemId]);

<ContentItemSettingsPanel
    contentItemId={contentItem.id}
    contentType={contentItem.contentType}
    contentItemSettingCollection={contentItemSettings ?? []}
    isSubmitting={createOrUpdateOverride.isPending}
    onModified={(setting) => void saveAsync(setting)}
    onOverrideRemoved={(setting) => setOverrideToRemove(setting)}
    showBorder />
`;

const resolutionSample = `
// WHICH ROW WINS is design §6.4, resolved by the one shared resolver the cards use:
//
//   an override carrying THIS item's ContentItemId   → it wins, in full
//   no override                                      → the content type default
//   neither                                          → nothing, said honestly
//
// FULL PRECEDENCE, never a per-flag merge. "The most specific row" and "the settings
// in force" are the same sentence — an override does not tighten the default, it
// replaces it. A soft-deleted row is excluded from resolution entirely (§6.6).
`;

const savingSample = `
// SAVING ALWAYS WRITES AN OVERRIDE, never the type default:
//
//   seeded from the default   → the row goes out with contentItemId set and id ''
//                               → the consumer POSTs a new override
//   seeded from an override   → the row keeps that override's id
//                               → the consumer PUTs it
//
// Every field the form does not edit rides along verbatim from the seed row. That is
// not a nicety: the foundation refuses an add with no ContentTypeName, a description
// over its ceiling or a negative SortOrder.
//
// REMOVE OVERRIDE hard-deletes the item's row, and the item goes back to its type
// default. It renders only against an override — the server refuses to remove a
// default, because every content type must always have a live one (§12.5.2 rule 5).
`;

const settingFor = (
    contentItemId: string | null,
    overrides: Partial<ContentItemSetting> = {}): ContentItemSetting => ({
        id: contentItemId == null ? 'default-devotional' : 'override-devotional',
        contentItemId,
        contentType: ContentType.Devotional,
        contentTypeName: 'Devotional',
        contentTypeDescription: 'A daily devotional.',
        contentTypeIconCssClass: 'bi-sunrise',
        sortOrder: 30,
        isAvailableAsGeneralUserContribution: true,
        hasTitle: true,
        hasAuthor: true,
        tagsAllowed: true,
        showTags: true,
        commentsAllowed: true,
        showComments: true,
        reactionsAllowed: true,
        showReactions: true,
        limitReactionsToLoveOnly: false,
        linksAllowed: true,
        showLinks: true,
        attachmentsAllowed: true,
        showAttachments: true,
        bibleReferenceAllowed: true,
        showBibleReferences: true,
        isDeleted: false,
        createdBy: 'seed',
        createdWhen: '2026-01-01T00:00:00+00:00',
        updatedBy: 'seed',
        updatedWhen: '2026-01-01T00:00:00+00:00',
        deletedBy: null,
        deletedWhen: null,
        deletionReason: null,
        ...overrides
    });

const demoContentItemId = 'demo-devotional-1';

const demoDefault = settingFor(null);

// The narrowed row: this one devotional keeps its comments visible but closed, and takes no
// new attachments. Exactly the shape an administrator reaches for a contentious post.
const demoOverride = settingFor(demoContentItemId, {
    commentsAllowed: false,
    attachmentsAllowed: false,
    showAttachments: false
});

// THE PEOPLE THIS PANEL TELLS APART. Deliberately its own list rather than the shared
// securityContextOptions six: those are built around OWNERSHIP, and this panel has no ownership
// gate — settings are administrator-authored configuration, so "also owner" and "not owner"
// would render identically and a reader would reasonably read that sameness as a bug.
//
// What it does tell apart is the administrator tier and the ReadOnly sanction, so those are the
// personas offered. A signed-out reader is not expressible through AuthContextOverride, which
// always stands somebody up; the colocated panel tests cover that case.
const settingsSecurityContextOptions: ReadonlyArray<SecurityContextOption> = [
    {
        key: 'administrator',
        label: 'I am an administrator',
        roles: ['Administrators'],
        isOwner: false
    },
    {
        key: 'administrator-readonly',
        label: 'I am an administrator holding ReadOnly (sanctioned)',
        roles: ['Administrators', 'ReadOnly'],
        isOwner: false
    },
    {
        key: 'reviewer',
        label: 'I am a reviewer',
        roles: ['Reviewers'],
        isOwner: false
    },
    {
        key: 'publisher',
        label: 'I am a publisher',
        roles: ['Publishers'],
        isOwner: false
    }
];

const panelProps: ReadonlyArray<ComponentPropRow> = [
    {
        name: 'contentItemId',
        type: 'string?',
        description: 'The item being governed — from the URL, or from the ContentItemPanel '
            + 'beside it. ABSENT, only a content type default can resolve: with no item there '
            + 'is nothing to override, so the panel reads the default and offers no writes.'
    },
    {
        name: 'contentType',
        type: 'ContentType',
        description: 'The item’s content type. Together with contentItemId it is the whole of '
            + 'what §6.4 resolution needs.'
    },
    {
        name: 'contentItemSettingCollection',
        type: 'ContentItemSetting[]',
        description: 'The candidate rows — the type DEFAULTS and this item’s OVERRIDE, exactly '
            + 'what contentItemSettingService.useGetEffectiveSettingsFor returns. A collection '
            + 'rather than one row on purpose: handing the defaults alone silently '
            + 'un-overrides an overridden item.'
    },
    {
        name: 'mode',
        type: "'view' | 'modify'?",
        description: 'Lands the panel straight on a surface. Absent it opens on view — a '
            + 'sidebar that opened mid-edit would be a surprise, and reading is what a '
            + 'moderator came for. Modify is still subject to the role gate.'
    },
    {
        name: 'showRibbon',
        type: 'boolean',
        defaultValue: 'true',
        description: 'The read face’s corner ribbon naming which policy row is in force — grey '
            + 'Default, purple Override, coloured by contentItemSettings.css off '
            + 'data-setting-scope. ON by default, unlike the card’s approval ribbon: this panel '
            + 'exists to answer exactly that question. Turned off, the scope moves to an inline '
            + 'badge rather than disappearing. The modify face never wears one — the scope '
            + 'there is about to change.'
    },
    {
        name: 'isSubmitting',
        type: 'boolean',
        defaultValue: 'false',
        description: 'Freezes the buttons while the consumer is persisting, so one click is '
            + 'one write.'
    },
    {
        name: 'showBorder',
        type: 'boolean',
        defaultValue: 'true',
        description: 'Whether the panel is drawn as a bordered card. ON by default, unlike the '
            + 'content item family: this panel stands in a sidebar beside other cards rather '
            + 'than in a feed of its own, and an edge is what separates it from whatever sits '
            + 'above it.'
    },
    {
        name: 'cssClass / titleText / ariaLabel',
        type: 'string',
        defaultValue: '— / Content Settings / Content settings',
        description: 'The family’s presentation props, unchanged from the content item panels.'
    },
    {
        name: 'onModified',
        type: '(setting: ContentItemSetting) => void',
        description: 'Save settings. Carries the complete row to persist with contentItemId '
            + 'stamped and the id emptied where the form was seeded from the type default — so '
            + 'the consumer POSTs a create or PUTs an update off that one field.'
    },
    {
        name: 'onOverrideRemoved',
        type: '(setting: ContentItemSetting) => void',
        description: 'Remove Override. Carries the override row, id and all, to hard delete. '
            + 'Never raised against a type default. The CONSUMER confirms — this panel raises '
            + 'the intent, the page asks the question.'
    },
    {
        name: 'onModify / onReset',
        type: '() => void',
        description: 'Notifications only. The face switch and the revert are both internal, the '
            + 'way ContentItemPanel opens its editor in place; a surface that wants to route '
            + 'somewhere else listens here.'
    }
];

export function ContentItemSettingsPanelDoc() {
    useDocumentTitle('Content Item Settings Panel — Components — Glory 2 Him');

    const [securityContext, setSecurityContext] =
        useState<SecurityContextOption>(settingsSecurityContextOptions[0]);

    const [isComparing, setIsComparing] = useState(false);
    const [scopeKey, setScopeKey] = useState('override');
    const [modeKey, setModeKey] = useState('view');
    const [showsRibbon, setShowsRibbon] = useState(true);
    const [showsBorder, setShowsBorder] = useState(true);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [lastEvent, setLastEvent] = useState('');

    // WHICH ROWS THE PANEL IS HANDED is the demo's real switch: the panel does not take a scope
    // prop, it RESOLVES one. Adding the override row to the collection is the only way to make
    // the override face appear, which is exactly what a consumer does.
    const demoCollection =
        scopeKey === 'override'
            ? [demoDefault, demoOverride]
            : scopeKey === 'none'
                ? []
                : [demoDefault];

    return (
        <ComponentDoc
            name="Content Item Settings Panel"
            filePath="src/components/contentItemSettings/contentItemSettingsPanel.tsx"
            summary="The settings that actually govern ONE content item — the type default or
                the item's own override — read on one face and narrowed on the other. Built for
                the admin right-hand column, beside the approval round.">

            <DocSection
                title="The family"
                lead={
                    <>
                        One dispatcher, two faces, exactly as{' '}
                        <code>ContentItemPanel</code> is built.{' '}
                        <code>ContentItemSettingsPanel</code> owns what both faces share — the
                        §6.4 resolution, whether the winner is an override, and whether the
                        reader may write — and dispatches to a template.
                    </>
                }>
                <CodeSample code={familySample} caption="One tree, two faces" />
                <CodeSample code={minimalSample} caption="The wiring" />
            </DocSection>

            <DocSection
                title="Which settings apply"
                lead={
                    <>
                        The panel asks the same resolver every content item card asks, against
                        the same rows — so a sidebar can never disagree with the item beside it
                        about which policy is in force.
                    </>
                }>
                <CodeSample code={resolutionSample} caption="§6.4, full precedence" />
            </DocSection>

            <DocSection
                title="What a save writes"
                lead={
                    <>
                        Saving here narrows ONE item. The content type default is edited from{' '}
                        <code>/Admin/ContentItemSettings</code> and is never touched by this
                        panel, whichever row the form was seeded from.
                    </>
                }>
                <CodeSample code={savingSample} caption="Create or update, and the removal" />
            </DocSection>

            <DocSection
                title="What the consumer owns"
                lead={
                    <>
                        A pure presentation component: props in, events out, no fetching and no
                        mutation. The page owns the reads, the writes and the confirmation — the
                        last event this page received:{' '}
                        <code>{lastEvent.length > 0 ? lastEvent : '(none yet)'}</code>
                    </>
                }>
                <PropsTable rows={panelProps} />
            </DocSection>

            <DocSection
                title="Live"
                lead={
                    <>
                        The write affordances are <code>Administrators</code> only and are
                        withheld from a <code>ReadOnly</code> holder, a sanction that outranks
                        every grant. Step the demo into any of them below: the panel decides
                        RENDERING only, so showing a reader what another person would be offered
                        grants nothing — the foundation re-decides every save and removal against
                        the stored row (§14.6), and your own session is untouched.
                    </>
                }>

                <DemoRadioGroup
                    title="Security context"
                    name="settings-security-context"
                    selectedKey={securityContext.key}
                    onChange={(key) => setSecurityContext(
                        settingsSecurityContextOptions.find(
                            (option) => option.key === key)
                        ?? settingsSecurityContextOptions[0])}
                    options={settingsSecurityContextOptions} />
                <DemoRadioGroup
                    title="What the collection holds"
                    name="settings-scope"
                    selectedKey={scopeKey}
                    onChange={setScopeKey}
                    options={[
                        { key: 'override', label: 'The type default AND this item’s override' },
                        { key: 'default', label: 'The type default only' },
                        { key: 'none', label: 'Nothing — no row resolves' }
                    ]} />

                <DemoRadioGroup
                    title="mode"
                    name="settings-mode"
                    selectedKey={modeKey}
                    onChange={setModeKey}
                    options={[
                        { key: 'view', label: 'view — the read face (the default)' },
                        { key: 'modify', label: 'modify — straight onto the form' }
                    ]} />

                <DemoControls
                    toggles={[
                        {
                            name: 'showRibbon',
                            label: 'showRibbon',
                            defaultValue: true,
                            value: showsRibbon,
                            onChange: setShowsRibbon
                        },
                        {
                            name: 'showBorder',
                            label: 'showBorder',
                            defaultValue: true,
                            value: showsBorder,
                            onChange: setShowsBorder
                        },
                        {
                            name: 'isSubmitting',
                            label: 'isSubmitting',
                            defaultValue: false,
                            value: isSubmitting,
                            onChange: setIsSubmitting
                        },
                        {
                            name: 'compareScopes',
                            label: 'Compare the default and the override side by side',
                            value: isComparing,
                            onChange: setIsComparing
                        }
                    ]} />

                {/* THE TWO SCOPES SIDE BY SIDE — the same item, the same panel, the only
                    difference being whether the collection carries an override for it. What
                    changes: the ribbon's word and colour (grey Default / purple Override), the
                    sentence under the switches, whether Remove Override is offered at all, and
                    the switch values themselves, since the two rows say different things. */}
                <DemoSecurityContext option={securityContext}>
                {isComparing ? (
                    <LiveDemo title="Default beside Override">
                        <div className="row g-3">
                            <div className="col-12 col-lg-6">
                                <p className="small text-body-secondary mb-2">
                                    The collection holds the type default only.
                                </p>

                                <ContentItemSettingsPanel
                                    contentItemId={demoContentItemId}
                                    contentType={ContentType.Devotional}
                                    contentItemSettingCollection={[demoDefault]}
                                    showRibbon={showsRibbon}
                                    isSubmitting={isSubmitting}
                                    showBorder={showsBorder}
                                    onModify={() => setLastEvent('onModify — default')}
                                    onModified={() => setLastEvent('onModified — default')} />
                            </div>

                            <div className="col-12 col-lg-6">
                                <p className="small text-body-secondary mb-2">
                                    The collection also holds this item's override.
                                </p>

                                <ContentItemSettingsPanel
                                    contentItemId={demoContentItemId}
                                    contentType={ContentType.Devotional}
                                    contentItemSettingCollection={[demoDefault, demoOverride]}
                                    showRibbon={showsRibbon}
                                    isSubmitting={isSubmitting}
                                    showBorder={showsBorder}
                                    onModify={() => setLastEvent('onModify — override')}
                                    onModified={() => setLastEvent('onModified — override')}
                                    onOverrideRemoved={(setting) => setLastEvent(
                                        `onOverrideRemoved — ${setting.id}`)} />
                            </div>
                        </div>
                    </LiveDemo>
                ) : (
                <LiveDemo title={scopeKey === 'override'
                    ? 'With an override'
                    : scopeKey === 'default'
                        ? 'On the type default'
                        : 'With nothing resolved'}>
                    <div style={{ maxWidth: '30rem' }}>
                        <ContentItemSettingsPanel
                            mode={modeKey === 'modify' ? 'modify' : 'view'}
                            contentItemId={demoContentItemId}
                            contentType={ContentType.Devotional}
                            contentItemSettingCollection={demoCollection}
                            showRibbon={showsRibbon}
                            isSubmitting={isSubmitting}
                            showBorder={showsBorder}
                            onModify={() => setLastEvent('onModify')}
                            onReset={() => setLastEvent('onReset')}
                            onModified={(setting) => setLastEvent(
                                `onModified — ${setting.id.length === 0 ? 'create' : 'update'}`
                                + ` for contentItemId ${setting.contentItemId}`)}
                            onOverrideRemoved={(setting) => setLastEvent(
                                `onOverrideRemoved — ${setting.id}`)} />
                    </div>
                </LiveDemo>
                )}
                </DemoSecurityContext>
            </DocSection>
        </ComponentDoc>
    );
}
