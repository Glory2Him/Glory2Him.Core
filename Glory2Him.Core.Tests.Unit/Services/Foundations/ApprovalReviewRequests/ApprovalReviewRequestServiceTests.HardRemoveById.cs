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
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests.Exceptions;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviewRequests
{
    public partial class ApprovalReviewRequestServiceTests
    {
        [Fact]
        public async Task ShouldHardRemoveApprovalReviewRequestByIdAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            ApprovalReviewRequest randomApprovalReviewRequest = CreateRandomApprovalReviewRequest();
            Guid inputApprovalReviewRequestId = randomApprovalReviewRequest.Id;
            ApprovalReviewRequest storageApprovalReviewRequest = randomApprovalReviewRequest;
            ApprovalReviewRequest deletedApprovalReviewRequest = storageApprovalReviewRequest.DeepClone();
            ApprovalReviewRequest expectedApprovalReviewRequest = deletedApprovalReviewRequest.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestByIdAsync(
                    inputApprovalReviewRequestId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalReviewRequest);

            this.storageBrokerMock.Setup(broker =>
                broker.DeleteApprovalReviewRequestAsync(
                    storageApprovalReviewRequest, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(deletedApprovalReviewRequest);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalReviewRequestAsync(
                    It.IsAny<EventEnvelope<ApprovalReviewRequest>>(),
                    ApprovalReviewRequestEventOperation.HardRemoved))
                        .Returns(new ValueTask<EventPublishResult<ApprovalReviewRequest>>(
                            new EventPublishResult<ApprovalReviewRequest>()));

            // when
            ApprovalReviewRequest actualApprovalReviewRequest =
                await this.approvalReviewRequestService.HardRemoveApprovalReviewRequestByIdAsync(
                    inputApprovalReviewRequestId,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalReviewRequest.Should().BeEquivalentTo(expectedApprovalReviewRequest);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteApprovalReviewRequestAsync(
                    storageApprovalReviewRequest, It.IsAny<CancellationToken>()),
                Times.Once);

            // The hard removal rides the SAME address as an ordinary withdrawal and is told apart
            // by its composed event name, so a consumer subscribes to one removal address.
            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalReviewRequestAsync(
                    It.IsAny<EventEnvelope<ApprovalReviewRequest>>(),
                    ApprovalReviewRequestEventOperation.HardRemoved),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName == EventBrokerIdentifiers
                            .ApprovalReviewRequestOnHardRemovingApprovalReviewRequestByIdSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        /// <summary>
        /// Hard removal destroys the row and its audit trail, so it is <c>Admin</c>-only —
        /// deliberately narrower than withdrawal, which the whole review tier may perform (§7.9
        /// rule 5). Withdrawal is reversible bookkeeping; this is not.
        /// </summary>
        [Theory]
        [MemberData(nameof(NonAdminRoleSets))]
        public async Task ShouldThrowValidationExceptionOnHardRemoveIfUserIsNotAdminAndLogItAsync(
            string[] nonAdminRoles)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(nonAdminRoles);
            Guid someApprovalReviewRequestId = Guid.NewGuid();

            var unauthorizedApprovalReviewRequestException =
                new UnauthorizedApprovalReviewRequestException(
                    message: "The current user is not allowed to permanently remove this " +
                        "approval review request.");

            var expectedApprovalReviewRequestValidationException =
                new ApprovalReviewRequestValidationException(
                    message: "Approval review request validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedApprovalReviewRequestException);

            // when
            ValueTask<ApprovalReviewRequest> hardRemoveTask =
                this.approvalReviewRequestService.HardRemoveApprovalReviewRequestByIdAsync(
                    someApprovalReviewRequestId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    hardRemoveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowNotFoundExceptionOnHardRemoveIfRequestDoesNotExistAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            Guid someApprovalReviewRequestId = Guid.NewGuid();
            ApprovalReviewRequest noApprovalReviewRequest = null;

            var notFoundApprovalReviewRequestException =
                new NotFoundApprovalReviewRequestException(
                    message: "Approval review request not found with id: " +
                        $"{someApprovalReviewRequestId}.");

            var expectedApprovalReviewRequestValidationException =
                new ApprovalReviewRequestValidationException(
                    message: "Approval review request validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: notFoundApprovalReviewRequestException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestByIdAsync(
                    someApprovalReviewRequestId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(noApprovalReviewRequest);

            // when
            ValueTask<ApprovalReviewRequest> hardRemoveTask =
                this.approvalReviewRequestService.HardRemoveApprovalReviewRequestByIdAsync(
                    someApprovalReviewRequestId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    hardRemoveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnHardRemoveIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext unauthenticatedSecurityContext)
        {
            // given
            this.ambientSecurityContext = unauthenticatedSecurityContext;
            Guid someApprovalReviewRequestId = Guid.NewGuid();

            var unauthorizedApprovalReviewRequestException =
                new UnauthorizedApprovalReviewRequestException(
                    message: "The current user is not authenticated.");

            var expectedApprovalReviewRequestValidationException =
                new ApprovalReviewRequestValidationException(
                    message: "Approval review request validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedApprovalReviewRequestException);

            // when
            ValueTask<ApprovalReviewRequest> hardRemoveTask =
                this.approvalReviewRequestService.HardRemoveApprovalReviewRequestByIdAsync(
                    someApprovalReviewRequestId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    hardRemoveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemoveIfIdIsInvalidAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            Guid invalidApprovalReviewRequestId = Guid.Empty;

            var invalidApprovalReviewRequestException =
                new InvalidApprovalReviewRequestException(
                    message: "Approval review request is invalid, fix the errors and try again.");

            invalidApprovalReviewRequestException.AddData(
                key: nameof(ApprovalReviewRequest.Id),
                values: "Id is required");

            var expectedApprovalReviewRequestValidationException =
                new ApprovalReviewRequestValidationException(
                    message: "Approval review request validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidApprovalReviewRequestException);

            // when
            ValueTask<ApprovalReviewRequest> hardRemoveTask =
                this.approvalReviewRequestService.HardRemoveApprovalReviewRequestByIdAsync(
                    invalidApprovalReviewRequestId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    hardRemoveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewRequestValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestValidationException))),
                Times.Once);
        }

        /// <summary>
        /// Unlike the withdraw path, hard removal is NOT short-circuited by an already-withdrawn
        /// row: destroying it is precisely the point, and refusing would leave withdrawn rows
        /// permanently unclearable.
        /// </summary>
        [Fact]
        public async Task ShouldHardRemoveAnAlreadyWithdrawnApprovalReviewRequestAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            ApprovalReviewRequest randomApprovalReviewRequest = CreateRandomApprovalReviewRequest();
            randomApprovalReviewRequest.IsDeleted = true;
            Guid inputApprovalReviewRequestId = randomApprovalReviewRequest.Id;
            ApprovalReviewRequest storageApprovalReviewRequest = randomApprovalReviewRequest;
            ApprovalReviewRequest deletedApprovalReviewRequest = storageApprovalReviewRequest.DeepClone();
            ApprovalReviewRequest expectedApprovalReviewRequest = deletedApprovalReviewRequest.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewRequestByIdAsync(
                    inputApprovalReviewRequestId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalReviewRequest);

            this.storageBrokerMock.Setup(broker =>
                broker.DeleteApprovalReviewRequestAsync(
                    storageApprovalReviewRequest, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(deletedApprovalReviewRequest);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalReviewRequestAsync(
                    It.IsAny<EventEnvelope<ApprovalReviewRequest>>(),
                    ApprovalReviewRequestEventOperation.HardRemoved))
                        .Returns(new ValueTask<EventPublishResult<ApprovalReviewRequest>>(
                            new EventPublishResult<ApprovalReviewRequest>()));

            // when
            ApprovalReviewRequest actualApprovalReviewRequest =
                await this.approvalReviewRequestService.HardRemoveApprovalReviewRequestByIdAsync(
                    inputApprovalReviewRequestId,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalReviewRequest.Should().BeEquivalentTo(expectedApprovalReviewRequest);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteApprovalReviewRequestAsync(
                    storageApprovalReviewRequest, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
