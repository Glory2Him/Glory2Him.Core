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
                this.approvalReviewWorkflowService.DismissStaleApprovalReviewAsync(
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

        // Two contribute-gate tests stood here — an unauthenticated caller and a ReadOnly one —
        // and both went with the public verb they exercised (#295). On the workflow path the
        // context is MINTED rather than inherited, and CreateSystemAsync sets IsAuthenticated
        // unconditionally, so neither state can reach the gate however the ambient caller looks.
        //
        // ValidateUserIsAllowedToContribute still runs, deliberately, for the same reason
        // ValidateDismissalIsTheWorkflowsOwnAct does: it guards a future caller that does not
        // mint. It is simply no longer reachable from a test, and a test that appeared to
        // exercise it would be asserting the stub, not the service.
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
                this.approvalReviewWorkflowService.DismissStaleApprovalReviewAsync(
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
                this.approvalReviewWorkflowService.DismissStaleApprovalReviewAsync(
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

        // Replaces the publisher-tier theory that stood here (#295). There is no tier to test
        // any more — no human may dismiss whatever roles they hold, because no route reaches
        // this verb: the public verb is gone, the request address is gone, and the one caller
        // mints the system identity itself.
        //
        // What remains testable is the defence-in-depth guard behind all of that. The mint is
        // mocked here, so this drives it to return an ORDINARY context — modelling a broker that
        // stopped minting, or a second caller added later that never did — and proves the verb
        // refuses rather than performing a privileged write under a caller's identity.
        [Fact]
        public async Task ShouldRefuseDismissalWhenTheContextIsNotTheWorkflowsOwnAsync()
        {
            // given: a fully authenticated caller, holding the tier that USED to be sufficient
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateSystemAsync(It.IsAny<ApprovalReview>()))
                    .ReturnsAsync(new EventEnvelope<ApprovalReview>
                    {
                        Content = new ApprovalReview(),
                        SecurityContext = this.ambientSecurityContext,
                        Metadata = new EventMetadata { EventId = Guid.NewGuid() },
                    });

            // when
            ValueTask<ApprovalReview> dismissTask =
                this.approvalReviewWorkflowService.DismissStaleApprovalReviewAsync(
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<ApprovalReviewValidationException>(dismissTask.AsTask);

            // then: refused BEFORE the row is read, so a caller cannot use the verb to learn
            // whether a review exists
            this.storageBrokerMock.Verify(broker =>
                    broker.SelectApprovalReviewByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateApprovalReviewAsync(
                        It.IsAny<ApprovalReview>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
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
                this.approvalReviewWorkflowService.DismissStaleApprovalReviewAsync(
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
