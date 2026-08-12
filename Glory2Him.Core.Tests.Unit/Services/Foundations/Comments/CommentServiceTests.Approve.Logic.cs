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
        public async Task ShouldApproveCommentAsync()
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Comment storageComment = CreateApprovableStorageComment();
            Comment inputComment = CreateApprovalDecision(storageComment.Id);

            Comment approvedComment = storageComment.DeepClone();
            approvedComment.ApprovalStatus = inputComment.ApprovalStatus;
            approvedComment.IsPublished = inputComment.IsPublished;
            approvedComment.PublishDate = inputComment.PublishDate;
            approvedComment.IsApprovedByBypass = false;
            approvedComment.ApprovedByBypassReason = null;

            Comment auditAppliedComment = approvedComment.DeepClone();
            Comment updatedComment = auditAppliedComment.DeepClone();
            Comment expectedComment = updatedComment.DeepClone();

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

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
                    CommentEventOperation.Approved))
                        .Returns(new ValueTask<EventPublishResult<Comment>>(
                            new EventPublishResult<Comment>()));

            // when
            Comment actualComment =
                await this.commentService.ApproveCommentAsync(
                    inputComment,
                    TestContext.Current.CancellationToken);

            // then
            actualComment.Should().BeEquivalentTo(expectedComment);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectCommentByIdAsync(
                        inputComment.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
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

            // the operation's OWN fact — never Modified. See ShouldNeverPublishModified...
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishCommentAsync(
                        It.IsAny<EventEnvelope<Comment>>(),
                        CommentEventOperation.Approved),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .CommentOnApprovingCommentSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.AtLeastOnce);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldPublishRejectedWhenTheDecisionRejectsOnApproveAsync()
        {
            // given: the fact follows the DECISION, not the verb. A rejection announced on the
            // Approved address would tell every subscriber the row is live.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            Comment storageComment = CreateApprovableStorageComment();
            Comment inputComment = CreateRejectionDecision(storageComment.Id);

            // when
            await CaptureSavedCommentOnApproveAsync(storageComment, inputComment);

            // then
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishCommentAsync(
                        It.IsAny<EventEnvelope<Comment>>(),
                        CommentEventOperation.Rejected),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishCommentAsync(
                        It.IsAny<EventEnvelope<Comment>>(),
                        CommentEventOperation.Approved),
                Times.Never);
        }

        [Fact]
        public async Task ShouldNeverPublishModifiedOnApproveAsync()
        {
            // given: the transitions exist to keep the approval workflow's cycle-breaker intact
            // (design §9.7.1). The workflow subscribes to Modified and causes Approved, so an
            // approve that published Modified would re-enter the handler that caused it. This is
            // issue #111 case 1: assert the published operation explicitly, both ways.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            Comment storageComment = CreateApprovableStorageComment();
            Comment inputComment = CreateApprovalDecision(storageComment.Id);

            // when
            await CaptureSavedCommentOnApproveAsync(storageComment, inputComment);

            // then
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishCommentAsync(
                        It.IsAny<EventEnvelope<Comment>>(),
                        CommentEventOperation.Modified),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishCommentAsync(
                        It.IsAny<EventEnvelope<Comment>>(),
                        CommentEventOperation.Approved),
                Times.Once);
        }

        [Fact]
        public async Task ShouldSaveOnlyTheApprovalFieldsFromTheCallerOnApproveAsync()
        {
            // given: the caller sends a FULLY populated entity whose every non-approval field
            // differs from storage. Approve owns IApproval and nothing else, so the saved row
            // must take the approval values from the caller and everything else from storage
            // (issue #111 case 2: field scope respected). Asserting the whole row against the
            // pre-act snapshot — excluding only the fields approve owns — catches a stray write
            // on ANY other field, without naming entity-specific columns.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            Comment storageComment = CreateApprovableStorageComment();
            Comment expectedStorageComment = storageComment.DeepClone();

            // a fully random caller copy (differs from storage on every field), pinned only to
            // the id and a valid approval outcome
            Comment inputComment = CreateRandomComment();
            inputComment.Id = storageComment.Id;
            inputComment.ApprovalStatus = ApprovalStatus.Approved;
            inputComment.IsPublished = true;
            inputComment.PublishDate = GetRandomDateTimeOffset();

            // when
            Comment savedComment = await CaptureSavedCommentOnApproveAsync(storageComment, inputComment);

            // then
            savedComment.Should().NotBeNull();

            // the fields the operation owns came from the caller
            savedComment.ApprovalStatus.Should().Be(inputComment.ApprovalStatus);
            savedComment.IsPublished.Should().Be(inputComment.IsPublished);
            savedComment.PublishDate.Should().Be(inputComment.PublishDate);

            // everything else came from STORAGE — asserted against the pre-act snapshot, so
            // copying any caller field onto the row fails here. The bypass pair is derived
            // (false / null here) and excluded from the storage comparison.
            savedComment.Should().BeEquivalentTo(
                expectedStorageComment,
                options => options
                    .Excluding(comment => comment.ApprovalStatus)
                    .Excluding(comment => comment.IsPublished)
                    .Excluding(comment => comment.PublishDate)
                    .Excluding(comment => comment.IsApprovedByBypass)
                    .Excluding(comment => comment.ApprovedByBypassReason));
        }

        // ── The bypass record is DERIVED, not copied ─────────────────────────────────────────

        [Fact]
        public async Task ShouldIgnoreTheCallersBypassRecordOnApproveAsync()
        {
            // given: the caller claims a bypass it was never granted. The decision came back
            // permitted WITHOUT one, so the saved row must say so — otherwise the flag means
            // "the caller said so" rather than "the rules were waived".
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            Comment storageComment = CreateApprovableStorageComment();
            storageComment.IsApprovedByBypass = false;
            storageComment.ApprovedByBypassReason = null;

            Comment inputComment = CreateApprovalDecision(storageComment.Id);
            inputComment.IsApprovedByBypass = true;
            inputComment.ApprovedByBypassReason = "caller supplied";

            SetupAccessBrokerToPermit();

            // when
            Comment savedComment = await CaptureSavedCommentOnApproveAsync(storageComment, inputComment);

            // then
            savedComment.Should().NotBeNull();
            savedComment.IsApprovedByBypass.Should().BeFalse();
            savedComment.ApprovedByBypassReason.Should().BeNull();

            savedComment.ApprovalStatus.Should().Be(inputComment.ApprovalStatus);
            savedComment.IsPublished.Should().Be(inputComment.IsPublished);
            savedComment.PublishDate.Should().Be(inputComment.PublishDate);
        }

        [Fact]
        public async Task ShouldRecordTheBypassOnTheRowWhenTheDecisionWaivedTheConditionsAsync()
        {
            // given: the mirror image — the caller claims nothing and the DECISION reports a
            // bypass. The flag has to travel from the verdict onto the row.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            Comment storageComment = CreateApprovableStorageComment();
            storageComment.IsApprovedByBypass = false;
            storageComment.ApprovedByBypassReason = null;

            Comment inputComment = CreateApprovalDecision(storageComment.Id);
            inputComment.IsApprovedByBypass = false;
            inputComment.ApprovedByBypassReason = null;

            SetupAccessBrokerToPermitByBypass();

            // when
            Comment savedComment = await CaptureSavedCommentOnApproveAsync(storageComment, inputComment);

            // then
            savedComment.Should().NotBeNull();
            savedComment.IsApprovedByBypass.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldClearAnEarlierBypassRecordWhenTheRowIsApprovedNormallyAsync()
        {
            // given: a row bypass-approved once already, amended since, and now approved on its
            // merits. A row that met its conditions this time must stop claiming they were
            // waived, or the flag accumulates for the rest of its life.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            Comment storageComment = CreateApprovableStorageComment();
            storageComment.IsApprovedByBypass = true;
            storageComment.ApprovedByBypassReason = "an earlier bypass";

            Comment inputComment = CreateApprovalDecision(storageComment.Id);

            SetupAccessBrokerToPermit();

            // when
            Comment savedComment = await CaptureSavedCommentOnApproveAsync(storageComment, inputComment);

            // then
            savedComment.Should().NotBeNull();
            savedComment.IsApprovedByBypass.Should().BeFalse();
            savedComment.ApprovedByBypassReason.Should().BeNull();
        }
    }
}
