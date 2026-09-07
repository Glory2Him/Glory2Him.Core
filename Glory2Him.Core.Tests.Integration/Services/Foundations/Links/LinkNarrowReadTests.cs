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
            // given: a soft delete never clears IsPublished, and the slot index names that column
            // alone, so the tombstone still holds the slot
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
