import { useDocumentTitle } from '../../useDocumentTitle';
import { demoQuoteItem } from './shared/contentItemDemoData';
import { ContentItemPanelPlayground } from './shared/contentItemPanelPlayground';

import {
    CodeSample,
    ComponentDoc,
    ComponentPropRow,
    DocSection,
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
        description: 'The same decided bundle, passed straight through — the status pair, '
            + 'the section switches, the content length, the engagement row and every event '
            + 'hook behave identically because they ARE the default template’s, inherited '
            + 'by composition. See the Content Item Default Panel page for the full table.'
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
                        override is exactly one line there. Because it derives from the
                        default template, its control surface is the default&rsquo;s, whole:
                        the playground below is the same one every page in the family carries.
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
                        The full family control surface over a quote rendering through this
                        override — security context, ribbon status, the status pair, and the
                        way into the edit face as an owner.
                    </>
                }>
                <ContentItemPanelPlayground contentItem={demoQuoteItem} />
            </DocSection>
        </ComponentDoc>
    );
}
