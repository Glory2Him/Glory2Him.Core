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
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Tests.Integration.Brokers;
using Xunit;

namespace Glory2Him.Core.Tests.Integration.Services.Foundations.ApprovalReviewRequests
{
    /// <summary>
    /// Proves <see cref="IAccessBroker.FindRetirableApprovalReviewRequestIdsAsync"/> — §7.9 rule
    /// 8's gather — against a real catalogue.
    ///
    /// <para>The unit suite settles the predicate over an in-memory queryable, which is where a
    /// dropped conjunct shows up most legibly. What only a real database can say is that the
    /// predicate TRANSLATES at all: it is composed onto a live <c>IQueryable</c> off the storage
    /// broker and never evaluated in memory in production, so LINQ-to-Objects passing is not
    /// evidence that SQL Server was ever asked the same question. Same reasoning as the narrow
    /// reads this fixture already hosts.</para>
    ///
    /// <para>The <c>AccessBroker</c> is constructed over the fixture's REAL storage broker rather
    /// than mocked, because a mock is exactly the thing that would let a non-translatable
    /// predicate pass.</para>
    /// </summary>
    [Collection(NarrowReadIntegrationCollection.Name)]
    public sealed class RetirableApprovalReviewRequestReadTests : IDisposable
    {
        private readonly NarrowReadQueryBroker broker;
        private readonly IAccessBroker accessBroker;
        private readonly List<Approval> seededApprovals;
        private readonly List<ApprovalReviewRequest> seededApprovalReviewRequests;

        public RetirableApprovalReviewRequestReadTests(NarrowReadQueryBroker broker)
        {
            this.broker = broker;
            this.accessBroker = new AccessBroker(broker.StorageBroker);
            this.seededApprovals = new List<Approval>();
            this.seededApprovalReviewRequests = new List<ApprovalReviewRequest>();
        }

        [Fact]
        public async Task ShouldFindOnlyTheOutstandingRequestsOfTheClosingRoundAsync()
        {
            // given: a closing round with one outstanding invitation and one already gone, and a
            // DIFFERENT round whose invitation must survive — that last row is the whole reason
            // the ApprovalId conjunct has to reach SQL rather than being applied afterwards
            Approval closingApproval = await SeedApprovalAsync(ApprovalStatus.Approved);
            Approval otherApproval = await SeedApprovalAsync(ApprovalStatus.Submitted);

            ApprovalReviewRequest outstandingRequest =
                await SeedApprovalReviewRequestAsync(closingApproval.Id, isDeleted: false);

            ApprovalReviewRequest alreadyGoneRequest =
                await SeedApprovalReviewRequestAsync(closingApproval.Id, isDeleted: true);

            ApprovalReviewRequest otherRoundsRequest =
                await SeedApprovalReviewRequestAsync(otherApproval.Id, isDeleted: false);

            // when
            List<Guid> actualRetirableRequestIds =
                await this.accessBroker.FindRetirableApprovalReviewRequestIdsAsync(
                    approvalId: closingApproval.Id,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualRetirableRequestIds.Should().Equal(new[] { outstandingRequest.Id });
            actualRetirableRequestIds.Should().NotContain(alreadyGoneRequest.Id);
            actualRetirableRequestIds.Should().NotContain(otherRoundsRequest.Id);
        }

        // The empty-round case lives in the unit suite alone, deliberately. Nothing about it
        // depends on a real catalogue — no index, no collation, no three-valued logic — so
        // repeating it here would buy a second assertion of the same fact at the price of a
        // database round trip.

        private async Task<Approval> SeedApprovalAsync(ApprovalStatus approvalStatus)
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
                ApprovalStatus = approvalStatus,
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
            Guid approvalId,
            bool isDeleted)
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

        // Requests first: they carry the FK, and the approvals cannot go while they point at one.
        public void Dispose()
        {
            this.broker.ClearAsync(this.seededApprovalReviewRequests)
                .AsTask().GetAwaiter().GetResult();

            this.broker.ClearAsync(this.seededApprovals).AsTask().GetAwaiter().GetResult();
        }
    }
}
