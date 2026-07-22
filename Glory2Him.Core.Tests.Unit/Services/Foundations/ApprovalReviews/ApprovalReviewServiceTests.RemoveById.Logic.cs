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
using FluentAssertions;
using Force.DeepCloner;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviews
{
    public partial class ApprovalReviewServiceTests
    {
        [Fact]
        public async Task ShouldRemoveApprovalReviewByIdAsync()
        {
            // given
            ApprovalReview randomApprovalReview = CreateRandomApprovalReview();
            randomApprovalReview.IsDeleted = false;
            ApprovalReview storageApprovalReview = randomApprovalReview;

            ApprovalReview auditedApprovalReview = storageApprovalReview.DeepClone();
            auditedApprovalReview.IsDeleted = true;

            ApprovalReview expectedApprovalReview = auditedApprovalReview.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    randomApprovalReview.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApprovalReview, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedApprovalReview);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalReviewAsync(auditedApprovalReview, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedApprovalReview);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalReviewAsync(
                    It.IsAny<EventEnvelope<ApprovalReview>>(),
                    ApprovalReviewEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<ApprovalReview>>(
                        new EventPublishResult<ApprovalReview>()));

            // when
            ApprovalReview actualApprovalReview =
                await this.approvalReviewService.RemoveApprovalReviewByIdAsync(
                    randomApprovalReview.Id,
                    deletionReason: null,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalReview.Should().BeEquivalentTo(expectedApprovalReview);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    randomApprovalReview.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApprovalReview, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalReviewAsync(auditedApprovalReview, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalReviewAsync(
                    It.IsAny<EventEnvelope<ApprovalReview>>(),
                    ApprovalReviewEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalReviewOnRemovingApprovalReviewByIdSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRemoveApprovalReviewByIdWithDeletionReasonAsync()
        {
            // given
            string someDeletionReason = GetRandomString();
            ApprovalReview randomApprovalReview = CreateRandomApprovalReview();
            randomApprovalReview.IsDeleted = false;
            ApprovalReview storageApprovalReview = randomApprovalReview;

            ApprovalReview auditedApprovalReview = storageApprovalReview.DeepClone();
            auditedApprovalReview.IsDeleted = true;
            auditedApprovalReview.DeletionReason = someDeletionReason;

            ApprovalReview expectedApprovalReview = auditedApprovalReview.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    randomApprovalReview.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApprovalReview, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedApprovalReview);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalReviewAsync(auditedApprovalReview, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedApprovalReview);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalReviewAsync(
                    It.IsAny<EventEnvelope<ApprovalReview>>(),
                    ApprovalReviewEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<ApprovalReview>>(
                        new EventPublishResult<ApprovalReview>()));

            // when
            ApprovalReview actualApprovalReview =
                await this.approvalReviewService.RemoveApprovalReviewByIdAsync(
                    randomApprovalReview.Id,
                    deletionReason: someDeletionReason,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalReview.Should().BeEquivalentTo(expectedApprovalReview);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    randomApprovalReview.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApprovalReview, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalReviewAsync(auditedApprovalReview, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalReviewAsync(
                    It.IsAny<EventEnvelope<ApprovalReview>>(),
                    ApprovalReviewEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalReviewOnRemovingApprovalReviewByIdSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnEarlyOnRemoveByIdIfAlreadyDeletedAsync()
        {
            // given
            ApprovalReview alreadyDeletedApprovalReview = CreateRandomApprovalReview();
            alreadyDeletedApprovalReview.IsDeleted = true;
            Guid someApprovalReviewId = alreadyDeletedApprovalReview.Id;
            ApprovalReview expectedApprovalReview = alreadyDeletedApprovalReview;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(alreadyDeletedApprovalReview);

            // when
            ApprovalReview actualApprovalReview =
                await this.approvalReviewService.RemoveApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualApprovalReview.Should().BeEquivalentTo(expectedApprovalReview);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    someApprovalReviewId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
