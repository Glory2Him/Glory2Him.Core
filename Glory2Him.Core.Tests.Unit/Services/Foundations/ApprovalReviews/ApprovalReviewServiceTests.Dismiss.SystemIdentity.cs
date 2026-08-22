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
    /// The system identity as the ONLY admissible actor on dismissal (#295).
    /// </summary>
    /// <remarks>
    /// Two tests stood here and are gone with the routes they covered. One dismissed under an
    /// AMBIENT system context through the public verb — unreachable now, because that verb no
    /// longer exists and no ambient context can carry the flag. The other refused a system
    /// identity asserted on an inbound envelope, which needed a request address to assert it on;
    /// that address is gone too.
    ///
    /// What replaced them is stronger than either: the capability is absent rather than guarded.
    /// The refusal itself is still proven, in
    /// <c>ShouldRefuseDismissalWhenTheContextIsNotTheWorkflowsOwnAsync</c>.
    /// </remarks>
    public partial class ApprovalReviewServiceTests
    {
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

            // The MINTED context is what gets stamped, not the ambient one. Without this the
            // service could satisfy the gate with CreateSystemAsync and then hand the audit
            // broker the caller's own context — the authority right and the attribution wrong,
            // with every other assertion here still green.
            //
            // SubjectId survives the mint because the audit answer to "who caused this" is a
            // person; the roles do not, because the flag is the whole of the authority.
            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<ApprovalReview>(),
                    It.Is<SecurityContext>(securityContext =>
                        securityContext.IsSystemIdentity
                            && securityContext.SubjectId
                                == this.ambientSecurityContext.SubjectId
                            && securityContext.Roles.Count == 0)),
                Times.Once,
                failMessage: "the row must be stamped from the context this service minted, " +
                    "carrying the deciding human forward and no roles");
        }
    }
}
