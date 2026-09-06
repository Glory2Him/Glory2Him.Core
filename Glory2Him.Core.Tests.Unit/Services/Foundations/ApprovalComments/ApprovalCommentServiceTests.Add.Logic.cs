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
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalComments
{
    public partial class ApprovalCommentServiceTests
    {
        [Fact]
        public async Task ShouldAddApprovalCommentAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ApprovalComment randomApprovalComment = CreateApprovalCommentFiller(randomDateTimeOffset).Create();
            ApprovalComment inputApprovalComment = randomApprovalComment;
            ApprovalComment auditAppliedApprovalComment = inputApprovalComment.DeepClone();
            ApprovalComment storageApprovalComment = auditAppliedApprovalComment.DeepClone();
            ApprovalComment expectedApprovalComment = storageApprovalComment.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputApprovalComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedApprovalComment.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertApprovalCommentAsync(auditAppliedApprovalComment, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageApprovalComment);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    ApprovalCommentEventOperation.Added))
                    .Returns(new ValueTask<EventPublishResult<ApprovalComment>>(
                        new EventPublishResult<ApprovalComment>()));

            // when
            ApprovalComment actualApprovalComment =
                await this.approvalCommentService.AddApprovalCommentAsync(
                    inputApprovalComment,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalComment.Should().BeEquivalentTo(expectedApprovalComment);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyAddAuditValuesAsync(inputApprovalComment, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.storageBrokerMock.Verify(broker =>
                    broker.InsertApprovalCommentAsync(auditAppliedApprovalComment, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    ApprovalCommentEventOperation.Added),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalCommentOnAddingApprovalCommentSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(ApprovalCommentType.Comment, true)]
        [InlineData(ApprovalCommentType.Question, false)]
        public async Task ShouldStoreTheCallersCommentTypeAndResolutionVerbatimOnAddAsync(
            ApprovalCommentType commentType,
            bool isResolved)
        {
            // given: BOTH birth pairings are legitimate and the add path applies no rule to
            // either (§7.8 rule 1). A remark asks for nothing and is born settled; an ask holds
            // the approval shut until somebody entitled to settles it. Pinning either — the
            // tempting "fix" §7.8 warns about — would make it impossible to leave a remark
            // without blocking the approval.
            //
            // The FILLER draws neither field, so both are stated here: a test asserting on a
            // drawn value proves nothing about what the caller asked for.
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ApprovalComment randomApprovalComment =
                CreateApprovalCommentFiller(randomDateTimeOffset).Create();

            randomApprovalComment.CommentType = commentType;
            randomApprovalComment.IsResolved = isResolved;

            ApprovalComment inputApprovalComment = randomApprovalComment;
            ApprovalComment auditAppliedApprovalComment = inputApprovalComment.DeepClone();
            ApprovalComment storageApprovalComment = auditAppliedApprovalComment.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputApprovalComment, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedApprovalComment.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertApprovalCommentAsync(
                    auditAppliedApprovalComment, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalComment);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    ApprovalCommentEventOperation.Added))
                    .Returns(new ValueTask<EventPublishResult<ApprovalComment>>(
                        new EventPublishResult<ApprovalComment>()));

            // when
            ApprovalComment actualApprovalComment =
                await this.approvalCommentService.AddApprovalCommentAsync(
                    inputApprovalComment,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalComment.CommentType.Should().Be(commentType);
            actualApprovalComment.IsResolved.Should().Be(isResolved);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertApprovalCommentAsync(
                    It.Is<ApprovalComment>(inserted =>
                        inserted.CommentType == commentType
                            && inserted.IsResolved == isResolved),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
