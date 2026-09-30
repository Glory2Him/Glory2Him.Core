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
using Glory2Him.Core.Brokers.DateTimes;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Services.Foundations.ContentItems;
using Glory2Him.Core.Tests.Integration.Brokers;
using Moq;
using Xunit;

namespace Glory2Him.Core.Tests.Integration.Services.Foundations.ContentItems
{
    /// <summary>
    /// Proves the content-item NARROW READS against a real catalogue — the group slice, the
    /// version high-water mark, the published-slot incumbent, the content-type pin, the
    /// duplicate-content check, and the public-groups read.
    ///
    /// <para>Each predicate used to be composed in <c>ContentItemService</c> or
    /// <c>ContentItemProcessingService</c> onto a live queryable and executed synchronously; they
    /// moved to the broker so the query could be awaited with the caller's cancellation token.
    /// The unit suite proved them against LINQ-to-Objects, which no longer stands in for SQL, so
    /// the cases that turn on storage semantics live here.</para>
    ///
    /// <para>The public-groups read is the exception: its condition is written in
    /// <c>ContentItemService</c> as a query-shaping function and handed to the broker
    /// (§ARC12.2.1 rule 3). Its unit tests execute that function over LINQ-to-Objects, and
    /// <see cref="ShouldAnswerThePublicContentItemGroupsInSqlAsync"/> proves that the same
    /// function translates and answers in SQL (rule 6).</para>
    /// </summary>
    [Collection(NarrowReadIntegrationCollection.Name)]
    public sealed class ContentItemNarrowReadTests : IDisposable
    {
        private readonly NarrowReadQueryBroker broker;
        private readonly List<ContentItem> seededContentItems;

        public ContentItemNarrowReadTests(NarrowReadQueryBroker broker)
        {
            this.broker = broker;
            this.seededContentItems = new List<ContentItem>();
        }

        [Fact]
        public async Task ShouldReturnOnlyTheRequestedGroupsRowsIncludingTombstonesAsync()
        {
            // given: the group slice is unfiltered — callers differ on whether a tombstone
            // counts, and the read must not take that decision for them
            Guid groupId = Guid.NewGuid();

            ContentItem liveContentItem = CreateContentItem(groupId: groupId, version: 1);
            ContentItem deletedContentItem = CreateContentItem(groupId: groupId, version: 2);
            deletedContentItem.IsDeleted = true;

            ContentItem otherGroupContentItem = CreateContentItem(
                groupId: Guid.NewGuid(), version: 1);

            await SeedAsync(liveContentItem, deletedContentItem, otherGroupContentItem);

            // when
            List<ContentItem> groupContentItems =
                await this.broker.StorageBroker.SelectContentItemsByGroupIdAsync(
                    groupId, TestContext.Current.CancellationToken);

            // then
            groupContentItems.Select(contentItem => contentItem.Id)
                .Should().BeEquivalentTo(new[] { liveContentItem.Id, deletedContentItem.Id });
        }

        /// <summary>
        /// The unique index on (GroupId, Version) carries no IsDeleted filter, so a soft-deleted
        /// row still owns its number. Skipping it hands a fork a number that collides, which was
        /// issue #271.
        /// </summary>
        [Fact]
        public async Task ShouldCountSoftDeletedRowsInTheGroupVersionsAsync()
        {
            // given
            Guid groupId = Guid.NewGuid();

            ContentItem liveContentItem = CreateContentItem(groupId: groupId, version: 1);
            ContentItem deletedNewerContentItem = CreateContentItem(groupId: groupId, version: 2);
            deletedNewerContentItem.IsDeleted = true;

            await SeedAsync(liveContentItem, deletedNewerContentItem);

            // when
            List<int> versions =
                await this.broker.StorageBroker.SelectContentItemVersionsInGroupAsync(
                    groupId, TestContext.Current.CancellationToken);

            // then
            versions.Should().BeEquivalentTo(new[] { 1, 2 });
        }

        [Fact]
        public async Task ShouldReturnNoVersionsForAnUnknownGroupAsync()
        {
            // when
            List<int> versions =
                await this.broker.StorageBroker.SelectContentItemVersionsInGroupAsync(
                    Guid.NewGuid(), TestContext.Current.CancellationToken);

            // then: an empty set, which the caller reads as a high-water mark of zero
            versions.Should().BeEmpty();
        }

        /// <summary>
        /// The slot READ carries no IsDeleted conjunct, so a row that was published and then
        /// soft-deleted without its flag being cleared still surfaces as the incumbent. Skipping
        /// it would leave the group unpublishable.
        ///
        /// <para>Note the index does not say so: migration
        /// <c>20260830122844_FilterPublishedSlotIndexesOnLiveRows</c> narrowed
        /// <c>IX_ContentItem_IsPublished</c> to <c>[IsPublished] = 1 AND [IsDeleted] = 0</c>, so a
        /// tombstone is outside it. The read is what holds this behaviour, and this is the test
        /// that says so.</para>
        /// </summary>
        [Fact]
        public async Task ShouldFindThePublishedTombstoneHoldingTheGroupSlotAsync()
        {
            // given
            Guid groupId = Guid.NewGuid();

            ContentItem publishedTombstone = CreateContentItem(groupId: groupId, version: 1);
            publishedTombstone.IsPublished = true;
            publishedTombstone.IsDeleted = true;

            ContentItem candidateContentItem = CreateContentItem(groupId: groupId, version: 2);

            await SeedAsync(publishedTombstone, candidateContentItem);

            // when
            ContentItem incumbent =
                await this.broker.StorageBroker.SelectPublishedContentItemInGroupAsync(
                    groupId, candidateContentItem.Id, TestContext.Current.CancellationToken);

            // then
            incumbent.Should().NotBeNull();
            incumbent.Id.Should().Be(publishedTombstone.Id);
        }

        [Fact]
        public async Task ShouldExcludeTheTargetAndOtherGroupsFromThePublishedProbeAsync()
        {
            // given: the target itself is published, and so is a row in a different group.
            // Either one being returned would unpublish the wrong row.
            Guid groupId = Guid.NewGuid();

            ContentItem publishedTarget = CreateContentItem(groupId: groupId, version: 1);
            publishedTarget.IsPublished = true;

            ContentItem otherGroupPublished = CreateContentItem(
                groupId: Guid.NewGuid(), version: 1);

            otherGroupPublished.IsPublished = true;

            await SeedAsync(publishedTarget, otherGroupPublished);

            // when
            ContentItem incumbent =
                await this.broker.StorageBroker.SelectPublishedContentItemInGroupAsync(
                    groupId, publishedTarget.Id, TestContext.Current.CancellationToken);

            // then
            incumbent.Should().BeNull();
        }

        /// <summary>
        /// The pin reads any one row of the group to learn the ContentType every version shares,
        /// and is unfiltered so a caller who cannot SEE the siblings cannot skip it (§3.4.2).
        /// </summary>
        [Fact]
        public async Task ShouldPinContentTypeAgainstASiblingAFilteredReadWouldHideAsync()
        {
            // given: the group's only row is soft-deleted and belongs to somebody else
            Guid groupId = Guid.NewGuid();

            ContentItem hiddenSibling = CreateContentItem(groupId: groupId, version: 1);
            hiddenSibling.ContentType = ContentType.Testimony;
            hiddenSibling.IsDeleted = true;

            await SeedAsync(hiddenSibling);

            // when
            ContentItem pin = await this.broker.StorageBroker.SelectContentItemInGroupAsync(
                groupId, TestContext.Current.CancellationToken);

            // then
            pin.Should().NotBeNull();
            pin.ContentType.Should().Be(ContentType.Testimony);
        }

        [Fact]
        public async Task ShouldReportDuplicateContentOutsideTheExcludedGroupAsync()
        {
            // given
            string contentHash = Guid.NewGuid().ToString("N");

            ContentItem duplicateContentItem = CreateContentItem(
                groupId: Guid.NewGuid(), version: 1);

            duplicateContentItem.ContentType = ContentType.Testimony;
            duplicateContentItem.ContentHash = contentHash;

            await SeedAsync(duplicateContentItem);

            // when
            bool exists = await this.broker.StorageBroker.ExistsContentItemContentAsync(
                ContentType.Testimony,
                contentHash,
                excludedGroupId: Guid.NewGuid(),
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldNotReportDuplicateContentInsideTheExcludedGroupAsync()
        {
            // given: the only match is the caller's own group, which a modify must not collide
            // with
            Guid groupId = Guid.NewGuid();
            string contentHash = Guid.NewGuid().ToString("N");

            ContentItem ownGroupContentItem = CreateContentItem(groupId: groupId, version: 1);
            ownGroupContentItem.ContentType = ContentType.Testimony;
            ownGroupContentItem.ContentHash = contentHash;

            await SeedAsync(ownGroupContentItem);

            // when
            bool exists = await this.broker.StorageBroker.ExistsContentItemContentAsync(
                ContentType.Testimony,
                contentHash,
                excludedGroupId: groupId,
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldNotReportDuplicateContentWhenTheOnlyMatchIsSoftDeletedAsync()
        {
            // given: the duplicate rule is about LIVE content — a tombstone is not a duplicate
            string contentHash = Guid.NewGuid().ToString("N");

            ContentItem deletedContentItem = CreateContentItem(
                groupId: Guid.NewGuid(), version: 1);

            deletedContentItem.ContentType = ContentType.Testimony;
            deletedContentItem.ContentHash = contentHash;
            deletedContentItem.IsDeleted = true;

            await SeedAsync(deletedContentItem);

            // when
            bool exists = await this.broker.StorageBroker.ExistsContentItemContentAsync(
                ContentType.Testimony,
                contentHash,
                excludedGroupId: null,
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldNotReportDuplicateContentUnderADifferentContentTypeAsync()
        {
            // given: the same hash under another type is not the same content
            string contentHash = Guid.NewGuid().ToString("N");

            ContentItem otherTypeContentItem = CreateContentItem(
                groupId: Guid.NewGuid(), version: 1);

            otherTypeContentItem.ContentType = ContentType.Testimony;
            otherTypeContentItem.ContentHash = contentHash;

            await SeedAsync(otherTypeContentItem);

            // when
            bool exists = await this.broker.StorageBroker.ExistsContentItemContentAsync(
                ContentType.Devotional,
                contentHash,
                excludedGroupId: null,
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            exists.Should().BeFalse();
        }

        /// <summary>
        /// The tip derivation, asked as a boolean. Three conjuncts, and two facts: this one says
        /// a higher live sibling in the group IS seen, and the negative below says none of the
        /// three decoys is — a tombstone at a higher version does NOT hold the tip away from a
        /// live row (the mirror image of the version high-water mark, which counts it), a higher
        /// version in ANOTHER group is irrelevant, and the row itself is not "higher" than itself.
        /// </summary>
        [Fact]
        public async Task ShouldReportAHigherLiveVersionInTheSameGroupAsync()
        {
            // given
            Guid groupId = Guid.NewGuid();

            ContentItem candidate = CreateContentItem(groupId: groupId, version: 1);
            ContentItem newerLive = CreateContentItem(groupId: groupId, version: 2);

            await SeedAsync(candidate, newerLive);

            // when
            bool exists =
                await this.broker.StorageBroker.ExistsHigherLiveContentItemVersionInGroupAsync(
                    groupId, 1, TestContext.Current.CancellationToken);

            // then
            exists.Should().BeTrue();
        }

        /// <summary>
        /// One negative fact carrying all three decoys at once, which is what the three separate
        /// negatives it replaced (#486) bought between them. Seeding them together loses nothing:
        /// each decoy is a row the predicate must keep excluding, so the test dies under any
        /// mutation that BROADENS a conjunct — drop <c>IsDeleted == false</c> and the tombstone at
        /// version 4 answers, drop <c>GroupId == groupId</c> and the other group's version 9 does,
        /// weaken <c>&gt;</c> to <c>&gt;=</c> and the candidate reports itself as superseded.
        ///
        /// <para>It says nothing about a mutation that NARROWS one: reversing <c>&gt;</c> to
        /// <c>&lt;</c> leaves this false and this test green. That direction is
        /// <see cref="ShouldReportAHigherLiveVersionInTheSameGroupAsync"/>'s to catch, which is
        /// why the positive fact stays — the two are a pair, not a fact and a spare.</para>
        /// </summary>
        [Fact]
        public async Task ShouldNotReportADecoyAsHoldingTheTipAsync()
        {
            // given: the tombstone owns version 4 for NUMBERING purposes, but nobody edits it, so
            // the live version 3 is still the tip. Neither is the other group's version 9
            // relevant, nor is the candidate higher than itself.
            Guid groupId = Guid.NewGuid();

            ContentItem candidate = CreateContentItem(groupId: groupId, version: 3);

            ContentItem deletedNewer = CreateContentItem(groupId: groupId, version: 4);
            deletedNewer.IsDeleted = true;

            ContentItem otherGroupNewer = CreateContentItem(groupId: Guid.NewGuid(), version: 9);

            await SeedAsync(candidate, deletedNewer, otherGroupNewer);

            // when
            bool exists =
                await this.broker.StorageBroker.ExistsHigherLiveContentItemVersionInGroupAsync(
                    groupId, 3, TestContext.Current.CancellationToken);

            // then
            exists.Should().BeFalse();
        }

        /// <summary>
        /// The public-groups read's query-shaping function, run against the real catalogue
        /// (§ARC12.2.1 rule 6). Its unit tests execute it over LINQ-to-Objects; what only SQL
        /// answers is whether EF translates the id match over a supplied list, the correlated
        /// test for a visible version in the same group, and the nullable publish-date term -
        /// and whether the translated query answers as criteria 1 to 4 require.
        /// </summary>
        [Fact]
        public async Task ShouldAnswerThePublicContentItemGroupsInSqlAsync()
        {
            // given: whole seconds, so the boundary row compares equal in SQL as it does in .NET
            DateTimeOffset currentDateTime = DateTimeOffset.FromUnixTimeSeconds(
                DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            Guid visibleGroupId = Guid.NewGuid();

            ContentItem visibleContentItem = CreateCanonicallyVisibleContentItem(
                groupId: visibleGroupId, version: 1, publishDate: null);

            visibleContentItem.ContentType = ContentType.Devotional;

            ContentItem draftContentItem = CreateContentItem(groupId: visibleGroupId, version: 2);
            draftContentItem.ContentType = ContentType.Devotional;

            ContentItem boundaryContentItem = CreateCanonicallyVisibleContentItem(
                groupId: Guid.NewGuid(), version: 1, publishDate: currentDateTime);

            ContentItem deletedContentItem = CreateCanonicallyVisibleContentItem(
                groupId: Guid.NewGuid(), version: 1, publishDate: null);

            deletedContentItem.IsDeleted = true;

            ContentItem unapprovedContentItem = CreateCanonicallyVisibleContentItem(
                groupId: Guid.NewGuid(), version: 1, publishDate: null);

            unapprovedContentItem.ApprovalStatus = ApprovalStatus.Submitted;

            ContentItem unpublishedContentItem = CreateCanonicallyVisibleContentItem(
                groupId: Guid.NewGuid(), version: 1, publishDate: null);

            unpublishedContentItem.IsPublished = false;

            ContentItem futurePublishedContentItem = CreateCanonicallyVisibleContentItem(
                groupId: Guid.NewGuid(), version: 1, publishDate: currentDateTime.AddMinutes(1));

            await SeedAsync(
                visibleContentItem,
                draftContentItem,
                boundaryContentItem,
                deletedContentItem,
                unapprovedContentItem,
                unpublishedContentItem,
                futurePublishedContentItem);

            IReadOnlyList<Guid> inputContentItemIds = new[]
            {
                draftContentItem.Id,
                visibleContentItem.Id,
                boundaryContentItem.Id,
                deletedContentItem.Id,
                unapprovedContentItem.Id,
                unpublishedContentItem.Id,
                futurePublishedContentItem.Id,
                Guid.NewGuid()
            };

            var expectedPublicContentItemGroups = new[]
            {
                new PublicContentItemGroup(
                    visibleContentItem.Id, visibleGroupId, ContentType.Devotional),

                new PublicContentItemGroup(
                    draftContentItem.Id, visibleGroupId, ContentType.Devotional),

                new PublicContentItemGroup(
                    boundaryContentItem.Id, boundaryContentItem.GroupId, ContentType.Testimony)
            };

            IContentItemService contentItemService = CreateContentItemService(currentDateTime);

            // when
            IReadOnlyList<PublicContentItemGroup> actualPublicContentItemGroups =
                await contentItemService.RetrievePublicContentItemGroupsAsync(
                    inputContentItemIds, TestContext.Current.CancellationToken);

            // then
            actualPublicContentItemGroups.Should().BeEquivalentTo(
                expectedPublicContentItemGroups);
        }

        // A real service over the real storage broker, so the condition under test is the one
        // the service authors rather than a copy of it. Only the clock is set; the read consults
        // no other broker.
        private IContentItemService CreateContentItemService(DateTimeOffset currentDateTime)
        {
            var dateTimeBrokerMock = new Mock<IDateTimeBroker>();

            dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            return new ContentItemService(
                storageBroker: this.broker.StorageBroker,
                dateTimeBroker: dateTimeBrokerMock.Object,
                identifierBroker: new Mock<IIdentifierBroker>().Object,
                eventBroker: new Mock<IEventBroker>().Object,
                eventEnvelopeBroker: new Mock<IEventEnvelopeBroker>().Object,
                securityAuditBroker: new Mock<ISecurityAuditBroker>().Object,
                accessBroker: new Mock<IAccessBroker>().Object,
                envelopeIntegrityBroker: new Mock<IEnvelopeIntegrityBroker>().Object,
                loggingBroker: new Mock<ILoggingBroker>().Object);
        }

        private static ContentItem CreateCanonicallyVisibleContentItem(
            Guid groupId,
            int version,
            DateTimeOffset? publishDate)
        {
            ContentItem contentItem = CreateContentItem(groupId, version);
            contentItem.ApprovalStatus = ApprovalStatus.Approved;
            contentItem.IsPublished = true;
            contentItem.PublishDate = publishDate;

            return contentItem;
        }

        private static ContentItem CreateContentItem(Guid groupId, int version)
        {
            string actorUserId = Guid.NewGuid().ToString();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            return new ContentItem
            {
                Id = Guid.NewGuid(),
                ContentType = ContentType.Testimony,
                Title = "seeded",
                Author = "seeded",
                Content = "seeded",
                ShareabilityBasis = ShareabilityBasis.Owned,
                SharePermission = string.Empty,
                ContentHash = Guid.NewGuid().ToString("N"),
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

        private async Task SeedAsync(params ContentItem[] contentItems)
        {
            await this.broker.SeedAsync(contentItems);
            this.seededContentItems.AddRange(contentItems);
        }

        public void Dispose() =>
            this.broker.ClearAsync(this.seededContentItems).AsTask().GetAwaiter().GetResult();
    }
}
