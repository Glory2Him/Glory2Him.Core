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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Brokers.DateTimes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Factories.Events;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ApprovalComments;

namespace Glory2Him.Core.Services.Foundations.ApprovalComments
{
    /// <summary>
    /// Foundation service for approval comments. Every operation is both callable directly (the
    /// non-event path: object in → request envelope → shared do-work) and reachable through
    /// the event substrate (the event path in the <c>.Substrate</c> partial: request envelope
    /// in → shared do-work). The private <c>DoXAsync</c> methods own auditing, validation,
    /// storage, and publishing the past-tense fact, so the two paths cannot diverge; the
    /// inbound envelope carries the original caller's <c>SecurityContext</c> and anchors the
    /// causation chain.
    /// </summary>
    public partial class ApprovalCommentService : IApprovalCommentService
    {
        private readonly IStorageBroker storageBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventBroker eventBroker;
        private readonly IEventEnvelopeFactory eventEnvelopeFactory;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly ILoggingBroker loggingBroker;

        public ApprovalCommentService(
            IStorageBroker storageBroker,
            IDateTimeBroker dateTimeBroker,
            IIdentifierBroker identifierBroker,
            IEventBroker eventBroker,
            IEventEnvelopeFactory eventEnvelopeFactory,
            ISecurityAuditBroker securityAuditBroker,
            ILoggingBroker loggingBroker)
        {
            this.storageBroker = storageBroker;
            this.dateTimeBroker = dateTimeBroker;
            this.identifierBroker = identifierBroker;
            this.eventBroker = eventBroker;
            this.eventEnvelopeFactory = eventEnvelopeFactory;
            this.securityAuditBroker = securityAuditBroker;
            this.loggingBroker = loggingBroker;
        }

        public ValueTask<ApprovalComment> AddApprovalCommentAsync(
            ApprovalComment approvalComment,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalCommentIsNotNull(approvalComment);

                EventEnvelope<ApprovalComment> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: approvalComment);

                return await DoAddApprovalCommentAsync(
                    approvalComment: approvalComment,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<IQueryable<ApprovalComment>> RetrieveAllApprovalCommentsAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                return await this.storageBroker.SelectAllApprovalCommentsAsync(cancellationToken);
            });

        public ValueTask<ApprovalComment> RetrieveApprovalCommentByIdAsync(
            Guid approvalCommentId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRetrieveApprovalCommentById(approvalCommentId);

                ApprovalComment maybeApprovalComment =
                    await this.storageBroker.SelectApprovalCommentByIdAsync(approvalCommentId, cancellationToken);

                ValidateStorageApprovalComment(maybeApprovalComment, approvalCommentId);

                return maybeApprovalComment;
            });

        public ValueTask<ApprovalComment> ModifyApprovalCommentAsync(
            ApprovalComment approvalComment,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalCommentIsNotNull(approvalComment);

                EventEnvelope<ApprovalComment> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: approvalComment);

                return await DoModifyApprovalCommentAsync(
                    approvalComment: approvalComment,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ApprovalComment> RemoveApprovalCommentByIdAsync(
            Guid approvalCommentId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var removeRequest = new ApprovalComment
                {
                    Id = approvalCommentId,
                    DeletionReason = deletionReason
                };

                EventEnvelope<ApprovalComment> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: removeRequest);

                return await DoRemoveApprovalCommentByIdAsync(
                    approvalCommentId: approvalCommentId,
                    deletionReason: deletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ApprovalComment> HardRemoveApprovalCommentByIdAsync(
            Guid approvalCommentId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var hardRemoveRequest = new ApprovalComment
                {
                    Id = approvalCommentId
                };

                EventEnvelope<ApprovalComment> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: hardRemoveRequest);

                return await DoHardRemoveApprovalCommentByIdAsync(
                    approvalCommentId: approvalCommentId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        private async ValueTask<ApprovalComment> DoAddApprovalCommentAsync(
            ApprovalComment approvalComment,
            EventEnvelope<ApprovalComment> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            approvalComment = await this.securityAuditBroker
                .ApplyAddAuditValuesAsync(entity: approvalComment, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnAddApprovalCommentAsync(
                approvalComment: approvalComment,
                securityContext: inboundEnvelope.SecurityContext);

            ApprovalComment addedApprovalComment =
                await this.storageBroker.InsertApprovalCommentAsync(approvalComment, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalCommentOnAddingApprovalCommentSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalComment> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: addedApprovalComment);

            await this.eventBroker.PublishApprovalCommentAsync(
                envelope: outboundEnvelope,
                operation: ApprovalCommentEventOperation.Added);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalCommentOnAddingApprovalCommentSubscriptionName,
                cancellationToken: cancellationToken);

            return addedApprovalComment;
        }

        private async ValueTask<ApprovalComment> DoModifyApprovalCommentAsync(
            ApprovalComment approvalComment,
            EventEnvelope<ApprovalComment> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            approvalComment = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(entity: approvalComment, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnModifyApprovalCommentAsync(
                approvalComment: approvalComment,
                securityContext: inboundEnvelope.SecurityContext);

            ApprovalComment maybeApprovalComment = await this.storageBroker.SelectApprovalCommentByIdAsync(
                approvalCommentId: approvalComment.Id,
                cancellationToken: cancellationToken);

            ValidateStorageApprovalComment(maybeApprovalComment, approvalCommentId: approvalComment.Id);

            approvalComment = await this.securityAuditBroker
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    entity: approvalComment,
                    storageEntity: maybeApprovalComment);

            ValidateAgainstStorageApprovalCommentOnModify(
                inputApprovalComment: approvalComment,
                storageApprovalComment: maybeApprovalComment);

            ApprovalComment updatedApprovalComment =
                await this.storageBroker.UpdateApprovalCommentAsync(approvalComment, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalCommentOnModifyingApprovalCommentSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalComment> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedApprovalComment);

            await this.eventBroker.PublishApprovalCommentAsync(
                envelope: outboundEnvelope,
                operation: ApprovalCommentEventOperation.Modified);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalCommentOnModifyingApprovalCommentSubscriptionName,
                cancellationToken: cancellationToken);

            return updatedApprovalComment;
        }

        private async ValueTask<ApprovalComment> DoRemoveApprovalCommentByIdAsync(
            Guid approvalCommentId,
            string? deletionReason,
            EventEnvelope<ApprovalComment> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRemoveApprovalCommentById(approvalCommentId);

            ApprovalComment maybeApprovalComment =
                await this.storageBroker.SelectApprovalCommentByIdAsync(approvalCommentId, cancellationToken);

            ValidateStorageApprovalComment(maybeApprovalComment, approvalCommentId);

            if (maybeApprovalComment.IsDeleted)
                return maybeApprovalComment;

            if (deletionReason is not null)
                maybeApprovalComment.DeletionReason = deletionReason;

            ApprovalComment auditedApprovalComment =
                await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(
                    entity: maybeApprovalComment,
                    securityContext: inboundEnvelope.SecurityContext);

            ApprovalComment removedApprovalComment = await this.storageBroker.UpdateApprovalCommentAsync(
                approvalComment: auditedApprovalComment,
                cancellationToken: cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalCommentOnRemovingApprovalCommentByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalComment> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: removedApprovalComment);

            await this.eventBroker.PublishApprovalCommentAsync(
                envelope: outboundEnvelope,
                operation: ApprovalCommentEventOperation.Removed);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalCommentOnRemovingApprovalCommentByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return removedApprovalComment;
        }

        private async ValueTask<ApprovalComment> DoHardRemoveApprovalCommentByIdAsync(
            Guid approvalCommentId,
            EventEnvelope<ApprovalComment> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnHardRemoveApprovalCommentById(approvalCommentId);

            ApprovalComment maybeApprovalComment =
                await this.storageBroker.SelectApprovalCommentByIdAsync(approvalCommentId, cancellationToken);

            ValidateStorageApprovalComment(maybeApprovalComment, approvalCommentId);

            ApprovalComment deletedApprovalComment =
                await this.storageBroker.DeleteApprovalCommentAsync(maybeApprovalComment, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalCommentOnHardRemovingApprovalCommentByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalComment> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: deletedApprovalComment);

            await this.eventBroker.PublishApprovalCommentAsync(
                envelope: outboundEnvelope,
                operation: ApprovalCommentEventOperation.HardRemoved);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalCommentOnHardRemovingApprovalCommentByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return deletedApprovalComment;
        }
    }
}
