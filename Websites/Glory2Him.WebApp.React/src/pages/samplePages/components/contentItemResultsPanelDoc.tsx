import { useState } from 'react';

import {
    ContentItemResultsPanel
} from '../../../components/contentItems/contentItemResultsPanel';

import { useDocumentTitle } from '../../useDocumentTitle';

import {
    contentItemElementShape
} from './shared/contentItemShapeSamples';
import { demoItems } from './shared/contentItemDemoData';

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
├── ContentItemSearchBarPanel
└── ContentItemResultsPanel       ◄ this page
    └── ContentItemPanel …
`;

const resultsProps: ReadonlyArray<ComponentPropRow> = [
    {
        name: 'contentItemCollection',
        type: 'ContentItemSearchItem[]',
        defaultValue: '[]',
        description: 'The ACCUMULATED results, each element self-contained (item + winning '
            + 'setting). The consumer’s infinite query keeps the pages; this panel appends '
            + 'nothing of its own.'
    },
    {
        name: 'isLoading',
        type: 'boolean',
        defaultValue: 'false',
        description: 'The FIRST page. While it is on, the list is replaced by a loading line '
            + 'rather than emptied, so a re-search does not flash “nothing found” on its way '
            + 'to results.'
    },
    {
        name: 'isLoadingMore / hasMore / onLoadMore',
        type: 'boolean / boolean / () => void',
        description: 'The infinite scroll: a sentinel raises onLoadMore as it nears the '
            + 'viewport while hasMore is on (never while a page is in flight), with a Load '
            + 'more button as the fallback where IntersectionObserver is unavailable.'
    },
    {
        name: 'showModerationSection / showApprovalStatusRibbon',
        type: 'boolean',
        defaultValue: 'false',
        description: 'Threaded to every card — ContentItemPanel owns what each means.'
    },
    {
        name: 'emptyText',
        type: 'string',
        description: 'What an empty, settled list says.'
    }
];

export function ContentItemResultsPanelDoc() {
    useDocumentTitle('Content Item Results Panel — Components — Glory 2 Him');

    const [isLoading, setIsLoading] = useState(false);
    const [isLoadingMore, setIsLoadingMore] = useState(false);
    const [hasMore, setHasMore] = useState(false);
    const [showModerationSection, setShowModerationSection] = useState(false);
    const [showApprovalStatusRibbon, setShowApprovalStatusRibbon] = useState(false);
    const [loadMoreCount, setLoadMoreCount] = useState(0);

    return (
        <ComponentDoc
            name="Content Item Results Panel"
            filePath="src/components/contentItems/contentItemResultsPanel.tsx"
            summary="The results half of the ContentItemListPanel family: every matched item,
                one ContentItemPanel each, scrolled rather than paged.">

            <DocSection
                title="Where it stands in the family"
                lead={
                    <>
                        Composed by <code>ContentItemListPanel</code> beneath the bar, and a
                        page may render it directly when it has no bar to offer. Presentation
                        only: the consumer&rsquo;s infinite query owns the pages — this panel
                        asks for more, never fetches it.
                    </>
                }>
                <CodeSample code={familySample} caption="One tree, every face" />
            </DocSection>

            <DocSection title="Props">
                <PropsTable rows={resultsProps} />
            </DocSection>

            <DocSection
                title="The shapes"
                lead={
                    <>
                        The self-contained element every card in the list renders from — the family's one projection.
                    </>
                }>
                <CodeSample
                    code={contentItemElementShape}
                    caption="One element of contentItemCollection" />
            </DocSection>

            <DocSection
                title="Live"
                lead={
                    <>
                        Every switch here is one of this panel&rsquo;s own props.{' '}
                        <code>onLoadMore</code> raised so far:{' '}
                        <code>{loadMoreCount}</code> — scroll the sentinel into view with{' '}
                        <code>hasMore</code> on, or use the Load more button where it renders.
                    </>
                }>
                <DemoControls toggles={[
                    {
                        name: 'is-loading',
                        label: 'isLoading (first page in flight)',
                    defaultValue: false,
                        value: isLoading,
                        onChange: setIsLoading
                    },
                    {
                        name: 'is-loading-more',
                        label: 'isLoadingMore (next page in flight)',
                    defaultValue: false,
                        value: isLoadingMore,
                        onChange: setIsLoadingMore
                    },
                    {
                        name: 'has-more',
                        label: 'hasMore (another page exists)',
                    defaultValue: false,
                        value: hasMore,
                        onChange: setHasMore
                    },
                    {
                        name: 'is-moderated-view',
                        label: 'showModerationSection (Moderate wears Edit’s clothes)',
                    defaultValue: false,
                        value: showModerationSection,
                        onChange: setShowModerationSection
                    },
                    {
                        name: 'show-approval-status-ribbon',
                        label: 'showApprovalStatusRibbon (status corner ribbons)',
                    defaultValue: false,
                        value: showApprovalStatusRibbon,
                        onChange: setShowApprovalStatusRibbon
                    }
                ]} />

                <LiveDemo>
                    <ContentItemResultsPanel
                        contentItemCollection={demoItems}
                        isLoading={isLoading}
                        isLoadingMore={isLoadingMore}
                        hasMore={hasMore}
                        showModerationSection={showModerationSection}
                        showApprovalStatusRibbon={showApprovalStatusRibbon}
                        onLoadMore={() => setLoadMoreCount((count) => count + 1)} />
                </LiveDemo>
            </DocSection>
        </ComponentDoc>
    );
}
