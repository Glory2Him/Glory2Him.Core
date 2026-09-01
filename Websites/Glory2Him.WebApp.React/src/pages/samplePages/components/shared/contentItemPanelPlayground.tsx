import { useState } from 'react';
import { ContentItemPanel } from '../../../../components/contentItems/contentItemPanel';

import {
    ApprovalStatus,
    ContentItemSearchItem
} from '../../../../models/components/contentItems/contentItemSearchItem';

import {
    ContentItemFormItem
} from '../../../../models/components/contentItems/contentItemFormItem';

import { DemoControls, DemoRadioGroup, LiveDemo } from './componentDoc';

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

    // Low enough that the demo items actually cut, so the read-more affordances show.
    truncateAt?: number;
}

export function ContentItemPanelPlayground({
    contentItem,
    truncateAt = 160
}: ContentItemPanelPlaygroundProps) {
    const [lastEvent, setLastEvent] = useState('');

    const [securityContext, setSecurityContext] = useState(securityContextOptions[0]);

    const [ribbonStatus, setRibbonStatus] =
        useState<ApprovalStatus>(contentItem.approvalStatus ?? ApprovalStatus.Draft);

    const [showApprovalStatusRibbon, setShowApprovalStatusRibbon] = useState(true);
    const [showApprovalStatus, setShowApprovalStatus] = useState(true);
    const [showContentExpanded, setShowContentExpanded] = useState(false);
    const [allowInPlaceExpansion, setAllowInPlaceExpansion] = useState(true);
    const [isEditingAllowed, setIsEditingAllowed] = useState(true);
    const [isModeratedView, setIsModeratedView] = useState(false);
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

            <DemoControls toggles={[
                {
                    name: 'panel-ribbons',
                    label: 'showApprovalStatusRibbon',
                    value: showApprovalStatusRibbon,
                    onChange: setShowApprovalStatusRibbon
                },
                {
                    name: 'panel-status-pill',
                    label: 'showApprovalStatus',
                    value: showApprovalStatus,
                    onChange: setShowApprovalStatus
                },
                {
                    name: 'panel-content-expanded',
                    label: 'showContentExpanded',
                    value: showContentExpanded,
                    onChange: setShowContentExpanded
                },
                {
                    name: 'panel-in-place-expansion',
                    label: 'allowInPlaceExpansion (read-more toggles here)',
                    value: allowInPlaceExpansion,
                    onChange: setAllowInPlaceExpansion
                },
                {
                    name: 'panel-editing',
                    label: 'isEditingAllowed',
                    value: isEditingAllowed,
                    onChange: setIsEditingAllowed
                },
                {
                    name: 'panel-moderated',
                    label: 'isModeratedView',
                    value: isModeratedView,
                    onChange: setIsModeratedView
                },
                {
                    name: 'panel-tags',
                    label: 'showTagSection',
                    value: showTagSection,
                    onChange: setShowTagSection
                },
                {
                    name: 'panel-bible-references',
                    label: 'showBibleReferenceSection',
                    value: showBibleReferenceSection,
                    onChange: setShowBibleReferenceSection
                },
                {
                    name: 'panel-reactions',
                    label: 'showReactionSection',
                    value: showReactionSection,
                    onChange: setShowReactionSection
                },
                {
                    name: 'panel-comments',
                    label: 'showCommentsSection',
                    value: showCommentsSection,
                    onChange: setShowCommentsSection
                },
                {
                    name: 'panel-share',
                    label: 'showShareSection',
                    value: showShareSection,
                    onChange: setShowShareSection
                },
                {
                    name: 'panel-save',
                    label: 'showSaveSection',
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
                        isEditingAllowed={isEditingAllowed}
                        isModeratedView={isModeratedView}
                        showTagSection={showTagSection}
                        showBibleReferenceSection={showBibleReferenceSection}
                        showReactionSection={showReactionSection}
                        showCommentsSection={showCommentsSection}
                        showShareSection={showShareSection}
                        showSaveSection={showSaveSection}
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
