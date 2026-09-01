import { useState } from 'react';
import { ContentItemPanel } from '../../../components/contentItems/contentItemPanel';
import { useAuth } from '../../../components/securitys/authProvider';

import {
    ContentItemSetting
} from '../../../models/foundations/contentItemSettings/contentItemSetting';

import { ContentType } from '../../../models/foundations/contentItemSettings/contentType';

import { useDocumentTitle } from '../../useDocumentTitle';
import { demoStoryItem } from './shared/contentItemDemoData';

import {
    contentItemElementShape,
    contentItemFormItemShape,
    contentItemSettingShape
} from './shared/contentItemShapeSamples';

import { ContentItemPanelPlayground } from './shared/contentItemPanelPlayground';

import {
    CodeSample,
    ComponentDoc,
    ComponentPropRow,
    DemoControls,
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
              ContentItemVerseImagePanel    (the verse whole — purple chip)
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

// view + edit in place — with showEditSection on and onModified wired, the
// owner's Edit affordance swaps the card for ContentItemEditPanel right here.
<ContentItemPanel
    contentItem={searchItem}
    showEditSection
    onModified={(item) => saveAsync(item)}
    onRemoved={(item) => removeAsync(item)} />
`;

const dispatchSample = `
// WHERE EDIT GOES is the page's wiring, not a new prop:
//
//   onEditClick alone            → the event fires and the PAGE routes to its own
//                                  edit surface (what every feed page does today)
//   showEditSection + onModified → Edit opens ContentItemEditPanel IN PLACE
//
// mode="edit" lands the panel straight on the editor without the reader taking
// Edit first — still subject to showEditSection and the role gates.
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
            + 'the reader taking Edit first, still subject to showEditSection. Absent, the '
            + 'item decides: no item is add, an item reads until Edit is taken.'
    },
    {
        name: 'showEditSection',
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
        name: 'showModerationSection',
        type: 'boolean',
        defaultValue: 'false',
        description: 'Marks the card as sitting on a MODERATED surface: Moderate stands '
            + 'alone, wearing Edit’s pencil and label.'
    },
    {
        name: 'showApprovalStatusRibbon',
        type: 'boolean',
        defaultValue: 'false',
        description: 'The card wears a corner ribbon naming its approval status — grey Draft, '
            + 'yellow Submitted, green Approved, red Rejected — coloured by contentItems.css '
            + 'off data-approval-status.'
    },
    {
        name: 'showApprovalStatus',
        type: 'boolean',
        defaultValue: 'false',
        description: 'The ribbon’s sibling: the approval-status PILL beside the type chip, '
            + 'in the same colours. On, every status shows — Approved included: a surface '
            + 'that asks for statuses is asking for all of them.'
    },
    {
        name: 'showContentExpanded / truncateAt / allowInPlaceExpansion',
        type: 'boolean / number / boolean',
        defaultValue: 'false / 400 / false',
        description: 'The content length, threaded from the list. Collapsed, the content '
            + 'is cut at truncateAt characters with an ellipsis and the read-more '
            + 'affordance; a detail surface passes showContentExpanded and the whole '
            + 'content stands. Where read-more GOES is allowInPlaceExpansion’s answer: '
            + 'off, it raises onReadMore and the page routes to the detail surface '
            + 'with its back context; on, it toggles the expansion in place and the '
            + 'expanded card offers show-less — one affordance, never both jobs at once.'
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

export function ContentItemPanelDoc() {
    useDocumentTitle('Content Item Panel — Components — Glory 2 Him');
    const { isAuthenticated } = useAuth();

    const [lastEvent, setLastEvent] = useState('');

    // The add face's own switches — the form-lifecycle props the view face has no use for.
    const [isAddLoading, setIsAddLoading] = useState(false);
    const [isAddSubmitting, setIsAddSubmitting] = useState(false);
    const [showsAddApiIssues, setShowsAddApiIssues] = useState(false);

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
                title="The shapes"
                lead={
                    <>
                        Annotated literals of every shape this panel takes and emits: the element the view and edit faces run on, the setting rows behind the add face, and the form item the writes hand the page.
                    </>
                }>
                <CodeSample
                    code={contentItemElementShape}
                    caption="contentItem — the self-contained element" />
                <CodeSample
                    code={contentItemSettingShape}
                    caption="One row of contentItemSettingCollection (the add tiles and the fallback)" />
                <CodeSample
                    code={contentItemFormItemShape}
                    caption="What onAdded / onModified emit — ContentItemFormItem" />
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
                        The same card every feed shows, driven by the family’s ONE
                        playground — the identical control surface every template page
                        carries, because the templates inherit it from the dispatcher. As
                        an owner, Edit swaps the card for the editor in place; Save swaps
                        the element back with the amendments; the moderation tier sees the
                        shield, and <code>showModerationSection</code> restyles it into
                        Edit&rsquo;s pencil, standing alone.
                    </>
                }>
                <ContentItemPanelPlayground contentItem={demoStoryItem} />
            </DocSection>
        </ComponentDoc>
    );
}
