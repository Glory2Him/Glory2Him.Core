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
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalComments.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalComments
{
    public partial class ApprovalCommentServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemovingApprovalCommentByIdEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<ApprovalComment>? nullEnvelope = null;

            var invalidApprovalCommentEventException =
                new InvalidApprovalCommentEventException(
                    message: "Invalid approval comment event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalCommentEventException);

            // when
            ValueTask<EventEnvelope<ApprovalComment>?> onHardRemovingTask =
                this.approvalCommentService.OnHardRemovingApprovalCommentByIdAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualApprovalCommentValidationException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    onHardRemovingTask.AsTask);

            // then
            actualApprovalCommentValidationException.Should().BeEquivalentTo(
                expectedApprovalCommentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemovingApprovalCommentByIdEventWhenIdIsInvalidAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<ApprovalComment>
            {
                Content = new ApprovalComment { Id = Guid.Empty },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var invalidApprovalCommentException = new InvalidApprovalCommentException(
                message: "Approval comment is invalid, fix the errors and try again.");

            invalidApprovalCommentException.UpsertDataList(
                key: nameof(ApprovalComment.Id),
                value: "Id is required");

            var expectedApprovalCommentValidationException = new ApprovalCommentValidationException(
                message: "Approval comment validation error occurred, fix the errors and try again.",
                innerException: invalidApprovalCommentException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalCommentOnHardRemovingApprovalCommentByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            // when
            ValueTask<EventEnvelope<ApprovalComment>?> onHardRemovingTask =
                this.approvalCommentService.OnHardRemovingApprovalCommentByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualApprovalCommentValidationException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    onHardRemovingTask.AsTask);

            // then
            actualApprovalCommentValidationException.Should().BeEquivalentTo(
                expectedApprovalCommentValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalCommentOnHardRemovingApprovalCommentByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnHardRemovingApprovalCommentByIdEventWhenApprovalCommentNotFoundAsync()
        {
            // given
            Guid someApprovalCommentId = Guid.NewGuid();
            ApprovalComment noApprovalComment = null!;

            var requestEnvelope = new EventEnvelope<ApprovalComment>
            {
                Content = new ApprovalComment { Id = someApprovalCommentId },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var notFoundApprovalCommentException = new NotFoundApprovalCommentException(
                message: $"Approval comment not found with id: {someApprovalCommentId}.");

            var expectedApprovalCommentValidationException = new ApprovalCommentValidationException(
                message: "Approval comment validation error occurred, fix the errors and try again.",
                innerException: notFoundApprovalCommentException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalCommentOnHardRemovingApprovalCommentByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    someApprovalCommentId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(noApprovalComment);

            // when
            ValueTask<EventEnvelope<ApprovalComment>?> onHardRemovingTask =
                this.approvalCommentService.OnHardRemovingApprovalCommentByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualApprovalCommentValidationException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    onHardRemovingTask.AsTask);

            // then: the raw not-found from the shared do-work is categorized the same way
            // the non-event path categorizes it — the event path must not degrade it to a
            // service exception.
            actualApprovalCommentValidationException.Should().BeEquivalentTo(
                expectedApprovalCommentValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    someApprovalCommentId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
