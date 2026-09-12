// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, 'I am the way and the truth and the life.
//                  No one comes to the Father except through me.'"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Threading;
using System.Threading.Tasks;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.ApprovalReviewers
{
    /// <summary>
    /// §12.5.4 business rule 4 — the two retirements are SUBSCRIPTIONS on this service rather
    /// than calls on the round's. §7.9 rule 6 hears <c>ApprovalReview-Added</c>: the invited
    /// person answered, so their invitation retires itself.
    ///
    /// <para>Neither retirement has a caller to authorise, and that is the point — both run under
    /// the system identity the workflow seam mints for itself. What binds instead is the
    /// ENVELOPE, verified before a single field is read from it, which is why the verification
    /// test sits beside the happy path rather than filed away as a validation.</para>
    /// </summary>
    public partial class ApprovalReviewerOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldRetireTheInvitationWhenItsTargetRecordsAReviewAsync()
        {
            // given: the person who has just reviewed is one the round had invited
            Guid approvalId = Guid.NewGuid();
            string invitedUserId = Guid.NewGuid().ToString();
            Guid requestId = Guid.NewGuid();

            SetupReviewerScopeById(
                approvalId: approvalId,
                requestId: requestId,
                requestedUserId: invitedUserId);

            // when
            await this.approvalReviewerOrchestrationService.OnApprovalReviewAddedAsync(
                CreateReviewAddedEnvelope(approvalId, invitedUserId),
                TestContext.Current.CancellationToken);

            // then: retired through the WORKFLOW seam, which mints the system identity itself —
            // DeletedBy must say "answered", not name whoever triggered the delivery
            this.approvalReviewRequestWorkflowServiceMock.Verify(service =>
                service.RetireAnsweredApprovalReviewRequestAsync(
                    requestId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// The common case: most reviews are recorded by people who were never formally asked, so
        /// there is nothing to retire and the delivery stays silent.
        /// </summary>
        [Fact]
        public async Task ShouldNotRetireAnythingWhenTheReviewerWasNeverInvitedAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();

            SetupReviewerScopeById(
                approvalId: approvalId,
                requestId: Guid.NewGuid(),
                requestedUserId: Guid.NewGuid().ToString());

            // when
            await this.approvalReviewerOrchestrationService.OnApprovalReviewAddedAsync(
                CreateReviewAddedEnvelope(approvalId, Guid.NewGuid().ToString()),
                TestContext.Current.CancellationToken);

            // then
            this.approvalReviewRequestWorkflowServiceMock.Verify(service =>
                service.RetireAnsweredApprovalReviewRequestAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// The property the <c>onVerifiedAsync</c> hook existed to hold, and it has to survive
        /// the move: the signature check precedes the gather and the write. Retiring on the
        /// strength of an unverified envelope would let anyone reaching the address clear the
        /// panel.
        /// </summary>
        [Fact]
        public async Task ShouldNotRetireAnythingWhenTheEnvelopeFailsVerificationAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            string invitedUserId = Guid.NewGuid().ToString();

            SetupReviewerScopeById(
                approvalId: approvalId,
                requestId: Guid.NewGuid(),
                requestedUserId: invitedUserId);

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<It.IsAnyType>>(),
                    It.IsAny<string>(),
                    It.IsAny<EnvelopeDirection>()))
                        .ReturnsAsync(false);

            // when
            await Assert.ThrowsAnyAsync<Exception>(() =>
                this.approvalReviewerOrchestrationService.OnApprovalReviewAddedAsync(
                    CreateReviewAddedEnvelope(approvalId, invitedUserId),
                    TestContext.Current.CancellationToken).AsTask());

            // then: not the gather either. The refusal lands before a single field of the
            // envelope has been trusted, which is what "verified before it is read" means.
            this.accessBrokerMock.Verify(broker =>
                broker.RetrieveApprovalReviewerScopeByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.approvalReviewRequestWorkflowServiceMock.Verify(service =>
                service.RetireAnsweredApprovalReviewRequestAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// The event name is bound INTO the HMAC, so a handler expecting the wrong one refuses a
        /// genuine envelope it was correctly delivered — and refuses it silently, because the
        /// fact still arrives. The DIRECTION is the same trap: <c>EventBroker</c> signs the
        /// publish leg as <c>Request</c>, so a receiver asking for <c>Reply</c> would fail every
        /// verification with nothing to show for it.
        /// </summary>
        [Fact]
        public async Task ShouldVerifyTheReviewAddedEnvelopeUnderItsPublishedNameAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            string invitedUserId = Guid.NewGuid().ToString();

            SetupReviewerScopeById(
                approvalId: approvalId,
                requestId: Guid.NewGuid(),
                requestedUserId: invitedUserId);

            // when
            await this.approvalReviewerOrchestrationService.OnApprovalReviewAddedAsync(
                CreateReviewAddedEnvelope(approvalId, invitedUserId),
                TestContext.Current.CancellationToken);

            // then
            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<ApprovalReview>>(),
                    "ApprovalReviewAdded",
                    EnvelopeDirection.Request),
                Times.Once);
        }

        private static EventEnvelope<ApprovalReview> CreateReviewAddedEnvelope(
            Guid approvalId,
            string createdBy) =>
            new EventEnvelope<ApprovalReview>
            {
                Content = new ApprovalReview
                {
                    Id = Guid.NewGuid(),
                    ApprovalId = approvalId,
                    CreatedBy = createdBy,
                },

                SecurityContext = new SecurityContext { IsAuthenticated = true },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

        private void SetupReviewerScopeById(
            Guid approvalId,
            Guid requestId,
            string requestedUserId) =>
            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveApprovalReviewerScopeByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new ApprovalReviewerScope
                        {
                            ApprovalId = approvalId,
                            ApprovalStatus = ApprovalStatus.Submitted,
                            EntityCreatedBy = "somebody-else",
                            RoleSubjects = Array.Empty<RoleSubject>(),
                            ActiveReviewerUserIds = Array.Empty<string>(),
                            RecordedReviewerUserIds = Array.Empty<string>(),

                            ActiveRequests = new[]
                            {
                                new ActiveReviewRequest
                                {
                                    Id = requestId,
                                    RequestedUserId = requestedUserId,
                                }
                            },
                        });
    }
}
