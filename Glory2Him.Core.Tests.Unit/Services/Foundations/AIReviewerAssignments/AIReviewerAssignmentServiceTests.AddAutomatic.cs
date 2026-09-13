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
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.AIReviewerAssignments
{
    public partial class AIReviewerAssignmentServiceTests
    {
        /// <summary>
        /// §8.6.2.1 — a round opened at <c>Submitted</c> under a policy that says Berean should
        /// look at it, so the assignment arrives with nobody having clicked. The caller hands
        /// over the ACT and an approval id; the service mints both the identity and the row's
        /// own <c>Id</c>, which is what makes <c>IsSystemIdentity</c> unforgeable by
        /// construction rather than by validation.
        ///
        /// <para><b>What it catches.</b> The caller here holds NO review role — the system
        /// identity <c>CreateSystemAsync</c> mints is deliberately roleless — so routing this
        /// verb through <c>ValidateUserIsAllowedToManageAIReviewerAssignments</c> reds it, which
        /// is the split §8.6.2.1 relies on. It also reds on either flag being carried rather
        /// than driven to <c>false</c>, on the <c>Id</c> coming from anywhere but
        /// <c>IIdentifierBroker</c>, and on any <c>ProcessedEvents</c> bookkeeping appearing:
        /// the seam has no request address and no receiver name, so both halves of the dedup
        /// pair would be written with no reader.</para>
        /// </summary>
        [Fact]
        public async Task ShouldAddAnAutomaticAIReviewerAssignmentUnderTheSystemIdentityAsync()
        {
            // given: the caller holds NO review role — the system identity is the authority here
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Guid inputApprovalId = Guid.NewGuid();
            Guid mintedIdentifier = Guid.NewGuid();

            var auditAppliedAIReviewerAssignment = new AIReviewerAssignment
            {
                Id = mintedIdentifier,
                ApprovalId = inputApprovalId,
                IsAIReviewCompleted = false,
                IsAIReviewCommentsPresent = false,
                CreatedBy = SystemIdentity.UserId,
                UpdatedBy = SystemIdentity.UserId,
                CreatedWhen = randomDateTimeOffset,
                UpdatedWhen = randomDateTimeOffset
            };

            AIReviewerAssignment storageAIReviewerAssignment =
                auditAppliedAIReviewerAssignment.DeepClone();

            AIReviewerAssignment expectedAIReviewerAssignment =
                storageAIReviewerAssignment.DeepClone();

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(mintedIdentifier);

            // Matched on the row the verb ASSEMBLES and on a system context, so the setup itself
            // states what this verb owes: the minted id, the caller's approval id, both flags
            // false, and a stamp taken from an identity nobody could have supplied.
            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    It.Is<AIReviewerAssignment>(aiReviewerAssignment =>
                        aiReviewerAssignment.Id == mintedIdentifier
                            && aiReviewerAssignment.ApprovalId == inputApprovalId
                            && aiReviewerAssignment.IsAIReviewCompleted == false
                            && aiReviewerAssignment.IsAIReviewCommentsPresent == false),
                    It.Is<SecurityContext>(securityContext =>
                        securityContext.IsSystemIdentity)))
                            .ReturnsAsync(auditAppliedAIReviewerAssignment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(SystemIdentity.UserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertAIReviewerAssignmentAsync(
                    auditAppliedAIReviewerAssignment, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageAIReviewerAssignment);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAIReviewerAssignmentAsync(
                    It.IsAny<EventEnvelope<AIReviewerAssignment>>(),
                    AIReviewerAssignmentEventOperation.Added))
                        .Returns(new ValueTask<EventPublishResult<AIReviewerAssignment>>(
                            new EventPublishResult<AIReviewerAssignment>()));

            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            // when
            AIReviewerAssignment actualAIReviewerAssignment =
                await this.aiReviewerAssignmentWorkflowService
                    .AddAutomaticAIReviewerAssignmentAsync(inputApprovalId, cancellationToken);

            // then: the returned row is the STORED one, not the assembled one
            actualAIReviewerAssignment.Should().BeEquivalentTo(expectedAIReviewerAssignment);
            actualAIReviewerAssignment.ApprovalId.Should().Be(inputApprovalId);
            actualAIReviewerAssignment.IsAIReviewCompleted.Should().BeFalse();
            actualAIReviewerAssignment.IsAIReviewCommentsPresent.Should().BeFalse();

            // The caller hands over no entity, so the row's identity is the service's to mint.
            this.identifierBrokerMock.Verify(broker =>
                broker.GetIdentifierAsync(),
                Times.Once);

            // PINNED ON THE SUBJECT, not only on the flag. IsSystemIdentity alone is satisfied by
            // CreateElevatedAsync too, and that verb KEEPS the caller as the subject — so a verb
            // switched to it would still pass a flag-only assertion while stamping CreatedBy with
            // whoever's submission opened the round, which is the one thing §14.6.1's actor rule
            // forbids here. The trigger survives on DelegatedBySubjectId, so the trail back to the
            // person is kept without making them the author.
            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(
                    It.Is<AIReviewerAssignment>(aiReviewerAssignment =>
                        aiReviewerAssignment.Id == mintedIdentifier
                            && aiReviewerAssignment.ApprovalId == inputApprovalId
                            && aiReviewerAssignment.IsAIReviewCompleted == false
                            && aiReviewerAssignment.IsAIReviewCommentsPresent == false),
                    It.Is<SecurityContext>(securityContext =>
                        securityContext.IsSystemIdentity
                            && securityContext.SubjectId == SystemIdentity.UserId
                            && securityContext.Username == SystemIdentity.Username
                            && securityContext.DelegatedBySubjectId
                                == this.ambientSecurityContext.SubjectId)),
                Times.Once);

            // The CALLER's token, not a fresh one: a cancelled request has to reach the insert.
            this.storageBrokerMock.Verify(broker =>
                broker.InsertAIReviewerAssignmentAsync(
                    auditAppliedAIReviewerAssignment, cancellationToken),
                Times.Once);

            // The ordinary Added fact any assignment publishes — §8.6.2.1 mints no address of its
            // own in either direction, and an automatic assignment is not something a caller may
            // ask for.
            this.eventBrokerMock.Verify(broker =>
                broker.PublishAIReviewerAssignmentAsync(
                    It.IsAny<EventEnvelope<AIReviewerAssignment>>(),
                    AIReviewerAssignmentEventOperation.Added),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(), It.IsAny<CancellationToken>()),
                Times.Never);

            // No ProcessedEvents bookkeeping on either side, matching both siblings and
            // Events.md §EVN19 rule 1: the seam has no request address and no receiver name a
            // subscription owns, so a record written here would have no reader.
            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.IsAny<ProcessedEvent>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// THE ROW THIS VERB ASSEMBLES IS THE SAME ROW THE MODERATOR'S ADD WRITES, so the same
        /// on-add rules stand over it. An approval id the caller never filled in is the one of
        /// those rules a caller of THIS verb can actually trip, and it is refused before storage
        /// is touched and before anything is published.
        ///
        /// <para><b>What it catches.</b> Dropping
        /// <c>ValidateOnAddAIReviewerAssignmentAsync</c> from this path: an assignment keyed on
        /// no approval would reach the insert, where nothing but a foreign key stands between it
        /// and a row belonging to nobody. Reusing the public path's validator rather than
        /// writing a second, weaker copy is the point — the row is the same row, so the rules
        /// over it must not be able to drift apart.</para>
        /// </summary>
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddAutomaticIfApprovalIdIsInvalidAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Guid invalidApprovalId = Guid.Empty;
            Guid mintedIdentifier = Guid.NewGuid();

            var auditAppliedAIReviewerAssignment = new AIReviewerAssignment
            {
                Id = mintedIdentifier,
                ApprovalId = invalidApprovalId,
                IsAIReviewCompleted = false,
                IsAIReviewCommentsPresent = false,
                CreatedBy = SystemIdentity.UserId,
                UpdatedBy = SystemIdentity.UserId,
                CreatedWhen = randomDateTimeOffset,
                UpdatedWhen = randomDateTimeOffset
            };

            var invalidAIReviewerAssignmentException =
                new InvalidAIReviewerAssignmentException(
                    message: "AI reviewer assignment is invalid, fix the errors and try again.");

            invalidAIReviewerAssignmentException.AddData(
                key: nameof(AIReviewerAssignment.ApprovalId),
                values: "Id is required");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, fix the errors and try again.",
                    innerException: invalidAIReviewerAssignmentException);

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(mintedIdentifier);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    It.IsAny<AIReviewerAssignment>(), It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedAIReviewerAssignment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(SystemIdentity.UserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<AIReviewerAssignment> addAutomaticTask =
                this.aiReviewerAssignmentWorkflowService
                    .AddAutomaticAIReviewerAssignmentAsync(
                        invalidApprovalId,
                        TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    addAutomaticTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishAIReviewerAssignmentAsync(
                    It.IsAny<EventEnvelope<AIReviewerAssignment>>(),
                    It.IsAny<AIReviewerAssignmentEventOperation>()),
                Times.Never);
        }

        /// <summary>
        /// THE CONTRIBUTION HALF OF THE GATE RUNS, and it runs FIRST. The other half —
        /// <c>ValidateUserIsAllowedToManageAIReviewerAssignments</c> — deliberately does not: the
        /// system identity holds no roles, so asking for the review tier here would refuse the
        /// only caller this verb has. That negative is proved by the happy path above, which
        /// succeeds under a roleless context; this is the positive half.
        ///
        /// <para>Reached the same way the system-identity guard above is, through a
        /// pass-through mint, so the unauthenticated caller's own context arrives at the do-work.
        /// The message is what pins WHICH gate refused: the act guard would answer differently,
        /// so a test that only asserted "refused" could not tell the two apart.</para>
        /// </summary>
        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnAddAutomaticIfTheContextMayNotContributeAsync(
            SecurityContext unauthenticatedSecurityContext)
        {
            // given
            this.systemContextIsGenuine = false;
            this.ambientSecurityContext = unauthenticatedSecurityContext;
            Guid someApprovalId = Guid.NewGuid();

            var unauthorizedAIReviewerAssignmentException =
                new UnauthorizedAIReviewerAssignmentException(
                    message: "The current user is not authenticated.");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedAIReviewerAssignmentException);

            // when
            ValueTask<AIReviewerAssignment> addAutomaticTask =
                this.aiReviewerAssignmentWorkflowService
                    .AddAutomaticAIReviewerAssignmentAsync(
                        someApprovalId,
                        TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    addAutomaticTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishAIReviewerAssignmentAsync(
                    It.IsAny<EventEnvelope<AIReviewerAssignment>>(),
                    It.IsAny<AIReviewerAssignmentEventOperation>()),
                Times.Never);
        }

        /// <summary>
        /// THE AUTHORIZATION BOUNDARY ON THIS SEAM. There is no tier to ask for — the system
        /// identity holds no roles — so "is this the workflow's own act" is the whole of the
        /// gate, and a gate nobody tests is a comment.
        ///
        /// <para>It is unreachable through the public seam, which mints the context itself two
        /// methods up, so this exercises it through the ambient <c>CreateSystemAsync</c> stub
        /// returning a NON-system context. What it protects against is a future second caller of
        /// the private do-work supplying its own envelope — the route by which a person's context
        /// could otherwise reach a write that must record the system.</para>
        ///
        /// <para><b>What it catches.</b> Deleting
        /// <c>ValidateAutomaticAssignmentIsTheWorkflowsOwnAct</c>: an administrator — who passes
        /// the contribution half of the gate on their own — would then be able to author an
        /// assignment with <c>CreatedBy</c> naming them, which is a moderator's deliberate
        /// request stamped onto an act nobody performed.</para>
        /// </summary>
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddAutomaticIfTheContextIsNotTheSystemAsync()
        {
            // given: the mint is a pass-through, so the ADMINISTRATOR's own context — their roles,
            // their subject, no system flag — reaches the do-work instead of a system-minted one.
            // An administrator because they are the one caller who passes every other gate on this
            // path unaided, so this guard is all that stands between them and the write.
            this.systemContextIsGenuine = false;
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            Guid someApprovalId = Guid.NewGuid();

            var unauthorizedAIReviewerAssignmentException =
                new UnauthorizedAIReviewerAssignmentException(
                    message: "Assigning the AI reviewer automatically is the approval workflow's "
                        + "own act; no user may perform it.");

            var expectedAIReviewerAssignmentValidationException =
                new AIReviewerAssignmentValidationException(
                    message: "AI reviewer assignment validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedAIReviewerAssignmentException);

            // when
            ValueTask<AIReviewerAssignment> addAutomaticTask =
                this.aiReviewerAssignmentWorkflowService
                    .AddAutomaticAIReviewerAssignmentAsync(
                        someApprovalId,
                        TestContext.Current.CancellationToken);

            AIReviewerAssignmentValidationException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentValidationException>(
                    addAutomaticTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentValidationException))),
                Times.Once);

            // refused BEFORE any storage call, and before the row is even assembled
            this.storageBrokerMock.Verify(broker =>
                broker.InsertAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishAIReviewerAssignmentAsync(
                    It.IsAny<EventEnvelope<AIReviewerAssignment>>(),
                    It.IsAny<AIReviewerAssignmentEventOperation>()),
                Times.Never);
        }

        /// <summary>
        /// <c>ISecurityAuditBroker</c> faulting, and the first of the four mappings criterion 12
        /// names: a SQL error is the CRITICAL dependency failure, logged through
        /// <c>LogCriticalAsync</c> rather than <c>LogErrorAsync</c>.
        ///
        /// <para>The arm itself is the class's existing one — this verb shares the same
        /// <c>TryCatch</c> and the same <c>AIReviewerAssignment*</c> family as the caller-facing
        /// add, which is what keeps the orchestration that will call it at two exception families
        /// rather than three. That is a fact worth verifying rather than assuming, which is why
        /// this and its four siblings below are named tests rather than a line of prose.</para>
        /// </summary>
        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnAddAutomaticIfSqlErrorOccursAndLogItAsync()
        {
            // given
            Guid someApprovalId = Guid.NewGuid();
            SqlException sqlException = GetSqlException();

            var failedStorageAIReviewerAssignmentException =
                new FailedStorageAIReviewerAssignmentException(
                    message: "Failed AI reviewer assignment storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

            var expectedAIReviewerAssignmentDependencyException =
                new AIReviewerAssignmentDependencyException(
                    message: "AI reviewer assignment dependency error occurred, contact support.",
                    innerException: failedStorageAIReviewerAssignmentException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    It.IsAny<AIReviewerAssignment>(), It.IsAny<SecurityContext>()))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<AIReviewerAssignment> addAutomaticTask =
                this.aiReviewerAssignmentWorkflowService
                    .AddAutomaticAIReviewerAssignmentAsync(
                        someApprovalId,
                        TestContext.Current.CancellationToken);

            AIReviewerAssignmentDependencyException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentDependencyException>(
                    addAutomaticTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentDependencyException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// The STORAGE INSERT faulting, and the second of the four mappings: a general storage
        /// error is the ordinary dependency failure rather than the critical one, logged through
        /// <c>LogErrorAsync</c>.
        /// </summary>
        [Fact]
        public async Task ShouldThrowDependencyExceptionOnAddAutomaticIfStorageErrorOccursAndLogItAsync()
        {
            // given
            Guid someApprovalId = Guid.NewGuid();
            ArrangeAnAutomaticAssignmentReachingStorage(someApprovalId);
            var dbUpdateException = new DbUpdateException();

            var failedStorageAIReviewerAssignmentException =
                new FailedStorageAIReviewerAssignmentException(
                    message: "Failed AI reviewer assignment storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

            var expectedAIReviewerAssignmentDependencyException =
                new AIReviewerAssignmentDependencyException(
                    message: "AI reviewer assignment dependency error occurred, contact support.",
                    innerException: failedStorageAIReviewerAssignmentException);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dbUpdateException);

            // when
            ValueTask<AIReviewerAssignment> addAutomaticTask =
                this.aiReviewerAssignmentWorkflowService
                    .AddAutomaticAIReviewerAssignmentAsync(
                        someApprovalId,
                        TestContext.Current.CancellationToken);

            AIReviewerAssignmentDependencyException actualException =
                await Assert.ThrowsAsync<AIReviewerAssignmentDependencyException>(
                    addAutomaticTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedAIReviewerAssignmentDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAIReviewerAssignmentDependencyException))),
                Times.Once);

            // the fact is never published for a row that never landed
            this.eventBrokerMock.Verify(broker =>
                broker.PublishAIReviewerAssignmentAsync(
                    It.IsAny<EventEnvelope<AIReviewerAssignment>>(),
                    It.IsAny<AIReviewerAssignmentEventOperation>()),
                Times.Never);
        }

        /// <summary>
        /// The arrangement every fault BELOW the validation gate needs: an id to mint, an audit
        /// stamp the on-add rules accept, and a clock the recency rule accepts. Without it the
        /// verb refuses the row before it ever reaches the dependency the test is about, and the
        /// test would pass for the wrong reason.
        /// </summary>
        private AIReviewerAssignment ArrangeAnAutomaticAssignmentReachingStorage(Guid approvalId)
        {
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            var auditAppliedAIReviewerAssignment = new AIReviewerAssignment
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                IsAIReviewCompleted = false,
                IsAIReviewCommentsPresent = false,
                CreatedBy = SystemIdentity.UserId,
                UpdatedBy = SystemIdentity.UserId,
                CreatedWhen = randomDateTimeOffset,
                UpdatedWhen = randomDateTimeOffset
            };

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(auditAppliedAIReviewerAssignment.Id);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    It.IsAny<AIReviewerAssignment>(), It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedAIReviewerAssignment);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(SystemIdentity.UserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            return auditAppliedAIReviewerAssignment;
        }
    }
}
