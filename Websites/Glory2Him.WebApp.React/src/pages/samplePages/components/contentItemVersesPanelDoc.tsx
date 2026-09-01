import { useDocumentTitle } from '../../useDocumentTitle';
import { demoVersesItem } from './shared/contentItemDemoData';
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
└── ContentItemVersesPanel          ◄ this page — derives via contentSlot
`;

const versesProps: ReadonlyArray<ComponentPropRow> = [
    {
        name: '…everything ContentItemDefaultPanel takes',
        type: 'ContentItemTemplateProps',
        description: 'The same decided bundle, passed straight through — see the Content '
            + 'Item Default Panel page for the full table. The purple Verses chip is the '
            + 'measured palette in contentItems.css, keyed off data-content-type like every '
            + 'other type.'
    },
    {
        name: '(the verse face)',
        type: '—',
        description: 'The verse stands large — the whole, wrapped like the prose it is — '
            + 'over the item’s image when one exists and on a quiet light block when none '
            + 'does. Unlike the quote face, NOTHING is appended: the reference lives inside '
            + 'the verse text itself, and the scripture is not signed.'
    }
];

export function ContentItemVersesPanelDoc() {
    useDocumentTitle('Content Item Verses Panel — Components — Glory 2 Him');

    return (
        <ComponentDoc
            name="Content Item Verses Panel"
            filePath="src/components/contentItems/contentItemVersesPanel.tsx"
            summary="The Verses override of the ContentItemPanel view templates: the verse
                whole, standing large — everything else is the default template, inherited by
                composition.">

            <DocSection
                title="Where it stands in the family"
                lead={
                    <>
                        Registered against <code>ContentType.Verses</code> in{' '}
                        <code>ContentItemPanel</code>&rsquo;s template registry, the way every
                        override arrives — one line, seeds and all. Because it derives from
                        the default template, its control surface is the default&rsquo;s,
                        whole: the playground below is the same one every page in the family
                        carries.
                    </>
                }>
                <CodeSample code={familySample} caption="One tree, every face" />
            </DocSection>

            <DocSection title="Props">
                <PropsTable rows={versesProps} />
            </DocSection>

            <DocSection
                title="Live"
                lead={
                    <>
                        The full family control surface over a verse rendering through this
                        override — security context, ribbon status, the status pair, and the
                        way into the edit face as an owner.
                    </>
                }>
                <ContentItemPanelPlayground contentItem={demoVersesItem} />
            </DocSection>
        </ComponentDoc>
    );
}
