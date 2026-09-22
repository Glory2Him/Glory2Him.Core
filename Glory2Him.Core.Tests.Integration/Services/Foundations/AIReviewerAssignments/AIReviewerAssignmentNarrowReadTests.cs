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
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Tests.Integration.Brokers;
using Xunit;

namespace Glory2Him.Core.Tests.Integration.Services.Foundations.AIReviewerAssignments
{
    /// <summary>
    /// Proves <c>StorageBroker.SelectAIReviewerAssignmentByApprovalIdAsync</c> against a real
    /// catalogue — the <c>IsDeleted == false</c> filter, and the filtered unique index's own claim
    /// that at most one row this query could ever return (issue #637).
    /// </summary>
    [Collection(NarrowReadIntegrationCollection.Name)]
    public sealed class AIReviewerAssignmentNarrowReadTests : IDisposable
    {
        private readonly NarrowReadQueryBroker broker;
        private readonly List<Approval> seededApprovals;
        private readonly List<AIReviewerAssignment> seededAIReviewerAssignments;

        public AIReviewerAssignmentNarrowReadTests(NarrowReadQueryBroker broker)
        {
            this.broker = broker;
            this.seededApprovals = new List<Approval>();
            this.seededAIReviewerAssignments = new List<AIReviewerAssignment>();
        }

        [Fact]
        public async Task ShouldReturnOnlyTheRequestedApprovalsAssignmentAsync()
        {
            // given: two rounds, each carrying its own LIVE assignment. Querying only one key
            // and asserting a match is not enough — with no ORDER BY, FirstOrDefaultAsync can
            // return either live row by physical read order even if the ApprovalId conjunct is
            // dropped entirely, so a single-sided assertion would pass or fail on luck. Reading
            // BOTH keys and requiring each to answer with its OWN row is what a dropped conjunct
            // cannot satisfy: with two candidate rows and no filter, both reads would collapse
            // onto whichever row happens to be read first, so at most one of the two assertions
            // below could ever hold.
            Approval requestedApproval = await SeedApprovalAsync();
            Approval otherApproval = await SeedApprovalAsync();

            AIReviewerAssignment requestedAssignment =
                await SeedAIReviewerAssignmentAsync(requestedApproval.Id, isDeleted: false);

            AIReviewerAssignment otherAssignment =
                await SeedAIReviewerAssignmentAsync(otherApproval.Id, isDeleted: false);

            // when
            AIReviewerAssignment actualRequestedAssignment =
                await this.broker.StorageBroker.SelectAIReviewerAssignmentByApprovalIdAsync(
                    requestedApproval.Id, TestContext.Current.CancellationToken);

            AIReviewerAssignment actualOtherAssignment =
                await this.broker.StorageBroker.SelectAIReviewerAssignmentByApprovalIdAsync(
                    otherApproval.Id, TestContext.Current.CancellationToken);

            // then
            actualRequestedAssignment.Should().NotBeNull();
            actualRequestedAssignment.Id.Should().Be(requestedAssignment.Id);
            actualOtherAssignment.Should().NotBeNull();
            actualOtherAssignment.Id.Should().Be(otherAssignment.Id);
        }

        [Fact]
        public async Task ShouldReturnNullForAnApprovalWithNoAssignmentAsync()
        {
            // given: a round nobody has ever assigned Berean to
            Approval approvalWithNoAssignment = await SeedApprovalAsync();

            // when
            AIReviewerAssignment actualAssignment =
                await this.broker.StorageBroker.SelectAIReviewerAssignmentByApprovalIdAsync(
                    approvalWithNoAssignment.Id, TestContext.Current.CancellationToken);

            // then: the empty answer, not a fault
            actualAssignment.Should().BeNull();
        }

        [Fact]
        public async Task ShouldExcludeASoftDeletedAssignmentAsync()
        {
            // given: the round's only assignment has been withdrawn — the filtered unique index
            // leaves this round free for a fresh assignment, and the read must agree
            Approval approval = await SeedApprovalAsync();
            await SeedAIReviewerAssignmentAsync(approval.Id, isDeleted: true);

            // when
            AIReviewerAssignment actualAssignment =
                await this.broker.StorageBroker.SelectAIReviewerAssignmentByApprovalIdAsync(
                    approval.Id, TestContext.Current.CancellationToken);

            // then
            actualAssignment.Should().BeNull();
        }

        /// <summary>
        /// Matches criterion 2 of #528: an already-cancelled token must cancel the read rather
        /// than let it run to completion.
        /// </summary>
        [Fact]
        public async Task ShouldCancelTheAssignmentReadWhenTheTokenIsAlreadyCancelledAsync()
        {
            // given
            var alreadyCancelledToken = new CancellationToken(canceled: true);

            // when
            Func<Task> readingAssignment = async () =>
                await this.broker.StorageBroker.SelectAIReviewerAssignmentByApprovalIdAsync(
                    Guid.NewGuid(), alreadyCancelledToken);

            // then
            await readingAssignment.Should().ThrowAsync<OperationCanceledException>();
        }

        private async Task<Approval> SeedApprovalAsync()
        {
            string actorUserId = Guid.NewGuid().ToString();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var approval = new Approval
            {
                Id = Guid.NewGuid(),
                EntityType = EntityType.Association,
                EntityId = Guid.NewGuid(),
                ApprovalStatus = ApprovalStatus.Submitted,
                CreatedBy = actorUserId,
                CreatedWhen = now,
                UpdatedBy = actorUserId,
                UpdatedWhen = now,
            };

            await this.broker.SeedAsync(approval);
            this.seededApprovals.Add(approval);

            return approval;
        }

        private async Task<AIReviewerAssignment> SeedAIReviewerAssignmentAsync(
            Guid approvalId, bool isDeleted)
        {
            string actorUserId = Guid.NewGuid().ToString();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var aiReviewerAssignment = new AIReviewerAssignment
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                CreatedBy = actorUserId,
                CreatedWhen = now,
                UpdatedBy = actorUserId,
                UpdatedWhen = now,
                IsDeleted = isDeleted,
                DeletedBy = isDeleted ? actorUserId : null,
                DeletedWhen = isDeleted ? now : null,
                DeletionReason = isDeleted ? "seeded" : null,
            };

            await this.broker.SeedAsync(aiReviewerAssignment);
            this.seededAIReviewerAssignments.Add(aiReviewerAssignment);

            return aiReviewerAssignment;
        }

        // AI reviewer assignments and their approvals last: the assignment carries the FK, and
        // the approval cannot go while it points at one.
        public void Dispose()
        {
            this.broker.ClearAsync(this.seededAIReviewerAssignments)
                .AsTask().GetAwaiter().GetResult();

            this.broker.ClearAsync(this.seededApprovals).AsTask().GetAwaiter().GetResult();
        }
    }
}
