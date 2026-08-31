import { useState } from 'react';
import { ContentItemEditPanel } from '../../../components/contentItems/contentItemEditPanel';

import {
    ContentItemFormItem
} from '../../../models/components/contentItems/contentItemFormItem';

import { useDocumentTitle } from '../../useDocumentTitle';
import { demoSettings, demoStoryItem } from './shared/contentItemDemoData';

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
├── ContentItemEditPanel          ◄ this page (the edit template)
├── ContentItemDefaultPanel
└── ContentItem{ContentType}Panel …
`;

// The same demo story every page in the family shows, in its editor register: submittedById
// becomes createdBy, the audit name the [OWNER] gate decides on.
const demoFormItem: ContentItemFormItem = {
    id: demoStoryItem.id,
    contentType: demoStoryItem.contentType,
    contentItemSetting: demoStoryItem.contentItemSetting,
    title: demoStoryItem.title ?? '',
    author: demoStoryItem.author ?? '',
    content: demoStoryItem.content,
    shareabilityBasis: demoStoryItem.shareabilityBasis ?? 3,
    sharePermission: demoStoryItem.sharePermission ?? '',
    createdBy: demoStoryItem.submittedById,
    approvalStatus: demoStoryItem.approvalStatus
};

const editProps: ReadonlyArray<ComponentPropRow> = [
    {
        name: 'contentItem',
        type: 'ContentItemFormItem',
        description: 'The item under amendment, seeding every field. The content type is '
            + 'create-only (§12.4.1 rule 7a), so the editor states it as a frozen chip rather '
            + 'than offering the picker. Hand over a STABLE object — the editor reseeds when '
            + 'its identity changes.'
    },
    {
        name: 'isEditingAllowed',
        type: 'boolean',
        defaultValue: 'false',
        description: 'The surface switch, ahead of every role check. Off, the editor refuses '
            + 'outright — there is no read face here to fall back to. The role gates then '
            + 'decide per person: the owner at any status, the publisher tier on a live item, '
            + 'never the reviewer tier; a ReadOnly block outranks everything (#366).'
    },
    {
        name: 'shouldShowRibbons',
        type: 'boolean',
        defaultValue: 'false',
        description: 'The status corner ribbon, on the editor too — an amendment should not '
            + 'hide what state the row is in.'
    },
    {
        name: 'isSubmitting',
        type: 'boolean',
        defaultValue: 'false',
        description: 'Freezes Save, Cancel and Delete while the consumer is persisting.'
    },
    {
        name: 'onModified / onRemoved / onCancelled',
        type: '(item) => void',
        description: 'What the reader decided. Whether onModified is a PUT or, on a terminal '
            + 'item, a fork into a new version (§3.4 rule 16) is the page’s business; '
            + 'onRemoved fires only after the confirm dialog — removal rides on the editor.'
    }
];

export function ContentItemEditPanelDoc() {
    useDocumentTitle('Content Item Edit Panel — Components — Glory 2 Him');

    const [isEditingAllowed, setIsEditingAllowed] = useState(true);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [shouldShowRibbons, setShouldShowRibbons] = useState(true);
    const [lastEvent, setLastEvent] = useState('');

    return (
        <ComponentDoc
            name="Content Item Edit Panel"
            filePath="src/components/contentItems/contentItemEditPanel.tsx"
            summary="The edit template of the ContentItemPanel family: the frozen type and the
                seeded form, with removal riding on it. ContentItemPanel dispatches here when
                its Edit affordance is taken in place.">

            <DocSection
                title="Where it stands in the family"
                lead={
                    <>
                        The same form engine as <code>ContentItemAddPanel</code> with an item:
                        the fields shape from the item&rsquo;s own embedded setting, a field a
                        policy hides survives a save untouched, and a permission note the
                        reader withdraws is dropped.
                    </>
                }>
                <CodeSample code={familySample} caption="One tree, every face" />
            </DocSection>

            <DocSection title="Props">
                <PropsTable rows={editProps} />
            </DocSection>

            <DocSection
                title="Live"
                lead={
                    <>
                        Every switch is one of this template&rsquo;s own props. The role gates
                        are real: this demo row is amendable (Draft) and your admin session
                        qualifies through the default edit roles — flip{' '}
                        <code>isEditingAllowed</code> off and the editor refuses. Last event:{' '}
                        <code>{lastEvent.length > 0 ? lastEvent : '(none yet)'}</code>
                    </>
                }>
                <DemoControls toggles={[
                    {
                        name: 'is-editing-allowed',
                        label: 'isEditingAllowed (the surface switch)',
                        value: isEditingAllowed,
                        onChange: setIsEditingAllowed
                    },
                    {
                        name: 'is-submitting',
                        label: 'isSubmitting (a write in flight)',
                        value: isSubmitting,
                        onChange: setIsSubmitting
                    },
                    {
                        name: 'should-show-ribbons',
                        label: 'shouldShowRibbons (status corner ribbon)',
                        value: shouldShowRibbons,
                        onChange: setShouldShowRibbons
                    }
                ]} />

                <LiveDemo>
                    <ContentItemEditPanel
                        contentItem={demoFormItem}
                        contentItemSettingCollection={demoSettings}
                        isEditingAllowed={isEditingAllowed}
                        isSubmitting={isSubmitting}
                        shouldShowRibbons={shouldShowRibbons}
                        onModified={(item) => setLastEvent(`onModified(${item.id})`)}
                        onRemoved={(item) => setLastEvent(`onRemoved(${item.id})`)}
                        onCancelled={() => setLastEvent('onCancelled()')} />
                </LiveDemo>
            </DocSection>
        </ComponentDoc>
    );
}
