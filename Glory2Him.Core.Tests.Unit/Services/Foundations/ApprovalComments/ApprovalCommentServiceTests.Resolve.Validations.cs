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
        // The review roles read a comment thread without owning it, so none of them may declare
        // someone else's comment settled. Admin is the GLOBAL role only — an entity-scoped
        // "-Publisher" decides approvals, which is not the same as lifting the block
        // RequireReviewCommentResolutionBeforeApprovals holds shut.
        public static TheoryData<string[]> NonResolverRoleSets() =>
            new TheoryData<string[]>
            {
                new string[0],
                new[] { Roles.Reviewer },
                new[] { Roles.Publisher },
                new[] { Roles.ContentItemReviewer },
                new[] { Roles.ContentItemPublisher },
            };

        [Fact]
        public async Task ShouldThrowValidationExceptionOnResolveIfIdIsInvalidAsync()
        {
            // given
            var invalidApprovalCommentException =
                new InvalidApprovalCommentException(
                    message: "Approval comment is invalid, fix the errors and try again.");

            invalidApprovalCommentException.UpsertDataList(
                key: nameof(ApprovalComment.Id),
                value: "Id is required");

            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalCommentException);

            // when
            ValueTask<ApprovalComment> resolveTask =
                this.approvalCommentService.ResolveApprovalCommentAsync(
                    Guid.Empty,
                    isResolved: true,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(resolveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalCommentValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowUnauthorizedOnResolveIfCallerIsNotAuthenticatedAsync(
            SecurityContext unauthenticatedContext)
        {
            // given
            this.ambientSecurityContext = unauthenticatedContext;

            var unauthorizedApprovalCommentException =
                new UnauthorizedApprovalCommentException(
                    message: "The current user is not authenticated.");

            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedApprovalCommentException);

            // when
            ValueTask<ApprovalComment> resolveTask =
                this.approvalCommentService.ResolveApprovalCommentAsync(
                    Guid.NewGuid(),
                    isResolved: true,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(resolveTask.AsTask);

            // then: the contribution gate refuses before any row is read
            actualException.Should().BeEquivalentTo(expectedApprovalCommentValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowUnauthorizedOnResolveIfCallerIsBlockedFromContributingAsync()
        {
            // given: a read-only caller is blocked from every write, resolve included
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.ReadOnly);

            var unauthorizedApprovalCommentException =
                new UnauthorizedApprovalCommentException(
                    message: "The current user is blocked from contributing approval comments.");

            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedApprovalCommentException);

            // when
            ValueTask<ApprovalComment> resolveTask =
                this.approvalCommentService.ResolveApprovalCommentAsync(
                    Guid.NewGuid(),
                    isResolved: true,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(resolveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalCommentValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnResolveIfTheApprovalCommentIsMissingAsync()
        {
            // given
            Guid approvalCommentId = Guid.NewGuid();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    approvalCommentId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ApprovalComment)null);

            var notFoundApprovalCommentException =
                new NotFoundApprovalCommentException(
                    message: $"Approval comment not found with id: {approvalCommentId}.");

            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: notFoundApprovalCommentException);

            // when
            ValueTask<ApprovalComment> resolveTask =
                this.approvalCommentService.ResolveApprovalCommentAsync(
                    approvalCommentId,
                    isResolved: true,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(resolveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalCommentValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalCommentAsync(
                    It.IsAny<ApprovalComment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnResolveIfTheApprovalCommentIsSoftDeletedAsync()
        {
            // given: a withdrawn comment blocks nothing, so there is nothing left on it to
            // settle. Reported as not-found, matching the read posture.
            ApprovalComment storageApprovalComment = CreateRandomApprovalComment();
            storageApprovalComment.IsDeleted = true;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    storageApprovalComment.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalComment);

            var notFoundApprovalCommentException =
                new NotFoundApprovalCommentException(
                    message: $"Approval comment not found with id: {storageApprovalComment.Id}.");

            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: notFoundApprovalCommentException);

            // when
            ValueTask<ApprovalComment> resolveTask =
                this.approvalCommentService.ResolveApprovalCommentAsync(
                    storageApprovalComment.Id,
                    isResolved: true,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(resolveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalCommentValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalCommentAsync(
                    It.IsAny<ApprovalComment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(NonResolverRoleSets))]
        public async Task ShouldThrowUnauthorizedOnResolveIfCallerIsNeitherTheAuthorNorAnAdminAsync(
            string[] roles)
        {
            // given: a reviewer who wants to respond to an outstanding comment writes one of their
            // own — declaring somebody else's comment settled is the author's call, or an
            // administrator's
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(roles);

            ApprovalComment storageApprovalComment = CreateRandomApprovalComment();
            storageApprovalComment.IsResolved = false;

            // a DIFFERENT user from the comment's author
            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(GetRandomString());

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    storageApprovalComment.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalComment);

            var unauthorizedApprovalCommentException =
                new UnauthorizedApprovalCommentException(
                    message: "The current user is not allowed to resolve this approval comment.");

            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedApprovalCommentException);

            // when
            ValueTask<ApprovalComment> resolveTask =
                this.approvalCommentService.ResolveApprovalCommentAsync(
                    storageApprovalComment.Id,
                    isResolved: true,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(resolveTask.AsTask);

            // then: nothing written, nothing announced
            actualException.Should().BeEquivalentTo(expectedApprovalCommentValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalCommentAsync(
                    It.IsAny<ApprovalComment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    It.IsAny<ApprovalCommentEventOperation>()),
                Times.Never);

            // The row-local gate runs FIRST, so a non-owner is refused without the parent
            // Approval ever being read. Without this the two gates can be swapped and nothing
            // fails — the caller sees the same refusal while a cross-entity read has already
            // happened on their behalf. The suite's usual VerifyNoOtherCalls tail cannot catch
            // it: that convention deliberately excludes accessBrokerMock.
            this.accessBrokerMock.Verify(broker =>
                broker.MayResolveApprovalCommentAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(AccessDenialReason.ApprovalNotOpenForComment)]
        [InlineData(AccessDenialReason.ParentApprovalUnavailable)]
        public async Task ShouldThrowValidationExceptionOnResolveIfTheAccessBrokerRefusesAndLogItAsync(
            AccessDenialReason denialReason)
        {
            // given: once the round closes the flags are final — a resolution after the fact
            // would move a gate on an approval that is no longer being decided
            string randomUserId = GetRandomString();
            ApprovalComment storageApprovalComment = CreateRandomApprovalComment();
            storageApprovalComment.CreatedBy = randomUserId;
            storageApprovalComment.IsResolved = false;

            SetupAccessBrokerToRefuse(denialReason);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    storageApprovalComment.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalComment);

            var unauthorizedApprovalCommentException = new UnauthorizedApprovalCommentException(
                message: "The current user is not allowed to act on this approval comment.");

            var expectedApprovalCommentValidationException = new ApprovalCommentValidationException(
                message: "Approval comment validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalCommentException);

            // when
            ValueTask<ApprovalComment> resolveTask =
                this.approvalCommentService.ResolveApprovalCommentAsync(
                    storageApprovalComment.Id,
                    isResolved: true,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(resolveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalCommentValidationException);

            this.accessBrokerMock.Verify(broker =>
                broker.MayResolveApprovalCommentAsync(
                    storageApprovalComment.ApprovalId,
                    storageApprovalComment.CreatedBy,
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalCommentAsync(
                    It.IsAny<ApprovalComment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyTheRefusalWasLoggedWithoutReachingTheCaller();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnResolveIfTheApprovalCommentIsAlreadyResolvedAsync()
        {
            // given: a resolution that changes nothing is refused rather than treated as
            // idempotent. A spurious Resolved would announce to anything watching
            // RequireReviewCommentResolutionBeforeApprovals that a gate moved when it did not.
            string randomUserId = GetRandomString();
            ApprovalComment storageApprovalComment = CreateRandomApprovalComment();
            storageApprovalComment.CreatedBy = randomUserId;
            storageApprovalComment.IsResolved = true;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    storageApprovalComment.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalComment);

            var invalidApprovalCommentException =
                new InvalidApprovalCommentException(
                    message: "Approval comment is already resolved.");

            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalCommentException);

            // when
            ValueTask<ApprovalComment> resolveTask =
                this.approvalCommentService.ResolveApprovalCommentAsync(
                    storageApprovalComment.Id,
                    isResolved: true,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(resolveTask.AsTask);

            // then: nothing written, nothing announced
            actualException.Should().BeEquivalentTo(expectedApprovalCommentValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalCommentAsync(
                    It.IsAny<ApprovalComment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalCommentAsync(
                    It.IsAny<EventEnvelope<ApprovalComment>>(),
                    It.IsAny<ApprovalCommentEventOperation>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnResolveIfTheApprovalCommentIsAlreadyUnresolvedAsync()
        {
            // given: the reopening half of the same rule
            string randomUserId = GetRandomString();
            ApprovalComment storageApprovalComment = CreateRandomApprovalComment();
            storageApprovalComment.CreatedBy = randomUserId;
            storageApprovalComment.IsResolved = false;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    storageApprovalComment.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalComment);

            var invalidApprovalCommentException =
                new InvalidApprovalCommentException(
                    message: "Approval comment is already unresolved.");

            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalCommentException);

            // when
            ValueTask<ApprovalComment> resolveTask =
                this.approvalCommentService.ResolveApprovalCommentAsync(
                    storageApprovalComment.Id,
                    isResolved: false,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(resolveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalCommentValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalCommentAsync(
                    It.IsAny<ApprovalComment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldRefuseAnUnauthorizedResolveBeforeLookingAtTheResolutionStateAsync()
        {
            // given: permission is settled first, so a caller who may not act cannot use the
            // "already resolved" response to learn whether a comment on a thread is still
            // outstanding
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);

            ApprovalComment storageApprovalComment = CreateRandomApprovalComment();
            storageApprovalComment.IsResolved = true;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(GetRandomString());

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalCommentByIdAsync(
                    storageApprovalComment.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalComment);

            var unauthorizedApprovalCommentException =
                new UnauthorizedApprovalCommentException(
                    message: "The current user is not allowed to resolve this approval comment.");

            var expectedApprovalCommentValidationException =
                new ApprovalCommentValidationException(
                    message: "Approval comment validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedApprovalCommentException);

            // when: the request is a no-op AND unauthorized — the unauthorized answer must win
            ValueTask<ApprovalComment> resolveTask =
                this.approvalCommentService.ResolveApprovalCommentAsync(
                    storageApprovalComment.Id,
                    isResolved: true,
                    TestContext.Current.CancellationToken);

            ApprovalCommentValidationException actualException =
                await Assert.ThrowsAsync<ApprovalCommentValidationException>(resolveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalCommentValidationException);
        }
    }
}
