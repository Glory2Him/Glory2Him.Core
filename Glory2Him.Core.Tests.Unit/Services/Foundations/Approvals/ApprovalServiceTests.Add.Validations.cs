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
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Approvals.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Approvals
{
    public partial class ApprovalServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfApprovalIsNullAndLogItAsync()
        {
            // given
            Approval nullApproval = null;

            var nullApprovalException =
                new NullApprovalException(message: "Approval is null.");

            var expectedApprovalValidationException =
                new ApprovalValidationException(
                    message: "Approval validation error occurred, fix the errors and try again.",
                    innerException: nullApprovalException);

            // when
            ValueTask<Approval> addApprovalTask =
                this.approvalService.AddApprovalAsync(
                    nullApproval,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    addApprovalTask.AsTask);

            // then
            actualApprovalValidationException.Should().BeEquivalentTo(
                expectedApprovalValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalValidationException))),
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
        public async Task ShouldThrowValidationExceptionOnAddIfApprovalIsInvalidAndLogItAsync(
            string invalidText)
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            DateTimeOffset startDate = randomDateTimeOffset.AddSeconds(-90);
            DateTimeOffset endDate = randomDateTimeOffset;

            var invalidApproval = new Approval
            {
                Id = Guid.Empty,
                EntityId = Guid.Empty,
                CreatedBy = invalidText,
                UpdatedBy = invalidText,
                CreatedWhen = default,
                UpdatedWhen = default
            };

            var invalidApprovalException =
                new InvalidApprovalException(
                    message: "Approval is invalid, fix the errors and try again.");

            invalidApprovalException.AddData(
                key: nameof(Approval.Id),
                values: "Id is required");

            invalidApprovalException.AddData(
                key: nameof(Approval.EntityId),
                values: "Id is required");

            invalidApprovalException.AddData(
                key: nameof(Approval.CreatedBy),
                values: "Text is required");

            invalidApprovalException.AddData(
                key: nameof(Approval.UpdatedBy),
                values: "Text is required");

            invalidApprovalException.AddData(
                key: nameof(Approval.CreatedWhen),
                values: new[]
                {
                    "Date is required",

                    "Date is not recent. Expected a value between " +
                        $"{startDate} and {endDate} but found {default(DateTimeOffset)}"
                });

            invalidApprovalException.AddData(
                key: nameof(Approval.UpdatedWhen),
                values: "Date is required");

            var expectedApprovalValidationException =
                new ApprovalValidationException(
                    message: "Approval validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApproval, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApproval);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidText);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Approval> addApprovalTask =
                this.approvalService.AddApprovalAsync(
                    invalidApproval,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    addApprovalTask.AsTask);

            // then
            actualApprovalValidationException.Should().BeEquivalentTo(
                expectedApprovalValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApproval, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfApprovalExceedsMaxLengthAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomStringWithLengthOf(256);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Approval invalidApproval = CreateApprovalFiller(randomDateTimeOffset, randomUserId).Create();

            var invalidApprovalException =
                new InvalidApprovalException(
                    message: "Approval is invalid, fix the errors and try again.");

            invalidApprovalException.AddData(
                key: nameof(Approval.CreatedBy),
                values: $"Text exceed max length of {invalidApproval.CreatedBy.Length - 1} characters");

            invalidApprovalException.AddData(
                key: nameof(Approval.UpdatedBy),
                values: $"Text exceed max length of {invalidApproval.UpdatedBy.Length - 1} characters");

            var expectedApprovalValidationException =
                new ApprovalValidationException(
                    message: "Approval validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApproval, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApproval);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Approval> addApprovalTask =
                this.approvalService.AddApprovalAsync(
                    invalidApproval,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    addApprovalTask.AsTask);

            // then
            actualApprovalValidationException.Should().BeEquivalentTo(
                expectedApprovalValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApproval, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfUpdatedWhenIsNotSameAsCreatedWhenAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Approval randomApproval = CreateApprovalFiller(randomDateTimeOffset, randomUserId).Create();
            Approval invalidApproval = randomApproval;
            invalidApproval.UpdatedWhen = GetRandomDateTimeOffset();

            var invalidApprovalException =
                new InvalidApprovalException(
                    message: "Approval is invalid, fix the errors and try again.");

            invalidApprovalException.AddData(
                key: nameof(Approval.UpdatedWhen),
                values: $"Date is not the same as {nameof(Approval.CreatedWhen)}");

            var expectedApprovalValidationException =
                new ApprovalValidationException(
                    message: "Approval validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApproval, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApproval);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Approval> addApprovalTask =
                this.approvalService.AddApprovalAsync(
                    invalidApproval,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    addApprovalTask.AsTask);

            // then
            actualApprovalValidationException.Should().BeEquivalentTo(
                expectedApprovalValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApproval, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfCreatedByIsNotSameAsCurrentUserIdAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomString();
            string differentUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Approval randomApproval = CreateApprovalFiller(randomDateTimeOffset, randomUserId).Create();
            Approval invalidApproval = randomApproval;
            invalidApproval.CreatedBy = differentUserId;
            invalidApproval.UpdatedBy = differentUserId;

            var invalidApprovalException =
                new InvalidApprovalException(
                    message: "Approval is invalid, fix the errors and try again.");

            invalidApprovalException.AddData(
                key: nameof(Approval.CreatedBy),
                values: $"Expected value to be '{randomUserId}' but found '{differentUserId}'.");

            var expectedApprovalValidationException =
                new ApprovalValidationException(
                    message: "Approval validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApproval, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApproval);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Approval> addApprovalTask =
                this.approvalService.AddApprovalAsync(
                    invalidApproval,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    addApprovalTask.AsTask);

            // then
            actualApprovalValidationException.Should().BeEquivalentTo(
                expectedApprovalValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApproval, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfUpdatedByIsNotSameAsCreatedByAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Approval randomApproval = CreateApprovalFiller(randomDateTimeOffset, randomUserId).Create();
            Approval invalidApproval = randomApproval;
            invalidApproval.UpdatedBy = GetRandomString();

            var invalidApprovalException =
                new InvalidApprovalException(
                    message: "Approval is invalid, fix the errors and try again.");

            invalidApprovalException.AddData(
                key: nameof(Approval.UpdatedBy),
                values: $"Text is not the same as {nameof(Approval.CreatedBy)}");

            var expectedApprovalValidationException =
                new ApprovalValidationException(
                    message: "Approval validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApproval, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApproval);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Approval> addApprovalTask =
                this.approvalService.AddApprovalAsync(
                    invalidApproval,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    addApprovalTask.AsTask);

            // then
            actualApprovalValidationException.Should().BeEquivalentTo(
                expectedApprovalValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApproval, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(MinutesBeforeOrAfter))]
        public async Task ShouldThrowValidationExceptionOnAddIfCreatedWhenIsNotRecentAndLogItAsync(int minutes)
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Approval randomApproval = CreateApprovalFiller(randomDateTimeOffset, randomUserId).Create();
            Approval invalidApproval = randomApproval;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidApproval.CreatedWhen = randomDateTimeOffset.AddMinutes(minutes);
            invalidApproval.UpdatedWhen = invalidApproval.CreatedWhen;

            var invalidApprovalException =
                new InvalidApprovalException(
                    message: "Approval is invalid, fix the errors and try again.");

            invalidApprovalException.AddData(
                key: nameof(Approval.CreatedWhen),
                values: $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                    $"but found {invalidApproval.CreatedWhen}");

            var expectedApprovalValidationException =
                new ApprovalValidationException(
                    message: "Approval validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApproval, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApproval);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<Approval> addApprovalTask =
                this.approvalService.AddApprovalAsync(
                    invalidApproval,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    addApprovalTask.AsTask);

            // then
            actualApprovalValidationException.Should().BeEquivalentTo(
                expectedApprovalValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApproval, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnAddIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext invalidSecurityContext)
        {
            // given
            this.ambientSecurityContext = invalidSecurityContext;
            Approval someApproval = CreateRandomApproval();

            var unauthorizedApprovalException = new UnauthorizedApprovalException(
                message: "The current user is not authenticated.");

            var expectedApprovalValidationException = new ApprovalValidationException(
                message: "Approval validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalException);

            // when
            ValueTask<Approval> addApprovalTask =
                this.approvalService.AddApprovalAsync(
                    someApproval,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    addApprovalTask.AsTask);

            // then
            actualApprovalValidationException.Should().BeEquivalentTo(
                expectedApprovalValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfUserIsBlockedFromContributingAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.ReadOnly);
            Approval someApproval = CreateRandomApproval();

            var unauthorizedApprovalException = new UnauthorizedApprovalException(
                message: "The current user is blocked from contributing approvals.");

            var expectedApprovalValidationException = new ApprovalValidationException(
                message: "Approval validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalException);

            // when
            ValueTask<Approval> addApprovalTask =
                this.approvalService.AddApprovalAsync(
                    someApproval,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualApprovalValidationException =
                await Assert.ThrowsAsync<ApprovalValidationException>(
                    addApprovalTask.AsTask);

            // then
            actualApprovalValidationException.Should().BeEquivalentTo(
                expectedApprovalValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// An approval is born undecided: Draft and Submitted are the contributable statuses,
        /// and the remaining three are the workflow's to record through the modify-side
        /// decision gate. A row inserted already Approved would skip that gate entirely.
        /// </summary>
        [Theory]
        [InlineData(ApprovalStatus.Draft, false)]
        [InlineData(ApprovalStatus.Submitted, false)]
        [InlineData(ApprovalStatus.Approved, true)]
        [InlineData(ApprovalStatus.Rejected, true)]
        [InlineData(ApprovalStatus.Dismissed, true)]
        public async Task ShouldRefuseAnApprovalThatArrivesDecidedOnAddAsync(
            ApprovalStatus approvalStatus,
            bool expectRefusal)
        {
            // given: everything else deliberately blank, so the run always throws and the only
            // question is whether ApprovalStatus is among the reported errors
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            var invalidApproval = new Approval
            {
                ApprovalStatus = approvalStatus,
            };

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(invalidApproval);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(GetRandomString());

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Approval> addApprovalTask =
                this.approvalService.AddApprovalAsync(
                    invalidApproval,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualException =
                await Assert.ThrowsAsync<ApprovalValidationException>(addApprovalTask.AsTask);

            // then
            bool statusWasRefused = actualException.InnerException!.Data.Keys
                .Cast<string>()
                .Contains(nameof(Approval.ApprovalStatus));

            statusWasRefused.Should().Be(expectRefusal);
        }

        /// <summary>
        /// The bypass pair is the §8.6.1 decision's to derive (§9.7.5), so neither half may
        /// arrive on add — a row inserted with the pair set would attest that conditions were
        /// waived when no decision ever ran, and nothing on an Approval row rewrites the pair
        /// afterwards. The over-long case asserts BOTH messages so the 500 cap on the add path
        /// cannot silently vanish behind the not-allowed rule.
        /// </summary>
        [Fact]
        public async Task ShouldRefuseTheBypassPairOnAddAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            var invalidApproval = new Approval
            {
                IsApprovedByBypass = true,
                ApprovedByBypassReason = new string('x', 501),
            };

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(invalidApproval);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(GetRandomString());

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Approval> addApprovalTask =
                this.approvalService.AddApprovalAsync(
                    invalidApproval,
                    TestContext.Current.CancellationToken);

            ApprovalValidationException actualException =
                await Assert.ThrowsAsync<ApprovalValidationException>(addApprovalTask.AsTask);

            // then
            IDictionary actualData = actualException.InnerException!.Data;

            actualData.Keys.Cast<string>()
                .Should().Contain(nameof(Approval.IsApprovedByBypass));

            ((IEnumerable<string>)actualData[nameof(Approval.ApprovedByBypassReason)]!)
                .Should().BeEquivalentTo(
                    "Text exceed max length of 500 characters",
                    "Text is not allowed on add");

            this.storageBrokerMock.Verify(broker =>
                    broker.InsertApprovalAsync(
                        It.IsAny<Approval>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

    }
}
