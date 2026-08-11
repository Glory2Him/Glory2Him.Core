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
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalComments.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalComments
{
    public partial class ApprovalCommentServiceTests
    {
        // ── A comment's author and its approval are not the caller's to choose ───────
        //
        // Comments became load-bearing for the approval gate once
        // RequireReviewCommentResolutionBeforeApprovals settled: that setting blocks approval
        // until every ApprovalComment.IsResolved is true. So a comment attributed to someone
        // else, or moved onto another approval, moves that gate — attribution here is an
        // integrity rule, not a display detail.

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfUserIdIsNotSameAsCurrentUserIdAndLogItAsync()
        {
            // given: CreatedBy is left matching the caller so the failure can only come from
            // the UserId rule under test. Bound rather than stamped, matching how every other
            // actor fact in this codebase is handled: a caller who meant to attribute the
            // comment elsewhere gets the mismatch back by name instead of a silent rewrite.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            string randomUserId = GetRandomString();
            string differentUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalComment invalidApprovalComment =
                CreateApprovalCommentFiller(randomDateTimeOffset, randomUserId).Create();

            invalidApprovalComment.UserId = differentUserId;

            var invalidApprovalCommentException =
                new InvalidApprovalCommentException(
                    message: "Approval comment is invalid, fix the errors and try again.");

            invalidApprovalCommentException.AddData(
                key: nameof(ApprovalComment.UserId),
                values: $"Expected value to be '{randomUserId}' but found '{differentUserId}'.");

            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidApprovalComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalComment> addApprovalCommentTask =
                this.approvalCommentService.AddApprovalCommentAsync(
                    invalidApprovalComment,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualApprovalCommentValidationException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    addApprovalCommentTask.AsTask);

            // then
            actualApprovalCommentValidationException.Should().BeEquivalentTo(
                expectedApprovalCommentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertApprovalCommentAsync(
                    It.IsAny<ApprovalComment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldAddApprovalCommentWhenUserIdIsTheCurrentUserAsync()
        {
            // given: the paired positive — the rule must admit the honest case, or it is
            // just an outage
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalComment inputApprovalComment =
                CreateApprovalCommentFiller(randomDateTimeOffset, randomUserId).Create();

            inputApprovalComment.UserId = randomUserId;
            ApprovalComment insertedApprovalComment = inputApprovalComment.DeepClone();
            ApprovalComment expectedApprovalComment = insertedApprovalComment.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputApprovalComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertApprovalCommentAsync(inputApprovalComment, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(insertedApprovalComment);

            // when
            ApprovalComment actualApprovalComment =
                await this.approvalCommentService.AddApprovalCommentAsync(
                    inputApprovalComment,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalComment.Should().BeEquivalentTo(expectedApprovalComment);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertApprovalCommentAsync(inputApprovalComment, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfUserIdWasRePointedAndLogItAsync()
        {
            // given: pinned against STORAGE rather than against the caller, because an Admin
            // may legitimately amend anyone's comment — but correcting the text must not mean
            // moving the comment onto someone else's name
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            ApprovalComment invalidApprovalComment =
                CreateRandomModifyApprovalComment(randomDateTimeOffset, randomUserId);

            ApprovalComment storageApprovalComment = invalidApprovalComment.DeepClone();
            storageApprovalComment.UpdatedWhen =
                storageApprovalComment.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            storageApprovalComment.UserId = GetRandomString();

            var invalidApprovalCommentException =
                new InvalidApprovalCommentException(
                    message: "Approval comment is invalid, fix the errors and try again.");

            invalidApprovalCommentException.AddData(
                key: nameof(ApprovalComment.UserId),
                values: $"Text is not the same as {nameof(ApprovalComment.UserId)}");

            await AssertModifyIsRefusedAsync(
                invalidApprovalComment,
                storageApprovalComment,
                invalidApprovalCommentException,
                randomUserId,
                randomDateTimeOffset);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfApprovalIdWasRePointedAndLogItAsync()
        {
            // given: moving a comment onto another approval moves the gate it belongs to —
            // an unresolved comment could be walked off the approval it was blocking
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            ApprovalComment invalidApprovalComment =
                CreateRandomModifyApprovalComment(randomDateTimeOffset, randomUserId);

            ApprovalComment storageApprovalComment = invalidApprovalComment.DeepClone();
            storageApprovalComment.UpdatedWhen =
                storageApprovalComment.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            storageApprovalComment.ApprovalId = Guid.NewGuid();

            var invalidApprovalCommentException =
                new InvalidApprovalCommentException(
                    message: "Approval comment is invalid, fix the errors and try again.");

            invalidApprovalCommentException.AddData(
                key: nameof(ApprovalComment.ApprovalId),
                values: $"Id is not the same as {nameof(ApprovalComment.ApprovalId)}");

            await AssertModifyIsRefusedAsync(
                invalidApprovalComment,
                storageApprovalComment,
                invalidApprovalCommentException,
                randomUserId,
                randomDateTimeOffset);
        }

        private async Task AssertModifyIsRefusedAsync(
            ApprovalComment invalidApprovalComment,
            ApprovalComment storageApprovalComment,
            InvalidApprovalCommentException invalidApprovalCommentException,
            string actorUserId,
            DateTimeOffset currentDateTimeOffset)
        {
            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(actorUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    invalidApprovalComment.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidApprovalComment,
                    storageApprovalComment))
                        .ReturnsAsync(invalidApprovalComment);

            // when
            ValueTask<ApprovalComment> modifyApprovalCommentTask =
                this.approvalCommentService.ModifyApprovalCommentAsync(
                    invalidApprovalComment,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualApprovalCommentValidationException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    modifyApprovalCommentTask.AsTask);

            // then
            actualApprovalCommentValidationException.Should().BeEquivalentTo(
                expectedApprovalCommentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalCommentAsync(
                    It.IsAny<ApprovalComment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
