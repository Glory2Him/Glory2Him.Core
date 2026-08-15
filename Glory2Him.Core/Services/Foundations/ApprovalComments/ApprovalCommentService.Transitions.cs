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
    /// <para>Modify is the author's wording path — it owns the text and refuses to touch
    /// anything fixed at add. <c>IsResolved</c> is not part of that: recording that a question
    /// was answered changes no words, and it is the one comment operation an <c>Admin</c> may
    /// perform on another person's row. So it gets its own operation, owning exactly
    /// <c>IsResolved</c> and publishing its own fact. That separation is what makes the
    /// <c>Admin</c> exception expressible at all — widening modify would have handed the same
    /// role the author's words, which §14.7 rule 5 withdraws.</para>
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
            // power to declare someone else's question answered.
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

            // Permission is settled before the state is looked at, so a caller who may not act
            // learns nothing about whether the question is currently open.
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

            // A withdrawn comment blocks nothing, so there is no question left on it to answer.
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
            // Admin resolves on the author's behalf: CreatedBy still names who asked the
            // question, UpdatedBy names who declared it answered
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
