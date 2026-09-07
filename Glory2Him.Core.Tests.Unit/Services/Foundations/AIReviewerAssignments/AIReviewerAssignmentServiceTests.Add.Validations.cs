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
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.AIReviewerAssignments
{
    public partial class AIReviewerAssignmentServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfAIReviewerAssignmentIsNullAndLogItAsync()
        {
            // given
            AIReviewerAssignment nullAIReviewerAssignment = null;

            var nullAIReviewerAssignmentException =
                new NullAIReviewerAssignmentException(message: "AI reviewer assignment is null.");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: nullAIReviewerAssignmentException);

            // when
            ValueTask<AIReviewerAssignment> addAIReviewerAssignmentTask =
                this.aiReviewerAssignmentService.AddAIReviewerAssignmentAsync(
                    nullAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualAIReviewerAssignmentValidationException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    addAIReviewerAssignmentTask.AsTask);

            // then
            actualAIReviewerAssignmentValidationException.Should().BeEquivalentTo(
                expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
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
        public async Task ShouldThrowValidationExceptionOnAddIfAIReviewerAssignmentIsInvalidAndLogItAsync(
            string invalidText)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            DateTimeOffset startDate = randomDateTimeOffset.AddSeconds(-90);
            DateTimeOffset endDate = randomDateTimeOffset;

            var invalidAIReviewerAssignment = new AIReviewerAssignment
            {
                Id = Guid.Empty,
                ApprovalId = Guid.Empty,
                CreatedBy = invalidText,
                UpdatedBy = invalidText,
                CreatedWhen = default,
                UpdatedWhen = default
            };

            var invalidAIReviewerAssignmentException =
                new InvalidAIReviewerAssignmentException(
                    message: "AI reviewer assignment is invalid, fix the errors and try again.");

            invalidAIReviewerAssignmentException.AddData(
                key: nameof(AIReviewerAssignment.Id),
                values: "Id is required");

            invalidAIReviewerAssignmentException.AddData(
                key: nameof(AIReviewerAssignment.ApprovalId),
                values: "Id is required");

            invalidAIReviewerAssignmentException.AddData(
                key: nameof(AIReviewerAssignment.CreatedBy),
                values: "Text is required");

            invalidAIReviewerAssignmentException.AddData(
                key: nameof(AIReviewerAssignment.UpdatedBy),
                values: "Text is required");

            invalidAIReviewerAssignmentException.AddData(
                key: nameof(AIReviewerAssignment.CreatedWhen),
                values: new[]
                {
                    "Date is required",

                    "Date is not recent. Expected a value between " +
                        $"{startDate} and {endDate} but found {default(DateTimeOffset)}"
                });

            invalidAIReviewerAssignmentException.AddData(
                key: nameof(AIReviewerAssignment.UpdatedWhen),
                values: "Date is required");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAIReviewerAssignmentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    invalidAIReviewerAssignment, It.IsAny<SecurityContext>()))
                        .ReturnsAsync(invalidAIReviewerAssignment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidText);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<AIReviewerAssignment> addAIReviewerAssignmentTask =
                this.aiReviewerAssignmentService.AddAIReviewerAssignmentAsync(
                    invalidAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualAIReviewerAssignmentValidationException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    addAIReviewerAssignmentTask.AsTask);

            // then
            actualAIReviewerAssignmentValidationException.Should().BeEquivalentTo(
                expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfCreatedByIsNotTheActingUserAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            AIReviewerAssignment randomAIReviewerAssignment =
                CreateAIReviewerAssignmentFiller(randomDateTimeOffset).Create();

            AIReviewerAssignment inputAIReviewerAssignment = randomAIReviewerAssignment;
            string someoneElsesUserId = Guid.NewGuid().ToString();

            var invalidAIReviewerAssignmentException =
                new InvalidAIReviewerAssignmentException(
                    message: "AI reviewer assignment is invalid, fix the errors and try again.");

            invalidAIReviewerAssignmentException.AddData(
                key: nameof(AIReviewerAssignment.CreatedBy),
                values: $"Expected value to be '{someoneElsesUserId}' but found " +
                    $"'{inputAIReviewerAssignment.CreatedBy}'.");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAIReviewerAssignmentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    inputAIReviewerAssignment, It.IsAny<SecurityContext>()))
                        .ReturnsAsync(inputAIReviewerAssignment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(someoneElsesUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<AIReviewerAssignment> addAIReviewerAssignmentTask =
                this.aiReviewerAssignmentService.AddAIReviewerAssignmentAsync(
                    inputAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualAIReviewerAssignmentValidationException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    addAIReviewerAssignmentTask.AsTask);

            // then
            actualAIReviewerAssignmentValidationException.Should().BeEquivalentTo(
                expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnAddIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext unauthenticatedSecurityContext)
        {
            // given
            this.ambientSecurityContext = unauthenticatedSecurityContext;
            AIReviewerAssignment someAIReviewerAssignment = CreateRandomAIReviewerAssignment();

            var unauthorizedAIReviewerAssignmentException =
                new UnauthorizedAIReviewerAssignmentException(
                    message: "The current user is not authenticated.");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedAIReviewerAssignmentException);

            // when
            ValueTask<AIReviewerAssignment> addAIReviewerAssignmentTask =
                this.aiReviewerAssignmentService.AddAIReviewerAssignmentAsync(
                    someAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualAIReviewerAssignmentValidationException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    addAIReviewerAssignmentTask.AsTask);

            // then
            actualAIReviewerAssignmentValidationException.Should().BeEquivalentTo(
                expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfUserIsReadOnlyAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.Reviewers, Roles.ReadOnly);

            AIReviewerAssignment someAIReviewerAssignment = CreateRandomAIReviewerAssignment();

            var unauthorizedAIReviewerAssignmentException =
                new UnauthorizedAIReviewerAssignmentException(
                    message: "The current user is blocked from managing AI reviewer assignments.");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedAIReviewerAssignmentException);

            // when
            ValueTask<AIReviewerAssignment> addAIReviewerAssignmentTask =
                this.aiReviewerAssignmentService.AddAIReviewerAssignmentAsync(
                    someAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualAIReviewerAssignmentValidationException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    addAIReviewerAssignmentTask.AsTask);

            // then
            actualAIReviewerAssignmentValidationException.Should().BeEquivalentTo(
                expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(NonReviewRoleSets))]
        public async Task ShouldThrowValidationExceptionOnAddIfUserHasNoReviewRoleAndLogItAsync(
            string[] nonReviewRoles)
        {
            // given: assigning Berean is coordination of the round, so it takes the same tier a
            // human reviewer invitation does — a signed-in reader outside it has none
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(nonReviewRoles);
            AIReviewerAssignment someAIReviewerAssignment = CreateRandomAIReviewerAssignment();

            var unauthorizedAIReviewerAssignmentException =
                new UnauthorizedAIReviewerAssignmentException(
                    message: "The current user is not allowed to manage AI reviewer assignments.");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: unauthorizedAIReviewerAssignmentException);

            // when
            ValueTask<AIReviewerAssignment> addAIReviewerAssignmentTask =
                this.aiReviewerAssignmentService.AddAIReviewerAssignmentAsync(
                    someAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualAIReviewerAssignmentValidationException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    addAIReviewerAssignmentTask.AsTask);

            // then
            actualAIReviewerAssignmentValidationException.Should().BeEquivalentTo(
                expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(), It.IsAny<CancellationToken>()),
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            DateTimeOffset startDate = randomDateTimeOffset.AddSeconds(-90);
            DateTimeOffset endDate = randomDateTimeOffset;

            DateTimeOffset invalidDateTimeOffset =
                randomDateTimeOffset.AddMinutes(minutesBeforeOrAfter);

            AIReviewerAssignment randomAIReviewerAssignment =
                CreateAIReviewerAssignmentFiller(invalidDateTimeOffset).Create();

            AIReviewerAssignment inputAIReviewerAssignment = randomAIReviewerAssignment;

            var invalidAIReviewerAssignmentException =
                new InvalidAIReviewerAssignmentException(
                    message: "AI reviewer assignment is invalid, fix the errors and try again.");

            invalidAIReviewerAssignmentException.AddData(
                key: nameof(AIReviewerAssignment.CreatedWhen),
                values: "Date is not recent. Expected a value between " +
                    $"{startDate} and {endDate} but found {invalidDateTimeOffset}");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAIReviewerAssignmentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    inputAIReviewerAssignment, It.IsAny<SecurityContext>()))
                        .ReturnsAsync(inputAIReviewerAssignment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputAIReviewerAssignment.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<AIReviewerAssignment> addAIReviewerAssignmentTask =
                this.aiReviewerAssignmentService.AddAIReviewerAssignmentAsync(
                    inputAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualAIReviewerAssignmentValidationException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    addAIReviewerAssignmentTask.AsTask);

            // then
            actualAIReviewerAssignmentValidationException.Should().BeEquivalentTo(
                expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfTextExceedsMaxLengthAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            AIReviewerAssignment randomAIReviewerAssignment =
                CreateAIReviewerAssignmentFiller(randomDateTimeOffset).Create();

            string tooLongText = GetRandomStringWithLengthOf(256);
            randomAIReviewerAssignment.CreatedBy = tooLongText;
            randomAIReviewerAssignment.UpdatedBy = tooLongText;
            AIReviewerAssignment inputAIReviewerAssignment = randomAIReviewerAssignment;

            var invalidAIReviewerAssignmentException =
                new InvalidAIReviewerAssignmentException(
                    message: "AI reviewer assignment is invalid, fix the errors and try again.");

            invalidAIReviewerAssignmentException.AddData(
                key: nameof(AIReviewerAssignment.CreatedBy),
                values: "Text exceed max length of 255 characters");

            invalidAIReviewerAssignmentException.AddData(
                key: nameof(AIReviewerAssignment.UpdatedBy),
                values: "Text exceed max length of 255 characters");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: invalidAIReviewerAssignmentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    inputAIReviewerAssignment, It.IsAny<SecurityContext>()))
                        .ReturnsAsync(inputAIReviewerAssignment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(tooLongText);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<AIReviewerAssignment> addAIReviewerAssignmentTask =
                this.aiReviewerAssignmentService.AddAIReviewerAssignmentAsync(
                    inputAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualAIReviewerAssignmentValidationException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    addAIReviewerAssignmentTask.AsTask);

            // then
            actualAIReviewerAssignmentValidationException.Should().BeEquivalentTo(
                expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
