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
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalComments.Exceptions;
using Moq;
using Xunit;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalComments
{
    public partial class ApprovalCommentServiceTests
    {
        // The cross-entity gates. Everything these cover needs the parent Approval, which a
        // single-entity service may not read for itself — the whole reason IAccessBroker is a
        // dependency here at all.
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfTheAccessBrokerRefusesAndLogItAsync()
        {
            // given: a closed round or a taken-down parent. The service does not distinguish
            // them to the caller — the verdict's reason is logged, never returned (§14.5).
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            ApprovalComment randomApprovalComment =
                CreateApprovalCommentFiller(randomDateTimeOffset).Create();

            SetupAccessBrokerToRefuse(AccessDenialReason.ApprovalNotOpenForComment);

            var unauthorizedApprovalCommentException = new UnauthorizedApprovalCommentException(
                message: "The current user is not allowed to act on this approval comment.");

            var expectedApprovalCommentValidationException = new ApprovalCommentValidationException(
                message: "Approval comment validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    It.IsAny<ApprovalComment>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(randomApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomApprovalComment.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalComment> addApprovalCommentTask =
                this.approvalCommentService.AddApprovalCommentAsync(
                    randomApprovalComment,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    addApprovalCommentTask.AsTask);

            // then
            actualException.Should()
                .BeEquivalentTo(expectedApprovalCommentValidationException);

            this.accessBrokerMock.Verify(broker =>
                broker.MayRecordApprovalCommentAsync(
                    randomApprovalComment.ApprovalId,
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            // the row is never written when the parent refuses it, and nothing is announced
            this.storageBrokerMock.Verify(broker =>
                broker.InsertApprovalCommentAsync(
                    It.IsAny<ApprovalComment>(),
                    It.IsAny<CancellationToken>()),
                        Times.Never);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    It.IsAny<ApprovalCommentEventOperation>()),
                        Times.Never);

            VerifyTheRefusalWasLoggedWithoutReachingTheCaller();
        }

        [Theory]
        [InlineData(AccessDenialReason.ApprovalNotOpenForComment)]
        [InlineData(AccessDenialReason.ParentApprovalUnavailable)]
        public async Task ShouldThrowValidationExceptionOnModifyIfTheAccessBrokerRefusesAndLogItAsync(
            AccessDenialReason denialReason)
        {
            // given: both reasons refuse identically from the caller's side
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            ApprovalComment randomApprovalComment =
                CreateRandomModifyApprovalComment(randomDateTimeOffset, randomUserId);

            ApprovalComment storageApprovalComment = randomApprovalComment.DeepClone();

            storageApprovalComment.UpdatedWhen =
                storageApprovalComment.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            SetupAccessBrokerToRefuse(denialReason);

            var unauthorizedApprovalCommentException = new UnauthorizedApprovalCommentException(
                message: "The current user is not allowed to act on this approval comment.");

            var expectedApprovalCommentValidationException = new ApprovalCommentValidationException(
                message: "Approval comment validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<ApprovalComment>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(randomApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    randomApprovalComment.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalComment);

            // when
            ValueTask<ApprovalComment> modifyApprovalCommentTask =
                this.approvalCommentService.ModifyApprovalCommentAsync(
                    randomApprovalComment,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    modifyApprovalCommentTask.AsTask);

            // then
            actualException.Should()
                .BeEquivalentTo(expectedApprovalCommentValidationException);

            // the stored author is what the gate is asked about, never a payload value
            this.accessBrokerMock.Verify(broker =>
                broker.MayAmendApprovalCommentAsync(
                    storageApprovalComment.ApprovalId,
                    storageApprovalComment.CreatedBy,
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalCommentAsync(
                    It.IsAny<ApprovalComment>(),
                    It.IsAny<CancellationToken>()),
                        Times.Never);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    It.IsAny<ApprovalCommentEventOperation>()),
                        Times.Never);

            VerifyTheRefusalWasLoggedWithoutReachingTheCaller();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRemoveIfTheAccessBrokerRefusesAndLogItAsync()
        {
            // given: a closed round locks withdrawal too — what was said stands as recorded
            string randomUserId = GetRandomString();
            ApprovalComment storageApprovalComment = CreateRandomApprovalComment();
            storageApprovalComment.IsDeleted = false;

            SetupAccessBrokerToRefuse(AccessDenialReason.ApprovalNotOpenForComment);

            var unauthorizedApprovalCommentException = new UnauthorizedApprovalCommentException(
                message: "The current user is not allowed to act on this approval comment.");

            var expectedApprovalCommentValidationException = new ApprovalCommentValidationException(
                message: "Approval comment validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalCommentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageApprovalComment.CreatedBy);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    storageApprovalComment.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalComment);

            // when
            ValueTask<ApprovalComment> removeApprovalCommentTask =
                this.approvalCommentService.RemoveApprovalCommentByIdAsync(
                    storageApprovalComment.Id,
                    cancellationToken: TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    removeApprovalCommentTask.AsTask);

            // then
            actualException.Should()
                .BeEquivalentTo(expectedApprovalCommentValidationException);

            this.accessBrokerMock.Verify(broker =>
                broker.MayAmendApprovalCommentAsync(
                    storageApprovalComment.ApprovalId,
                    storageApprovalComment.CreatedBy,
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            // The gate must refuse BEFORE the row is written and before the fact is announced.
            // Without these two, moving the gate below the write still passes: the caller sees
            // the same refusal while the soft delete has already landed and
            // ApprovalComment-Removed has already gone out to everything re-testing an approval
            // blocked on RequireReviewCommentResolutionBeforeApprovals.
            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalCommentAsync(
                    It.IsAny<ApprovalComment>(),
                    It.IsAny<CancellationToken>()),
                        Times.Never);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    It.IsAny<ApprovalCommentEventOperation>()),
                        Times.Never);

            VerifyTheRefusalWasLoggedWithoutReachingTheCaller();
        }

        // §14.5: the true reason goes to the log, and the caller is told only that they may not
        // act. The verdict's explanation is composed from resolved policy values, so echoing it
        // outward would leak the approval configuration through a public event address.
        private void VerifyTheRefusalWasLoggedWithoutReachingTheCaller() =>
            this.loggingBrokerMock.Verify(broker =>
                broker.LogWarningAsync(It.Is<string>(message =>
                    message.Contains("Approval comment denied")
                        && message.Contains("Reported to the caller as unauthorized"))),
                            Times.Once);
    }
}
