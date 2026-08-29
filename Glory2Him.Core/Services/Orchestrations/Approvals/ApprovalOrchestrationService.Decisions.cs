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

            // A person clicked Approve or Reject, so UpdatedBy records them - the only write on
            // this seam that belongs to a human rather than to the workflow.
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
        {
            EventEnvelope<TEntity> commandEnvelope =
                await this.eventEnvelopeBroker.CreateSystemAsync(content: command);

            await publish(commandEnvelope, operation);
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
