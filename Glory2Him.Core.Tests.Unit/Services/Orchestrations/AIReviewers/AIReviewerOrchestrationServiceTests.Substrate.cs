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
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.AIReviewers
{
    /// <summary>
    /// §8.6.2.1's automatic assignment — a moderator opening a round that has entered review
    /// finds Berean already on it, with nobody having pressed anything.
    ///
    /// <para>Two subscriptions, <c>Approval-Added</c> and <c>Approval-Modified</c>, delegating to
    /// ONE private body: the gates, the write and the failure posture are literally the same
    /// code, so a rule cannot be fixed on one address and left broken on the other. What differs
    /// is the accepted event name alone.</para>
    ///
    /// <para>There is no caller to authorise here and that is the point — the identity on the
    /// inbound envelope belongs to whoever moved the round, usually an author with no review role
    /// at all. What binds instead is the ENVELOPE, verified before a single field is read from
    /// it.</para>
    /// </summary>
    public partial class AIReviewerOrchestrationServiceTests
    {
        /// <summary>
        /// Criterion 1, and criterion 7's whole assertion: the write goes through the WORKFLOW
        /// seam with this round's id, and never through the caller-facing foundation whose gate
        /// asks for a review-tier role the system identity does not hold.
        /// </summary>
        [Fact]
        public async Task ShouldAssignBereanWhenARoundOpensAlreadySubmittedAsync()
        {
            // given: a round created at Submitted under a policy that asks for Berean
            Guid approvalId = Guid.NewGuid();

            SetupAutomaticAIReviewerPolicy(approvalId, isAutomaticallyRequested: true);
            SetupAIReviewerEverAssigned(approvalId, isEverAssigned: false);
            SetupAutomaticAIReviewerAssignmentWrite();

            // when
            await this.aiReviewerOrchestrationService.OnApprovalAddedAsync(
                CreateApprovalFactEnvelope(approvalId, ApprovalStatus.Submitted),
                TestContext.Current.CancellationToken);

            // then: the seam, handed the act and no context — it mints the system identity for
            // itself, which is what makes the flag unforgeable by construction (#534)
            this.aiReviewerAssignmentWorkflowServiceMock.Verify(service =>
                service.AddAutomaticAIReviewerAssignmentAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // NOT the caller-facing door. Its gate asks for a tier nobody on this path holds, and
            // a round assigned through it would record the wrong author.
            this.aiReviewerAssignmentServiceMock.Verify(service =>
                service.AddAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// Criterion 2. The second subscription, covering both ways a round reaches
        /// <c>Submitted</c> after it opened — the submission of a round opened at <c>Draft</c>,
        /// and §8.6 HR-4's reset re-opening a decided one. One address hears every route,
        /// because all of them write through <c>ModifyApprovalAsync</c>.
        /// </summary>
        [Fact]
        public async Task ShouldAssignBereanWhenARoundReachesSubmittedLaterAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();

            SetupAutomaticAIReviewerPolicy(approvalId, isAutomaticallyRequested: true);
            SetupAIReviewerEverAssigned(approvalId, isEverAssigned: false);
            SetupAutomaticAIReviewerAssignmentWrite();

            // when
            await this.aiReviewerOrchestrationService.OnApprovalModifiedAsync(
                CreateApprovalFactEnvelope(approvalId, ApprovalStatus.Submitted),
                TestContext.Current.CancellationToken);

            // then
            this.aiReviewerAssignmentWorkflowServiceMock.Verify(service =>
                service.AddAutomaticAIReviewerAssignmentAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.aiReviewerAssignmentServiceMock.Verify(service =>
                service.AddAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // Both handlers, driven through one switch so every GATE below can be a theory over the
        // pair rather than a test written twice. That is criterion 2's "one private body" made
        // observable: the two differ only in the accepted event name, so a rule fixed on one
        // address and left broken on the other fails here rather than shipping.
        private ValueTask<EventEnvelope<Approval>?> DeliverApprovalFactAsync(
            string approvalEventOperation,
            EventEnvelope<Approval> envelope,
            CancellationToken cancellationToken) =>
            approvalEventOperation switch
            {
                AddedOperation =>
                    this.aiReviewerOrchestrationService.OnApprovalAddedAsync(
                        envelope, cancellationToken),

                ModifiedOperation =>
                    this.aiReviewerOrchestrationService.OnApprovalModifiedAsync(
                        envelope, cancellationToken),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(approvalEventOperation),
                    approvalEventOperation,
                    "This service subscribes to no other Approval fact address."),
            };

        private const string AddedOperation = "Added";
        private const string ModifiedOperation = "Modified";

        // The signed fact the foundation publishes about a round. Every gate but the verdict and
        // the presence check reads this envelope and nothing else — it is inside the HMAC, which
        // is what makes a status gate a gate rather than a reading of whatever the sender felt
        // like writing.
        private static EventEnvelope<Approval> CreateApprovalFactEnvelope(
            Guid approvalId,
            ApprovalStatus approvalStatus,
            bool isDeleted = false,
            SecurityContext securityContext = null) =>
            new EventEnvelope<Approval>
            {
                Content = new Approval
                {
                    Id = approvalId,
                    EntityType = EntityType.Tag,
                    EntityId = Guid.NewGuid(),
                    ApprovalStatus = approvalStatus,
                    IsDeleted = isDeleted,
                },

                SecurityContext = securityContext
                    ?? new SecurityContext { IsAuthenticated = true },

                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

        // The ONE composed field §8.6.1 rule 4 allows this path to read. IsOffered is carried
        // alongside it deliberately at the value the composition implies — the decision function
        // composes IsAutomaticallyRequested as IsOffered && IsAIReviewerAutomaticallyRequested,
        // so a verdict answering true here has already answered the offer, and a test that let
        // the two disagree would be arranging a state the decision function cannot produce.
        //
        // Keyed on the approval id rather than It.IsAny, so a test cannot pass by answering a
        // question about a different round.
        private void SetupAutomaticAIReviewerPolicy(
            Guid approvalId,
            bool isAutomaticallyRequested) =>
            this.accessBrokerMock.Setup(broker =>
                broker.ResolveAIReviewerPolicyByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new AIReviewerPolicyVerdict
                        {
                            IsOffered = isAutomaticallyRequested,
                            IsAutomaticallyRequested = isAutomaticallyRequested,
                        });

        private void SetupAIReviewerEverAssigned(Guid approvalId, bool isEverAssigned) =>
            this.accessBrokerMock.Setup(broker =>
                broker.IsAIReviewerEverAssignedAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(isEverAssigned);

        // The seam echoes back the row it wrote, so a test can assert on the argument and on what
        // came back and know they are the same round.
        private void SetupAutomaticAIReviewerAssignmentWrite() =>
            this.aiReviewerAssignmentWorkflowServiceMock.Setup(service =>
                service.AddAutomaticAIReviewerAssignmentAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Guid approvalId, CancellationToken _) =>
                            new AIReviewerAssignment
                            {
                                Id = Guid.NewGuid(),
                                ApprovalId = approvalId,
                                IsAIReviewCompleted = false,
                                IsAIReviewCommentsPresent = false,
                            });
    }
}
