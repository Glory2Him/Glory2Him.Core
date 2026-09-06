import { ReactElement, ReactNode, useEffect, useId, useState } from 'react';
import { Avatar } from '../coreUI/avatar';
import { ConfirmDialog } from '../coreUI/confirmDialog';
import { useDismissableMenu } from '../../hooks/useDismissableMenu';
import { useAuth } from '../securitys/authProvider';
import {
    ApprovalDecision,
    ApprovalReviewItem,
    ApprovalStatus,
    ApprovalVerdictItem,
    ReviewerCandidateItem
} from '../../models/components/approvals/approvalReviewItem';
import './approvals.css';

// An approval round, rendered: who has reviewed, the viewer's own vote, why approval is blocked,
// and the publisher-tier decision controls (design §8.5–§8.7, §12.5.3, §16.7.2).
//
// SECURITY POSTURE. Every gate below decides what to RENDER and nothing more. The approval
// orchestration re-decides votes, decisions and bypass against the stored row (§14.6): a hidden
// control is a courtesy to the reader, never an authorization boundary. Wherever the server has
// already answered a question per caller — canApprove, isBypassAllowedForCurrentUser — the
// verdict's answer is used verbatim rather than re-derived from role names.
//
// FRESHNESS CONTRACT. This is a pure presentation component: props in, events out, no fetching.
// The CONSUMER owns freshness — it must re-fetch the review collection and the verdict, and
// re-render this panel, whenever a review is cast elsewhere, a comment is added or resolved, or
// the approval is decided (including auto-approval). SignalR, polling or a refetch after each
// event callback are all the consumer's choice; without one of them this panel simply shows the
// world as of the last props it was handed.
//
// THEMING. Styling is expressed as CSS CLASSES rather than colours, so every control follows the
// light/dark theme. Pass btn-success, btn-danger or any theme class — never a literal colour.
export interface ReviewPanelProps {
    // ── Subject ───────────────────────────────────────────────────────────────
    // Names the entity under approval so the vote-tier roles can be composed (§18.6,
    // capability-last and plural): {entityType}-Reviewers / {entityType}-Publishers, and — only
    // when entityType is 'ContentItem' and a contentType is given — the narrower
    // ContentItem-{contentType}-Reviewers / -Publishers pair. Identifiers are NOT used to fetch
    // anything here; the consumer resolves reviews and verdict itself.
    entityType: string;
    contentType?: string;

    // The entity owner's account id. The owner never votes on their own submission, so their
    // placeholder row and vote dropdown are suppressed — the server refuses it regardless.
    entityOwnerId?: string;

    // The approval's current status. Deliberately its own prop rather than read off the verdict:
    // the verdict endpoint admits only the moderation tier (§16.7.2), so a read-only viewer has
    // a status to show and no verdict to show it from. While it is not Submitted the panel is
    // frozen — no vote dropdown, no bypass, no decision controls.
    approvalStatus: ApprovalStatus;

    // ── Presentation ──────────────────────────────────────────────────────────
    showBorder?: boolean;
    cssClass?: string;
    titleText?: string;
    outcomeTitleText?: string;
    resetApprovalText?: string;
    resetConfirmTitleText?: string;
    resetConfirmMessageText?: string;
    resetConfirmButtonText?: string;
    resetCancelButtonText?: string;
    isLoading?: boolean;

    // ── Reviews ───────────────────────────────────────────────────────────────
    // Every recorded review. The viewer's own row (matched by reviewerUserId, never by name) is
    // pulled to the top; the rest render alphabetically. An eligible viewer with no row gets a
    // synthesized "Vote…" placeholder — an uncast vote has no ApprovalReview row to project.
    approvalReviewCollection?: ReadonlyArray<ApprovalReviewItem>;

    // The approval's pending ApprovalReviewRequest rows (design §7.9). They share ONE
    // alphabetical list with the cast votes, wearing a "Requested" chip where a vote would be —
    // a reader asks "where does this round stand?" per person, so a name keeps its place whether
    // or not the answer has arrived. The signed-in viewer is excluded: their own row already
    // renders the vote dropdown, and one person must not appear twice.
    requestedReviewerCollection?: ReadonlyArray<ReviewerCandidateItem>;

    // ── Review requests ───────────────────────────────────────────────────────
    // The cog beside the title opens a picker over these candidates — the §16.7.4 candidates
    // read, fetched by the CONSUMER (never here) when onReviewerLookupRequested fires. The
    // picker filters the supplied list client-side by name; it never searches the server.
    reviewerCandidateCollection?: ReadonlyArray<ReviewerCandidateItem>;

    // People worth asking first, rendered above everyone else with the CONSUMER's reason on each
    // ("Recently reviewed this type"). The panel does no ranking of its own: who is a good
    // reviewer for this item depends on history the panel cannot see, and inventing an order
    // would quietly become policy.
    suggestedReviewerCollection?: ReadonlyArray<ReviewerCandidateItem>;

    // ── The AI reviewer (design §8.6.2, "Berean") ─────────────────────────────
    // Supplying this candidate is the WHOLE of what offers the AI reviewer: it renders as the
    // first row of Suggestions, ahead of every human one, and a panel handed nothing offers
    // nothing. That is §8.6.2's fail-closed posture expressed as the absence of a prop — the
    // consumer resolves IsAIReviewerOffered and passes the candidate only when the setting says
    // yes, exactly as it resolves everything else the panel renders.
    //
    // The panel knows nothing of the second switch. IsAIAllowedToVote decides whether Berean
    // casts an ApprovalReview alongside its comment, which happens on the round rather than in
    // this picker — a Berean that may be asked but may not vote is offered here identically.
    //
    // The panel applies NO policy of its own here, in keeping with everything else it does: it
    // does not know what an AI reviewer is for, only that this one is pinned to the top and
    // wears a glyph instead of initials.
    aiReviewerCandidate?: ReviewerCandidateItem;

    // THE SEAM the AI review pipeline is plumbed into. Fired INSTEAD OF onReviewRequested when
    // the AI reviewer is picked, never alongside it — the two are different operations and the
    // consumer must not post an AI assignment to the human review-request endpoint by accident.
    //
    // What a consumer does with it is §8.6.2's business and none of the panel's: the design has
    // it publish an assignment fact that an AI-review process consumes, calls the classification
    // library from, and files a comment and/or a review under Berean's system identity. None of
    // that exists yet (§13.4: "no AI broker or content-analysis service exists in code today"),
    // which is why this is a callback and not a fetch.
    onAIReviewerRequested?: (candidate: ReviewerCandidateItem) => void;

    // How many people may be waiting on at once. Counted on OUTSTANDING invitations, so an
    // answered request frees its slot.
    maxReviewerRequests?: number;

    isCandidatesLoading?: boolean;

    // Fired when the picker opens, so a consumer can fetch (or refresh) the candidate list
    // lazily instead of paying the lookup on every render of the panel.
    onReviewerLookupRequested?: () => void;

    // Fired when a candidate is picked. The consumer POSTs the review request and refreshes the
    // requested collection — the server dissolves a duplicate quietly (§7.9 rule 4), so the
    // consumer needs no existence check of its own.
    onReviewRequested?: (candidate: ReviewerCandidateItem) => void;

    // Fired from the picker's Requested section, which is the ONLY route to unassigning
    // somebody — the main list carries no withdraw control. Pending requests only: once the
    // invitation has been answered the server refuses the withdrawal (§7.9 rule 5), and a
    // person who has voted renders ticked and inert in the picker rather than clickable.
    onReviewRequestWithdrawn?: (candidate: ReviewerCandidateItem) => void;

    // Fired when the viewer casts or changes their vote (Approved or Rejected only — "Vote…" is
    // a placeholder, not a castable state). The consumer persists it via the ApprovalReviews API
    // and then refreshes the collection AND the verdict, which the new vote may have changed.
    onReviewStatusChanged?: (vote: ApprovalStatus) => void;

    // ── Outcome ───────────────────────────────────────────────────────────────
    // The per-caller verdict from GET api/Approvals/{entityType}/{entityId}/Verdict. Absent for
    // viewers outside the moderation tier — the outcome section then shows the status pill
    // alone, which is §14.7 posture D: a verdict names resolved policy and is never public.
    approvalVerdict?: ApprovalVerdictItem;

    // Fired when a decision is submitted. isBypassRequested is true only for an approve over
    // unmet conditions with the checkbox ticked; rejection never records a bypass — it withholds
    // approval rather than granting it (§12.5.3 rule 13). Maps 1:1 onto
    // POST api/Approvals/{entityType}/{entityId}/Decision.
    onApprovalStatusChanged?: (
        decision: ApprovalDecision,
        isBypassRequested: boolean,
        bypassReason: string) => void;

    // §8.6 HR-4's administrator override: puts a DECIDED round back to Submitted and dismisses
    // its reviews, so an outcome applied by accident can be taken back without starting a new
    // round. Maps 1:1 onto POST api/Approvals/{entityType}/{entityId}/Reset.
    //
    // The control renders only on a decided round, and only for an administrator — deciding an
    // open round is the publisher tier's, undeciding a closed one is the override. Rendering is
    // all that decides here: the server re-decides against the stored row, as everywhere.
    onApprovalReset?: () => void;

    // ── Roles ─────────────────────────────────────────────────────────────────
    // Comma-separated overrides. Defaults are composed from entityType/contentType per §18.6;
    // pass these only when a surface needs a different render gate. decisionRoles deliberately
    // excludes the Reviewers tier: HR-3 — a reviewer may never set an ApprovalStatus.
    voteRoles?: string;
    decisionRoles?: string;

    // ── Text ──────────────────────────────────────────────────────────────────
    votePlaceholderText?: string;
    approvedText?: string;
    rejectedText?: string;

    approveVoteDescription?: string;
    rejectVoteDescription?: string;
    blockedTitleText?: string;
    awaitingApprovalText?: string;
    approvedStatusText?: string;
    rejectedStatusText?: string;
    dismissedStatusText?: string;
    requestedVoteText?: string;
    requestedVoteCssClass?: string;

    // Sits where a username sits, because that is what the AI reviewer has instead of one.
    aiReviewerTaglineText?: string;
    aiReviewerIconCssClass?: string;
    pickerTitleText?: string;
    suggestionsSectionText?: string;
    requestedSectionText?: string;
    everyoneElseSectionText?: string;
    requestCapReachedText?: string;
    requestReviewTooltip?: string;
    candidateFilterPlaceholderText?: string;
    noCandidatesText?: string;
    withdrawRequestTooltip?: string;
    bypassLabelText?: string;
    bypassReasonPlaceholderText?: string;

    // Shown under the reason box, and only while that empty box is the one thing holding Submit
    // shut. A disabled button explains nothing on its own.
    bypassReasonRequiredText?: string;

    setStatusText?: string;
    approveOptionText?: string;
    approveOptionDescription?: string;
    rejectOptionText?: string;
    rejectOptionDescription?: string;
    submitButtonText?: string;
    emptyText?: string;

    // ── Theme classes ─────────────────────────────────────────────────────────
    approvedVoteCssClass?: string;
    rejectedVoteCssClass?: string;

    uncastVoteCssClass?: string;
    awaitingPillCssClass?: string;
    approvedPillCssClass?: string;
    rejectedPillCssClass?: string;
    dismissedPillCssClass?: string;
    blockedIconCssClass?: string;
    bypassCssClass?: string;
    setStatusCssClass?: string;
    approveSelectionCssClass?: string;
    rejectSelectionCssClass?: string;
}

const parseRoles = (roles: string): ReadonlyArray<string> =>
    roles
        .split(',')
        .map((role) => role.trim())
        .filter((role) => role.length > 0);

// One name for one tier. "Administrators" used to be the portal's own vocabulary sitting beside
// a separate core "Admin", so both had to be listed here; #368 collapsed them (SeedData).
const AdministratorRoles = 'Administrators';

export function ReviewPanel({
    entityType,
    contentType,
    entityOwnerId,
    approvalStatus,
    showBorder = false,
    cssClass = '',
    titleText = 'Approval Reviews',
    outcomeTitleText = 'Review Outcome',
    resetApprovalText = 'Reset approval',
    resetConfirmTitleText = 'Reset this approval?',

    resetConfirmMessageText =
        'This item will go back to Submitted for review and every recorded review will be '
        + 'dismissed. It will be removed from the public site until it is approved again.',

    resetConfirmButtonText = 'Reset approval',
    resetCancelButtonText = 'Cancel',
    isLoading = false,
    approvalReviewCollection = [],
    requestedReviewerCollection = [],
    reviewerCandidateCollection = [],
    suggestedReviewerCollection = [],
    aiReviewerCandidate,
    maxReviewerRequests = 15,
    isCandidatesLoading = false,
    onReviewerLookupRequested,
    onReviewRequested,
    onAIReviewerRequested,
    onReviewRequestWithdrawn,
    onReviewStatusChanged,
    onApprovalReset,
    approvalVerdict,
    onApprovalStatusChanged,
    voteRoles,
    decisionRoles,
    votePlaceholderText = 'Vote...',
    approvedText = 'Approved',
    rejectedText = 'Rejected',

    approveVoteDescription = 'I am happy with this item',
    rejectVoteDescription = 'I do not think we should approve this item',
    blockedTitleText = 'Approval is blocked',
    awaitingApprovalText = 'Awaiting approval',
    approvedStatusText = 'Approved',
    rejectedStatusText = 'Rejected',
    dismissedStatusText = 'Dismissed',
    requestedVoteText = 'Requested',
    requestedVoteCssClass = 'btn-warning',
    aiReviewerTaglineText = 'Your AI Pair Reviewer',
    aiReviewerIconCssClass = 'bi-person-fill',
    pickerTitleText = 'Request up to {max} reviewers',
    suggestionsSectionText = 'Suggestions',
    requestedSectionText = 'Requested',
    everyoneElseSectionText = 'Everyone else',
    requestCapReachedText = 'Request limit reached. Withdraw one to ask somebody else.',
    requestReviewTooltip = 'Request a review',
    candidateFilterPlaceholderText = 'Filter by name',
    noCandidatesText = 'No eligible reviewers found.',
    withdrawRequestTooltip = 'Withdraw review request',
    bypassLabelText = 'Approve without waiting for requirements to be met (bypass rules)',
    bypassReasonPlaceholderText = 'Reason for bypassing the approval requirements',
    bypassReasonRequiredText = 'Give a reason for the bypass before submitting.',
    setStatusText = 'Set approval status',
    approveOptionText = 'Approve this item',
    approveOptionDescription = 'Approve this item based on the Reviewer votes',
    rejectOptionText = 'Reject this item',
    rejectOptionDescription = 'Reject this item based on the Reviewer votes',
    submitButtonText = 'Submit',
    emptyText = '',
    approvedVoteCssClass = 'btn-success',
    rejectedVoteCssClass = 'btn-danger',

    uncastVoteCssClass = 'btn-secondary',
    awaitingPillCssClass = 'btn-dark',
    approvedPillCssClass = 'btn-success',
    rejectedPillCssClass = 'btn-danger',
    dismissedPillCssClass = 'btn-secondary',
    blockedIconCssClass = 'bi-exclamation-circle-fill text-danger',
    bypassCssClass = 'text-danger',
    setStatusCssClass = 'g2h-review-status-select',
    approveSelectionCssClass = 'btn-success',
    rejectSelectionCssClass = 'btn-danger'
}: ReviewPanelProps) {
    const { isAuthenticated, user, userRoles } = useAuth();
    const headingId = useId();
    const outcomeHeadingId = useId();
    const bypassReasonMessageId = useId();

    // All three menus are ours rather than Bootstrap's, so dismissal, labelling and focus come
    // from one shared hook instead of being written out three times — see useDismissableMenu for
    // why adopting `data-bs-toggle` here was rejected.
    const voteMenu = useDismissableMenu({ initialFocus: 'container' });
    const picker = useDismissableMenu();
    const decisionMenu = useDismissableMenu({ initialFocus: 'container' });

    const [candidateFilter, setCandidateFilter] = useState('');
    const [selectedDecision, setSelectedDecision] = useState<ApprovalDecision | undefined>();
    const [isBypassChecked, setIsBypassChecked] = useState(false);
    const [bypassReason, setBypassReason] = useState('');

    // Only an APPROVED round asks before resetting. Resetting one takes a live item off the
    // public site, which is worth a second's pause; a rejected item is not public, so there is
    // nothing to warn about and a dialog would only be in the way.
    const [isConfirmingReset, setIsConfirmingReset] = useState(false);

    // §18.6 composition, capability LAST and plural — ContentItem-Blog-Reviewers, never
    // Reviewers-ContentItem-Blog: the services recognise a review role by its "-Reviewers"
    // suffix. The content-type tier exists only for ContentItem (§18.6 rule 5).
    const contentTypedRole = (capability: string): string | undefined =>
        entityType === 'ContentItem' && contentType != null && contentType.length > 0
            ? `${entityType}-${contentType}-${capability}`
            : undefined;

    const defaultVoteRoles = [
        'Reviewers', 'Publishers', ...parseRoles(AdministratorRoles),
        `${entityType}-Reviewers`, `${entityType}-Publishers`,
        contentTypedRole('Reviewers'), contentTypedRole('Publishers')
    ].filter((role): role is string => role != null);

    // HR-3: the Reviewers tier may never set an ApprovalStatus — deciding is the Publishers
    // tier's and Administrators' alone, so no -Reviewers role appears here.
    const defaultDecisionRoles = [
        'Publishers', ...parseRoles(AdministratorRoles),
        `${entityType}-Publishers`,
        contentTypedRole('Publishers')
    ].filter((role): role is string => role != null);

    const voteRoleList = voteRoles != null ? parseRoles(voteRoles) : defaultVoteRoles;
    const decisionRoleList = decisionRoles != null ? parseRoles(decisionRoles) : defaultDecisionRoles;

    const holdsAnyRole = (roles: ReadonlyArray<string>): boolean =>
        roles.some((role) => userRoles.includes(role));

    const viewerId = user?.userId ?? '';
    const isOwner = viewerId.length > 0 && viewerId === entityOwnerId;

    // WHO BEREAN IS, asked in one place. The aiReviewerCandidate prop is the single source of
    // that answer, so a row anywhere in the panel — a pending request, a cast review, a picker
    // entry — is recognised the same way and cannot be styled as the AI reviewer in one list
    // and as a person in another.
    const isAIReviewer = (userId: string): boolean =>
        aiReviewerCandidate != null
            && userId.length > 0
            && userId === aiReviewerCandidate.userId;
    const isSubmitted = approvalStatus === ApprovalStatus.Submitted;

    // The round reached an outcome. Both outcomes are resettable: a rejection is as easy to
    // apply by accident as an approval, and the operation takes either back.
    const isDecided =
        approvalStatus === ApprovalStatus.Approved
            || approvalStatus === ApprovalStatus.Rejected;

    // ADMINISTRATORS ALONE, and not the decision tier: a publisher decides an open round, and
    // undeciding a closed one is the override (§8.6 HR-4). Asked against the administrator role
    // directly rather than through decisionRoleList, which admits publishers.
    const mayResetApproval =
        isAuthenticated
            && isDecided
            && onApprovalReset != null
            && holdsAnyRole(parseRoles(AdministratorRoles));

    const mayVote =
        isAuthenticated
        && isOwner === false
        && isSubmitted
        && holdsAnyRole(voteRoleList);

    const mayDecide =
        isAuthenticated
        && isSubmitted
        && holdsAnyRole(decisionRoleList);

    // Requesting a review is coordination, not decision, so it is open to the whole tier above
    // the read-only view — reviewers included (§7.9 rule 2) — and, unlike voting, the entity's
    // owner is not excluded: soliciting review of your own submission is not passing verdict
    // on it.
    const mayRequest =
        isAuthenticated
        && isSubmitted
        && holdsAnyRole(voteRoleList);

    // WHAT THIS PANEL IS ABOUT: the round as it stands. Only three states are in scope —
    // approved, rejected, and pending (someone asked who has not answered). Two kinds of row
    // are therefore excluded outright rather than styled:
    //
    //   Dismissed — dismissal is what HAPPENS to a verdict when the content it judged changes
    //   (§9.5). The row is retained as evidence that somebody once ruled on superseded text; it
    //   is not a standing opinion on what is being reviewed now, and showing it would invite a
    //   publisher to count it.
    //
    //   Soft-deleted — a withdrawn review keeps its row, and a withdrawn opinion is no opinion.
    //
    // Filtering here rather than at each use site is deliberate: every derived list below reads
    // from this one, so nothing downstream can accidentally reintroduce a row that is out of
    // scope. A consumer whose projection already filters can leave isDeleted unset.
    const visibleReviews = approvalReviewCollection.filter(
        (item) => item.isDeleted !== true && item.vote !== ApprovalStatus.Dismissed);

    const viewerReview = visibleReviews.find(
        (item) => item.reviewerUserId.length > 0 && item.reviewerUserId === viewerId);

    // A vote SUPERSEDES an outstanding request: once somebody answers, the invitation is spent
    // (§7.9 rule 6 retires it server-side), and rendering both would show one person twice.
    const pendingRequests = requestedReviewerCollection.filter(
        (candidate) => candidate.userId !== viewerId
            && visibleReviews.every((item) => item.reviewerUserId !== candidate.userId));

    // Votes and outstanding requests share ONE alphabetical list rather than sitting in separate
    // blocks. A reader is asking "where does this round stand?", and that question is answered
    // per person — so a name keeps its place whether or not the answer has arrived yet.
    const otherRows = visibleReviews
        .filter((item) => item.reviewerUserId !== viewerId)
        .map((item) => ({
            key: item.id ?? item.reviewerUserId,
            userId: item.reviewerUserId,
            displayName: item.reviewerDisplayName,
            userName: item.reviewerUserName,
            vote: item.vote as ApprovalStatus | undefined,
        }))
        .concat(pendingRequests.map((candidate) => ({
            key: `requested-${candidate.userId}`,
            userId: candidate.userId,
            displayName: candidate.displayName,
            userName: candidate.userName,
            vote: undefined as ApprovalStatus | undefined,
        })))
        .sort((left, right) => left.displayName.localeCompare(
            right.displayName, undefined, { sensitivity: 'base' }));

    // The viewer's row leads even before they vote: an eligible reviewer with no ApprovalReview
    // row yet gets a synthesized placeholder, because "you have not voted" is the one state the
    // stored collection cannot carry.
    const showPlaceholderRow = viewerReview == null && mayVote;

    // The verdict is a moving target — another vote, a new comment, a resolved thread all change
    // it. A bypass tick given against ONE set of reasons must not silently carry over to the
    // next, so any content change resets the checkbox, its reason, and a pending selection.
    //
    // approvalId LEADS it, and it is the field that matters most. Two different approvals blocked
    // for the same reasons produce identical signatures for every other field — the common case,
    // not a corner one — so without the id a consumer that swaps this panel from one item to the
    // next without remounting keeps the previous item's bypass tick, its typed justification and
    // its pending decision, over a repainted panel showing the new item. Submitting then writes
    // one item's justification onto another's permanent record, and the server cannot catch it
    // because a bypass reason is free text it only checks for being non-blank.
    // The reasons are taken as the publisher READ them, message and all, not just their codes. A
    // code can stay put while what it says changes — "at least 3 approving review(s)" becomes
    // "at least 2" as votes land — and consent given against the old sentence is not consent to
    // the new one.
    const verdictSignature = approvalVerdict == null
        ? ''
        : `${approvalVerdict.approvalId}|`
            + approvalVerdict.blockReasons
                .map((reason) => `${reason.code}:${reason.message}`)
                .join('|')
            + `|${approvalVerdict.canApprove}|${approvalVerdict.isBypassAllowedForCurrentUser}`;

    useEffect(() => {
        setIsBypassChecked(false);
        setBypassReason('');
        setSelectedDecision(undefined);
    }, [verdictSignature]);

    // A ROUND IS OPEN WHILE THE ENTITY IS DRAFT OR SUBMITTED. Draft belongs here, and this
    // used to admit Submitted alone — which threw away the one reason §16.7.2 added
    // BlockedDueToDraftStatus to carry: "This item has not been submitted for review yet.
    // Submit it to start the approval process." Core composes that reason first and alone for a
    // draft, precisely so a UI can state it and send somebody to advance the item; the panel
    // then dropped it and showed a bare "Awaiting approval" pill instead.
    //
    // What the guard is actually FOR is a settled round: a consumer refreshing after a decision
    // can hand over a terminal status with a verdict fetched a moment earlier, and painting
    // block reasons over that states an outcome already overtaken. So it names the states in
    // which a round is still open, and Approved, Rejected and Dismissed are what it excludes.
    const isRoundOpen =
        approvalStatus === ApprovalStatus.Draft
        || approvalStatus === ApprovalStatus.Submitted;

    const isBlocked = isRoundOpen && approvalVerdict?.isBlocked === true;

    // BYPASS IS SUBMITTED-ONLY, said outright rather than inherited. It was already shut on a
    // draft, but only through mayDecide happening to require isSubmitted — a guard that holds
    // by accident of another one is a guard nobody can find, and this is a rule in its own
    // right: a bypass waives the CONDITIONS of a round (§9.7.5), and a draft has no round to
    // waive. Nothing rescues an item nobody has offered; it has to be submitted first.
    const showBypassCheckbox =
        mayDecide
        && isSubmitted
        && isBlocked
        && approvalVerdict?.isBypassAllowedForCurrentUser === true;

    // The whole of the approve rule, per the verdict (§16.7.2): canApprove already folds the
    // conditions AND this caller's standing (HR-2 owners, carrying reviewers). A checked bypass
    // waives the conditions wholesale (§9.7.5). Reject is NOT gated by any of this — a direct
    // reject needs no conditions and no bypass (§12.5.3 rule 13).
    const mayApproveNow =
        approvalVerdict != null
        && (approvalVerdict.canApprove || (isBlocked && showBypassCheckbox && isBypassChecked));

    const isBypassApprove =
        selectedDecision === ApprovalDecision.Approve
        && isBlocked
        && isBypassChecked;

    // An unexplained bypass is refused by the server before any policy is read, so Submit holds
    // until the reason exists rather than letting the click round-trip into a 400.
    const isBypassReasonMissing = isBypassApprove && bypassReason.trim().length === 0;

    const maySubmitDecision =
        selectedDecision != null
        && (selectedDecision === ApprovalDecision.Reject || mayApproveNow)
        && isBypassReasonMissing === false;

    const onBypassToggled = (checked: boolean) => {
        setIsBypassChecked(checked);

        if (checked === false) {
            setBypassReason('');

            // A selection made under the bypass must not outlive it: with the tick gone an
            // Approve the caller cannot perform is no longer a selection worth submitting.
            if (selectedDecision === ApprovalDecision.Approve
                && approvalVerdict?.canApprove !== true) {
                setSelectedDecision(undefined);
            }
        }
    };

    const castVote = (vote: ApprovalStatus) => {
        // Focus goes back to the vote button: the choice is made, and the button now shows it.
        voteMenu.close();

        if (vote !== viewerReview?.vote) {
            onReviewStatusChanged?.(vote);
        }
    };

    const togglePicker = () => {
        const opening = picker.isOpen === false;

        picker.toggle();
        setCandidateFilter('');

        if (opening) {
            onReviewerLookupRequested?.();
        }
    };

    // Requesting somebody. The picker STAYS OPEN: assigning several reviewers is one task, and
    // closing after each pick would make the common case four round trips through the cog.
    //
    // THE AI REVIEWER LEAVES BY ITS OWN DOOR. Berean is not a person with an account behind the
    // review-request endpoints, and routing it through onReviewRequested would hand the consumer
    // an assignment it can only post to a surface that will refuse it. The two callbacks are
    // exclusive: whichever one fires, the other does not.
    const requestReview = (candidate: ReviewerCandidateItem) => {
        if (isAIReviewer(candidate.userId)) {
            onAIReviewerRequested?.(candidate);

            return;
        }

        onReviewRequested?.(candidate);
    };

    const withdrawRequest = (candidate: ReviewerCandidateItem) => {
        onReviewRequestWithdrawn?.(candidate);
    };

    const matchesFilter = (candidate: ReviewerCandidateItem): boolean => {
        const filter = candidateFilter.trim().toLowerCase();

        return filter.length === 0
            || candidate.displayName.toLowerCase().includes(filter)
            || (candidate.userName ?? '').toLowerCase().includes(filter);
    };

    const requestedUserIds = new Set(
        requestedReviewerCollection.map((candidate) => candidate.userId));

    const votedUserIds = new Set(visibleReviews.map((item) => item.reviewerUserId));

    // The picker's three sections. NOBODY is filtered out of it — a person already assigned
    // stays listed, ticked, so that searching for them finds them and answers "why is this
    // person not here?" before it is asked. What differs between the sections is what a click
    // MEANS, and whether there is one at all.
    const suggestedUserIds = new Set(
        [...suggestedReviewerCollection, ...(aiReviewerCandidate == null
            ? []
            : [aiReviewerCandidate])].map((candidate) => candidate.userId));

    // REQUESTED WINS THE TIE, and it used to be Suggestions. A person handed to both
    // collections — which the natural consumer does, since it ranks its suggestions out of the
    // same candidates read — was shown once under Suggestions, where a click MEANS request. The
    // Requested section is the ONLY route to unassigning somebody (the main list carries no
    // withdraw control, by design), so being suggested made a standing invitation impossible to
    // withdraw, and the row invited a second request instead. Pinning the AI reviewer into
    // suggestedUserIds widened that from "a consumer that ranks carelessly" to "every round
    // where Berean has been asked".
    //
    // So the sections split on what a click can DO rather than on what is worth saying about
    // the person: an outstanding invitation is withdrawable and nothing else, so it belongs
    // under Requested wherever else it was offered from. The one thing lost is the suggestion
    // reason on that row, which is a sentence about why to ask somebody already asked.
    const requestedPickerRows = requestedReviewerCollection.filter(matchesFilter);

    // BEREAN LEADS THE SUGGESTIONS, ahead of every human one (§8.6.2), mirroring GitHub's
    // Copilot-reviewer suggestion. It sits INSIDE the Suggestions band rather than in a section
    // of its own: it is one more name worth asking first, and a band holding a single row would
    // read as a separate kind of thing to do.
    //
    // It answers the filter box like any other row — a picker where typing a name leaves one
    // entry stubbornly pinned at the top reads as a bug — and it is subject to the same tick and
    // cap rules, so a Berean that has already reviewed renders inert exactly as a person would.
    // Berean included, an outstanding invitation drops out of here and renders under Requested.
    const suggestionRows = (aiReviewerCandidate == null
        ? suggestedReviewerCollection
        : [aiReviewerCandidate, ...suggestedReviewerCollection.filter(
            (candidate) => candidate.userId !== aiReviewerCandidate.userId)])
        .filter((candidate) => matchesFilter(candidate)
            && requestedUserIds.has(candidate.userId) === false);

    // Everyone else, with the already-voted at the top so the assigned reader sees them first.
    // The two groups keep the order the consumer supplied within themselves; only the split is
    // the panel's doing.
    //
    // "Everyone ELSE" is meant literally: a person already shown under Suggestions or Requested
    // does not appear again here. The natural consumer ranks its suggestions out of the same
    // candidates read, so without this the same name renders twice in one open picker.
    const everyoneElseRows = reviewerCandidateCollection
        .filter((candidate) =>
            matchesFilter(candidate)
                && requestedUserIds.has(candidate.userId) === false
                && suggestedUserIds.has(candidate.userId) === false)
        .slice()
        .sort((left, right) => {
            const leftVoted = votedUserIds.has(left.userId) ? 0 : 1;
            const rightVoted = votedUserIds.has(right.userId) ? 0 : 1;

            return leftVoted - rightVoted;
        });

    // "Request up to N reviewers". Counted on OUTSTANDING invitations rather than on everybody
    // who has ever been asked, because a request that has been answered is no longer occupying a
    // slot — the cap limits how many people are being waited on at once.
    //
    // So it counts the same set the main list treats as outstanding, not the raw collection. A
    // request whose target has since voted is normally retired server-side (§7.9 rule 6), but
    // when that retirement fails or the panel is a few seconds stale it lingers — and counting
    // it would refuse new invitations on behalf of somebody who has already answered, with
    // nothing on screen saying why.
    const outstandingRequests = requestedReviewerCollection.filter(
        (candidate) => votedUserIds.has(candidate.userId) === false);

    const isAtRequestCap = outstandingRequests.length >= maxReviewerRequests;
    const chooseDecision = (decision: ApprovalDecision) => {
        // Back to the trigger, which now reads as the chosen decision — and which sits directly
        // above the Submit button the user is heading for next.
        decisionMenu.close();
        setSelectedDecision(decision);
    };

    const submitDecision = () => {
        if (selectedDecision == null) {
            return;
        }

        onApprovalStatusChanged?.(
            selectedDecision,
            isBypassApprove,
            isBypassApprove ? bypassReason.trim() : '');
    };

    // Only two verdicts ever reach a badge. Dismissed and soft-deleted rows were removed from
    // visibleReviews above rather than styled here, because they are not opinions on the round
    // as it stands — see the note there.
    const voteBadgeCssClass = (vote: ApprovalStatus): string =>
        vote === ApprovalStatus.Approved ? approvedVoteCssClass : rejectedVoteCssClass;

    const voteBadgeText = (vote: ApprovalStatus): string =>
        vote === ApprovalStatus.Approved ? approvedText : rejectedText;

    const requestReset = (): void => {
        if (approvalStatus === ApprovalStatus.Approved) {
            setIsConfirmingReset(true);

            return;
        }

        onApprovalReset?.();
    };

    const confirmReset = (): void => {
        setIsConfirmingReset(false);
        onApprovalReset?.();
    };

    const statusPill = (): { text: string; pillCssClass: string; iconCssClass: string } => {
        if (approvalStatus === ApprovalStatus.Approved) {
            return {
                text: approvedStatusText,
                pillCssClass: approvedPillCssClass,
                iconCssClass: 'bi-check-circle-fill'
            };
        }

        if (approvalStatus === ApprovalStatus.Rejected) {
            return {
                text: rejectedStatusText,
                pillCssClass: rejectedPillCssClass,
                iconCssClass: 'bi-slash-circle'
            };
        }

        if (approvalStatus === ApprovalStatus.Dismissed) {
            return {
                text: dismissedStatusText,
                pillCssClass: dismissedPillCssClass,
                iconCssClass: 'bi-dash-circle'
            };
        }

        // Draft and Submitted both read as waiting: a draft has not opened the round yet and a
        // submitted one has not closed it.
        return {
            text: awaitingApprovalText,
            pillCssClass: awaitingPillCssClass,
            iconCssClass: 'bi-circle-fill text-warning'
        };
    };

    const renderViewerVoteControl = (): ReactNode => {
        const currentVote = viewerReview?.vote;

        const buttonCssClass = currentVote == null
            ? uncastVoteCssClass
            : voteBadgeCssClass(currentVote);

        const buttonText = currentVote == null
            ? votePlaceholderText
            : voteBadgeText(currentVote);

        if (mayVote === false) {
            // A cast vote stays visible after the round closes or the roles change; it is simply
            // no longer a control.
            return currentVote == null
                ? null
                : (
                    <span className={`btn btn-sm w-100 ${buttonCssClass} g2h-review-vote-badge mb-0`}>
                        {buttonText}
                    </span>
                );
        }

        return (
            <div className="dropdown" ref={voteMenu.containerRef}>
                <button
                    type="button"
                    id={voteMenu.triggerId}
                    ref={voteMenu.triggerRef}
                    className={
                        'btn btn-sm dropdown-toggle w-100 d-flex justify-content-between '
                        + `align-items-center ${buttonCssClass} mb-0`}

                    // NO aria-haspopup, on any of the three, and that is a decision rather than
                    // an omission — see useDismissableMenu for why. These are disclosures:
                    // aria-expanded and aria-controls are true of them, and nothing more is.
                    aria-controls={voteMenu.isOpen ? voteMenu.menuId : undefined}
                    aria-expanded={voteMenu.isOpen}
                    onClick={voteMenu.toggle}>
                    {buttonText}
                </button>

                {voteMenu.isOpen && (
                    <div
                        id={voteMenu.menuId}
                        ref={voteMenu.menuRef}
                        tabIndex={-1}
                        aria-labelledby={voteMenu.triggerId}
                        className="dropdown-menu dropdown-menu-end show shadow w-100">
                        <button
                            type="button"
                            className="dropdown-item"
                            onClick={() => castVote(ApprovalStatus.Approved)}>
                            <span className="fw-bold d-block">
                                {currentVote === ApprovalStatus.Approved && (
                                    <i className="bi bi-check me-1" aria-hidden="true"></i>
                                )}
                                {approvedText}
                            </span>
                            <small className="text-muted">{approveVoteDescription}</small>
                        </button>

                        <button
                            type="button"
                            className="dropdown-item"
                            onClick={() => castVote(ApprovalStatus.Rejected)}>
                            <span className="fw-bold d-block">
                                {currentVote === ApprovalStatus.Rejected && (
                                    <i className="bi bi-check me-1" aria-hidden="true"></i>
                                )}
                                {rejectedText}
                            </span>
                            <small className="text-muted">{rejectVoteDescription}</small>
                        </button>
                    </div>
                )}
            </div>
        );
    };

    // ONE IDENTITY BLOCK, wherever somebody is named — the review list and the picker both.
    // #354 asks for the two surfaces to look the same, and the way to keep them looking the same
    // is for there to be one of them rather than two that agree today.
    //
    // The shape: an avatar spanning both text lines, the DISPLAY NAME over the USERNAME. The
    // name leads because that is what a moderator recognises; the username settles which account
    // it was when two people share a name, which is the whole reason it is on screen at all.
    //
    // The second line is dropped when there is no username rather than padded with a fallback,
    // and dropped again when the username IS the display name — see the note on secondLine.
    //
    // The AI reviewer wears its tagline in that slot, because a tagline is what it has instead
    // of a username, and a glyph instead of initials (see Avatar).
    //
    // Returned as a FRAGMENT, not a wrapper: the avatar and the text sit as siblings in the
    // caller's own flex container, so the picker's row stays a <button> laid out exactly as it
    // was and the review row can be a <div> without either one nesting a block inside phrasing
    // content to get there.
    const renderIdentity = (
        identity: { userId: string; displayName: string; userName?: string },
        avatarSizePx: number,
        extraLine?: string
    ): ReactElement => {
        const isAI = isAIReviewer(identity.userId);
        const suppliedSecondLine = isAI ? aiReviewerTaglineText : identity.userName;

        // NEVER THE SAME NAME TWICE. Where an account has no preferred name and no full name, the
        // server's display-name composition falls back to the username (ComposeDisplayName), so
        // both fields arrive holding it — and a row that printed one person's name stacked under
        // itself says nothing the first line has not already said.
        //
        // Compared case-insensitively and trimmed, because "Tester" over "tester" is that same
        // coincidence rather than two facts. The reads are right to report what the account
        // holds; deciding not to draw it twice belongs to the surface, and this is the surface.
        const secondLine =
            suppliedSecondLine != null
                && suppliedSecondLine.trim().toLowerCase()
                    === identity.displayName.trim().toLowerCase()
                ? undefined
                : suppliedSecondLine;

        return (
            <>
                {/* DECORATIVE here, always: the display name is rendered immediately to its
                    right, so a labelled avatar makes every row announce the person twice — and
                    inside the picker's <button> it lands in the button's accessible name too. */}
                <Avatar
                    name={identity.displayName}
                    sizePx={avatarSizePx}
                    iconCssClass={isAI ? aiReviewerIconCssClass : undefined}
                    isDecorative />

                <span className="text-truncate">
                    <span className="fw-bold d-block g2h-review-identity-name">
                        {identity.displayName}
                    </span>

                    {/* NOT .text-muted — see approvals.css. This theme redefines the token it
                        resolves to, and it paints these two lines at 1.49:1 on white. */}
                    {secondLine != null && secondLine.length > 0 && (
                        <small className="d-block g2h-review-identity-username">
                            {secondLine}
                        </small>
                    )}

                    {extraLine != null && extraLine.length > 0 && (
                        <small className="d-block g2h-review-identity-reason">
                            {extraLine}
                        </small>
                    )}
                </span>
            </>
        );
    };

    // One row of the picker. What a click MEANS is the only thing that differs between the three
    // sections, and each is a rule from §7.9 rather than a UI preference:
    //
    //   suggestion / everyone-with-no-vote -> request them (rule 2: coordination is open to the
    //   whole review tier)
    //
    //   requested -> withdraw (rule 5). This is the ONLY route to unassigning somebody, which is
    //   why the requested section exists as its own group rather than as ticks in the main list.
    //
    //   everyone-who-has-voted -> nothing at all. A cast verdict is theirs (§8.6.1, owner-only),
    //   so there is no "unassign" to offer; the row is rendered ticked and inert rather than
    //   hidden, so that searching for the person finds them and answers "why are they missing?"
    //   before it is asked.
    const renderPickerRow = (
        candidate: ReviewerCandidateItem,
        kind: 'suggestion' | 'requested' | 'everyone'
    ): ReactElement => {
        const hasVoted = votedUserIds.has(candidate.userId);
        const isTicked = kind === 'requested' || hasVoted;
        // A cast verdict makes the row inert WHEREVER it appears, Requested included. A person
        // can be both invited and answered - rule 6 normally retires the invitation, but a
        // failed retirement or a stale panel leaves it standing - and withdrawing an ANSWERED
        // invitation is refused by the server (§7.9 rule 5). Leaving that row clickable offers
        // the one click in this panel that round-trips into an error, which is exactly what
        // gating Submit on the verdict exists to avoid.
        const isInert = hasVoted;

        // The cap stops new invitations, never withdrawals — otherwise reaching the limit would
        // trap the round with no way to free a slot.
        const isBlockedByCap = isAtRequestCap && kind !== 'requested' && hasVoted === false;
        const isDisabled = isInert || isBlockedByCap;

        const onClick = () => {
            if (kind === 'requested') {
                withdrawRequest(candidate);

                return;
            }

            requestReview(candidate);
        };

        // A visually-hidden hint rather than an aria-label. An aria-label REPLACES the accessible
        // name, so the button would announce as "Request a review: Mary" while reading "mary.a
        // Mary Adeyemi" on screen - the mismatch WCAG 2.5.3 exists to stop, and one that breaks
        // voice control. Appending keeps the visible text in the name and still says what a
        // click will do.
        const actionHint = isInert
            ? 'has already reviewed'
            : isBlockedByCap
                ? 'request limit reached'
                : kind === 'requested'
                    ? withdrawRequestTooltip
                    : requestReviewTooltip;

        return (
            <button
                key={`${kind}-${candidate.userId}`}
                type="button"
                className="dropdown-item d-flex align-items-center gap-2 g2h-review-picker-row"
                disabled={isDisabled}
                aria-pressed={isTicked}
                onClick={onClick}>
                <span className="g2h-review-picker-tick" aria-hidden="true">
                    {isTicked && <i className="bi bi-check"></i>}
                </span>

                {renderIdentity(candidate, 28, candidate.suggestionReason)}

                <span className="visually-hidden">{actionHint}</span>
            </button>
        );
    };

    const renderPickerSection = (
        title: string,
        rows: ReadonlyArray<ReviewerCandidateItem>,
        kind: 'suggestion' | 'requested' | 'everyone'
    ): ReactNode => rows.length === 0 ? null : (
        <div className="g2h-review-picker-section">
            <p className="g2h-review-picker-section-title small fw-bold mb-0 px-3 py-1">
                {title}
            </p>

            {rows.map((candidate) => renderPickerRow(candidate, kind))}
        </div>
    );

    // ONE REVIEWER, STACKED: the identity block on top, the vote across the full width beneath
    // it (#354). The vote used to sit at the end of a single line, which put the three things a
    // moderator scans — who, which account, what they said — in a row that a long name pushed
    // the answer off the end of, on the narrow column this panel is designed for.
    //
    // Full width is also what makes the votes COMPARABLE. Down a list of identical bars the eye
    // reads the colours as a tally; sized to their own text, "Approved" and "Requested" are
    // different widths saying different things and the round has to be read one row at a time.
    const renderReviewRow = (
        identity: { userId: string; displayName: string; userName?: string },
        control: ReactNode,
        key: string
    ): ReactElement => (
        <div key={key} className="py-2 border-bottom g2h-review-row">
            <div className="d-flex align-items-center gap-2">
                {renderIdentity(identity, 34)}
            </div>

            {control != null && <div className="mt-2">{control}</div>}
        </div>
    );

    const viewerRow = (): ReactNode => {
        if (viewerReview != null || showPlaceholderRow) {
            const name = viewerReview?.reviewerDisplayName
                ?? user?.displayName
                ?? user?.userName
                ?? '';

            // The one row that can still be labelled when the RESOLVER names nobody — a stale
            // or failed §16.7.4 read, or an id that resolved to no account — because the
            // viewer's own account is in the auth context either way. Every other row takes its
            // username from that read, which does carry one.
            //
            // The review row WINS the fallback, and the order matters: the recorded review stays
            // the authority on its own labelling for the second line exactly as it is for the
            // first, so both lines come from the one resolver rather than one from each source.
            const identity = {
                userId: viewerReview?.reviewerUserId ?? viewerId,
                displayName: name,
                userName: viewerReview?.reviewerUserName ?? user?.userName
            };

            const control = renderViewerVoteControl();

            return control == null && viewerReview == null
                ? null
                : renderReviewRow(identity, control, 'viewer-row');
        }

        return null;
    };

    const decisionSelection = selectedDecision === ApprovalDecision.Approve
        ? { text: approveOptionText, selectionCssClass: approveSelectionCssClass }
        : selectedDecision === ApprovalDecision.Reject
            ? { text: rejectOptionText, selectionCssClass: rejectSelectionCssClass }
            : undefined;

    const pill = statusPill();
    const renderedViewerRow = viewerRow();

    const panelCssClass = showBorder
        ? `g2h-review-panel border rounded-3 p-3 p-lg-4 ${cssClass}`
        : `g2h-review-panel ${cssClass}`;

    return (
        <section className={panelCssClass} aria-labelledby={headingId}>
            <div className="d-flex justify-content-between align-items-start mb-3">
                <h4 className="mb-0" id={headingId}>{titleText}</h4>

                {mayRequest && (
                    <div className="dropdown" ref={picker.containerRef}>
                        <button
                            type="button"
                            id={picker.triggerId}
                            ref={picker.triggerRef}
                            className="btn btn-link p-0 text-body g2h-review-request-cog"
                            title={requestReviewTooltip}
                            aria-label={requestReviewTooltip}
                            aria-controls={picker.isOpen ? picker.menuId : undefined}
                            aria-expanded={picker.isOpen}
                            onClick={togglePicker}>
                            <i className="bi bi-gear-fill" aria-hidden="true"></i>
                        </button>

                        {picker.isOpen && (
                            <div
                                id={picker.menuId}
                                ref={picker.menuRef}
                                aria-labelledby={picker.triggerId}
                                className="dropdown-menu dropdown-menu-end show shadow p-0 g2h-review-candidate-picker">
                                <div className="g2h-review-picker-head px-3 pt-3 pb-2">
                                    <p className="fw-bold small mb-2">
                                        {pickerTitleText.replace(
                                            '{max}', String(maxReviewerRequests))}
                                    </p>

                                    <input
                                        type="text"
                                        className="form-control form-control-sm"
                                        placeholder={candidateFilterPlaceholderText}
                                        aria-label={candidateFilterPlaceholderText}
                                        value={candidateFilter}
                                        onChange={(event) =>
                                            setCandidateFilter(event.target.value)} />

                                    {isAtRequestCap && (
                                        <p className="small text-warning-emphasis mb-0 mt-2">
                                            {requestCapReachedText}
                                        </p>
                                    )}
                                </div>

                                {isCandidatesLoading ? (
                                    <p className="small text-muted mb-0 px-3 py-2">Loading…</p>
                                ) : (
                                    <div className="g2h-review-picker-list">
                                        {renderPickerSection(
                                            suggestionsSectionText, suggestionRows, 'suggestion')}

                                        {renderPickerSection(
                                            requestedSectionText, requestedPickerRows, 'requested')}

                                        {renderPickerSection(
                                            everyoneElseSectionText, everyoneElseRows, 'everyone')}

                                        {suggestionRows.length === 0
                                            && requestedPickerRows.length === 0
                                            && everyoneElseRows.length === 0 && (
                                            <p className="small text-muted mb-0 px-3 py-2">
                                                {noCandidatesText}
                                            </p>
                                        )}
                                    </div>
                                )}
                            </div>
                        )}
                    </div>
                )}
            </div>

            {isLoading ? (
                <p className="small text-muted mb-3">Loading…</p>
            ) : (
                <div className="mb-4">
                    {renderedViewerRow}

                    {otherRows.map((row) => renderReviewRow(
                        row,
                        row.vote != null ? (
                            <span className={`btn btn-sm w-100 ${voteBadgeCssClass(row.vote)} g2h-review-vote-badge mb-0`}>
                                {voteBadgeText(row.vote)}
                            </span>
                        ) : (
                            // Asked and not yet answered. A warning chip rather than a muted one:
                            // an outstanding request is the round waiting on somebody, which is
                            // the thing a publisher is deciding whether to keep waiting for.
                            <span className={`btn btn-sm w-100 ${requestedVoteCssClass} g2h-review-vote-badge mb-0`}>
                                {requestedVoteText}
                            </span>
                        ),
                        row.key))}

                    {renderedViewerRow == null
                        && otherRows.length === 0
                        && emptyText.length > 0
                        && <p className="small text-muted mb-0">{emptyText}</p>}
                </div>
            )}

            {/* THE WHOLE OUTCOME WAITS ON THE LOAD. Every part of it — the blocked reasons,
                the status pill, the bypass and the decision controls — is read off rows and a
                verdict that have not arrived yet, so painting any of it mid-load states an
                outcome nobody has computed: an unblocked-looking pill over a round that turns
                out to be blocked, and a decision control the caller may not be allowed. */}
            {isLoading === false && (
                <>
                    <h4 className="mb-3" id={outcomeHeadingId}>{outcomeTitleText}</h4>

                    {isBlocked && (
                        <div className="d-flex mb-3 g2h-review-blocked" role="status">
                            <i
                                className={`bi ${blockedIconCssClass} fs-3 me-2`}
                                aria-hidden="true"></i>
                            <div>
                                <span className="fw-bold d-block">{blockedTitleText}</span>
                                {approvalVerdict?.blockReasons.map((reason) => (
                                    <span
                                        className="small d-block g2h-review-block-reason"
                                        key={reason.code}>
                                        {reason.message}
                                    </span>
                                ))}
                            </div>
                        </div>
                    )}

                    <div className={`btn ${pill.pillCssClass} w-100 mb-3 g2h-review-status-pill`}>
                        <i
                            className={`bi ${pill.iconCssClass} me-2 small`}
                            aria-hidden="true"></i>
                        {pill.text}
                    </div>

                    {/* Directly under the outcome it undoes, and a LINK rather than a button:
                        it is the quiet way out of a decision, not a third thing to press
                        alongside approve and reject. */}
                    {mayResetApproval && (
                        <div className="text-end mb-3">
                            <button
                                type="button"
                                className="btn btn-link p-0 g2h-review-reset"
                                onClick={requestReset}>
                                {resetApprovalText}
                            </button>
                        </div>
                    )}

                    {showBypassCheckbox && (
                        <div className={`form-check mb-3 ${bypassCssClass}`}>
                            <input
                                type="checkbox"
                                className="form-check-input"
                                id={`${headingId}-bypass`}
                                checked={isBypassChecked}
                                onChange={(event) => onBypassToggled(event.target.checked)} />
                            <label className="form-check-label" htmlFor={`${headingId}-bypass`}>
                                {bypassLabelText}
                            </label>
                        </div>
                    )}

                    {showBypassCheckbox && isBypassChecked && (
                        <div className="mb-3">
                            <input
                                type="text"
                                className={isBypassReasonMissing
                                    ? 'form-control is-invalid'
                                    : 'form-control'}
                                placeholder={bypassReasonPlaceholderText}
                                aria-label={bypassReasonPlaceholderText}
                                aria-required="true"
                                aria-describedby={
                                    isBypassReasonMissing ? bypassReasonMessageId : undefined}
                                value={bypassReason}
                                onChange={(event) => setBypassReason(event.target.value)} />

                            {/* Said only once the empty box is genuinely the thing in the way —
                                the bypass ticked AND Approve chosen — so it reads as the answer
                                to "why is Submit dead?" rather than as a scolding for a box the
                                caller has only just been shown. */}
                            {isBypassReasonMissing && (
                                <div
                                    id={bypassReasonMessageId}
                                    className="invalid-feedback d-block">
                                    {bypassReasonRequiredText}
                                </div>
                            )}
                        </div>
                    )}

                {mayDecide && (
                    <>
                        <div className="dropdown" ref={decisionMenu.containerRef}>
                            <button
                                type="button"
                                id={decisionMenu.triggerId}
                                ref={decisionMenu.triggerRef}
                                className={`btn w-100 dropdown-toggle d-flex justify-content-between align-items-center ${decisionSelection?.selectionCssClass ?? setStatusCssClass} mb-0`}
                                aria-controls={decisionMenu.isOpen ? decisionMenu.menuId : undefined}
                                aria-expanded={decisionMenu.isOpen}
                                onClick={decisionMenu.toggle}>
                                {decisionSelection?.text ?? setStatusText}
                            </button>

                            {decisionMenu.isOpen && (
                                <div
                                    id={decisionMenu.menuId}
                                    ref={decisionMenu.menuRef}
                                    tabIndex={-1}
                                    aria-labelledby={decisionMenu.triggerId}
                                    className="dropdown-menu show shadow w-100">
                                    <button
                                        type="button"
                                        className="dropdown-item"
                                        disabled={mayApproveNow === false}
                                        onClick={() => chooseDecision(ApprovalDecision.Approve)}>
                                        <span className="fw-bold d-block">{approveOptionText}</span>
                                        <small className="text-muted">{approveOptionDescription}</small>
                                    </button>

                                    <button
                                        type="button"
                                        className="dropdown-item"
                                        onClick={() => chooseDecision(ApprovalDecision.Reject)}>
                                        <span className="fw-bold d-block">{rejectOptionText}</span>
                                        <small className="text-muted">{rejectOptionDescription}</small>
                                    </button>
                                </div>
                            )}
                        </div>

                        {decisionSelection != null && (
                            <button
                                type="button"
                                className={`btn w-100 mt-2 mb-0 ${decisionSelection.selectionCssClass}`}
                                disabled={maySubmitDecision === false}
                                onClick={submitDecision}>
                                {submitButtonText}
                            </button>
                        )}
                    </>
                )}
                </>
            )}

            <ConfirmDialog
                visible={isConfirmingReset}
                title={resetConfirmTitleText}
                message={resetConfirmMessageText}
                confirmText={resetConfirmButtonText}
                cancelText={resetCancelButtonText}
                onConfirm={confirmReset}
                onCancel={() => setIsConfirmingReset(false)} />
        </section>
    );
}
