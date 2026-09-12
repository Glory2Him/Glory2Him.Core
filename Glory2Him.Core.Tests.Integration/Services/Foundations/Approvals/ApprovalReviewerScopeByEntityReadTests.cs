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
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Tests.Integration.Brokers;
using Xunit;

namespace Glory2Him.Core.Tests.Integration.Services.Foundations.Approvals
{
    /// <summary>
    /// Proves <see cref="IAccessBroker.RetrieveApprovalReviewerScopeByEntityAsync"/>'s
    /// <c>EntityType</c> conjunct reaches the database rather than being applied in memory.
    ///
    /// <para>The overload is built on the broker's private <c>FindApprovalAsync</c>, which
    /// composes <c>EntityType == entityType &amp;&amp; EntityId == entityId</c> onto a live
    /// <c>IQueryable</c>. The unit suite proves the predicate over an in-memory queryable, which
    /// cannot tell a dropped conjunct from a working one when the two rows it would need to tell
    /// apart never coexist in the same seeded list by accident — a mock is exactly the thing that
    /// would let a non-translatable or narrowed predicate pass. Same reasoning as
    /// <c>RetirableApprovalReviewRequestReadTests</c>.</para>
    ///
    /// <para>The <c>AccessBroker</c> is constructed over the fixture's REAL storage broker rather
    /// than mocked, for the same reason.</para>
    /// </summary>
    [Collection(NarrowReadIntegrationCollection.Name)]
    public sealed class ApprovalReviewerScopeByEntityReadTests : IDisposable
    {
        private readonly NarrowReadQueryBroker broker;
        private readonly IAccessBroker accessBroker;
        private readonly List<Approval> seededApprovals;

        public ApprovalReviewerScopeByEntityReadTests(NarrowReadQueryBroker broker)
        {
            this.broker = broker;
            this.accessBroker = new AccessBroker(broker.StorageBroker);
            this.seededApprovals = new List<Approval>();
        }

        [Fact]
        public async Task ShouldNotMatchAnApprovalThatSharesOnlyTheEntityIdAsync()
        {
            // given: two approvals sharing the SAME EntityId, one on the probed EntityType and one
            // on a different one — the EntityType conjunct is the only thing standing between them
            Guid sharedEntityId = Guid.NewGuid();

            Approval matchingApproval = await SeedApprovalAsync(
                entityType: EntityType.Link,
                entityId: sharedEntityId);

            Approval otherTypeApproval = await SeedApprovalAsync(
                entityType: EntityType.Comment,
                entityId: sharedEntityId);

            // when
            ApprovalReviewerScope actualScope =
                await this.accessBroker.RetrieveApprovalReviewerScopeByEntityAsync(
                    entityType: EntityType.Link,
                    entityId: sharedEntityId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualScope.Should().NotBeNull();
            actualScope.ApprovalId.Should().Be(matchingApproval.Id);
            actualScope.ApprovalId.Should().NotBe(otherTypeApproval.Id);
        }

        private async Task<Approval> SeedApprovalAsync(EntityType entityType, Guid entityId)
        {
            string actorUserId = Guid.NewGuid().ToString();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var approval = new Approval
            {
                Id = Guid.NewGuid(),
                EntityType = entityType,
                EntityId = entityId,
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

        public void Dispose() =>
            this.broker.ClearAsync(this.seededApprovals).AsTask().GetAwaiter().GetResult();
    }
}
