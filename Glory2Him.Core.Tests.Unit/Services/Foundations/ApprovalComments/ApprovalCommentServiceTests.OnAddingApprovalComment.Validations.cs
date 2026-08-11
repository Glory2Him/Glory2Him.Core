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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalComments.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalComments
{
    public partial class ApprovalCommentServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddingApprovalCommentEventWhenEnvelopeIsInvalidAsync()
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
            ValueTask<EventEnvelope<ApprovalComment>?> onAddingTask =
                this.approvalCommentService.OnAddingApprovalCommentAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualApprovalCommentValidationException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    onAddingTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnAddingApprovalCommentEventWhenMetadataIsNullAsync()
        {
            // given
            var invalidEnvelope = new EventEnvelope<ApprovalComment>
            {
                Content = new ApprovalComment { Id = Guid.NewGuid() },
                Metadata = null!
            };

            var invalidApprovalCommentEventException =
                new InvalidApprovalCommentEventException(
                    message: "Invalid approval comment event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalCommentEventException);

            // when
            ValueTask<EventEnvelope<ApprovalComment>?> onAddingTask =
                this.approvalCommentService.OnAddingApprovalCommentAsync(
                    invalidEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualApprovalCommentValidationException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    onAddingTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnAddingApprovalCommentEventWhenContentIsNullAsync()
        {
            // given
            var invalidEnvelope = new EventEnvelope<ApprovalComment>
            {
                Content = null!,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var invalidApprovalCommentEventException =
                new InvalidApprovalCommentEventException(
                    message: "Invalid approval comment event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalCommentEventException);

            // when
            ValueTask<EventEnvelope<ApprovalComment>?> onAddingTask =
                this.approvalCommentService.OnAddingApprovalCommentAsync(
                    invalidEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualApprovalCommentValidationException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    onAddingTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnAddingApprovalCommentEventWhenIntegrityVerificationFailsAsync()
        {
            // given
            var forgedEnvelope = new EventEnvelope<ApprovalComment>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new ApprovalComment { Id = Guid.NewGuid() },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            string expectedEventName =
                $"{nameof(ApprovalComment)}{ApprovalCommentEventOperation.Adding}";

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    forgedEnvelope,
                    expectedEventName,
                    EnvelopeDirection.Request))
                        .ReturnsAsync(false);

            var invalidApprovalCommentEventException =
                new InvalidApprovalCommentEventException(
                    message: "Invalid approval comment event. Integrity verification failed.");

            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalCommentEventException);

            // when
            ValueTask<EventEnvelope<ApprovalComment>?> onAddingTask =
                this.approvalCommentService.OnAddingApprovalCommentAsync(
                    forgedEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualApprovalCommentValidationException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    onAddingTask.AsTask);

            // then
            actualApprovalCommentValidationException.Should().BeEquivalentTo(
                expectedApprovalCommentValidationException);

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    forgedEnvelope,
                    expectedEventName,
                    EnvelopeDirection.Request),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalCommentValidationException))),
                Times.Once);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
