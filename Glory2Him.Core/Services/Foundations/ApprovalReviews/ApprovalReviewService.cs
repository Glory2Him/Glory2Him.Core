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
using Glory2Him.Core.Models.Foundations.ApprovalReviews;

namespace Glory2Him.Core.Services.Foundations.ApprovalReviews
{
    /// <summary>
    /// Foundation service for approval reviews. Every operation is both callable directly (the
    /// non-event path: object in → request envelope → shared do-work) and reachable through
    /// the event substrate (the event path in the <c>.Substrate</c> partial: request envelope
    /// in → shared do-work). The private <c>DoXAsync</c> methods own auditing, validation,
    /// storage, and publishing the past-tense fact, so the two paths cannot diverge; the
    /// inbound envelope carries the original caller's <c>SecurityContext</c> and anchors the
    /// causation chain.
    /// </summary>
    public partial class ApprovalReviewService : IApprovalReviewService
    {
        private readonly IStorageBroker storageBroker;
        private readonly IDateTimeBroker dateTimeBroker;
        private readonly IIdentifierBroker identifierBroker;
        private readonly IEventBroker eventBroker;
        private readonly IEventEnvelopeFactory eventEnvelopeFactory;
        private readonly ISecurityAuditBroker securityAuditBroker;
        private readonly ILoggingBroker loggingBroker;

        public ApprovalReviewService(
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

        public ValueTask<ApprovalReview> AddApprovalReviewAsync(
            ApprovalReview approvalReview,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalReviewIsNotNull(approvalReview);

                EventEnvelope<ApprovalReview> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: approvalReview);

                return await DoAddApprovalReviewAsync(
                    approvalReview: approvalReview,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<IQueryable<ApprovalReview>> RetrieveAllApprovalReviewsAsync(
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                return await this.storageBroker.SelectAllApprovalReviewsAsync(cancellationToken);
            });

        public ValueTask<ApprovalReview> RetrieveApprovalReviewByIdAsync(
            Guid approvalReviewId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRetrieveApprovalReviewById(approvalReviewId);

                ApprovalReview maybeApprovalReview =
                    await this.storageBroker.SelectApprovalReviewByIdAsync(approvalReviewId, cancellationToken);

                ValidateStorageApprovalReview(maybeApprovalReview, approvalReviewId);

                return maybeApprovalReview;
            });

        public ValueTask<ApprovalReview> ModifyApprovalReviewAsync(
            ApprovalReview approvalReview,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateApprovalReviewIsNotNull(approvalReview);

                EventEnvelope<ApprovalReview> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: approvalReview);

                return await DoModifyApprovalReviewAsync(
                    approvalReview: approvalReview,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ApprovalReview> RemoveApprovalReviewByIdAsync(
            Guid approvalReviewId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var removeRequest = new ApprovalReview
                {
                    Id = approvalReviewId,
                    DeletionReason = deletionReason
                };

                EventEnvelope<ApprovalReview> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: removeRequest);

                return await DoRemoveApprovalReviewByIdAsync(
                    approvalReviewId: approvalReviewId,
                    deletionReason: deletionReason,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        public ValueTask<ApprovalReview> HardRemoveApprovalReviewByIdAsync(
            Guid approvalReviewId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var hardRemoveRequest = new ApprovalReview
                {
                    Id = approvalReviewId
                };

                EventEnvelope<ApprovalReview> envelope =
                    await this.eventEnvelopeFactory.CreateAsync(content: hardRemoveRequest);

                return await DoHardRemoveApprovalReviewByIdAsync(
                    approvalReviewId: approvalReviewId,
                    inboundEnvelope: envelope,
                    cancellationToken: cancellationToken);
            });

        private async ValueTask<ApprovalReview> DoAddApprovalReviewAsync(
            ApprovalReview approvalReview,
            EventEnvelope<ApprovalReview> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            approvalReview = await this.securityAuditBroker
                .ApplyAddAuditValuesAsync(entity: approvalReview, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnAddApprovalReviewAsync(
                approvalReview: approvalReview,
                securityContext: inboundEnvelope.SecurityContext);

            ApprovalReview addedApprovalReview =
                await this.storageBroker.InsertApprovalReviewAsync(approvalReview, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalReviewOnAddingApprovalReviewSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalReview> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: addedApprovalReview);

            await this.eventBroker.PublishApprovalReviewAsync(
                envelope: outboundEnvelope,
                operation: ApprovalReviewEventOperation.Added);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalReviewOnAddingApprovalReviewSubscriptionName,
                cancellationToken: cancellationToken);

            return addedApprovalReview;
        }

        private async ValueTask<ApprovalReview> DoModifyApprovalReviewAsync(
            ApprovalReview approvalReview,
            EventEnvelope<ApprovalReview> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            approvalReview = await this.securityAuditBroker
                .ApplyModifyAuditValuesAsync(entity: approvalReview, securityContext: inboundEnvelope.SecurityContext);

            await ValidateOnModifyApprovalReviewAsync(
                approvalReview: approvalReview,
                securityContext: inboundEnvelope.SecurityContext);

            ApprovalReview maybeApprovalReview = await this.storageBroker.SelectApprovalReviewByIdAsync(
                approvalReviewId: approvalReview.Id,
                cancellationToken: cancellationToken);

            ValidateStorageApprovalReview(maybeApprovalReview, approvalReviewId: approvalReview.Id);

            approvalReview = await this.securityAuditBroker
                .EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    entity: approvalReview,
                    storageEntity: maybeApprovalReview);

            ValidateAgainstStorageApprovalReviewOnModify(
                inputApprovalReview: approvalReview,
                storageApprovalReview: maybeApprovalReview);

            ApprovalReview updatedApprovalReview =
                await this.storageBroker.UpdateApprovalReviewAsync(approvalReview, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalReviewOnModifyingApprovalReviewSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalReview> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: updatedApprovalReview);

            await this.eventBroker.PublishApprovalReviewAsync(
                envelope: outboundEnvelope,
                operation: ApprovalReviewEventOperation.Modified);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalReviewOnModifyingApprovalReviewSubscriptionName,
                cancellationToken: cancellationToken);

            return updatedApprovalReview;
        }

        private async ValueTask<ApprovalReview> DoRemoveApprovalReviewByIdAsync(
            Guid approvalReviewId,
            string? deletionReason,
            EventEnvelope<ApprovalReview> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnRemoveApprovalReviewById(approvalReviewId);

            ApprovalReview maybeApprovalReview =
                await this.storageBroker.SelectApprovalReviewByIdAsync(approvalReviewId, cancellationToken);

            ValidateStorageApprovalReview(maybeApprovalReview, approvalReviewId);

            if (maybeApprovalReview.IsDeleted)
                return maybeApprovalReview;

            if (deletionReason is not null)
                maybeApprovalReview.DeletionReason = deletionReason;

            ApprovalReview auditedApprovalReview =
                await this.securityAuditBroker.ApplyRemoveAuditValuesAsync(
                    entity: maybeApprovalReview,
                    securityContext: inboundEnvelope.SecurityContext);

            ApprovalReview removedApprovalReview = await this.storageBroker.UpdateApprovalReviewAsync(
                approvalReview: auditedApprovalReview,
                cancellationToken: cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalReviewOnRemovingApprovalReviewByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalReview> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: removedApprovalReview);

            await this.eventBroker.PublishApprovalReviewAsync(
                envelope: outboundEnvelope,
                operation: ApprovalReviewEventOperation.Removed);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalReviewOnRemovingApprovalReviewByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return removedApprovalReview;
        }

        private async ValueTask<ApprovalReview> DoHardRemoveApprovalReviewByIdAsync(
            Guid approvalReviewId,
            EventEnvelope<ApprovalReview> inboundEnvelope,
            CancellationToken cancellationToken)
        {
            ValidateOnHardRemoveApprovalReviewById(approvalReviewId);

            ApprovalReview maybeApprovalReview =
                await this.storageBroker.SelectApprovalReviewByIdAsync(approvalReviewId, cancellationToken);

            ValidateStorageApprovalReview(maybeApprovalReview, approvalReviewId);

            ApprovalReview deletedApprovalReview =
                await this.storageBroker.DeleteApprovalReviewAsync(maybeApprovalReview, cancellationToken);

            await RecordEventProcessedAsync(
                envelope: inboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalReviewOnHardRemovingApprovalReviewByIdSubscriptionName,
                cancellationToken: cancellationToken);

            EventEnvelope<ApprovalReview> outboundEnvelope =
                await this.eventEnvelopeFactory.CreateNextAsync(
                    sourceEnvelope: inboundEnvelope,
                    content: deletedApprovalReview);

            await this.eventBroker.PublishApprovalReviewAsync(
                envelope: outboundEnvelope,
                operation: ApprovalReviewEventOperation.HardRemoved);

            await RecordEventProcessedAsync(
                envelope: outboundEnvelope,
                receiverName: EventBrokerIdentifiers.ApprovalReviewOnHardRemovingApprovalReviewByIdSubscriptionName,
                cancellationToken: cancellationToken);

            return deletedApprovalReview;
        }
    }
}
