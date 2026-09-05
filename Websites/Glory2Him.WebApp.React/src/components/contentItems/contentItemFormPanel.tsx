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
    approvalStatusMemberNames,
    approvalStatusRibbonLabels
} from '../../models/components/contentItems/contentItemTemplate';

import {
    ApprovalStatus,
    ContentItemFormItem,
    ContentItemValidationIssues,
    contributorApprovalStatusLabels,
    contributorApprovalStatusMembers,
    defaultContributorApprovalStatus,
    defaultShareabilityBasis,
    isContributorApprovalStatus,
    isOwnedShareabilityBasis,
    isPermissionShareabilityBasis,
    ShareabilityBasis,
    shareabilityBasisLabels,
    shareabilityBasisMembers
} from '../../models/components/contentItems/contentItemFormItem';

// The corner ribbon's geometry lives in coreUI.css as .g2h-corner-ribbon; imported here
// rather than relied on transitively, so the shape cannot go missing without a build error.
import '../coreUI/coreUI.css';
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

// THE FORM ENGINE behind ContentItemAddPanel and ContentItemEditPanel — the two writing faces
// of ContentItemPanel. One surface, two entries: no item is `add` (the picker and a blank
// form), an item is `edit` (the frozen type and the seeded form). READING is not here at all:
// the view templates (ContentItemDefaultPanel and its per-type overrides) are the one read
// surface the family has, which is the point of the merge — one component tree to keep true.
// The ContentForm named in the design §20.6 table, built the way ReviewPanel and
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
// BESIDE this panel on the page rather than within it. The APPROVAL DECISION belongs to
// ReviewPanel: what this panel carries is the "Submit as" row, and that offers the contributor's
// own two states alone — Draft and Submitted. A decided item (Approved, Rejected, Dismissed) does
// not render the row at all, because reversing a reviewer is not a move this surface has.
//
// THEMING. Styling is expressed as CSS CLASSES rather than colours, so every control follows the
// light/dark theme. Pass btn-primary, btn-danger or any theme class — never a literal colour.
export interface ContentItemFormPanelProps {
    // ── Subject ───────────────────────────────────────────────────────────────
    // Absent puts the panel in `add`. Present, the panel IS the editor for it.
    //
    // Hand over a STABLE object — a fetched row, or a projection memoized by the consumer. The
    // editor is seeded from it whenever its identity changes, so a fresh object literal built on
    // every render would reseed the fields mid-keystroke.
    contentItem?: ContentItemFormItem;

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

    // ── Contributor ───────────────────────────────────────────────────────────
    // WHOSE NAME AN OWNED BASIS PREFILLS INTO THE AUTHOR FIELD when the consumer has resolved
    // one — an amendment to somebody else's contribution must not be signed with the editor's
    // name. Absent, the signed-in reader's own display name serves, who in `add` IS the
    // contributor.
    submittedByDisplayName?: string;

    isLoading?: boolean;

    // Freezes the buttons while the consumer is persisting, so one click is one write.
    isSubmitting?: boolean;

    // ── Validation ────────────────────────────────────────────────────────────
    // What the API said was wrong, keyed by ITS parameter names. Matched case-insensitively
    // against the rendered fields; anything the panel cannot place renders in a summary above the
    // form rather than being dropped. The panel validates nothing itself — the server is the
    // authority on what a content item must carry, and a second opinion here would drift from it
    // — WITH TWO RULED EXCEPTIONS the panel is the right surface for: a permission basis makes
    // the Permission details box mandatory (a claim of permission with no permission named is
    // not a submission the product accepts), and the effective setting's Max*Length ceilings
    // cap the fields — the input refuses further typing, and a value already over a lowered
    // ceiling is refused at submit with the limit named.
    validationIssues?: ContentItemValidationIssues;

    // ── Actions ───────────────────────────────────────────────────────────────
    // THE SURFACE SWITCH, ahead of every role check. Off by default, the safe posture
    // AssociationPanel takes with showModerationActions. While it is off the panel renders no
    // action affordance at all — no Edit, no Delete, no route into `edit` — however the roles
    // fall, and `mode="edit"` is refused back to `read`. It only ever subtracts: on, with no
    // qualifying role, still shows nothing. Add mode is not its concern — a consumer that does
    // not want an add surface does not render one.
    showEditSection?: boolean;

    // Whether the panel wears a corner ribbon naming the item's approval status: grey
    // Draft, yellow Submitted, green Approved, red Rejected — the colours in
    // contentItems.css, keyed by data-approval-status. Off by default, and moot in add
    // mode: an item that does not exist yet has no status to wear.
    showApprovalStatusRibbon?: boolean;

    // WHAT THE "Submit as" ROW OPENS ON where the model names no status — `add`, which has no
    // item at all, and an item whose projection left approvalStatus unset. Submitted by
    // default: the contribution page exists to put work in front of a reviewer, and the button
    // under the row says so.
    //
    // It seeds and nothing more. An item that HAS a status is reported by that status, never by
    // this — a surface whose drafts should open as drafts passes ApprovalStatus.Draft, and its
    // submitted items still read Submitted.
    //
    // Only the two a contributor owns are meaningful here. A decided status would name a state
    // the row does not render for, which in `add` would leave the form with no way to answer
    // the question at all.
    approvalStatusDefault?: ApprovalStatus;

    // ── Events ────────────────────────────────────────────────────────────────
    // The panel mutates nothing and fetches nothing. The CONSUMER owns persistence: it decides
    // whether onModified is a PUT or, on a terminal item, a fork into a new version (§3.4 rule 16).
    onAdded?: (item: ContentItemFormItem) => void;
    onModified?: (item: ContentItemFormItem) => void;
    onRemoved?: (item: ContentItemFormItem) => void;
    onCancelled?: () => void;

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
    authorPrefilledHintText?: string;
    contentLabelText?: string;
    shareabilityLabelText?: string;
    sharePermissionLabelText?: string;
    sharePermissionPlaceholderText?: string;
    sharePermissionRequiredText?: string;
    submitAsLabelText?: string;

    // The over-length refusal, with {max} standing in for the ceiling the setting names.
    maxLengthExceededText?: string;
    submitButtonText?: string;
    saveButtonText?: string;
    cancelButtonText?: string;
    deleteButtonText?: string;
    validationSummaryText?: string;
    blockedText?: string;
    typeBlockedText?: string;
    noTypesText?: string;
    loadingText?: string;
    deleteConfirmTitleText?: string;
    deleteConfirmMessageText?: string;
    deleteConfirmButtonText?: string;
    // ── Theme classes ─────────────────────────────────────────────────────────
    submitButtonCssClass?: string;
    deleteButtonCssClass?: string;
}

const parseRoles = (roles: string): ReadonlyArray<string> =>
    roles
        .split(',')
        .map((role) => role.trim())
        .filter((role) => role.length > 0);

// The enum MEMBER name. It does two jobs: it is what a content-type-scoped role name is composed
// from (§18.6), and it is the key contentItems.css hangs the type's chip colour on. Never the
// setting's ContentTypeName, which an administrator may edit at any time — a renamed type must
// not silently shed either its role names or its colour.
const contentTypeKeyOf = (contentType: ContentType): string => ContentType[contentType] ?? '';

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
    approvalStatus: ApprovalStatus;
};

const draftFromItem = (
    contentItem: ContentItemFormItem | undefined,
    approvalStatusDefault: ApprovalStatus): ContentItemDraft => ({
    contentType: contentItem?.contentType ?? null,
    title: contentItem?.title ?? '',
    author: contentItem?.author ?? '',
    content: contentItem?.content ?? '',
    shareabilityBasis: contentItem?.shareabilityBasis ?? defaultShareabilityBasis,
    sharePermission: contentItem?.sharePermission ?? '',

    // WHERE THE CONTRIBUTOR'S OWN ANSWER LIVES once they give one. Until then it is not read
    // at all — an unanswered row reports the model, not this (see effectiveApprovalStatus) —
    // so the seed exists to keep the draft coherent with what is on screen rather than to
    // drive it.
    approvalStatus: contentItem?.approvalStatus ?? approvalStatusDefault
});

export function ContentItemFormPanel({
    contentItem,
    contentItemSettingCollection = [],
    showBorder = false,
    cssClass = '',
    titleText = '',
    ariaLabel = 'Content item',
    submittedByDisplayName,
    isLoading = false,
    isSubmitting = false,
    validationIssues,
    showEditSection = false,
    showApprovalStatusRibbon = false,
    approvalStatusDefault = defaultContributorApprovalStatus,
    onAdded,
    onModified,
    onRemoved,
    onCancelled,
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
    // No longer "leave blank if it's your own": an owned basis FILLS this field rather than
    // wanting it empty, so the old instruction contradicted what the form now does.
    authorPlaceholderText = 'e.g. Dwight L. Moody',
    contentLabelText = '',
    shareabilityLabelText = 'How are you permitted to share this?',
    sharePermissionLabelText = 'Permission details',
    sharePermissionRequiredText =
    'Please say what permission you have — it is required for this sharing basis.',
    maxLengthExceededText =
    'Too long — this type allows at most {max} characters here.',
    // NOT a claim, the EVIDENCE. "Permission granted by the author, 12 Jan 2026" is a
    // contributor's word for it; what a reviewer can act on is the wording itself, pasted in —
    // which is also why the field is a textarea rather than a line (see below).
    sharePermissionPlaceholderText =
    'e.g. paste the permission itself — the email, the copyright notice, or the sharing terms',
    submitAsLabelText = 'Submit as',
    submitButtonText = 'Submit for review',
    saveButtonText = 'Save',
    cancelButtonText = 'Cancel',
    deleteButtonText = 'Delete',
    validationSummaryText = 'Please fix the following and try again:',
    blockedText = 'Contributions are not open to this account.',
    typeBlockedText = 'Not open to this account',
    noTypesText = 'Contributions are not open for any content type right now.',
    loadingText = 'Loading…',
    deleteConfirmTitleText = 'Are you sure?',
    deleteConfirmMessageText =
    'This removes the contribution. It cannot be undone from here.',
    deleteConfirmButtonText = 'Delete',
    authorPrefilledHintText =
    'Your display name. Change it if you write under a different one.',
    submitButtonCssClass = 'btn-primary',
    deleteButtonCssClass = 'btn-outline-danger'
}: ContentItemFormPanelProps) {
    const { isAuthenticated, user, userRoles } = useAuth();
    const location = useLocation();
    const headingId = useId();
    const fieldId = useId();

    const [draft, setDraft] =
        useState<ContentItemDraft>(() => draftFromItem(contentItem, approvalStatusDefault));
    const [isConfirmingDelete, setIsConfirmingDelete] = useState(false);

    // The one client-side mandatory rule (see validationIssues above): raised on a refused
    // submit, cleared the moment the reader answers — by typing a detail, or implicitly by
    // moving the basis off a permission one, which unrenders the field and its message.
    const [isSharePermissionMissing, setIsSharePermissionMissing] = useState(false);

    // Whether the contributor has had their hands on the Author field. Once they have, the
    // prefill above stops second-guessing them — including when they deliberately empty it.
    const [isAuthorTouched, setIsAuthorTouched] = useState(false);

    // The same question for the "Submit as" row, and it matters more there: the status is the
    // one field on this form that ANOTHER PROCESS moves, so until the contributor has answered
    // it themselves the row reports the item rather than a copy of it — see
    // effectiveApprovalStatus.
    const [isSubmitAsTouched, setIsSubmitAsTouched] = useState(false);

    const contentItemId = contentItem?.id;

    // A different item is a different editor. Keyed on the identity rather than the object so a
    // consumer re-rendering with an equivalent row does not wipe what is being typed.
    useEffect(() => {
        setDraft(draftFromItem(contentItem, approvalStatusDefault));
        setIsConfirmingDelete(false);
        setIsAuthorTouched(false);
        setIsSubmitAsTouched(false);
        setIsSharePermissionMissing(false);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [contentItemId]);

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
        const segment = contentType == null ? '' : contentTypeKeyOf(contentType);

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
    // THE ITEM'S OWN EMBEDDED WINNER COMES FIRST: a projection handed over by a list surface
    // already resolved §6.4 for this item, and re-resolving from a collection that may not
    // even hold the override would silently un-override it. The collection remains the
    // answer for every OTHER type — the add picker's tiles — and the fallback for an item
    // that arrived without its setting.
    const settingFor = (contentType: ContentType | null): ContentItemSetting | undefined =>
        contentItem?.contentItemSetting != null && contentType === contentItem.contentType
            ? contentItem.contentItemSetting
            : resolveContentItemSetting(
                contentItemSettingCollection, contentType, contentItem?.id);

    const typeNameOf = (contentType: ContentType | null): string =>
        contentItem?.contentItemSetting != null && contentType === contentItem.contentType
            ? contentItem.contentItemSetting.contentTypeName
            : contentTypeNameOf(contentItemSettingCollection, contentType, contentItem?.id);

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
        showEditSection
        && isAuthenticated
        && contentItem != null
        && isBlockedForSelectedType === false
        && ((resolvedEditRoleList.includes(OwnerRole) && isOwnedByViewer(contentItem))
            || (isAmendableStatus && holdsAnyRole(resolvedEditRoleList)));

    const mayDelete =
        showEditSection
        && isAuthenticated
        && contentItem != null
        && isBlockedForSelectedType === false
        && ((resolvedDeleteRoleList.includes(OwnerRole) && isOwnedByViewer(contentItem))
            || holdsAnyRole(resolvedDeleteRoleList));

    const mayAdd =
        isAuthenticated
        && contributableSettings.length > 0
        && (resolvedAddRoleList.length === 0 || holdsAnyRole(resolvedAddRoleList));

    // The item IS the mode: no item is `add`, an item is `edit`. Whether editing is actually
    // open to this reader is mayEdit's answer, rendered as a refusal rather than silently
    // becoming a different surface — the read face belongs to the view templates now.
    const activeMode: 'add' | 'edit' = contentItem == null ? 'add' : 'edit';

    const issueEntries = Object.entries(validationIssues ?? {});

    const issuesFor = (fieldName: string): ReadonlyArray<string> =>
        [
            ...issueEntries
                .filter(([key]) => key.toLowerCase() === fieldName.toLowerCase())
                .flatMap(([, messages]) => messages),

            // The mandatory-permission rule speaks through the same channel the server's
            // messages use, so the field lights up and is announced identically either way
            // — and the setting's ceilings speak through it too.
            ...(fieldName.toLowerCase() === 'sharepermission' && isSharePermissionMissing
                ? [sharePermissionRequiredText]
                : []),

            ...(fieldName.toLowerCase() === 'title' && titleLengthIssue != null
                ? [titleLengthIssue]
                : []),

            ...(fieldName.toLowerCase() === 'author' && authorLengthIssue != null
                ? [authorLengthIssue]
                : []),

            ...(fieldName.toLowerCase() === 'content' && contentLengthIssue != null
                ? [contentLengthIssue]
                : [])
        ];

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
    // THE EXCEPTION IS A FIELD HIDDEN BY THE READER'S OWN ANSWER rather than by policy, which
    // drops to empty in BOTH directions instead of falling back to the stored value.
    // SharePermission under a non-permission basis is the one: a note saying "permission granted
    // by the author" against an item whose basis no longer rests on anybody's permission is a
    // claim the contributor withdrew, and the server correlates nothing (it length-checks and no
    // more), so keeping it would file a contradiction no read surface ever shows again.
    //
    // The type is taken as an argument rather than read off the draft, so the create-only rule
    // (§12.4.1 rule 7a) is enforced by the call rather than by remembering to check: an existing
    // item keeps the type it was validated under, whatever the picker last held.
    const toFormItem = (contentType: ContentType): ContentItemFormItem => ({
        ...contentItem,
        contentType,
        title: hasTitleField ? draft.title : contentItem?.title ?? '',

        // effectiveAuthor, not draft.author: a prefilled name the contributor left alone is the
        // answer they gave, and what the field showed them is what gets filed.
        author: hasAuthorField ? effectiveAuthor : contentItem?.author ?? '',

        content: draft.content,
        shareabilityBasis: draft.shareabilityBasis,
        sharePermission: hasSharePermissionField ? draft.sharePermission : '',

        // effectiveApprovalStatus, not draft.approvalStatus: what the row was SHOWING is what
        // gets filed — the same rule the author field keeps one line up. It also settles the
        // hidden case on its own, since an unanswerable row is reporting the model: a decided
        // item files the decision it arrived with, whether it was decided before the editor
        // opened or underneath it.
        approvalStatus: effectiveApprovalStatus,

        // The emitted projection is SELF-CONTAINED like everything else that carries this
        // shape: `add` constructs the winner from the collection it shaped the form with, so
        // the consumer can hand what it receives straight to a detail surface.
        contentItemSetting: settingFor(contentType)
    });

    // The mandatory-permission gate, asked by BOTH submits: a permission basis with no
    // permission named is refused here rather than posted — the one rule this panel decides
    // itself (see validationIssues above).
    const holdsRequiredSharePermission = (): boolean => {
        if (hasSharePermissionField && draft.sharePermission.trim().length === 0) {
            setIsSharePermissionMissing(true);
            return false;
        }

        return true;
    };

    // The ceilings gate BOTH submits — the live messages already say why, so refusing is
    // simply not posting what they name.
    const holdsFieldLengths = (): boolean =>
        titleLengthIssue == null
        && authorLengthIssue == null
        && contentLengthIssue == null;

    const submitAdd = () => {
        if (selectedContentType != null
            && holdsRequiredSharePermission()
            && holdsFieldLengths()) {
            onAdded?.(toFormItem(selectedContentType));
        }
    };

    const submitModify = () => {
        if (contentItem != null
            && holdsRequiredSharePermission()
            && holdsFieldLengths()) {
            onModified?.(toFormItem(contentItem.contentType));
        }
    };

    const cancelEdit = () => {
        setDraft(draftFromItem(contentItem, approvalStatusDefault));
        onCancelled?.();
    };

    const confirmRemove = () => {
        setIsConfirmingDelete(false);

        if (contentItem != null) {
            onRemoved?.(contentItem);
        }
    };

    // THE BASIS IN PLAY: whatever the reader has selected — so a field that comes and goes
    // with the basis moves the moment the dropdown does.
    const effectiveShareabilityBasis = draft.shareabilityBasis;

    const isOwnedBasis = isOwnedShareabilityBasis(effectiveShareabilityBasis);

    const hasTitleField = activeSetting?.hasTitle ?? (contentItem?.title ?? '').length > 0;

    // Governed by the type's own hasAuthor and nothing else. An owned basis does NOT remove this
    // field: "it's my own" says who wrote it, but not what they want to be called for it, and a
    // contributor may well publish under a pen name, an initial, or a maiden name. So the basis
    // fills the field in rather than taking it away — see contributorDisplayName below.
    const hasAuthorField =
        activeSetting?.hasAuthor ?? (contentItem?.author ?? '').length > 0;

    // THE SETTING'S CEILINGS, enforced twice over: maxLength on the input stops further
    // typing, and these live messages catch what typing cannot cause — a stored value
    // already over a ceiling an administrator lowered afterwards — refusing the submit
    // rather than posting what the server would bounce.
    const maxLengthIssueOf = (
        value: string,
        maxLength: number | null | undefined): string | null =>
        maxLength != null && value.length > maxLength
            ? maxLengthExceededText.replace('{max}', String(maxLength))
            : null;

    const titleLengthIssue = hasTitleField
        ? maxLengthIssueOf(draft.title, activeSetting?.maxTitleLength)
        : null;

    const contentLengthIssue =
        maxLengthIssueOf(draft.content, activeSetting?.maxContentLength);

    // WHOSE NAME AN OWNED BASIS PUTS IN THE AUTHOR FIELD. The submitter's where the consumer has
    // resolved one — an amendment to somebody else's contribution must not be signed with the
    // editor's name — otherwise the signed-in reader's, who in `add` is the contributor.
    const contributorDisplayName = (submittedByDisplayName ?? user?.displayName ?? '').trim();

    // WHAT THE AUTHOR FIELD ACTUALLY HOLDS, derived rather than stored.
    //
    // Derived, because the reader's own name can arrive after the first paint (auth resolves
    // asynchronously) and an effect writing it into the draft would be racing a field they may
    // already be typing in. Deriving it means a late name simply appears.
    //
    // It fills only an EMPTY field, and only until the contributor touches it. Both conditions
    // matter: without the first, opening an existing item whose author is "Grace Abara" under an
    // owned basis would overwrite her with whoever is looking at it; without the second, a pen
    // name would be reverted the moment anything else on the form changed.
    const isAuthorPrefilled =
        isAuthorTouched === false
        && isOwnedBasis
        && draft.author.length === 0
        && contributorDisplayName.length > 0;

    const effectiveAuthor = isAuthorPrefilled ? contributorDisplayName : draft.author;

    const authorLengthIssue = hasAuthorField
        ? maxLengthIssueOf(effectiveAuthor, activeSetting?.maxAuthorLength)
        : null;

    // Neither this nor the author rule above is setting-driven the way hasTitle is: both follow
    // the basis the reader has selected right now, and each drives its field, the placement of its
    // messages and what is submitted, so all three agree by construction.
    const hasSharePermissionField =
        isPermissionShareabilityBasis(effectiveShareabilityBasis);

    // THE STATUS AS THE MODEL HOLDS IT, which is what the ribbon and the status pill render
    // from — so everything below reads the same fact those do, and the row cannot sit on a
    // stale copy while the corner of the same panel says otherwise.
    //
    // BOTH HALVES ARE PROPS, deliberately. Where the model names no status — `add`, which has
    // no item at all, and an item whose projection left the field unset — the consumer's
    // approvalStatusDefault answers, and it answers LIVE. Reading the draft's seed here
    // instead would have re-introduced the same staleness one level down: a consumer moving
    // the default would move nothing, because the draft reseeds on the item's identity and
    // `add` has no identity to change.
    const storedApprovalStatus = contentItem?.approvalStatus ?? approvalStatusDefault;

    // WHETHER THE STATUS IS STILL THE CONTRIBUTOR'S TO SET, which is the whole of the "Submit as"
    // row's render rule.
    //
    // READ OFF THE MODEL, NOT THE DRAFT. The status is the one field on this form that ANOTHER
    // PROCESS decides — a reviewer approving or rejecting the row, here or elsewhere — so the
    // authority is the row as the consumer last handed it over, and a decision reaching the
    // panel takes the question away rather than leaving the surface offering a move that is no
    // longer anybody's to make.
    //
    // A DECIDED ITEM SHOWS NOTHING HERE. Approved, Rejected and Dismissed are a reviewer's
    // award, and there is no transition backwards out of them for a contributor to take — so the
    // label and the dropdown go together rather than the dropdown being rendered disabled, which
    // would still be offering a move nobody has.
    const hasSubmitAsField = isContributorApprovalStatus(storedApprovalStatus);

    // WHAT THE "SUBMIT AS" ROW ACTUALLY HOLDS, derived rather than stored — the same shape
    // effectiveAuthor takes, and for the same reason turned up the other way round.
    //
    // The draft is seeded once per ITEM (see the reset effect), which is right for a field the
    // reader types: a consumer re-render must not wipe half a sentence. But the status is not
    // theirs alone — a review decision, or any other surface, moves it under a live editor with
    // the item's identity unchanged, and a seeded copy would go on reporting what the row used
    // to say while the ribbon two inches away showed the truth.
    //
    // So an UNANSWERED row simply reports the model, and follows it wherever it goes; once the
    // contributor has answered it, their pending choice is theirs and stands. Withdrawing a
    // submission back to Draft survives a re-render, exactly as a pen name does.
    const effectiveApprovalStatus =
        isSubmitAsTouched && hasSubmitAsField ? draft.approvalStatus : storedApprovalStatus;

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
        ...(hasSharePermissionField ? ['SharePermission'] : []),
        ...(hasSubmitAsField ? ['ApprovalStatus'] : [])
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

    // FROZEN, the tiles still stand: the edit face wears the SAME layout the add face does
    // — one look for both writing surfaces — but the type is create-only (§12.4.1 rule 7a),
    // so every tile is disabled and the item's own stays selected. An item whose type is
    // not a contributable tile (a blog post) still shows: its own setting joins the row.
    const frozenPickerSettings =
        offerableSettings.some((setting) => setting.contentType === selectedContentType)
            || activeSetting == null
            ? offerableSettings
            : [...offerableSettings, activeSetting];

    const renderTypePicker = (isFrozen: boolean): ReactNode => (
        <fieldset className="mb-4">
            <legend className="form-label fw-bold fs-6">
                {isFrozen ? typeLabelText : typePickerTitleText}
            </legend>

            <div className="row g-3 row-cols-2 row-cols-md-3 row-cols-lg-5">
                {(isFrozen ? frozenPickerSettings : offerableSettings).map((setting) => {
                    const isSelected = setting.contentType === selectedContentType;
                    const isTypeBlocked = isFrozen === false && isBlockedFor(setting.contentType);

                    // No colour class here. The tile carries aria-pressed and its type key, and
                    // contentItems.css paints the selection from the SAME palette the chip reads
                    // — so a selected Testimony tile and a Testimony chip cannot disagree, and a
                    // recolour is one stylesheet edit rather than two.
                    const selectionCssClass = isSelected && isTypeBlocked === false
                        ? 'g2h-content-item-type-selected'
                        : '';

                    return (
                        <div key={setting.id} className="col">
                            <button
                                type="button"
                                className={`card h-100 w-100 text-center border p-3 g2h-content-item-type ${selectionCssClass}`}
                                data-content-type={contentTypeKeyOf(setting.contentType)}
                                aria-pressed={isSelected && isTypeBlocked === false}
                                disabled={isTypeBlocked || isFrozen}
                                title={isTypeBlocked ? typeBlockedText : undefined}
                                onClick={() => {
                                    if (isFrozen === false) {
                                        setDraft((current) => ({
                                            ...current,
                                            contentType: setting.contentType
                                        }));
                                    }
                                }}>
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

            {/* In edit there is no picker to answer for, so a ContentType message reaches
                the summary instead (see placedFieldNames). */}
            {isFrozen === false && issuesFor('ContentType').map((message) => (
                <div key={message} className="small text-danger mt-2">{message}</div>
            ))}
        </fieldset>
    );

    // THE TYPE CHIP, and the only place a type's colour is decided — except that no colour is
    // decided HERE. The chip carries the type's enum member name as a data attribute and
    // contentItems.css keys the palette off it, so a type is recoloured by editing a stylesheet
    // and a newly seeded type wears a neutral chip until somebody chooses one, rather than
    // inheriting a colour meant for a different type.
    //
    // That indirection is also what makes the picker live: the attribute is rendered from
    // whatever type is IN PLAY, so moving the selection in `add` re-resolves the cascade on the
    // next paint. Nothing is wired between the dropdown and the colour — the selection simply is
    // the selector.
    const renderTypeChip = (): ReactNode => {
        if (selectedContentType == null) {
            return null;
        }

        const iconCssClass = activeSetting?.contentTypeIconCssClass ?? '';

        return (
            <span
                className="badge g2h-content-item-chip"
                data-content-type={contentTypeKeyOf(selectedContentType)}>
                {iconCssClass.length > 0 && (
                    <i className={`bi ${iconCssClass} me-2`} aria-hidden="true"></i>
                )}

                {selectedTypeName}
            </span>
        );
    };

    // The type is create-only, so `edit` states it rather than offering it.
    const renderFrozenType = (): ReactNode => (
        <div className="mb-3">
            <span className="form-label d-block">{typeLabelText}</span>

            <p className="form-control-plaintext mb-0">{renderTypeChip()}</p>
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
                        maxLength={activeSetting?.maxTitleLength ?? undefined}
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
                        maxLength={activeSetting?.maxAuthorLength ?? undefined}
                        {...fieldIssueAttributes('Author')}
                        aria-describedby={isAuthorPrefilled ? `${fieldId}-author-hint` : undefined}
                        value={effectiveAuthor}
                        placeholder={authorPlaceholderText}
                        onChange={(event) => {
                            setIsAuthorTouched(true);

                            setDraft((current) =>
                                ({ ...current, author: event.target.value }));
                        }} />

                    {/* Only while the panel is the one supplying the name. Once the contributor
                        has typed their own it is theirs, and a note explaining where it came
                        from would be describing something that is no longer true. */}
                    {isAuthorPrefilled && (
                        <div className="form-text" id={`${fieldId}-author-hint`}>
                            {authorPrefilledHintText}
                        </div>
                    )}

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
                    maxLength={activeSetting?.maxContentLength ?? undefined}
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

            {/* The field only renders under a permission basis, and under one it is
                MANDATORY — the asterisk is unconditional because the two arrive together. */}
            {hasSharePermissionField && (
                <div className="mb-3">
                    <label className="form-label" htmlFor={`${fieldId}-share-permission`}>
                        {sharePermissionLabelText}{' '}
                        <span className="text-danger" aria-hidden="true">*</span>
                    </label>

                    {/* A TEXTAREA, not a line, because the answer the field wants is PASTED
                        rather than typed — an email granting permission, a licence or a
                        copyright line, all of which arrive with their own newlines. It opens at
                        one row so it costs no more space than the input it replaces, and the
                        reader drags it taller when they have more to paste. */}
                    <textarea
                        className={'form-control g2h-content-item-share-permission'
                            + invalidCssClass('SharePermission')}
                        id={`${fieldId}-share-permission`}
                        aria-required="true"
                        {...fieldIssueAttributes('SharePermission')}
                        maxLength={500}
                        rows={1}
                        value={draft.sharePermission}
                        placeholder={sharePermissionPlaceholderText}
                        onChange={(event) => {
                            setIsSharePermissionMissing(false);

                            setDraft((current) =>
                                ({ ...current, sharePermission: event.target.value }));
                        }}></textarea>

                    {renderFieldIssues('SharePermission')}
                </div>
            )}

            {/* THE LAST QUESTION THE FORM ASKS, and it belongs last: everything above is what
                the contribution IS, and this is what to DO with it — so the reader answers it
                with the whole of their submission already in front of them, one row above the
                button that acts on the answer. */}
            {hasSubmitAsField && (
                <div className="mb-4">
                    <label className="form-label" htmlFor={`${fieldId}-approval-status`}>
                        {submitAsLabelText}{' '}
                        <span className="text-danger" aria-hidden="true">*</span>
                    </label>

                    <select
                        className={`form-select${invalidCssClass('ApprovalStatus')}`}
                        id={`${fieldId}-approval-status`}
                        aria-required="true"
                        {...fieldIssueAttributes('ApprovalStatus')}
                        value={effectiveApprovalStatus}
                        onChange={(event) => {
                            setIsSubmitAsTouched(true);

                            setDraft((current) => ({
                                ...current,
                                approvalStatus: Number(event.target.value) as ApprovalStatus
                            }));
                        }}>
                        {contributorApprovalStatusMembers.map((status) => (
                            <option key={status} value={status}>
                                {contributorApprovalStatusLabels[status]}
                            </option>
                        ))}
                    </select>

                    {renderFieldIssues('ApprovalStatus')}
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
                {renderTypePicker(false)}
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

    // The editor refuses rather than downgrades: with no read face here, a reader the gates
    // turn away is told so — the same posture the add face takes for a blocked account.
    const renderEdit = (): ReactNode => {
        if (mayEdit === false) {
            return <div className="alert alert-warning" role="alert">{blockedText}</div>;
        }

        return (
            <>
                {renderValidationSummary()}

                {/* The same tiles the add face shows, frozen — with the chip as the
                    fallback when the consumer handed over no default rows to stand as
                    tiles. */}
                {frozenPickerSettings.length > 0
                    ? renderTypePicker(true)
                    : renderFrozenType()}

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

                    {/* Removal rides on the editor now that there is no read face to carry
                        it. Right-aligned: it is the one control here that destroys. */}
                    {mayDelete && (
                        <button
                            type="button"
                            className={`btn btn-sm ${deleteButtonCssClass} ms-auto mb-0`}
                            disabled={isSubmitting}
                            onClick={() => setIsConfirmingDelete(true)}>
                            <i className="bi bi-trash me-1" aria-hidden="true"></i>
                            {deleteButtonText}
                        </button>
                    )}
                </div>
            </>
        );
    };

    // The corner ribbon: the item's status member name is what the stylesheet colours,
    // the same contract the card template keeps. An item without a status — or one whose
    // status has no ribbon entry (Dismissed) — wears none.
    const ribbonLabel =
        showApprovalStatusRibbon && contentItem?.approvalStatus != null
            ? approvalStatusRibbonLabels[contentItem.approvalStatus]
            : undefined;

    const panelCssClass = showBorder
        ? `g2h-content-item-panel border rounded-3 p-3 p-lg-4 ${cssClass}`
        : `g2h-content-item-panel ${cssClass}`;

    const ribbonedPanelCssClass =
        ribbonLabel != null
            ? `${panelCssClass} g2h-has-corner-ribbon g2h-has-approval-ribbon`
            : panelCssClass;

    const hasHeading = titleText.length > 0;

    return (
        <section
            className={ribbonedPanelCssClass}
            aria-labelledby={hasHeading ? headingId : undefined}
            aria-label={hasHeading ? undefined : ariaLabel}>

            {ribbonLabel != null && (
                <span
                    className="g2h-corner-ribbon g2h-approval-ribbon"
                    data-approval-status={approvalStatusMemberNames[contentItem!.approvalStatus!]}>
                    {ribbonLabel}
                </span>
            )}

            {hasHeading && <h4 className="mb-3" id={headingId}>{titleText}</h4>}

            {isLoading ? (
                <p className="small text-muted mb-0">{loadingText}</p>
            ) : activeMode === 'add' ? (
                renderAdd()
            ) : (
                renderEdit()
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
