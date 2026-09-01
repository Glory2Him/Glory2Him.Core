import { useState } from 'react';

import {
    ContentItemDefaultPanel
} from '../../../components/contentItems/contentItemDefaultPanel';

import { useDocumentTitle } from '../../useDocumentTitle';
import { demoReactionOptions, demoStoryItem } from './shared/contentItemDemoData';

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
├── ContentItemDefaultPanel       ◄ this page (the view template most types use)
└── ContentItem{ContentType}Panel   overrides DERIVE from this one, via contentSlot
`;

const templateProps: ReadonlyArray<ComponentPropRow> = [
    {
        name: 'contentItem',
        type: 'ContentItemSearchItem',
        description: 'The self-contained element: the item and its winning setting travel '
            + 'together. The template reads nothing beyond it.'
    },
    {
        name: 'contentSlot',
        type: 'ReactNode?',
        description: 'THE DERIVATION POINT. Absent, the default content block renders '
            + '(thumbnail, badge and title, the truncated content, read-more). An '
            + 'override — Quotes, '
            + 'Verses — renders THIS template with contentSlot replaced, so the meta row, '
            + 'the pills and the engagement row are written once and carried identically.'
    },
    {
        name: 'showsEditButton / showsModerateButton',
        type: 'boolean',
        description: 'Decided ONCE in ContentItemPanel — ownership for Edit, the moderation '
            + 'tier for Moderate — and handed over decided. A template only renders them; '
            + 'moderateButtonIconCss and moderateButtonLabel arrive resolved the same way.'
    },
    {
        name: 'areReactionCountsExpanded / isReactionPickerOpen',
        type: 'boolean',
        description: 'The two per-card render toggles, owned by the dispatching panel’s '
            + 'state and handed over decided — the assigned cluster’s compact⇄counts face, '
            + 'and whether the Like picker stands open.'
    },
    {
        name: 'showApprovalStatusRibbon',
        type: 'boolean',
        description: 'The status corner ribbon, rendered on the card ROOT so every derived '
            + 'template wears it identically.'
    },
    {
        name: 'showTagSection / showBibleReferenceSection / showReactionSection / '
            + 'showCommentsSection / showShareSection / showSaveSection',
        type: 'boolean',
        defaultValue: 'true',
        description: 'The section switches, ANDed with the setting on the element: the '
            + 'setting says what the type shows, the switch says what this surface has room '
            + 'for. A section renders only when both agree.'
    },
    {
        name: 'onTitleClick / onReadMoreClick / …',
        type: '(item) => void',
        description: 'The event hooks, unchanged across the family — a control renders only '
            + 'where somebody is listening: no onTitleClick and the title stands as plain '
            + 'heading text, no onReadMoreClick and no way-in renders (a detail surface '
            + 'already IS the way in).'
    }
];

export function ContentItemDefaultPanelDoc() {
    useDocumentTitle('Content Item Default Panel — Components — Glory 2 Him');

    const [showsEditButton, setShowsEditButton] = useState(true);
    const [showsModerateButton, setShowsModerateButton] = useState(false);
    const [areReactionCountsExpanded, setAreReactionCountsExpanded] = useState(false);
    const [isReactionPickerOpen, setIsReactionPickerOpen] = useState(false);
    const [showApprovalStatusRibbon, setShowApprovalStatusRibbon] = useState(false);
    const [showTagSection, setShowTagSection] = useState(true);
    const [showBibleReferenceSection, setShowBibleReferenceSection] = useState(true);
    const [showReactionSection, setShowReactionSection] = useState(true);
    const [showCommentsSection, setShowCommentsSection] = useState(true);
    const [showShareSection, setShowShareSection] = useState(true);
    const [showSaveSection, setShowSaveSection] = useState(true);
    const [lastEvent, setLastEvent] = useState('');

    return (
        <ComponentDoc
            name="Content Item Default Panel"
            filePath="src/components/contentItems/contentItemDefaultPanel.tsx"
            summary="The view template most content types render through: type badge, content
                block, meta row, tag and reference pills, and the engagement row. The per-type
                overrides derive from it by replacing contentSlot alone.">

            <DocSection
                title="Where it stands in the family"
                lead={
                    <>
                        A template renders a FULLY DECIDED bundle: ownership, the moderation
                        tier, the reaction gating and the per-card toggles are all
                        <code> ContentItemPanel</code>&rsquo;s decisions, handed over made.
                        That is why every switch below exists here as a plain boolean prop —
                        this page plays the dispatcher.
                    </>
                }>
                <CodeSample code={familySample} caption="One tree, every face" />
            </DocSection>

            <DocSection title="Props">
                <PropsTable rows={templateProps} />
            </DocSection>

            <DocSection
                title="Live"
                lead={
                    <>
                        Every switch is one of this template&rsquo;s own props — the decided
                        bundle, driven by hand. Last event:{' '}
                        <code>{lastEvent.length > 0 ? lastEvent : '(none yet)'}</code>
                    </>
                }>
                <DemoControls toggles={[
                    {
                        name: 'shows-edit',
                        label: 'showsEditButton (decided: the viewer owns it)',
                        value: showsEditButton,
                        onChange: setShowsEditButton
                    },
                    {
                        name: 'shows-moderate',
                        label: 'showsModerateButton (decided: the viewer moderates)',
                        value: showsModerateButton,
                        onChange: setShowsModerateButton
                    },
                    {
                        name: 'counts-expanded',
                        label: 'areReactionCountsExpanded (cluster face)',
                        value: areReactionCountsExpanded,
                        onChange: setAreReactionCountsExpanded
                    },
                    {
                        name: 'picker-open',
                        label: 'isReactionPickerOpen (Like choices open)',
                        value: isReactionPickerOpen,
                        onChange: setIsReactionPickerOpen
                    },
                    {
                        name: 'ribbons',
                        label: 'showApprovalStatusRibbon (status corner ribbon)',
                        value: showApprovalStatusRibbon,
                        onChange: setShowApprovalStatusRibbon
                    },
                    {
                        name: 'tag-section',
                        label: 'showTagSection',
                        value: showTagSection,
                        onChange: setShowTagSection
                    },
                    {
                        name: 'bible-reference-section',
                        label: 'showBibleReferenceSection',
                        value: showBibleReferenceSection,
                        onChange: setShowBibleReferenceSection
                    },
                    {
                        name: 'reaction-section',
                        label: 'showReactionSection',
                        value: showReactionSection,
                        onChange: setShowReactionSection
                    },
                    {
                        name: 'comments-section',
                        label: 'showCommentsSection',
                        value: showCommentsSection,
                        onChange: setShowCommentsSection
                    },
                    {
                        name: 'share-section',
                        label: 'showShareSection',
                        value: showShareSection,
                        onChange: setShowShareSection
                    },
                    {
                        name: 'save-section',
                        label: 'showSaveSection',
                        value: showSaveSection,
                        onChange: setShowSaveSection
                    }
                ]} />

                <LiveDemo>
                    <ContentItemDefaultPanel
                        contentItem={demoStoryItem}
                        contentItemSetting={demoStoryItem.contentItemSetting}
                        contentTypeName="Story"
                        offeredReactions={showReactionSection ? demoReactionOptions : []}
                        showsEditButton={showsEditButton}
                        showsModerateButton={showsModerateButton}
                        moderateButtonIconCss="bi bi-shield"
                        moderateButtonLabel="Moderate"
                        showApprovalStatusRibbon={showApprovalStatusRibbon}
                        showTagSection={showTagSection}
                        showBibleReferenceSection={showBibleReferenceSection}
                        showReactionSection={showReactionSection}
                        showCommentsSection={showCommentsSection}
                        showShareSection={showShareSection}
                        showSaveSection={showSaveSection}
                        truncateAt={120}
                        allowInPlaceExpansion={false}
                        isContentExpanded={false}
                        areReactionCountsExpanded={areReactionCountsExpanded}
                        onAssignedReactionsClick={() =>
                            setAreReactionCountsExpanded(!areReactionCountsExpanded)}
                        isReactionPickerOpen={isReactionPickerOpen}
                        onReactionClick={() =>
                            setIsReactionPickerOpen(!isReactionPickerOpen)}
                        onReactionSelected={(_item, reaction) => {
                            setIsReactionPickerOpen(false);
                            setLastEvent(`onReactionSelected(${reaction.label})`);
                        }}
                        onTitleClick={(item) => setLastEvent(`onTitleClick(${item.id})`)}
                        onReadMoreClick={(item) => setLastEvent(`onReadMoreClick(${item.id})`)}
                        onCommentsClick={(item) => setLastEvent(`onCommentsClick(${item.id})`)}
                        onShareClick={(item) => setLastEvent(`onShareClick(${item.id})`)}
                        onSaveClick={(item) => setLastEvent(`onSaveClick(${item.id})`)}
                        onEditClick={(item) => setLastEvent(`onEditClick(${item.id})`)}
                        onModerateClick={(item) => setLastEvent(`onModerateClick(${item.id})`)}
                        onTagClick={(_item, tag) => setLastEvent(`onTagClick(${tag})`)}
                        onBibleReferenceClick={(_item, reference) =>
                            setLastEvent(`onBibleReferenceClick(${reference})`)} />
                </LiveDemo>
            </DocSection>
        </ComponentDoc>
    );
}
