import { ReactNode, useEffect, useId, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { ConfirmDialog } from '../coreUI/confirmDialog';
import { useAuth } from '../securitys/authProvider';
import { ContentItemSetting } from '../../models/foundations/contentItemSettings/contentItemSetting';
import { ContentType } from '../../models/foundations/contentItemSettings/contentType';

import {
    contentTypeNameOf,
    resolveContentItemSetting
} from '../../services/views/contentItems/resolveContentItemSetting';

import {
    ApprovalStatus,
    ContentItemFormItem,
    ContentItemPanelMode,
    ContentItemValidationIssues,
    ShareabilityBasis,
    shareabilityBasisLabels,
    shareabilityBasisMembers
} from '../../models/components/contentItems/contentItemFormItem';

import './contentItems.css';

// The special member of a role set that means "the person who contributed this one". It is
// resolved per item rather than per user, which is why it cannot be an ordinary role name.
// AssociationPanel defines the same sentinel for its own gates; it is restated here rather than
// imported so this panel carries no dependency on the association surface.
export const OwnerRole = '[OWNER]';

// The placeholder a role set uses for the item's content type, so the narrow §18.6 tier can be
// expressed in an overridable string: `ContentItem-{ContentType}-ReadOnly` resolves to
// `ContentItem-Devotional-ReadOnly` for a devotional. A role naming it is dropped entirely when
// no content type is in play rather than composed into a name nobody holds.
export const ContentTypeToken = '{ContentType}';

// One content item, in the three states it has: contributed (`add`), read (`read`), and amended
// (`edit`). The ContentForm named in the design §20.6 table, built the way ReviewPanel and
// AssociationPanel were.
//
// SECURITY POSTURE. Every gate below decides what to RENDER and nothing more. The foundation and
// processing services re-decide add, modify and remove against the STORED row (§14.6, §14.7
// posture A), and must: a hidden button is a courtesy to the reader, never an authorization
// boundary. The block question is asked FIRST and outranks every grant, `[OWNER]` included — the
// same order the server-side gates use.
//
// FRESHNESS CONTRACT. This is a pure presentation component: props in, events out, no fetching,
// no sockets, no mutation. The CONSUMER owns persistence and freshness — it re-fetches and
// re-renders the panel whenever the item changes underneath it (an approval decision, another
// editor's save, a removal). Without that, the panel simply shows the world as of the last props
// it was handed.
//
// WHAT IT DELIBERATELY DOES NOT CONTAIN. Tags and bible references — AssociationPanel and its two
// wrappers already own that surface with their own approval and role rules, and they render
// BESIDE this panel on the page rather than within it. Approval controls belong to ReviewPanel.
//
// THEMING. Styling is expressed as CSS CLASSES rather than colours, so every control follows the
// light/dark theme. Pass btn-primary, btn-danger or any theme class — never a literal colour.
export interface ContentItemPanelProps {
    // ── Subject ───────────────────────────────────────────────────────────────
    // Absent puts the panel in `add`. Present, `read` is the default surface.
    //
    // Hand over a STABLE object — a fetched row, or a projection memoized by the consumer. The
    // editor is seeded from it whenever its identity changes, so a fresh object literal built on
    // every render would reseed the fields mid-keystroke.
    contentItem?: ContentItemFormItem;

    // Overrides the mode derived from `contentItem`, so a consumer can land straight on an edit
    // surface without faking a click. `edit` is refused back to `read` when isEditingAllowed is
    // off, or when the roles do not allow it.
    mode?: ContentItemPanelMode;

    // Which fields exist is per content type and is PASSED IN, never fetched: the ContentItemSetting
    // rows the consumer already holds (hasTitle, hasAuthor, contentTypeName, contentTypeIconCssClass).
    //
    // THE PANEL RESOLVES THE EFFECTIVE SETTING ITSELF, per §6.4 and §12.5.2 rules 1-2 — hand over
    // whatever rows you have and the most specific one wins. An item-level override (the row whose
    // ContentItemId is this item's) takes FULL precedence over the content type default; a
    // soft-deleted row is excluded from resolution entirely (§6.6). A mixed collection is
    // therefore safe: one item's override is never applied to another's.
    //
    // In `add` the picker offers the content type DEFAULTS that carry
    // IsAvailableAsGeneralUserContribution — an override belongs to one existing item and can
    // never be a type somebody contributes under, so it is never a tile. The tiles are ordered by
    // the rows' own SortOrder, so hand them over in any order.
    //
    // What is read here is the FIELD SHAPING and the type's presentation: hasTitle, hasAuthor,
    // contentTypeName, contentTypeDescription and contentTypeIconCssClass. The facet pairs
    // (TagsAllowed/ShowTags, Comments, Reactions, Links, Attachments, BibleReferences — §6.5)
    // govern surfaces this panel deliberately does not own: they are read by the association,
    // comment and reaction panels that render BESIDE it, each against this same effective row.
    contentItemSettingCollection?: ReadonlyArray<ContentItemSetting>;

    // ── Presentation ──────────────────────────────────────────────────────────
    showBorder?: boolean;
    cssClass?: string;
    titleText?: string;

    // Named for a screen reader when no visible title is rendered.
    ariaLabel?: string;

    // Whether the READ surface renders the item's own title. On by default, which is right for a
    // panel sitting among other content. A page whose whole subject is this one item states the
    // title in its own <h1> instead and turns this off, so the heading is not said twice.
    showItemTitle?: boolean;

    isLoading?: boolean;

    // Freezes the buttons while the consumer is persisting, so one click is one write.
    isSubmitting?: boolean;

    // ── Validation ────────────────────────────────────────────────────────────
    // What the API said was wrong, keyed by ITS parameter names. Matched case-insensitively
    // against the rendered fields; anything the panel cannot place renders in a summary above the
    // form rather than being dropped. The panel validates nothing itself — the server is the
    // authority on what a content item must carry, and a second opinion here would drift from it.
    validationIssues?: ContentItemValidationIssues;

    // ── Actions ───────────────────────────────────────────────────────────────
    // THE SURFACE SWITCH, ahead of every role check. Off by default, the safe posture
    // AssociationPanel takes with showModerationActions. While it is off the panel renders no
    // action affordance at all — no Edit, no Delete, no route into `edit` — however the roles
    // fall, and `mode="edit"` is refused back to `read`. It only ever subtracts: on, with no
    // qualifying role, still shows nothing. Add mode is not its concern — a consumer that does
    // not want an add surface does not render one.
    isEditingAllowed?: boolean;

    // ── Events ────────────────────────────────────────────────────────────────
    // The panel mutates nothing and fetches nothing. The CONSUMER owns persistence: it decides
    // whether onModified is a PUT or, on a terminal item, a fork into a new version (§3.4 rule 16).
    onAdded?: (item: ContentItemFormItem) => void;
    onModified?: (item: ContentItemFormItem) => void;
    onRemoved?: (item: ContentItemFormItem) => void;
    onCancelled?: () => void;
    onModeChanged?: (mode: ContentItemPanelMode) => void;

    // ── Roles ─────────────────────────────────────────────────────────────────
    // Names the entity so the role names can be composed per §18.6 (capability LAST, and plural —
    // a role names the people in a group). Only `ContentItem` has the content-type tier.
    entityType?: string;

    // Comma-separated overrides, all resolving the {ContentType} placeholder against the type in
    // play — the SELECTED type in `add`, the item's own type in `read` / `edit`.
    //
    // The block set is asked first on every gate and outranks every grant below, `[OWNER]`
    // included (#366): a contributor holding ContentItem-Devotional-ReadOnly sees no Edit and no
    // Delete on their own devotional, and no add surface for that type — while stories and quotes
    // stay open to them. `ReadOnly` is singular at every tier: it names a state its holder is in,
    // not a group.
    blockRoles?: string;

    // Empty means "any authenticated reader may contribute", which is the design's position —
    // there is no Contributor role (§18.6). [OWNER] is meaningless here (there is no item yet)
    // and is ignored.
    addRoles?: string;

    // The owner at ANY status; the Publisher tier and Administrators only while the item is Draft
    // or Submitted — an Approved or Rejected item is terminal to them. The Reviewers tier appears
    // in none of these sets: a reviewer reviews.
    editRoles?: string;

    // Removal is a takedown, not a moderation step (§14.7 posture A.3), so the Publisher tier does
    // not get it.
    deleteRoles?: string;

    // ── Login ─────────────────────────────────────────────────────────────────
    // Replaces the add form when nobody is signed in. Defaults to the current path as the return
    // url, exactly as AssociationPanel and SecuredRoute build it, so the reader lands back here
    // after signing in.
    loginHref?: string;
    loginButtonText?: string;
    loginButtonCssClass?: string;

    // ── Text ──────────────────────────────────────────────────────────────────
    typePickerTitleText?: string;
    typeLabelText?: string;
    titleLabelText?: string;
    titlePlaceholderText?: string;
    authorLabelText?: string;
    authorPlaceholderText?: string;
    contentLabelText?: string;
    shareabilityLabelText?: string;
    shareabilityReadLabelText?: string;
    sharePermissionLabelText?: string;
    sharePermissionPlaceholderText?: string;
    submitButtonText?: string;
    saveButtonText?: string;
    cancelButtonText?: string;
    editButtonText?: string;
    deleteButtonText?: string;
    validationSummaryText?: string;
    blockedText?: string;
    typeBlockedText?: string;
    noTypesText?: string;
    loadingText?: string;
    emptyText?: string;
    deleteConfirmTitleText?: string;
    deleteConfirmMessageText?: string;
    deleteConfirmButtonText?: string;
    authorByText?: string;

    // ── Theme classes ─────────────────────────────────────────────────────────
    submitButtonCssClass?: string;
    editButtonCssClass?: string;
    deleteButtonCssClass?: string;
}

const parseRoles = (roles: string): ReadonlyArray<string> =>
    roles
        .split(',')
        .map((role) => role.trim())
        .filter((role) => role.length > 0);

// The enum MEMBER name, which is what a role name is composed from (§18.6) — never the setting's
// ContentTypeName, which is editable per row and is what visitors read.
const roleSegmentOf = (contentType: ContentType): string => ContentType[contentType] ?? '';

// What the editor holds while it is being filled in. Separate from ContentItemFormItem because a
// half-typed form has no content type yet in `add`, and every text field is a string here even
// where the item's is optional.
type ContentItemDraft = {
    contentType: ContentType | null;
    title: string;
    author: string;
    content: string;
    shareabilityBasis: ShareabilityBasis;
    sharePermission: string;
};

const draftFromItem = (contentItem: ContentItemFormItem | undefined): ContentItemDraft => ({
    contentType: contentItem?.contentType ?? null,
    title: contentItem?.title ?? '',
    author: contentItem?.author ?? '',
    content: contentItem?.content ?? '',
    shareabilityBasis: contentItem?.shareabilityBasis ?? ShareabilityBasis.Owned,
    sharePermission: contentItem?.sharePermission ?? ''
});

export function ContentItemPanel({
    contentItem,
    mode,
    contentItemSettingCollection = [],
    showBorder = false,
    cssClass = '',
    titleText = '',
    ariaLabel = 'Content item',
    showItemTitle = true,
    isLoading = false,
    isSubmitting = false,
    validationIssues,
    isEditingAllowed = false,
    onAdded,
    onModified,
    onRemoved,
    onCancelled,
    onModeChanged,
    entityType = 'ContentItem',
    blockRoles,
    addRoles = '',
    editRoles,
    deleteRoles,
    loginHref,
    loginButtonText = 'Login to contribute',
    loginButtonCssClass = 'btn-outline-primary',
    typePickerTitleText = 'What are you sharing?',
    typeLabelText = 'Type',
    titleLabelText = 'Title',
    titlePlaceholderText = '',
    authorLabelText = 'Author',
    authorPlaceholderText = "e.g. Dwight L. Moody — leave blank if it's your own",
    contentLabelText = '',
    shareabilityLabelText = 'How are you permitted to share this?',
    shareabilityReadLabelText = 'Shareability',
    sharePermissionLabelText = 'Permission details',
    sharePermissionPlaceholderText =
    'e.g. Permission granted by the author by email, 12 Jan 2026',
    submitButtonText = 'Submit for review',
    saveButtonText = 'Save',
    cancelButtonText = 'Cancel',
    editButtonText = 'Edit',
    deleteButtonText = 'Delete',
    validationSummaryText = 'Please fix the following and try again:',
    blockedText = 'Contributions are not open to this account.',
    typeBlockedText = 'Not open to this account',
    noTypesText = 'Contributions are not open for any content type right now.',
    loadingText = 'Loading…',
    emptyText = 'There is nothing to show.',
    deleteConfirmTitleText = 'Are you sure?',
    deleteConfirmMessageText =
    'This removes the contribution. It cannot be undone from here.',
    deleteConfirmButtonText = 'Delete',
    authorByText = 'By',
    submitButtonCssClass = 'btn-primary',
    editButtonCssClass = 'btn-outline-primary',
    deleteButtonCssClass = 'btn-outline-danger'
}: ContentItemPanelProps) {
    const { isAuthenticated, user, userRoles } = useAuth();
    const location = useLocation();
    const headingId = useId();
    const fieldId = useId();

    const [requestedMode, setRequestedMode] = useState<ContentItemPanelMode | null>(null);
    const [draft, setDraft] = useState<ContentItemDraft>(() => draftFromItem(contentItem));
    const [isConfirmingDelete, setIsConfirmingDelete] = useState(false);

    const contentItemId = contentItem?.id;

    // A different item is a different editor. Keyed on the identity rather than the object so a
    // consumer re-rendering with an equivalent row does not wipe what is being typed.
    //
    // `mode` is a dependency too, so a CHANGE to the prop overrules a surface the reader asked
    // for earlier. Without it the reader's first Edit or Cancel would shadow the prop for the
    // rest of the item's life, and a consumer driving the panel from its own state could neither
    // close the editor after a save nor reopen it.
    useEffect(() => {
        setDraft(draftFromItem(contentItem));
        setRequestedMode(null);
        setIsConfirmingDelete(false);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [contentItemId, mode]);

    const resolvedLoginHref =
        loginHref ?? `/Account/Login?returnUrl=${encodeURIComponent(location.pathname)}`;

    const defaultBlockRoles =
        `ReadOnly, ${entityType}-ReadOnly, ${entityType}-${ContentTypeToken}-ReadOnly`;

    const defaultEditRoles =
        `${OwnerRole}, Publishers, ${entityType}-Publishers,`
        + ` ${entityType}-${ContentTypeToken}-Publishers, Administrators`;

    const defaultDeleteRoles = `${OwnerRole}, Administrators`;

    const blockRoleList = parseRoles(blockRoles ?? defaultBlockRoles);
    const addRoleList = parseRoles(addRoles);
    const editRoleList = parseRoles(editRoles ?? defaultEditRoles);
    const deleteRoleList = parseRoles(deleteRoles ?? defaultDeleteRoles);

    // A role naming a content type it has no value for would compose into a name nobody can hold,
    // so it is dropped rather than resolved against an empty segment.
    const resolveRoles = (
        roles: ReadonlyArray<string>,
        contentType: ContentType | null): ReadonlyArray<string> => {
        const segment = contentType == null ? '' : roleSegmentOf(contentType);

        return roles
            .map((role) => role.includes(ContentTypeToken) === false
                ? role
                : segment.length > 0 ? role.replace(ContentTypeToken, segment) : '')
            .filter((role) => role.length > 0);
    };

    const holdsAnyRole = (roles: ReadonlyArray<string>): boolean =>
        roles.some((role) => role !== OwnerRole && userRoles.includes(role));

    // The block question, asked before every grant. A ReadOnly at any tier trumps everything
    // within its scope (#366) — including the item's own contributor.
    const isBlockedFor = (contentType: ContentType | null): boolean =>
        holdsAnyRole(resolveRoles(blockRoleList, contentType));

    // CreatedBy is the account id: the audit trail resolves it through oid → objectidentifier →
    // nameidentifier, and a local Identity cookie carries only the last, which ASP.NET Core
    // Identity fills with AppUser.Id — the same value /api/accounts/me returns as userId.
    // A display name is deliberately NOT accepted: two accounts can share one.
    const isOwnedByViewer = (item: ContentItemFormItem): boolean => {
        const createdBy = item.createdBy ?? '';
        const viewerId = user?.userId ?? '';

        return isAuthenticated
            && createdBy.length > 0
            && viewerId.length > 0
            && createdBy === viewerId;
    };

    // A soft-deleted row is excluded from active policy resolution (§6.6), so it never reaches
    // any of the resolution below — filtered once here rather than remembered at each use.
    const activeSettings =
        contentItemSettingCollection.filter((setting) => setting.isDeleted !== true);

    // §6.4 / §12.5.2 rules 1-2 resolution, shared with the page above so the two cannot drift.
    // `add` passes no item id and so can only ever resolve a default, which is right: an override
    // cannot exist for an item that does not exist yet.
    const settingFor = (contentType: ContentType | null): ContentItemSetting | undefined =>
        resolveContentItemSetting(contentItemSettingCollection, contentType, contentItem?.id);

    const typeNameOf = (contentType: ContentType | null): string =>
        contentTypeNameOf(contentItemSettingCollection, contentType, contentItem?.id);

    // What the picker may offer: the content type DEFAULTS, and only those open to a general
    // contribution. An override belongs to one existing item and can never be a type somebody
    // contributes under, so it is not a tile however the consumer's collection arrived; and
    // IsAvailableAsGeneralUserContribution is the flag that answers "may this be contributed",
    // which is exactly the question a tile asks.
    //
    // SORTED BY THE ROW'S OWN SortOrder, ascending. The order the tiles appear in is a
    // presentation decision the administrator makes on the setting, not an accident of the
    // order the consumer's read answered with — and this is a pure presentation component, so
    // it sorts what it is handed rather than trusting the caller to have done it. A tie keeps
    // the order the rows arrived in, which `sort` guarantees.
    const offerableSettings = activeSettings
        .filter((setting) => setting.contentItemId == null
            && setting.isAvailableAsGeneralUserContribution)
        .sort((first, second) => first.sortOrder - second.sortOrder);

    const contributableSettings =
        offerableSettings.filter((setting) => isBlockedFor(setting.contentType) === false);

    // The type the surface is about: what the picker has landed on while adding, the item's own
    // type once there is one. Every gate and every field-shaping decision reads off this.
    const selectedContentType =
        contentItem?.contentType
        ?? draft.contentType
        ?? contributableSettings[0]?.contentType
        ?? offerableSettings[0]?.contentType
        ?? null;

    const activeSetting = settingFor(selectedContentType);
    const isBlockedForSelectedType = isBlockedFor(selectedContentType);

    // Every grant set is resolved against the type in play, exactly as the block set is — a role
    // still carrying the placeholder would be compared against the reader's roles as the literal
    // `ContentItem-{ContentType}-Publishers`, which nobody holds, and the gate would silently
    // never open.
    const resolvedAddRoleList = resolveRoles(addRoleList, selectedContentType);
    const resolvedEditRoleList = resolveRoles(editRoleList, selectedContentType);
    const resolvedDeleteRoleList = resolveRoles(deleteRoleList, selectedContentType);

    const status = contentItem?.approvalStatus ?? ApprovalStatus.Draft;

    const isAmendableStatus =
        status === ApprovalStatus.Draft || status === ApprovalStatus.Submitted;

    // The owner amends at any status — the consumer decides whether that PUTs or forks (§3.4 rule
    // 16). The rest of the tier is confined to a live item: a decided one is terminal to them.
    const mayEdit =
        isEditingAllowed
        && isAuthenticated
        && contentItem != null
        && isBlockedForSelectedType === false
        && ((resolvedEditRoleList.includes(OwnerRole) && isOwnedByViewer(contentItem))
            || (isAmendableStatus && holdsAnyRole(resolvedEditRoleList)));

    const mayDelete =
        isEditingAllowed
        && isAuthenticated
        && contentItem != null
        && isBlockedForSelectedType === false
        && ((resolvedDeleteRoleList.includes(OwnerRole) && isOwnedByViewer(contentItem))
            || holdsAnyRole(resolvedDeleteRoleList));

    const mayAdd =
        isAuthenticated
        && contributableSettings.length > 0
        && (resolvedAddRoleList.length === 0 || holdsAnyRole(resolvedAddRoleList));

    const derivedMode: ContentItemPanelMode =
        mode ?? (contentItem == null ? 'add' : 'read');

    const resolvedMode = requestedMode ?? derivedMode;

    // isEditingAllowed and the role gates both subtract, and both apply to a mode passed in as a
    // prop exactly as they do to one the reader asked for.
    const activeMode: ContentItemPanelMode =
        resolvedMode === 'edit' && mayEdit === false ? 'read' : resolvedMode;

    const changeMode = (nextMode: ContentItemPanelMode) => {
        setRequestedMode(nextMode);
        onModeChanged?.(nextMode);
    };

    const issueEntries = Object.entries(validationIssues ?? {});

    const issuesFor = (fieldName: string): ReadonlyArray<string> =>
        issueEntries
            .filter(([key]) => key.toLowerCase() === fieldName.toLowerCase())
            .flatMap(([, messages]) => messages);

    const invalidCssClass = (fieldName: string): string =>
        issuesFor(fieldName).length > 0 ? ' is-invalid' : '';

    // The feedback block is addressable so the input can point at it: Bootstrap's is-invalid is a
    // COLOUR, and a message sitting next to a field is not the same as a message attached to it.
    const issuesId = (fieldName: string): string => `${fieldId}-${fieldName.toLowerCase()}-issues`;

    const renderFieldIssues = (fieldName: string): ReactNode => {
        const messages = issuesFor(fieldName);

        if (messages.length === 0) {
            return null;
        }

        return (
            <div className="invalid-feedback" id={issuesId(fieldName)}>
                {messages.map((message, index) =>
                    <div key={`${fieldName}-${index}`}>{message}</div>)}
            </div>
        );
    };

    // aria-invalid and aria-describedby only when there is something to describe, so a clean
    // field is not announced as carrying an empty error.
    const fieldIssueAttributes = (fieldName: string) =>
        issuesFor(fieldName).length > 0
            ? { 'aria-invalid': true, 'aria-describedby': issuesId(fieldName) }
            : {};

    // A FIELD THE READER CANNOT SEE CONTRIBUTES NOTHING, and the row keeps whatever it already
    // had. One rule, and it settles both halves of the problem:
    //
    //   `add`   — a title typed under Story and then abandoned by picking Quote (whose effective
    //             setting has no title) must NOT be posted: the reader can no longer see it, the
    //             type is create-only, and nothing on the read surface would ever show it again.
    //             There is no item, so the fallback is empty and the value is dropped.
    //   `edit`  — a title already ON the row survives an amendment it was not shown for. Hiding
    //             is a rendering rule and never a destructive one (§20.6.2), so the fallback is
    //             the stored value.
    //
    // The type is taken as an argument rather than read off the draft, so the create-only rule
    // (§12.4.1 rule 7a) is enforced by the call rather than by remembering to check: an existing
    // item keeps the type it was validated under, whatever the picker last held.
    const toFormItem = (contentType: ContentType): ContentItemFormItem => ({
        ...contentItem,
        contentType,
        title: hasTitleField ? draft.title : contentItem?.title ?? '',
        author: hasAuthorField ? draft.author : contentItem?.author ?? '',
        content: draft.content,
        shareabilityBasis: draft.shareabilityBasis,

        // SharePermission is hidden by the reader's OWN answer rather than by policy, which is
        // why it drops to empty in both directions instead of falling back to the stored value.
        // A note saying "permission granted by the author" stored against an item the contributor
        // has just declared Owned is a provenance claim they withdrew - the server correlates
        // nothing (it length-checks and no more), so keeping it would file a contradiction that
        // no read surface ever shows again.
        sharePermission: hasSharePermissionField ? draft.sharePermission : ''
    });

    const submitAdd = () => {
        if (selectedContentType != null) {
            onAdded?.(toFormItem(selectedContentType));
        }
    };

    const submitModify = () => {
        if (contentItem != null) {
            onModified?.(toFormItem(contentItem.contentType));
        }
    };

    const cancelEdit = () => {
        setDraft(draftFromItem(contentItem));
        changeMode('read');
        onCancelled?.();
    };

    const confirmRemove = () => {
        setIsConfirmingDelete(false);

        if (contentItem != null) {
            onRemoved?.(contentItem);
        }
    };

    const hasTitleField = activeSetting?.hasTitle ?? (contentItem?.title ?? '').length > 0;
    const hasAuthorField = activeSetting?.hasAuthor ?? (contentItem?.author ?? '').length > 0;

    // Not a setting-driven flag like the two above: this one follows the basis the reader has
    // selected right now, and it drives the field, the placement of its messages and what is
    // submitted, so all three agree by construction.
    const hasSharePermissionField =
        draft.shareabilityBasis === ShareabilityBasis.PermissionGranted;
    const selectedTypeName = typeNameOf(selectedContentType);

    const contentLabel = contentLabelText.length > 0
        ? contentLabelText
        : selectedTypeName.length > 0 ? selectedTypeName : 'Content';

    // WHICH FIELD NAMES ACTUALLY HAVE SOMEWHERE TO LAND on this surface — derived, never listed.
    // A message with nowhere to render must reach the summary instead of vanishing, and what is
    // rendered varies: the picker exists in `add` alone, Title and Author come and go with the
    // effective setting, and SharePermission appears only under PermissionGranted. Listing the
    // six names statically silently swallowed a ContentType message in `edit`, and a Title
    // message for a type whose setting says it has none.
    const placedFieldNames = [
        ...(activeMode === 'add' ? ['ContentType'] : []),
        ...(hasTitleField ? ['Title'] : []),
        ...(hasAuthorField ? ['Author'] : []),
        'Content',
        'ShareabilityBasis',
        ...(hasSharePermissionField ? ['SharePermission'] : [])
    ];

    // Keyed on the FIELD as well as the message: the server's messages are shared literals
    // ("Text is required" for every text field), so two unplaced fields colliding on one key is
    // the norm rather than an edge case, and React treats same-keyed siblings as undefined.
    const unplacedIssues = issueEntries
        .filter(([key]) =>
            placedFieldNames.some((field) => field.toLowerCase() === key.toLowerCase()) === false)
        .flatMap(([field, messages]) =>
            messages.map((message, index) => ({ id: `${field}-${index}`, message })));

    const renderValidationSummary = (): ReactNode =>
        unplacedIssues.length === 0 ? null : (
            <div className="alert alert-danger" role="alert">
                <p className="mb-1">{validationSummaryText}</p>

                <ul className="mb-0 ps-3">
                    {unplacedIssues.map((issue) => <li key={issue.id}>{issue.message}</li>)}
                </ul>
            </div>
        );

    const renderTypePicker = (): ReactNode => (
        <fieldset className="mb-4">
            <legend className="form-label fw-bold fs-6">{typePickerTitleText}</legend>

            <div className="row g-3 row-cols-2 row-cols-md-3 row-cols-lg-5">
                {offerableSettings.map((setting) => {
                    const isSelected = setting.contentType === selectedContentType;
                    const isTypeBlocked = isBlockedFor(setting.contentType);

                    const selectionCssClass = isSelected && isTypeBlocked === false
                        ? 'border-primary bg-primary bg-opacity-10'
                        : '';

                    return (
                        <div key={setting.id} className="col">
                            <button
                                type="button"
                                className={`card h-100 w-100 text-center border p-3 g2h-content-item-type ${selectionCssClass}`}
                                aria-pressed={isSelected && isTypeBlocked === false}
                                disabled={isTypeBlocked}
                                title={isTypeBlocked ? typeBlockedText : undefined}
                                onClick={() =>
                                    setDraft((current) =>
                                        ({ ...current, contentType: setting.contentType }))}>
                                <i
                                    className={`bi ${setting.contentTypeIconCssClass} text-primary fs-4 mx-auto`}
                                    aria-hidden="true"></i>

                                <span className="fw-bold d-block mt-1">
                                    {setting.contentTypeName}
                                </span>

                                <small className="text-muted d-block">
                                    {isTypeBlocked
                                        ? typeBlockedText
                                        : setting.contentTypeDescription}
                                </small>
                            </button>
                        </div>
                    );
                })}
            </div>

            {issuesFor('ContentType').map((message) => (
                <div key={message} className="small text-danger mt-2">{message}</div>
            ))}
        </fieldset>
    );

    // The type is create-only, so `edit` states it rather than offering it.
    const renderFrozenType = (): ReactNode => (
        <div className="mb-3">
            <span className="form-label d-block">{typeLabelText}</span>

            <p className="form-control-plaintext mb-0">
                {activeSetting != null && (
                    <i
                        className={`bi ${activeSetting.contentTypeIconCssClass} text-primary me-2`}
                        aria-hidden="true"></i>
                )}

                {selectedTypeName}
            </p>
        </div>
    );

    const renderEditableFields = (): ReactNode => (
        <>
            {hasTitleField && (
                <div className="mb-3">
                    <label className="form-label" htmlFor={`${fieldId}-title`}>
                        {titleLabelText} <span className="text-danger" aria-hidden="true">*</span>
                    </label>

                    <input
                        type="text"
                        className={`form-control${invalidCssClass('Title')}`}
                        id={`${fieldId}-title`}
                        aria-required="true"
                        {...fieldIssueAttributes('Title')}
                        value={draft.title}
                        placeholder={titlePlaceholderText.length > 0
                            ? titlePlaceholderText
                            : `e.g. ${selectedTypeName} title`}
                        onChange={(event) =>
                            setDraft((current) => ({ ...current, title: event.target.value }))} />

                    {renderFieldIssues('Title')}
                </div>
            )}

            {hasAuthorField && (
                <div className="mb-3">
                    <label className="form-label" htmlFor={`${fieldId}-author`}>
                        {authorLabelText}
                    </label>

                    <input
                        type="text"
                        className={`form-control${invalidCssClass('Author')}`}
                        id={`${fieldId}-author`}
                        {...fieldIssueAttributes('Author')}
                        value={draft.author}
                        placeholder={authorPlaceholderText}
                        onChange={(event) =>
                            setDraft((current) => ({ ...current, author: event.target.value }))} />

                    {renderFieldIssues('Author')}
                </div>
            )}

            <div className="mb-4">
                <label className="form-label" htmlFor={`${fieldId}-content`}>
                    {contentLabel} <span className="text-danger" aria-hidden="true">*</span>
                </label>

                <textarea
                    className={`form-control${invalidCssClass('Content')}`}
                    id={`${fieldId}-content`}
                    aria-required="true"
                    {...fieldIssueAttributes('Content')}
                    rows={7}
                    value={draft.content}
                    placeholder={`Share your ${contentLabel.toLowerCase()}…`}
                    onChange={(event) =>
                        setDraft((current) =>
                            ({ ...current, content: event.target.value }))}></textarea>

                {renderFieldIssues('Content')}
            </div>

            <div className="mb-3">
                <label className="form-label" htmlFor={`${fieldId}-shareability`}>
                    {shareabilityLabelText}{' '}
                    <span className="text-danger" aria-hidden="true">*</span>
                </label>

                <select
                    className={`form-select${invalidCssClass('ShareabilityBasis')}`}
                    id={`${fieldId}-shareability`}
                    aria-required="true"
                    {...fieldIssueAttributes('ShareabilityBasis')}
                    value={draft.shareabilityBasis}
                    onChange={(event) =>
                        setDraft((current) => ({
                            ...current,
                            shareabilityBasis: Number(event.target.value) as ShareabilityBasis
                        }))}>
                    {shareabilityBasisMembers.map((basis) => (
                        <option key={basis} value={basis}>{shareabilityBasisLabels[basis]}</option>
                    ))}
                </select>

                {renderFieldIssues('ShareabilityBasis')}
            </div>

            {hasSharePermissionField && (
                <div className="mb-4">
                    <label className="form-label" htmlFor={`${fieldId}-share-permission`}>
                        {sharePermissionLabelText}
                    </label>

                    <input
                        type="text"
                        className={`form-control${invalidCssClass('SharePermission')}`}
                        id={`${fieldId}-share-permission`}
                        {...fieldIssueAttributes('SharePermission')}
                        maxLength={500}
                        value={draft.sharePermission}
                        placeholder={sharePermissionPlaceholderText}
                        onChange={(event) =>
                            setDraft((current) =>
                                ({ ...current, sharePermission: event.target.value }))} />

                    {renderFieldIssues('SharePermission')}
                </div>
            )}
        </>
    );

    // Not a <form>: the association panels that sit beside this one commit a chip on Enter, and
    // inside a form with a submit button that Enter would submit the page instead.
    const renderAdd = (): ReactNode => {
        if (isAuthenticated === false) {
            return (
                <Link
                    to={resolvedLoginHref}
                    className={`btn ${loginButtonCssClass} mb-0`}>
                    <i className="bi bi-box-arrow-in-right me-1"></i>{loginButtonText}
                </Link>
            );
        }

        if (offerableSettings.length === 0) {
            return <div className="alert alert-info" role="alert">{noTypesText}</div>;
        }

        if (mayAdd === false) {
            return <div className="alert alert-warning" role="alert">{blockedText}</div>;
        }

        return (
            <>
                {renderValidationSummary()}
                {renderTypePicker()}
                {renderEditableFields()}

                <div className="d-flex align-items-center gap-3">
                    <button
                        type="button"
                        className={`btn ${submitButtonCssClass} mb-0`}
                        disabled={isSubmitting}
                        onClick={submitAdd}>
                        {submitButtonText}
                    </button>

                    <button
                        type="button"
                        className="btn btn-link text-body p-0 mb-0"
                        disabled={isSubmitting}
                        onClick={() => onCancelled?.()}>
                        {cancelButtonText}
                    </button>
                </div>
            </>
        );
    };

    const renderEdit = (): ReactNode => (
        <>
            {renderValidationSummary()}
            {renderFrozenType()}
            {renderEditableFields()}

            <div className="d-flex align-items-center gap-3">
                <button
                    type="button"
                    className={`btn ${submitButtonCssClass} mb-0`}
                    disabled={isSubmitting}
                    onClick={submitModify}>
                    {saveButtonText}
                </button>

                <button
                    type="button"
                    className="btn btn-link text-body p-0 mb-0"
                    disabled={isSubmitting}
                    onClick={cancelEdit}>
                    {cancelButtonText}
                </button>
            </div>
        </>
    );

    const renderRead = (): ReactNode => {
        if (contentItem == null) {
            return <p className="small text-muted mb-0">{emptyText}</p>;
        }

        const shareability = shareabilityBasisLabels[contentItem.shareabilityBasis];

        return (
            <article>
                <p className="small text-uppercase fw-bold text-primary mb-2">
                    {activeSetting != null && (
                        <i
                            className={`bi ${activeSetting.contentTypeIconCssClass} me-1`}
                            aria-hidden="true"></i>
                    )}

                    {selectedTypeName}
                </p>

                {showItemTitle && hasTitleField && (contentItem.title ?? '').length > 0 && (
                    <h3 className="mb-2">{contentItem.title}</h3>
                )}

                {hasAuthorField && (contentItem.author ?? '').length > 0 && (
                    <p className="text-muted mb-3">{authorByText} {contentItem.author}</p>
                )}

                <div className="g2h-content-item-body mb-3">{contentItem.content}</div>

                <p className="small text-muted mb-1">
                    {shareabilityReadLabelText}: {shareability}
                </p>

                {contentItem.shareabilityBasis === ShareabilityBasis.PermissionGranted
                    && (contentItem.sharePermission ?? '').length > 0 && (
                        <p className="small text-muted mb-1">
                            {sharePermissionLabelText}: {contentItem.sharePermission}
                        </p>
                    )}

                {(mayEdit || mayDelete) && (
                    <div className="d-flex align-items-center gap-2 mt-3">
                        {mayEdit && (
                            <button
                                type="button"
                                className={`btn btn-sm ${editButtonCssClass} mb-0`}
                                disabled={isSubmitting}
                                onClick={() => changeMode('edit')}>
                                <i className="bi bi-pencil me-1" aria-hidden="true"></i>
                                {editButtonText}
                            </button>
                        )}

                        {mayDelete && (
                            <button
                                type="button"
                                className={`btn btn-sm ${deleteButtonCssClass} mb-0`}
                                disabled={isSubmitting}
                                onClick={() => setIsConfirmingDelete(true)}>
                                <i className="bi bi-trash me-1" aria-hidden="true"></i>
                                {deleteButtonText}
                            </button>
                        )}
                    </div>
                )}
            </article>
        );
    };

    const panelCssClass = showBorder
        ? `g2h-content-item-panel border rounded-3 p-3 p-lg-4 ${cssClass}`
        : `g2h-content-item-panel ${cssClass}`;

    const hasHeading = titleText.length > 0;

    return (
        <section
            className={panelCssClass}
            aria-labelledby={hasHeading ? headingId : undefined}
            aria-label={hasHeading ? undefined : ariaLabel}>

            {hasHeading && <h4 className="mb-3" id={headingId}>{titleText}</h4>}

            {isLoading ? (
                <p className="small text-muted mb-0">{loadingText}</p>
            ) : activeMode === 'add' ? (
                renderAdd()
            ) : activeMode === 'edit' ? (
                renderEdit()
            ) : (
                renderRead()
            )}

            <ConfirmDialog
                visible={isConfirmingDelete}
                title={deleteConfirmTitleText}
                message={deleteConfirmMessageText}
                confirmText={deleteConfirmButtonText}
                cancelText={cancelButtonText}
                onConfirm={confirmRemove}
                onCancel={() => setIsConfirmingDelete(false)} />
        </section>
    );
}
