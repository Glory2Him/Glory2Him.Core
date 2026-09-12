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
    /// composes <c>EntityType == entityType &amp;&amp; EntityId == entityId</c> onto a live,
    /// UNORDERED <c>IQueryable</c> via <c>FirstOrDefault</c>. The unit suite proves the predicate
    /// over an in-memory queryable, which cannot tell a dropped conjunct from a working one when
    /// the two rows it would need to tell apart never coexist in the same seeded list by
    /// accident — a mock is exactly the thing that would let a non-translatable or narrowed
    /// predicate pass. Same reasoning as <c>RetirableApprovalReviewRequestReadTests</c>.</para>
    ///
    /// <para>Seeding two rows and asserting on which one comes back is not enough here: over an
    /// unordered scan, dropping the conjunct still sometimes returns the right row by luck.
    /// <see cref="ShouldNotMatchAnApprovalThatSharesOnlyTheEntityIdAsync"/> instead seeds ONLY the
    /// non-matching row, the same deterministic shape as
    /// <c>ApprovalEntityProbeReadTests.ShouldNotMatchARowThatSharesOnlyOneHalfOfTheKeyAsync</c> —
    /// a single wrong row can never coincidentally satisfy "is null".</para>
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
            // given: ONLY a non-matching approval — same EntityId as the one probed for, but a
            // different EntityType. Seeding just this one row (rather than it alongside a
            // matching row) is deliberate: with two rows present, FirstOrDefault over an
            // unordered IQueryable can still return the right row by luck even if the EntityType
            // conjunct is dropped from the predicate, so that shape does not fail reliably under
            // that mutation. With only the non-matching row seeded, a dropped conjunct has nothing
            // else to return and the null assertion below fails every time.
            Guid probedEntityId = Guid.NewGuid();

            Approval otherTypeApproval = await SeedApprovalAsync(
                entityType: EntityType.Comment,
                entityId: probedEntityId);

            // when
            ApprovalReviewerScope actualScope =
                await this.accessBroker.RetrieveApprovalReviewerScopeByEntityAsync(
                    entityType: EntityType.Link,
                    entityId: probedEntityId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualScope.Should().BeNull();
        }

        [Fact]
        public async Task ShouldMatchAnApprovalByEntityTypeAndEntityIdAsync()
        {
            // given: only the matching approval — no competing row is needed to prove the happy
            // path, and the deterministic negative case above already proves the conjunct reaches
            // the database
            Guid entityId = Guid.NewGuid();

            Approval matchingApproval = await SeedApprovalAsync(
                entityType: EntityType.Link,
                entityId: entityId);

            // when
            ApprovalReviewerScope actualScope =
                await this.accessBroker.RetrieveApprovalReviewerScopeByEntityAsync(
                    entityType: EntityType.Link,
                    entityId: entityId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualScope.Should().NotBeNull();
            actualScope.ApprovalId.Should().Be(matchingApproval.Id);
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
