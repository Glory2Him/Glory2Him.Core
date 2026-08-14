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
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Approvals;
using Moq;
using Xunit;

namespace Glory2Him.Core.Tests.Unit.Brokers.Securities
{
    public partial class AccessBrokerTests
    {
        [Fact]
        public async Task ShouldGatherTheParentApprovalStateAndDeletionOnRecordCommentAsync()
        {
            // given: these two facts are the whole reason the gate lives here — neither is
            // readable by ApprovalCommentService, which is single-entity
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            Approval approval = CreateApproval(
                Guid.NewGuid(),
                EntityType.ContentItem,
                Guid.NewGuid(),
                ApprovalStatus.Submitted);

            approval.IsDeleted = true;
            SetupApprovalById(approval);

            // when
            await this.accessBroker.MayRecordApprovalCommentAsync(
                approval.Id,
                securityContext,
                TestContext.Current.CancellationToken);

            // then
            this.capturedRecordApprovalCommentRequest.Should().NotBeNull();

            // the actor is resolved from the security context by the audit broker, never taken
            // from the caller — if this stopped being wired the gate would decide for a stranger
            this.capturedRecordApprovalCommentRequest.Actor.UserId.Should()
                .Be(this.auditResolvedUserId);

            VerifyTheActorWasResolvedFor(securityContext);
            this.capturedRecordApprovalCommentRequest.ApprovalState.Should().Be(ApprovalState.Submitted);
            this.capturedRecordApprovalCommentRequest.IsParentApprovalDeleted.Should().BeTrue();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByIdAsync(approval.Id, It.IsAny<CancellationToken>()),
                    Times.Once);
        }

        [Theory]
        [InlineData(ApprovalStatus.Draft, ApprovalState.Draft)]
        [InlineData(ApprovalStatus.Submitted, ApprovalState.Submitted)]
        [InlineData(ApprovalStatus.Approved, ApprovalState.Approved)]
        [InlineData(ApprovalStatus.Rejected, ApprovalState.Rejected)]
        public async Task ShouldMapTheApprovalStatusOntoTheCommentRequestStateAsync(
            ApprovalStatus storedStatus,
            ApprovalState expectedState)
        {
            // given
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            Approval approval = CreateApproval(
                Guid.NewGuid(),
                EntityType.ContentItem,
                Guid.NewGuid(),
                storedStatus);

            SetupApprovalById(approval);

            // when
            await this.accessBroker.MayRecordApprovalCommentAsync(
                approval.Id,
                securityContext,
                TestContext.Current.CancellationToken);

            // then
            this.capturedRecordApprovalCommentRequest.ApprovalState.Should().Be(expectedState);
        }

        [Fact]
        public async Task ShouldPassTheStoredCommentAuthorThroughOnAmendAsync()
        {
            // given: the author comes from storage, never from the caller's payload — a
            // submitted value would let a caller nominate themselves as someone else's author
            string storedAuthor = GetRandomString();
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            Approval approval = CreateApproval(
                Guid.NewGuid(),
                EntityType.ContentItem,
                Guid.NewGuid(),
                ApprovalStatus.Submitted);

            SetupApprovalById(approval);

            // when
            await this.accessBroker.MayAmendApprovalCommentAsync(
                approval.Id,
                storedAuthor,
                securityContext,
                TestContext.Current.CancellationToken);

            // then
            this.capturedAmendApprovalCommentRequest.Should().NotBeNull();
            this.capturedAmendApprovalCommentRequest.CommentCreatedBy.Should().Be(storedAuthor);
            this.capturedAmendApprovalCommentRequest.ApprovalState.Should().Be(ApprovalState.Submitted);
        }

        [Fact]
        public async Task ShouldPassTheStoredCommentAuthorThroughOnResolveAsync()
        {
            // given
            string storedAuthor = GetRandomString();
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            Approval approval = CreateApproval(
                Guid.NewGuid(),
                EntityType.ContentItem,
                Guid.NewGuid(),
                ApprovalStatus.Submitted);

            SetupApprovalById(approval);

            // when
            await this.accessBroker.MayResolveApprovalCommentAsync(
                approval.Id,
                storedAuthor,
                securityContext,
                TestContext.Current.CancellationToken);

            // then
            this.capturedResolveApprovalCommentRequest.Should().NotBeNull();
            this.capturedResolveApprovalCommentRequest.CommentCreatedBy.Should().Be(storedAuthor);
            this.capturedResolveApprovalCommentRequest.ApprovalState.Should()
                .Be(ApprovalState.Submitted);
        }

        [Fact]
        public async Task ShouldRefuseAndNotAskTheClientWhenTheApprovalIsMissingOnRecordAsync()
        {
            // given: a comment whose approval cannot be found fails closed rather than being
            // waved through to a decision function that was never given the facts
            Guid approvalId = Guid.NewGuid();
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(approvalId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Approval)null);

            // when
            AccessVerdict actualVerdict = await this.accessBroker.MayRecordApprovalCommentAsync(
                approvalId,
                securityContext,
                TestContext.Current.CancellationToken);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.ParentApprovalUnavailable);

            actualVerdict.Explanation.Should().Contain(approvalId.ToString());
            this.capturedRecordApprovalCommentRequest.Should().BeNull();

            this.accessClientMock.Verify(client =>
                client.MayRecordApprovalCommentAsync(It.IsAny<RecordApprovalCommentRequest>()),
                    Times.Never);
        }

        [Fact]
        public async Task ShouldRefuseAndNotAskTheClientWhenTheApprovalIsMissingOnAmendAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(approvalId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Approval)null);

            // when
            AccessVerdict actualVerdict = await this.accessBroker.MayAmendApprovalCommentAsync(
                approvalId,
                GetRandomString(),
                securityContext,
                TestContext.Current.CancellationToken);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.ParentApprovalUnavailable);

            this.capturedAmendApprovalCommentRequest.Should().BeNull();

            this.accessClientMock.Verify(client =>
                client.MayAmendApprovalCommentAsync(It.IsAny<AmendApprovalCommentRequest>()),
                    Times.Never);
        }

        [Fact]
        public async Task ShouldRefuseAndNotAskTheClientWhenTheApprovalIsMissingOnResolveAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(approvalId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Approval)null);

            // when
            AccessVerdict actualVerdict = await this.accessBroker.MayResolveApprovalCommentAsync(
                approvalId,
                GetRandomString(),
                securityContext,
                TestContext.Current.CancellationToken);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.ParentApprovalUnavailable);

            this.capturedResolveApprovalCommentRequest.Should().BeNull();

            this.accessClientMock.Verify(client =>
                client.MayResolveApprovalCommentAsync(It.IsAny<ResolveApprovalCommentRequest>()),
                    Times.Never);
        }
    }
}
