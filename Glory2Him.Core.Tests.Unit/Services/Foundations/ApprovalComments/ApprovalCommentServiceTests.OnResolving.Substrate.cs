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
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalComments.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalComments
{
    public partial class ApprovalCommentServiceTests
    {
        [Fact]
        public async Task ShouldResolveOnResolvingApprovalCommentEventAsync()
        {
            // given: the event path carries the id and the flag in the envelope; the do-work
            // reads only those two off it, exactly as the direct path does
            string randomUserId = GetRandomString();

            ApprovalComment storageApprovalComment = CreateRandomApprovalComment();
            storageApprovalComment.CreatedBy = randomUserId;
            storageApprovalComment.IsResolved = false;

            var requestEnvelope = new EventEnvelope<ApprovalComment>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),

                Content = new ApprovalComment
                {
                    Id = storageApprovalComment.Id,
                    IsResolved = true
                },

                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    storageApprovalComment.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalComment);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<ApprovalComment>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((ApprovalComment entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalCommentAsync(
                    It.IsAny<ApprovalComment>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ApprovalComment entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    It.IsAny<ApprovalCommentEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<ApprovalComment>>(
                            new EventPublishResult<ApprovalComment>()));

            // when
            EventEnvelope<ApprovalComment>? actualReplyEnvelope =
                await this.approvalCommentService.OnResolvingApprovalCommentAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope.Content.IsResolved.Should().BeTrue();

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    ApprovalCommentEventOperation.Resolved),
                Times.Once);
        }

        [Fact]
        public async Task ShouldReopenOnResolvingApprovalCommentEventWhenTheFlagIsFalseAsync()
        {
            // given: the flag comes off the envelope, so the event path can make a comment
            // outstanding again as well as settle it — a handler hard-wired to true would make
            // the address one-way
            string randomUserId = GetRandomString();

            ApprovalComment storageApprovalComment = CreateRandomApprovalComment();
            storageApprovalComment.CreatedBy = randomUserId;
            storageApprovalComment.IsResolved = true;

            var requestEnvelope = new EventEnvelope<ApprovalComment>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),

                Content = new ApprovalComment
                {
                    Id = storageApprovalComment.Id,
                    IsResolved = false
                },

                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    storageApprovalComment.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalComment);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<ApprovalComment>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((ApprovalComment entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalCommentAsync(
                    It.IsAny<ApprovalComment>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ApprovalComment entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    It.IsAny<ApprovalCommentEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<ApprovalComment>>(
                            new EventPublishResult<ApprovalComment>()));

            // when
            EventEnvelope<ApprovalComment>? actualReplyEnvelope =
                await this.approvalCommentService.OnResolvingApprovalCommentAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope.Content.IsResolved.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldSkipResolveAndReplyNullWhenResolvingApprovalCommentEventAlreadyProcessedAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<ApprovalComment>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new ApprovalComment { Id = Guid.NewGuid(), IsResolved = true },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalCommentOnResolvingApprovalCommentSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<ApprovalComment>? actualReplyEnvelope =
                await this.approvalCommentService.OnResolvingApprovalCommentAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: no work, no fact, no reply — the duplicate is acknowledged silently
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalCommentOnResolvingApprovalCommentSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnResolvingApprovalCommentEventWhenEnvelopeIsInvalidAsync()
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
            ValueTask<EventEnvelope<ApprovalComment>?> onResolvingTask =
                this.approvalCommentService.OnResolvingApprovalCommentAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    onResolvingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalCommentValidationException);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnResolvingApprovalCommentEventWhenSignatureIsInvalidAsync()
        {
            // given: the signature is what makes the envelope's SecurityContext trustworthy on
            // the event path — without it, whoever can put a message on this address states
            // their own roles and would be believed (design §14.6 rule 4)
            EventEnvelope<ApprovalComment> requestEnvelope =
                CreateRandomApprovalCommentRequestEnvelope();

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    It.IsAny<string>(),
                    It.IsAny<EnvelopeDirection>()))
                        .ReturnsAsync(false);

            var invalidApprovalCommentEventException =
                new InvalidApprovalCommentEventException(
                    message: "Invalid approval comment event. Integrity verification failed.");

            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalCommentEventException);

            // when
            ValueTask<EventEnvelope<ApprovalComment>?> onResolvingTask =
                this.approvalCommentService.OnResolvingApprovalCommentAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(
                    onResolvingTask.AsTask);

            // then: nothing is read and nothing is written on an unverified envelope
            actualException.Should().BeEquivalentTo(expectedApprovalCommentValidationException);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }
    }
}
