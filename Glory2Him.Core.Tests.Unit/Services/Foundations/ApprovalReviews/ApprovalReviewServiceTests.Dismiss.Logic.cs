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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviews
{
    public partial class ApprovalReviewServiceTests
    {
        [Fact]
        public async Task ShouldSaveOnlyTheStatusFieldOnDismissAsync()
        {
            // given: dismiss owns ONLY StatusId. It drives the review to Dismissed and must
            // leave every other field exactly as stored — the reviewer's verdict, comment and
            // provenance are not the dismissal's to touch.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            ApprovalReview storageApprovalReview = CreateRandomApprovalReview();
            storageApprovalReview.StatusId = ApprovalStatus.Approved;
            ApprovalReview expectedStorageApprovalReview = storageApprovalReview.DeepClone();

            ApprovalReview savedApprovalReview = null;

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    storageApprovalReview.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<ApprovalReview>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((ApprovalReview entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalReviewAsync(
                    It.IsAny<ApprovalReview>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<ApprovalReview, CancellationToken>(
                            (entity, _) => savedApprovalReview = entity.DeepClone())
                        .ReturnsAsync((ApprovalReview entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalReviewAsync(
                    It.IsAny<EventEnvelope<ApprovalReview>>(),
                    It.IsAny<ApprovalReviewEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<ApprovalReview>>(
                            new EventPublishResult<ApprovalReview>()));

            // when
            await this.approvalReviewWorkflowService.DismissStaleApprovalReviewAsync(
                storageApprovalReview.Id,
                TestContext.Current.CancellationToken);

            // then
            savedApprovalReview.Should().NotBeNull();
            savedApprovalReview.StatusId.Should().Be(ApprovalStatus.Dismissed);

            savedApprovalReview.Should().BeEquivalentTo(
                expectedStorageApprovalReview,
                options => options.Excluding(approvalReview => approvalReview.StatusId));

            // NO ProcessedEvents row, in either direction (#295). The dual record every other
            // transition writes existed to stop a do-work shared with an event handler
            // processing one delivery twice; this path has no handler, so both rows would be
            // written for a reader that no longer exists.
            //
            // Asserted rather than narrated. The absence is argued at length in the service
            // comment and was, until this assertion, pinned by nothing — re-adding either call
            // left all 4096 tests green.
            this.storageBrokerMock.Verify(broker =>
                    broker.InsertProcessedEventAsync(
                        It.IsAny<ProcessedEvent>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            // The call inventory this suite lost with the publisher-tier theory. Every sibling
            // *.Logic.cs in this folder carries one; without it a new broker call on the
            // dismissal path lands unnoticed.
            this.storageBrokerMock.Verify(broker =>
                    broker.SelectApprovalReviewByIdAsync(
                        storageApprovalReview.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateApprovalReviewAsync(
                        It.IsAny<ApprovalReview>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyModifyAuditValuesAsync(
                        It.IsAny<ApprovalReview>(),
                        It.IsAny<SecurityContext>()),
                Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                    broker.CreateSystemAsync(It.IsAny<ApprovalReview>()),
                Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                    broker.CreateNextAsync(
                        It.IsAny<EventEnvelope<ApprovalReview>>(),
                        It.IsAny<ApprovalReview>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishApprovalReviewAsync(
                        It.IsAny<EventEnvelope<ApprovalReview>>(),
                        ApprovalReviewEventOperation.Dismissed),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldNeverPublishModifiedOnDismissAsync()
        {
            // given: dismissal is caused by an entity change and must never look like the
            // reviewer amending their verdict — publishing Modified would re-enter §8.8's
            // machinery (design §9.7.1).
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            ApprovalReview storageApprovalReview = CreateRandomApprovalReview();
            storageApprovalReview.StatusId = ApprovalStatus.Approved;

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    storageApprovalReview.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<ApprovalReview>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((ApprovalReview entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalReviewAsync(
                    It.IsAny<ApprovalReview>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ApprovalReview entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalReviewAsync(
                    It.IsAny<EventEnvelope<ApprovalReview>>(),
                    It.IsAny<ApprovalReviewEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<ApprovalReview>>(
                            new EventPublishResult<ApprovalReview>()));

            // when
            await this.approvalReviewWorkflowService.DismissStaleApprovalReviewAsync(
                storageApprovalReview.Id,
                TestContext.Current.CancellationToken);

            // then
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishApprovalReviewAsync(
                        It.IsAny<EventEnvelope<ApprovalReview>>(),
                        ApprovalReviewEventOperation.Modified),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishApprovalReviewAsync(
                        It.IsAny<EventEnvelope<ApprovalReview>>(),
                        ApprovalReviewEventOperation.Dismissed),
                Times.Once);
        }
    }
}
