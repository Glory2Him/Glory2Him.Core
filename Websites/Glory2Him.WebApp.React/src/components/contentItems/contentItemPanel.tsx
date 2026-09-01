import { ComponentType, useEffect, useState } from 'react';
import { useAuth } from '../securitys/authProvider';
import { ContentItemAddPanel } from './contentItemAddPanel';
import { ContentItemDefaultPanel } from './contentItemDefaultPanel';
import { ContentItemEditPanel } from './contentItemEditPanel';
import { ContentItemQuotesPanel } from './contentItemQuotesPanel';
import { ContentItemVersesPanel } from './contentItemVersesPanel';
import { ContentItemSetting } from '../../models/foundations/contentItemSettings/contentItemSetting';

import {
    ContentType,
    contentTypeLabels
} from '../../models/foundations/contentItemSettings/contentType';

import {
    ContentItemEvents,
    ContentItemSectionToggles,
    ContentItemTemplateProps,
    ContentItemText
} from '../../models/components/contentItems/contentItemTemplate';

import {
    ContentItemFormItem,
    ContentItemPanelMode,
    ContentItemValidationIssues,
    defaultShareabilityBasis
} from '../../models/components/contentItems/contentItemFormItem';

import {
    ContentItemReactionOption,
    ContentItemSearchItem
} from '../../models/components/contentItems/contentItemSearchItem';

import './contentItems.css';

// THE BLUE BLOCK: one content item, on whichever face the moment asks for. This panel owns
// everything that is the same for every face — the effective-setting reads, the per-card UI
// state, the reaction gating, the ownership and role gates — and DISPATCHES:
//
//   no item                          → ContentItemAddPanel   (the picker and a blank form)
//   the editor, taken or asked for   → ContentItemEditPanel  (the frozen type, the seeded form)
//   otherwise                        → a VIEW template by content type:
//                                      ContentItemDefaultPanel, or a registered override
//                                      (ContentItemQuotesPanel, ContentItemVersesPanel)
//
// One family, one tree: ContentItemListPanel renders this same panel for every result, and a
// details page renders it for its one item — there is no second component to keep in sync.
//
// A pure presentation component, like everything in this family: props in, events out, no
// fetching, no mutation. The CONSUMER owns persistence — onAdded/onModified/onRemoved say what
// the reader decided, and whether that is a POST, a PUT or a fork is the page's business.
// The six section switches ride along too (ContentItemSectionToggles): all default true, so
// the projection's setting decides unless this surface overrides — see the model for why.
export interface ContentItemPanelProps
    extends ContentItemEvents, ContentItemText, ContentItemSectionToggles {
    // SELF-CONTAINED: the element carries the item AND its winning setting, resolved by the
    // projection (§6.4). The view faces consult no collection — every gate reads the one row
    // that governs this item, so a mixed page is safe by construction and updating one item
    // is one element swapped by the consumer.
    //
    // ABSENT, THE PANEL IS THE ADD SURFACE — hand over contentItemSettingCollection and the
    // picker offers its contributable types. ContentItemListPanel always has an item, so a
    // card in a list can never fall into `add`.
    contentItem?: ContentItemSearchItem;

    // THE ADD-MODE SIGNAL, and the editor's tile/fallback rows: the content type DEFAULTS the
    // consumer holds. Populated with no contentItem, the panel renders the add face from these
    // rows (ContentItemListPanel never populates this prop). With an item it is simply the
    // fallback behind the element's own embedded winner.
    contentItemSettingCollection?: ReadonlyArray<ContentItemSetting>;

    // Lands the panel straight on a surface — 'edit' opens the editor without the reader
    // taking Edit first (still subject to showEditSection). Absent, the item decides: no
    // item is 'add', an item reads until Edit is taken.
    mode?: ContentItemPanelMode;

    // The reaction choices behind the Like control — pulled by the page from GET api/Reactions
    // (approved rows only) and handed over. Empty means no card offers one, whatever the
    // settings say: a surface that cannot persist a reaction must not appear to accept one.
    reactionOptions?: ReadonlyArray<ContentItemReactionOption>;

    // Whether this card sits on a MODERATED surface (the admin queue). Off — the default —
    // the card offers Edit to its submitter and Moderate (shield) to the moderation tier,
    // side by side. On, only Moderate renders, wearing Edit's pencil and label: on a surface
    // that IS moderation, the moderation action is simply what editing means.
    showModerationSection?: boolean;

    // Whether the card wears a corner ribbon naming its approval status — coloured by the
    // stylesheet off data-approval-status. Off by default: the public feed already says
    // approved by existing.
    showApprovalStatusRibbon?: boolean;

    // The ribbon's sibling: whether the card wears its approval-status PILL beside the
    // type chip. Off by default; on, every status shows, Approved included.
    showApprovalStatus?: boolean;

    // ── The content length ─────────────────────────────────────────────────
    // Off (the default), the content is cut at truncateAt characters with an ellipsis and
    // the read-more affordance; on, the full content stands — what a detail surface asks.
    showContentExpanded?: boolean;

    // The character position the cut happens at. Only content actually longer than this
    // is cut — a short devotional never wears an ellipsis.
    truncateAt?: number;

    // WHICH LINK BUTTON the card carries — the two are never shared. Off (the default),
    // readMoreLinkButton renders while the content is cut and raises onReadMore, the
    // page's route to the detail surface with its back context. On,
    // expandCollapseLinkButton renders instead, raising onExpandCollapse — this panel
    // toggles the expansion in place, and the expanded card offers show-less.
    allowInPlaceExpansion?: boolean;

    // THE SURFACE SWITCH for the edit face, ahead of every role check — off by default, so a
    // list card whose page wired onModified for element swaps can still never become an
    // editor by accident. On, the owner's Edit affordance opens ContentItemEditPanel IN PLACE
    // when the page listens on onModified/onRemoved; pages that route to a separate edit
    // surface keep wiring onEditClick instead.
    showEditSection?: boolean;

    // ── The form faces (add and edit) ─────────────────────────────────────────
    isLoading?: boolean;

    // Freezes the form buttons while the consumer is persisting, so one click is one write.
    isSubmitting?: boolean;

    // What the API said was wrong, keyed by ITS parameter names — see ContentItemFormPanel,
    // which owns how they land on fields.
    validationIssues?: ContentItemValidationIssues;

    // Whose name an owned basis prefills into the Author field — the resolved submitter where
    // the consumer has one, the signed-in reader otherwise.
    submittedByDisplayName?: string;

    ariaLabel?: string;
    titleText?: string;
    showBorder?: boolean;

    onAdded?: (item: ContentItemFormItem) => void;
    onModified?: (item: ContentItemFormItem) => void;
    onRemoved?: (item: ContentItemFormItem) => void;
    onCancelled?: () => void;
}

// THE TEMPLATE REGISTRY. An override renders when one is registered for the item's type; the
// default renders otherwise. Adding one is exactly this one line — Verses arrived that way
// when ContentType.Verses landed, seeds and all.
const templateOverrides:
    Partial<Record<ContentType, ComponentType<ContentItemTemplateProps>>> = {
    [ContentType.Quote]: ContentItemQuotesPanel,
    [ContentType.Verses]: ContentItemVersesPanel
};

// Element → editor seed. The search element and the form item are the same facts in two
// registers — the one rename is submittedById back to createdBy, the audit name the form's
// [OWNER] gate decides on.
const toFormItem = (contentItem: ContentItemSearchItem): ContentItemFormItem => ({
    id: contentItem.id,
    contentType: contentItem.contentType,
    contentItemSetting: contentItem.contentItemSetting,
    title: contentItem.title ?? '',
    author: contentItem.author ?? '',
    content: contentItem.content,
    shareabilityBasis: contentItem.shareabilityBasis ?? defaultShareabilityBasis,
    sharePermission: contentItem.sharePermission ?? '',
    createdBy: contentItem.submittedById,
    approvalStatus: contentItem.approvalStatus
});

export function ContentItemPanel({
    contentItem,
    contentItemSettingCollection = [],
    mode,
    reactionOptions = [],
    showModerationSection = false,
    showApprovalStatusRibbon = false,
    showApprovalStatus = false,
    showEditSection = false,
    showContentExpanded = false,
    truncateAt = 400,
    allowInPlaceExpansion = false,
    isLoading = false,
    isSubmitting = false,
    validationIssues,
    submittedByDisplayName,
    ariaLabel,
    titleText,
    showBorder,
    onAdded,
    onModified,
    onRemoved,
    onCancelled,
    showReactionSection = true,
    onReactionSelected,
    onEditClick,
    onModerateClick,
    onExpandCollapse,
    ...eventsAndText
}: ContentItemPanelProps) {
    const { isAuthenticated, user, userRoles } = useAuth();

    // The per-card render toggles, and whether the reader has taken Edit in place. Local state
    // is right even in a presentation component: which face is showing is nothing the consumer
    // persists.
    const [areReactionCountsExpanded, setAreReactionCountsExpanded] = useState(false);
    const [isReactionPickerOpen, setIsReactionPickerOpen] = useState(false);
    const [isEditorTaken, setIsEditorTaken] = useState(false);

    // The in-place expansion, seeded from the surface's own answer — consulted only while
    // allowInPlaceExpansion is on; otherwise showContentExpanded decides alone.
    const [isContentToggledOpen, setIsContentToggledOpen] = useState(showContentExpanded);

    // A different item is a different surface, and a changed mode prop overrules an Edit the
    // reader took earlier — the same identity-keyed reset the form engine keeps for its draft.
    const contentItemId = contentItem?.id;

    useEffect(() => {
        setIsEditorTaken(false);
    }, [contentItemId, mode]);

    useEffect(() => {
        setIsContentToggledOpen(showContentExpanded);
    }, [contentItemId, showContentExpanded]);

    // ── The add face ──────────────────────────────────────────────────────────
    // No item: the panel IS the contribution form, shaped from the settings collection. Every
    // hook above has already run, so the early return is safe.
    if (contentItem == null) {
        return (
            <ContentItemAddPanel
                contentItemSettingCollection={contentItemSettingCollection}
                isLoading={isLoading}
                isSubmitting={isSubmitting}
                validationIssues={validationIssues}
                submittedByDisplayName={submittedByDisplayName}
                ariaLabel={ariaLabel}
                titleText={titleText}
                showBorder={showBorder}
                onAdded={onAdded}
                onCancelled={onCancelled} />
        );
    }

    // The winner rode in on the element. The name falls back to the fixed enum label, which
    // exists for every member and so is never empty — the same rule contentTypeNameOf keeps.
    const contentItemSetting = contentItem.contentItemSetting;

    const contentTypeName =
        contentItemSetting?.contentTypeName
        ?? contentTypeLabels[contentItem.contentType]
        ?? '';

    // What this card may OFFER, decided against its own effective row. Both halves of the §6.5
    // pair are asked — ReactionsAllowed says the type accepts them, ShowReactions says this
    // surface renders them — plus the panel's own condition that somebody is listening, because
    // a control whose event goes nowhere is worse than no control.
    const offeredReactions = (() => {
        if (showReactionSection === false
            || onReactionSelected == null
            || reactionOptions.length === 0
            || contentItemSetting?.reactionsAllowed === false
            || contentItemSetting?.showReactions === false) {
            return [] as ReadonlyArray<ContentItemReactionOption>;
        }

        return contentItemSetting?.limitReactionsToLoveOnly === true
            ? reactionOptions.filter((reaction) => reaction.isLove === true)
            : reactionOptions;
    })();

    // WHO SUBMITTED IT is an account-id comparison, exactly the [OWNER] rule the form engine
    // decides on — never a display name, which two accounts can share.
    const viewerOwnsItem =
        isAuthenticated
        && (contentItem.submittedById ?? '').length > 0
        && contentItem.submittedById === user?.userId;

    // The moderation tier, at every §18.6 scope the item's type composes — and the ReadOnly
    // veto asked FIRST, at its three scopes, because a sanction outranks every grant (#366).
    // RENDER decisions only: the server re-decides both actions against the stored row.
    const contentTypeSegment = ContentType[contentItem.contentType] ?? '';

    const holdsAnyRole = (roles: ReadonlyArray<string>): boolean =>
        roles.some((role) => userRoles.includes(role));

    const isBlocked = holdsAnyRole([
        'ReadOnly',
        'ContentItem-ReadOnly',
        `ContentItem-${contentTypeSegment}-ReadOnly`
    ]);

    const viewerModerates =
        isAuthenticated
        && isBlocked === false
        && holdsAnyRole([
            'Administrators',
            'Reviewers',
            'Publishers',
            'ContentItem-Reviewers',
            'ContentItem-Publishers',
            `ContentItem-${contentTypeSegment}-Reviewers`,
            `ContentItem-${contentTypeSegment}-Publishers`
        ]);

    // ── The edit face ─────────────────────────────────────────────────────────
    // WHERE EDIT GOES is the page's wiring: a page listening on onModified/onRemoved gets the
    // editor IN PLACE (this face); a page that wired onEditClick alone is routing to its own
    // edit surface and gets the event, exactly as before the merge.
    const opensEditorInPlace =
        showEditSection && (onModified != null || onRemoved != null);

    if ((mode === 'edit' || isEditorTaken) && opensEditorInPlace) {
        return (
            <ContentItemEditPanel
                contentItem={toFormItem(contentItem)}
                contentItemSettingCollection={contentItemSettingCollection}
                showEditSection={showEditSection}
                isLoading={isLoading}
                isSubmitting={isSubmitting}
                validationIssues={validationIssues}
                submittedByDisplayName={submittedByDisplayName}
                showApprovalStatusRibbon={showApprovalStatusRibbon}
                ariaLabel={ariaLabel}
                titleText={titleText}
                showBorder={showBorder}
                onModified={(item) => {
                    // A committed save CLOSES the editor the way Cancel does — back to the
                    // view face. What the card then shows is the CONSUMER's element: the
                    // page persists and swaps it (the one-element swap), so the amendments
                    // appear; a page that has not swapped yet honestly shows the stored
                    // row.
                    setIsEditorTaken(false);
                    onModified?.(item);
                }}
                onRemoved={onRemoved}
                onCancelled={() => {
                    setIsEditorTaken(false);
                    onCancelled?.();
                }} />
        );
    }

    // ── The view face ─────────────────────────────────────────────────────────
    // The showModerationSection matrix: an ordinary surface offers both, each to its own people; a
    // moderated surface offers Moderate alone, wearing Edit's pencil and label — on a surface
    // that IS moderation, the moderation action is simply what editing means there.
    const showsEditButton =
        showModerationSection === false
        && viewerOwnsItem
        && isBlocked === false
        && (opensEditorInPlace || onEditClick != null);

    const showsModerateButton = viewerModerates && onModerateClick != null;

    const Template =
        templateOverrides[contentItem.contentType] ?? ContentItemDefaultPanel;

    return (
        <Template
            contentItem={contentItem}
            contentItemSetting={contentItemSetting}
            contentTypeName={contentTypeName}
            offeredReactions={offeredReactions}
            showsEditButton={showsEditButton}
            showsModerateButton={showsModerateButton}
            moderateButtonIconCss={showModerationSection ? 'bi bi-pencil' : 'bi bi-shield'}
            moderateButtonLabel={showModerationSection ? 'Edit' : 'Moderate'}
            showApprovalStatusRibbon={showApprovalStatusRibbon}
            showApprovalStatus={showApprovalStatus}
            truncateAt={truncateAt}
            allowInPlaceExpansion={allowInPlaceExpansion}
            isContentExpanded={allowInPlaceExpansion
                ? isContentToggledOpen
                : showContentExpanded}
            onExpandCollapse={(item) => {
                setIsContentToggledOpen(!isContentToggledOpen);
                onExpandCollapse?.(item);
            }}
            showReactionSection={showReactionSection}
            onEditClick={(item) =>
                opensEditorInPlace ? setIsEditorTaken(true) : onEditClick?.(item)}
            onModerateClick={onModerateClick}
            areReactionCountsExpanded={areReactionCountsExpanded}
            onAssignedReactionsClick={
                () => setAreReactionCountsExpanded(!areReactionCountsExpanded)}
            isReactionPickerOpen={isReactionPickerOpen}
            onReactionClick={() => setIsReactionPickerOpen(!isReactionPickerOpen)}
            onReactionSelected={(item, reaction) => {
                setIsReactionPickerOpen(false);
                onReactionSelected?.(item, reaction);
            }}
            {...eventsAndText} />
    );
}
