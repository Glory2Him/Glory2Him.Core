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
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments.Exceptions;
using Glory2Him.Core.Models.Orchestrations.AIReviewers;
using Glory2Him.Core.Models.Orchestrations.AIReviewers.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.AIReviewers
{
    /// <summary>
    /// How the AIReviewerAssignment foundation's failures reach a caller, and the one failure this
    /// flow answers rather than reports — the uniqueness collision two simultaneous requests race
    /// into.
    /// </summary>
    public partial class AIReviewerOrchestrationServiceTests
    {
        /// <summary>
        /// The whole family, in one set, each paired with the category it must land in — and the
        /// SPLIT is the point. This service's catch chain names the AIReviewerAssignment
        /// foundation explicitly (alongside Approval), so the two validation-shaped families are
        /// re-surfaced as a DEPENDENCY VALIDATION fault and the two infrastructure-shaped ones as
        /// a DEPENDENCY fault.
        ///
        /// <para>That distinction is the whole reason the arm exists rather than letting the
        /// <c>Xeption</c> catch-all take them. A validation-shaped refusal is the CALLER'S to fix
        /// — a blocked caller, an assignment born completed, a collision that outlived the
        /// re-read — and reaches the exposer as a <c>400</c> (or a <c>409</c> for the collision).
        /// Through the catch-all all four became a <c>424</c>, which tells the caller the server
        /// is broken about a request the server understood perfectly and declined.</para>
        ///
        /// <para><c>isCallerFixable</c> rather than two near-identical sets: every one of these
        /// tests asserts the SAME thing about a different call site, and splitting the data
        /// rather than the tests keeps the call sites the axis the file is organised on.</para>
        /// </summary>
        public static TheoryData<Xeption, bool> AIReviewerAssignmentFoundationExceptions()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            return new TheoryData<Xeption, bool>
            {
                { new AIReviewerAssignmentValidationException(
                    message: randomMessage, innerException: innerException), true },

                { new AIReviewerAssignmentDependencyValidationException(
                    message: randomMessage, innerException: innerException), true },

                { new AIReviewerAssignmentDependencyException(
                    message: randomMessage, innerException: innerException), false },

                { new AIReviewerAssignmentServiceException(
                    message: randomMessage, innerException: innerException), false },
            };
        }

        /// <summary>
        /// The wrapper a foundation fault of this family must arrive in, by category — built once
        /// here so the call-site tests below assert the mapping identically rather than each
        /// restating it.
        /// </summary>
        private static Xeption ExpectedAIReviewerWrapperFor(
            Xeption foundationException,
            bool isCallerFixable) =>
            isCallerFixable
                ? new AIReviewerOrchestrationDependencyValidationException(
                    message: ExpectedDependencyValidationMessage,
                    innerException: (foundationException.InnerException as Xeption)!)
                : new AIReviewerOrchestrationDependencyException(
                    message: ExpectedDependencyMessage,
                    innerException: (foundationException.InnerException as Xeption)!);

        [Theory]
        [MemberData(nameof(AIReviewerAssignmentFoundationExceptions))]
        public async Task ShouldThrowDependencyExceptionOnRequestAIReviewerIfThePresenceReadDoesAndLogItAsync(
            Xeption foundationException,
            bool isCallerFixable)
        {
            // given: the presence read decides which of the three upsert branches runs, so a
            // failure here means the flow does not know what the click meant. Nothing may be
            // written on a guess.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            Guid approvalId = Guid.NewGuid();

            Xeption expectedException =
                ExpectedAIReviewerWrapperFor(foundationException, isCallerFixable);

            SetupResolvedRound(approvalId: approvalId);
            SetupAIReviewerOffer(isOffered: true);

            this.aiReviewerAssignmentServiceMock.Setup(service =>
                service.RetrieveAIReviewerAssignmentByApprovalIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<AIReviewerAssignment> requestTask =
                this.aiReviewerOrchestrationService.RequestAIReviewerAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            Xeption actualException =
                await Assert.ThrowsAnyAsync<Xeption>(
                    requestTask.AsTask);

            // then
            actualException.Should().BeOfType(expectedException.GetType());
            actualException.Should().BeEquivalentTo(expectedException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

            // Every family on this service funnels through LogError — none escalates to
            // LogCritical — so the day one is "upgraded" a test says so rather than a log filter.
            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            VerifyNoAIReviewerAssignmentWrite();

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(AIReviewerAssignmentFoundationExceptions))]
        public async Task ShouldThrowDependencyExceptionOnRequestAIReviewerIfTheAddDoesAndLogItAsync(
            Xeption foundationException,
            bool isCallerFixable)
        {
            // given: the create branch, reached because nothing stands. This is the one write the
            // flow makes on a round Berean has never been asked about.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            Guid approvalId = Guid.NewGuid();

            Xeption expectedException =
                ExpectedAIReviewerWrapperFor(foundationException, isCallerFixable);

            SetupResolvedRound(approvalId: approvalId);
            SetupAIReviewerOffer(isOffered: true);
            SetupStoredAIReviewerAssignment(approvalId, storageAssignment: null);

            this.aiReviewerAssignmentServiceMock.Setup(service =>
                service.AddAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<AIReviewerAssignment> requestTask =
                this.aiReviewerOrchestrationService.RequestAIReviewerAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            Xeption actualException =
                await Assert.ThrowsAnyAsync<Xeption>(
                    requestTask.AsTask);

            // then
            actualException.Should().BeOfType(expectedException.GetType());
            actualException.Should().BeEquivalentTo(expectedException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            // The collision handler must not have swallowed this: an ordinary failure is not a
            // race, so nothing may be re-read and nothing else may be written.
            this.aiReviewerAssignmentServiceMock.Verify(service =>
                service.RetrieveAIReviewerAssignmentByApprovalIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.aiReviewerAssignmentServiceMock.Verify(service =>
                service.ModifyAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(AIReviewerAssignmentFoundationExceptions))]
        public async Task ShouldThrowDependencyExceptionOnRequestAIReviewerIfTheResetDoesAndLogItAsync(
            Xeption foundationException,
            bool isCallerFixable)
        {
            // given: the re-request branch, which is a MODIFY of the standing row rather than a
            // second add. Failed separately from the add above because the two are different
            // writes reached from different states of the same round.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            Guid approvalId = Guid.NewGuid();

            Xeption expectedException =
                ExpectedAIReviewerWrapperFor(foundationException, isCallerFixable);

            SetupResolvedRound(approvalId: approvalId);
            SetupAIReviewerOffer(isOffered: true);

            SetupStoredAIReviewerAssignment(
                approvalId,
                CreateAIReviewerAssignment(approvalId, isAIReviewCompleted: true));

            this.aiReviewerAssignmentServiceMock.Setup(service =>
                service.ModifyAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<AIReviewerAssignment> requestTask =
                this.aiReviewerOrchestrationService.RequestAIReviewerAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            Xeption actualException =
                await Assert.ThrowsAnyAsync<Xeption>(
                    requestTask.AsTask);

            // then
            actualException.Should().BeOfType(expectedException.GetType());
            actualException.Should().BeEquivalentTo(expectedException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            this.aiReviewerAssignmentServiceMock.Verify(service =>
                service.AddAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(AIReviewerAssignmentFoundationExceptions))]
        public async Task ShouldThrowDependencyExceptionOnWithdrawAIReviewerIfTheRemoveDoesAndLogItAsync(
            Xeption foundationException,
            bool isCallerFixable)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid approvalId = Guid.NewGuid();

            AIReviewerAssignment standingAssignment = CreateAIReviewerAssignment(approvalId);

            Xeption expectedException =
                ExpectedAIReviewerWrapperFor(foundationException, isCallerFixable);

            SetupResolvedRound(approvalId: approvalId);
            SetupStoredAIReviewerAssignment(approvalId, standingAssignment);

            this.aiReviewerAssignmentServiceMock.Setup(service =>
                service.RemoveAIReviewerAssignmentByIdAsync(
                    standingAssignment.Id,
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<AIReviewerAssignment> withdrawTask =
                this.aiReviewerOrchestrationService.WithdrawAIReviewerAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            Xeption actualException =
                await Assert.ThrowsAnyAsync<Xeption>(
                    withdrawTask.AsTask);

            // then
            actualException.Should().BeOfType(expectedException.GetType());
            actualException.Should().BeEquivalentTo(expectedException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.IsAny<Exception>()),
                Times.Never);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(AIReviewerAssignmentFoundationExceptions))]
        public async Task ShouldThrowDependencyExceptionOnRetrieveAIReviewerStatusIfTheReadDoesAndLogItAsync(
            Xeption foundationException,
            bool isCallerFixable)
        {
            // given: a status a panel polls every few seconds, so a half-composed answer would be
            // rendered as fact. Nothing partial goes back — the read fails outright.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            Guid approvalId = Guid.NewGuid();

            Xeption expectedException =
                ExpectedAIReviewerWrapperFor(foundationException, isCallerFixable);

            SetupResolvedRound(approvalId: approvalId);
            SetupAIReviewerOffer(isOffered: true);

            this.aiReviewerAssignmentServiceMock.Setup(service =>
                service.RetrieveAIReviewerAssignmentByApprovalIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<AIReviewerStatus> statusTask =
                this.aiReviewerOrchestrationService.RetrieveAIReviewerStatusAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            Xeption actualException =
                await Assert.ThrowsAnyAsync<Xeption>(
                    statusTask.AsTask);

            // then
            actualException.Should().BeOfType(expectedException.GetType());
            actualException.Should().BeEquivalentTo(expectedException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

            VerifyNoAIReviewerAssignmentWrite();

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// The APPROVAL foundation's families reach the caller through the same chain, and they
        /// have to: this service reads that foundation on every operation and WRITES to it on one
        /// path — the read-triggered repair opens a missing round. Without these arms the
        /// collision two concurrent repairs produce would fall to the catch-all and be reported as
        /// a 424 rather than as the caller's to retry.
        /// </summary>
        [Theory]
        [MemberData(nameof(ApprovalFoundationExceptions))]
        public async Task ShouldThrowDependencyExceptionOnRetrieveAIReviewerStatusIfTheRoundProbeDoesAndLogItAsync(
            Xeption foundationException,
            bool isCallerFixable)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);

            Xeption expectedException =
                ExpectedAIReviewerWrapperFor(foundationException, isCallerFixable);

            this.approvalServiceMock.Setup(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(foundationException);

            // when
            ValueTask<AIReviewerStatus> statusTask =
                this.aiReviewerOrchestrationService.RetrieveAIReviewerStatusAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            Xeption actualException =
                await Assert.ThrowsAnyAsync<Xeption>(
                    statusTask.AsTask);

            // then
            actualException.Should().BeOfType(expectedException.GetType());
            actualException.Should().BeEquivalentTo(expectedException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedException))),
                Times.Once);

            VerifyNoAIReviewerAssignmentWrite();

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// The Approval foundation's four families, split by category exactly as the AI reviewer's
        /// are — same shape, same reason.
        /// </summary>
        public static TheoryData<Xeption, bool> ApprovalFoundationExceptions()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            return new TheoryData<Xeption, bool>
            {
                { new Glory2Him.Core.Models.Foundations.Approvals.Exceptions
                    .ApprovalValidationException(
                        message: randomMessage, innerException: innerException), true },

                { new Glory2Him.Core.Models.Foundations.Approvals.Exceptions
                    .ApprovalDependencyValidationException(
                        message: randomMessage, innerException: innerException), true },

                { new Glory2Him.Core.Models.Foundations.Approvals.Exceptions
                    .ApprovalDependencyException(
                        message: randomMessage, innerException: innerException), false },

                { new Glory2Him.Core.Models.Foundations.Approvals.Exceptions
                    .ApprovalServiceException(
                        message: randomMessage, innerException: innerException), false },
            };
        }

        /// <summary>
        /// Anything unanticipated is this service's own fault until proven otherwise, so it is
        /// categorised as a SERVICE error rather than filed against the collaborator it happened
        /// next to.
        /// </summary>
        [Fact]
        public async Task ShouldThrowServiceExceptionOnRequestAIReviewerIfServiceErrorOccursAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            Guid approvalId = Guid.NewGuid();
            var serviceException = new Exception("Service error occurred.");

            var failedAIReviewerOrchestrationServiceException =
                new FailedAIReviewerOrchestrationServiceException(
                    message: "Failed AI reviewer orchestration service error occurred, " +
                        "please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            var expectedServiceException =
                new AIReviewerOrchestrationServiceException(
                    message: "AI reviewer orchestration service error occurred, contact support.",
                    innerException: failedAIReviewerOrchestrationServiceException);

            SetupResolvedRound(approvalId: approvalId);
            SetupAIReviewerOffer(isOffered: true);
            SetupStoredAIReviewerAssignment(approvalId, storageAssignment: null);

            this.aiReviewerAssignmentServiceMock.Setup(service =>
                service.AddAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<AIReviewerAssignment> requestTask =
                this.aiReviewerOrchestrationService.RequestAIReviewerAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            AIReviewerOrchestrationServiceException actualException =
                await Assert.ThrowsAsync<AIReviewerOrchestrationServiceException>(
                    requestTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(SameExceptionAs(expectedServiceException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// THE RACE the presence check cannot see. Two callers — a double click, two moderators —
        /// both find the round unassigned, both write, and
        /// <c>UX_AIReviewerAssignments_ApprovalId</c> refuses the loser.
        ///
        /// <para>"Somebody else asked half a second before you" is the same outcome as "you asked
        /// twice", which the still-pending branch answers with the standing row — so the collision
        /// dissolves the same way rather than telling the caller their assignment failed when it
        /// stands. The winner's row is RE-READ rather than assumed: this caller has never seen
        /// it.</para>
        /// </summary>
        [Fact]
        public async Task ShouldReturnTheWinningAssignmentWhenTwoAIReviewerRequestsRaceAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            Guid approvalId = Guid.NewGuid();

            AIReviewerAssignment winningAssignment = CreateAIReviewerAssignment(approvalId);

            var collisionException = new AIReviewerAssignmentDependencyValidationException(
                message: "AI reviewer assignment dependency validation error occurred, " +
                    "fix the errors and try again.",
                innerException: new AlreadyExistsAIReviewerAssignmentException(
                    message: "AI reviewer assignment already exists, " +
                        "a uniqueness rule rejected the write.",
                    innerException: new Exception(),
                    data: new Hashtable()));

            SetupResolvedRound(approvalId: approvalId);
            SetupAIReviewerOffer(isOffered: true);

            // the FIRST read sees nothing, which is what lets both callers try; the re-read after
            // the collision sees the winner's row
            this.aiReviewerAssignmentServiceMock.SetupSequence(service =>
                service.RetrieveAIReviewerAssignmentByApprovalIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((AIReviewerAssignment)null)
                        .ReturnsAsync(winningAssignment);

            this.aiReviewerAssignmentServiceMock.Setup(service =>
                service.AddAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(collisionException);

            // when
            AIReviewerAssignment actualAssignment =
                await this.aiReviewerOrchestrationService.RequestAIReviewerAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // then: the winner's row, not a failure
            actualAssignment.Id.Should().Be(winningAssignment.Id);

            this.aiReviewerAssignmentServiceMock.Verify(service =>
                service.RetrieveAIReviewerAssignmentByApprovalIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            // and the loser did NOT then reset the winner's pending row — the collision is
            // answered by reading, never by writing again
            this.aiReviewerAssignmentServiceMock.Verify(service =>
                service.ModifyAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            // and THIS layer logged no error of its own: the collision was ANSWERED here, not
            // merely translated on its way out.
            //
            // Not a claim that the running system logs nothing. The foundation has already
            // written this collision to LogErrorAsync before throwing it, and that entry is a
            // real one — the assertion holds only because the foundation is a mock. What it pins
            // is that the orchestration adds no SECOND error line for an outcome it resolved.
            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.IsAny<Xeption>()),
                Times.Never);
        }

        /// <summary>
        /// The narrow window on the other side of the same race: the winner's assignment is
        /// withdrawn between the collision and the re-read. There is no row to hand back, so the
        /// collision is the honest answer and goes to the caller unchanged.
        ///
        /// <para>Without this the handler could be written to manufacture a row, or to return
        /// <c>null</c> — and a <c>null</c> here would reach the exposer as a <c>200</c> carrying
        /// nothing, telling the caller their request succeeded when no assignment exists.</para>
        /// </summary>
        [Fact]
        public async Task ShouldSurfaceTheCollisionWhenTheWinningAssignmentHasSinceGoneAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Publishers);
            Guid approvalId = Guid.NewGuid();

            var alreadyExistsException = new AlreadyExistsAIReviewerAssignmentException(
                message: "AI reviewer assignment already exists, " +
                    "a uniqueness rule rejected the write.",
                innerException: new Exception(),
                data: new Hashtable());

            var collisionException = new AIReviewerAssignmentDependencyValidationException(
                message: "AI reviewer assignment dependency validation error occurred, " +
                    "fix the errors and try again.",
                innerException: alreadyExistsException);

            SetupResolvedRound(approvalId: approvalId);
            SetupAIReviewerOffer(isOffered: true);
            SetupStoredAIReviewerAssignment(approvalId, storageAssignment: null);

            this.aiReviewerAssignmentServiceMock.Setup(service =>
                service.AddAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(collisionException);

            // when
            ValueTask<AIReviewerAssignment> requestTask =
                this.aiReviewerOrchestrationService.RequestAIReviewerAsync(
                    EntityType.ContentItem,
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            // DEPENDENCY VALIDATION, not dependency: a stale caller is not a broken server. This
            // is the arm that carries the collision to the exposer's 409 rather than a 424.
            AIReviewerOrchestrationDependencyValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerOrchestrationDependencyValidationException>(
                    requestTask.AsTask);

            // then: the collision's own inner survives the wrapping, so the reason is still
            // readable at the exposer rather than flattened into "contact support"
            actualException.InnerException.Should()
                .BeOfType<AlreadyExistsAIReviewerAssignmentException>();

            this.aiReviewerAssignmentServiceMock.Verify(service =>
                service.ModifyAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // Neither write may happen on a path that failed before deciding what the click meant.
        // Both are named because the upsert reaches for one or the other, never for both, and a
        // check that only named the add would miss a reset made on a row nobody had read.
        private void VerifyNoAIReviewerAssignmentWrite()
        {
            this.aiReviewerAssignmentServiceMock.Verify(service =>
                service.AddAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.aiReviewerAssignmentServiceMock.Verify(service =>
                service.ModifyAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.aiReviewerAssignmentServiceMock.Verify(service =>
                service.RemoveAIReviewerAssignmentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
