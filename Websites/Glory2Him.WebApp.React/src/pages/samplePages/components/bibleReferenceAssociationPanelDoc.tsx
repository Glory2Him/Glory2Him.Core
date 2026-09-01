import { useState } from 'react';
import { Link } from 'react-router-dom';
import { BibleReferenceAssociationPanel } from '../../../components/associations/bibleReferenceAssociationPanel';
import { useAuth } from '../../../components/securitys/authProvider';
import {
    ApprovalStatus,
    AssociationItem
} from '../../../models/components/associations/associationItem';
import { useDocumentTitle } from '../../useDocumentTitle';
import {
    CodeSample,
    ComponentDoc,
    ComponentPropRow,
    DocSection,
    LiveDemo,
    PropsTable
} from './shared/componentDoc';

const minimalSample = `
import { BibleReferenceAssociationPanel } from '../../components/associations/bibleReferenceAssociationPanel';

// The book icon, the blue chips, the USFM deep links and the prompts are all defaults.
<BibleReferenceAssociationPanel
    associationCollection={references}
    onAdd={suggestReferenceAsync}
    onRemove={removeReferenceAsync}
    onApprove={approveReferenceAsync}
    onReject={denyReferenceAsync} />
`;

const hrefSample = `
// The chip reads as the post cites it and addresses as the deep-link route parses it, which is
// why the href comes from bibleReferenceHref rather than from the label:
//
//   "Romans 3:23"          ->  /BibleReferences/ROM.3.23
//   "Joshua 10:8, 12-13"   ->  /BibleReferences/JOS.10          (a multi-part citation lands on
//                                                                its opening chapter)
//   "not a reference"      ->  /Search?q=not%20a%20reference    (a link that lands somewhere
//                                                                useful beats one that 404s)
`;

const defaultRows: ReadonlyArray<ComponentPropRow> = [
    {
        name: 'title', type: 'string', defaultValue: "'Bible references'",
        description: 'Panel heading.'
    },
    {
        name: 'suggestTitle', type: 'string', defaultValue: "'Suggest a bible reference'",
        description: 'Rendered uppercase above the suggestion box.'
    },
    {
        name: 'suggestDescription', type: 'string',
        defaultValue: "'Know a matching verse? …'",
        description: 'Prompt beneath the suggest heading.'
    },
    {
        name: 'addPlaceholderText', type: 'string', defaultValue: "'e.g. Romans 3:23…'",
        description: 'Placeholder inside the suggestion box.'
    },
    {
        name: 'chipCssClass', type: 'string', defaultValue: "'btn-primary-soft'",
        description: 'The blue chip look, theme-aware in light and dark mode.'
    },
    {
        name: 'approvedIconCssClass', type: 'string', defaultValue: "'bi-book'",
        description: 'Set as the APPROVED icon rather than a flat chip icon, so a reference still waiting shows the hourglass instead. One icon slot, filled by whichever status applies.'
    },
    {
        name: 'chipHrefFor', type: '(item) => string', defaultValue: 'bibleReferenceHref(value)',
        description: 'Addresses the passage itself, in USFM form.'
    },
    {
        name: 'normalizeAddedValue', type: '(raw) => string', defaultValue: 'trim',
        description: 'Trimmed only — unlike a tag, nothing is stripped from the front.'
    },
    {
        name: 'loginButtonText', type: 'string',
        defaultValue: "'Login to suggest a bible reference'",
        description: 'Shown to a signed-out reader in place of the box.'
    },
    {
        name: 'showAdd / showRemove / showModeration', type: 'boolean', defaultValue: 'true',
        description: 'On by default so the bare component matches the post-detail panel.'
    }
];

export const BibleReferenceAssociationPanelDoc = () => {
    useDocumentTitle('Bible Reference Association — Glory 2 Him');

    const { user } = useAuth();
    const viewerId = user?.userId ?? 'demo-viewer';

    const [references, setReferences] = useState<ReadonlyArray<AssociationItem>>([
        {
            id: '1', value: 'Joshua 10:8, 12-13',
            createdBy: 'another-user', approvalStatus: ApprovalStatus.Approved
        },
        {
            id: '2', value: '2 Kings 20:9-11',
            createdBy: 'another-user', approvalStatus: ApprovalStatus.Approved
        },
        {
            id: '3', value: 'Romans 3:23',
            createdBy: viewerId, approvalStatus: ApprovalStatus.Submitted
        }
    ]);

    const withStatus = (item: AssociationItem, approvalStatus: ApprovalStatus) =>
        setReferences(references.map((existing) =>
            existing.id === item.id ? { ...existing, approvalStatus } : existing));

    return (
        <ComponentDoc
            name="Bible Reference Association Panel"
            filePath="src/components/associations/bibleReferenceAssociationPanel.tsx"
            summary={
                <>
                    <Link to="/SamplePages/Components/Association-Panel">Association Panel</Link>
                    {' '}dressed as the bible reference panel: blue chips carrying a book icon once
                    approved, each addressing the passage itself. Every base prop stays
                    overridable — the defaults only fill the gaps.
                </>
            }>

            <DocSection
                title="Live"
                lead="Wired to local state. The last reference is yours and still waiting, so it shows the hourglass in place of the book.">
                <LiveDemo>
                    <BibleReferenceAssociationPanel
                        associationCollection={references}
                        emptyText="No references yet."
                        onAdd={(value) => setReferences([...references, {
                            id: value,
                            value,
                            createdBy: viewerId,
                            approvalStatus: ApprovalStatus.Submitted
                        }])}
                        onRemove={(item) =>
                            setReferences(references.filter((existing) => existing.id !== item.id))}
                        onApprove={(item) => withStatus(item, ApprovalStatus.Approved)}
                        onReject={(item) => withStatus(item, ApprovalStatus.Rejected)} />
                </LiveDemo>
            </DocSection>

            <DocSection
                title="Defaults"
                lead={
                    <>
                        Everything else comes from{' '}
                        <Link to="/SamplePages/Components/Association-Panel">Association Panel</Link>,
                        whose full prop list and visibility gates apply unchanged.
                    </>
                }>
                <PropsTable rows={defaultRows} />
            </DocSection>

            <DocSection title="Minimal usage">
                <CodeSample code={minimalSample} />
            </DocSection>

            <DocSection title="How a chip addresses its passage">
                <CodeSample code={hrefSample} />
            </DocSection>
        </ComponentDoc>
    );
};
