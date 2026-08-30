import { useState } from 'react';
import { ReviewPanel } from '../../../components/approvals/reviewPanel';
import {
    ApprovalDecision,
    ApprovalReviewItem,
    ApprovalStatus,
    ApprovalVerdictItem,
    ReviewerCandidateItem
} from '../../../models/components/approvals/approvalReviewItem';
import { useAuth } from '../../../components/securitys/authProvider';
import { useDocumentTitle } from '../../useDocumentTitle';
import {
    CodeSample,
    ComponentDoc,
    ComponentPropRow,
    DocSection,
    LiveDemo,
    PropsTable
} from './shared/componentDoc';

const minimalSample = `
import { ReviewPanel } from '../../components/approvals/reviewPanel';

<ReviewPanel
    entityType="ContentItem"
    contentType="Blog"
    entityOwnerId={contentItem.createdBy}
    approvalStatus={approval.approvalStatus}
    approvalReviewCollection={reviews}
    approvalVerdict={verdict}
    onReviewStatusChanged={(vote) => castVoteAsync(vote)}
    onApprovalStatusChanged={(decision, isBypassRequested, bypassReason) =>
        decideAsync(decision, isBypassRequested, bypassReason)} />
`;

const freshnessSample = `
// THE CONSUMER OWNS FRESHNESS (design §20.6.1).
//
// The panel is pure: it shows the world as of the last props it was handed. It never fetches
// and never subscribes. So the consumer must re-fetch BOTH the reviews and the verdict when
// anything changes the round underneath it:
//
//   - somebody else casts or changes a review
//   - a comment is added, resolved or withdrawn  (this moves the block reasons)
//   - the approval is decided, or auto-approval fires
//
// SignalR over the facts the workflow already publishes (§10.17) is the intended channel;
// polling is an acceptable first cut. Either way REFETCH ON RECONNECT — a missed message
// must not leave a stale panel showing an approve button for a round that has closed.

const { reviews, verdict, refresh } = useApprovalRound(entityType, entityId);

useApprovalRoundChanges(entityId, refresh);   // SignalR or poll -> refresh()

<ReviewPanel
    approvalReviewCollection={reviews}
    approvalVerdict={verdict}
    onReviewStatusChanged={async (vote) => {
        await castVoteAsync(vote);
        await refresh();          // the new vote may have changed the block reasons
    }} />
`;

const verdictSample = `
// The outcome gates read the SERVER's answer verbatim. Do not re-derive them from roles.
//
//   canApprove                    already folds the §8.5 conditions AND this caller's
//                                 standing — HR-2 self-approval, and the reviewer whose own
//                                 review carried the round over the line.
//   isBypassAllowedForCurrentUser already folds the caller's tier and
//                                 DoNotAllowBypassingSettings.
//
// A browser cannot compute either from role names, which is why the verdict carries them.

<ReviewPanel approvalVerdict={verdict} ... />

// Reject is NOT gated by the block rules (§12.5.3 rule 13): a direct rejection withholds
// approval rather than granting it, so it needs no conditions and no bypass. Approve is.
`;

const john: ApprovalReviewItem = {
    reviewerUserId: 'user-john',
    reviewerDisplayName: 'John',
    vote: ApprovalStatus.Approved
};

const susan: ApprovalReviewItem = {
    reviewerUserId: 'user-susan',
    reviewerDisplayName: 'Susan',
    vote: ApprovalStatus.Approved
};

const mary: ReviewerCandidateItem = {
    userId: 'user-mary',
    displayName: 'Mary Adeyemi',
    userName: 'mary.a'
};

const christo: ReviewerCandidateItem = {
    userId: 'user-christo',
    displayName: 'Christo du Toit',
    userName: 'cjdutoit',
    suggestionReason: 'Recently reviewed this type'
};

const johnCandidate: ReviewerCandidateItem = {
    userId: 'user-john',
    displayName: 'John',
    userName: 'john.b'
};

const paul: ReviewerCandidateItem = {
    userId: 'user-paul',
    displayName: 'Paul Nkemdirim',
    userName: 'paul.n'
};

const unblockedVerdict: ApprovalVerdictItem = {
    approvalId: 'approval-1',
    approvalStatus: ApprovalStatus.Submitted,
    blockReasons: [],
    isBlocked: false,
    isBypassAllowedForCurrentUser: false,
    canApprove: true,
    approvalCount: 3,
    requiredNumberOfApprovals: 3,
    unresolvedApprovalCommentCount: 0
};

const blockedVerdict: ApprovalVerdictItem = {
    approvalId: 'approval-1',
    approvalStatus: ApprovalStatus.Submitted,

    blockReasons: [
        { code: 1, message: 'At least 3 approving review(s) is required by reviewers.' },
        { code: 2, message: 'A rejected review is blocking approval.' },
        { code: 3, message: 'All review comments must be resolved.' }
    ],

    isBlocked: true,
    isBypassAllowedForCurrentUser: true,
    canApprove: false,
    approvalCount: 2,
    requiredNumberOfApprovals: 3,
    unresolvedApprovalCommentCount: 1
};

const propRows: ReadonlyArray<ComponentPropRow> = [
    {
        name: 'entityType',
        type: 'string',
        description: 'Names the entity under approval so the §18.6 vote and decision tiers can '
            + 'be composed — capability-last and plural, e.g. ContentItem-Reviewers.'
    },
    {
        name: 'contentType',
        type: 'string?',
        description: 'Adds the narrow ContentItem-{contentType}-Reviewers/-Publishers tier. '
            + 'Only ContentItem carries one (§18.6 rule 5).'
    },
    {
        name: 'entityOwnerId',
        type: 'string?',
        description: 'The entity owner. Suppresses their vote control — nobody reviews their '
            + 'own submission, and the server refuses it regardless.'
    },
    {
        name: 'approvalStatus',
        type: 'ApprovalStatus',
        description: 'Drives the status pill and freezes every control once the round is no '
            + 'longer Submitted. Its own prop rather than read off the verdict, because a '
            + 'read-only viewer gets a status and no verdict.'
    },
    {
        name: 'approvalVerdict',
        type: 'ApprovalVerdictItem?',
        description: 'The per-caller verdict from GET api/Approvals/{entityType}/{entityId}/'
            + 'Verdict. Absent for viewers outside the moderation tier (§16.7.2), which is why '
            + 'the outcome section degrades to the status pill alone.'
    },
    {
        name: 'approvalReviewCollection',
        type: 'ApprovalReviewItem[]',
        defaultValue: '[]',
        description: 'Every recorded review. The viewer\u2019s own row is matched by '
            + 'reviewerUserId — never by name — and pulled to the top; the rest sort '
            + 'alphabetically.'
    },
    {
        name: 'requestedReviewerCollection',
        type: 'ReviewerCandidateItem[]',
        defaultValue: '[]',
        description: 'Pending ApprovalReviewRequest rows (\u00a77.9), rendered with a warning-'
            + 'coloured \u201cRequested\u201d chip in the SAME alphabetical list as the votes. A '
            + 'vote supersedes a request, and the signed-in viewer is excluded \u2014 their own '
            + 'row is already the vote control.'
    },
    {
        name: 'reviewerCandidateCollection',
        type: 'ReviewerCandidateItem[]',
        defaultValue: '[]',
        description: 'Everyone the cog\u2019s picker can offer, fetched by the CONSUMER when '
            + 'onReviewerLookupRequested fires. Nobody is filtered out of it: people who have '
            + 'already voted stay listed, ticked and inert, so a search finds them.'
    },
    {
        name: 'suggestedReviewerCollection',
        type: 'ReviewerCandidateItem[]',
        defaultValue: '[]',
        description: 'Worth asking first, shown above everyone else with each entry\u2019s own '
            + 'suggestionReason. The panel does no ranking \u2014 who suits this item depends on '
            + 'history it cannot see, and inventing an order would quietly become policy.'
    },
    {
        name: 'maxReviewerRequests',
        type: 'number',
        defaultValue: '15',
        description: 'How many people may be waited on at once; names the picker heading and '
            + 'stops new requests at the limit. Counted on OUTSTANDING invitations, so an '
            + 'answered one frees its slot. Withdrawal is never blocked. Default 15.'
    },
    {
        name: 'onReviewStatusChanged',
        type: '(vote) => void',
        description: 'The viewer cast or changed their vote. Persist it, then refresh the '
            + 'reviews AND the verdict — the new vote may have moved the block reasons.'
    },
    {
        name: 'onApprovalStatusChanged',
        type: '(decision, isBypassRequested, bypassReason) => void',
        description: 'Maps 1:1 onto POST .../Decision. A rejection always sends '
            + '(Reject, false, "") — rejection never records a bypass.'
    },
    {
        name: 'onReviewerLookupRequested',
        type: '() => void',
        description: 'The picker opened. Fetch the candidates lazily here rather than on every '
            + 'render of the panel.'
    },
    {
        name: 'onReviewRequested',
        type: '(candidate) => void',
        description: 'A candidate was picked. POST the request and refresh; the server dissolves '
            + 'a duplicate quietly (§7.9 rule 4), so no existence check is needed.'
    },
    {
        name: 'onReviewRequestWithdrawn',
        type: '(candidate) => void',
        description: 'Withdraw a PENDING invitation (§7.9 rule 5). Open to the whole review '
            + 'tier, not just whoever sent it.'
    },
    {
        name: 'isCandidatesLoading',
        type: 'boolean',
        defaultValue: 'false',
        description: 'Shown inside the picker while the consumer fetches candidates. Its own '
            + 'flag rather than isLoading: the cog can be opened while the reviewer rows are '
            + 'already settled, and the two spinners answer different questions.'
    },
    {
        name: 'voteRoles',
        type: 'string?',
        description: 'Comma-separated override for who may vote. Defaults to the §18.6 tier '
            + 'composed from entityType and contentType — pass "" in a demo to render the panel '
            + 'as a reader with no vote control.'
    },
    {
        name: 'decisionRoles',
        type: 'string?',
        description: 'The same override for who may decide. The server re-decides both '
            + '(§14.6), so these change what is RENDERED and nothing else.'
    },
    {
        name: 'titleText',
        type: 'string',
        defaultValue: "'Approval Reviews'",
        description: 'Heading above the reviewer rows.'
    },
    {
        name: 'outcomeTitleText',
        type: 'string',
        defaultValue: "'Review Outcome'",
        description: 'Heading above the block reasons and decision controls.'
    },
    {
        name: 'pickerTitleText',
        type: 'string',
        defaultValue: "'Request up to {max} reviewers'",
        description: 'Picker heading. {max} is replaced with maxReviewerRequests.'
    },
    {
        name: 'cssClass',
        type: 'string',
        defaultValue: "''",
        description: 'Appended to the panel’s own class list, for a consumer that needs to '
            + 'place it in a grid or constrain its width.'
    },
    {
        name: 'text overrides',
        type: 'string (many)',
        description: 'Every visible string is a prop with an English default: votePlaceholderText, '
            + 'approvedText, rejectedText, approveVoteDescription, rejectVoteDescription, '
            + 'blockedTitleText, awaitingApprovalText, approvedStatusText, rejectedStatusText, '
            + 'dismissedStatusText, requestedVoteText, suggestionsSectionText, '
            + 'requestedSectionText, everyoneElseSectionText, requestCapReachedText, '
            + 'requestReviewTooltip, withdrawRequestTooltip, candidateFilterPlaceholderText, '
            + 'noCandidatesText, bypassLabelText, bypassReasonPlaceholderText, setStatusText, '
            + 'approveOptionText, approveOptionDescription, rejectOptionText, '
            + 'rejectOptionDescription, submitButtonText and emptyText. Nothing is hard-coded, '
            + 'so the panel can be translated without touching it.'
    },
    {
        name: 'showBorder',
        type: 'boolean',
        defaultValue: 'false',
        description: 'Wraps the panel in the bordered card. Default false.'
    },
    {
        name: 'isLoading',
        type: 'boolean',
        defaultValue: 'false',
        description: 'Shows the loading line instead of the review rows.'
    },
    {
        name: 'voteRoles / decisionRoles',
        type: 'string?',
        description: 'Comma-separated overrides. Defaults compose from entityType/contentType. '
            + 'decisionRoles excludes every -Reviewers role: HR-3 bars a reviewer from setting '
            + 'an ApprovalStatus.'
    },
    {
        name: 'CSS class props',
        type: 'string',
        description: 'approvedVoteCssClass, rejectedVoteCssClass, awaitingPillCssClass, '
            + 'setStatusCssClass and the rest. Theme classes only — never literal colours, so '
            + 'the panel follows light and dark mode.'
    }
];

export function ReviewPanelDoc() {
    useDocumentTitle('Review Panel');
    const [lastEvent, setLastEvent] = useState('');

    // The panel reads the viewer from the auth context, so the only way to demonstrate a cast
    // vote of ONE'S OWN is to put the signed-in reader into the collection.
    const { user } = useAuth();

    const viewerHasVoted: ApprovalReviewItem = {
        reviewerUserId: user?.userId ?? '',
        reviewerDisplayName: user?.displayName ?? 'You',
        vote: ApprovalStatus.Approved
    };

    const describeDecision = (
        decision: ApprovalDecision,
        isBypassRequested: boolean,
        bypassReason: string
    ) =>
        setLastEvent(
            'onApprovalStatusChanged('
            + (decision === ApprovalDecision.Approve ? 'Approve' : 'Reject')
            + ', bypass=' + isBypassRequested
            + ', reason="' + bypassReason + '")');

    return (
        <ComponentDoc
            name="Review Panel"
            filePath="src/components/approvals/reviewPanel.tsx"
            summary={
                <>
                    An approval round rendered: who has reviewed, the viewer&rsquo;s own vote, why
                    approval is blocked, and the publisher-tier decision controls. Pure
                    presentation &mdash; props in, events out, no fetching and no sockets.
                </>
            }>

            <DocSection
                title="Security posture"
                lead={
                    <>
                        Every gate below decides what to <strong>render</strong> and nothing more.
                        The approval orchestration re-decides votes, decisions and bypass against
                        the stored row (&sect;14.6): a hidden control is a courtesy to the reader,
                        never an authorization boundary. Where the server has already answered a
                        question per caller &mdash; <code>canApprove</code>,{' '}
                        <code>isBypassAllowedForCurrentUser</code> &mdash; the verdict&rsquo;s
                        answer is used verbatim rather than re-derived from role names.
                    </>
                }>
                <CodeSample code={verdictSample} caption="Reading the verdict" />
            </DocSection>

            <DocSection
                title="Freshness is the consumer's job"
                lead={
                    <>
                        The panel never fetches and never subscribes, so it shows the world as of
                        the last props it was handed. A consumer that does not refresh will leave
                        an approve button on a round that has already closed.
                    </>
                }>
                <CodeSample code={freshnessSample} caption="The contract (design 20.6.1)" />
            </DocSection>

            <DocSection title="Minimal usage">
                <CodeSample code={minimalSample} />
            </DocSection>

            <DocSection
                title="Read-only viewer"
                lead={
                    <>
                        No review-tier role. The reviews and the status pill render; every control
                        is withheld, and there is no verdict at all &mdash; &sect;16.7.2 admits it
                        only to the moderation tier, so block reasons never reach an ordinary
                        reader.
                    </>
                }>
                <LiveDemo>
                    {/* The role props are pinned EMPTY here rather than left to compose from
                        entityType. These reference pages are Administrators-only, so a viewer
                        reading this one always holds the decision tier — and the demo would
                        render an approve button while claiming to show a reader who has none.
                        Pinning them is what makes the demo show the state it names. */}
                    <ReviewPanel
                        entityType="ContentItem"
                        approvalStatus={ApprovalStatus.Submitted}
                        approvalReviewCollection={[john, susan]}
                        voteRoles=""
                        decisionRoles=""
                        showBorder={true} />
                </LiveDemo>
            </DocSection>

            <DocSection
                title="Reviewer who has already voted"
                lead={
                    <>
                        A cast vote replaces the dropdown with a badge. The viewer&rsquo;s own row
                        stays pinned first so they can see their answer without hunting for it,
                        and the badge is not a control &mdash; changing a review is the server&rsquo;s
                        business (&sect;8.6.1 is owner-only) and happens through its own route, not
                        by re-opening a menu here.
                    </>
                }>
                <LiveDemo>
                    {/* The panel decides "mine" from the SIGNED-IN identity, not from a prop, so
                        the demo has to put the reader into the collection - hence the review
                        built from useAuth() above. Hard-coding a name here would show somebody
                        else voting, which is the state the demo above already shows. */}
                    <ReviewPanel
                        entityType="ContentItem"
                        approvalStatus={ApprovalStatus.Submitted}
                        approvalReviewCollection={[john, viewerHasVoted]}
                        decisionRoles=""
                        showBorder={true} />
                </LiveDemo>
            </DocSection>

            <DocSection
                title="Blocked round, bypass available"
                lead={
                    <>
                        Every block reason renders. The bypass checkbox appears only because the
                        verdict says this caller may bypass; ticking it reveals a{' '}
                        <strong>required</strong> reason box, and clearing it clears the reason.
                        &ldquo;Approve this item&rdquo; is disabled until the bypass is ticked
                        &mdash; but <strong>&ldquo;Reject this item&rdquo; stays enabled
                        throughout</strong>, because a direct rejection is not gated by the
                        conditions (&sect;12.5.3 rule 13).
                    </>
                }>
                <LiveDemo>
                    <ReviewPanel
                        entityType="ContentItem"
                        contentType="Blog"
                        approvalStatus={ApprovalStatus.Submitted}
                        approvalReviewCollection={[john, susan]}
                        requestedReviewerCollection={[mary]}
                        reviewerCandidateCollection={[johnCandidate, mary, paul]}
                        suggestedReviewerCollection={[christo]}
                        approvalVerdict={blockedVerdict}
                        onApprovalStatusChanged={describeDecision}
                        onReviewStatusChanged={(vote) =>
                            setLastEvent('onReviewStatusChanged('
                                + (vote === ApprovalStatus.Approved ? 'Approved' : 'Rejected')
                                + ')')}
                        onReviewerLookupRequested={() =>
                            setLastEvent('onReviewerLookupRequested()')}
                        onReviewRequested={(candidate) =>
                            setLastEvent('onReviewRequested(' + candidate.displayName + ')')}
                        onReviewRequestWithdrawn={(candidate) =>
                            setLastEvent(
                                'onReviewRequestWithdrawn(' + candidate.displayName + ')')}
                        showBorder={true} />
                </LiveDemo>

                {lastEvent.length > 0 && (
                    <p className="small text-body-secondary">
                        <i className="bi bi-broadcast me-1" aria-hidden="true"></i>
                        Last event: <code>{lastEvent}</code>
                    </p>
                )}
            </DocSection>

            <DocSection
                title="Unblocked round"
                lead={
                    <>
                        Nothing blocks, so no bypass is offered and the decision dropdown is
                        available on <code>canApprove</code> alone. Picking an option reveals a
                        Submit button in that option&rsquo;s own colour.
                    </>
                }>
                <LiveDemo>
                    <ReviewPanel
                        entityType="ContentItem"
                        approvalStatus={ApprovalStatus.Submitted}
                        approvalReviewCollection={[john, susan]}
                        approvalVerdict={unblockedVerdict}
                        onApprovalStatusChanged={describeDecision}
                        showBorder={true} />
                </LiveDemo>
            </DocSection>

            <DocSection
                title="Decided round"
                lead={
                    <>
                        Once the approval is terminal the panel freezes: a cast vote stays visible
                        as a badge but is no longer a control, and the decision dropdown and the
                        request cog are gone.
                    </>
                }>
                <LiveDemo>
                    <ReviewPanel
                        entityType="ContentItem"
                        approvalStatus={ApprovalStatus.Approved}
                        approvalReviewCollection={[john, susan]}
                        showBorder={true} />
                </LiveDemo>
            </DocSection>

            <DocSection
                title="The request picker"
                lead={
                    <>
                        The cog assigns reviewers. Its three sections differ in one thing &mdash;
                        what a click <em>means</em> &mdash; and each rule comes from &sect;7.9
                        rather than from taste.
                    </>
                }>
                <ul className="small">
                    <li>
                        <strong>Suggestions</strong> &mdash; worth asking first, each with the
                        consumer&rsquo;s own reason. Clicking requests them.
                    </li>
                    <li>
                        <strong>Requested</strong> &mdash; asked and not yet answered, shown
                        ticked. Clicking <strong>withdraws</strong>. This is the only route to
                        unassigning somebody (&sect;7.9 rule 5).
                    </li>
                    <li>
                        <strong>Everyone else</strong> &mdash; people who have already voted sit
                        at the top, ticked and <strong>inert</strong>: a cast verdict is theirs
                        (&sect;8.6.1 is owner-only), so there is no unassign to offer. Everyone
                        below them is clickable and gets requested.
                    </li>
                </ul>

                <p className="small text-body-secondary">
                    <strong>Nobody is filtered out of the picker.</strong> An assigned person
                    stays listed and ticked rather than disappearing, so searching for them finds
                    them &mdash; and answers &ldquo;why is this person not here?&rdquo; before it
                    is asked. The picker also stays open after a pick, because assigning several
                    reviewers is one task.
                </p>

                <p className="small text-body-secondary">
                    <code>maxReviewerRequests</code> caps how many people are waited on at once
                    and names the heading. At the cap new requests stop, but{' '}
                    <strong>withdrawal never does</strong> &mdash; otherwise reaching the limit
                    would trap the round with no way to free a slot.
                </p>

                <LiveDemo>
                    {/* All three sections populated at once, which no other demo does: Christo is
                        suggested, Mary is requested, John has voted and Paul is free. Open the cog
                        to see what each section does to a click. */}
                    <ReviewPanel
                        entityType="ContentItem"
                        contentType="Blog"
                        approvalStatus={ApprovalStatus.Submitted}
                        approvalReviewCollection={[john]}
                        requestedReviewerCollection={[mary]}
                        reviewerCandidateCollection={[johnCandidate, mary, paul]}
                        suggestedReviewerCollection={[christo]}
                        maxReviewerRequests={4}
                        decisionRoles=""
                        onReviewerLookupRequested={() =>
                            setLastEvent('onReviewerLookupRequested()')}
                        onReviewRequested={(candidate) =>
                            setLastEvent('onReviewRequested(' + candidate.displayName + ')')}
                        onReviewRequestWithdrawn={(candidate) =>
                            setLastEvent(
                                'onReviewRequestWithdrawn(' + candidate.displayName + ')')}
                        showBorder={true} />
                </LiveDemo>
            </DocSection>

            <DocSection
                title="What the panel shows, and what it leaves out"
                lead={
                    <>
                        Three states are in scope: approved, rejected, and pending. Votes and
                        outstanding requests share <em>one</em> alphabetical list with the viewer
                        first, because a reader asks &ldquo;where does this round stand?&rdquo;
                        per person &mdash; so a name keeps its place whether or not the answer has
                        arrived.
                    </>
                }>
                <p className="small text-body-secondary">
                    Two kinds of row are excluded outright rather than styled.{' '}
                    <strong>Dismissed</strong> verdicts describe content that has since changed
                    (&sect;9.5): the row is kept as evidence that somebody once ruled on
                    superseded text, not as a standing opinion, and showing it would invite a
                    publisher to count it. <strong>Soft-deleted</strong> reviews are withdrawn,
                    and a withdrawn opinion is no opinion. A vote also supersedes an outstanding
                    request, so nobody is listed twice.
                </p>
            </DocSection>

            <DocSection
                title="Roles"
                lead={
                    <>
                        Composed per &sect;18.6, capability-last and plural. Voting takes the
                        review tier &mdash; global <code>Reviewers</code>,{' '}
                        <code>Publishers</code>, <code>Administrators</code>,{' '}
                        <code>{'{EntityType}'}-Reviewers</code> / <code>-Publishers</code>, and for
                        ContentItem the narrow{' '}
                        <code>ContentItem-{'{ContentType}'}-Reviewers</code> /{' '}
                        <code>-Publishers</code> pair. Deciding takes the publisher tier only:
                        every <code>-Reviewers</code> role is excluded, because HR-3 bars a
                        reviewer from setting an approval status by any route. Requesting a review
                        is coordination rather than decision, so it is open to the whole review
                        tier (&sect;7.9 rule 2).
                    </>
                } />

            <DocSection title="Props">
                <PropsTable rows={propRows} />
            </DocSection>
        </ComponentDoc>
    );
}
