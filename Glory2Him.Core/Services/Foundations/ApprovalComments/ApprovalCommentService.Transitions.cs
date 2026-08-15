// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ApprovalComments;

namespace Glory2Him.Core.Services.Foundations.ApprovalComments
{
    /// <summary>
    /// The narrow state-transition operation (design §9.7.1, §14.7 rule 5).
    ///
    /// <para><c>IsResolved</c> records whether a comment is <b>settled</b> — whether it still
    /// requires something before the approval can proceed. Not every comment asks for anything:
    /// an observation, or a reviewer recording rationale for others to see, is created settled
    /// and never blocks (§7.8). So this operation is a settled/outstanding transition in both
    /// directions, not "declaring a question answered".</para>
    ///
    /// <para>It exists for the <c>Admin</c> route, not for exclusivity over the field. The owner
    /// may change <c>IsResolved</c> through the general modify as readily as through here — it is
    /// their row. What modify cannot express is an <c>Admin</c> settling a comment on the
    /// author's behalf: widening modify to admit one would have handed that role the author's
    /// words too, which §14.7 rule 5 withdraws. So resolution gets its own operation, owning
    /// exactly <c>IsResolved</c>, admitting owner-or-<c>Admin</c>, and publishing its own
    /// fact.</para>
    ///
    /// <para>Two paths writing one field costs nothing here: the approval workflow subscribes to
    /// both <c>ApprovalComment-Modified</c> and <c>ApprovalComment-Resolved</c> to re-test an
    /// approval blocked on <c>RequireReviewCommentResolutionBeforeApprovals</c>, so a gate move
    /// is announced on whichever address carried it.</para>
    ///
    /// <para>Like every transition it loads the row FIRST and authorizes against what is
    /// STORED; the request carries only the id and the flag to record.</para>
    /// </summary>
    internal partial class ApprovalCommentService
    {
        public ValueTask<ApprovalComment> ResolveApprovalCommentAsync(
            Guid approvalCommentId,
            bool isResolved,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Resolve owns only IsResolved, so the request carries nothing but the id and
                // the flag — the entity exists to anchor the security context and the causation
                // chain, exactly as the read path's does.
                var resolveRequest = new ApprovalComment
                {
                    Id = approvalCommentId,
                    IsResolved = isResolved
                };

                EventEnvelope<ApprovalComment> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: resolveRequest);

                return await DoResolveApprovalCommentAsync(
                    approvalCommentId: approvalCommentId,
                    isResolved: isResolved,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        private async ValueTask<ApprovalComment> DoResolveApprovalCommentAsync(
            Guid approvalCommentId,
            bool isResolved,
            EventEnvelope<ApprovalComment> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateUserIsAllowedToComment(inboundEnvelope.SecurityContext);
            ValidateOnResolveApprovalComment(approvalCommentId);

            ApprovalComment storageApprovalComment =
                await LoadResolveTargetAsync(
                    approvalCommentId: approvalCommentId,
                    cancellationToken: cancellationToken);

            // the row-local half: the author, or an Admin acting on their behalf. Narrower than
            // the read posture on purpose — a Reviewer may see the thread without owning the
            // power to declare someone else's comment settled.
            await ValidateUserCanResolveStorageApprovalCommentAsync(
                storageApprovalComment: storageApprovalComment,
                securityContext: inboundEnvelope.SecurityContext);

            // the cross-entity half: the round must still be open and the parent must not be
            // taken down. Asked about the STORED approval and author, never a payload value.
            await ValidateUserMayResolveApprovalCommentAsync(
                approvalId: storageApprovalComment.ApprovalId,
                commentCreatedBy: storageApprovalComment.CreatedBy,
                securityContext: inboundEnvelope.SecurityContext,
                cancellationToken: cancellationToken);

            // Permission is decided before the state is looked at, so a caller who may not act
            // learns nothing about whether the comment is currently outstanding.
            ValidateStorageApprovalCommentResolutionChanges(storageApprovalComment, isResolved);

            // the whole of the operation's remit is this one field
            storageApprovalComment.IsResolved = isResolved;

            return await SaveResolveTransitionAsync(
                approvalComment: storageApprovalComment,
                inboundEnvelope: inboundEnvelope,
                cancellationToken: cancellationToken);
        }

        // Loads the row the resolution acts on. Authorization and the no-op check are decided
        // against what is STORED, so the load happens first, and the NotFound guard belongs
        // with it.
        private async ValueTask<ApprovalComment> LoadResolveTargetAsync(
            Guid approvalCommentId,
            CancellationToken cancellationToken)
        {
            ApprovalComment maybeApprovalComment =
                await this.storageBroker.SelectApprovalCommentByIdAsync(
                    approvalCommentId: approvalCommentId,
                    cancellationToken: cancellationToken);

            ValidateStorageApprovalComment(maybeApprovalComment, approvalCommentId);

            // A withdrawn comment blocks nothing, so there is nothing left on it to settle.
            // Reported as not-found, matching the read posture, so a removed id is not
            // distinguishable from one that never existed.
            ValidateStorageApprovalCommentIsNotDeleted(maybeApprovalComment, approvalCommentId);

            return maybeApprovalComment;
        }

        // The transition tail: stamp the audit values, save, record the inbound delivery,
        // publish the operation's OWN fact (Resolved, never Modified), record the outbound one.
        private async ValueTask<ApprovalComment> SaveResolveTransitionAsync(
            ApprovalComment approvalComment,
            EventEnvelope<ApprovalComment> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            // stamps UpdatedBy with the ACTING user, which is the whole audit story when an
            // Admin settles a comment on the author's behalf: CreatedBy still names who wrote
            // it, UpdatedBy names who declared it settled
            approvalComment = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(
                    entity: approvalComment,
                    securityContext: inboundEnvelope.SecurityContext);

            ApprovalComment updatedApprovalComment =
                await this.storageBroker.UpdateApprovalCommentAsync(
                    approvalComment,
                    cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .ApprovalCommentOnResolvingApprovalCommentSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalComment> outboundEnvelope =
                await this.eventEnvelopeBroker.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedApprovalComment);

            await this.eventBroker.PublishApprovalCommentAsync(
                envelope: outboundEnvelope,
                operation: ApprovalCommentEventOperation.Resolved);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers
                    .ApprovalCommentOnResolvingApprovalCommentSubscriptionName,
                cancellationToken: cancellationToken);

            return updatedApprovalComment;
        }
    }
}
