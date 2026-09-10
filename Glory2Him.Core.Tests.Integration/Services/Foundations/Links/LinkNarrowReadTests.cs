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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Tests.Integration.Brokers;
using Xunit;

namespace Glory2Him.Core.Tests.Integration.Services.Foundations.Links
{
    /// <summary>
    /// The link twin of <c>ContentItemNarrowReadTests</c>: the group slice, the version
    /// high-water mark and the published-slot incumbent, proved against a real catalogue now that
    /// the predicates live in the broker rather than being composed onto a live queryable and
    /// executed synchronously in the service.
    ///
    /// <para><b>Why this duplicate is deliberate.</b> #486 ruled that a mechanism should be proved
    /// once rather than per entity, and this file was deleted on that reading. It came back,
    /// because the ruling does not transfer here: the Link and ContentItem predicates are
    /// duplicated SOURCE, not one shared declaration, so proving the shape once proves nothing
    /// about the other copy. Deleting this left three predicates — the version high-water mark
    /// (#271), the tip derivation and the published-slot probe — executable nowhere, since the
    /// unit suite mocks <c>IStorageBroker</c> and production reaches them only through the
    /// publication swap and the fork, which no acceptance test drives.</para>
    ///
    /// <para>A fixture parameterised over both entities was the alternative. It was rejected as
    /// the more expensive answer: per-entity broker method names force a delegate-adapter table,
    /// and the complexity costs more than the duplication saves. Design §5.6.4 records the same
    /// lesson from the published-slot indexes — hand-written twins drift, and the fix was to
    /// assert every one.</para>
    /// </summary>
    [Collection(NarrowReadIntegrationCollection.Name)]
    public sealed class LinkNarrowReadTests : IDisposable
    {
        private readonly NarrowReadQueryBroker broker;
        private readonly List<Link> seededLinks;

        public LinkNarrowReadTests(NarrowReadQueryBroker broker)
        {
            this.broker = broker;
            this.seededLinks = new List<Link>();
        }

        [Fact]
        public async Task ShouldReturnOnlyTheRequestedGroupsRowsIncludingTombstonesAsync()
        {
            // given
            Guid groupId = Guid.NewGuid();

            Link liveLink = CreateLink(groupId: groupId, version: 1);
            Link deletedLink = CreateLink(groupId: groupId, version: 2);
            deletedLink.IsDeleted = true;

            Link otherGroupLink = CreateLink(groupId: Guid.NewGuid(), version: 1);

            await SeedAsync(liveLink, deletedLink, otherGroupLink);

            // when
            List<Link> groupLinks = await this.broker.StorageBroker.SelectLinksByGroupIdAsync(
                groupId, TestContext.Current.CancellationToken);

            // then
            groupLinks.Select(link => link.Id)
                .Should().BeEquivalentTo(new[] { liveLink.Id, deletedLink.Id });
        }

        /// <summary>
        /// A soft-deleted row still owns its version number under the unfiltered unique index on
        /// (GroupId, Version) — issue #271.
        /// </summary>
        [Fact]
        public async Task ShouldCountSoftDeletedRowsInTheGroupVersionsAsync()
        {
            // given
            Guid groupId = Guid.NewGuid();

            Link liveLink = CreateLink(groupId: groupId, version: 1);
            Link deletedNewerLink = CreateLink(groupId: groupId, version: 2);
            deletedNewerLink.IsDeleted = true;

            await SeedAsync(liveLink, deletedNewerLink);

            // when
            List<int> versions = await this.broker.StorageBroker.SelectLinkVersionsInGroupAsync(
                groupId, TestContext.Current.CancellationToken);

            // then
            versions.Should().BeEquivalentTo(new[] { 1, 2 });
        }

        [Fact]
        public async Task ShouldReturnNoVersionsForAnUnknownGroupAsync()
        {
            // when
            List<int> versions = await this.broker.StorageBroker.SelectLinkVersionsInGroupAsync(
                Guid.NewGuid(), TestContext.Current.CancellationToken);

            // then
            versions.Should().BeEmpty();
        }

        [Fact]
        public async Task ShouldFindThePublishedTombstoneHoldingTheGroupSlotAsync()
        {
            // given: a soft delete never clears IsPublished and the slot READ carries no IsDeleted
            // conjunct, so the tombstone still surfaces as the incumbent. The index does not say
            // so — migration 20260830122844_FilterPublishedSlotIndexesOnLiveRows narrowed
            // UX_Links_GroupId_IsPublished to [IsPublished] = 1 AND [IsDeleted] = 0 — which is
            // exactly why the read needs its own proof.
            Guid groupId = Guid.NewGuid();

            Link publishedTombstone = CreateLink(groupId: groupId, version: 1);
            publishedTombstone.IsPublished = true;
            publishedTombstone.IsDeleted = true;

            Link candidateLink = CreateLink(groupId: groupId, version: 2);

            await SeedAsync(publishedTombstone, candidateLink);

            // when
            Link incumbent = await this.broker.StorageBroker.SelectPublishedLinkInGroupAsync(
                groupId, candidateLink.Id, TestContext.Current.CancellationToken);

            // then
            incumbent.Should().NotBeNull();
            incumbent.Id.Should().Be(publishedTombstone.Id);
        }

        [Fact]
        public async Task ShouldExcludeTheTargetAndOtherGroupsFromThePublishedProbeAsync()
        {
            // given
            Guid groupId = Guid.NewGuid();

            Link publishedTarget = CreateLink(groupId: groupId, version: 1);
            publishedTarget.IsPublished = true;

            Link otherGroupPublished = CreateLink(groupId: Guid.NewGuid(), version: 1);
            otherGroupPublished.IsPublished = true;

            await SeedAsync(publishedTarget, otherGroupPublished);

            // when
            Link incumbent = await this.broker.StorageBroker.SelectPublishedLinkInGroupAsync(
                groupId, publishedTarget.Id, TestContext.Current.CancellationToken);

            // then
            incumbent.Should().BeNull();
        }

        /// <summary>
        /// The link twin of the content-item tip derivation: a tombstone at a higher version does
        /// not take the tip away from a live row, and the row itself is not higher than itself.
        /// </summary>
        [Fact]
        public async Task ShouldReportAHigherLiveVersionInTheSameGroupAsync()
        {
            // given
            Guid groupId = Guid.NewGuid();

            Link candidate = CreateLink(groupId: groupId, version: 1);
            Link newerLive = CreateLink(groupId: groupId, version: 2);

            await SeedAsync(candidate, newerLive);

            // when
            bool exists = await this.broker.StorageBroker.ExistsHigherLiveLinkVersionInGroupAsync(
                groupId, 1, TestContext.Current.CancellationToken);

            // then
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldNotReportASoftDeletedHigherVersionAsHoldingTheTipAsync()
        {
            // given
            Guid groupId = Guid.NewGuid();

            Link candidate = CreateLink(groupId: groupId, version: 1);
            Link deletedNewer = CreateLink(groupId: groupId, version: 2);
            deletedNewer.IsDeleted = true;

            await SeedAsync(candidate, deletedNewer);

            // when
            bool exists = await this.broker.StorageBroker.ExistsHigherLiveLinkVersionInGroupAsync(
                groupId, 1, TestContext.Current.CancellationToken);

            // then
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldNotReportAHigherVersionFromAnotherGroupAsync()
        {
            // given
            Guid groupId = Guid.NewGuid();

            Link candidate = CreateLink(groupId: groupId, version: 1);
            Link otherGroupNewer = CreateLink(groupId: Guid.NewGuid(), version: 9);

            await SeedAsync(candidate, otherGroupNewer);

            // when
            bool exists = await this.broker.StorageBroker.ExistsHigherLiveLinkVersionInGroupAsync(
                groupId, 1, TestContext.Current.CancellationToken);

            // then
            exists.Should().BeFalse();
        }

        private static Link CreateLink(Guid groupId, int version)
        {
            string actorUserId = Guid.NewGuid().ToString();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            return new Link
            {
                Id = Guid.NewGuid(),
                Name = "seeded",
                Url = "https://example.invalid/seeded",
                LinkType = "General",
                GroupId = groupId,
                Version = version,
                ApprovalStatus = ApprovalStatus.Draft,
                CreatedBy = actorUserId,
                CreatedWhen = now,
                UpdatedBy = actorUserId,
                UpdatedWhen = now,
                DeletedBy = null,
                DeletedWhen = null,
            };
        }

        private async Task SeedAsync(params Link[] links)
        {
            await this.broker.SeedAsync(links);
            this.seededLinks.AddRange(links);
        }

        public void Dispose() =>
            this.broker.ClearAsync(this.seededLinks).AsTask().GetAwaiter().GetResult();
    }
}
