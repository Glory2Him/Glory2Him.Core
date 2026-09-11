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
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Bases;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Exceptions;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;

namespace Glory2Him.Core.Services.Orchestrations.Approvals
{
    internal partial class ApprovalOrchestrationService
    {
        public ValueTask<ApprovalOutcome> DecideApprovalAsync(
            EntityType entityType,
            Guid entityId,
            ApprovalDecision decision,
            bool isBypassRequested = false,
            string bypassReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                ValidateOnDecideApproval(
                    entityType: entityType,
                    entityId: entityId,
                    decision: decision,
                    isBypassRequested: isBypassRequested,
                    bypassReason: bypassReason);

                EventEnvelope<Approval> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(
                        content: new Approval
                        {
                            EntityType = entityType,
                            EntityId = entityId
                        });

                // Unfiltered, for the same reason the verdict reads it that way: a soft-deleted
                // row still occupies the key, and a visibility-filtered read would report "no
                // approval" for one that exists (§9.7.2 rule 3).
                ApprovalEntityMatch approvalMatch =
                    await this.approvalService.FindApprovalByEntityAsync(
                        entityType: entityType,
                        entityId: entityId,
                        cancellationToken: cancellationToken);

                ValidateStorageApprovalExists(approvalMatch, entityType, entityId);

                // §9.7.6 rule 3, and the outer half of a deliberate pair. The decision function
                // refuses a removed subject too — it is handed the same fact through
                // DecideApprovalRequest.IsSubjectDeleted — and §14.6 rule 2 makes the duplicate
                // intentional rather than redundant: a defect in one can only ever make the two
                // stricter.
                //
                // Reported as NOT FOUND, matching the read posture the entity's own transitions
                // already keep: §14.5 rule 3 makes a soft-deleted entity not found for every
                // caller, Administrators included, so the answer must not distinguish a takedown
                // from an id that never existed.
                bool isEntityVisible = await this.accessBroker.IsEntityVisibleAsync(
                    entityType: entityType,
                    entityId: entityId,
                    cancellationToken: cancellationToken);

                ValidateStorageEntityIsVisible(isEntityVisible, entityType, entityId);

                // The ONE authorisation. Everything after this is bookkeeping and a sync — the
                // question of whether this person may decide this approval is asked here, once,
                // against the row rather than against anything the caller supplied (§16.7.1).
                AccessVerdict verdict = await this.accessBroker.MayDecideApprovalByIdAsync(
                    approvalId: approvalMatch.Id,
                    decision: decision,
                    isBypassRequested: isBypassRequested,
                    bypassReason: bypassReason,
                    securityContext: envelope.SecurityContext,
                    cancellationToken: cancellationToken);

                ValidateUserMayDecideApproval(verdict);

                Approval decidedApproval = await RecordApprovalDecisionAsync(
                    approvalId: approvalMatch.Id,
                    decision: decision,
                    verdict: verdict,
                    bypassReason: bypassReason,
                    cancellationToken: cancellationToken);

                // The entity write is a SYNC, not a second decision, and it travels as a command
                // rather than a call: this service holds no entity services, and each side is
                // testable alone — here that the command was published, there that it is
                // honoured (§16.7.1). Asynchronous in principle, so the outcome says the sync was
                // requested rather than claiming it has landed.
                await PublishEntityApprovalCommandAsync(
                    approval: decidedApproval,
                    cancellationToken: cancellationToken);

                return new ApprovalOutcome
                {
                    ApprovalId = decidedApproval.Id,
                    EntityType = decidedApproval.EntityType,
                    EntityId = decidedApproval.EntityId,
                    ApprovalStatus = decidedApproval.ApprovalStatus,
                    IsApprovedByBypass = decidedApproval.IsApprovedByBypass,
                    ApprovedByBypassReason = decidedApproval.ApprovedByBypassReason,
                    IsEntitySyncRequested = true,
                };
            });

        // §9.8 names Approval.ApprovalStatus the source of truth, so it is written FIRST and the
        // entity follows. Entity-first would make a repair pass — which can only mean "drive the
        // entity to match the approval" — revert a decision that really happened.
        private async ValueTask<Approval> RecordApprovalDecisionAsync(
            Guid approvalId,
            ApprovalDecision decision,
            AccessVerdict verdict,
            string bypassReason,
            CancellationToken cancellationToken)
        {
            Approval storageApproval =
                await this.approvalService.RetrieveApprovalByIdAsync(
                    approvalId: approvalId,
                    cancellationToken: cancellationToken);

            storageApproval.ApprovalStatus = decision == ApprovalDecision.Approve
                ? ApprovalStatus.Approved
                : ApprovalStatus.Rejected;

            // Taken from the VERDICT, never from the request. A caller who asked for a bypass the
            // conditions did not need gets no bypass recorded — otherwise "what was approved
            // without meeting its conditions" answers with rows that met them (§9.7.1 rule 3).
            storageApproval.IsApprovedByBypass = verdict.IsBypassUsed;

            storageApproval.ApprovedByBypassReason = verdict.IsBypassUsed
                ? bypassReason
                : null;

            // A person clicked Approve or Reject, so UpdatedBy records them rather than the
            // workflow. The reset (§16.7.5) is attributed the same way and for the same reason —
            // an administrator pressed it — so this is one of two human writes on this seam, not
            // the only one.
            return await this.approvalService.ModifyApprovalAsync(
                approval: storageApproval,
                attribution: WorkflowAttribution.DecidingCaller,
                cancellationToken: cancellationToken);
        }

        // Carries the decided approval state to whichever entity owns it. Versioned types
        // (ContentItem, Link) are addressed to their PROCESSING service, which owns the
        // group's published slot; every other approvable type is Single-Row, has no group,
        // and goes straight to its foundation (§7.5.1, §12.4.1 rule 10).
        //
        // Carries the decided approval state to whichever entity owns it. The payload is
        // deliberately minimal — the id and the IApproval members — because the transition reads
        // everything it authorises against from its own stored row and copies only these.
        private async ValueTask PublishEntityApprovalCommandAsync(
            Approval approval,
            CancellationToken cancellationToken)
        {
            bool isApproved = approval.ApprovalStatus == ApprovalStatus.Approved;

            switch (approval.EntityType)
            {
                case EntityType.Tag:
                    await PublishCommandAsync(
                        ApplyDecision(new Models.Foundations.Tags.Tag(), approval, isApproved),
                        Models.Events.Foundations.TagEventOperation.Approving,
                        (envelope, operation) =>
                            this.eventBroker.PublishTagAsync(envelope, operation));
                    return;

                case EntityType.ContentItem:
                    await PublishCommandAsync(
                        ApplyDecision(
                            new Models.Foundations.ContentItems.ContentItem(),
                            approval,
                            isApproved),
                        // The PROCESSING address, not the foundation's. ContentItem is
                        // Versioned, so granting approval also has to clear the group's
                        // published slot first, and only the processing service can order the
                        // two writes (§12.4.1 rule 10, §9.7.7 rule 7).
                        Models.Events.Processings.ContentItemProcessingEventOperation.Approving,
                        (envelope, operation) =>
                            this.eventBroker.PublishContentItemProcessingAsync(
                                envelope, operation));
                    return;

                case EntityType.Link:
                    await PublishCommandAsync(
                        ApplyDecision(new Models.Foundations.Links.Link(), approval, isApproved),
                        // The PROCESSING address — Link is Versioned, same as ContentItem.
                        Models.Events.Processings.LinkProcessingEventOperation.Approving,
                        (envelope, operation) =>
                            this.eventBroker.PublishLinkProcessingAsync(envelope, operation));
                    return;

                case EntityType.Comment:
                    await PublishCommandAsync(
                        ApplyDecision(
                            new Models.Foundations.Comments.Comment(),
                            approval,
                            isApproved),
                        Models.Events.Foundations.CommentEventOperation.Approving,
                        (envelope, operation) =>
                            this.eventBroker.PublishCommentAsync(envelope, operation));
                    return;

                case EntityType.Reaction:
                    await PublishCommandAsync(
                        ApplyDecision(
                            new Models.Foundations.Reactions.Reaction(),
                            approval,
                            isApproved),
                        Models.Events.Foundations.ReactionEventOperation.Approving,
                        (envelope, operation) =>
                            this.eventBroker.PublishReactionAsync(envelope, operation));
                    return;

                case EntityType.BibleReference:
                    await PublishCommandAsync(
                        ApplyDecision(
                            new Models.Foundations.BibleReferences.BibleReference(),
                            approval,
                            isApproved),
                        Models.Events.Foundations.BibleReferenceEventOperation.Approving,
                        (envelope, operation) =>
                            this.eventBroker.PublishBibleReferenceAsync(envelope, operation));
                    return;

                case EntityType.Association:
                    await PublishCommandAsync(
                        ApplyDecision(
                            new Models.Foundations.Associations.Association(),
                            approval,
                            isApproved),
                        Models.Events.Foundations.AssociationEventOperation.Approving,
                        (envelope, operation) =>
                            this.eventBroker.PublishAssociationAsync(envelope, operation));
                    return;

                // An approvable type with no command route would otherwise decide the Approval
                // row and silently leave its entity behind, diverging the two records (§9.8)
                // with nothing to show for it.
                default:
                    throw new NotSupportedApprovalOrchestrationException(
                        message: $"No approval command route is defined for " +
                            $"{approval.EntityType}. The approval was decided but its entity " +
                            "cannot be synchronised.");
            }
        }

        // Publishes under the WORKFLOW's identity. The human already authorised this on the
        // Approval row, and asking again here would fail deterministically: the decision function
        // refuses any outcome once the approval is no longer Submitted, which it no longer is.
        private async ValueTask PublishCommandAsync<TEntity, TOperation>(
            TEntity command,
            TOperation operation,
            Func<EventEnvelope<TEntity>, TOperation, ValueTask<EventPublishResult<TEntity>>> publish)
            where TOperation : struct, Enum
        {
            EventEnvelope<TEntity> commandEnvelope =
                await this.eventEnvelopeBroker.CreateSystemAsync(content: command);

            EventPublishResult<TEntity> publishResult =
                await publish(commandEnvelope, operation);

            // §EVN23. This is the one publish in the solution that is not merely announcing a
            // fact — it is the INSTRUCTION that carries a recorded decision to the entity that
            // owns it. The approval row is already committed by the time this runs, so a
            // delivery that failed leaves the approval saying Approved and the entity still
            // sitting at Submitted, with nothing to notice it: delivery is contained, so the
            // failure appears only here, and nothing redelivers it.
            //
            // Logged rather than thrown, for the same reason the foundations log: the decision
            // the caller asked for WAS recorded, and reporting it as failed would be wrong.
            //
            // Being the last statement of THIS METHOD buys nothing here, and an earlier version
            // of this comment claimed otherwise. This is a shared helper: ResetApprovalAsync
            // reaches it through PublishEntityApprovalCommandAsync and then still owes §8.6.2's
            // ResetStaleAIReviewerAssignmentAsync. What makes the report safe is that it is
            // CONTAINED below, not where it sits.
            //
            // ApprovalOutcome.IsEntitySyncRequested stays true — §16.7.1 defines it as
            // requested rather than landed, and the command was in fact published.
            if (publishResult.HasFailedDeliveries)
            {
                // CONTAINED, because the report is bookkeeping on somebody else's path and does
                // not get to decide that path's outcome — the same shape, and the same argument,
                // as ResetStaleAIReviewerAssignmentAsync and Substrate's onVerified hook.
                // LogCriticalAsync has no try/catch of its own, so a faulting sink (back-
                // pressure, a disposed provider at shutdown) would otherwise propagate: it would
                // report a COMMITTED write as a failed one, which is exactly what rule 2 forbids
                // and what being last in this method does NOT prevent. Being last only stops the
                // throw skipping work that follows it HERE — and where this report sits in a
                // shared helper, work still follows it in the CALLER.
                //
                // Swallowed rather than logged, and that is the one place this differs from
                // those two: the sink that would have to carry a second message is the one that
                // just threw. The delivery failure is already recorded on the event store's own
                // row, which the event id and subscription id locate.
                //
                // EVERY exception, cancellation included, and that is deliberate.
                // ILoggingBroker.LogCriticalAsync(Exception) takes no CancellationToken, so the
                // caller's token cannot reach it: an OperationCanceledException raised in there
                // is never the caller cancelling, it is the SINK failing — a disposed provider
                // at shutdown, or a batching provider's own flush deadline — which is the case
                // this block exists for. An earlier version carved it out by exception filter,
                // copied from ResetStaleAIReviewerAssignmentAsync and Substrate's onVerified
                // hook; both of those guard calls that DO take a token, so the carve-out means
                // something there and nothing here.
                //
                // What the carve-out actually did: the escape landed in this service's TryCatch,
                // whose cancellation arm matches an OperationCanceledException whose token is
                // NOT cancelled — the exact shape of a sink-originated one — and turned a
                // committed write into a timeout reported to the caller. It also skipped the
                // outbound dedup write below, letting a redelivery re-apply the transition.
                try
                {
                    await this.loggingBroker.LogCriticalAsync(
                        FailedEventDeliveryException.ForFailedDeliveries(publishResult, operation));
                }
                catch (Exception)
                {
                }
            }
        }

        // The decided state, and nothing else. Publication is asked for only alongside an
        // approval — the transition derives it off for every other target anyway, and asking for
        // it on a rejection is refused by the shape validation before any policy is read.
        private static TEntity ApplyDecision<TEntity>(
            TEntity entity,
            Approval approval,
            bool isApproved)
            where TEntity : IApproval, IKey
        {
            entity.Id = approval.EntityId;
            entity.ApprovalStatus = approval.ApprovalStatus;
            entity.IsPublished = isApproved;
            entity.PublishDate = isApproved ? DateTimeOffset.UtcNow : null;
            entity.IsApprovedByBypass = approval.IsApprovedByBypass;
            entity.ApprovedByBypassReason = approval.ApprovedByBypassReason;

            return entity;
        }
    }
}
