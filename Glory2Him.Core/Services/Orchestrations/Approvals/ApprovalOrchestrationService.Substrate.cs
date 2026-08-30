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

using System;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Bases;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests.Exceptions;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Orchestrations.Approvals;

namespace Glory2Him.Core.Services.Orchestrations.Approvals
{
    internal partial class ApprovalOrchestrationService
    {
        // The workflow's ears. Each handler does one thing: name the entity type the fact came
        // from, and hand the row's identity to the flow that decides what it means.
        //
        // Typed per entity because the substrate is typed — an envelope carries one entity and
        // the address it arrived on is what says which. The EntityType is supplied HERE rather
        // than read off the payload, so a forged or mistyped body cannot make a Tag fact drive a
        // ContentItem's approval.
        //
        // For the seven ENTITIES: -Added and -Modified only. An entity's removal is not an
        // approval state (§9.7.6) — a takedown is not a moderation step, and re-opening an
        // approval because its subject was withdrawn is the opposite of what should happen. The
        // consequences of removal are handled where they belong: the removing flow unpublishes,
        // the queue filters, the transition refuses a deleted subject.
        //
        // The WORKFLOW RECORDS below are the deliberate exception (#196 decision 10). Removing
        // an ApprovalReview or an ApprovalComment moves the threshold rather than withdrawing a
        // subject, so those removals ARE subscribed.

        public ValueTask<EventEnvelope<Tag>?> OnTagAddedAsync(
            EventEnvelope<Tag> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Tag,
                eventName: "TagAdded",
                react: ProcessEntityAddedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<Tag>?> OnTagModifiedAsync(
            EventEnvelope<Tag> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Tag,
                eventName: "TagModified",
                react: ProcessEntityModifiedAsync,
                cancellationToken: cancellationToken);

        // The verified name is the name the PUBLISHER signed, and the publisher composes it as
        // entityName + operation where entityName belongs to the tier that owns the address this
        // subscription binds. ContentItem and Link take their top-layer fact from the PROCESSING
        // tier (§12.4.1 rules 6-7), and EventBroker.ContentItemProcessing/LinkProcessing sign
        // with "ContentItemProcessing"/"LinkProcessing" accordingly — so these four read
        // "...Processing..." while the five single-row entities below, whose fact comes from
        // their foundation, use the bare entity name.
        //
        // Getting this wrong is silent: the event name is bound INTO the HMAC, so a mismatch
        // does not misroute anything, it makes the receiver refuse a genuine envelope it was
        // correctly delivered.
        public ValueTask<EventEnvelope<ContentItem>?> OnContentItemAddedAsync(
            EventEnvelope<ContentItem> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.ContentItem,
                eventName: "ContentItemProcessingAdded",
                react: ProcessEntityAddedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<ContentItem>?> OnContentItemModifiedAsync(
            EventEnvelope<ContentItem> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.ContentItem,
                eventName: "ContentItemProcessingModified",
                react: ProcessEntityModifiedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<Link>?> OnLinkAddedAsync(
            EventEnvelope<Link> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Link,
                eventName: "LinkProcessingAdded",
                react: ProcessEntityAddedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<Link>?> OnLinkModifiedAsync(
            EventEnvelope<Link> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Link,
                eventName: "LinkProcessingModified",
                react: ProcessEntityModifiedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<Comment>?> OnCommentAddedAsync(
            EventEnvelope<Comment> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Comment,
                eventName: "CommentAdded",
                react: ProcessEntityAddedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<Comment>?> OnCommentModifiedAsync(
            EventEnvelope<Comment> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Comment,
                eventName: "CommentModified",
                react: ProcessEntityModifiedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<Reaction>?> OnReactionAddedAsync(
            EventEnvelope<Reaction> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Reaction,
                eventName: "ReactionAdded",
                react: ProcessEntityAddedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<Reaction>?> OnReactionModifiedAsync(
            EventEnvelope<Reaction> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Reaction,
                eventName: "ReactionModified",
                react: ProcessEntityModifiedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<BibleReference>?> OnBibleReferenceAddedAsync(
            EventEnvelope<BibleReference> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.BibleReference,
                eventName: "BibleReferenceAdded",
                react: ProcessEntityAddedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<BibleReference>?> OnBibleReferenceModifiedAsync(
            EventEnvelope<BibleReference> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.BibleReference,
                eventName: "BibleReferenceModified",
                react: ProcessEntityModifiedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<Association>?> OnAssociationAddedAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Association,
                eventName: "AssociationAdded",
                react: ProcessEntityAddedAsync,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<Association>?> OnAssociationModifiedAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToEntityFactAsync(
                envelope: envelope,
                entityType: EntityType.Association,
                eventName: "AssociationModified",
                react: ProcessEntityModifiedAsync,
                cancellationToken: cancellationToken);

        // ── The workflow records' ears (§10.17(a)) ────────────────────────────────────
        //
        // EVERY fact address on ApprovalReview and ApprovalComment has a subscriber, because
        // every one of them can move a §8.5 predicate: the evaluation reads comments through
        // IsDeleted is false && IsResolved is false, and reviews through IsDeleted is false &&
        // Verdict != Dismissed. An address left unwatched is a gate that moves unnoticed.
        //
        // All of them are keyed on the record's ApprovalId rather than an entity id. A workflow
        // record names the round it belongs to directly, and the entity is whatever that
        // approval points at, so reaching for the entity would be a second lookup for something
        // the flow resolves anyway. The id is trusted because it is inside the HMAC: the
        // envelope was signed by this system over this content, and verified before it is read.
        //
        // NEVER infer a direction from the address. A fact means the inputs changed, never that
        // the approval may now complete — every handler re-runs the WHOLE §8.5 evaluation and
        // lets the decision function answer. A comment born already settled (§7.8) is the common
        // case and moves nothing, which the re-test ESTABLISHES rather than assumes.
        //
        // Both comment resolution addresses are wired, and that is not belt-and-braces:
        // IsResolved has two writers by design (§14.7 rule 5) — the owner through the general
        // modify, the owner or an administrator through the resolve transition — and which one carried a
        // change depends on nothing more than which control was clicked.

        // The one workflow-record fact that also RETIRES something. A review being recorded is
        // how an invitation gets answered (§7.9 rule 6), so the request the reviewer was asked
        // through stops being outstanding here. Passed as an after-verification hook rather than
        // run inline, because retiring on the strength of an envelope whose signature has not
        // been checked would let anyone reaching the address clear the panel.
        public ValueTask<EventEnvelope<ApprovalReview>?> OnApprovalReviewAddedAsync(
            EventEnvelope<ApprovalReview> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToWorkflowRecordFactAsync(
                envelope: envelope,
                acceptedEventNames: new[] { "ApprovalReviewAdded" },
                approvalId: envelope?.Content?.ApprovalId ?? Guid.Empty,
                cancellationToken: cancellationToken,
                onVerifiedAsync: () => RetireAnsweredReviewRequestAsync(
                    approvalId: envelope?.Content?.ApprovalId ?? Guid.Empty,
                    reviewerUserId: envelope?.Content?.CreatedBy,
                    cancellationToken: cancellationToken));

        public ValueTask<EventEnvelope<ApprovalReview>?> OnApprovalReviewModifiedAsync(
            EventEnvelope<ApprovalReview> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToWorkflowRecordFactAsync(
                envelope: envelope,
                acceptedEventNames: new[] { "ApprovalReviewModified" },
                approvalId: envelope?.Content?.ApprovalId ?? Guid.Empty,
                cancellationToken: cancellationToken);

        // Two names, one address. HardRemoved is published to the Removed address on purpose
        // (EventBrokerIdentifiers.ApprovalReview.cs), so this handler receives envelopes signed
        // under either and must accept both — §8.5 reads a withdrawn review the same way
        // whether the row was soft-deleted or is simply gone.
        public ValueTask<EventEnvelope<ApprovalReview>?> OnApprovalReviewRemovedAsync(
            EventEnvelope<ApprovalReview> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToWorkflowRecordFactAsync(
                envelope: envelope,
                acceptedEventNames:
                    new[] { "ApprovalReviewRemoved", "ApprovalReviewHardRemoved" },
                approvalId: envelope?.Content?.ApprovalId ?? Guid.Empty,
                cancellationToken: cancellationToken);

        // A dismissed verdict leaves the active set (§9.5), which moves the count.
        //
        // This is the one address that can be published by this service's OWN work: the
        // §9.7.4 stale-review reset loops over the round's reviews calling
        // DismissStaleApprovalReviewAsync, and each of those publishes here. Delivery is
        // synchronous, so an unguarded re-test would run mid-loop against a HALF-dismissed set
        // and could auto-approve off a review population that is still being torn down.
        //
        // The loop announces itself and this handler stands down for that approval only —
        // suppressing the re-test, never the signature check. The dismissing flow re-evaluates
        // once, at the end, which is the correct single evaluation for the whole act.
        //
        // WHY THE SUBSCRIPTION REMAINS, and the honest answer: it is currently UNREACHABLE.
        //
        // It used to earn its place because a dismissal could also arrive from a HUMAN — a
        // publisher driving a verdict to Dismissed by hand — and nothing else re-evaluated that.
        // #295 removed every human route.
        //
        // A concurrent DIFFERENT round does not save it either, which was the first replacement
        // argument and is also wrong. ApprovalReview-Dismissed has exactly one publisher
        // (SaveDismissTransitionAsync), reached by exactly one caller (the reset loop), and that
        // loop sets the suppression before it publishes. Delivery is synchronous on the
        // publisher's execution context and the guard is an AsyncLocal, so EVERY production
        // publish of this fact lands inside its own suppression window. Measured: two
        // overlapping resets produced four deliveries and zero re-tests.
        //
        // KEPT, and that is settled rather than pending (#300). §10.17 (a) requires a subscriber
        // on every fact address, and that universal is enforced by a test derived from the
        // operation enum so it cannot be hand-carved. Removing this one would carve the first
        // exception into it — one suppressed delivery per dismissal is a smaller price than a
        // weaker rule for every address.
        //
        // The guard's SCOPING (one approval, not all) is a real property too, pinned by
        // ShouldStillReTestADifferentRoundWhileDismissingAsync, which publishes from outside any
        // window the way a repair pass or an administrative tool one day would — and such a
        // caller would find this handler already correct.
        public ValueTask<EventEnvelope<ApprovalReview>?> OnApprovalReviewDismissedAsync(
            EventEnvelope<ApprovalReview> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToWorkflowRecordFactAsync(
                envelope: envelope,
                acceptedEventNames: new[] { "ApprovalReviewDismissed" },
                approvalId: envelope?.Content?.ApprovalId ?? Guid.Empty,
                cancellationToken: cancellationToken);

        public ValueTask<EventEnvelope<ApprovalComment>?> OnApprovalCommentAddedAsync(
            EventEnvelope<ApprovalComment> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToWorkflowRecordFactAsync(
                envelope: envelope,
                acceptedEventNames: new[] { "ApprovalCommentAdded" },
                approvalId: envelope?.Content?.ApprovalId ?? Guid.Empty,
                cancellationToken: cancellationToken);

        // The owner flipping IsResolved through the general modify — writer one of two.
        public ValueTask<EventEnvelope<ApprovalComment>?> OnApprovalCommentModifiedAsync(
            EventEnvelope<ApprovalComment> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToWorkflowRecordFactAsync(
                envelope: envelope,
                acceptedEventNames: new[] { "ApprovalCommentModified" },
                approvalId: envelope?.Content?.ApprovalId ?? Guid.Empty,
                cancellationToken: cancellationToken);

        // The owner or an administrator flipping it through the resolve transition — writer two of two.
        public ValueTask<EventEnvelope<ApprovalComment>?> OnApprovalCommentResolvedAsync(
            EventEnvelope<ApprovalComment> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToWorkflowRecordFactAsync(
                envelope: envelope,
                acceptedEventNames: new[] { "ApprovalCommentResolved" },
                approvalId: envelope?.Content?.ApprovalId ?? Guid.Empty,
                cancellationToken: cancellationToken);

        // Two names, one address — see OnApprovalReviewRemovedAsync.
        public ValueTask<EventEnvelope<ApprovalComment>?> OnApprovalCommentRemovedAsync(
            EventEnvelope<ApprovalComment> envelope,
            CancellationToken cancellationToken = default) =>
            ReactToWorkflowRecordFactAsync(
                envelope: envelope,
                acceptedEventNames:
                    new[] { "ApprovalCommentRemoved", "ApprovalCommentHardRemoved" },
                approvalId: envelope?.Content?.ApprovalId ?? Guid.Empty,
                cancellationToken: cancellationToken);

        private async ValueTask<EventEnvelope<TRecord>?> ReactToWorkflowRecordFactAsync<TRecord>(
            EventEnvelope<TRecord> envelope,
            string[] acceptedEventNames,
            Guid approvalId,
            CancellationToken cancellationToken,
            Func<ValueTask> onVerifiedAsync = null)
        {
            // FIRST, and deliberately ahead of the signature check — unlike the suppression
            // test below, which sits after it on purpose. Cancellation abandons the delivery
            // outright, so there is nothing left to verify; suppression only decides whether to
            // ACT, which is a decision no envelope gets to reach unverified.
            //
            // Without this the suppressed branch checks the token NOWHERE: it returns below
            // without ever reaching ProcessApprovalInputsChangedAsync, which holds the only
            // other guard on this path.
            cancellationToken.ThrowIfCancellationRequested();

            await ValidateEntityFactEnvelopeAsync(envelope, acceptedEventNames);

            // After verification, before the suppression test. Suppression decides whether to
            // RE-TEST the round; retiring an answered invitation is not a re-test and must
            // happen either way, or a review recorded during a dismissal cascade would leave its
            // invitation standing forever.
            //
            // ISOLATED from the re-test that follows, and this is the whole point of the catch.
            // The hook is bookkeeping - it deletes a row that records who was asked. The re-test
            // decides whether the round is now approved. Letting the first abort the second
            // trades a stale invitation for a round that never re-evaluates: the vote that would
            // have carried it over the line is counted by nothing, and no later event re-drives
            // it, so the item sits blocked with its conditions provably met and nothing on any
            // screen saying why. The stale invitation is the far smaller harm, and a person in
            // the review tier can clear it by hand.
            //
            // BROAD on purpose, and the breadth is the point. An earlier version named the four
            // ApprovalReviewRequest exception types, which read as disciplined and guarded the
            // wrong half: the hook's FIRST statement is an access-broker read, and that broker
            // catches nothing, so a storage outage arrives as a raw SqlException and walked
            // straight past the filter - taking the re-test with it in exactly the case the
            // isolation exists for.
            //
            // The argument for absorbing does not rest on which exception was raised. It rests
            // on what the act IS: bookkeeping, on somebody else's path, whose failure is never a
            // reason to leave a round un-evaluated. A defect in here is still a defect and still
            // logged; what it must not do is decide the workflow's outcome.
            //
            // Cancellation is the one thing that passes through. A cancelled request means the
            // caller has gone, and the re-test below has nothing left to serve.
            if (onVerifiedAsync is not null)
            {
                try
                {
                    await onVerifiedAsync();
                }
                catch (Exception onVerifiedException)
                    when (onVerifiedException is not OperationCanceledException)
                {
                    await this.loggingBroker.LogErrorAsync(onVerifiedException);
                }
            }

            // Deliberately AFTER the signature check. An unverifiable envelope is refused
            // whether or not we would have acted on it — suppression must never become a way
            // to skip verification.
            if (IsDismissalReTestSuppressedFor(approvalId))
            {
                return null;
            }

            await ProcessApprovalInputsChangedAsync(
                approvalId: approvalId,
                cancellationToken: cancellationToken);

            return null;
        }

        // The shared body. A fact is a notification, so nothing is replied with: returning the
        // inbound envelope would put this service's name on a fact another service published.
        private async ValueTask<EventEnvelope<TEntity>?> ReactToEntityFactAsync<TEntity>(
            EventEnvelope<TEntity> envelope,
            EntityType entityType,
            string eventName,
            Func<EntityType, Guid, CancellationToken, ValueTask<ApprovalOutcome>> react,
            CancellationToken cancellationToken)
            where TEntity : IKey
        {
            // Ahead of the verify, for the same reason as above. The token IS observed further
            // down inside react, so this is not an unguarded path — but a caller who has already
            // cancelled should not pay for an HMAC verification first.
            cancellationToken.ThrowIfCancellationRequested();

            await ValidateEntityFactEnvelopeAsync(envelope, eventName);

            await react(entityType, envelope.Content.Id, cancellationToken);

            return null;
        }
    }
}
