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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.Comments.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Comments
{
    public partial class CommentServiceTests
    {
        // ── OnSubmitting ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task ShouldSubmitOnSubmittingCommentEventAsync()
        {
            // given: the event path carries the id in the envelope; the do-work reads only the
            // id off it and drives the row Draft -> Submitted, exactly as the direct path does
            Comment storageComment = CreateSubmittableStorageComment();

            var requestEnvelope = new EventEnvelope<Comment>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new Comment { Id = storageComment.Id },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageComment.CreatedBy);

            SetupCommentStorageRead(storageComment);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Comment>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((Comment entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateCommentAsync(
                    It.IsAny<Comment>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Comment entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishCommentAsync(
                    It.IsAny<EventEnvelope<Comment>>(),
                    It.IsAny<CommentEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<Comment>>(
                            new EventPublishResult<Comment>()));

            // when
            EventEnvelope<Comment>? actualReplyEnvelope =
                await this.commentService.OnSubmittingCommentAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishCommentAsync(
                        It.IsAny<EventEnvelope<Comment>>(),
                        CommentEventOperation.Submitted),
                Times.Once);
        }

        [Fact]
        public async Task ShouldSkipSubmitAndReplyNullWhenSubmittingCommentEventAlreadyProcessedAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<Comment>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new Comment { Id = Guid.NewGuid() },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.CommentOnSubmittingCommentSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<Comment>? actualReplyEnvelope =
                await this.commentService.OnSubmittingCommentAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: no work, no fact, no reply — the duplicate is acknowledged silently
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.CommentOnSubmittingCommentSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnSubmittingCommentEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<Comment>? nullEnvelope = null;

            var invalidCommentEventException =
                new InvalidCommentEventException(
                    message: "Invalid comment event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: invalidCommentEventException);

            // when
            ValueTask<EventEnvelope<Comment>?> onSubmittingTask =
                this.commentService.OnSubmittingCommentAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    onSubmittingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedCommentValidationException);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        // ── OnApproving ──────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task ShouldApproveOnApprovingCommentEventAsync()
        {
            // given
            Comment storageComment = CreateApprovableStorageComment();

            var requestEnvelope = new EventEnvelope<Comment>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher),
                Content = CreateApprovalDecision(storageComment.Id),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            SetupCommentStorageRead(storageComment);
            SetupAccessBrokerToPermit();

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Comment>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((Comment entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateCommentAsync(
                    It.IsAny<Comment>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Comment entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishCommentAsync(
                    It.IsAny<EventEnvelope<Comment>>(),
                    It.IsAny<CommentEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<Comment>>(
                            new EventPublishResult<Comment>()));

            // when
            EventEnvelope<Comment>? actualReplyEnvelope =
                await this.commentService.OnApprovingCommentAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishCommentAsync(
                        It.IsAny<EventEnvelope<Comment>>(),
                        CommentEventOperation.Approved),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldSkipApproveAndReplyNullWhenApprovingCommentEventAlreadyProcessedAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<Comment>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher),
                Content = CreateApprovalDecision(Guid.NewGuid()),
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.CommentOnApprovingCommentSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<Comment>? actualReplyEnvelope =
                await this.commentService.OnApprovingCommentAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: a duplicate approve neither re-decides nor re-announces
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.CommentOnApprovingCommentSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnApprovingCommentEventWhenEnvelopeIsInvalidAsync()
        {
            // given
            EventEnvelope<Comment>? nullEnvelope = null;

            var invalidCommentEventException =
                new InvalidCommentEventException(
                    message: "Invalid comment event. " +
                        "The event envelope, its content and metadata are required.");

            var expectedCommentValidationException =
                new CommentValidationException(
                    message: "Comment validation error occurred, fix the errors and try again.",
                    innerException: invalidCommentEventException);

            // when
            ValueTask<EventEnvelope<Comment>?> onApprovingTask =
                this.commentService.OnApprovingCommentAsync(
                    nullEnvelope!,
                    TestContext.Current.CancellationToken);

            CommentValidationException actualException =
                await Assert.ThrowsAsync<CommentValidationException>(
                    onApprovingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedCommentValidationException);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
