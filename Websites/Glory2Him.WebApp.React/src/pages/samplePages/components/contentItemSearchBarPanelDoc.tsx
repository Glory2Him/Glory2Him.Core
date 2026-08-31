import { useState } from 'react';

import {
    ContentItemSearchBarPanel
} from '../../../components/contentItems/contentItemSearchBarPanel';

import {
    ContentItemSearchCriteria,
    emptyContentItemSearchCriteria
} from '../../../models/components/contentItems/contentItemSearchItem';

import { useDocumentTitle } from '../../useDocumentTitle';
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

    return (
        <ComponentDoc
            name="Content Item Search Bar Panel"
            filePath="src/components/contentItems/contentItemSearchBarPanel.tsx"
            summary="The search bar of the ContentItemListPanel family: the query box; the
                advanced Category, Author, Submitted by, Shareability, Tags and Bible
                references options (each list with its own Any/All match mode); and the
                removable filter chips.">

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
                                shareabilityBasis: committed.shareabilityBasis
                            }));
                        }} />
                </LiveDemo>
            </DocSection>

            <DocSection title="Props">
                <PropsTable rows={barProps} />
            </DocSection>
        </ComponentDoc>
    );
}
