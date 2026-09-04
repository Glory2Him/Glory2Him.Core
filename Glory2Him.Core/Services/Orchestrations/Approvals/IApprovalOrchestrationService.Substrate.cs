// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, 'I am the way and the truth and the life.
//                  No one comes to the Father except through me.'"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Tags;

namespace Glory2Him.Core.Services.Orchestrations.Approvals
{
    /// <summary>
    /// The event-facing surface of the workflow: fact handlers invoked by the event substrate,
    /// one per entity per lifecycle fact. These are wired to event listeners exclusively in
    /// <c>EventSubscriptionRegistration</c> — the service exposes the capability; the central
    /// registration decides which address each one listens on, which is where the top-layer
    /// routing of §10.17 rule 1 is actually enforced.
    ///
    /// <para>Every handler replies <c>null</c>. A fact is a notification, so there is nothing
    /// to reply with — the responder shape is here only because the delivery records one.</para>
    /// </summary>
    public partial interface IApprovalOrchestrationService
    {
        ValueTask<EventEnvelope<Tag>?> OnTagAddedAsync(
            EventEnvelope<Tag> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<Tag>?> OnTagModifiedAsync(
            EventEnvelope<Tag> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<ContentItem>?> OnContentItemAddedAsync(
            EventEnvelope<ContentItem> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<ContentItem>?> OnContentItemModifiedAsync(
            EventEnvelope<ContentItem> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<Link>?> OnLinkAddedAsync(
            EventEnvelope<Link> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<Link>?> OnLinkModifiedAsync(
            EventEnvelope<Link> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<Comment>?> OnCommentAddedAsync(
            EventEnvelope<Comment> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<Comment>?> OnCommentModifiedAsync(
            EventEnvelope<Comment> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<Reaction>?> OnReactionAddedAsync(
            EventEnvelope<Reaction> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<Reaction>?> OnReactionModifiedAsync(
            EventEnvelope<Reaction> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<BibleReference>?> OnBibleReferenceAddedAsync(
            EventEnvelope<BibleReference> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<BibleReference>?> OnBibleReferenceModifiedAsync(
            EventEnvelope<BibleReference> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<Association>?> OnAssociationAddedAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<Association>?> OnAssociationModifiedAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default);

        // The submit verb's fact, one per entity (§9.2 rule 3). Each reacts as a modification:
        // the round follows the entity to Submitted and is then evaluated (§9.7.4).
        ValueTask<EventEnvelope<Tag>?> OnTagSubmittedAsync(
            EventEnvelope<Tag> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<ContentItem>?> OnContentItemSubmittedAsync(
            EventEnvelope<ContentItem> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<Link>?> OnLinkSubmittedAsync(
            EventEnvelope<Link> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<Comment>?> OnCommentSubmittedAsync(
            EventEnvelope<Comment> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<Reaction>?> OnReactionSubmittedAsync(
            EventEnvelope<Reaction> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<BibleReference>?> OnBibleReferenceSubmittedAsync(
            EventEnvelope<BibleReference> envelope,
            CancellationToken cancellationToken = default);

        ValueTask<EventEnvelope<Association>?> OnAssociationSubmittedAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Reacts to a recorded approval review by evaluating the round it belongs to
        /// (design §9.7.5).
        /// </summary>
        ValueTask<EventEnvelope<ApprovalReview>?> OnApprovalReviewAddedAsync(
            EventEnvelope<ApprovalReview> envelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Re-tests the round when a recorded verdict is amended (design §10.17(a)).
        /// </summary>
        ValueTask<EventEnvelope<ApprovalReview>?> OnApprovalReviewModifiedAsync(
            EventEnvelope<ApprovalReview> envelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Re-tests the round when a review is withdrawn — dropping an approving verdict from
        /// the count, or unblocking a round a rejection was holding (design §10.17(a)).
        /// Serves both the soft and hard removal, which share one address.
        /// </summary>
        ValueTask<EventEnvelope<ApprovalReview>?> OnApprovalReviewRemovedAsync(
            EventEnvelope<ApprovalReview> envelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Re-tests the round when a verdict leaves the active set by dismissal (design §9.5).
        /// Stands down for the approval whose stale reviews this service is itself dismissing,
        /// so the round is evaluated once at the end of that loop rather than once per review.
        /// </summary>
        ValueTask<EventEnvelope<ApprovalReview>?> OnApprovalReviewDismissedAsync(
            EventEnvelope<ApprovalReview> envelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Re-tests the round when a comment is added — one born outstanding blocks an approval
        /// that was clear (design §10.17(b)).
        /// </summary>
        ValueTask<EventEnvelope<ApprovalComment>?> OnApprovalCommentAddedAsync(
            EventEnvelope<ApprovalComment> envelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Re-tests the round when a comment is amended, which is one of the two ways
        /// <c>IsResolved</c> is written (design §14.7 rule 5).
        /// </summary>
        ValueTask<EventEnvelope<ApprovalComment>?> OnApprovalCommentModifiedAsync(
            EventEnvelope<ApprovalComment> envelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Re-tests the round when a comment is settled through the resolve transition — the
        /// other of the two writers of <c>IsResolved</c> (design §14.7 rule 5).
        /// </summary>
        ValueTask<EventEnvelope<ApprovalComment>?> OnApprovalCommentResolvedAsync(
            EventEnvelope<ApprovalComment> envelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Re-tests the round when a comment is removed — soft-deleting an outstanding one
        /// unblocks the approval (design §10.17(a)). Serves both the soft and hard removal,
        /// which share one address.
        /// </summary>
        ValueTask<EventEnvelope<ApprovalComment>?> OnApprovalCommentRemovedAsync(
            EventEnvelope<ApprovalComment> envelope,
            CancellationToken cancellationToken = default);
    }
}
