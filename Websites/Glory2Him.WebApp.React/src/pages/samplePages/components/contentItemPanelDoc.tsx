import { useState } from 'react';
import { ContentItemPanel } from '../../../components/contentItems/contentItemPanel';
import { useAuth } from '../../../components/securitys/authProvider';

import {
    ContentItemSetting
} from '../../../models/foundations/contentItemSettings/contentItemSetting';

import { ContentType } from '../../../models/foundations/contentItemSettings/contentType';

import {
    ApprovalStatus,
    ContentItemSearchItem
} from '../../../models/components/contentItems/contentItemSearchItem';

import { useDocumentTitle } from '../../useDocumentTitle';

import {
    DemoSecurityContext,
    demoSubmitterIdFor,
    SecurityContextSection,
    securityContextOptions
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
ContentItemListPanel                composes the two below
├── ContentItemSearchBarPanel         search bar + advanced options + filter chips
└── ContentItemResultsPanel           the results, infinite scroll
    └── ContentItemPanel              ONE item, on whichever face the moment asks for
        ├── ContentItemAddPanel       the template used to add content
        ├── ContentItemEditPanel      the template used to edit content
        ├── ContentItemDefaultPanel   the view template most types use
        └── ContentItem{ContentType}Panel   overrides, by ContentType:
              ContentItemQuotesPanel        (derives from the default template)
              ContentItemVersesPanel        (the verse whole — purple chip)
`;

const minimalSample = `
import { ContentItemPanel } from '../../components/contentItems/contentItemPanel';

// add — a settings collection and no item puts the panel on its add face:
// the type picker and a blank form. ContentItemListPanel never populates
// this prop, so a card in a list can never fall into add.
<ContentItemPanel
    contentItemSettingCollection={contributableSettings}
    validationIssues={validationIssues}
    isSubmitting={addContentItem.isPending}
    onAdded={(item) => addContentItemAsync(item)}
    onCancelled={() => navigate('/')} />

// view — an element renders through the view template for its type, exactly
// as a search result does. One projection, one face, the whole family.
<ContentItemPanel contentItem={searchItem} />

// view + edit in place — with isEditingAllowed on and onModified wired, the
// owner's Edit affordance swaps the card for ContentItemEditPanel right here.
<ContentItemPanel
    contentItem={searchItem}
    isEditingAllowed
    onModified={(item) => saveAsync(item)}
    onRemoved={(item) => removeAsync(item)} />
`;

const dispatchSample = `
// WHERE EDIT GOES is the page's wiring, not a new prop:
//
//   onEditClick alone            → the event fires and the PAGE routes to its own
//                                  edit surface (what every feed page does today)
//   isEditingAllowed + onModified → Edit opens ContentItemEditPanel IN PLACE
//
// mode="edit" lands the panel straight on the editor without the reader taking
// Edit first — still subject to isEditingAllowed and the role gates.
`;

const settingFor = (
    contentType: ContentType,
    contentTypeName: string,
    overrides: Partial<ContentItemSetting> = {}): ContentItemSetting => ({
    id: `setting-${contentTypeName.toLowerCase().replace(/\s+/g, '-')}`,
    contentItemId: null,
    contentType,
    contentTypeName,
    contentTypeDescription: `A ${contentTypeName.toLowerCase()}.`,
    contentTypeIconCssClass: 'bi-journal-text',
    sortOrder: contentType,
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
    linksAllowed: false,
    showLinks: false,
    attachmentsAllowed: false,
    showAttachments: false,
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

const demoSettings: ReadonlyArray<ContentItemSetting> = [
    settingFor(ContentType.Story, 'Story', { contentTypeIconCssClass: 'bi-book' }),
    settingFor(ContentType.Quote, 'Quote', {
        contentTypeIconCssClass: 'bi-quote', hasTitle: false
    }),
    settingFor(ContentType.Devotional, 'Devotional', {
        contentTypeIconCssClass: 'bi-sunrise'
    })
];

const demoItem: ContentItemSearchItem = {
    id: 'doc-demo-item',
    contentType: ContentType.Story,
    contentItemSetting: demoSettings[0],
    title: 'He carried me through',
    author: 'Grace Abara',
    content:
        'When the diagnosis came, I could not pray. But every morning there was '
        + 'bread on the table and a verse in my inbox, and looking back I can see '
        + 'that He was carrying me the whole way through it.',
    shareabilityBasis: 3,
    sharePermission: '',
    approvalStatus: ApprovalStatus.Draft,
    submittedById: 'doc-demo-user',
    tags: ['providence', 'healing'],
    bibleReferences: ['Deuteronomy 31:6'],
    reactionSummary: [
        { label: 'Amen', glyph: '\ud83d\ude4f', count: 12 },
        { label: 'Love', glyph: '\u2764\ufe0f', count: 5 }
    ],
    commentCount: 4
};

const panelProps: ReadonlyArray<ComponentPropRow> = [
    {
        name: 'contentItem',
        type: 'ContentItemSearchItem?',
        description: 'The SELF-CONTAINED element: the item and its winning setting travel '
            + 'together, resolved by the projection (§6.4). Absent, the panel is the ADD '
            + 'surface. Present, it renders through the view template for its type — and '
            + 'seeds the editor from this same element when Edit is taken, no further read.'
    },
    {
        name: 'contentItemSettingCollection',
        type: 'ContentItemSetting[]',
        defaultValue: '[]',
        description: 'THE ADD-MODE SIGNAL and the editor’s fallback rows: the content type '
            + 'defaults the consumer holds. Populated with no contentItem, the panel renders '
            + 'the add face from these rows. ContentItemListPanel never populates this prop.'
    },
    {
        name: 'mode',
        type: "'add' | 'read' | 'edit'?",
        description: 'Lands the panel straight on a surface — edit opens the editor without '
            + 'the reader taking Edit first, still subject to isEditingAllowed. Absent, the '
            + 'item decides: no item is add, an item reads until Edit is taken.'
    },
    {
        name: 'isEditingAllowed',
        type: 'boolean',
        defaultValue: 'false',
        description: 'The surface switch for the edit face, ahead of every role check. On, '
            + 'the owner’s Edit affordance opens ContentItemEditPanel in place when the page '
            + 'listens on onModified/onRemoved; pages that route to a separate edit surface '
            + 'keep wiring onEditClick instead. It only ever subtracts.'
    },
    {
        name: 'reactionOptions',
        type: 'ContentItemReactionOption[]',
        defaultValue: '[]',
        description: 'The choices behind the Like control, pulled by the page from GET '
            + 'api/Reactions (approved rows only). Empty means no Like control renders.'
    },
    {
        name: 'isModeratedView',
        type: 'boolean',
        defaultValue: 'false',
        description: 'Marks the card as sitting on a MODERATED surface: Moderate stands '
            + 'alone, wearing Edit’s pencil and label.'
    },
    {
        name: 'shouldShowRibbons',
        type: 'boolean',
        defaultValue: 'false',
        description: 'The card wears a corner ribbon naming its approval status — grey Draft, '
            + 'blue Submitted, green Approved, red Rejected — coloured by contentItems.css '
            + 'off data-approval-status.'
    },
    {
        name: 'showTagSection / showBibleReferenceSection / showReactionSection / '
            + 'showCommentsSection / showShareSection / showSaveSection',
        type: 'boolean',
        defaultValue: 'true',
        description: 'SECTION SWITCHES, separate from what the ContentItemSettings allow: '
            + 'the setting says what the type shows, these say what THIS surface has room '
            + 'for. A section renders only when BOTH agree — a page standing tags and '
            + 'bible references in side panels turns the in-card sections off so the same '
            + 'facts never show twice. All default true, so the projection’s setting stays '
            + 'the deciding factor unless a surface specifically overrides it.'
    },
    {
        name: 'validationIssues',
        type: 'Record<string, string[]>?',
        description: 'What the API said was wrong, keyed by ITS parameter names — matched '
            + 'case-insensitively against the form fields; anything unplaced renders in a '
            + 'summary rather than being dropped. Form faces only.'
    },
    {
        name: 'isSubmitting',
        type: 'boolean',
        defaultValue: 'false',
        description: 'Freezes the form buttons while the consumer is persisting, so one '
            + 'click is one write.'
    },
    {
        name: 'submittedByDisplayName',
        type: 'string?',
        description: 'Whose name an owned basis prefills into the Author field — the resolved '
            + 'submitter where the consumer has one, the signed-in reader otherwise.'
    },
    {
        name: 'onAdded / onModified / onRemoved / onCancelled',
        type: '(item) => void',
        description: 'What the reader decided on a form face. The CONSUMER owns persistence: '
            + 'whether onModified is a PUT or, on a terminal item, a fork into a new version '
            + '(§3.4 rule 16) is the page’s business. onRemoved fires only after the confirm '
            + 'dialog.'
    },
    {
        name: 'onEditClick / onModerateClick / onTitleClick / …',
        type: '(item) => void',
        description: 'The view-face event hooks, unchanged from the search family — see the '
            + 'Content Item List Panel page, which documents every one of them.'
    }
];

// The four statuses the corner ribbon can wear — Dismissed has no ribbon by design.
const ribbonStatusOptions = [
    { key: String(ApprovalStatus.Draft), label: 'Draft (grey)' },
    { key: String(ApprovalStatus.Submitted), label: 'Submitted (blue)' },
    { key: String(ApprovalStatus.Approved), label: 'Approved (green)' },
    { key: String(ApprovalStatus.Rejected), label: 'Rejected (red)' }
] as const;

export function ContentItemPanelDoc() {
    useDocumentTitle('Content Item Panel — Components — Glory 2 Him');
    const { isAuthenticated } = useAuth();

    const [lastEvent, setLastEvent] = useState('');

    // The add face's own switches — the form-lifecycle props the view face has no use for.
    const [isAddLoading, setIsAddLoading] = useState(false);
    const [isAddSubmitting, setIsAddSubmitting] = useState(false);
    const [showsAddApiIssues, setShowsAddApiIssues] = useState(false);

    // WHO the demo is viewed as, and WHAT status the item stands in. The gates are real —
    // presentation gates, which is exactly why the page may simulate any viewer: the demo
    // subtree runs under the chosen security context, and the item's submitter follows the
    // owner half of the choice.
    const [securityContext, setSecurityContext] = useState(securityContextOptions[0]);

    const [ribbonStatus, setRibbonStatus] =
        useState<ApprovalStatus>(ApprovalStatus.Draft);

    const viewedDemoItem = {
        ...demoItem,
        submittedById: demoSubmitterIdFor(securityContext),
        approvalStatus: ribbonStatus
    };

    // The playground's switches — each one of this panel's own props.
    const [shouldShowRibbons, setShouldShowRibbons] = useState(true);
    const [isEditingAllowed, setIsEditingAllowed] = useState(true);
    const [isModeratedView, setIsModeratedView] = useState(false);
    const [showTagSection, setShowTagSection] = useState(true);
    const [showBibleReferenceSection, setShowBibleReferenceSection] = useState(true);
    const [showReactionSection, setShowReactionSection] = useState(true);
    const [showCommentsSection, setShowCommentsSection] = useState(true);
    const [showShareSection, setShowShareSection] = useState(true);
    const [showSaveSection, setShowSaveSection] = useState(true);

    return (
        <ComponentDoc
            name="Content Item Panel"
            filePath="src/components/contentItems/contentItemPanel.tsx"
            summary="One content item, on whichever face the moment asks for — the view card
                every feed renders, the add form, and the in-place editor. One family, one
                tree: there is no separate detail component to keep in sync.">

            <DocSection
                title="The family"
                lead={
                    <>
                        The search family and the writing surfaces share ONE dispatcher.
                        <code> ContentItemPanel</code> owns everything that is the same for
                        every face — the setting reads, the ownership and role gates, the
                        reaction gating — and dispatches to a template: no item is{' '}
                        <code>ContentItemAddPanel</code>, Edit taken in place is{' '}
                        <code>ContentItemEditPanel</code>, and everything else renders the
                        view template for its content type. A page that wants the deep text
                        or role overrides renders the Add/Edit template directly — they are
                        exported components, not internals.
                    </>
                }>
                <CodeSample code={familySample} caption="One tree, every face" />
                <CodeSample code={minimalSample} caption="The three wirings" />
                <CodeSample code={dispatchSample} caption="Where Edit goes" />
            </DocSection>

            <DocSection
                title="What the consumer owns"
                lead={
                    <>
                        Everything in this family is a pure presentation component: props in,
                        events out, no fetching, no mutation. The page owns reads, redirects
                        and persistence — the last event this page received:{' '}
                        <code>{lastEvent.length > 0 ? lastEvent : '(none yet)'}</code>
                    </>
                }>
                <PropsTable rows={panelProps} />
            </DocSection>

            <DocSection
                title="The add face"
                lead={
                    <>
                        A settings collection and no item: the picker offers the contributable
                        types and the fields shape themselves from the chosen type&rsquo;s
                        effective setting. The switches are the form-lifecycle props this
                        face answers to. Signed out, it offers the way in instead.
                        {isAuthenticated === false && (
                            <strong> Sign in to see the form here.</strong>
                        )}
                    </>
                }>
                <DemoControls toggles={[
                    {
                        name: 'add-is-loading',
                        label: 'isLoading (settings still arriving)',
                        value: isAddLoading,
                        onChange: setIsAddLoading
                    },
                    {
                        name: 'add-is-submitting',
                        label: 'isSubmitting (a write in flight)',
                        value: isAddSubmitting,
                        onChange: setIsAddSubmitting
                    },
                    {
                        name: 'add-api-issues',
                        label: 'validationIssues (an API readback)',
                        value: showsAddApiIssues,
                        onChange: setShowsAddApiIssues
                    }
                ]} />

                <LiveDemo title="Live — add">
                    <ContentItemPanel
                        contentItemSettingCollection={demoSettings}
                        isLoading={isAddLoading}
                        isSubmitting={isAddSubmitting}
                        validationIssues={showsAddApiIssues
                            ? {
                                Content: ['Text is required'],
                                ContentHash:
                                    ['A content item already exists with the same content.']
                            }
                            : undefined}
                        onAdded={(item) =>
                            setLastEvent(`onAdded(${ContentType[item.contentType]})`)}
                        onCancelled={() => setLastEvent('onCancelled()')} />
                </LiveDemo>
            </DocSection>

            <DocSection
                title="The view face, and Edit in place"
                lead={
                    <>
                        The same card every feed shows, viewed as WHOEVER the security
                        context says — the ownership and role gates are presentation gates,
                        so the demo may honestly step into any viewer. As an owner, with{' '}
                        <code>isEditingAllowed</code> on and <code>onModified</code> wired,
                        <strong> Edit swaps the card for the editor right here</strong>{' '}
                        (Cancel brings the card back); the moderation tier sees the shield,
                        and <code>isModeratedView</code> restyles it into Edit&rsquo;s
                        pencil, standing alone. The ribbon wears whichever status the radio
                        picks — with <code>shouldShowRibbons</code> on.
                    </>
                }>
                <SecurityContextSection
                    selected={securityContext}
                    onChange={setSecurityContext} />

                <DemoRadioGroup
                    title="Ribbon status"
                    name="demo-ribbon-status"
                    options={ribbonStatusOptions}
                    selectedKey={String(ribbonStatus)}
                    onChange={(key) => setRibbonStatus(Number(key) as ApprovalStatus)} />

                <DemoControls toggles={[
                    {
                        name: 'panel-ribbons',
                        label: 'shouldShowRibbons',
                        value: shouldShowRibbons,
                        onChange: setShouldShowRibbons
                    },
                    {
                        name: 'panel-editing',
                        label: 'isEditingAllowed',
                        value: isEditingAllowed,
                        onChange: setIsEditingAllowed
                    },
                    {
                        name: 'panel-moderated',
                        label: 'isModeratedView',
                        value: isModeratedView,
                        onChange: setIsModeratedView
                    },
                    {
                        name: 'panel-tags',
                        label: 'showTagSection',
                        value: showTagSection,
                        onChange: setShowTagSection
                    },
                    {
                        name: 'panel-bible-references',
                        label: 'showBibleReferenceSection',
                        value: showBibleReferenceSection,
                        onChange: setShowBibleReferenceSection
                    },
                    {
                        name: 'panel-reactions',
                        label: 'showReactionSection',
                        value: showReactionSection,
                        onChange: setShowReactionSection
                    },
                    {
                        name: 'panel-comments',
                        label: 'showCommentsSection',
                        value: showCommentsSection,
                        onChange: setShowCommentsSection
                    },
                    {
                        name: 'panel-share',
                        label: 'showShareSection',
                        value: showShareSection,
                        onChange: setShowShareSection
                    },
                    {
                        name: 'panel-save',
                        label: 'showSaveSection',
                        value: showSaveSection,
                        onChange: setShowSaveSection
                    }
                ]} />

                <LiveDemo title="Live — view">
                    <DemoSecurityContext option={securityContext}>
                        <ContentItemPanel
                                contentItem={viewedDemoItem}
                            shouldShowRibbons={shouldShowRibbons}
                            isEditingAllowed={isEditingAllowed}
                            isModeratedView={isModeratedView}
                            showTagSection={showTagSection}
                            showBibleReferenceSection={showBibleReferenceSection}
                            showReactionSection={showReactionSection}
                            showCommentsSection={showCommentsSection}
                            showShareSection={showShareSection}
                            showSaveSection={showSaveSection}
                            onCommentsClick={(item) =>
                                setLastEvent(`onCommentsClick(${item.id})`)}
                            onShareClick={(item) =>
                                setLastEvent(`onShareClick(${item.id})`)}
                            onSaveClick={(item) =>
                                setLastEvent(`onSaveClick(${item.id})`)}
                            onModified={(item) =>
                                setLastEvent(`onModified(${item.id})`)}
                            onRemoved={(item) =>
                                setLastEvent(`onRemoved(${item.id})`)}
                            onModerateClick={(item) =>
                                setLastEvent(`onModerateClick(${item.id})`)} />
                    </DemoSecurityContext>
                </LiveDemo>
            </DocSection>
        </ComponentDoc>
    );
}
