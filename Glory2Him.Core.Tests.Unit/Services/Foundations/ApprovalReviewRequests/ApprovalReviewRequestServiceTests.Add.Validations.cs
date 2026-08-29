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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviewRequests
{
    public partial class ApprovalReviewRequestServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfApprovalReviewRequestIsNullAndLogItAsync()
        {
            // given
            ApprovalReviewRequest nullApprovalReviewRequest = null;

            var nullApprovalReviewRequestException =
                new NullApprovalReviewRequestException(message: "Approval review request is null.");

            var expectedApprovalReviewRequestValidationException =
                new ApprovalReviewRequestValidationException(
                    message: "Approval review request validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: nullApprovalReviewRequestException);

            // when
            ValueTask<ApprovalReviewRequest> addApprovalReviewRequestTask =
                this.approvalReviewRequestService.AddApprovalReviewRequestAsync(
                    nullApprovalReviewRequest,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualApprovalReviewRequestValidationException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    addApprovalReviewRequestTask.AsTask);

            // then
            actualApprovalReviewRequestValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewRequestValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ShouldThrowValidationExceptionOnAddIfApprovalReviewRequestIsInvalidAndLogItAsync(
            string invalidText)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            DateTimeOffset startDate = randomDateTimeOffset.AddSeconds(-90);
            DateTimeOffset endDate = randomDateTimeOffset;

            var invalidApprovalReviewRequest = new ApprovalReviewRequest
            {
                Id = Guid.Empty,
                ApprovalId = Guid.Empty,
                RequestedUserId = invalidText,
                CreatedBy = invalidText,
                UpdatedBy = invalidText,
                CreatedWhen = default,
                UpdatedWhen = default
            };

            var invalidApprovalReviewRequestException =
                new InvalidApprovalReviewRequestException(
                    message: "Approval review request is invalid, fix the errors and try again.");

            invalidApprovalReviewRequestException.AddData(
                key: nameof(ApprovalReviewRequest.Id),
                values: "Id is required");

            invalidApprovalReviewRequestException.AddData(
                key: nameof(ApprovalReviewRequest.ApprovalId),
                values: "Id is required");

            // A request naming nobody invites nobody, and would still occupy a slot in
            // UX_ApprovalReviewRequests_ApprovalId_RequestedUserId.
            invalidApprovalReviewRequestException.AddData(
                key: nameof(ApprovalReviewRequest.RequestedUserId),
                values: "Text is required");

            invalidApprovalReviewRequestException.AddData(
                key: nameof(ApprovalReviewRequest.CreatedBy),
                values: "Text is required");

            invalidApprovalReviewRequestException.AddData(
                key: nameof(ApprovalReviewRequest.UpdatedBy),
                values: "Text is required");

            invalidApprovalReviewRequestException.AddData(
                key: nameof(ApprovalReviewRequest.CreatedWhen),
                values: new[]
                {
                    "Date is required",

                    "Date is not recent. Expected a value between " +
                        $"{startDate} and {endDate} but found {default(DateTimeOffset)}"
                });

            invalidApprovalReviewRequestException.AddData(
                key: nameof(ApprovalReviewRequest.UpdatedWhen),
                values: "Date is required");

            var expectedApprovalReviewRequestValidationException =
                new ApprovalReviewRequestValidationException(
                    message: "Approval review request validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidApprovalReviewRequestException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    invalidApprovalReviewRequest, It.IsAny<SecurityContext>()))
                        .ReturnsAsync(invalidApprovalReviewRequest);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidText);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalReviewRequest> addApprovalReviewRequestTask =
                this.approvalReviewRequestService.AddApprovalReviewRequestAsync(
                    invalidApprovalReviewRequest,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualApprovalReviewRequestValidationException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    addApprovalReviewRequestTask.AsTask);

            // then
            actualApprovalReviewRequestValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewRequestValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// <c>CreatedBy</c> is the REQUESTER and must be the acting user. This is the rule that
        /// keeps §7.9's central claim true — that a request, unlike the placeholder review it
        /// replaces, names its author honestly rather than forging one.
        /// </summary>
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfCreatedByIsNotTheActingUserAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalReviewRequest randomApprovalReviewRequest =
                CreateApprovalReviewRequestFiller(randomDateTimeOffset).Create();

            ApprovalReviewRequest inputApprovalReviewRequest = randomApprovalReviewRequest;
            string someoneElsesUserId = Guid.NewGuid().ToString();

            var invalidApprovalReviewRequestException =
                new InvalidApprovalReviewRequestException(
                    message: "Approval review request is invalid, fix the errors and try again.");

            invalidApprovalReviewRequestException.AddData(
                key: nameof(ApprovalReviewRequest.CreatedBy),
                values: $"Expected value to be '{someoneElsesUserId}' but found " +
                    $"'{inputApprovalReviewRequest.CreatedBy}'.");

            var expectedApprovalReviewRequestValidationException =
                new ApprovalReviewRequestValidationException(
                    message: "Approval review request validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidApprovalReviewRequestException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    inputApprovalReviewRequest, It.IsAny<SecurityContext>()))
                        .ReturnsAsync(inputApprovalReviewRequest);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(someoneElsesUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalReviewRequest> addApprovalReviewRequestTask =
                this.approvalReviewRequestService.AddApprovalReviewRequestAsync(
                    inputApprovalReviewRequest,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualApprovalReviewRequestValidationException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    addApprovalReviewRequestTask.AsTask);

            // then
            actualApprovalReviewRequestValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewRequestValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnAddIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext unauthenticatedSecurityContext)
        {
            // given
            this.ambientSecurityContext = unauthenticatedSecurityContext;

            ApprovalReviewRequest someApprovalReviewRequest = CreateRandomApprovalReviewRequest();

            var unauthorizedApprovalReviewRequestException =
                new UnauthorizedApprovalReviewRequestException(
                    message: "The current user is not authenticated.");

            var expectedApprovalReviewRequestValidationException =
                new ApprovalReviewRequestValidationException(
                    message: "Approval review request validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedApprovalReviewRequestException);

            // when
            ValueTask<ApprovalReviewRequest> addApprovalReviewRequestTask =
                this.approvalReviewRequestService.AddApprovalReviewRequestAsync(
                    someApprovalReviewRequest,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualApprovalReviewRequestValidationException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    addApprovalReviewRequestTask.AsTask);

            // then
            actualApprovalReviewRequestValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewRequestValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfUserIsReadOnlyAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.Reviewer, Roles.ReadOnly);

            ApprovalReviewRequest someApprovalReviewRequest = CreateRandomApprovalReviewRequest();

            var unauthorizedApprovalReviewRequestException =
                new UnauthorizedApprovalReviewRequestException(
                    message: "The current user is blocked from contributing approval review requests.");

            var expectedApprovalReviewRequestValidationException =
                new ApprovalReviewRequestValidationException(
                    message: "Approval review request validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedApprovalReviewRequestException);

            // when
            ValueTask<ApprovalReviewRequest> addApprovalReviewRequestTask =
                this.approvalReviewRequestService.AddApprovalReviewRequestAsync(
                    someApprovalReviewRequest,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualApprovalReviewRequestValidationException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    addApprovalReviewRequestTask.AsTask);

            // then
            actualApprovalReviewRequestValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewRequestValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(NonReviewRoleSets))]
        public async Task ShouldThrowValidationExceptionOnAddIfUserHasNoReviewRoleAndLogItAsync(
            string[] nonReviewRoles)
        {
            // given: inviting somebody is coordination of the round, so it takes a place in the
            // round — a signed-in reader outside the review tier has none
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(nonReviewRoles);
            ApprovalReviewRequest someApprovalReviewRequest = CreateRandomApprovalReviewRequest();

            var unauthorizedApprovalReviewRequestException =
                new UnauthorizedApprovalReviewRequestException(
                    message: "The current user is not allowed to request approval reviews.");

            var expectedApprovalReviewRequestValidationException =
                new ApprovalReviewRequestValidationException(
                    message: "Approval review request validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedApprovalReviewRequestException);

            // when
            ValueTask<ApprovalReviewRequest> addApprovalReviewRequestTask =
                this.approvalReviewRequestService.AddApprovalReviewRequestAsync(
                    someApprovalReviewRequest,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualApprovalReviewRequestValidationException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    addApprovalReviewRequestTask.AsTask);

            // then
            actualApprovalReviewRequestValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewRequestValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// The audit timestamp must sit inside the recency window, in both directions: a stale
        /// replay and a clock-skewed future date are equally suspect on a row whose whole value is
        /// its audit trail.
        /// </summary>
        [Theory]
        [MemberData(nameof(MinutesBeforeOrAfter))]
        public async Task ShouldThrowValidationExceptionOnAddIfCreatedWhenIsNotRecentAndLogItAsync(
            int minutesBeforeOrAfter)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            DateTimeOffset startDate = randomDateTimeOffset.AddSeconds(-90);
            DateTimeOffset endDate = randomDateTimeOffset;

            DateTimeOffset invalidDateTimeOffset =
                randomDateTimeOffset.AddMinutes(minutesBeforeOrAfter);

            ApprovalReviewRequest randomApprovalReviewRequest =
                CreateApprovalReviewRequestFiller(invalidDateTimeOffset).Create();

            ApprovalReviewRequest inputApprovalReviewRequest = randomApprovalReviewRequest;

            var invalidApprovalReviewRequestException =
                new InvalidApprovalReviewRequestException(
                    message: "Approval review request is invalid, fix the errors and try again.");

            invalidApprovalReviewRequestException.AddData(
                key: nameof(ApprovalReviewRequest.CreatedWhen),
                values: "Date is not recent. Expected a value between " +
                    $"{startDate} and {endDate} but found {invalidDateTimeOffset}");

            var expectedApprovalReviewRequestValidationException =
                new ApprovalReviewRequestValidationException(
                    message: "Approval review request validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidApprovalReviewRequestException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    inputApprovalReviewRequest, It.IsAny<SecurityContext>()))
                        .ReturnsAsync(inputApprovalReviewRequest);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputApprovalReviewRequest.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalReviewRequest> addApprovalReviewRequestTask =
                this.approvalReviewRequestService.AddApprovalReviewRequestAsync(
                    inputApprovalReviewRequest,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualApprovalReviewRequestValidationException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    addApprovalReviewRequestTask.AsTask);

            // then
            actualApprovalReviewRequestValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewRequestValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfTextExceedsMaxLengthAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalReviewRequest randomApprovalReviewRequest =
                CreateApprovalReviewRequestFiller(randomDateTimeOffset).Create();

            randomApprovalReviewRequest.RequestedUserId = GetRandomStringWithLengthOf(256);
            randomApprovalReviewRequest.RequestedUserDisplayName = GetRandomStringWithLengthOf(256);
            ApprovalReviewRequest inputApprovalReviewRequest = randomApprovalReviewRequest;

            var invalidApprovalReviewRequestException =
                new InvalidApprovalReviewRequestException(
                    message: "Approval review request is invalid, fix the errors and try again.");

            invalidApprovalReviewRequestException.AddData(
                key: nameof(ApprovalReviewRequest.RequestedUserId),
                values: "Text exceed max length of 255 characters");

            invalidApprovalReviewRequestException.AddData(
                key: nameof(ApprovalReviewRequest.RequestedUserDisplayName),
                values: "Text exceed max length of 255 characters");

            var expectedApprovalReviewRequestValidationException =
                new ApprovalReviewRequestValidationException(
                    message: "Approval review request validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidApprovalReviewRequestException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    inputApprovalReviewRequest, It.IsAny<SecurityContext>()))
                        .ReturnsAsync(inputApprovalReviewRequest);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputApprovalReviewRequest.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalReviewRequest> addApprovalReviewRequestTask =
                this.approvalReviewRequestService.AddApprovalReviewRequestAsync(
                    inputApprovalReviewRequest,
                    TestContext.Current.CancellationToken);

            ApprovalReviewRequestValidationException actualApprovalReviewRequestValidationException =
                await Assert.ThrowsAsync<ApprovalReviewRequestValidationException>(
                    addApprovalReviewRequestTask.AsTask);

            // then
            actualApprovalReviewRequestValidationException.Should().BeEquivalentTo(
                expectedApprovalReviewRequestValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalReviewRequestValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertApprovalReviewRequestAsync(
                    It.IsAny<ApprovalReviewRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
