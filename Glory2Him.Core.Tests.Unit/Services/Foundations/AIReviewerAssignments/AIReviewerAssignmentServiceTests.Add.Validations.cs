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

        /// <summary>
        /// An assignment is born pending (design §8.6.2): the flag records that Berean's pass ran,
        /// and at the moment the row is created it has not. A caller who sends it already set is
        /// claiming work that never happened, so the add is refused rather than quietly cleared —
        /// the caller's request and the row it would produce are not the same thing, and it is
        /// entitled to know.
        /// </summary>
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfIsAIReviewCompletedIsSetAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            AIReviewerAssignment randomAIReviewerAssignment =
                CreateAIReviewerAssignmentFiller(randomDateTimeOffset).Create();

            // the comments flag is left false so this test answers for the completion rule alone
            randomAIReviewerAssignment.IsAIReviewCompleted = true;
            AIReviewerAssignment inputAIReviewerAssignment = randomAIReviewerAssignment;

            var invalidAIReviewerAssignmentException =
                new InvalidAIReviewerAssignmentException(
                    message: "AI reviewer assignment is invalid, fix the errors and try again.");

            invalidAIReviewerAssignmentException.AddData(
                key: nameof(AIReviewerAssignment.IsAIReviewCompleted),
                values: "Value is not allowed on add");

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

        /// <summary>
        /// The comments flag is refused on add for the same reason its companion is, and the row
        /// arriving fully "finished" — both flags set, the shape a system re-import would take —
        /// is refused on both counts at once rather than on whichever the rule list reaches first.
        /// The comments rule cannot be observed alone on this path: clearing completion to isolate
        /// it trips the pairing invariant instead, which is what the next test asserts.
        /// </summary>
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfIsAIReviewCommentsPresentIsSetAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            AIReviewerAssignment randomAIReviewerAssignment =
                CreateAIReviewerAssignmentFiller(randomDateTimeOffset).Create();

            randomAIReviewerAssignment.IsAIReviewCompleted = true;
            randomAIReviewerAssignment.IsAIReviewCommentsPresent = true;
            AIReviewerAssignment inputAIReviewerAssignment = randomAIReviewerAssignment;

            var invalidAIReviewerAssignmentException =
                new InvalidAIReviewerAssignmentException(
                    message: "AI reviewer assignment is invalid, fix the errors and try again.");

            invalidAIReviewerAssignmentException.AddData(
                key: nameof(AIReviewerAssignment.IsAIReviewCompleted),
                values: "Value is not allowed on add");

            invalidAIReviewerAssignmentException.AddData(
                key: nameof(AIReviewerAssignment.IsAIReviewCommentsPresent),
                values: "Value is not allowed on add");

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

        /// <summary>
        /// Comments present without a completed pass is an impossible row — the flag records
        /// something Berean left behind, and a pass that never finished left nothing — so the
        /// invariant is stated on the add path as well as the modify one. Here it is redundant:
        /// the birth-state rule refuses the same input first, and the caller is told both things,
        /// under the one key, in the order the rule list declares them. Asserting the pair pins
        /// that redundancy deliberately, so a later relaxation of the birth-state rules does not
        /// silently take the invariant with it.
        /// </summary>
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfCommentsPresentWithoutCompletionAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            AIReviewerAssignment randomAIReviewerAssignment =
                CreateAIReviewerAssignmentFiller(randomDateTimeOffset).Create();

            randomAIReviewerAssignment.IsAIReviewCompleted = false;
            randomAIReviewerAssignment.IsAIReviewCommentsPresent = true;
            AIReviewerAssignment inputAIReviewerAssignment = randomAIReviewerAssignment;

            var invalidAIReviewerAssignmentException =
                new InvalidAIReviewerAssignmentException(
                    message: "AI reviewer assignment is invalid, fix the errors and try again.");

            invalidAIReviewerAssignmentException.AddData(
                key: nameof(AIReviewerAssignment.IsAIReviewCommentsPresent),
                values: new[]
                {
                    "Value is not allowed on add",
                    "Comments present requires a completed AI review."
                });

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
    }
}
