import { useState } from 'react';

import {
    ContentItemSearchBarPanel
} from '../../../components/contentItems/contentItemSearchBarPanel';

import {
    ContentItemSearchCriteria,
    emptyContentItemSearchCriteria
} from '../../../models/components/contentItems/contentItemSearchItem';

import { useDocumentTitle } from '../../useDocumentTitle';

import {
    contentItemSettingShape
} from './shared/contentItemShapeSamples';
import { demoSettings } from './shared/contentItemDemoData';

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
ContentItemListPanel
├── ContentItemSearchBarPanel     ◄ this page
└── ContentItemResultsPanel
    └── ContentItemPanel …
`;

const barProps: ReadonlyArray<ComponentPropRow> = [
    {
        name: 'criteria',
        type: 'ContentItemSearchCriteria?',
        description: 'As last committed. Seeds the boxes and RESEEDS them when it changes, so '
            + 'a page landing from ?q= shows what it searched for and a pill-click upstream '
            + 'lands in its box — every filter in play is visible in the advanced options, '
            + 'removable where it stands.'
    },
    {
        name: 'onSearch',
        type: '(criteria) => void',
        description: 'The commit. Pressing Search (or Enter) hands the drafted criteria up; '
            + 'the CONSUMER owns what a search means — a query-string, a fetch, a filter.'
    },
    {
        name: '(the advanced options)',
        type: '—',
        description: 'Category and Shareability are closed lists; Author and Submitted by '
            + 'are free text (a typed submitted-by travels without an account id — only a '
            + 'pill click carries one); Tags and Bible references each collect pills on '
            + 'Enter with their own Any/All match mode — the references wearing the '
            + 'association surface’s blue and its book icon. Everything typed commits on '
            + 'Search, like the query.'
    },
    {
        name: 'showApprovalStatusSearchOptions',
        type: 'boolean',
        defaultValue: 'false',
        description: 'Adds the Approval status checkbox group — Draft, Submitted, Approved, '
            + 'Rejected — to the advanced options. THE ONE OPTION THAT DOES NOT WAIT FOR '
            + 'SEARCH: ticking a box commits there and then, the way a pill-click on a card '
            + 'does, and everything else drafted in the fold-out rides along on that commit. '
            + 'A per-surface decision, threaded from ContentItemListPanel: off for a public '
            + 'feed, on for “my posts” and the admin posts list.'
    },
    {
        name: 'searchApprovalDraftSelected / searchApprovalSubmittedSelected / '
            + 'searchApprovalApprovedSelected / searchApprovalRejectedSelected',
        type: 'boolean ×4',
        defaultValue: 'false / false / true / true',
        description: 'WHICH BOXES START TICKED — the surface’s default selection. They rest '
            + 'at the decided rows (Approved and Rejected), which is what a journal shows; '
            + '/myposts and /Admin/Posts turn all four on. A committed selection in the '
            + 'criteria OVERRIDES them, and unticking the last box hands the surface back to '
            + 'them — “no status at all” is not a search anybody means. The bar only draws '
            + 'the boxes: the page hands the same four to its read (useSearchContentItems’ '
            + 'defaultApprovalStatuses) so the results are read with exactly what is ticked.'
    },
    {
        name: 'contentItemSettingCollection',
        type: 'ContentItemSetting[]',
        defaultValue: '[]',
        description: 'The Category box is built from the per-type DEFAULT rows among these, in '
            + 'the administrator’s own SortOrder. Deliberately NOT filtered by '
            + 'IsAvailableAsGeneralUserContribution: searching is not contributing.'
    }
];

const demoTag = 'Faith';
const demoBibleReference = 'John 3:16';

export function ContentItemSearchBarPanelDoc() {
    useDocumentTitle('Content Item Search Bar Panel — Components — Glory 2 Him');

    // CONTROLLED, the way a real page drives it: onSearch commits back into the criteria,
    // and the criteria reseed the boxes — so the switches below both READ the committed
    // state and WRITE it, exactly what a pill-click on a card does upstream.
    const [criteria, setCriteria] =
        useState<ContentItemSearchCriteria>(emptyContentItemSearchCriteria);

    const [lastSearch, setLastSearch] = useState('');

    const [showsApprovalStatusSearchOptions, setShowsApprovalStatusSearchOptions] =
        useState(true);

    // The four default-selection flags, resting where the component's own defaults rest.
    // They seed the boxes only while the criteria carry no selection — tick a box in the
    // live bar and the committed criteria take over, exactly as on a page.
    const [draftSelected, setDraftSelected] = useState(false);
    const [submittedSelected, setSubmittedSelected] = useState(false);
    const [approvedSelected, setApprovedSelected] = useState(true);
    const [rejectedSelected, setRejectedSelected] = useState(true);

    return (
        <ComponentDoc
            name="Content Item Search Bar Panel"
            filePath="src/components/contentItems/contentItemSearchBarPanel.tsx"
            summary="The search bar of the ContentItemListPanel family: the query box; the
                advanced Category, Author, Submitted by, Shareability, Tags and Bible
                references options (each list with its own Any/All match mode); the
                opt-in Approval status checkboxes, which commit the moment they change;
                and the removable filter chips.">

            <DocSection
                title="Where it stands in the family"
                lead={
                    <>
                        Composed by <code>ContentItemListPanel</code> above the results; a
                        page that has already decided what it shows renders the results without
                        this bar at all. Presentation only: the bar drafts, the consumer
                        commits.
                    </>
                }>
                <CodeSample code={familySample} caption="One tree, every face" />
            </DocSection>

            <DocSection title="Props">
                <PropsTable rows={barProps} />
            </DocSection>

            <DocSection
                title="The shapes"
                lead={
                    <>
                        The setting rows the Category box is built from — defaults only; an override belongs to one item and is never a category.
                    </>
                }>
                <CodeSample
                    code={contentItemSettingShape}
                    caption="One row of contentItemSettingCollection (the Category box reads the defaults)" />
            </DocSection>

            <DocSection
                title="Live"
                lead={
                    <>
                        The switches drive the COMMITTED criteria — each one adds its filter
                        (a <code>#{demoTag}</code> tag, a <code>{demoBibleReference}</code>{' '}
                        reference) and reads back on when the criteria carry one, however it
                        got there. What is typed into the boxes commits on Search, and the
                        switches light up to match. Last committed search:{' '}
                        <code>{lastSearch.length > 0 ? lastSearch : '(none yet)'}</code>
                    </>
                }>
                <DemoControls toggles={[
                    {
                        name: 'tag-filter',
                        label: 'Criteria carry a Tag filter',
                        value: criteria.tags.length > 0,
                        onChange: (isOn) => setCriteria({
                            ...criteria,
                            tags: isOn ? [demoTag] : []
                        })
                    },
                    {
                        name: 'approval-status-search-options',
                        label: 'showApprovalStatusSearchOptions',
                        defaultValue: false,
                        value: showsApprovalStatusSearchOptions,
                        onChange: setShowsApprovalStatusSearchOptions
                    },
                    {
                        name: 'approval-draft-selected',
                        label: 'searchApprovalDraftSelected',
                        defaultValue: false,
                        value: draftSelected,
                        onChange: setDraftSelected
                    },
                    {
                        name: 'approval-submitted-selected',
                        label: 'searchApprovalSubmittedSelected',
                        defaultValue: false,
                        value: submittedSelected,
                        onChange: setSubmittedSelected
                    },
                    {
                        name: 'approval-approved-selected',
                        label: 'searchApprovalApprovedSelected',
                        defaultValue: true,
                        value: approvedSelected,
                        onChange: setApprovedSelected
                    },
                    {
                        name: 'approval-rejected-selected',
                        label: 'searchApprovalRejectedSelected',
                        defaultValue: true,
                        value: rejectedSelected,
                        onChange: setRejectedSelected
                    },
                    {
                        name: 'bible-reference-filter',
                        label: 'Criteria carry a Bible Reference filter',
                        value: criteria.bibleReferences.length > 0,
                        onChange: (isOn) => setCriteria({
                            ...criteria,
                            bibleReferences: isOn ? [demoBibleReference] : []
                        })
                    }
                ]} />

                <LiveDemo>
                    <ContentItemSearchBarPanel
                        criteria={criteria}
                        contentItemSettingCollection={demoSettings}
                        showApprovalStatusSearchOptions={showsApprovalStatusSearchOptions}
                        searchApprovalDraftSelected={draftSelected}
                        searchApprovalSubmittedSelected={submittedSelected}
                        searchApprovalApprovedSelected={approvedSelected}
                        searchApprovalRejectedSelected={rejectedSelected}
                        onSearch={(committed) => {
                            setCriteria(committed);

                            setLastSearch(JSON.stringify({
                                query: committed.query,
                                contentType: committed.contentType,
                                author: committed.author,
                                submittedBy: committed.submittedBy?.name ?? null,
                                tags: committed.tags,
                                tagMatchMode: committed.tagMatchMode,
                                bibleReferences: committed.bibleReferences,
                                bibleReferenceMatchMode: committed.bibleReferenceMatchMode,
                                shareabilityBasis: committed.shareabilityBasis,
                                approvalStatuses: committed.approvalStatuses
                            }));
                        }} />
                </LiveDemo>
            </DocSection>
        </ComponentDoc>
    );
}
