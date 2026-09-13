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
    }
}
