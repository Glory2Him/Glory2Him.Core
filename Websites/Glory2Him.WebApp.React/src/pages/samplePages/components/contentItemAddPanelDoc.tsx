import { useState } from 'react';
import { ContentItemAddPanel } from '../../../components/contentItems/contentItemAddPanel';
import { ContentType } from '../../../models/foundations/contentItemSettings/contentType';
import { useDocumentTitle } from '../../useDocumentTitle';

import {
    ApprovalStatus
} from '../../../models/components/contentItems/contentItemFormItem';

import {
    approvalStatusMemberNames
} from '../../../models/components/contentItems/contentItemTemplate';

import {
    contentItemFormItemShape,
    contentItemSettingShape
} from './shared/contentItemShapeSamples';
import { demoSettings } from './shared/contentItemDemoData';
import { DemoSecurityContext } from './shared/securityContextDemo';

import {
    CodeSample,
    ComponentDoc,
    ComponentPropRow,
    DemoControls,
    DemoRadioGroup,
    DemoRadioOption,
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

// THE CONTRIBUTION GATES, as a live board. The add face asks exactly two role questions — is
// this account held back (blockRoles), and may it contribute at all (addRoles) — and the block
// is asked FIRST and outranks every grant, #366. Each option below moves the role STRINGS and
// the roles the demo reader HOLDS together, because a board that moved only one half could
// never open or close a gate.
type ContributionGate = {
    key: string;
    label: string;
    roles: ReadonlyArray<string>;
    addRoles?: string;
    blockRoles?: string;
};

const contributionGates: ReadonlyArray<ContributionGate> = [
    {
        key: 'open',
        label: 'Open to any signed-in reader (the default)',
        roles: []
    },
    {
        key: 'restricted',
        label: 'addRoles = Contributors — which this reader lacks',
        roles: [],
        addRoles: 'Contributors'
    },
    {
        key: 'granted',
        label: 'addRoles = Contributors — and this reader holds it',
        roles: ['Contributors'],
        addRoles: 'Contributors'
    },
    {
        key: 'blocked',
        label: 'ContentItem-ReadOnly — the block outranks the grant',
        roles: ['Contributors', 'ContentItem-ReadOnly'],
        addRoles: 'Contributors'
    }
];

const gateOptions: ReadonlyArray<DemoRadioOption> =
    contributionGates.map((gate) => ({ key: gate.key, label: gate.label }));

// What an untouched "Submit as" row opens on. Only the two a contributor owns are meaningful:
// a decided status names a state the row does not render for, which on a face with no item
// would leave the question unanswerable.
const approvalStatusDefaultOptions: ReadonlyArray<DemoRadioOption> = [
    { key: String(ApprovalStatus.Submitted), label: 'Submitted (the default)' },
    { key: String(ApprovalStatus.Draft), label: 'Draft — a surface that files drafts' }
];

// The other half of the author prefill: whose name an owned basis fills in. Unset, the
// signed-in reader's own — who on the add face IS the contributor.
const demoSubmittedByDisplayName = 'Grace Abara';

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
        name: 'submitAsLabelText',
        type: 'string',
        defaultValue: '“Submit as”',
        description: 'The last row of the form, over the buttons: which state to file the '
            + 'contribution in. It offers the two a contributor owns and no others — '
            + 'Submitted and Draft — and drives approvalStatus on the emitted projection.'
    },
    {
        name: 'approvalStatusDefault',
        type: 'ApprovalStatus',
        defaultValue: 'Submitted',
        description: 'What that row OPENS on, which on this face is always the answer — '
            + 'there is no item yet to name a status. Submitted by default: the contribution '
            + 'page exists to put work in front of a reviewer, and the button under the row '
            + 'says so. Threaded through ContentItemPanel.'
    },
    {
        name: 'submittedByDisplayName',
        type: 'string?',
        description: 'Whose name an OWNED basis prefills into the Author field. Unset, the '
            + 'signed-in reader’s own — who on the add face IS the contributor. The prefill '
            + 'fills an empty field only, and stops the moment the contributor types: a pen '
            + 'name is theirs to keep.'
    },
    {
        name: 'blockRoles / addRoles',
        type: 'string?',
        description: 'Comma-separated role overrides; {ContentType} resolves against the '
            + 'selected type, and the ReadOnly block outranks every grant (#366). An empty '
            + 'addRoles means any authenticated reader may contribute, which is the design’s '
            + 'position — there is no Contributor role (§18.6).'
    },
    {
        name: 'entityType',
        type: 'string',
        defaultValue: '“ContentItem”',
        description: 'Names the entity the role sets are composed from per §18.6 — capability '
            + 'LAST and plural. Only ContentItem carries the content-type tier.'
    },
    {
        name: 'loginHref / loginButtonText / loginButtonCssClass',
        type: 'string?',
        description: 'The face shown when nobody is signed in — the form yields to a login '
            + 'affordance. The href defaults to the current path as the return url, exactly '
            + 'as SecuredRoute builds it, so the reader lands back here afterwards.'
    },
    {
        name: 'showBorder / cssClass / titleText / ariaLabel',
        type: 'boolean / string',
        defaultValue: 'false / “” / “” / “Content item”',
        description: 'The panel’s own frame and heading. With no visible title, ariaLabel is '
            + 'what names the section for a screen reader.'
    },
    {
        name: 'typePickerTitleText / titleLabelText / authorLabelText / …',
        type: 'string',
        description: 'Every visible string is an override with a stated default — the picker’s '
            + 'question, each field label and placeholder, the button pair, and the refusal '
            + 'messages. See ContentItemFormPanelProps for the full set.'
    },
    {
        name: 'submitButtonCssClass',
        type: 'string',
        defaultValue: '“btn-primary”',
        description: 'Theme CLASSES, never colours, so every control follows light and dark.'
    }
];

export function ContentItemAddPanelDoc() {
    useDocumentTitle('Content Item Add Panel — Components — Glory 2 Him');

    const [isLoading, setIsLoading] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [showsApiIssues, setShowsApiIssues] = useState(false);
    const [showBorder, setShowBorder] = useState(false);
    const [hasSubmittedByDisplayName, setHasSubmittedByDisplayName] = useState(false);
    const [gateKey, setGateKey] = useState(contributionGates[0].key);

    const [approvalStatusDefaultKey, setApprovalStatusDefaultKey] =
        useState(String(ApprovalStatus.Submitted));

    const [lastEvent, setLastEvent] = useState('');

    const gate = contributionGates.find((candidate) => candidate.key === gateKey)
        ?? contributionGates[0];

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
                        Every switch is one of this template&rsquo;s own props. The GATES board
                        moves <code>blockRoles</code>, <code>addRoles</code> and the roles the
                        demo reader holds together, so each option actually opens or closes the
                        surface: a grant this reader lacks refuses the form, and a
                        <code>ReadOnly</code> block refuses it even while the grant is held
                        (#366). The gates are presentation gates — the server re-decides every
                        write — so the demo may honestly step into any of them. Last event:{' '}
                        <code>{lastEvent.length > 0 ? lastEvent : '(none yet)'}</code>
                    </>
                }>
                <DemoRadioGroup
                    title="Contribution gates"
                    name="add-contribution-gate"
                    options={gateOptions}
                    selectedKey={gateKey}
                    onChange={setGateKey} />

                <DemoRadioGroup
                    title="approvalStatusDefault (what Submit as opens on)"
                    name="add-approval-status-default"
                    options={approvalStatusDefaultOptions}
                    selectedKey={approvalStatusDefaultKey}
                    onChange={setApprovalStatusDefaultKey} />

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
                    },
                    {
                        name: 'show-border',
                        label: 'showBorder (the panel wears its own frame)',
                        defaultValue: false,
                        value: showBorder,
                        onChange: setShowBorder
                    },
                    {
                        name: 'submitted-by-display-name',
                        label: `submittedByDisplayName (prefill ${demoSubmittedByDisplayName})`,
                        defaultValue: false,
                        value: hasSubmittedByDisplayName,
                        onChange: setHasSubmittedByDisplayName
                    }
                ]} />

                <LiveDemo>
                    {/* Ownership is moot on a face with no item — the add gates ask about
                        ROLES alone, so the wrapper is here for the roles it lends the
                        reader. */}
                    <DemoSecurityContext
                        option={{ ...gate, isOwner: true }}>

                        <ContentItemAddPanel
                            contentItemSettingCollection={demoSettings}
                            addRoles={gate.addRoles}
                            blockRoles={gate.blockRoles}
                            approvalStatusDefault={
                                Number(approvalStatusDefaultKey) as ApprovalStatus}
                            submittedByDisplayName={hasSubmittedByDisplayName
                                ? demoSubmittedByDisplayName
                                : undefined}
                            showBorder={showBorder}
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
                                setLastEvent(`onAdded(${ContentType[item.contentType]}`
                                    + `, ${approvalStatusMemberNames[
                                        item.approvalStatus ?? ApprovalStatus.Draft]})`)}
                            onCancelled={() => setLastEvent('onCancelled()')} />
                    </DemoSecurityContext>
                </LiveDemo>
            </DocSection>
        </ComponentDoc>
    );
}
