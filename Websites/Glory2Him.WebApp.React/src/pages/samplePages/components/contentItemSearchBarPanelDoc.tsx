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
ContentItemSearchPanel
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
            + 'is reflected here. Chips render for a submittedBy or tag criterion, each with '
            + 'its own remove control.'
    },
    {
        name: 'onSearch',
        type: '(criteria) => void',
        description: 'The commit. Pressing Search (or Enter) hands the drafted criteria up; '
            + 'the CONSUMER owns what a search means — a query-string, a fetch, a filter.'
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

export function ContentItemSearchBarPanelDoc() {
    useDocumentTitle('Content Item Search Bar Panel — Components — Glory 2 Him');

    const [withSubmittedByChip, setWithSubmittedByChip] = useState(false);
    const [withTagChip, setWithTagChip] = useState(false);
    const [lastSearch, setLastSearch] = useState('');

    const criteria: ContentItemSearchCriteria = {
        ...emptyContentItemSearchCriteria,
        submittedBy: withSubmittedByChip
            ? { id: 'demo-user', name: 'Grace Abara' }
            : null,
        tag: withTagChip ? 'providence' : null
    };

    return (
        <ComponentDoc
            name="Content Item Search Bar Panel"
            filePath="src/components/contentItems/contentItemSearchBarPanel.tsx"
            summary="The search bar of the ContentItemSearchPanel family: the query box, the
                advanced Category and Author options, and the removable filter chips.">

            <DocSection
                title="Where it stands in the family"
                lead={
                    <>
                        Composed by <code>ContentItemSearchPanel</code> above the results; a
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
                        Flip a chip on and the bar reseeds from the criteria — exactly what a
                        pill-click on a card does upstream. The last committed search this page
                        received: <code>{lastSearch.length > 0 ? lastSearch : '(none yet)'}</code>
                    </>
                }>
                <DemoControls toggles={[
                    {
                        name: 'submitted-by-chip',
                        label: 'Criteria carry a Submitted-by filter',
                        value: withSubmittedByChip,
                        onChange: setWithSubmittedByChip
                    },
                    {
                        name: 'tag-chip',
                        label: 'Criteria carry a Tag filter',
                        value: withTagChip,
                        onChange: setWithTagChip
                    }
                ]} />

                <LiveDemo>
                    <ContentItemSearchBarPanel
                        criteria={criteria}
                        contentItemSettingCollection={demoSettings}
                        onSearch={(committed) => setLastSearch(JSON.stringify({
                            query: committed.query,
                            contentType: committed.contentType,
                            author: committed.author,
                            submittedBy: committed.submittedBy?.name ?? null,
                            tag: committed.tag
                        }))} />
                </LiveDemo>
            </DocSection>

            <DocSection title="Props">
                <PropsTable rows={barProps} />
            </DocSection>
        </ComponentDoc>
    );
}
