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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.AIReviewerAssignments
{
    public partial class AIReviewerAssignmentServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfAIReviewerAssignmentIsNullAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            AIReviewerAssignment nullAIReviewerAssignment = null;

            var nullAIReviewerAssignmentException =
                new NullAIReviewerAssignmentException(message: "AI reviewer assignment is null.");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, fix the errors and try again.",
                    innerException: nullAIReviewerAssignmentException);

            // when
            ValueTask<AIReviewerAssignment> modifyAIReviewerAssignmentTask =
                this.aiReviewerAssignmentService.ModifyAIReviewerAssignmentAsync(
                    nullAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualAIReviewerAssignmentValidationException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    modifyAIReviewerAssignmentTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnModifyIfAIReviewerAssignmentIsInvalidAndLogItAsync(
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
                values: "Date is required");

            invalidAIReviewerAssignmentException.AddData(
                key: nameof(AIReviewerAssignment.UpdatedWhen),
                values: new[]
                {
                    "Date is required",
                    "Date is the same as CreatedWhen",

                    "Date is not recent. Expected a value between " +
                        $"{startDate} and {endDate} but found {default(DateTimeOffset)}"
                });

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, fix the errors and try again.",
                    innerException: invalidAIReviewerAssignmentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    invalidAIReviewerAssignment, It.IsAny<SecurityContext>()))
                        .ReturnsAsync(invalidAIReviewerAssignment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidText);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<AIReviewerAssignment> modifyAIReviewerAssignmentTask =
                this.aiReviewerAssignmentService.ModifyAIReviewerAssignmentAsync(
                    invalidAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualAIReviewerAssignmentValidationException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    modifyAIReviewerAssignmentTask.AsTask);

            // then
            actualAIReviewerAssignmentValidationException.Should().BeEquivalentTo(
                expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfAIReviewerAssignmentNotFoundAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            AIReviewerAssignment randomAIReviewerAssignment =
                CreateRandomModifyAIReviewerAssignment(randomDateTimeOffset, randomUserId);

            AIReviewerAssignment nonExistentAIReviewerAssignment = randomAIReviewerAssignment;
            AIReviewerAssignment noAIReviewerAssignment = null;

            var notFoundAIReviewerAssignmentException = new NotFoundAIReviewerAssignmentException(
                message: $"AI reviewer assignment not found with id: {nonExistentAIReviewerAssignment.Id}.");

            var expectedAIReviewerAssignmentValidationException = new AIReviewerAssignmentValidationException(
                message: "AI reviewer assignment validation error occurred, fix the errors and try again.",
                innerException: notFoundAIReviewerAssignmentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    nonExistentAIReviewerAssignment, It.IsAny<SecurityContext>()))
                        .ReturnsAsync(nonExistentAIReviewerAssignment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    nonExistentAIReviewerAssignment.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noAIReviewerAssignment);

            // when
            ValueTask<AIReviewerAssignment> modifyAIReviewerAssignmentTask =
                this.aiReviewerAssignmentService.ModifyAIReviewerAssignmentAsync(
                    nonExistentAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualAIReviewerAssignmentValidationException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    modifyAIReviewerAssignmentTask.AsTask);

            // then
            actualAIReviewerAssignmentValidationException.Should().BeEquivalentTo(
                expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);
        }

        /// <summary>
        /// A row's scope is fixed at creation — which approval it was assigned against — pinned
        /// against storage rather than accepted from the caller, mirroring
        /// ContentItemSetting's scope-pinning rule.
        /// </summary>
        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfStorageApprovalIdNotSameAsInputAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            AIReviewerAssignment randomAIReviewerAssignment =
                CreateRandomModifyAIReviewerAssignment(randomDateTimeOffset, randomUserId);

            AIReviewerAssignment invalidAIReviewerAssignment = randomAIReviewerAssignment;
            AIReviewerAssignment storageAIReviewerAssignment = randomAIReviewerAssignment.DeepClone();
            storageAIReviewerAssignment.ApprovalId = Guid.NewGuid();

            // shifted so the concurrency check (storage.UpdatedWhen must differ from the
            // caller's) does not ALSO fire here — this test is about the ApprovalId pin alone
            storageAIReviewerAssignment.UpdatedWhen =
                storageAIReviewerAssignment.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var invalidAIReviewerAssignmentException = new InvalidAIReviewerAssignmentException(
                message: "AI reviewer assignment is invalid, fix the errors and try again.");

            invalidAIReviewerAssignmentException.AddData(
                key: nameof(AIReviewerAssignment.ApprovalId),
                values: $"Value is not the same as {nameof(AIReviewerAssignment.ApprovalId)}");

            var expectedAIReviewerAssignmentValidationException = new AIReviewerAssignmentValidationException(
                message: "AI reviewer assignment validation error occurred, fix the errors and try again.",
                innerException: invalidAIReviewerAssignmentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    invalidAIReviewerAssignment, It.IsAny<SecurityContext>()))
                        .ReturnsAsync(invalidAIReviewerAssignment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    invalidAIReviewerAssignment.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageAIReviewerAssignment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidAIReviewerAssignment,
                    storageAIReviewerAssignment))
                        .ReturnsAsync(invalidAIReviewerAssignment);

            // when
            ValueTask<AIReviewerAssignment> modifyAIReviewerAssignmentTask =
                this.aiReviewerAssignmentService.ModifyAIReviewerAssignmentAsync(
                    invalidAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualAIReviewerAssignmentValidationException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    modifyAIReviewerAssignmentTask.AsTask);

            // then
            actualAIReviewerAssignmentValidationException.Should().BeEquivalentTo(
                expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfStorageCreatedWhenNotSameAsInputAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            AIReviewerAssignment randomAIReviewerAssignment =
                CreateRandomModifyAIReviewerAssignment(randomDateTimeOffset, randomUserId);

            AIReviewerAssignment invalidAIReviewerAssignment = randomAIReviewerAssignment;
            AIReviewerAssignment storageAIReviewerAssignment = randomAIReviewerAssignment.DeepClone();
            storageAIReviewerAssignment.CreatedWhen = GetRandomDateTimeOffset();
            storageAIReviewerAssignment.UpdatedWhen = GetRandomDateTimeOffset();

            var invalidAIReviewerAssignmentException = new InvalidAIReviewerAssignmentException(
                message: "AI reviewer assignment is invalid, fix the errors and try again.");

            invalidAIReviewerAssignmentException.AddData(
                key: nameof(AIReviewerAssignment.CreatedWhen),
                values: $"Date is not the same as {nameof(AIReviewerAssignment.CreatedWhen)}");

            var expectedAIReviewerAssignmentValidationException = new AIReviewerAssignmentValidationException(
                message: "AI reviewer assignment validation error occurred, fix the errors and try again.",
                innerException: invalidAIReviewerAssignmentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    invalidAIReviewerAssignment, It.IsAny<SecurityContext>()))
                        .ReturnsAsync(invalidAIReviewerAssignment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    invalidAIReviewerAssignment.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageAIReviewerAssignment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidAIReviewerAssignment,
                    storageAIReviewerAssignment))
                        .ReturnsAsync(invalidAIReviewerAssignment);

            // when
            ValueTask<AIReviewerAssignment> modifyAIReviewerAssignmentTask =
                this.aiReviewerAssignmentService.ModifyAIReviewerAssignmentAsync(
                    invalidAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualAIReviewerAssignmentValidationException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    modifyAIReviewerAssignmentTask.AsTask);

            // then
            actualAIReviewerAssignmentValidationException.Should().BeEquivalentTo(
                expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfStorageCreatedByNotSameAsInputAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            AIReviewerAssignment randomAIReviewerAssignment =
                CreateRandomModifyAIReviewerAssignment(randomDateTimeOffset, randomUserId);

            AIReviewerAssignment invalidAIReviewerAssignment = randomAIReviewerAssignment;
            AIReviewerAssignment storageAIReviewerAssignment = randomAIReviewerAssignment.DeepClone();
            storageAIReviewerAssignment.CreatedBy = GetRandomString();

            storageAIReviewerAssignment.UpdatedWhen =
                storageAIReviewerAssignment.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var invalidAIReviewerAssignmentException =
                new InvalidAIReviewerAssignmentException(
                    message: "AI reviewer assignment is invalid, fix the errors and try again.");

            invalidAIReviewerAssignmentException.AddData(
                key: nameof(AIReviewerAssignment.CreatedBy),
                values: $"Text is not the same as {nameof(AIReviewerAssignment.CreatedBy)}");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, fix the errors and try again.",
                    innerException: invalidAIReviewerAssignmentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    invalidAIReviewerAssignment, It.IsAny<SecurityContext>()))
                        .ReturnsAsync(invalidAIReviewerAssignment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    invalidAIReviewerAssignment.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageAIReviewerAssignment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidAIReviewerAssignment,
                    storageAIReviewerAssignment))
                        .ReturnsAsync(invalidAIReviewerAssignment);

            // when
            ValueTask<AIReviewerAssignment> modifyAIReviewerAssignmentTask =
                this.aiReviewerAssignmentService.ModifyAIReviewerAssignmentAsync(
                    invalidAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualAIReviewerAssignmentValidationException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    modifyAIReviewerAssignmentTask.AsTask);

            // then
            actualAIReviewerAssignmentValidationException.Should().BeEquivalentTo(
                expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfStorageUpdatedWhenSameAsInputAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            AIReviewerAssignment randomAIReviewerAssignment =
                CreateRandomModifyAIReviewerAssignment(randomDateTimeOffset, randomUserId);

            AIReviewerAssignment invalidAIReviewerAssignment = randomAIReviewerAssignment;
            AIReviewerAssignment storageAIReviewerAssignment = randomAIReviewerAssignment.DeepClone();

            var invalidAIReviewerAssignmentException =
                new InvalidAIReviewerAssignmentException(
                    message: "AI reviewer assignment is invalid, fix the errors and try again.");

            invalidAIReviewerAssignmentException.AddData(
                key: nameof(AIReviewerAssignment.UpdatedWhen),
                values: $"Date is the same as {nameof(AIReviewerAssignment.UpdatedWhen)}");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, fix the errors and try again.",
                    innerException: invalidAIReviewerAssignmentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    invalidAIReviewerAssignment, It.IsAny<SecurityContext>()))
                        .ReturnsAsync(invalidAIReviewerAssignment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    invalidAIReviewerAssignment.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageAIReviewerAssignment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidAIReviewerAssignment,
                    storageAIReviewerAssignment))
                        .ReturnsAsync(invalidAIReviewerAssignment);

            // when
            ValueTask<AIReviewerAssignment> modifyAIReviewerAssignmentTask =
                this.aiReviewerAssignmentService.ModifyAIReviewerAssignmentAsync(
                    invalidAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualAIReviewerAssignmentValidationException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    modifyAIReviewerAssignmentTask.AsTask);

            // then
            actualAIReviewerAssignmentValidationException.Should().BeEquivalentTo(
                expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfUpdatedByIsNotSameAsCurrentUserIdAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            string randomUserId = GetRandomString();
            string differentUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            AIReviewerAssignment randomAIReviewerAssignment =
                CreateRandomModifyAIReviewerAssignment(randomDateTimeOffset, randomUserId);

            AIReviewerAssignment invalidAIReviewerAssignment = randomAIReviewerAssignment;
            invalidAIReviewerAssignment.UpdatedBy = differentUserId;

            var invalidAIReviewerAssignmentException =
                new InvalidAIReviewerAssignmentException(
                    message: "AI reviewer assignment is invalid, fix the errors and try again.");

            invalidAIReviewerAssignmentException.AddData(
                key: nameof(AIReviewerAssignment.UpdatedBy),
                values: $"Expected value to be '{randomUserId}' but found '{differentUserId}'.");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, fix the errors and try again.",
                    innerException: invalidAIReviewerAssignmentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    invalidAIReviewerAssignment, It.IsAny<SecurityContext>()))
                        .ReturnsAsync(invalidAIReviewerAssignment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<AIReviewerAssignment> modifyAIReviewerAssignmentTask =
                this.aiReviewerAssignmentService.ModifyAIReviewerAssignmentAsync(
                    invalidAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualAIReviewerAssignmentValidationException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    modifyAIReviewerAssignmentTask.AsTask);

            // then
            actualAIReviewerAssignmentValidationException.Should().BeEquivalentTo(
                expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfUpdatedWhenIsSameAsCreatedWhenAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            AIReviewerAssignment randomAIReviewerAssignment =
                CreateRandomModifyAIReviewerAssignment(randomDateTimeOffset, randomUserId);

            AIReviewerAssignment invalidAIReviewerAssignment = randomAIReviewerAssignment;
            DateTimeOffset startDate = randomDateTimeOffset.AddSeconds(-90);
            DateTimeOffset endDate = randomDateTimeOffset;
            invalidAIReviewerAssignment.UpdatedWhen = invalidAIReviewerAssignment.CreatedWhen;

            var invalidAIReviewerAssignmentException =
                new InvalidAIReviewerAssignmentException(
                    message: "AI reviewer assignment is invalid, fix the errors and try again.");

            invalidAIReviewerAssignmentException.AddData(
                key: nameof(AIReviewerAssignment.UpdatedWhen),
                values: new[]
                {
                    $"Date is the same as {nameof(AIReviewerAssignment.CreatedWhen)}",
                    $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                        $"but found {invalidAIReviewerAssignment.UpdatedWhen}"
                });

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, fix the errors and try again.",
                    innerException: invalidAIReviewerAssignmentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    invalidAIReviewerAssignment, It.IsAny<SecurityContext>()))
                        .ReturnsAsync(invalidAIReviewerAssignment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<AIReviewerAssignment> modifyAIReviewerAssignmentTask =
                this.aiReviewerAssignmentService.ModifyAIReviewerAssignmentAsync(
                    invalidAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualAIReviewerAssignmentValidationException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    modifyAIReviewerAssignmentTask.AsTask);

            // then
            actualAIReviewerAssignmentValidationException.Should().BeEquivalentTo(
                expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);
        }

        [Theory]
        [MemberData(nameof(MinutesBeforeOrAfter))]
        public async Task ShouldThrowValidationExceptionOnModifyIfUpdatedWhenIsNotRecentAndLogItAsync(
            int minutes)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            AIReviewerAssignment randomAIReviewerAssignment =
                CreateRandomModifyAIReviewerAssignment(randomDateTimeOffset, randomUserId);

            AIReviewerAssignment invalidAIReviewerAssignment = randomAIReviewerAssignment;
            DateTimeOffset startDate = randomDateTimeOffset.AddSeconds(-90);
            DateTimeOffset endDate = randomDateTimeOffset;
            invalidAIReviewerAssignment.UpdatedWhen = randomDateTimeOffset.AddMinutes(minutes);

            var invalidAIReviewerAssignmentException =
                new InvalidAIReviewerAssignmentException(
                    message: "AI reviewer assignment is invalid, fix the errors and try again.");

            invalidAIReviewerAssignmentException.AddData(
                key: nameof(AIReviewerAssignment.UpdatedWhen),
                values: $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                    $"but found {invalidAIReviewerAssignment.UpdatedWhen}");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, fix the errors and try again.",
                    innerException: invalidAIReviewerAssignmentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    invalidAIReviewerAssignment, It.IsAny<SecurityContext>()))
                        .ReturnsAsync(invalidAIReviewerAssignment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<AIReviewerAssignment> modifyAIReviewerAssignmentTask =
                this.aiReviewerAssignmentService.ModifyAIReviewerAssignmentAsync(
                    invalidAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualAIReviewerAssignmentValidationException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    modifyAIReviewerAssignmentTask.AsTask);

            // then
            actualAIReviewerAssignmentValidationException.Should().BeEquivalentTo(
                expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfTextExceedsMaxLengthAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            string randomUserId = GetRandomStringWithLengthOf(256);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            AIReviewerAssignment invalidAIReviewerAssignment =
                CreateRandomModifyAIReviewerAssignment(randomDateTimeOffset, randomUserId);

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
                    message: "AI reviewer assignment validation error occurred, fix the errors and try again.",
                    innerException: invalidAIReviewerAssignmentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    invalidAIReviewerAssignment, It.IsAny<SecurityContext>()))
                        .ReturnsAsync(invalidAIReviewerAssignment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<AIReviewerAssignment> modifyAIReviewerAssignmentTask =
                this.aiReviewerAssignmentService.ModifyAIReviewerAssignmentAsync(
                    invalidAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualAIReviewerAssignmentValidationException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    modifyAIReviewerAssignmentTask.AsTask);

            // then
            actualAIReviewerAssignmentValidationException.Should().BeEquivalentTo(
                expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnModifyIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext invalidSecurityContext)
        {
            // given
            this.ambientSecurityContext = invalidSecurityContext;
            AIReviewerAssignment someAIReviewerAssignment = CreateRandomAIReviewerAssignment();

            var unauthorizedAIReviewerAssignmentException = new UnauthorizedAIReviewerAssignmentException(
                message: "The current user is not authenticated.");

            var expectedAIReviewerAssignmentValidationException = new AIReviewerAssignmentValidationException(
                message: "AI reviewer assignment validation error occurred, fix the errors and try again.",
                innerException: unauthorizedAIReviewerAssignmentException);

            // when
            ValueTask<AIReviewerAssignment> modifyAIReviewerAssignmentTask =
                this.aiReviewerAssignmentService.ModifyAIReviewerAssignmentAsync(
                    someAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualAIReviewerAssignmentValidationException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    modifyAIReviewerAssignmentTask.AsTask);

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
        [MemberData(nameof(NonReviewRoleSets))]
        public async Task ShouldThrowValidationExceptionOnModifyIfUserHasNoReviewRoleAndLogItAsync(
            string[] nonReviewRoles)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(nonReviewRoles);
            AIReviewerAssignment someAIReviewerAssignment = CreateRandomAIReviewerAssignment();

            var unauthorizedAIReviewerAssignmentException = new UnauthorizedAIReviewerAssignmentException(
                message: "The current user is not allowed to manage AI reviewer assignments.");

            var expectedAIReviewerAssignmentValidationException = new AIReviewerAssignmentValidationException(
                message: "AI reviewer assignment validation error occurred, fix the errors and try again.",
                innerException: unauthorizedAIReviewerAssignmentException);

            // when
            ValueTask<AIReviewerAssignment> modifyAIReviewerAssignmentTask =
                this.aiReviewerAssignmentService.ModifyAIReviewerAssignmentAsync(
                    someAIReviewerAssignment,
                    TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualAIReviewerAssignmentValidationException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    modifyAIReviewerAssignmentTask.AsTask);

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
    }
}
