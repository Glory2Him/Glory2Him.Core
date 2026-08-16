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
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.ApprovalReviews.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviews
{
    public partial class ApprovalReviewServiceTests
    {
        // the review roles must NOT be able to dismiss — dismissal is the workflow's act, not a
        // reviewer's verdict (§8.8, the HR-3 shape). A plain Reviewer and an entity-scoped
        // "-Reviewer" both hold the review tier but not the publisher tier.
        public static TheoryData<string[]> NonPublisherRoleSets() =>
            new TheoryData<string[]>
            {
                new string[0],
                new[] { Roles.Reviewer },
                new[] { Roles.ContentItemReviewer },
            };

        [Fact]
        public async Task ShouldThrowValidationExceptionOnDismissIfIdIsInvalidAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            var invalidApprovalReviewException =
                new InvalidApprovalReviewException(
                    message: "Approval review is invalid, fix the errors and try again.");

            invalidApprovalReviewException.UpsertDataList(
                key: nameof(ApprovalReview.Id),
                value: "Id is required");

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalReviewException);

            // when
            ValueTask<ApprovalReview> dismissTask =
                this.approvalReviewService.DismissApprovalReviewAsync(
                    Guid.Empty,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(dismissTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectApprovalReviewByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowUnauthorizedOnDismissIfCallerIsNotAuthenticatedAsync(
            SecurityContext unauthenticatedContext)
        {
            // given
            this.ambientSecurityContext = unauthenticatedContext;

            // when
            ValueTask<ApprovalReview> dismissTask =
                this.approvalReviewService.DismissApprovalReviewAsync(
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<ApprovalReviewValidationException>(dismissTask.AsTask);

            // then: the contribution gate refuses before any row is read
            this.storageBrokerMock.Verify(broker =>
                    broker.SelectApprovalReviewByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowUnauthorizedOnDismissIfCallerIsBlockedFromContributingAsync()
        {
            // given: a read-only caller is blocked from every write, dismiss included
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.ReadOnly);

            var unauthorizedApprovalReviewException =
                new UnauthorizedApprovalReviewException(
                    message: "The current user is blocked from contributing approval reviews.");

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedApprovalReviewException);

            // when
            ValueTask<ApprovalReview> dismissTask =
                this.approvalReviewService.DismissApprovalReviewAsync(
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(dismissTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectApprovalReviewByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnDismissIfTheReviewIsMissingAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);
            Guid approvalReviewId = Guid.NewGuid();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    approvalReviewId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ApprovalReview)null);

            var notFoundApprovalReviewException =
                new NotFoundApprovalReviewException(
                    message: $"Approval review not found with id: {approvalReviewId}.");

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, fix the errors and try again.",
                    innerException: notFoundApprovalReviewException);

            // when
            ValueTask<ApprovalReview> dismissTask =
                this.approvalReviewService.DismissApprovalReviewAsync(
                    approvalReviewId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(dismissTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateApprovalReviewAsync(
                        It.IsAny<ApprovalReview>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnDismissIfTheReviewIsSoftDeletedAsync()
        {
            // given: a soft-removed review is reported as not-found, matching the read posture
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            ApprovalReview storageApprovalReview = CreateRandomApprovalReview();
            storageApprovalReview.IsDeleted = true;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    storageApprovalReview.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalReview);

            var notFoundApprovalReviewException =
                new NotFoundApprovalReviewException(
                    message: $"Approval review not found with id: {storageApprovalReview.Id}.");

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, fix the errors and try again.",
                    innerException: notFoundApprovalReviewException);

            // when
            ValueTask<ApprovalReview> dismissTask =
                this.approvalReviewService.DismissApprovalReviewAsync(
                    storageApprovalReview.Id,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(dismissTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedApprovalReviewValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateApprovalReviewAsync(
                        It.IsAny<ApprovalReview>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(NonPublisherRoleSets))]
        public async Task ShouldThrowUnauthorizedOnDismissIfCallerLacksThePublisherTierAsync(
            string[] roles)
        {
            // given: dismissal is the workflow's act. A caller without the publisher tier — a
            // plain Reviewer or an entity-scoped "-Reviewer" included — may not drive a review
            // to Dismissed by hand (§8.8, the HR-3 shape).
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(roles);

            ApprovalReview storageApprovalReview = CreateRandomApprovalReview();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    storageApprovalReview.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalReview);

            var unauthorizedApprovalReviewException =
                new UnauthorizedApprovalReviewException(
                    message: "The current user is not allowed to dismiss this approval review.");

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedApprovalReviewException);

            // when
            ValueTask<ApprovalReview> dismissTask =
                this.approvalReviewService.DismissApprovalReviewAsync(
                    storageApprovalReview.Id,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(dismissTask.AsTask);

            // then: nothing was written
            actualException.Should().BeEquivalentTo(expectedApprovalReviewValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateApprovalReviewAsync(
                        It.IsAny<ApprovalReview>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishApprovalReviewAsync(
                        It.IsAny<EventEnvelope<ApprovalReview>>(),
                        It.IsAny<ApprovalReviewEventOperation>()),
                Times.Never);

            // The row-local publisher-tier gate runs FIRST, so a caller without it is refused
            // before the Approval row and the entity behind it are ever read. Without this the
            // two gates can be swapped and nothing fails — the caller sees the same refusal
            // while a cross-entity read has already happened on their behalf. The suite's usual
            // VerifyNoOtherCalls tail cannot catch it: that convention excludes accessBrokerMock.
            this.accessBrokerMock.Verify(broker =>
                    broker.MayDismissApprovalReviewAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<SecurityContext>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// Permission is decided before the row's state is looked at, so a caller who fails the
        /// entity-scoped tier is told "not allowed" rather than "already dismissed".
        ///
        /// <para>The distinction is the whole point: the other way round, anyone holding any
        /// <c>-Publisher</c> role could walk arbitrary review ids and read the refusal wording to
        /// learn which rows are already dismissed — an existence-and-state probe on approvals
        /// they have no authority over.</para>
        /// </summary>
        [Fact]
        public async Task ShouldRefuseDismissalBeforeRevealingThatTheReviewIsAlreadyDismissedAsync()
        {
            // given: a caller who clears the row-local suffix test but not the entity-scoped
            // tier, acting on a review that is ALREADY dismissed
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.TagPublisher);

            ApprovalReview storageApprovalReview = CreateRandomApprovalReview();
            storageApprovalReview.StatusId = ApprovalStatus.Dismissed;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    storageApprovalReview.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalReview);

            SetupAccessBrokerToRefuseDismissal(AccessDenialReason.NotInPublisherTier);

            // when
            ValueTask<ApprovalReview> dismissApprovalReviewTask =
                this.approvalReviewService.DismissApprovalReviewAsync(
                    storageApprovalReview.Id,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    dismissApprovalReviewTask.AsTask);

            // then: the authorization answer, not the state answer
            actualException.InnerException.Message.Should().Be(
                "The current user is not allowed to dismiss this approval review.");

            actualException.InnerException.Message.Should().NotContain("already dismissed");
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnDismissIfTheReviewIsAlreadyDismissedAsync()
        {
            // given: a dismissed review stays dismissed (§9.5). A second dismissal is refused
            // rather than treated as idempotent, so the caller learns it was a no-op instead of
            // it silently re-stamping the audit values and re-publishing the fact. The publisher
            // tier passes first, so this proves the state gate stands on its own.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publisher);

            ApprovalReview storageApprovalReview = CreateRandomApprovalReview();
            storageApprovalReview.StatusId = ApprovalStatus.Dismissed;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    storageApprovalReview.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalReview);

            var invalidApprovalReviewException =
                new InvalidApprovalReviewException(
                    message: "Approval review is already dismissed.");

            var expectedApprovalReviewValidationException =
                new ApprovalReviewValidationException(
                    message: "Approval review validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalReviewException);

            // when
            ValueTask<ApprovalReview> dismissTask =
                this.approvalReviewService.DismissApprovalReviewAsync(
                    storageApprovalReview.Id,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(dismissTask.AsTask);

            // then: nothing written, nothing announced
            actualException.Should().BeEquivalentTo(expectedApprovalReviewValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateApprovalReviewAsync(
                        It.IsAny<ApprovalReview>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishApprovalReviewAsync(
                        It.IsAny<EventEnvelope<ApprovalReview>>(),
                        It.IsAny<ApprovalReviewEventOperation>()),
                Times.Never);
        }
    }
}
