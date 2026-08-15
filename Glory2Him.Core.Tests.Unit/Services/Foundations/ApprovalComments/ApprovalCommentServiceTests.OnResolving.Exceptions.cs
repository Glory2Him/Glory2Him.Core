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
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalComments.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalComments
{
    public partial class ApprovalCommentServiceTests
    {
        // Every other operation on this service — the five CRUD verbs and the five other event
        // handlers — carries a cancelled-token test. OnResolving shipped without one, so the
        // guard local to the handler was unpinned: deleting it left the whole suite green while
        // a cancelled delivery ran the full write-and-publish.

        [Fact]
        public async Task ShouldThrowOperationCanceledOnResolvingApprovalCommentEventIfCancellationRequestedAsync()
        {
            // given: a caller-cancelled token must short-circuit BEFORE any work — this guards
            // the cancellationToken.ThrowIfCancellationRequested() line that is local to
            // OnResolvingApprovalCommentAsync
            var cancellationToken = new CancellationToken(canceled: true);

            EventEnvelope<ApprovalComment> requestEnvelope =
                CreateRandomApprovalCommentRequestEnvelope();

            // when
            ValueTask<EventEnvelope<ApprovalComment>?> onResolvingTask =
                this.approvalCommentService.OnResolvingApprovalCommentAsync(
                    requestEnvelope,
                    cancellationToken);

            // then: not even the deduplication lookup runs
            await Assert.ThrowsAsync<OperationCanceledException>(onResolvingTask.AsTask);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnResolvingApprovalCommentEventIfErrorOccursAndLogItAsync()
        {
            // given: the substrate handler is wrapped by TryCatchSubstrate, so a storage failure
            // reaching it is categorized and rethrown rather than swallowed — the substrate needs
            // the throw to record the delivery as Error and drive retries
            EventEnvelope<ApprovalComment> requestEnvelope =
                CreateRandomApprovalCommentRequestEnvelope();

            var dbUpdateException = new Microsoft.EntityFrameworkCore.DbUpdateException();

            var failedStorageApprovalCommentException = new FailedStorageApprovalCommentException(
                message: "Failed approval comment storage error occurred, contact support.",
                innerException: dbUpdateException,
                data: dbUpdateException.Data);

            var expectedApprovalCommentDependencyException = new ApprovalCommentDependencyException(
                message: "Approval comment dependency error occurred, contact support.",
                innerException: failedStorageApprovalCommentException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dbUpdateException);

            // when
            ValueTask<EventEnvelope<ApprovalComment>?> onResolvingTask =
                this.approvalCommentService.OnResolvingApprovalCommentAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalCommentDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalCommentDependencyException>(
                    onResolvingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalCommentDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentDependencyException))),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    It.IsAny<Glory2Him.Core.Models.Events.Foundations.ApprovalCommentEventOperation>()),
                Times.Never);
        }
    }
}
