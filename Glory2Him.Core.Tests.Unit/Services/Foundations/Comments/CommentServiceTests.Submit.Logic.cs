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
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Comments
{
    public partial class CommentServiceTests
    {
        [Fact]
        public async Task ShouldSubmitCommentByOwnerAsync()
        {
            // given: the owner submitting their own draft — no moderation role required
            Comment storageComment = CreateSubmittableStorageComment();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Comment submittedComment = storageComment.DeepClone();
            submittedComment.ApprovalStatus = ApprovalStatus.Submitted;

            Comment auditAppliedComment = submittedComment.DeepClone();
            Comment updatedComment = auditAppliedComment.DeepClone();
            Comment expectedComment = updatedComment.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageComment.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            SetupCommentStorageRead(storageComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Comment>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedComment);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateCommentAsync(
                    auditAppliedComment,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(updatedComment);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishCommentAsync(
                    It.IsAny<EventEnvelope<Comment>>(),
                    CommentEventOperation.Submitted))
                        .Returns(new ValueTask<EventPublishResult<Comment>>(
                            new EventPublishResult<Comment>()));

            // when
            Comment actualComment =
                await this.commentService.SubmitCommentByIdAsync(
                    storageComment.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualComment.Should().BeEquivalentTo(expectedComment);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectCommentByIdAsync(
                        storageComment.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyModifyAuditValuesAsync(
                        It.IsAny<Comment>(),
                        It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateCommentAsync(
                        auditAppliedComment,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            // the operation's OWN fact — never Modified
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishCommentAsync(
                        It.IsAny<EventEnvelope<Comment>>(),
                        CommentEventOperation.Submitted),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .CommentOnSubmittingCommentSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            // submit never consults the cross-entity decision — that is the approve's gate
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldSubmitCommentByPublisherWhoIsNotTheOwnerAsync()
        {
            // given: the publisher tier may move a submission status too — the same set the §9.2
            // modify carve-out admits. The caller is NOT the owner, so this proves the
            // publisher-tier branch rather than the ownership branch.
            Comment storageComment = CreateSubmittableStorageComment();

            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publishers);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync($"someone-else-{Guid.NewGuid()}");

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
            await this.commentService.SubmitCommentByIdAsync(
                storageComment.Id,
                TestContext.Current.CancellationToken);

            // then
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishCommentAsync(
                        It.IsAny<EventEnvelope<Comment>>(),
                        CommentEventOperation.Submitted),
                Times.Once);
        }

        [Fact]
        public async Task ShouldSaveOnlyTheStatusFieldOnSubmitAsync()
        {
            // given: submit owns ONLY the approval status. It drives Draft -> Submitted and must
            // leave every other field exactly as stored — a content edit is the general modify's
            // job, not submit's. Asserting the whole row against the pre-act snapshot, excluding
            // only the one field submit owns, catches any stray write.
            Comment storageComment = CreateSubmittableStorageComment();
            Comment expectedStorageComment = storageComment.DeepClone();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageComment.CreatedBy);

            // when
            Comment savedComment = await CaptureSavedCommentOnSubmitAsync(storageComment);

            // then
            savedComment.Should().NotBeNull();
            savedComment.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);

            savedComment.Should().BeEquivalentTo(
                expectedStorageComment,
                options => options.Excluding(comment => comment.ApprovalStatus));
        }

        [Fact]
        public async Task ShouldNeverPublishModifiedOnSubmitAsync()
        {
            // given: like every transition, submit publishes its own fact and never Modified —
            // the approval workflow's cycle-breaker (design §9.7.1, issue #111 case 1).
            Comment storageComment = CreateSubmittableStorageComment();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageComment.CreatedBy);

            // when
            await CaptureSavedCommentOnSubmitAsync(storageComment);

            // then
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishCommentAsync(
                        It.IsAny<EventEnvelope<Comment>>(),
                        CommentEventOperation.Modified),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishCommentAsync(
                        It.IsAny<EventEnvelope<Comment>>(),
                        CommentEventOperation.Submitted),
                Times.Once);
        }

        // Runs a permitted submit end to end (owner already set up by the caller) and hands back
        // a snapshot of the row that reached the storage broker.
        private async ValueTask<Comment> CaptureSavedCommentOnSubmitAsync(Comment storageComment)
        {
            Comment savedComment = null;

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
                        .Callback<Comment, CancellationToken>(
                            (entity, _) => savedComment = entity.DeepClone())
                        .ReturnsAsync((Comment entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishCommentAsync(
                    It.IsAny<EventEnvelope<Comment>>(),
                    It.IsAny<CommentEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<Comment>>(
                            new EventPublishResult<Comment>()));

            await this.commentService.SubmitCommentByIdAsync(
                storageComment.Id,
                TestContext.Current.CancellationToken);

            return savedComment;
        }
    }
}
