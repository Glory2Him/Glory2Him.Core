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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments.Exceptions;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.AIReviewerAssignments
{
    public partial class AIReviewerAssignmentServiceTests
    {
        /// <summary>
        /// §8.8 rule 1 and §8.6 HR-4 — the content moved under a pass nobody re-requested, so the
        /// two flags recording what that pass reported go back. It runs under the SYSTEM identity,
        /// which is why it cannot go through the public modify verb:
        /// <c>CreateSystemAsync</c> mints a context carrying no roles, and the manage gate asks
        /// for a review-tier one.
        ///
        /// <para><b>What it catches.</b> The caller here holds NO review role — the ordinary
        /// editor is the author revising their own submission (HR-1 forbids reviewing your own
        /// content) — so routing this verb back onto
        /// <c>ValidateUserIsAllowedToManageAIReviewerAssignments</c> reds it, and so does dropping
        /// either flag assignment: the audit stamp is matched on the pair already driven to
        /// false, which is what proves they move TOGETHER rather than one at a time.</para>
        /// </summary>
        [Fact]
        public async Task ShouldReturnAStaleAIReviewerAssignmentToPendingUnderTheSystemIdentityAsync()
        {
            // given: the caller holds NO review role — the system identity is the authority here
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            AIReviewerAssignment randomAIReviewerAssignment = CreateRandomAIReviewerAssignment();
            randomAIReviewerAssignment.IsAIReviewCompleted = true;
            randomAIReviewerAssignment.IsAIReviewCommentsPresent = true;
            Guid inputAIReviewerAssignmentId = randomAIReviewerAssignment.Id;
            AIReviewerAssignment storageAIReviewerAssignment = randomAIReviewerAssignment;

            AIReviewerAssignment auditAppliedAIReviewerAssignment =
                storageAIReviewerAssignment.DeepClone();

            auditAppliedAIReviewerAssignment.IsAIReviewCompleted = false;
            auditAppliedAIReviewerAssignment.IsAIReviewCommentsPresent = false;

            AIReviewerAssignment pendingAIReviewerAssignment =
                auditAppliedAIReviewerAssignment.DeepClone();

            AIReviewerAssignment expectedAIReviewerAssignment =
                pendingAIReviewerAssignment.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    inputAIReviewerAssignmentId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageAIReviewerAssignment);

            // Matched on the PAIR already at false and on a system context, so the setup itself
            // states what the transition owes: both flags cleared before the stamp, and the stamp
            // taken from an identity nobody could have supplied.
            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.Is<AIReviewerAssignment>(aiReviewerAssignment =>
                        aiReviewerAssignment.Id == inputAIReviewerAssignmentId
                            && aiReviewerAssignment.IsAIReviewCompleted == false
                            && aiReviewerAssignment.IsAIReviewCommentsPresent == false),
                    It.Is<SecurityContext>(securityContext =>
                        securityContext.IsSystemIdentity)))
                            .ReturnsAsync(auditAppliedAIReviewerAssignment);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAIReviewerAssignmentAsync(
                    auditAppliedAIReviewerAssignment, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(pendingAIReviewerAssignment);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAIReviewerAssignmentAsync(
                    It.IsAny<EventEnvelope<AIReviewerAssignment>>(),
                    AIReviewerAssignmentEventOperation.Modified))
                        .Returns(new ValueTask<EventPublishResult<AIReviewerAssignment>>(
                            new EventPublishResult<AIReviewerAssignment>()));

            // when
            AIReviewerAssignment actualAIReviewerAssignment =
                await this.aiReviewerAssignmentWorkflowService
                    .ReturnStaleAIReviewerAssignmentToPendingAsync(
                        inputAIReviewerAssignmentId,
                        TestContext.Current.CancellationToken);

            // then
            actualAIReviewerAssignment.Should().BeEquivalentTo(expectedAIReviewerAssignment);
            actualAIReviewerAssignment.IsAIReviewCompleted.Should().BeFalse();
            actualAIReviewerAssignment.IsAIReviewCommentsPresent.Should().BeFalse();

            // The audit values were stamped from a SYSTEM context, which is what makes UpdatedBy
            // mean "nobody asked for this; the content moved underneath it".
            //
            // PINNED ON THE SUBJECT, not only on the flag. IsSystemIdentity alone is satisfied by
            // CreateElevatedAsync too, and that verb KEEPS the caller as the subject — so a
            // transition switched to it would still pass a flag-only assertion while stamping
            // UpdatedBy with the author whose edit triggered the reset, which is the one thing
            // this seam exists to prevent. The caller survives on DelegatedBySubjectId, so the
            // trail back to the person is kept without making them the actor.
            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.Is<AIReviewerAssignment>(aiReviewerAssignment =>
                        aiReviewerAssignment.Id == inputAIReviewerAssignmentId
                            && aiReviewerAssignment.IsAIReviewCompleted == false
                            && aiReviewerAssignment.IsAIReviewCommentsPresent == false),
                    It.Is<SecurityContext>(securityContext =>
                        securityContext.IsSystemIdentity
                            && securityContext.SubjectId == SystemIdentity.UserId
                            && securityContext.Username == SystemIdentity.Username
                            && securityContext.DelegatedBySubjectId
                                == this.ambientSecurityContext.SubjectId)),
                Times.Once);

            // THE ROW STAYS. Berean is still on the round — only the flags went back, and a
            // remove here would leave the re-opened round without the reviewer it had.
            this.storageBrokerMock.Verify(broker =>
                broker.DeleteAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAIReviewerAssignmentAsync(
                    auditAppliedAIReviewerAssignment, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishAIReviewerAssignmentAsync(
                    It.IsAny<EventEnvelope<AIReviewerAssignment>>(),
                    AIReviewerAssignmentEventOperation.Modified),
                Times.Once);

            // No ProcessedEvents bookkeeping on this path: the verb has no event address and no
            // handler, so both rows would be written with no reader.
            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.IsAny<ProcessedEvent>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// ALREADY PENDING — the overwhelmingly common case, and the one the null answer from
        /// <c>IAccessBroker.FindResettableAIReviewerAssignmentIdAsync</c> ordinarily spares this
        /// verb entirely. It still has to hold here, because two legitimate paths reach one row
        /// inside a single act: an administrator resetting a round whose entity was also just
        /// edited.
        ///
        /// <para><b>What it catches.</b> Deleting the both-flags-false guard: the row would be
        /// written back unchanged and an <c>AIReviewerAssignment-Modified</c> fact published for a
        /// row that did not move, telling subscribers something happened when nothing did. The
        /// publish assertion is the half that would notice.</para>
        /// </summary>
        [Fact]
        public async Task ShouldNotRewriteAnAlreadyPendingAIReviewerAssignmentAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            AIReviewerAssignment randomAIReviewerAssignment = CreateRandomAIReviewerAssignment();
            randomAIReviewerAssignment.IsAIReviewCompleted = false;
            randomAIReviewerAssignment.IsAIReviewCommentsPresent = false;
            Guid inputAIReviewerAssignmentId = randomAIReviewerAssignment.Id;
            AIReviewerAssignment storageAIReviewerAssignment = randomAIReviewerAssignment;

            AIReviewerAssignment expectedAIReviewerAssignment =
                storageAIReviewerAssignment.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    inputAIReviewerAssignmentId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageAIReviewerAssignment);

            // when
            AIReviewerAssignment actualAIReviewerAssignment =
                await this.aiReviewerAssignmentWorkflowService
                    .ReturnStaleAIReviewerAssignmentToPendingAsync(
                        inputAIReviewerAssignmentId,
                        TestContext.Current.CancellationToken);

            // then: returned unchanged rather than refused — pending is the TARGET state, so a
            // second arrival at it is an ordering coincidence and not an error
            actualAIReviewerAssignment.Should().BeEquivalentTo(expectedAIReviewerAssignment);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishAIReviewerAssignmentAsync(
                    It.IsAny<EventEnvelope<AIReviewerAssignment>>(),
                    It.IsAny<AIReviewerAssignmentEventOperation>()),
                Times.Never);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// WITHDRAWN — a moderator took Berean off the round between the gather that named this id
        /// and this write. Returned unchanged rather than refused, which is deliberately NOT the
        /// public modify's posture: that one throws not-found because a PERSON asked for something
        /// and must learn it did not happen, while this caller is the workflow and is being told
        /// there is nothing left to put back.
        ///
        /// <para><b>What it catches.</b> Deleting the <c>IsDeleted</c> guard: the transition would
        /// write both flags onto a withdrawn row and publish a Modified fact for it, resurrecting
        /// the reporting half of an assignment nobody re-made.</para>
        /// </summary>
        [Fact]
        public async Task ShouldNotReturnAWithdrawnAIReviewerAssignmentToPendingAsync()
        {
            // given: withdrawn, and still carrying the flags a live row would be reset for — so
            // the absence of a write is about the deletion state and nothing else
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            AIReviewerAssignment randomAIReviewerAssignment = CreateRandomAIReviewerAssignment();
            randomAIReviewerAssignment.IsDeleted = true;
            randomAIReviewerAssignment.IsAIReviewCompleted = true;
            randomAIReviewerAssignment.IsAIReviewCommentsPresent = true;
            Guid inputAIReviewerAssignmentId = randomAIReviewerAssignment.Id;
            AIReviewerAssignment storageAIReviewerAssignment = randomAIReviewerAssignment;

            AIReviewerAssignment expectedAIReviewerAssignment =
                storageAIReviewerAssignment.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    inputAIReviewerAssignmentId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageAIReviewerAssignment);

            // when
            AIReviewerAssignment actualAIReviewerAssignment =
                await this.aiReviewerAssignmentWorkflowService
                    .ReturnStaleAIReviewerAssignmentToPendingAsync(
                        inputAIReviewerAssignmentId,
                        TestContext.Current.CancellationToken);

            // then
            actualAIReviewerAssignment.Should().BeEquivalentTo(expectedAIReviewerAssignment);
            actualAIReviewerAssignment.IsAIReviewCompleted.Should().BeTrue();

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishAIReviewerAssignmentAsync(
                    It.IsAny<EventEnvelope<AIReviewerAssignment>>(),
                    It.IsAny<AIReviewerAssignmentEventOperation>()),
                Times.Never);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// The shape check runs before storage is touched, so a malformed id never reaches a read.
        /// </summary>
        [Fact]
        public async Task ShouldThrowValidationExceptionOnReturnToPendingIfIdIsInvalidAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Guid invalidAIReviewerAssignmentId = Guid.Empty;

            var invalidAIReviewerAssignmentException =
                new InvalidAIReviewerAssignmentException(
                    message: "AI reviewer assignment is invalid, fix the errors and try again.");

            invalidAIReviewerAssignmentException.AddData(
                key: nameof(AIReviewerAssignment.Id),
                values: "Id is required");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, fix the errors and try again.",
                    innerException: invalidAIReviewerAssignmentException);

            // when
            ValueTask<AIReviewerAssignment> returnToPendingTask =
                this.aiReviewerAssignmentWorkflowService
                    .ReturnStaleAIReviewerAssignmentToPendingAsync(
                        invalidAIReviewerAssignmentId,
                        TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    returnToPendingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// The id named by a gather can be gone by the time the write runs — hard-deleted, or
        /// never there at all. Distinct from the withdrawn case above: a soft-deleted row is
        /// ANSWERED, a missing one is refused, because there is no row to hand back.
        /// </summary>
        [Fact]
        public async Task ShouldThrowNotFoundExceptionOnReturnToPendingIfAssignmentDoesNotExistAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Guid someAIReviewerAssignmentId = Guid.NewGuid();
            AIReviewerAssignment noAIReviewerAssignment = null;

            var notFoundAIReviewerAssignmentException =
                new NotFoundAIReviewerAssignmentException(
                    message: "AI reviewer assignment not found with id: " +
                        $"{someAIReviewerAssignmentId}.");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, fix the errors and try again.",
                    innerException: notFoundAIReviewerAssignmentException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    someAIReviewerAssignmentId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(noAIReviewerAssignment);

            // when
            ValueTask<AIReviewerAssignment> returnToPendingTask =
                this.aiReviewerAssignmentWorkflowService
                    .ReturnStaleAIReviewerAssignmentToPendingAsync(
                        someAIReviewerAssignmentId,
                        TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    returnToPendingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);
        }

        /// <summary>
        /// The system-identity guard is unreachable through the public seam — that seam mints the
        /// context itself two methods up — so this exercises it through the ambient
        /// <c>CreateSystemAsync</c> stub returning a non-system context. What it protects against
        /// is a future second caller of the private do-work supplying its own envelope, which is
        /// the route by which a person's context could otherwise reach a write that must record
        /// the system.
        ///
        /// <para><b>What it catches.</b> Deleting
        /// <c>ValidateReturnToPendingIsTheWorkflowsOwnAct</c>: an administrator — who passes the
        /// contribution half of the gate on their own — would then be able to drive an
        /// assignment's flags back with <c>UpdatedBy</c> naming them, which is the re-request's
        /// meaning stamped onto an act nobody performed.</para>
        /// </summary>
        [Fact]
        public async Task ShouldThrowValidationExceptionOnReturnToPendingIfTheContextIsNotTheSystemAsync()
        {
            // given: a caller-shaped context reaches the do-work instead of a system-minted one
            this.systemContextIsGenuine = false;
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            Guid someAIReviewerAssignmentId = Guid.NewGuid();

            var unauthorizedAIReviewerAssignmentException =
                new UnauthorizedAIReviewerAssignmentException(
                    message: "Returning a stale AI reviewer assignment to pending is the approval "
                        + "workflow's own act; no user may perform it.");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedAIReviewerAssignmentException);

            // when
            ValueTask<AIReviewerAssignment> returnToPendingTask =
                this.aiReviewerAssignmentWorkflowService
                    .ReturnStaleAIReviewerAssignmentToPendingAsync(
                        someAIReviewerAssignmentId,
                        TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    returnToPendingTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAIReviewerAssignmentByIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
