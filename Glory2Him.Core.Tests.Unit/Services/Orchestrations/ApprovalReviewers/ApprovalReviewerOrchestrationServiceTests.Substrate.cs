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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Approvals;
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

        /// <summary>
        /// §7.9 rule 8, and the whole of criterion 2's "one subscription hears all three routes".
        /// The three ways a round can close — the manual decision, the <c>BlockOnReject</c>
        /// rejection and the automatic approval — all write the outcome through
        /// <c>ModifyApprovalAsync</c>, so they arrive here as one fact and no enumeration of the
        /// sites has to be kept in step with anything.
        ///
        /// <para>Both outcomes retire, because both CLOSE: a rejection is as final for an
        /// unanswered invitation as an approval.</para>
        /// </summary>
        [Theory]
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        public async Task ShouldRetireTheOutstandingInvitationsWhenTheRoundClosesAsync(
            ApprovalStatus closingStatus)
        {
            // given: two people were asked and neither answered before the round was decided
            Guid approvalId = Guid.NewGuid();
            Guid firstRequestId = Guid.NewGuid();
            Guid secondRequestId = Guid.NewGuid();

            SetupRetirableApprovalReviewRequests(approvalId, firstRequestId, secondRequestId);
            SetupClosedRoundRetirement();

            // when
            await this.approvalReviewerOrchestrationService.OnApprovalModifiedAsync(
                CreateApprovalModifiedEnvelope(approvalId, closingStatus),
                TestContext.Current.CancellationToken);

            // then: EVERY outstanding row, not just the first one found
            this.approvalReviewRequestWorkflowServiceMock.Verify(service =>
                service.RetireClosedRoundApprovalReviewRequestAsync(
                    firstRequestId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.approvalReviewRequestWorkflowServiceMock.Verify(service =>
                service.RetireClosedRoundApprovalReviewRequestAsync(
                    secondRequestId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // And the ANSWERED retirement is not the verb reached: the two carry different
            // sentences, and a round closing on somebody is not the same as them answering.
            this.approvalReviewRequestWorkflowServiceMock.Verify(service =>
                service.RetireAnsweredApprovalReviewRequestAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            // The UNFILTERED read, keyed on this round. The caller-facing read applies §14.7
            // posture D, and two of the three routes have no moderator on them at all.
            this.accessBrokerMock.Verify(broker =>
                broker.FindRetirableApprovalReviewRequestIdsAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// §12.5.4 business rule 4(i). Only three of the workflow seam's six call sites close a
        /// round; the others publish <c>-Modified</c> on an OPEN one — the entity-status sync,
        /// the reinstatement of a soft-deleted approval, and §8.6 HR-4's reset. Retiring here
        /// would cancel a round's reviewers for the crime of somebody having looked at it.
        ///
        /// <para><b>This is the gate that was dead code and comes alive with the subscription.</b>
        /// It used to sit in <c>RetireUnansweredApprovalReviewRequestsAsync</c> under a comment
        /// recording that all three callers reached it having just written an outcome, so it
        /// refused nothing and deleting it would turn no test red. A bare subscription is what
        /// finally gives it something to refuse — so what this asserts is the ENVELOPE GATE, and
        /// it goes red if that gate is removed.</para>
        ///
        /// <para><b>Every non-closing status, not a sample of them.</b> The gate admits
        /// <c>Approved</c> and <c>Rejected</c>, so what it must refuse is the other three —
        /// <c>Dismissed</c> included, which is reachable and would otherwise be the one value of
        /// the enum nothing here exercises.</para>
        /// </summary>
        [Theory]
        [InlineData(ApprovalStatus.Draft)]
        [InlineData(ApprovalStatus.Submitted)]
        [InlineData(ApprovalStatus.Dismissed)]
        public async Task ShouldNotRetireAnythingWhileTheRoundIsStillOpenAsync(
            ApprovalStatus openStatus)
        {
            // given
            Guid approvalId = Guid.NewGuid();

            SetupRetirableApprovalReviewRequests(approvalId, Guid.NewGuid());
            SetupClosedRoundRetirement();

            // when
            await this.approvalReviewerOrchestrationService.OnApprovalModifiedAsync(
                CreateApprovalModifiedEnvelope(approvalId, openStatus),
                TestContext.Current.CancellationToken);

            // then: NO WRITE, and no GATHER either. The gate runs before the broker is reached,
            // which is what makes a -Modified that closed nothing cost one comparison.
            this.approvalReviewRequestWorkflowServiceMock.Verify(service =>
                service.RetireClosedRoundApprovalReviewRequestAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.accessBrokerMock.Verify(broker =>
                broker.FindRetirableApprovalReviewRequestIdsAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// §8.6 HR-4's override moves a decided round back to <c>Submitted</c>, which is the
        /// opposite of closing it, and a moderator then re-invites.
        ///
        /// <para><b>It passes because the ENVELOPE GATE refuses <c>Submitted</c>.</b> That is a
        /// different mechanism from the one this test used to assert: before the move it passed
        /// because <c>ResetApprovalAsync</c> never called the retirement helper at all, which
        /// stopped being a mechanism the moment the trigger became a fact every writer of the
        /// approval publishes. Carried over unchanged the test would have asserted the right
        /// outcome for a reason that no longer exists, and would have kept passing with the gate
        /// deleted.</para>
        ///
        /// <para>§7.9 rule 8 also rules that a reset does NOT bring retired invitations back: a
        /// moderator asks again. Nothing here resurrects a row, and there is deliberately no verb
        /// that could.</para>
        /// </summary>
        [Fact]
        public async Task ShouldNotRetireAnythingWhenAnAdministratorResetsTheRoundAsync()
        {
            // given: the reset's own write, which publishes -Modified carrying Submitted
            Guid approvalId = Guid.NewGuid();

            SetupRetirableApprovalReviewRequests(approvalId, Guid.NewGuid());
            SetupClosedRoundRetirement();

            // when
            await this.approvalReviewerOrchestrationService.OnApprovalModifiedAsync(
                CreateApprovalModifiedEnvelope(approvalId, ApprovalStatus.Submitted),
                TestContext.Current.CancellationToken);

            // then: the invitations a moderator is about to re-issue survive the sweep
            this.approvalReviewRequestWorkflowServiceMock.Verify(service =>
                service.RetireClosedRoundApprovalReviewRequestAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.accessBrokerMock.Verify(broker =>
                broker.FindRetirableApprovalReviewRequestIdsAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// The gate reads <c>envelope.Content.ApprovalStatus</c>, which is signed system data
        /// rather than a caller's claim — so the verification has to come first or the gate is
        /// reading whatever the sender felt like writing.
        /// </summary>
        [Fact]
        public async Task ShouldNotRetireAnythingWhenTheClosedRoundEnvelopeFailsVerificationAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();

            SetupRetirableApprovalReviewRequests(approvalId, Guid.NewGuid());
            SetupClosedRoundRetirement();

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<It.IsAnyType>>(),
                    It.IsAny<string>(),
                    It.IsAny<EnvelopeDirection>()))
                        .ReturnsAsync(false);

            // when
            await Assert.ThrowsAnyAsync<Exception>(() =>
                this.approvalReviewerOrchestrationService.OnApprovalModifiedAsync(
                    CreateApprovalModifiedEnvelope(approvalId, ApprovalStatus.Approved),
                    TestContext.Current.CancellationToken).AsTask());

            // then
            this.accessBrokerMock.Verify(broker =>
                broker.FindRetirableApprovalReviewRequestIdsAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.approvalReviewRequestWorkflowServiceMock.Verify(service =>
                service.RetireClosedRoundApprovalReviewRequestAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// <c>Approval-Modified</c> is the first of the <c>Approval</c> entity's own FACT
        /// addresses to carry a subscription — the five existing registrations all bind command
        /// addresses — so the name it verifies under is worth pinning against the literal rather
        /// than inferred from the address's tense. It is composed from the entity and the
        /// operation, never from the tense.
        /// </summary>
        [Fact]
        public async Task ShouldVerifyTheApprovalModifiedEnvelopeUnderItsPublishedNameAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();

            SetupRetirableApprovalReviewRequests(approvalId, Guid.NewGuid());
            SetupClosedRoundRetirement();

            // when
            await this.approvalReviewerOrchestrationService.OnApprovalModifiedAsync(
                CreateApprovalModifiedEnvelope(approvalId, ApprovalStatus.Approved),
                TestContext.Current.CancellationToken);

            // then
            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<Approval>>(),
                    "ApprovalModified",
                    EnvelopeDirection.Request),
                Times.Once);
        }

        private static EventEnvelope<Approval> CreateApprovalModifiedEnvelope(
            Guid approvalId,
            ApprovalStatus approvalStatus) =>
            new EventEnvelope<Approval>
            {
                Content = new Approval
                {
                    Id = approvalId,
                    EntityType = EntityType.ContentItem,
                    EntityId = Guid.NewGuid(),
                    ApprovalStatus = approvalStatus,
                },

                SecurityContext = new SecurityContext { IsSystemIdentity = true },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

        private void SetupRetirableApprovalReviewRequests(
            Guid approvalId,
            params Guid[] requestIds) =>
            this.accessBrokerMock.Setup(broker =>
                broker.FindRetirableApprovalReviewRequestIdsAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(requestIds.ToList());

        // The workflow seam echoes back a retired row, so a test can assert on the argument and
        // on what came back and know they are the same row.
        private void SetupClosedRoundRetirement() =>
            this.approvalReviewRequestWorkflowServiceMock.Setup(service =>
                service.RetireClosedRoundApprovalReviewRequestAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Guid approvalReviewRequestId, CancellationToken _) =>
                            new ApprovalReviewRequest
                            {
                                Id = approvalReviewRequestId,
                                IsDeleted = true,
                            });

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
