import { useState } from 'react';

import {
    ContentItemVersesPanel
} from '../../../components/contentItems/contentItemVersesPanel';

import { useDocumentTitle } from '../../useDocumentTitle';
import { demoVersesItem } from './shared/contentItemDemoData';

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

    const [shouldShowRibbons, setShouldShowRibbons] = useState(false);
    const [showBibleReferenceSection, setShowBibleReferenceSection] = useState(true);
    const [lastEvent, setLastEvent] = useState('');

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
                        override arrives — one line, seeds and all.
                    </>
                }>
                <CodeSample code={familySample} caption="One tree, every face" />
            </DocSection>

            <DocSection
                title="Live"
                lead={
                    <>
                        The switches are the template&rsquo;s own props, inherited from the
                        default. Last event:{' '}
                        <code>{lastEvent.length > 0 ? lastEvent : '(none yet)'}</code>
                    </>
                }>
                <DemoControls toggles={[
                    {
                        name: 'verses-ribbons',
                        label: 'shouldShowRibbons (status corner ribbon)',
                        value: shouldShowRibbons,
                        onChange: setShouldShowRibbons
                    },
                    {
                        name: 'verses-bible-reference-section',
                        label: 'showBibleReferenceSection',
                        value: showBibleReferenceSection,
                        onChange: setShowBibleReferenceSection
                    }
                ]} />

                <LiveDemo>
                    <ContentItemVersesPanel
                        contentItem={demoVersesItem}
                        contentItemSetting={demoVersesItem.contentItemSetting}
                        contentTypeName="Verse Image"
                        offeredReactions={[]}
                        showsEditButton={false}
                        showsModerateButton={false}
                        moderateButtonIconCss="bi bi-shield"
                        moderateButtonLabel="Moderate"
                        shouldShowRibbons={shouldShowRibbons}
                        showBibleReferenceSection={showBibleReferenceSection}
                        areReactionCountsExpanded={false}
                        onAssignedReactionsClick={() => { }}
                        isReactionPickerOpen={false}
                        onReactionClick={() => { }}
                        onBibleReferenceClick={(_item, reference) =>
                            setLastEvent(`onBibleReferenceClick(${reference})`)} />
                </LiveDemo>
            </DocSection>

            <DocSection title="Props">
                <PropsTable rows={versesProps} />
            </DocSection>
        </ComponentDoc>
    );
}
