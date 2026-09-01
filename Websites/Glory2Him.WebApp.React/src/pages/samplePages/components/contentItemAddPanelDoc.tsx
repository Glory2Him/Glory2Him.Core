import { useState } from 'react';
import { ContentItemAddPanel } from '../../../components/contentItems/contentItemAddPanel';
import { ContentType } from '../../../models/foundations/contentItemSettings/contentType';
import { useDocumentTitle } from '../../useDocumentTitle';

import {
    contentItemFormItemShape,
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
ContentItemPanel
├── ContentItemAddPanel           ◄ this page (the add template)
├── ContentItemEditPanel
├── ContentItemDefaultPanel
└── ContentItem{ContentType}Panel …
`;

const addProps: ReadonlyArray<ComponentPropRow> = [
    {
        name: 'contentItemSettingCollection',
        type: 'ContentItemSetting[]',
        defaultValue: '[]',
        description: 'The picker’s tiles: the content type DEFAULTS carrying '
            + 'IsAvailableAsGeneralUserContribution, in the rows’ own SortOrder. The chosen '
            + 'type’s effective setting shapes the fields (hasTitle, hasAuthor).'
    },
    {
        name: 'isLoading',
        type: 'boolean',
        defaultValue: 'false',
        description: 'A loading line instead of a half-built form.'
    },
    {
        name: 'isSubmitting',
        type: 'boolean',
        defaultValue: 'false',
        description: 'Freezes the buttons while the consumer is persisting, so one click is '
            + 'one write.'
    },
    {
        name: 'validationIssues',
        type: 'Record<string, string[]>?',
        description: 'What the API said was wrong, keyed by ITS parameter names — matched to '
            + 'fields case-insensitively; anything unplaced lands in a summary. The one '
            + 'client-side rule the form decides itself is the mandatory permission note '
            + 'under a permission basis.'
    },
    {
        name: 'onAdded / onCancelled',
        type: '(item) => void / () => void',
        description: 'What the reader decided. The CONSUMER owns persistence — the POST, the '
            + 'redirect, and the validation readback are the page’s work.'
    },
    {
        name: 'blockRoles / addRoles',
        type: 'string?',
        description: 'Comma-separated role overrides; {ContentType} resolves against the '
            + 'selected type, and the ReadOnly block outranks every grant (#366).'
    }
];

export function ContentItemAddPanelDoc() {
    useDocumentTitle('Content Item Add Panel — Components — Glory 2 Him');

    const [isLoading, setIsLoading] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [showsApiIssues, setShowsApiIssues] = useState(false);
    const [lastEvent, setLastEvent] = useState('');

    return (
        <ComponentDoc
            name="Content Item Add Panel"
            filePath="src/components/contentItems/contentItemAddPanel.tsx"
            summary="The add template of the ContentItemPanel family: the type picker and a
                blank form. ContentItemPanel dispatches here when it is handed a settings
                collection and no item; a page renders it directly for deep overrides.">

            <DocSection
                title="Where it stands in the family"
                lead={
                    <>
                        The form engine behind this template and{' '}
                        <code>ContentItemEditPanel</code> is one component, so the two writing
                        faces cannot drift: same fields, same shaping, same mandatory
                        permission rule, same validation readback.
                    </>
                }>
                <CodeSample code={familySample} caption="One tree, every face" />
            </DocSection>

            <DocSection title="Props">
                <PropsTable rows={addProps} />
            </DocSection>

            <DocSection
                title="The shapes"
                lead={
                    <>
                        The setting rows the picker offers and the fields shape from, and the form item a submit hands the page.
                    </>
                }>
                <CodeSample
                    code={contentItemSettingShape}
                    caption="One row of contentItemSettingCollection (the picker's tiles)" />
                <CodeSample
                    code={contentItemFormItemShape}
                    caption="What onAdded emits — ContentItemFormItem" />
            </DocSection>

            <DocSection
                title="Live"
                lead={
                    <>
                        Every switch is one of this template&rsquo;s own props. Signed out, the
                        form yields to a login affordance. Last event:{' '}
                        <code>{lastEvent.length > 0 ? lastEvent : '(none yet)'}</code>
                    </>
                }>
                <DemoControls toggles={[
                    {
                        name: 'is-loading',
                        label: 'isLoading (settings still arriving)',
                    defaultValue: false,
                        value: isLoading,
                        onChange: setIsLoading
                    },
                    {
                        name: 'is-submitting',
                        label: 'isSubmitting (a write in flight)',
                    defaultValue: false,
                        value: isSubmitting,
                        onChange: setIsSubmitting
                    },
                    {
                        name: 'api-issues',
                        label: 'validationIssues (an API readback)',
                    defaultValue: false,
                        value: showsApiIssues,
                        onChange: setShowsApiIssues
                    }
                ]} />

                <LiveDemo>
                    <ContentItemAddPanel
                        contentItemSettingCollection={demoSettings}
                        isLoading={isLoading}
                        isSubmitting={isSubmitting}
                        validationIssues={showsApiIssues
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
        </ComponentDoc>
    );
}
