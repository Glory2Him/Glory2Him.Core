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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalComments
{
    public partial class ApprovalCommentServiceTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldResolveApprovalCommentAsync(bool isResolved)
        {
            // given: the author records whether their comment is settled — whether it still
            // requires something before the approval can proceed. Un-settling rides the same
            // operation: an observation may later need action, and one settled prematurely
            // must be able to block again — without it a mistaken resolve permanently defeats
            // the gate.
            string randomUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            ApprovalComment storageApprovalComment = CreateRandomApprovalComment();
            storageApprovalComment.CreatedBy = randomUserId;
            storageApprovalComment.IsResolved = isResolved is false;

            ApprovalComment resolvedApprovalComment = storageApprovalComment.DeepClone();
            resolvedApprovalComment.IsResolved = isResolved;

            ApprovalComment auditAppliedApprovalComment = resolvedApprovalComment.DeepClone();
            ApprovalComment updatedApprovalComment = auditAppliedApprovalComment.DeepClone();
            ApprovalComment expectedApprovalComment = updatedApprovalComment.DeepClone();

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    storageApprovalComment.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<ApprovalComment>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedApprovalComment);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalCommentAsync(
                    auditAppliedApprovalComment,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(updatedApprovalComment);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    ApprovalCommentEventOperation.Resolved))
                        .Returns(new ValueTask<EventPublishResult<ApprovalComment>>(
                            new EventPublishResult<ApprovalComment>()));

            // when
            ApprovalComment actualApprovalComment =
                await this.approvalCommentService.ResolveApprovalCommentAsync(
                    storageApprovalComment.Id,
                    isResolved,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalComment.Should().BeEquivalentTo(expectedApprovalComment);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    storageApprovalComment.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // the gate is asked about the STORED approval and author, never a payload value
            this.accessBrokerMock.Verify(broker =>
                broker.MayResolveApprovalCommentAsync(
                    storageApprovalComment.ApprovalId,
                    storageApprovalComment.CreatedBy,
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalCommentAsync(
                    auditAppliedApprovalComment,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // the operation's OWN fact — never Modified
            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    ApprovalCommentEventOperation.Resolved),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .ApprovalCommentOnResolvingApprovalCommentSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task ShouldResolveOnBehalfOfTheAuthorWhenTheCallerIsAnAdminAsync()
        {
            // given: the one comment operation an Admin may run against another person's row.
            // Resolving records that a comment is settled, which changes no words — amending
            // or withdrawing someone else's comment stays refused (§14.7 rule 5).
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);

            ApprovalComment storageApprovalComment = CreateRandomApprovalComment();
            storageApprovalComment.IsResolved = false;

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            // a DIFFERENT user from the comment's author — the Admin role is what carries this
            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(GetRandomString());

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    storageApprovalComment.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalComment);

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
            ApprovalComment actualApprovalComment =
                await this.approvalCommentService.ResolveApprovalCommentAsync(
                    storageApprovalComment.Id,
                    isResolved: true,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalComment.IsResolved.Should().BeTrue();

            // the audit values are stamped for the ACTING user, so UpdatedBy records who
            // declared it settled while CreatedBy still names who wrote it
            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<ApprovalComment>(),
                    It.IsAny<SecurityContext>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    ApprovalCommentEventOperation.Resolved),
                Times.Once);
        }

        [Fact]
        public async Task ShouldSaveOnlyTheResolutionFieldOnResolveAsync()
        {
            // given: resolve owns ONLY IsResolved. It must leave every other field exactly as
            // stored — the words, their author and the approval they hang off are not the
            // resolution's to touch.
            string randomUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            ApprovalComment storageApprovalComment = CreateRandomApprovalComment();
            storageApprovalComment.CreatedBy = randomUserId;
            storageApprovalComment.IsResolved = false;

            ApprovalComment expectedStorageApprovalComment = storageApprovalComment.DeepClone();

            ApprovalComment savedApprovalComment = null;

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    storageApprovalComment.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalComment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<ApprovalComment>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((ApprovalComment entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalCommentAsync(
                    It.IsAny<ApprovalComment>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<ApprovalComment, CancellationToken>(
                            (entity, _) => savedApprovalComment = entity.DeepClone())
                        .ReturnsAsync((ApprovalComment entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    It.IsAny<ApprovalCommentEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<ApprovalComment>>(
                            new EventPublishResult<ApprovalComment>()));

            // when
            await this.approvalCommentService.ResolveApprovalCommentAsync(
                storageApprovalComment.Id,
                isResolved: true,
                TestContext.Current.CancellationToken);

            // then
            savedApprovalComment.Should().NotBeNull();
            savedApprovalComment.IsResolved.Should().BeTrue();

            savedApprovalComment.Should().BeEquivalentTo(
                expectedStorageApprovalComment,
                options => options.Excluding(approvalComment => approvalComment.IsResolved));
        }

        [Fact]
        public async Task ShouldNeverPublishModifiedOnResolveAsync()
        {
            // given: a consumer watching RequireReviewCommentResolutionBeforeApprovals subscribes
            // to the resolution address. Publishing Modified would hide the gate moving inside
            // the general edit stream (design §9.7.1).
            string randomUserId = GetRandomString();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            ApprovalComment storageApprovalComment = CreateRandomApprovalComment();
            storageApprovalComment.CreatedBy = randomUserId;
            storageApprovalComment.IsResolved = false;

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    storageApprovalComment.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalComment);

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
            await this.approvalCommentService.ResolveApprovalCommentAsync(
                storageApprovalComment.Id,
                isResolved: true,
                TestContext.Current.CancellationToken);

            // then
            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    ApprovalCommentEventOperation.Modified),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    ApprovalCommentEventOperation.Resolved),
                Times.Once);
        }
    }
}
