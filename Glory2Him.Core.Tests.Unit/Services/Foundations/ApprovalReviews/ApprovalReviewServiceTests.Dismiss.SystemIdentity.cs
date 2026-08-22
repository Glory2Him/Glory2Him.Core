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
        public async Task ShouldMintTheSystemIdentityItselfOnWorkflowDismissAsync()
        {
            // given: the ORDINARY case — an author revising their own submitted content. They
            // hold no publisher tier and never will, and HR-1 means they cannot have reviewed
            // their own work either. Under the caller's identity this dismissal is refused, and
            // the round then keeps approvals given to text that no longer exists.
            //
            // Automatic dismissal is not a user action, any more than automatic approval is
            // (#196 decision 9). So the workflow seam does not ask the author for authority it
            // could never have — it mints the system context itself. The caller supplies
            // nothing; that is what keeps the flag unusable by anyone who merely claims it.
            this.ambientSecurityContext = new SecurityContext
            {
                IsAuthenticated = true,
                SubjectId = GetRandomString(),
                Roles = []
            };

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
                await this.approvalReviewWorkflowService.DismissStaleApprovalReviewAsync(
                    storageApprovalReview.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalReview.StatusId.Should().Be(ApprovalStatus.Dismissed,
                because: "the workflow's own dismissal must succeed for a caller who holds no " +
                    "publisher tier — that caller is the author whose edit invalidated the " +
                    "review, and refusing them leaves the round carrying a stale approval");

            // Minted HERE, not handed in. The flag is honoured only because this service made
            // the context; an envelope arriving over the public event address gets it refused.
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateSystemAsync(It.IsAny<ApprovalReview>()),
                Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.IsAny<ApprovalReview>()),
                Times.Never,
                failMessage: "minting from the ambient caller would put the author's identity " +
                    "on a write they are not permitted to make, and the gate would refuse it");

            // Both tiers skipped together, as on the ambient-system path beside this.
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
