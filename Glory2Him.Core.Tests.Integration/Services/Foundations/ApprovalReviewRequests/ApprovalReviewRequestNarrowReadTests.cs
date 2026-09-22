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
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Tests.Integration.Brokers;
using Xunit;

namespace Glory2Him.Core.Tests.Integration.Services.Foundations.ApprovalReviewRequests
{
    /// <summary>
    /// Proves <c>StorageBroker.SelectApprovalReviewRequestsByApprovalIdAsync</c> against a real
    /// catalogue — that the slice is the round's own, and that the read is deliberately
    /// unfiltered beyond the approval id, so a withdrawn invitation is still included (issue
    /// #637).
    /// </summary>
    [Collection(NarrowReadIntegrationCollection.Name)]
    public sealed class ApprovalReviewRequestNarrowReadTests : IDisposable
    {
        private readonly NarrowReadQueryBroker broker;
        private readonly List<Approval> seededApprovals;
        private readonly List<ApprovalReviewRequest> seededApprovalReviewRequests;

        public ApprovalReviewRequestNarrowReadTests(NarrowReadQueryBroker broker)
        {
            this.broker = broker;
            this.seededApprovals = new List<Approval>();
            this.seededApprovalReviewRequests = new List<ApprovalReviewRequest>();
        }

        [Fact]
        public async Task ShouldReturnOnlyTheRequestedApprovalsRequestsAsync()
        {
            // given: two rounds, each carrying its own invitation — the requested round's key
            // must reach SQL rather than the read answering with every invitation in the table
            Approval requestedApproval = await SeedApprovalAsync();
            Approval otherApproval = await SeedApprovalAsync();

            ApprovalReviewRequest requestedRequest =
                await SeedApprovalReviewRequestAsync(requestedApproval.Id, isDeleted: false);

            await SeedApprovalReviewRequestAsync(otherApproval.Id, isDeleted: false);

            // when
            List<ApprovalReviewRequest> actualRequests =
                await this.broker.StorageBroker.SelectApprovalReviewRequestsByApprovalIdAsync(
                    requestedApproval.Id, TestContext.Current.CancellationToken);

            // then
            actualRequests.Select(request => request.Id)
                .Should().Equal(new[] { requestedRequest.Id });
        }

        [Fact]
        public async Task ShouldReturnAnEmptyListForAnApprovalWithNoRequestsAsync()
        {
            // given: a round nobody has been invited to review
            Approval approvalWithNoRequests = await SeedApprovalAsync();

            // when
            List<ApprovalReviewRequest> actualRequests =
                await this.broker.StorageBroker.SelectApprovalReviewRequestsByApprovalIdAsync(
                    approvalWithNoRequests.Id, TestContext.Current.CancellationToken);

            // then: the empty answer, not a fault
            actualRequests.Should().BeEmpty();
        }

        [Fact]
        public async Task ShouldIncludeAWithdrawnRequestAsync()
        {
            // given: a live invitation and a withdrawn one on the same round — the read is
            // deliberately unfiltered beyond the approval id, so both must come back
            Approval approval = await SeedApprovalAsync();

            ApprovalReviewRequest liveRequest =
                await SeedApprovalReviewRequestAsync(approval.Id, isDeleted: false);

            ApprovalReviewRequest withdrawnRequest =
                await SeedApprovalReviewRequestAsync(approval.Id, isDeleted: true);

            // when
            List<ApprovalReviewRequest> actualRequests =
                await this.broker.StorageBroker.SelectApprovalReviewRequestsByApprovalIdAsync(
                    approval.Id, TestContext.Current.CancellationToken);

            // then
            actualRequests.Select(request => request.Id).Should().BeEquivalentTo(
                new[] { liveRequest.Id, withdrawnRequest.Id });
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

        private async Task<ApprovalReviewRequest> SeedApprovalReviewRequestAsync(
            Guid approvalId, bool isDeleted)
        {
            string actorUserId = Guid.NewGuid().ToString();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var approvalReviewRequest = new ApprovalReviewRequest
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                RequestedUserId = Guid.NewGuid().ToString(),
                RequestedUserDisplayName = "Seeded Invitee",
                CreatedBy = actorUserId,
                CreatedWhen = now,
                UpdatedBy = actorUserId,
                UpdatedWhen = now,
                IsDeleted = isDeleted,
                DeletedBy = isDeleted ? actorUserId : null,
                DeletedWhen = isDeleted ? now : null,
                DeletionReason = isDeleted ? "seeded" : null,
            };

            await this.broker.SeedAsync(approvalReviewRequest);
            this.seededApprovalReviewRequests.Add(approvalReviewRequest);

            return approvalReviewRequest;
        }

        // Requests before their approvals: they carry the FK, and the approval cannot go while
        // one still points at it.
        public void Dispose()
        {
            this.broker.ClearAsync(this.seededApprovalReviewRequests)
                .AsTask().GetAwaiter().GetResult();

            this.broker.ClearAsync(this.seededApprovals).AsTask().GetAwaiter().GetResult();
        }
    }
}
