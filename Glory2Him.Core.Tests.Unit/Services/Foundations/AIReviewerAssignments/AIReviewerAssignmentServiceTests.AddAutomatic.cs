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
    }
}
