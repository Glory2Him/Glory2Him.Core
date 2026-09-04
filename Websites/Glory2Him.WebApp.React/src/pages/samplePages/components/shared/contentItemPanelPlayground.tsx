import { useState } from 'react';
import { ContentItemPanel } from '../../../../components/contentItems/contentItemPanel';

import {
    ApprovalStatus,
    ContentItemSearchItem
} from '../../../../models/components/contentItems/contentItemSearchItem';

import {
    ContentItemFormItem
} from '../../../../models/components/contentItems/contentItemFormItem';

import {
    DemoControls,
    DemoNumberInput,
    DemoRadioGroup,
    LiveDemo
} from './componentDoc';

import {
    DemoSecurityContext,
    demoSubmitterIdFor,
    SecurityContextSection,
    securityContextOptions
} from './securityContextDemo';

// ONE view-face playground for every page that demonstrates a card: the Content Item Panel
// page and the three view-template pages all render THIS, so the boards cannot drift — the
// same security context, the same ribbon statuses, the same switches, the same live element
// swap on save. The templates' own reference pages demo through the DISPATCHER on purpose:
// the gates, the edit transition and the status pair are ContentItemPanel's decisions, and a
// template shown outside them answers questions nobody asks on a page.

// The four statuses the ribbon and the pill can wear — Dismissed has neither by design.
const ribbonStatusOptions = [
    { key: String(ApprovalStatus.Draft), label: 'Draft (grey)' },
    { key: String(ApprovalStatus.Submitted), label: 'Submitted (yellow)' },
    { key: String(ApprovalStatus.Approved), label: 'Approved (green)' },
    { key: String(ApprovalStatus.Rejected), label: 'Rejected (red)' }
] as const;

export interface ContentItemPanelPlaygroundProps {
    contentItem: ContentItemSearchItem;
}

export function ContentItemPanelPlayground({
    contentItem
}: ContentItemPanelPlaygroundProps) {
    const [lastEvent, setLastEvent] = useState('');

    // The component's own default — type a smaller value to watch the cut move.
    const [truncateAt, setTruncateAt] = useState(400);

    const [securityContext, setSecurityContext] = useState(securityContextOptions[0]);

    const [ribbonStatus, setRibbonStatus] =
        useState<ApprovalStatus>(contentItem.approvalStatus ?? ApprovalStatus.Draft);

    const [showApprovalStatusRibbon, setShowApprovalStatusRibbon] = useState(true);
    const [showApprovalStatus, setShowApprovalStatus] = useState(true);
    const [showContentExpanded, setShowContentExpanded] = useState(false);
    const [allowInPlaceExpansion, setAllowInPlaceExpansion] = useState(true);
    const [showEditSection, setShowEditSection] = useState(true);
    const [showModerationSection, setShowModerationSection] = useState(false);
    const [allowTitleClick, setAllowTitleClick] = useState(false);
    const [showTagSection, setShowTagSection] = useState(true);
    const [showBibleReferenceSection, setShowBibleReferenceSection] = useState(true);
    const [showReactionSection, setShowReactionSection] = useState(true);
    const [showCommentsSection, setShowCommentsSection] = useState(true);
    const [showShareSection, setShowShareSection] = useState(true);
    const [showSaveSection, setShowSaveSection] = useState(true);

    // THE ONE-ELEMENT SWAP, demonstrated: a save closes the editor and the card shows the
    // amendments because this page swapped its element — exactly what a real page does after
    // its PUT.
    const [amendedItem, setAmendedItem] = useState<ContentItemFormItem | null>(null);

    const viewedItem: ContentItemSearchItem = {
        ...contentItem,
        submittedById: demoSubmitterIdFor(securityContext),
        approvalStatus: ribbonStatus,

        ...(amendedItem == null
            ? {}
            : {
                title: amendedItem.title,
                author: amendedItem.author,
                content: amendedItem.content,
                shareabilityBasis: amendedItem.shareabilityBasis,
                sharePermission: amendedItem.sharePermission
            })
    };

    return (
        <>
            <SecurityContextSection
                selected={securityContext}
                onChange={setSecurityContext} />

            <DemoRadioGroup
                title="Ribbon status"
                name={`demo-ribbon-status-${contentItem.id}`}
                options={ribbonStatusOptions}
                selectedKey={String(ribbonStatus)}
                onChange={(key) => setRibbonStatus(Number(key) as ApprovalStatus)} />

            <DemoNumberInput
                label="truncateAt"
                name={`truncate-at-${contentItem.id}`}
                value={truncateAt}
                defaultValue={400}
                onChange={setTruncateAt} />

            <DemoControls toggles={[
                {
                    name: 'panel-ribbons',
                    label: 'showApprovalStatusRibbon',
                    defaultValue: false,
                    value: showApprovalStatusRibbon,
                    onChange: setShowApprovalStatusRibbon
                },
                {
                    name: 'panel-status-pill',
                    label: 'showApprovalStatus',
                    defaultValue: false,
                    value: showApprovalStatus,
                    onChange: setShowApprovalStatus
                },
                {
                    name: 'panel-content-expanded',
                    label: 'showContentExpanded',
                    defaultValue: false,
                    value: showContentExpanded,
                    onChange: setShowContentExpanded
                },
                {
                    name: 'panel-in-place-expansion',
                    label: 'allowInPlaceExpansion (read-more toggles here)',
                    defaultValue: false,
                    value: allowInPlaceExpansion,
                    onChange: setAllowInPlaceExpansion
                },
                {
                    name: 'panel-editing',
                    label: 'showEditSection',
                    defaultValue: false,
                    value: showEditSection,
                    onChange: setShowEditSection
                },
                {
                    name: 'panel-moderated',
                    label: 'showModerationSection',
                    defaultValue: false,
                    value: showModerationSection,
                    onChange: setShowModerationSection
                },
                {
                    name: 'panel-title-click',
                    label: 'allowTitleClick (the title is a way in)',
                    defaultValue: false,
                    value: allowTitleClick,
                    onChange: setAllowTitleClick
                },
                {
                    name: 'panel-tags',
                    label: 'showTagSection',
                    defaultValue: true,
                    value: showTagSection,
                    onChange: setShowTagSection
                },
                {
                    name: 'panel-bible-references',
                    label: 'showBibleReferenceSection',
                    defaultValue: true,
                    value: showBibleReferenceSection,
                    onChange: setShowBibleReferenceSection
                },
                {
                    name: 'panel-reactions',
                    label: 'showReactionSection',
                    defaultValue: true,
                    value: showReactionSection,
                    onChange: setShowReactionSection
                },
                {
                    name: 'panel-comments',
                    label: 'showCommentsSection',
                    defaultValue: true,
                    value: showCommentsSection,
                    onChange: setShowCommentsSection
                },
                {
                    name: 'panel-share',
                    label: 'showShareSection',
                    defaultValue: true,
                    value: showShareSection,
                    onChange: setShowShareSection
                },
                {
                    name: 'panel-save',
                    label: 'showSaveSection',
                    defaultValue: true,
                    value: showSaveSection,
                    onChange: setShowSaveSection
                }
            ]} />

            <p className="small text-body-secondary">
                Last event:{' '}
                <code>{lastEvent.length > 0 ? lastEvent : '(none yet)'}</code>
            </p>

            <LiveDemo title="Live — view">
                <DemoSecurityContext option={securityContext}>
                    <ContentItemPanel
                        contentItem={viewedItem}
                        showApprovalStatusRibbon={showApprovalStatusRibbon}
                        showApprovalStatus={showApprovalStatus}
                        showContentExpanded={showContentExpanded}
                        truncateAt={truncateAt}
                        allowInPlaceExpansion={allowInPlaceExpansion}
                        showEditSection={showEditSection}
                        showModerationSection={showModerationSection}
                        allowTitleClick={allowTitleClick}
                        onTitleClick={(item) => setLastEvent(`onTitleClick(${item.id})`)}
                        showTagSection={showTagSection}
                        showBibleReferenceSection={showBibleReferenceSection}
                        showReactionSection={showReactionSection}
                        showCommentsSection={showCommentsSection}
                        showShareSection={showShareSection}
                        showSaveSection={showSaveSection}
                        onReadMore={(item) => setLastEvent(`onReadMore(${item.id})`)}
                        onExpandCollapse={(item) =>
                            setLastEvent(`onExpandCollapse(${item.id})`)}
                        onCommentsClick={(item) =>
                            setLastEvent(`onCommentsClick(${item.id})`)}
                        onShareClick={(item) => setLastEvent(`onShareClick(${item.id})`)}
                        onSaveClick={(item) => setLastEvent(`onSaveClick(${item.id})`)}
                        onTagClick={(_item, tag) => setLastEvent(`onTagClick(${tag})`)}
                        onBibleReferenceClick={(_item, reference) =>
                            setLastEvent(`onBibleReferenceClick(${reference})`)}
                        onModified={(item) => {
                            setAmendedItem(item);
                            setLastEvent(`onModified(${item.id})`);
                        }}
                        onRemoved={(item) => setLastEvent(`onRemoved(${item.id})`)}
                        onModerateClick={(item) =>
                            setLastEvent(`onModerateClick(${item.id})`)} />
                </DemoSecurityContext>
            </LiveDemo>
        </>
    );
}
