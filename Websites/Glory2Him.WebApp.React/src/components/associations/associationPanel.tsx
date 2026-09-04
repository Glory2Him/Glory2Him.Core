import { KeyboardEvent, ReactElement, ReactNode, useId, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '../securitys/authProvider';
import { GlobalModerationRoles } from './associationRoles';
import {
    ApprovalStatus,
    AssociationItem
} from '../../models/components/associations/associationItem';
import './associations.css';

// The special member of removeRoles that means "the person who contributed this one". It is
// resolved per item rather than per user, which is why it cannot be an ordinary role name.
export const OwnerRole = '[OWNER]';

// A labelled set of association chips — a post's tags, its bible references, anything projected
// to AssociationItem — with an optional box beneath for suggesting another.
//
// SECURITY POSTURE. Every gate below decides what to RENDER and nothing more. The foundation
// services re-decide add, delete and approval against the stored row themselves (design §14.6),
// and must: a hidden button is a courtesy to the reader, never an authorization boundary.
//
// THEMING. Styling is expressed as CSS CLASSES rather than colours, so a chip follows the
// light/dark theme like everything else on the page. Pass btn-success-soft, btn-primary-soft or
// any theme class — never a literal colour, which would be fixed in both themes.
//
// Unlike SuggestionPanel, which this replaces, the panel reads useAuth() itself rather than
// taking isAuthenticated as a prop: the owner rules need the viewer's identity per item, and
// SecuredComponent already establishes reading auth from context inside a gating component.
export interface AssociationPanelProps {
    // ── Presentation ──────────────────────────────────────────────────────────
    title: string;

    // Surrounds the panel with the bordered card ContributionPrompt uses.
    showBorder?: boolean;
    cssClass?: string;

    // ── Chips ─────────────────────────────────────────────────────────────────
    associationCollection?: ReadonlyArray<AssociationItem>;

    // The theme class carrying the chip's look — btn-success-soft, btn-primary-soft, …
    chipCssClass?: string;

    // Leading decoration, rendered in this order: [status icon][prefix][value]. Neither the icon
    // nor the prefix is part of the stored value.
    chipPrefixText?: string;

    // Shown when no status-specific icon applies. The three below take precedence for their
    // own status, so a bible reference reads as a book once approved and as an hourglass while
    // it is still waiting.
    chipIconCssClass?: string;
    approvedIconCssClass?: string;
    pendingIconCssClass?: string;
    rejectedIconCssClass?: string;

    approvedTooltip?: string;
    pendingTooltip?: string;
    rejectedTooltip?: string;

    // How a chip behaves when clicked. chipHrefFor wins when both are given and is the better
    // choice for navigation: a real link can be middle-clicked, opened in a new tab and is
    // announced as a destination, none of which an onClick handler gets.
    chipHrefFor?: (item: AssociationItem) => string;
    chipOnClick?: (item: AssociationItem) => void;

    // ── Collection states ─────────────────────────────────────────────────────
    isLoading?: boolean;
    emptyText?: string;

    // Comma-separated roles that may see an item in ANY status — a draft, and a refusal. The
    // widest grant there is, so it is administrators-only by default: a draft is nobody's
    // business but its author's, and a refusal is nobody's but the tier that can still act on it.
    viewAllRoles?: string;

    // ── Actions ───────────────────────────────────────────────────────────────
    // The single switch over Remove, Reject and Approve. OFF by default, which is the safe
    // posture: chips render according to the visibility gates, but the panel is read-only —
    // with ONE carve-out, the contributor's own unapproved item, which they may still withdraw.
    // That is what lets someone add and tidy their own suggestions before anyone rules on them.
    //
    // Switched on, the full action matrix applies: Remove to removeRoles, Reject and Approve to
    // moderationRoles, and the owner branch resolved first so nobody rules on their own
    // submission. Turn it on for a moderation surface, leave it off everywhere else.
    showModerationActions?: boolean;

    // ── Remove ────────────────────────────────────────────────────────────────
    // Deletes the association outright. Distinct from Reject, which is a recorded verdict that
    // leaves the row in place — a moderator commonly wants both available.
    onRemove?: (item: AssociationItem) => void;
    removeTooltip?: string;
    removeIconCssClass?: string;
    removeButtonCssClass?: string;

    // Comma-separated. Empty means "any authenticated reader". [OWNER] means the contributor of
    // that specific item, and only while it is still unapproved — once approved it is no longer
    // theirs alone to withdraw. Holding any other listed role allows it outright, so an
    // administrator may remove an item whether or not they contributed it.
    removeRoles?: string;

    // ── Moderation (approve / reject) ─────────────────────────────────────────
    // The verdict pair, offered on a SUBMITTED item to a moderator who does not own it. Owning
    // it suppresses both, so nobody rules on their own submission — an administrator included;
    // the owner is left with Remove. All three actions compose, so a moderator looking at
    // somebody else's submission sees Remove, Reject and Approve together.
    onApprove?: (item: AssociationItem) => void;
    onReject?: (item: AssociationItem) => void;
    approveTooltip?: string;
    rejectTooltip?: string;
    approveIconCssClass?: string;
    rejectIconCssClass?: string;
    approveButtonCssClass?: string;
    rejectButtonCssClass?: string;

    // Comma-separated. [OWNER] is meaningless here and is ignored — owning the item suppresses
    // the verdict pair rather than granting it.
    moderationRoles?: string;

    // ── Add ───────────────────────────────────────────────────────────────────
    showAdd?: boolean;

    // Comma-separated, empty by default: any authenticated reader may suggest. [OWNER] is
    // meaningless — there is no item yet to own — and is ignored.
    addRoles?: string;
    suggestTitle?: string;
    suggestDescription?: string;
    addPlaceholderText?: string;
    addMaxLength?: number;
    addButtonText?: string;

    // Raised once PER ASSOCIATION by Enter AND by the add button, which share one commit path,
    // after that value has been normalized and found not to be a duplicate. One box can hold
    // SEVERAL: a comma or a semicolon separates them, so "faith, healing" arrives as two calls,
    // and "grace and faith; love" as two more — the words inside a value are left alone, which
    // is why only those two characters separate.
    //
    // The PARENT owns the collection: nothing is appended here, so an optimistic chip and a
    // server round-trip are both the caller's call. A handler that appends to state MUST do so
    // functionally — setItems(previous => [...previous, one]) — because several calls land in
    // the same tick and a stale closure would keep only the last of them.
    onAdd?: (value: string) => void;

    // Applied to each separated value, before the duplicate check and before onAdd. Defaults to
    // a trim.
    normalizeAddedValue?: (rawValue: string) => string;

    // ── Login ─────────────────────────────────────────────────────────────────
    // Replaces the add box when adding is on but nobody is signed in. Defaults to the current
    // path as the return url, so the reader lands back here.
    loginHref?: string;
    loginButtonText?: string;
    loginButtonCssClass?: string;
    loginButtonOnClick?: () => void;
}

// One box, possibly several associations. A comma and a semicolon separate; nothing else does,
// so "grace and faith" stays one association, spaces and conjunction intact, and a bible
// reference keeps the colons and dashes it is written with.
const separateValues = (rawValue: string): ReadonlyArray<string> => rawValue.split(/[,;]/);

const parseRoles = (roles: string): ReadonlyArray<string> =>
    roles
        .split(',')
        .map((role) => role.trim())
        .filter((role) => role.length > 0);

export function AssociationPanel({
    title,
    showBorder = false,
    cssClass = '',
    associationCollection = [],
    chipCssClass = 'btn-success-soft',
    chipPrefixText = '',
    chipIconCssClass,
    approvedIconCssClass,
    pendingIconCssClass = 'bi-hourglass-split',
    rejectedIconCssClass = 'bi-slash-circle',
    approvedTooltip = '',
    pendingTooltip = 'Pending approval',
    rejectedTooltip = 'Not approved',
    chipHrefFor,
    chipOnClick,
    isLoading = false,
    emptyText = '',
    viewAllRoles = 'Administrators',
    showModerationActions = false,
    onRemove,
    removeTooltip = 'Remove',
    removeIconCssClass = 'bi-x-lg',
    removeButtonCssClass = 'btn-danger',
    removeRoles = `${OwnerRole}, Administrators`,
    onApprove,
    onReject,
    approveTooltip = 'Approve',
    rejectTooltip = 'Reject',
    approveIconCssClass = 'bi-check-lg',
    rejectIconCssClass = 'bi-slash-circle',
    approveButtonCssClass = 'btn-success',
    rejectButtonCssClass = 'btn-warning',
    moderationRoles = GlobalModerationRoles,
    showAdd = false,
    addRoles = '',
    suggestTitle = '',
    suggestDescription = '',
    addPlaceholderText = '',
    addMaxLength = 100,
    addButtonText = 'Add',
    onAdd,
    normalizeAddedValue = (rawValue: string) => rawValue.trim(),
    loginHref,
    loginButtonText = 'Login to suggest',
    loginButtonCssClass = 'btn-outline-primary',
    loginButtonOnClick
}: AssociationPanelProps) {
    const { isAuthenticated, user, userRoles } = useAuth();
    const location = useLocation();
    const [draft, setDraft] = useState('');
    const headingId = useId();

    const resolvedLoginHref =
        loginHref ?? `/Account/Login?returnUrl=${encodeURIComponent(location.pathname)}`;

    const removeRoleList = parseRoles(removeRoles);
    const moderationRoleList = parseRoles(moderationRoles);
    const viewAllRoleList = parseRoles(viewAllRoles);

    const statusOf = (item: AssociationItem): ApprovalStatus =>
        item.approvalStatus ?? ApprovalStatus.Approved;

    const isPendingStatus = (status: ApprovalStatus): boolean =>
        status === ApprovalStatus.Draft || status === ApprovalStatus.Submitted;

    // CreatedBy is the account id: the audit trail resolves it through oid → objectidentifier →
    // nameidentifier, and a local Identity cookie carries only the last, which ASP.NET Core
    // Identity fills with AppUser.Id — the same value /api/accounts/me returns as userId.
    // A display name is deliberately NOT accepted: two accounts can share one.
    const isOwnedByViewer = (item: AssociationItem): boolean => {
        const createdBy = item.createdBy ?? '';
        const viewerId = user?.userId ?? '';

        return isAuthenticated
            && createdBy.length > 0
            && viewerId.length > 0
            && createdBy === viewerId;
    };

    const holdsAnyRole = (roles: ReadonlyArray<string>): boolean =>
        roles.some((role) => role !== OwnerRole && userRoles.includes(role));

    const mayRemove = (item: AssociationItem): boolean => {
        if (isAuthenticated === false) {
            return false;
        }

        // The carve-out that survives showModerationActions being off: a contributor may tidy
        // their own suggestion before anyone rules on it. It covers exactly the two statuses the
        // owner can still see — a suggestion is theirs to withdraw while it is a draft or
        // awaiting a decision, and no longer once it has been accepted into the post, or refused.
        const ownerMayRemove =
            removeRoleList.includes(OwnerRole)
            && isOwnedByViewer(item)
            && isPendingStatus(statusOf(item));

        if (ownerMayRemove) {
            return true;
        }

        // Everyone else needs the moderation surface switched on.
        if (showModerationActions === false) {
            return false;
        }

        return removeRoleList.length === 0 || holdsAnyRole(removeRoleList);
    };

    // A verdict is only ever passed on somebody ELSE'S submission: owning the item suppresses
    // the pair outright, so nobody rules on their own — an administrator included. They keep
    // Remove, which mayRemove decides separately.
    const mayModerate = (item: AssociationItem): boolean =>
        showModerationActions
        && isAuthenticated
        && statusOf(item) === ApprovalStatus.Submitted
        && isOwnedByViewer(item) === false
        && holdsAnyRole(moderationRoleList);

    // Three tiers, widest last. A rejected suggestion never lingers publicly on the post that
    // refused it, and a draft is nobody's business but its author's and the publishing tier's.
    const isVisible = (item: AssociationItem): boolean => {
        // Removal outranks every other gate, approval included: an Approved row that has been
        // taken down is still gone.
        if (item.isDeleted === true) {
            return false;
        }

        const status = statusOf(item);

        if (status === ApprovalStatus.Approved) {
            return true;
        }

        // The widest grant: any status, a draft included.
        if (holdsAnyRole(viewAllRoleList)) {
            return true;
        }

        // The contributor follows their own suggestion until it is decided — not past it.
        if (isOwnedByViewer(item) && isPendingStatus(status)) {
            return true;
        }

        // A moderator sees what is waiting on their decision, and only that: a draft was never
        // put forward for anyone to judge, and a refusal has already been judged.
        return status === ApprovalStatus.Submitted && holdsAnyRole(moderationRoleList);
    };

    const iconFor = (item: AssociationItem): string | undefined => {
        const status = statusOf(item);

        if (status === ApprovalStatus.Approved) {
            return approvedIconCssClass ?? chipIconCssClass;
        }

        if (isPendingStatus(status)) {
            return pendingIconCssClass ?? chipIconCssClass;
        }

        return rejectedIconCssClass ?? chipIconCssClass;
    };

    const tooltipFor = (item: AssociationItem): string => {
        const status = statusOf(item);

        if (status === ApprovalStatus.Approved) {
            return approvedTooltip;
        }

        return isPendingStatus(status) ? pendingTooltip : rejectedTooltip;
    };

    const addRoleList = parseRoles(addRoles);

    const mayAdd =
        showAdd
        && isAuthenticated
        && (addRoleList.length === 0 || holdsAnyRole(addRoleList));

    // Signed out with adding on: the box is replaced by a way in, never simply hidden —
    // otherwise the reader cannot tell the panel accepts suggestions at all.
    const showLoginPrompt = showAdd && isAuthenticated === false;

    const visibleItems = associationCollection.filter(isVisible);

    // Each separated value is judged on its OWN, so one that is empty or already listed costs
    // the others nothing: "faith, , Faith, healing" adds faith and healing and quietly drops the
    // blank and the repeat. The box clears either way — whatever was typed has been dealt with.
    const commitDraft = () => {
        setDraft('');

        const accepted: string[] = [];

        const isAlreadyListed = (value: string): boolean =>
            // The whole collection is checked, not just the visible slice — a suggestion the
            // reader cannot see is still a duplicate — and so is anything this same commit has
            // already accepted, which is what makes "faith, faith" a single addition.
            associationCollection.some(
                (item) => item.value.toLowerCase() === value.toLowerCase())
            || accepted.some(
                (acceptedValue) => acceptedValue.toLowerCase() === value.toLowerCase());

        separateValues(draft).forEach((separatedValue) => {
            const value = normalizeAddedValue(separatedValue);

            if (value.length === 0 || isAlreadyListed(value)) {
                return;
            }

            accepted.push(value);
            onAdd?.(value);
        });
    };

    // The button's other half. Both ways in call commitDraft, so the separating, normalizing
    // and duplicate checks cannot drift apart between them.
    const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
        if (event.key === 'Enter') {
            // Enter inside a form would submit the page before the value is ever read.
            event.preventDefault();
            commitDraft();
        }
    };

    const renderChipLabel = (item: AssociationItem): ReactElement => {
        const icon = iconFor(item);

        const content: ReactNode = (
            <>
                {icon != null && <i className={`bi ${icon} me-1`} aria-hidden="true"></i>}
                {chipPrefixText}{item.value}
            </>
        );

        if (chipHrefFor != null) {
            return (
                <Link to={chipHrefFor(item)} className="g2h-association-chip-label">
                    {content}
                </Link>
            );
        }

        if (chipOnClick != null) {
            return (
                <button
                    type="button"
                    className="g2h-association-chip-label"
                    onClick={() => chipOnClick(item)}>
                    {content}
                </button>
            );
        }

        return <span className="g2h-association-chip-label">{content}</span>;
    };

    // One small square per permitted action, in escalating order of consequence left to right:
    // Remove (destroys the row), Reject (records a refusal), Approve (accepts it). They compose
    // rather than exclude, so a moderator on somebody else's submission gets all three.
    const renderChipActions = (item: AssociationItem): ReactNode => {
        const canModerate = mayModerate(item);

        return (
            <>
                {mayRemove(item) && (
                    <button
                        type="button"
                        className={`btn ${removeButtonCssClass} g2h-association-chip-action`}
                        title={removeTooltip}
                        aria-label={`${removeTooltip} ${item.value}`}
                        onClick={() => onRemove?.(item)}>
                        <i className={`bi ${removeIconCssClass}`} aria-hidden="true"></i>
                    </button>
                )}

                {canModerate && (
                    <button
                        type="button"
                        className={`btn ${rejectButtonCssClass} g2h-association-chip-action`}
                        title={rejectTooltip}
                        aria-label={`${rejectTooltip} ${item.value}`}
                        onClick={() => onReject?.(item)}>
                        <i className={`bi ${rejectIconCssClass}`} aria-hidden="true"></i>
                    </button>
                )}

                {canModerate && (
                    <button
                        type="button"
                        className={`btn ${approveButtonCssClass} g2h-association-chip-action`}
                        title={approveTooltip}
                        aria-label={`${approveTooltip} ${item.value}`}
                        onClick={() => onApprove?.(item)}>
                        <i className={`bi ${approveIconCssClass}`} aria-hidden="true"></i>
                    </button>
                )}
            </>
        );
    };

    const panelCssClass = showBorder
        ? `g2h-association-panel border rounded-3 p-3 p-lg-4 ${cssClass}`
        : `g2h-association-panel ${cssClass}`;

    return (
        <section className={panelCssClass} aria-labelledby={headingId}>
            <h4 className="mb-3" id={headingId}>{title}</h4>

            {isLoading ? (
                <p className="small text-muted mb-3">Loading…</p>
            ) : visibleItems.length === 0 ? (
                emptyText.length > 0 && <p className="small text-muted mb-3">{emptyText}</p>
            ) : (
                <div className="d-flex flex-wrap gap-2 mb-3">
                    {visibleItems.map((item) => {
                        const isPending = isPendingStatus(statusOf(item));
                        const tooltip = tooltipFor(item);

                        return (
                            <span
                                key={item.id ?? item.value}
                                className={`btn ${chipCssClass} g2h-association-chip ${isPending ? 'g2h-association-chip-pending' : ''} mb-0`}
                                title={tooltip.length > 0 ? tooltip : undefined}>
                                {renderChipLabel(item)}
                                {renderChipActions(item)}
                            </span>
                        );
                    })}
                </div>
            )}

            {(mayAdd || showLoginPrompt) && suggestTitle.length > 0 && (
                <p className="small text-uppercase fw-bold mb-1">{suggestTitle}</p>
            )}

            {(mayAdd || showLoginPrompt) && suggestDescription.length > 0 && (
                <p className="small mb-2">{suggestDescription}</p>
            )}

            {mayAdd && (
                <div className="d-flex gap-2 mb-2">
                    <input
                        className="form-control"
                        type="text"
                        placeholder={addPlaceholderText}
                        maxLength={addMaxLength}
                        value={draft}
                        onChange={(event) => setDraft(event.target.value)}
                        onKeyDown={onKeyDown}
                        aria-label={suggestTitle.length > 0 ? suggestTitle : `Add to ${title}`} />

                    <button
                        type="button"
                        className="btn btn-primary mb-0 text-nowrap"
                        onClick={commitDraft}>
                        {addButtonText}
                    </button>
                </div>
            )}

            {showLoginPrompt && (
                loginButtonOnClick != null ? (
                    <button
                        type="button"
                        className={`btn btn-sm ${loginButtonCssClass} mb-2`}
                        onClick={loginButtonOnClick}>
                        <i className="bi bi-box-arrow-in-right me-1"></i>{loginButtonText}
                    </button>
                ) : (
                    <Link to={resolvedLoginHref} className={`btn btn-sm ${loginButtonCssClass} mb-2`}>
                        <i className="bi bi-box-arrow-in-right me-1"></i>{loginButtonText}
                    </Link>
                )
            )}
        </section>
    );
}
