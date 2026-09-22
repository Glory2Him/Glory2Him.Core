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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Tests.Integration.Brokers;
using Xunit;

namespace Glory2Him.Core.Tests.Integration.Services.Foundations.ApprovalComments
{
    /// <summary>
    /// Proves <c>StorageBroker.SelectApprovalCommentsByApprovalIdAsync</c> against a real
    /// catalogue — that the slice is the round's own, and that the read is deliberately
    /// unfiltered beyond the approval id, so a tombstoned comment is still included (§14.7
    /// visibility is the service's to apply over the result, issue #637).
    /// </summary>
    [Collection(NarrowReadIntegrationCollection.Name)]
    public sealed class ApprovalCommentNarrowReadTests : IDisposable
    {
        private readonly NarrowReadQueryBroker broker;
        private readonly List<Approval> seededApprovals;
        private readonly List<ApprovalComment> seededApprovalComments;

        public ApprovalCommentNarrowReadTests(NarrowReadQueryBroker broker)
        {
            this.broker = broker;
            this.seededApprovals = new List<Approval>();
            this.seededApprovalComments = new List<ApprovalComment>();
        }

        [Fact]
        public async Task ShouldReturnOnlyTheRequestedApprovalsCommentsAsync()
        {
            // given: two rounds, each carrying its own comment — the requested round's key must
            // reach SQL rather than the read answering with every comment in the table
            Approval requestedApproval = await SeedApprovalAsync();
            Approval otherApproval = await SeedApprovalAsync();

            ApprovalComment requestedComment =
                await SeedApprovalCommentAsync(requestedApproval.Id, isDeleted: false);

            await SeedApprovalCommentAsync(otherApproval.Id, isDeleted: false);

            // when
            List<ApprovalComment> actualComments =
                await this.broker.StorageBroker.SelectApprovalCommentsByApprovalIdAsync(
                    requestedApproval.Id, TestContext.Current.CancellationToken);

            // then
            actualComments.Select(comment => comment.Id)
                .Should().Equal(new[] { requestedComment.Id });
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

        private async Task<ApprovalComment> SeedApprovalCommentAsync(
            Guid approvalId, bool isDeleted)
        {
            string actorUserId = Guid.NewGuid().ToString();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var approvalComment = new ApprovalComment
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                Comment = "seeded",
                CreatedBy = actorUserId,
                CreatedWhen = now,
                UpdatedBy = actorUserId,
                UpdatedWhen = now,
                IsDeleted = isDeleted,
                DeletedBy = isDeleted ? actorUserId : null,
                DeletedWhen = isDeleted ? now : null,
                DeletionReason = isDeleted ? "seeded" : null,
            };

            await this.broker.SeedAsync(approvalComment);
            this.seededApprovalComments.Add(approvalComment);

            return approvalComment;
        }

        // Comments before their approvals: they carry the FK, and the approval cannot go while
        // one still points at it.
        public void Dispose()
        {
            this.broker.ClearAsync(this.seededApprovalComments).AsTask().GetAwaiter().GetResult();
            this.broker.ClearAsync(this.seededApprovals).AsTask().GetAwaiter().GetResult();
        }
    }
}
