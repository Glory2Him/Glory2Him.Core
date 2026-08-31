import { useState } from 'react';

import {
    ContentItemQuotesPanel
} from '../../../components/contentItems/contentItemQuotesPanel';

import { useDocumentTitle } from '../../useDocumentTitle';
import { demoQuoteItem, demoReactionOptions } from './shared/contentItemDemoData';

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
ContentItemPanel
├── ContentItemAddPanel
├── ContentItemEditPanel
├── ContentItemDefaultPanel         the meta row, pills and engagement row live here
└── ContentItemQuotesPanel          ◄ this page — derives via contentSlot
`;

const derivationSample = `
// AN OVERRIDE DERIVES FROM THE DEFAULT TEMPLATE by rendering it with contentSlot
// replaced — the React register of inheritance. What an override may change is how
// the CONTENT reads; what it may not change is what the card offers.
export function ContentItemQuotesPanel(props: ContentItemTemplateProps) {
    return (
        <ContentItemDefaultPanel
            {...props}
            contentSlot={/* the quote face: words large, author beneath */} />
    );
}
`;

const quoteProps: ReadonlyArray<ComponentPropRow> = [
    {
        name: '…everything ContentItemDefaultPanel takes',
        type: 'ContentItemTemplateProps',
        description: 'The same decided bundle, passed straight through — the section '
            + 'switches, the ribbon, the engagement row and every event hook behave '
            + 'identically because they ARE the default template’s, inherited by '
            + 'composition. See the Content Item Default Panel page for the full table.'
    },
    {
        name: '(the quote face)',
        type: '—',
        description: 'The words stand large over the item’s image when one exists, and on a '
            + 'quiet light block when none does — no stock photo under somebody’s words. '
            + 'The author reads appended, “— author”.'
    }
];

export function ContentItemQuotesPanelDoc() {
    useDocumentTitle('Content Item Quotes Panel — Components — Glory 2 Him');

    const [shouldShowRibbons, setShouldShowRibbons] = useState(false);
    const [showReactionSection, setShowReactionSection] = useState(true);
    const [showShareSection, setShowShareSection] = useState(true);
    const [isReactionPickerOpen, setIsReactionPickerOpen] = useState(false);
    const [lastEvent, setLastEvent] = useState('');

    return (
        <ComponentDoc
            name="Content Item Quotes Panel"
            filePath="src/components/contentItems/contentItemQuotesPanel.tsx"
            summary="The Quote override of the ContentItemPanel view templates: the words
                stand large, the author beneath — everything else is the default template,
                inherited by composition.">

            <DocSection
                title="Where it stands in the family"
                lead={
                    <>
                        Registered against <code>ContentType.Quote</code> in{' '}
                        <code>ContentItemPanel</code>&rsquo;s template registry — adding an
                        override is exactly one line there.
                    </>
                }>
                <CodeSample code={familySample} caption="One tree, every face" />
                <CodeSample code={derivationSample} caption="Derivation, the React way" />
            </DocSection>

            <DocSection title="Props">
                <PropsTable rows={quoteProps} />
            </DocSection>

            <DocSection
                title="Live"
                lead={
                    <>
                        The switches are the template&rsquo;s own props, inherited from the
                        default — flip them and the shared rows react exactly as they do on
                        every other card. Last event:{' '}
                        <code>{lastEvent.length > 0 ? lastEvent : '(none yet)'}</code>
                    </>
                }>
                <DemoControls toggles={[
                    {
                        name: 'quote-ribbons',
                        label: 'shouldShowRibbons (status corner ribbon)',
                        value: shouldShowRibbons,
                        onChange: setShouldShowRibbons
                    },
                    {
                        name: 'quote-reaction-section',
                        label: 'showReactionSection',
                        value: showReactionSection,
                        onChange: setShowReactionSection
                    },
                    {
                        name: 'quote-share-section',
                        label: 'showShareSection',
                        value: showShareSection,
                        onChange: setShowShareSection
                    },
                    {
                        name: 'quote-picker-open',
                        label: 'isReactionPickerOpen (Like choices open)',
                        value: isReactionPickerOpen,
                        onChange: setIsReactionPickerOpen
                    }
                ]} />

                <LiveDemo>
                    <ContentItemQuotesPanel
                        contentItem={demoQuoteItem}
                        contentItemSetting={demoQuoteItem.contentItemSetting}
                        contentTypeName="Quote"
                        offeredReactions={showReactionSection ? demoReactionOptions : []}
                        showsEditButton={false}
                        showsModerateButton={false}
                        moderateButtonIconCss="bi bi-shield"
                        moderateButtonLabel="Moderate"
                        shouldShowRibbons={shouldShowRibbons}
                        showReactionSection={showReactionSection}
                        showShareSection={showShareSection}
                        areReactionCountsExpanded={false}
                        onAssignedReactionsClick={() => { }}
                        isReactionPickerOpen={isReactionPickerOpen}
                        onReactionClick={() =>
                            setIsReactionPickerOpen(!isReactionPickerOpen)}
                        onReactionSelected={(_item, reaction) => {
                            setIsReactionPickerOpen(false);
                            setLastEvent(`onReactionSelected(${reaction.label})`);
                        }}
                        onShareClick={(item) => setLastEvent(`onShareClick(${item.id})`)}
                        onAuthorClick={(item) =>
                            setLastEvent(`onAuthorClick(${item.author ?? ''})`)} />
                </LiveDemo>
            </DocSection>
        </ComponentDoc>
    );
}
