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
            // given: two rounds, each carrying its own LIVE assignment — the requested round's
            // key must reach SQL rather than the read answering with whichever row it finds first
            Approval requestedApproval = await SeedApprovalAsync();
            Approval otherApproval = await SeedApprovalAsync();

            AIReviewerAssignment requestedAssignment =
                await SeedAIReviewerAssignmentAsync(requestedApproval.Id, isDeleted: false);

            await SeedAIReviewerAssignmentAsync(otherApproval.Id, isDeleted: false);

            // when
            AIReviewerAssignment actualAssignment =
                await this.broker.StorageBroker.SelectAIReviewerAssignmentByApprovalIdAsync(
                    requestedApproval.Id, TestContext.Current.CancellationToken);

            // then
            actualAssignment.Should().NotBeNull();
            actualAssignment.Id.Should().Be(requestedAssignment.Id);
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
