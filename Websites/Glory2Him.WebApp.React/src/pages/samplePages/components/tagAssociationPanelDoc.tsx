import { useState } from 'react';
import { Link } from 'react-router-dom';
import { TagAssociationPanel } from '../../../components/associations/tagAssociationPanel';
import {
    ApprovalStatus,
    AssociationItem
} from '../../../models/components/associations/associationItem';
import { useDocumentTitle } from '../../useDocumentTitle';
import {
    CodeSample,
    ComponentDoc,
    ComponentPropRow,
    DemoControls,
    DocSection,
    LiveDemo,
    PropsTable
} from './shared/componentDoc';

import {
    DemoSecurityContext,
    demoOtherSubmitterId,
    demoViewerId,
    SecurityContextSection,
    securityContextOptions
} from './shared/securityContextDemo';

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
        name: 'moderationRoles', type: 'string',
        defaultValue: "'Reviewers, Publishers, Administrators, Tag-Reviewers, Tag-Publishers'",
        description: 'The global tier plus the Tag-scoped pair (§18.6).'
    },
    {
        name: 'showAdd / showRemove / showModeration', type: 'boolean', defaultValue: 'true',
        description: 'On by default so the bare component matches the post-detail panel.'
    }
];

export const TagAssociationPanelDoc = () => {
    useDocumentTitle('Tag Association — Glory 2 Him');

    // Playground state: initial values match the wrapper's own resting posture — showAdd on
    // from its default, moderation off from the base default — with each label printing the
    // default so the reader always knows where a switch rests.
    const [securityContext, setSecurityContext] = useState(securityContextOptions[0]);
    const [showAdd, setShowAdd] = useState(true);
    const [showModerationActions, setShowModerationActions] = useState(false);
    const [showBorder, setShowBorder] = useState(false);
    const [isLoading, setIsLoading] = useState(false);

    const [tags, setTags] = useState<ReadonlyArray<AssociationItem>>([
        { id: '1', value: 'creation', createdBy: demoOtherSubmitterId, approvalStatus: ApprovalStatus.Approved },
        { id: '2', value: 'science', createdBy: demoOtherSubmitterId, approvalStatus: ApprovalStatus.Approved },
        { id: '3', value: 'faith', createdBy: demoOtherSubmitterId, approvalStatus: ApprovalStatus.Approved },
        { id: '4', value: 'miracles', createdBy: demoOtherSubmitterId, approvalStatus: ApprovalStatus.Approved },
        { id: '5', value: 'grace', createdBy: demoOtherSubmitterId, approvalStatus: ApprovalStatus.Submitted },
        { id: '6', value: 'test', createdBy: demoViewerId, approvalStatus: ApprovalStatus.Submitted }
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
                lead="Wired to local state. The last tag is yours and still waiting, so it carries the hourglass and a way to withdraw it — and the controls step the same panel through every viewer and surface switch.">
                <SecurityContextSection
                    selected={securityContext}
                    onChange={setSecurityContext} />

                <DemoControls toggles={[
                    {
                        name: 'tag-add',
                        label: 'showAdd',
                        defaultValue: true,
                        value: showAdd,
                        onChange: setShowAdd
                    },
                    {
                        name: 'tag-moderation',
                        label: 'showModerationActions',
                        defaultValue: false,
                        value: showModerationActions,
                        onChange: setShowModerationActions
                    },
                    {
                        name: 'tag-border',
                        label: 'showBorder',
                        defaultValue: false,
                        value: showBorder,
                        onChange: setShowBorder
                    },
                    {
                        name: 'tag-loading',
                        label: 'isLoading',
                        defaultValue: false,
                        value: isLoading,
                        onChange: setIsLoading
                    }
                ]} />

                <LiveDemo>
                    <DemoSecurityContext option={securityContext}>
                        <TagAssociationPanel
                            associationCollection={tags}
                            emptyText="No tags yet."
                            showAdd={showAdd}
                            showModerationActions={showModerationActions}
                            showBorder={showBorder}
                            isLoading={isLoading}
                            onAdd={(value) => setTags([...tags, {
                                id: value,
                                value,
                                createdBy: demoViewerId,
                                approvalStatus: ApprovalStatus.Submitted
                            }])}
                            onRemove={(item) =>
                                setTags(tags.filter((existing) => existing.id !== item.id))}
                            onApprove={(item) => withStatus(item, ApprovalStatus.Approved)}
                            onReject={(item) => withStatus(item, ApprovalStatus.Rejected)} />
                    </DemoSecurityContext>
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
