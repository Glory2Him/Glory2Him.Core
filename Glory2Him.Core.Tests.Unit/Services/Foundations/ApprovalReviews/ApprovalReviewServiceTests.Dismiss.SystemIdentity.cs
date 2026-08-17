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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.ApprovalReviews.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalReviews
{
    /// <summary>
    /// The system identity as a second admissible actor on dismissal, and the boundary that
    /// keeps it off the event path.
    /// </summary>
    public partial class ApprovalReviewServiceTests
    {
        // The context ApprovalOrchestrationService mints for the workflow's own writes. Roleless
        // on purpose: the flag is the whole of its authority, so a test that passes with roles
        // attached would not be proving the flag did anything.
        private static SecurityContext CreateSystemSecurityContext() =>
            new SecurityContext
            {
                IsAuthenticated = true,
                Roles = [],
                IsSystemIdentity = true
            };

        [Fact]
        public async Task ShouldDismissApprovalReviewForASystemIdentityAsync()
        {
            // given: dismissing stale reviews after the OWNER's edit is a write the workflow must
            // make and no human is permitted to — the owner holds no publisher tier, and the
            // reviewers whose reviews are being withdrawn are the last parties who should
            // withdraw them (§8.6 regardless-rule 1).
            this.ambientSecurityContext = CreateSystemSecurityContext();

            ApprovalReview storageApprovalReview = CreateRandomApprovalReview();
            storageApprovalReview.StatusId = ApprovalStatus.Approved;

            ApprovalReview dismissedApprovalReview = storageApprovalReview.DeepClone();
            dismissedApprovalReview.StatusId = ApprovalStatus.Dismissed;

            ApprovalReview auditAppliedApprovalReview = dismissedApprovalReview.DeepClone();
            ApprovalReview updatedApprovalReview = auditAppliedApprovalReview.DeepClone();

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    storageApprovalReview.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalReview);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<ApprovalReview>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedApprovalReview);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalReviewAsync(
                    auditAppliedApprovalReview,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(updatedApprovalReview);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalReviewAsync(
                    It.IsAny<EventEnvelope<ApprovalReview>>(),
                    ApprovalReviewEventOperation.Dismissed))
                        .Returns(new ValueTask<EventPublishResult<ApprovalReview>>(
                            new EventPublishResult<ApprovalReview>()));

            // when
            ApprovalReview actualApprovalReview =
                await this.approvalReviewService.DismissApprovalReviewAsync(
                    storageApprovalReview.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalReview.StatusId.Should().Be(ApprovalStatus.Dismissed);

            // both tiers are skipped together — the second is the same question as the first,
            // narrowed to the entity under review, so admitting the workflow past one and not
            // the other would refuse it for having no roles either way
            this.accessBrokerMock.Verify(broker =>
                    broker.MayDismissApprovalReviewAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<SecurityContext>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldRefuseASystemIdentityClaimedOnAnInboundEnvelopeOnDismissAsync()
        {
            // given: on the event path the security context is deserialized and unverified
            // (§14.6 rule 4), so a caller who can reach the public ApprovalReview-Dismissing
            // address would otherwise dismiss any review in the system by setting one JSON
            // property — withdrawing the very verdicts that were blocking an approval.
            //
            // Roleless, exactly as the genuine system context is, so the ONLY thing that could
            // authorize this is the claim.
            ApprovalReview storageApprovalReview = CreateRandomApprovalReview();
            storageApprovalReview.StatusId = ApprovalStatus.Approved;

            var requestEnvelope = new EventEnvelope<ApprovalReview>
            {
                SecurityContext = CreateSystemSecurityContext(),
                Content = new ApprovalReview { Id = storageApprovalReview.Id },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalReviewByIdAsync(
                    storageApprovalReview.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalReview);

            var unauthorizedApprovalReviewException =
                new UnauthorizedApprovalReviewException(
                    message: "The current user is not allowed to dismiss this approval review.");

            // when
            ValueTask<EventEnvelope<ApprovalReview>?> dismissTask =
                this.approvalReviewService.OnDismissingApprovalReviewAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ApprovalReviewValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewValidationException>(
                    dismissTask.AsTask);

            // then: treated as the ordinary unprivileged caller it is, and refused at the
            // publisher tier it does not hold
            actualException.InnerException.Should()
                .BeEquivalentTo(unauthorizedApprovalReviewException);

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
