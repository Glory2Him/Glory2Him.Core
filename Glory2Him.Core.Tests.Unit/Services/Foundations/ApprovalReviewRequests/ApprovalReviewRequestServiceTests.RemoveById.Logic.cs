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
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviewRequests
{
    public partial class ApprovalReviewRequestServiceTests
    {
        [Fact]
        public async Task ShouldRemoveApprovalReviewRequestByIdAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalReviewRequest randomApprovalReviewRequest =
                CreateApprovalReviewRequestFiller(randomDateTimeOffset).Create();

            Guid inputApprovalReviewRequestId = randomApprovalReviewRequest.Id;
            string randomDeletionReason = GetRandomString();
            ApprovalReviewRequest storageApprovalReviewRequest = randomApprovalReviewRequest;
            ApprovalReviewRequest auditAppliedApprovalReviewRequest = storageApprovalReviewRequest.DeepClone();
            auditAppliedApprovalReviewRequest.IsDeleted = true;
            ApprovalReviewRequest removedApprovalReviewRequest = auditAppliedApprovalReviewRequest.DeepClone();
            ApprovalReviewRequest expectedApprovalReviewRequest = removedApprovalReviewRequest.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestByIdAsync(
                    inputApprovalReviewRequestId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalReviewRequest);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(
                    storageApprovalReviewRequest,
                    It.IsAny<SecurityContext>(),
                    randomDeletionReason))
                        .ReturnsAsync(auditAppliedApprovalReviewRequest);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalReviewRequestAsync(
                    auditAppliedApprovalReviewRequest, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(removedApprovalReviewRequest);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalReviewRequestAsync(
                    It.IsAny<EventEnvelope<ApprovalReviewRequest>>(),
                    ApprovalReviewRequestEventOperation.Removed))
                        .Returns(new ValueTask<EventPublishResult<ApprovalReviewRequest>>(
                            new EventPublishResult<ApprovalReviewRequest>()));

            // when
            ApprovalReviewRequest actualApprovalReviewRequest =
                await this.approvalReviewRequestService.RemoveApprovalReviewRequestByIdAsync(
                    inputApprovalReviewRequestId,
                    randomDeletionReason,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalReviewRequest.Should().BeEquivalentTo(expectedApprovalReviewRequest);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalReviewRequestAsync(
                    auditAppliedApprovalReviewRequest, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalReviewRequestAsync(
                    It.IsAny<EventEnvelope<ApprovalReviewRequest>>(),
                    ApprovalReviewRequestEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName == EventBrokerIdentifiers
                            .ApprovalReviewRequestOnRemovingApprovalReviewRequestByIdSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        /// <summary>
        /// The one place this service diverges from its <c>ApprovalReview</c> sibling, and the
        /// divergence is deliberate (§7.9 rule 5): a review is withdrawn only by its author,
        /// because withdrawing it retracts a VERDICT. A request carries no judgement at all, so
        /// anyone in the requesting tier may undo an invitation sent to the wrong person —
        /// including when the person who sent it is unavailable, which is exactly the case the
        /// rule exists to serve. <c>DeletedBy</c> records who did it.
        ///
        /// <para>This test would fail the moment an owner-only gate were copied across from the
        /// sibling service, which is the point of asserting it here rather than only in the
        /// happy path above.</para>
        /// </summary>
        [Theory]
        [MemberData(nameof(ReviewRoles))]
        public async Task ShouldRemoveApprovalReviewRequestRaisedBySomebodyElseAsync(string reviewRole)
        {
            // given: the caller is NOT the requester and is not the invited person either
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalReviewRequest randomApprovalReviewRequest =
                CreateApprovalReviewRequestFiller(
                    dateTimeOffset: randomDateTimeOffset,
                    userId: "the-requester").Create();

            randomApprovalReviewRequest.RequestedUserId = "the-invited-person";
            Guid inputApprovalReviewRequestId = randomApprovalReviewRequest.Id;
            ApprovalReviewRequest storageApprovalReviewRequest = randomApprovalReviewRequest;
            ApprovalReviewRequest auditAppliedApprovalReviewRequest = storageApprovalReviewRequest.DeepClone();
            auditAppliedApprovalReviewRequest.IsDeleted = true;
            auditAppliedApprovalReviewRequest.DeletedBy = "a-different-moderator";
            ApprovalReviewRequest removedApprovalReviewRequest = auditAppliedApprovalReviewRequest.DeepClone();
            ApprovalReviewRequest expectedApprovalReviewRequest = removedApprovalReviewRequest.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestByIdAsync(
                    inputApprovalReviewRequestId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalReviewRequest);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(
                    storageApprovalReviewRequest,
                    It.IsAny<SecurityContext>(),
                    It.IsAny<string>()))
                        .ReturnsAsync(auditAppliedApprovalReviewRequest);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalReviewRequestAsync(
                    auditAppliedApprovalReviewRequest, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(removedApprovalReviewRequest);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalReviewRequestAsync(
                    It.IsAny<EventEnvelope<ApprovalReviewRequest>>(),
                    ApprovalReviewRequestEventOperation.Removed))
                        .Returns(new ValueTask<EventPublishResult<ApprovalReviewRequest>>(
                            new EventPublishResult<ApprovalReviewRequest>()));

            // when
            ApprovalReviewRequest actualApprovalReviewRequest =
                await this.approvalReviewRequestService.RemoveApprovalReviewRequestByIdAsync(
                    inputApprovalReviewRequestId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then: it is withdrawn, and the withdrawer is recorded
            actualApprovalReviewRequest.Should().BeEquivalentTo(expectedApprovalReviewRequest);
            actualApprovalReviewRequest.DeletedBy.Should().Be("a-different-moderator");

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalReviewRequestAsync(
                    auditAppliedApprovalReviewRequest, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// Withdrawing an already-withdrawn request is a no-op rather than an error: the caller
        /// asked for a state the row is already in. Nothing is written and no fact is published,
        /// so a retried withdrawal cannot emit a second <c>-Removed</c>.
        /// </summary>
        [Fact]
        public async Task ShouldNotRewithdrawAnAlreadyWithdrawnApprovalReviewRequestAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalReviewRequest randomApprovalReviewRequest =
                CreateApprovalReviewRequestFiller(randomDateTimeOffset).Create();

            randomApprovalReviewRequest.IsDeleted = true;
            Guid inputApprovalReviewRequestId = randomApprovalReviewRequest.Id;
            ApprovalReviewRequest storageApprovalReviewRequest = randomApprovalReviewRequest;
            ApprovalReviewRequest expectedApprovalReviewRequest = storageApprovalReviewRequest.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestByIdAsync(
                    inputApprovalReviewRequestId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalReviewRequest);

            // when
            ApprovalReviewRequest actualApprovalReviewRequest =
                await this.approvalReviewRequestService.RemoveApprovalReviewRequestByIdAsync(
                    inputApprovalReviewRequestId,
                    deletionReason: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualApprovalReviewRequest.Should().BeEquivalentTo(expectedApprovalReviewRequest);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalReviewRequestAsync(
                    It.IsAny<EventEnvelope<ApprovalReviewRequest>>(),
                    It.IsAny<ApprovalReviewRequestEventOperation>()),
                Times.Never);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }
    }
}
