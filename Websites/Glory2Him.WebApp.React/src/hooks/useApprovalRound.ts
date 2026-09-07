import { useCallback, useMemo } from 'react';
import { approvalService } from '../services/foundations/approvalService';
import { approvalCommentService } from '../services/foundations/approvalCommentService';
import { EntityTypeName } from '../models/foundations/approvals/approval';

import {
    ApprovalReviewItem,
    ApprovalVerdictItem,
    ReviewerCandidateItem
} from '../models/components/approvals/approvalReviewItem';

import {
    ReviewCommentItem
} from '../models/components/approvals/reviewCommentItem';

import {
    toApprovalReviewItem,
    toApprovalVerdictItem,
    toRequestedReviewerItem,
    toReviewerCandidateItem
} from '../services/views/approvals/toReviewPanelItems';

import {
    toReviewCommentItems
} from '../services/views/approvals/toReviewCommentItems';

// ONE ROUND, ASSEMBLED. ReviewPanel takes five separate collections and no fetching of its own,
// and four endpoints answer them — so the assembling is a page's job, and every page that shows
// a round would otherwise write the same chain out again.
//
// THE CHAIN IS NOT A CHOICE. Only the verdict knows the approval's id: an ApprovalReview names
// the approval it belongs to and nothing about the post it judges. So the verdict is read first
// and the reviews wait on the id it returns. The other three reads are independent of that
// chain — the candidates, the outstanding requests and the reviewer names are all asked per
// ENTITY, the names because the server resolves who a round involved for itself — and they
// start immediately alongside it.
//
// A REFUSAL IS AN ANSWER HERE. A post with no approval row 404s, and so does a caller outside
// the moderation tier (§14.5 rule 1, so the endpoint cannot be used to probe what exists). Both
// leave the verdict undefined, which is exactly what the panel wants for "no verdict": it shows
// the round read-only rather than a decision surface nobody is entitled to.
export const useApprovalRound = (
    entityType: EntityTypeName,
    entityId: string,
    enabled = true) => {
    const {
        data: approvalVerdict,
        isLoading: isVerdictLoading,
        refetch: refetchVerdict
    } = approvalService.useGetApprovalVerdict(entityType, entityId, enabled);

    const approvalId = approvalVerdict?.approvalId ?? '';

    const {
        data: approvalReviews,
        isLoading: areReviewsLoading,
        refetch: refetchReviews
    } = approvalService.useGetApprovalReviews(approvalId, enabled);

    // No refetch taken off this one: the candidate list cannot move on a round event, so the
    // freshness channel deliberately leaves it out. See the note on refresh below.
    const { data: reviewerCandidates } =
        approvalService.useGetReviewerCandidates(entityType, entityId, enabled);

    const { data: reviewRequests, refetch: refetchRequests } =
        approvalService.useGetReviewRequests(entityType, entityId, enabled);

    // Berean's status (design §8.6.2) — keyed by entity like the candidates and requests above,
    // for the same reason: nothing about it depends on the approval's id.
    const { data: aiReviewerStatus, refetch: refetchAIReviewerStatus } =
        approvalService.useGetAIReviewerStatus(entityType, entityId, enabled);

    // The names of everybody the round involved — its reviewers, its invitees AND its comment
    // authors — resolved server-side off the round itself, so nothing here gathers ids off the
    // rows and the read does not wait on them.
    const { data: reviewerDisplayNames, refetch: refetchDisplayNames } =
        approvalService.useGetReviewerDisplayNames(entityType, entityId, enabled);

    // THE THREAD, on the same chain as the reviews and for the same reason: a comment names the
    // approval it hangs off and nothing about the post being judged, so it waits on the verdict's
    // id. It is assembled HERE rather than in a hook of its own because both things it needs —
    // the approval id and the names — are already resolved on this one, and a second hook would
    // resolve them again.
    const {
        data: approvalComments,
        isLoading: areCommentsLoading,
        refetch: refetchComments
    } = approvalCommentService.useGetApprovalComments(approvalId, enabled);

    const approvalReviewCollection: ReadonlyArray<ApprovalReviewItem> = useMemo(
        () => (approvalReviews ?? []).map(
            (review) => toApprovalReviewItem(review, reviewerDisplayNames ?? [])),
        [approvalReviews, reviewerDisplayNames]);

    // The resolver travels to the requests too: the row carries the name it was addressed to
    // and no username, and §16.7.4 keeps it that way deliberately. Outstanding invitations are
    // part of the round the resolver names, so the username is already in hand.
    const requestedReviewerCollection: ReadonlyArray<ReviewerCandidateItem> = useMemo(
        () => (reviewRequests ?? [])
            .filter((request) => request.isDeleted !== true)
            .map((request) => toRequestedReviewerItem(request, reviewerDisplayNames ?? [])),
        [reviewRequests, reviewerDisplayNames]);

    const reviewerCandidateCollection: ReadonlyArray<ReviewerCandidateItem> = useMemo(
        () => (reviewerCandidates ?? []).map(toReviewerCandidateItem),
        [reviewerCandidates]);

    const approvalVerdictItem: ApprovalVerdictItem | undefined = useMemo(
        () => approvalVerdict == null ? undefined : toApprovalVerdictItem(approvalVerdict),
        [approvalVerdict]);

    const reviewCommentCollection: ReadonlyArray<ReviewCommentItem> = useMemo(
        () => toReviewCommentItems(approvalComments ?? [], reviewerDisplayNames ?? []),
        [approvalComments, reviewerDisplayNames]);

    // THE ROWS ARE WHAT IS LOADING, not the verdict alone. The panel's isLoading holds back the
    // reviews AND the outcome derived from them, so it must stay true until the reviews the
    // verdict unlocked have arrived too — otherwise the round paints itself empty for a beat
    // between the two reads and reads as "nobody has reviewed this".
    const isLoading =
        isVerdictLoading
        || (approvalId.length > 0 && areReviewsLoading);

    // THE FRESHNESS CHANNEL'S OTHER HALF (design §20.6.1). This hook owns the round's reads, so
    // it is also the one place that can re-run them — a caller outside this file has no query
    // keys to invalidate and no business knowing them.
    //
    // WHAT IS RE-READ IS WHAT CAN MOVE. §20.6.1 names the triggers: a review cast elsewhere, a
    // comment added or resolved, a decision or auto-approval landing. The verdict, the reviews,
    // the outstanding requests and the reviewer names all move on those; the CANDIDATES do not —
    // §7.9 leaves everybody listed whether or not they have answered, so only the entity's owner
    // or a role change moves that set, and neither is a round event. It is also the most
    // expensive of the five (two directory-wide role-membership reads), so polling it four times
    // a minute for an answer that cannot change is the one read worth leaving out. It is still
    // invalidated by every write, in approvalService's invalidateRound.
    //
    // THE NAMES STAY IN, though they look as static as the candidates: a reviewer whose FIRST
    // vote arrives through the poll is an id the names set has never carried, and without this
    // their row renders under the panel's fallback instead of their name. The same is true, and
    // more often, of a comment AUTHOR — the round's name set carries them too (§16.7.4), and the
    // ordinary case is a submitter whose first trace on the round is the answer they just wrote.
    //
    // THE THREAD IS IN THE CHANNEL rather than polling on its own. §20.6.1 already names "a
    // comment added or resolved" as a trigger, and it is the read most likely to move under an
    // open tab — two moderators working one submission is the case the thread exists for. Giving
    // it its own refetchInterval would have polled a HIDDEN tab, missed the reconnect case, and
    // cancelled its own in-flight read on a slow network; joining here inherits all three
    // answers, and one channel means one place to replace when SignalR lands. It is gated on
    // approvalId exactly as the reviews are, and for the same reason: with no round the filter
    // would go out as `approvalId eq ` and be refused every interval.
    //
    // REFETCH IS NOT INVALIDATION, and both of its differences bite here.
    //
    // It ignores `enabled`, so EVERY gate the reads declare has to be restated by hand here or
    // refresh quietly reaches past all of them. The reviews read carries its own: with no
    // approval row the verdict is undefined, approvalId is empty, and an ungated refetch would
    // ask the server for `approvalId eq ` — a malformed filter, refused, silently (the reads
    // suppress their toasts by design), every interval for as long as the tab is open. The other
    // four are gated on the caller's `enabled` and a non-empty entityId, so refresh answers to
    // those before it asks for anything: a disabled round refreshes into nothing, which is what
    // a caller that switched it off asked for.
    //
    // It also DEFAULTS TO CANCELLING an in-flight fetch and starting again. Nothing here forwards
    // an AbortSignal to axios, so a cancelled request still runs and its answer is discarded —
    // and a read slower than the poll's interval would be restarted forever, leaving the panel
    // frozen on stale props with no spinner to admit it. That is the exact failure §20.6.1
    // exists to prevent, so every refetch joins the in-flight read rather than replacing it.
    const refresh = useCallback(async () => {
        if (enabled === false || entityId.length === 0) {
            return;
        }

        await Promise.all([
            refetchVerdict({ cancelRefetch: false }),
            approvalId.length > 0
                ? refetchReviews({ cancelRefetch: false })
                : Promise.resolve(),
            approvalId.length > 0
                ? refetchComments({ cancelRefetch: false })
                : Promise.resolve(),
            refetchRequests({ cancelRefetch: false }),
            refetchDisplayNames({ cancelRefetch: false }),
            refetchAIReviewerStatus({ cancelRefetch: false })
        ]);
    }, [
        enabled,
        entityId,
        approvalId,
        refetchVerdict,
        refetchReviews,
        refetchComments,
        refetchRequests,
        refetchDisplayNames,
        refetchAIReviewerStatus
    ]);

    return {
        approvalVerdict: approvalVerdictItem,
        approvalReviewCollection,

        // THE ROWS THEMSELVES, beside their projection: a changed vote is a PUT of the row that
        // was read (§7.7 rule 1), audit fields and all, and the projection deliberately carries
        // none of that.
        approvalReviews: approvalReviews ?? [],
        requestedReviewerCollection,
        reviewerCandidateCollection,
        isLoading,

        // THE THREAD, projected — and the raw rows beside it, because an edit is a PUT of the row
        // that was read (the foundation pins four fields against storage) and the projection
        // deliberately carries none of that.
        reviewCommentCollection,
        reviewComments: approvalComments ?? [],

        // Its OWN loading flag rather than folded into isLoading. The thread and the round paint
        // in different columns, and holding the review panel back for a comment read it does not
        // use would make the decision surface slower for no reason.
        areReviewCommentsLoading: approvalId.length > 0 && areCommentsLoading,

        // RAW, not projected: whether Berean is offered, requested and how far it's got are
        // already the shape a consumer needs (build the ReviewerCandidateItem + status pair for
        // ReviewPanel's aiReviewerCandidate/aiReviewerAssignment props from this), so there is
        // nothing here for a view-service to translate.
        aiReviewerStatus,

        refresh
    };
};
