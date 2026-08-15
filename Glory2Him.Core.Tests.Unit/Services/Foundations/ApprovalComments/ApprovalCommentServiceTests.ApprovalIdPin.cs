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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldThrowValidationExceptionOnModifyIfIsResolvedWasChangedAndLogItAsync(
            bool storedResolution)
        {
            // given: IsResolved belongs to the resolve transition. Flipping it through modify
            // would move the gate RequireReviewCommentResolutionBeforeApprovals holds shut while
            // publishing ApprovalComment-Modified — anything watching the resolution address for
            // that gate would never hear about it. Both directions are pinned: reopening a
            // question by stealth is the same hole as answering one.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            ApprovalComment invalidApprovalComment =
                CreateRandomModifyApprovalComment(randomDateTimeOffset, randomUserId);

            invalidApprovalComment.IsResolved = storedResolution is false;

            ApprovalComment storageApprovalComment = invalidApprovalComment.DeepClone();
            storageApprovalComment.UpdatedWhen =
                storageApprovalComment.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            storageApprovalComment.IsResolved = storedResolution;

            var invalidApprovalCommentException =
                new InvalidApprovalCommentException(
                    message: "Approval comment is invalid, fix the errors and try again.");

            invalidApprovalCommentException.AddData(
                key: nameof(ApprovalComment.IsResolved),
                values: $"Flag is not the same as {nameof(ApprovalComment.IsResolved)}");

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
