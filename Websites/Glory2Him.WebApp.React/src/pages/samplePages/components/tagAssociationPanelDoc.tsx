import { useState } from 'react';
import { Link } from 'react-router-dom';
import { TagAssociationPanel } from '../../../components/associations/tagAssociationPanel';
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
import { TagAssociationPanel } from '../../components/associations/tagAssociationPanel';

// Everything the post-detail panel needs. Titles, prompts, the hash prefix, the search links
// and the hash-stripping normalizer all come from the component's own defaults.
<TagAssociationPanel
    associationCollection={tags}
    onAdd={suggestTagAsync}
    onRemove={removeTagAsync}
    onApprove={approveTagAsync}
    onReject={denyTagAsync} />
`;

const overrideSample = `
// Any base prop overrides a default without giving up the rest — the hash prefix, the search
// href and the normalizer all survive.
<TagAssociationPanel
    title="Topics"
    suggestDescription="Missing a topic? Tell us."
    chipCssClass="btn-info-soft"
    showModerationActions={false}
    associationCollection={topics} />
`;

const defaultRows: ReadonlyArray<ComponentPropRow> = [
    {
        name: 'title', type: 'string', defaultValue: "'Tags'",
        description: 'Panel heading.'
    },
    {
        name: 'suggestTitle', type: 'string', defaultValue: "'Suggest a tag'",
        description: 'Rendered uppercase above the suggestion box.'
    },
    {
        name: 'suggestDescription', type: 'string',
        defaultValue: "'Think a tag is missing? …'",
        description: 'Prompt beneath the suggest heading.'
    },
    {
        name: 'addPlaceholderText', type: 'string', defaultValue: "'Start typing a tag…'",
        description: 'Placeholder inside the suggestion box.'
    },
    {
        name: 'chipCssClass', type: 'string', defaultValue: "'btn-success-soft'",
        description: 'The green chip look, theme-aware in light and dark mode.'
    },
    {
        name: 'chipPrefixText', type: 'string', defaultValue: "'#'",
        description: 'The hash in front of every tag. Not part of the stored value.'
    },
    {
        name: 'chipHrefFor', type: '(item) => string', defaultValue: '/Search?q={value}',
        description: 'A clicked tag searches for it.'
    },
    {
        name: 'normalizeAddedValue', type: '(raw) => string', defaultValue: 'trim + strip #',
        description: 'A leading hash is how people write tags but is not part of the tag itself.'
    },
    {
        name: 'loginButtonText', type: 'string', defaultValue: "'Login to suggest a tag'",
        description: 'Shown to a signed-out reader in place of the box.'
    },
    {
        name: 'showAdd / showRemove / showModeration', type: 'boolean', defaultValue: 'true',
        description: 'On by default so the bare component matches the post-detail panel.'
    }
];

export const TagAssociationPanelDoc = () => {
    useDocumentTitle('Tag Association — Glory 2 Him');

    const { user } = useAuth();
    const viewerId = user?.userId ?? 'demo-viewer';

    const [tags, setTags] = useState<ReadonlyArray<AssociationItem>>([
        { id: '1', value: 'creation', createdBy: 'another-user', approvalStatus: ApprovalStatus.Approved },
        { id: '2', value: 'science', createdBy: 'another-user', approvalStatus: ApprovalStatus.Approved },
        { id: '3', value: 'faith', createdBy: 'another-user', approvalStatus: ApprovalStatus.Approved },
        { id: '4', value: 'miracles', createdBy: 'another-user', approvalStatus: ApprovalStatus.Approved },
        { id: '5', value: 'test', createdBy: viewerId, approvalStatus: ApprovalStatus.Submitted }
    ]);

    const withStatus = (item: AssociationItem, approvalStatus: ApprovalStatus) =>
        setTags(tags.map((existing) =>
            existing.id === item.id ? { ...existing, approvalStatus } : existing));

    return (
        <ComponentDoc
            name="Tag Association Panel"
            filePath="src/components/associations/tagAssociationPanel.tsx"
            summary={
                <>
                    <Link to="/SamplePages/Components/Association-Panel">Association Panel</Link>
                    {' '}dressed as the tag panel: green chips, a hash in front of each, and a
                    search for anything clicked. Every base prop stays overridable — the defaults
                    only fill the gaps.
                </>
            }>

            <DocSection
                title="Live"
                lead="Wired to local state. The last tag is yours and still waiting, so it carries the hourglass and a way to withdraw it.">
                <LiveDemo>
                    <TagAssociationPanel
                        associationCollection={tags}
                        emptyText="No tags yet."
                        onAdd={(value) => setTags([...tags, {
                            id: value,
                            value,
                            createdBy: viewerId,
                            approvalStatus: ApprovalStatus.Submitted
                        }])}
                        onRemove={(item) =>
                            setTags(tags.filter((existing) => existing.id !== item.id))}
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

            <DocSection title="Overriding a default">
                <CodeSample code={overrideSample} />
            </DocSection>
        </ComponentDoc>
    );
};
