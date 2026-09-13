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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Tests.Integration.Brokers;
using Xunit;

namespace Glory2Him.Core.Tests.Integration.Services.Foundations.AIReviewerAssignments
{
    /// <summary>
    /// Proves <see cref="IAccessBroker.IsAIReviewerEverAssignedAsync"/> — §8.6.2.1's gate 5 —
    /// against a real catalogue.
    ///
    /// <para>The unit suite settles the predicate over an in-memory queryable, which is where a
    /// dropped conjunct shows up most legibly. Two things only a real database can say:</para>
    ///
    /// <list type="number">
    /// <item>that the predicate <b>translates</b> at all. It is composed onto a live
    /// <c>IQueryable</c> off the storage broker and never evaluated in memory in production, so
    /// LINQ-to-Objects passing is not evidence SQL Server was ever asked the same
    /// question.</item>
    /// <item>that the <b>true-regardless-of-<c>IsDeleted</c></b> semantics survive the round trip.
    /// The seeded set carries a soft-deleted assignment on the round under test, and that is the
    /// row the whole gate turns on: <c>UX_AIReviewerAssignments_ApprovalId</c> is FILTERED to live
    /// rows, so a reader who assumed the index covered this probe would have built gate 5 on a
    /// read that answers "nothing here" for a round a moderator took Berean off.</item>
    /// </list>
    ///
    /// <para>The <c>AccessBroker</c> is constructed over the fixture's REAL storage broker rather
    /// than mocked, because a mock is exactly the thing that would let a non-translatable
    /// predicate pass.</para>
    /// </summary>
    [Collection(NarrowReadIntegrationCollection.Name)]
    public sealed class AIReviewerEverAssignedReadTests : IDisposable
    {
        private readonly NarrowReadQueryBroker broker;
        private readonly IAccessBroker accessBroker;
        private readonly List<Approval> seededApprovals;
        private readonly List<AIReviewerAssignment> seededAIReviewerAssignments;

        public AIReviewerEverAssignedReadTests(NarrowReadQueryBroker broker)
        {
            this.broker = broker;
            this.accessBroker = new AccessBroker(broker.StorageBroker);
            this.seededApprovals = new List<Approval>();
            this.seededAIReviewerAssignments = new List<AIReviewerAssignment>();
        }

        [Fact]
        public async Task ShouldFindAWithdrawnAssignmentAndNotAnotherRoundsLiveOneAsync()
        {
            // given: a round whose only assignment was WITHDRAWN — the soft delete
            // WithdrawAIReviewerAsync performs — and a different round carrying a live one.
            // That second row is what forces the ApprovalId conjunct to reach SQL rather than
            // being applied afterwards.
            Approval withdrawnRound = await SeedApprovalAsync();
            Approval otherRound = await SeedApprovalAsync();
            Approval untouchedRound = await SeedApprovalAsync();

            await SeedAIReviewerAssignmentAsync(withdrawnRound.Id, isDeleted: true);
            await SeedAIReviewerAssignmentAsync(otherRound.Id, isDeleted: false);

            // when
            bool isEverAssignedToWithdrawnRound =
                await this.accessBroker.IsAIReviewerEverAssignedAsync(
                    approvalId: withdrawnRound.Id,
                    cancellationToken: TestContext.Current.CancellationToken);

            bool isEverAssignedToUntouchedRound =
                await this.accessBroker.IsAIReviewerEverAssignedAsync(
                    approvalId: untouchedRound.Id,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            isEverAssignedToWithdrawnRound.Should().BeTrue(
                because: "a withdrawn assignment still says somebody decided about Berean on "
                    + "this round, and UX_AIReviewerAssignments_ApprovalId is filtered to live "
                    + "rows so the index cannot answer that question");

            isEverAssignedToUntouchedRound.Should().BeFalse(
                because: "a live assignment on a DIFFERENT round must not be found — that is the "
                    + "ApprovalId conjunct reaching SQL rather than being applied in memory "
                    + "afterwards");
        }

        private async Task<Approval> SeedApprovalAsync()
        {
            string actorUserId = Guid.NewGuid().ToString();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var approval = new Approval
            {
                Id = Guid.NewGuid(),

                // Association rather than the zero member, so a dropped key conjunct anywhere
                // downstream cannot match by defaulting to ContentItem.
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
            Guid approvalId,
            bool isDeleted)
        {
            string actorUserId = Guid.NewGuid().ToString();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var aiReviewerAssignment = new AIReviewerAssignment
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                IsAIReviewCompleted = false,
                IsAIReviewCommentsPresent = false,
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

        // Assignments first: they carry the FK, and the approvals cannot go while they point at
        // one.
        public void Dispose()
        {
            this.broker.ClearAsync(this.seededAIReviewerAssignments)
                .AsTask().GetAwaiter().GetResult();

            this.broker.ClearAsync(this.seededApprovals).AsTask().GetAwaiter().GetResult();
        }
    }
}
