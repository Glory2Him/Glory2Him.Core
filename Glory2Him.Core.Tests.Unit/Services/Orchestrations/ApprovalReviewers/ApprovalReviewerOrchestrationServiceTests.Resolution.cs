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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Orchestrations.ApprovalReviewers.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.ApprovalReviewers
{
    /// <summary>
    /// How every operation here finds the round behind the entity it was handed (§12.5.4 business
    /// rule 2, §16.7.2, §9.7.2 rule 1).
    /// </summary>
    public partial class ApprovalReviewerOrchestrationServiceTests
    {
        /// <summary>
        /// ONE broker read, keyed on the ENTITY. The old shape resolved the approval through
        /// <c>IApprovalWorkflowService.FindApprovalByEntityAsync</c> and then gathered the scope
        /// by id, which read the same row twice; the by-entity overload does both at once.
        ///
        /// <para>An economy rather than a new capability, and the assertions say so both ways:
        /// the by-entity form is asked, and neither the foundation probe nor the by-id gather is
        /// asked at all. Without the second half this case passes on a service that still takes
        /// two reads and simply happens to answer.</para>
        /// </summary>
        [Fact]
        public async Task ShouldResolveTheRoundThroughTheEntityKeyedScopeReadAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            EntityType entityType = EntityType.ContentItem;
            Guid entityId = Guid.NewGuid();
            Guid approvalId = Guid.NewGuid();

            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveApprovalReviewerScopeByEntityAsync(
                    entityType,
                    entityId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new ApprovalReviewerScope
                        {
                            ApprovalId = approvalId,
                            ApprovalStatus = ApprovalStatus.Submitted,
                            EntityCreatedBy = "the-entity-owner",
                            RoleSubjects = Array.Empty<RoleSubject>(),
                            ActiveReviewerUserIds = Array.Empty<string>(),
                            RecordedReviewerUserIds = Array.Empty<string>(),
                            ActiveRequests = Array.Empty<ActiveReviewRequest>(),
                        });

            this.approvalReviewRequestServiceMock.Setup(service =>
                service.RetrieveApprovalReviewRequestsByApprovalIdAsync(
                    approvalId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<ApprovalReviewRequest>());

            // when
            IReadOnlyList<ApprovalReviewRequest> approvalReviewRequests =
                await this.approvalReviewerOrchestrationService
                    .RetrieveApprovalReviewRequestsAsync(
                        entityType,
                        entityId,
                        TestContext.Current.CancellationToken);

            // then
            approvalReviewRequests.Should().BeEmpty();

            this.accessBrokerMock.Verify(broker =>
                broker.RetrieveApprovalReviewerScopeByEntityAsync(
                    entityType, entityId, It.IsAny<CancellationToken>()),
                Times.Once);

            // and the two reads it replaced are not made at all
            this.approvalServiceMock.Verify(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.accessBrokerMock.Verify(broker =>
                broker.RetrieveApprovalReviewerScopeByIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // The scope the entity-keyed gather hands back once a round exists. Deliberately spare —
        // the cases below are about WHETHER a round was found or opened, not about who is on it.
        private static ApprovalReviewerScope EmptyScope(Guid approvalId) =>
            new ApprovalReviewerScope
            {
                ApprovalId = approvalId,
                ApprovalStatus = ApprovalStatus.Submitted,
                EntityCreatedBy = "the-entity-owner",
                RoleSubjects = Array.Empty<RoleSubject>(),
                ActiveReviewerUserIds = Array.Empty<string>(),
                RecordedReviewerUserIds = Array.Empty<string>(),
                ActiveRequests = Array.Empty<ActiveReviewRequest>(),
            };

        private void SetupRepairedApprovalEcho() =>
            this.approvalServiceMock.Setup(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Approval approval, CancellationToken _) => approval);

        /// <summary>
        /// §9.7.2 rule 1's read-triggered repair, kept by the move. The panel keys on the
        /// approval, so an entity whose round was never opened would answer a moderator 404 on a
        /// picker they can do nothing about.
        ///
        /// <para>Re-read rather than trusted: the answer comes from a SECOND gather, so a repair
        /// that could not run still ends in the honest not-found rather than in a round this
        /// service invented locally.</para>
        /// </summary>
        [Fact]
        public async Task ShouldOpenTheMissingRoundAndRetryTheEntityKeyedReadAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            EntityType entityType = EntityType.ContentItem;
            Guid entityId = Guid.NewGuid();
            Guid repairedApprovalId = Guid.NewGuid();

            this.accessBrokerMock.SetupSequence(broker =>
                broker.RetrieveApprovalReviewerScopeByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ApprovalReviewerScope)null)
                        .ReturnsAsync(EmptyScope(repairedApprovalId));

            SetupEntityApprovalStatus(entityType, entityId, ApprovalStatus.Submitted);
            SetupRepairedApprovalEcho();

            this.approvalReviewRequestServiceMock.Setup(service =>
                service.RetrieveApprovalReviewRequestsByApprovalIdAsync(
                    repairedApprovalId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<ApprovalReviewRequest>());

            // when
            IReadOnlyList<ApprovalReviewRequest> approvalReviewRequests =
                await this.approvalReviewerOrchestrationService
                    .RetrieveApprovalReviewRequestsAsync(
                        entityType,
                        entityId,
                        TestContext.Current.CancellationToken);

            // then: the read ANSWERED rather than refusing
            approvalReviewRequests.Should().BeEmpty();

            // and the round was OPENED at the entity's own status, carrying a minted id — the
            // foundation stamps the audit values itself but refuses an empty Id
            this.approvalServiceMock.Verify(service =>
                service.AddApprovalAsync(
                    It.Is<Approval>(approval =>
                        approval.EntityType == entityType
                            && approval.EntityId == entityId
                            && approval.Id != Guid.Empty
                            && approval.ApprovalStatus == ApprovalStatus.Submitted),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // and the gather was asked TWICE — once before the repair and once after it
            this.accessBrokerMock.Verify(broker =>
                broker.RetrieveApprovalReviewerScopeByEntityAsync(
                    entityType, entityId, It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        /// <summary>
        /// And a DRAFT entity opens a Draft round, which is the other half of §9.2 rules 1-2:
        /// nothing enters review on a status nobody offered. The read still ends in the honest
        /// not-found here, because the retry is arranged to find nothing — what this asserts is
        /// the status the repair chose, not the answer the caller got.
        /// </summary>
        [Fact]
        public async Task ShouldOpenTheRepairedRoundAtDraftWhenTheEntityIsStillADraftAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            EntityType entityType = EntityType.ContentItem;
            Guid entityId = Guid.NewGuid();

            SetupEntityApprovalStatus(entityType, entityId, ApprovalStatus.Draft);
            SetupRepairedApprovalEcho();

            // when
            ValueTask<IReadOnlyList<ApprovalReviewRequest>> retrieveTask =
                this.approvalReviewerOrchestrationService.RetrieveApprovalReviewRequestsAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<ApprovalReviewerOrchestrationValidationException>(
                retrieveTask.AsTask);

            // then
            this.approvalServiceMock.Verify(service =>
                service.AddApprovalAsync(
                    It.Is<Approval>(approval =>
                        approval.ApprovalStatus == ApprovalStatus.Draft),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// §14.5 rule 3: a taken-down entity answers not-found on every one of these operations,
        /// for every caller including <c>Administrators</c>, and no round is opened for it.
        /// VISIBLE rather than merely present — a taken-down entity keeps its
        /// <c>ApprovalStatus</c> (§9.7.6) and this repository has no EF global query filters, so
        /// a round would otherwise be minted for a tombstone.
        ///
        /// <para><b>What refuses here is the RESOLVER's gate, which runs before the repair is
        /// reached at all.</b> The repair carries its own copy, which is therefore unreachable
        /// from this entry point and deliberate rather than redundant (§14.6 rule 2): that helper
        /// mints an <c>Approval</c> for an entity id that came straight off a route, so it
        /// defends itself rather than trusting whatever calls it next. Deleting the repair's copy
        /// would turn no test red today, and the comment above it says so.</para>
        /// </summary>
        [Fact]
        public async Task ShouldNotOpenARoundForAnEntityThatCannotBeSeenAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            EntityType entityType = EntityType.ContentItem;
            Guid entityId = Guid.NewGuid();

            SetupEntityVisibility(isEntityVisible: false);
            SetupEntityApprovalStatus(entityType, entityId, ApprovalStatus.Submitted);
            SetupRepairedApprovalEcho();

            // when
            ValueTask<IReadOnlyList<ApprovalReviewRequest>> retrieveTask =
                this.approvalReviewerOrchestrationService.RetrieveApprovalReviewRequestsAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewerOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewerOrchestrationValidationException>(
                    retrieveTask.AsTask);

            // then: reported exactly as a missing round is, so the two cannot be told apart
            actualException.InnerException.Should()
                .BeOfType<NotFoundApprovalReviewerOrchestrationException>();

            actualException.InnerException!.Message.Should().Be(
                $"Approval not found for {entityType} with id: {entityId}.");

            this.approvalServiceMock.Verify(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// AND GATED ON THE ENTITY STILL BEING IN PLAY (§9.8). A round opens at the entity's own
        /// status, and the foundation admits only <c>Draft</c> or <c>Submitted</c> on an add — so
        /// a DECIDED entity has no status this repair could legally open a round at, and opening
        /// one at <c>Draft</c> beneath an <c>Approved</c> row would write a divergence the
        /// modified flow could never reconcile. A status that could not be read at all is refused
        /// for the same reason.
        /// </summary>
        [Theory]
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        [InlineData(null)]
        public async Task ShouldNotOpenARoundForAnEntityThatIsNoLongerInPlayAsync(
            ApprovalStatus? entityApprovalStatus)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            EntityType entityType = EntityType.ContentItem;
            Guid entityId = Guid.NewGuid();

            SetupEntityApprovalStatus(entityType, entityId, entityApprovalStatus);
            SetupRepairedApprovalEcho();

            // when
            ValueTask<IReadOnlyList<ApprovalReviewRequest>> retrieveTask =
                this.approvalReviewerOrchestrationService.RetrieveApprovalReviewRequestsAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewerOrchestrationValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewerOrchestrationValidationException>(
                    retrieveTask.AsTask);

            // then: the honest not-found rather than a fabricated round saying the content has
            // not been submitted yet
            actualException.InnerException.Should()
                .BeOfType<NotFoundApprovalReviewerOrchestrationException>();

            this.approvalServiceMock.Verify(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// THE PRE-INSERT RE-PROBE, and why it is kept. The repair re-checks immediately before
        /// writing, so a round that appeared while its two gates were running is FOUND rather than
        /// collided with — the same point the approval round's own retrieve-or-create re-probes
        /// at. Dropping it would widen the collision window to span two extra broker calls and
        /// turn a race that repair absorbs into a refusal.
        /// </summary>
        [Fact]
        public async Task ShouldNotOpenARoundWhenAConcurrentRepairAlreadyOpenedItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            EntityType entityType = EntityType.ContentItem;
            Guid entityId = Guid.NewGuid();
            Guid winningApprovalId = Guid.NewGuid();

            this.accessBrokerMock.SetupSequence(broker =>
                broker.RetrieveApprovalReviewerScopeByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ApprovalReviewerScope)null)
                        .ReturnsAsync(EmptyScope(winningApprovalId));

            SetupEntityApprovalStatus(entityType, entityId, ApprovalStatus.Submitted);
            SetupRepairedApprovalEcho();

            // the winner's row, seen by the repair's own pre-insert probe
            this.approvalServiceMock.Setup(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new ApprovalEntityMatch
                        {
                            Id = winningApprovalId,
                            ApprovalStatus = ApprovalStatus.Submitted,
                            IsDeleted = false,
                        });

            this.approvalReviewRequestServiceMock.Setup(service =>
                service.RetrieveApprovalReviewRequestsByApprovalIdAsync(
                    winningApprovalId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<ApprovalReviewRequest>());

            // when
            IReadOnlyList<ApprovalReviewRequest> approvalReviewRequests =
                await this.approvalReviewerOrchestrationService
                    .RetrieveApprovalReviewRequestsAsync(
                        entityType,
                        entityId,
                        TestContext.Current.CancellationToken);

            // then: the winner's round answered, and nothing was written
            approvalReviewRequests.Should().BeEmpty();

            this.approvalServiceMock.Verify(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
