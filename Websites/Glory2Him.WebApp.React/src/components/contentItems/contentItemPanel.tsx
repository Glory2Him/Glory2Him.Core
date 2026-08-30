import { ReactNode, useEffect, useId, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { Avatar } from '../coreUI/avatar';
import { ConfirmDialog } from '../coreUI/confirmDialog';
import { formatDate } from '../coreUI/dateFormats';
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
    defaultShareabilityBasis,
    isOwnedShareabilityBasis,
    isPermissionShareabilityBasis,
    ShareabilityBasis,
    shareabilityBasisLabels,
    shareabilityBasisMembers,
    shareabilityBasisReadLabels
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

    // WHICH HEADING the read surface renders that title as. h3 by default, which is right for a
    // panel sitting among other content; a page whose whole subject is this one item raises it to
    // h1 and keeps the title where the design puts it — under the type chip — rather than
    // duplicating it above the panel to get the outline right.
    titleHeadingLevel?: 'h1' | 'h2' | 'h3' | 'h4';

    // ── Byline ────────────────────────────────────────────────────────────────
    // WHO CONTRIBUTED IT, resolved. The item itself carries only CreatedBy, an account id, so the
    // name and face are PASSED IN by the consumer that looked them up (GET /api/contributors/{id})
    // — this panel fetches nothing. Absent, the read surface simply omits the block: a byline that
    // is still loading must not flash a placeholder name under somebody's testimony.
    //
    // The image url is optional independently of the name: Avatar draws a deterministic initials
    // circle when there is no picture, which is the same fallback the rest of the site uses.
    submittedByDisplayName?: string;
    submittedByImageUrl?: string;

    // Where the contributor's name links to, when it should link at all. Absent, the name is
    // rendered as plain text — the correct default, because there is no public contributor page
    // yet and a link to nowhere is worse than no link.
    submittedByHref?: string;

    // ── Engagement ────────────────────────────────────────────────────────────
    // The figures that read along the bottom of the byline. Each is INDEPENDENTLY OPTIONAL and
    // undefined leaves it out rather than rendering a zero — the same contract AuthorByline takes.
    // That matters: "0 comments" is a claim that the conversation is empty, whereas a surface
    // whose comments are switched off (§6.5 ShowComments) has no claim to make at all.
    //
    // NONE OF THEM ARE COMPUTED HERE. Reading time is a function of the content, and the three
    // counts are separate reads against the comment, reaction and view surfaces — all of them the
    // consumer's to gather, because this panel is pure presentation.
    readingTimeMinutes?: number;
    reactionCount?: number;
    commentCount?: number;
    viewCount?: number;

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
    authorPrefilledHintText?: string;
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
    submittedByLabelText?: string;
    readingTimeLabelText?: string;

    // Singular and plural are separate props rather than one string with an 's' appended, because
    // "1 reactions" under a contribution is the sort of small wrongness a reader notices and a
    // translator cannot fix.
    reactionLabelText?: string;
    reactionsLabelText?: string;
    commentLabelText?: string;
    commentsLabelText?: string;
    viewLabelText?: string;
    viewsLabelText?: string;

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

// The enum MEMBER name. It does two jobs: it is what a content-type-scoped role name is composed
// from (§18.6), and it is the key contentItems.css hangs the type's chip colour on. Never the
// setting's ContentTypeName, which an administrator may edit at any time — a renamed type must
// not silently shed either its role names or its colour.
const contentTypeKeyOf = (contentType: ContentType): string => ContentType[contentType] ?? '';

// WHETHER TWO NAMES NAME THE SAME PERSON, for the one decision that turns on it: whether the
// read surface would be printing the author and the submitter as two separate facts when they are
// one. Compared case- and accent-insensitively because "normal user" and "Normal User" are the
// same person typed twice, and an empty name matches nothing — an unresolved submitter is not
// evidence of a duplicate.
//
// It is deliberately a NAME comparison and never an identity one: the panel has no account id for
// whoever the Author field names, and cannot get one. Two different people who share a display
// name will collapse into one column here, which is the right trade for a byline — the alternative
// prints the same name twice under most contributions on the site.
const isSameName = (first: string, second: string): boolean =>
    first.length > 0
    && second.length > 0
    && first.localeCompare(second, undefined, { sensitivity: 'accent' }) === 0;

// The wire carries createdWhen as an ISO string, and a projection is free to leave it out. A
// value that will not parse is dropped rather than printed as "Invalid Date" under somebody's
// contribution.
const toRenderableDate = (value: string | undefined): Date | null => {
    if (value == null || value.length === 0) {
        return null;
    }

    const parsedDate = new Date(value);

    return Number.isNaN(parsedDate.getTime()) ? null : parsedDate;
};

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
    shareabilityBasis: contentItem?.shareabilityBasis ?? defaultShareabilityBasis,
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
    titleHeadingLevel = 'h3',
    submittedByDisplayName,
    submittedByImageUrl,
    submittedByHref,
    readingTimeMinutes,
    reactionCount,
    commentCount,
    viewCount,
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
    // No longer "leave blank if it's your own": an owned basis FILLS this field rather than
    // wanting it empty, so the old instruction contradicted what the form now does.
    authorPlaceholderText = 'e.g. Dwight L. Moody',
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
    authorPrefilledHintText =
    'Your display name. Change it if you write under a different one.',
    submittedByLabelText = 'Submitted by',
    readingTimeLabelText = 'min read',
    reactionLabelText = 'reaction',
    reactionsLabelText = 'reactions',
    commentLabelText = 'comment',
    commentsLabelText = 'comments',
    viewLabelText = 'View',
    viewsLabelText = 'Views',
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

    // Whether the contributor has had their hands on the Author field. Once they have, the
    // prefill above stops second-guessing them — including when they deliberately empty it.
    const [isAuthorTouched, setIsAuthorTouched] = useState(false);

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
        setIsAuthorTouched(false);
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

    // THE BASIS IN PLAY. The item's own on the read surface, whatever the reader has selected on
    // the two editing surfaces — so a field that comes and goes with the basis moves the moment
    // the dropdown does, and what a reader is shown never depends on an editor's draft.
    const effectiveShareabilityBasis =
        activeMode === 'read' && contentItem != null
            ? contentItem.shareabilityBasis
            : draft.shareabilityBasis;

    const isOwnedBasis = isOwnedShareabilityBasis(effectiveShareabilityBasis);

    const hasTitleField = activeSetting?.hasTitle ?? (contentItem?.title ?? '').length > 0;

    // Governed by the type's own hasAuthor and nothing else. An owned basis does NOT remove this
    // field: "it's my own" says who wrote it, but not what they want to be called for it, and a
    // contributor may well publish under a pen name, an initial, or a maiden name. So the basis
    // fills the field in rather than taking it away — see contributorDisplayName below.
    const hasAuthorField =
        activeSetting?.hasAuthor ?? (contentItem?.author ?? '').length > 0;

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

    // Neither this nor the author rule above is setting-driven the way hasTitle is: both follow
    // the basis the reader has selected right now, and each drives its field, the placement of its
    // messages and what is submitted, so all three agree by construction.
    const hasSharePermissionField =
        isPermissionShareabilityBasis(effectiveShareabilityBasis);
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

    // One labelled column of the byline's top row: a quiet caption over the value it names.
    const renderBylineFact = (
        factKey: string,
        labelText: string,
        value: ReactNode,
        leading?: ReactNode): ReactNode => (
        <div className="g2h-content-item-fact" key={factKey}>
            {leading}

            <span>
                <span className="g2h-content-item-fact-label d-block">{labelText}</span>
                <span className="g2h-content-item-fact-value d-block">{value}</span>
            </span>
        </div>
    );

    // A count and the noun it counts, agreeing in number. Undefined is left OUT rather than
    // rendered as a zero: "0 comments" asserts that the conversation is empty, which is a
    // different statement from a surface that has no comments to report (§6.5 ShowComments).
    const renderCountFigure = (
        figureKey: string,
        count: number | undefined,
        iconCssClass: string,
        singularText: string,
        pluralText: string): ReactNode =>
        count == null ? null : (
            <li className="nav-item" key={figureKey}>
                <i className={`${iconCssClass} me-1`} aria-hidden="true"></i>
                {count.toLocaleString()} {count === 1 ? singularText : pluralText}
            </li>
        );

    // WHO CONTRIBUTED IT, WHO WROTE IT, AND WHAT MAY BE DONE WITH IT — the three answers a reader
    // wants before the first paragraph — with the article's own figures reading underneath.
    //
    // Every part is conditional and the whole block disappears when none of them can be answered,
    // so a panel handed nothing but an item renders no empty scaffolding. The shareability column
    // is the one constant: every item has a basis, and a reader is always entitled to know it.
    const renderByline = (item: ContentItemFormItem): ReactNode => {
        // The RESOLVED submitter and nothing else — never contributorDisplayName, which falls
        // back to the signed-in reader. Falling back here would compare the author against
        // whoever happens to be looking and hide the column for a stranger's benefit.
        const submittedByName = (submittedByDisplayName ?? '').trim();
        const authorName = (item.author ?? '').trim();
        const submittedOn = toRenderableDate(item.createdWhen);

        // The name links only where the consumer gave it somewhere to go. There is no public
        // contributor page yet, and a link to nowhere reads as a broken one.
        const submittedByValue = submittedByHref == null || submittedByHref.length === 0
            ? submittedByName
            : <Link className="text-reset" to={submittedByHref}>{submittedByName}</Link>;

        const facts = [
            submittedByName.length === 0
                ? null
                : renderBylineFact(
                    'submitted-by',
                    submittedByLabelText,
                    submittedByValue,
                    <Avatar
                        name={submittedByName}
                        imageUrl={submittedByImageUrl}
                        sizePx={44} />),

            // Suppressed only when it would print ONE PERSON TWICE — the author and the
            // submitter being the same name, which is what an untouched owned basis produces.
            // A contributor who publishes under another name has said something the submitter
            // column does not, so it is shown; and where no submitter has been resolved there is
            // nothing to be duplicating, so it is shown then too.
            hasAuthorField === false
                || authorName.length === 0
                || isSameName(authorName, submittedByName)
                ? null
                : renderBylineFact('author', authorLabelText, authorName),

            renderBylineFact(
                'shareability',
                shareabilityReadLabelText,
                shareabilityBasisReadLabels[item.shareabilityBasis])
        ].filter((fact) => fact != null);

        const figures = [
            submittedOn == null
                ? null
                : <li className="nav-item" key="submitted-on">{formatDate(submittedOn)}</li>,

            readingTimeMinutes == null
                ? null
                : (
                    <li className="nav-item" key="reading-time">
                        <i className="bi bi-clock-fill me-1" aria-hidden="true"></i>
                        {readingTimeMinutes} {readingTimeLabelText}
                    </li>
                ),

            renderCountFigure(
                'reactions', reactionCount, 'far fa-heart',
                reactionLabelText, reactionsLabelText),

            renderCountFigure(
                'comments', commentCount, 'far fa-comment',
                commentLabelText, commentsLabelText),

            renderCountFigure(
                'views', viewCount, 'far fa-eye',
                viewLabelText, viewsLabelText)
        ].filter((figure) => figure != null);

        if (facts.length === 0 && figures.length === 0) {
            return null;
        }

        return (
            <div className="g2h-content-item-byline mb-4">
                {facts.length > 0 && (
                    <div className="g2h-content-item-facts">{facts}</div>
                )}

                {figures.length > 0 && (
                    <ul className="nav nav-divider align-items-center small mb-0 mt-3">
                        {figures}
                    </ul>
                )}
            </div>
        );
    };

    const renderRead = (): ReactNode => {
        if (contentItem == null) {
            return <p className="small text-muted mb-0">{emptyText}</p>;
        }

        // The heading level is the CONSUMER's call (see titleHeadingLevel): a panel among other
        // content is an h3, a page whose whole subject is this item is an h1. Capitalised because
        // JSX reads a lowercase tag name as a literal element and an uppercase one as a value.
        const TitleHeading = titleHeadingLevel;

        const showsTitle =
            showItemTitle && hasTitleField && (contentItem.title ?? '').length > 0;

        return (
            <article>
                <p className="mb-2">{renderTypeChip()}</p>

                {showsTitle && (
                    <TitleHeading className="g2h-content-item-title mb-3">
                        {contentItem.title}
                    </TitleHeading>
                )}

                {renderByline(contentItem)}

                <div className="g2h-content-item-body mb-3">{contentItem.content}</div>

                {hasSharePermissionField
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
