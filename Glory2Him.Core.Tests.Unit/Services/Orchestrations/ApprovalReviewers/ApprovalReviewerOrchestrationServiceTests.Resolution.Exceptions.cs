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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Approvals.Exceptions;
using Glory2Him.Core.Models.Orchestrations.ApprovalReviewers.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.ApprovalReviewers
{
    /// <summary>
    /// The <c>Approval*</c> arm — the fourth, and the one this service pays for keeping the
    /// read-triggered repair (§12.5.4 business rule 1, criterion 4's "only genuinely new arm").
    ///
    /// <para><b>Why it needed its own file rather than arriving with a moved suite.</b> The other
    /// three arms came across with the tests that already exercised them, because the operations
    /// that raise them moved too. <c>Approval*</c> had no such suite to travel with: nothing on
    /// the old service reached the <c>Approval</c> foundation from a reviewer operation at all.
    /// So the arm existed, structurally correct, and no case proved it caught anything.</para>
    ///
    /// <para><b>Both call sites, because the repair has two.</b> The pre-insert re-probe
    /// (<c>FindApprovalByEntityAsync</c>) and the insert itself (<c>AddApprovalAsync</c>) are the
    /// only places this service touches that foundation, and they fail for different reasons — a
    /// read that cannot run, versus a write the database refuses.</para>
    ///
    /// <para><b>The validation-shaped case is not hypothetical.</b> Two concurrent repairs racing
    /// to open the same round collide on <c>UX_Approvals_EntityType_EntityId</c>; that surfaces
    /// here as an <c>ApprovalDependencyValidationException</c>, and the exposer answers it 400
    /// rather than the 424 it would get from the catch-all. Retrying finds the round the winner
    /// opened. Without this arm the honest "somebody beat you to it, try again" is reported as
    /// the server being broken.</para>
    ///
    /// <para><b>Only HALF of these cases can fail if the arm is deleted, and that is worth knowing
    /// before somebody trusts the other half.</b> Measured: removing the <c>Approval*</c> arm reds
    /// the four validation-shaped cases and leaves the four failure-shaped ones green, because
    /// <c>catch (Xeption downstreamException)</c> routes to the same wrapper with the same inner.
    /// So the failure-shaped cases pin the OUTCOME a caller sees, not the arm that produces it.
    /// They are kept for the reason the service's own chain gives for naming those two blocks
    /// explicitly rather than letting them reach the catch-all by coincidence: today's wrapping
    /// happening to match is not the same as the family being handled, and the day a block is
    /// added above them the coincidence stops holding. A reader looking for the case that proves
    /// the arm should look at the validation-shaped pair.</para>
    /// </summary>
    public partial class ApprovalReviewerOrchestrationServiceTests
    {
        // The two VALIDATION-shaped members of the Approval family — a fault in what this
        // orchestration asked for, so they become a DEPENDENCY VALIDATION exception. Each carries
        // an inner Xeption because the wrapper unwraps one level: it re-raises
        // `exception.InnerException as Xeption`, so a family member with no inner would prove the
        // fallback rather than the mapping.
        public static TheoryData<Xeption> RepairApprovalValidationExceptions()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            return new TheoryData<Xeption>
            {
                new ApprovalValidationException(
                    message: randomMessage, innerException: innerException),

                new ApprovalDependencyValidationException(
                    message: randomMessage, innerException: innerException),
            };
        }

        // The two FAILURE-shaped members — something external went wrong rather than something
        // the caller or this service got wrong.
        public static TheoryData<Xeption> RepairApprovalFailureExceptions()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            return new TheoryData<Xeption>
            {
                new ApprovalDependencyException(
                    message: randomMessage, innerException: innerException),

                new ApprovalServiceException(
                    message: randomMessage, innerException: innerException),
            };
        }

        // The round is missing and the entity is in play, so the repair runs and reaches the
        // Approval foundation. Every case below needs exactly this, and spelling it out at each
        // call site would bury the one line that differs.
        private void SetupRepairReachesTheApprovalFoundation(
            EntityType entityType,
            Guid entityId)
        {
            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveApprovalReviewerScopeByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ApprovalReviewerScope)null);

            SetupEntityApprovalStatus(entityType, entityId, ApprovalStatus.Submitted);
        }

        [Theory]
        [MemberData(nameof(RepairApprovalValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnResolveIfTheRepairsProbeDoesAndLogItAsync(
            Xeption approvalFoundationException)
        {
            // given: the repair's pre-insert re-probe refuses
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            EntityType entityType = EntityType.ContentItem;
            Guid entityId = Guid.NewGuid();
            SetupRepairReachesTheApprovalFoundation(entityType, entityId);

            var expectedDependencyValidationException =
                new ApprovalReviewerOrchestrationDependencyValidationException(
                    message: ExpectedDependencyValidationMessage,
                    innerException: (approvalFoundationException.InnerException as Xeption)!);

            this.approvalServiceMock.Setup(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalFoundationException);

            // when
            ValueTask<IReadOnlyList<ApprovalReviewRequest>> retrieveTask =
                this.approvalReviewerOrchestrationService.RetrieveApprovalReviewRequestsAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewerOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewerOrchestrationDependencyValidationException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(RepairApprovalFailureExceptions))]
        public async Task ShouldThrowDependencyExceptionOnResolveIfTheRepairsProbeDoesAndLogItAsync(
            Xeption approvalFoundationException)
        {
            // given: the same probe, failing for an external reason instead
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            EntityType entityType = EntityType.ContentItem;
            Guid entityId = Guid.NewGuid();
            SetupRepairReachesTheApprovalFoundation(entityType, entityId);

            var expectedDependencyException =
                new ApprovalReviewerOrchestrationDependencyException(
                    message: ExpectedDependencyMessage,
                    innerException: (approvalFoundationException.InnerException as Xeption)!);

            this.approvalServiceMock.Setup(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalFoundationException);

            // when
            ValueTask<IReadOnlyList<ApprovalReviewRequest>> retrieveTask =
                this.approvalReviewerOrchestrationService.RetrieveApprovalReviewRequestsAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewerOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalReviewerOrchestrationDependencyException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);
        }

        /// <summary>
        /// The insert, and the case §12.5.4 documents as reachable: two concurrent repairs racing
        /// to open the same round collide on <c>UX_Approvals_EntityType_EntityId</c>, the loser
        /// gets a dependency-validation failure, and the exposer answers 400. Unmapped it would be
        /// a 424 — the server reporting itself broken about a request it understood perfectly.
        /// </summary>
        [Theory]
        [MemberData(nameof(RepairApprovalValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnResolveIfTheRepairsAddDoesAndLogItAsync(
            Xeption approvalFoundationException)
        {
            // given: the probe finds nothing, so the repair writes — and the write is refused
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            EntityType entityType = EntityType.ContentItem;
            Guid entityId = Guid.NewGuid();
            SetupRepairReachesTheApprovalFoundation(entityType, entityId);

            var expectedDependencyValidationException =
                new ApprovalReviewerOrchestrationDependencyValidationException(
                    message: ExpectedDependencyValidationMessage,
                    innerException: (approvalFoundationException.InnerException as Xeption)!);

            this.approvalServiceMock.Setup(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalFoundationException);

            // when
            ValueTask<IReadOnlyList<ApprovalReviewRequest>> retrieveTask =
                this.approvalReviewerOrchestrationService.RetrieveApprovalReviewRequestsAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewerOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<ApprovalReviewerOrchestrationDependencyValidationException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(RepairApprovalFailureExceptions))]
        public async Task ShouldThrowDependencyExceptionOnResolveIfTheRepairsAddDoesAndLogItAsync(
            Xeption approvalFoundationException)
        {
            // given: the same write, failing for an external reason instead
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            EntityType entityType = EntityType.ContentItem;
            Guid entityId = Guid.NewGuid();
            SetupRepairReachesTheApprovalFoundation(entityType, entityId);

            var expectedDependencyException =
                new ApprovalReviewerOrchestrationDependencyException(
                    message: ExpectedDependencyMessage,
                    innerException: (approvalFoundationException.InnerException as Xeption)!);

            this.approvalServiceMock.Setup(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(approvalFoundationException);

            // when
            ValueTask<IReadOnlyList<ApprovalReviewRequest>> retrieveTask =
                this.approvalReviewerOrchestrationService.RetrieveApprovalReviewRequestsAsync(
                    entityType,
                    entityId,
                    TestContext.Current.CancellationToken);

            ApprovalReviewerOrchestrationDependencyException actualException =
                await Assert.ThrowsAsync<ApprovalReviewerOrchestrationDependencyException>(
                    retrieveTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedDependencyException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);
        }
    }
}
