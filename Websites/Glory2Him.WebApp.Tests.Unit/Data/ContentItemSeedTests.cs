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
using Glory2Him.Core.Brokers.Hashes;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.WebApp.Data;
using Moq;

namespace Glory2Him.WebApp.Tests.Unit.Data
{
    /// <summary>
    /// Pins the demo content matrix to the enums it is composed from.
    ///
    /// <para><b>Why this needs a test at all.</b> The seed exists so that every surface which
    /// LISTS content items can be developed against rows in each approval status. A pair that
    /// stops being covered fails silently — the surface simply never shows that status, and the
    /// only symptom is somebody eventually asking why. Adding a ContentType member is the obvious
    /// way to cause it, and the seeded set is the last place anyone would look.</para>
    ///
    /// <para>These assert the SHAPE of the composition rather than a list of rows, so a hand
    /// written specimen table that drifts from the enum fails here.</para>
    /// </summary>
    public class ContentItemSeedTests
    {
        private const string ContributorId = "8f3f1a5e-2f1e-4c0e-9c31-9b8fd3a51f77";
        private static readonly DateTimeOffset SeededWhen = new(2026, 8, 31, 9, 0, 0, TimeSpan.Zero);

        private readonly Mock<IHashBroker> hashBrokerMock;

        public ContentItemSeedTests()
        {
            this.hashBrokerMock = new Mock<IHashBroker>();

            // Echoes what it is handed, so a hash assertion below is really an assertion about
            // the NORMALIZED content the seed hands over — the thing the duplicate rule keys on.
            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync(It.IsAny<string>()))
                    .Returns((string text) => new ValueTask<string>(text));
        }

        private ValueTask<IReadOnlyList<ContentItem>> BuildSeedAsync() =>
            ContentItemSeedData.BuildSeedContentItemsAsync(
                this.hashBrokerMock.Object,
                ContributorId,
                SeededWhen);

        [Fact]
        public async Task ShouldSeedEveryContentTypeAndApprovalStatusPair()
        {
            // given
            ContentType[] expectedContentTypes = Enum.GetValues<ContentType>();

            // when
            IReadOnlyList<ContentItem> seedContentItems = await BuildSeedAsync();

            // then
            foreach (ContentType contentType in expectedContentTypes)
            {
                foreach (ApprovalStatus approvalStatus in ContentItemSeedData.SeededApprovalStatuses)
                {
                    seedContentItems.Should().ContainSingle(
                        contentItem => contentItem.ContentType == contentType
                            && contentItem.ApprovalStatus == approvalStatus,
                        because:
                            $"a surface listing {contentType} must have a {approvalStatus} row "
                                + "to show");
                }
            }

            seedContentItems.Should().HaveCount(
                expectedContentTypes.Length * ContentItemSeedData.SeededApprovalStatuses.Length);
        }

        /// <summary>
        /// Dismissed is a state the workflow MOVES a row into once its approval stops counting.
        /// Nothing is ever born in it, so a seeded one would be a row no code path could produce.
        /// </summary>
        [Fact]
        public async Task ShouldNotSeedAnyDismissedContentItem()
        {
            // when
            IReadOnlyList<ContentItem> seedContentItems = await BuildSeedAsync();

            // then
            seedContentItems.Should().NotContain(
                contentItem => contentItem.ApprovalStatus == ApprovalStatus.Dismissed);
        }

        /// <summary>
        /// Identifiers are deterministic so a restart amends nothing, and distinct so no slot
        /// overwrites another. A group is a separate identity from the item in it: every seeded
        /// row is its own lineage at version one, so no two may share a GroupId either.
        /// </summary>
        [Fact]
        public async Task ShouldGiveEverySlotItsOwnIdentityAndLineage()
        {
            // when
            IReadOnlyList<ContentItem> seedContentItems = await BuildSeedAsync();

            // then
            seedContentItems.Select(contentItem => contentItem.Id).Should().OnlyHaveUniqueItems();
            seedContentItems.Select(contentItem => contentItem.GroupId).Should().OnlyHaveUniqueItems();
            seedContentItems.Should().OnlyContain(contentItem => contentItem.Version == 1);
        }

        [Fact]
        public async Task ShouldMintTheSameIdentifiersOnEveryRun()
        {
            // given
            IReadOnlyList<ContentItem> firstRun = await BuildSeedAsync();

            // when
            IReadOnlyList<ContentItem> secondRun = await BuildSeedAsync();

            // then
            secondRun.Select(contentItem => contentItem.Id)
                .Should().Equal(firstRun.Select(contentItem => contentItem.Id));

            secondRun.Select(contentItem => contentItem.GroupId)
                .Should().Equal(firstRun.Select(contentItem => contentItem.GroupId));
        }

        /// <summary>
        /// Design 14.1 canonical visibility is approved AND published AND past its publish date.
        /// An approved row left unpublished is invisible to the public read, which would make the
        /// approved eighth of the matrix prove nothing about the surfaces it exists to exercise.
        /// </summary>
        [Fact]
        public async Task ShouldPublishExactlyTheApprovedContentItemsInThePast()
        {
            // when
            IReadOnlyList<ContentItem> seedContentItems = await BuildSeedAsync();

            // then
            foreach (ContentItem contentItem in seedContentItems)
            {
                bool isApproved = contentItem.ApprovalStatus == ApprovalStatus.Approved;

                contentItem.IsPublished.Should().Be(
                    isApproved,
                    because: $"a {contentItem.ApprovalStatus} row must not be published");

                if (isApproved)
                {
                    contentItem.PublishDate.Should().NotBeNull();
                    contentItem.PublishDate.Should().BeOnOrBefore(SeededWhen);
                }
                else
                {
                    contentItem.PublishDate.Should().BeNull();
                }
            }
        }

        /// <summary>
        /// A row that filled a field its content type does not render would be a row no
        /// contribution form could have produced. The seeded defaults carry HasTitle false for
        /// Quote and HasAuthor false for Testimony, Series and Topic.
        /// </summary>
        [Theory]
        [InlineData(ContentType.Quote)]
        public async Task ShouldLeaveTheTitleUnsetForATypeThatDoesNotCarryOne(ContentType contentType)
        {
            // when
            IReadOnlyList<ContentItem> seedContentItems = await BuildSeedAsync();

            // then
            seedContentItems
                .Where(contentItem => contentItem.ContentType == contentType)
                .Should().OnlyContain(contentItem => string.IsNullOrWhiteSpace(contentItem.Title));
        }

        [Theory]
        [InlineData(ContentType.Testimony)]
        [InlineData(ContentType.Series)]
        [InlineData(ContentType.Topic)]
        public async Task ShouldLeaveTheAuthorUnsetForATypeThatDoesNotCarryOne(ContentType contentType)
        {
            // when
            IReadOnlyList<ContentItem> seedContentItems = await BuildSeedAsync();

            // then
            seedContentItems
                .Where(contentItem => contentItem.ContentType == contentType)
                .Should().OnlyContain(contentItem => string.IsNullOrWhiteSpace(contentItem.Author));
        }

        [Theory]
        [InlineData(ContentType.Story)]
        [InlineData(ContentType.Devotional)]
        [InlineData(ContentType.BibleStudy)]
        [InlineData(ContentType.BlogPost)]
        public async Task ShouldGiveEveryTitledTypeATitle(ContentType contentType)
        {
            // when
            IReadOnlyList<ContentItem> seedContentItems = await BuildSeedAsync();

            // then
            seedContentItems
                .Where(contentItem => contentItem.ContentType == contentType)
                .Should().OnlyContain(contentItem =>
                    string.IsNullOrWhiteSpace(contentItem.Title) == false);
        }

        /// <summary>
        /// The duplicate rule (design 3.4.2) is scoped per (ContentType, ContentHash). Two seeded
        /// rows sharing content would collide on it, and a body reused across content types would
        /// make the surfaces impossible to tell apart on screen.
        /// </summary>
        [Fact]
        public async Task ShouldGiveEverySlotItsOwnContent()
        {
            // when
            IReadOnlyList<ContentItem> seedContentItems = await BuildSeedAsync();

            // then
            seedContentItems.Select(contentItem => contentItem.Content)
                .Should().OnlyHaveUniqueItems();

            seedContentItems.Select(contentItem => contentItem.ContentHash)
                .Should().OnlyHaveUniqueItems();
        }

        /// <summary>
        /// The hash the duplicate rule keys on is computed over NORMALIZED content — trimmed,
        /// whitespace collapsed, lowercased — exactly as ContentItemProcessingService computes it.
        /// A seeded row carrying the raw body's hash is a row the rule silently ignores.
        /// </summary>
        [Fact]
        public async Task ShouldHashTheNormalizedContentRatherThanTheRawBody()
        {
            // when
            IReadOnlyList<ContentItem> seedContentItems = await BuildSeedAsync();

            // then
            foreach (ContentItem contentItem in seedContentItems)
            {
                contentItem.ContentHash.Should().Be(contentItem.Content.ToLowerInvariant());
            }
        }

        /// <summary>
        /// RetrieveAllContentItemsAsync widens the collection read for CreatedBy == actorUserId,
        /// so the attribution is what lets the demo contributor see their own draft, submitted and
        /// rejected rows. Stamped on both halves of the audit pair because nothing has amended
        /// these rows since they were written.
        /// </summary>
        [Fact]
        public async Task ShouldAttributeEverySlotToTheContributorItWasGiven()
        {
            // when
            IReadOnlyList<ContentItem> seedContentItems = await BuildSeedAsync();

            // then
            seedContentItems.Should().OnlyContain(contentItem =>
                contentItem.CreatedBy == ContributorId
                    && contentItem.UpdatedBy == ContributorId
                    && contentItem.IsDeleted == false);
        }

        /// <summary>
        /// A feed is worth looking at only if it has an order. The slots are staggered backwards
        /// from the seed moment so an $orderby on CreatedWhen has something to sort.
        /// </summary>
        [Fact]
        public async Task ShouldStaggerTheSlotsBackwardsFromTheSeedMoment()
        {
            // when
            IReadOnlyList<ContentItem> seedContentItems = await BuildSeedAsync();

            // then
            seedContentItems.Select(contentItem => contentItem.CreatedWhen)
                .Should().BeInDescendingOrder().And.OnlyHaveUniqueItems();

            seedContentItems.Should().OnlyContain(contentItem =>
                contentItem.CreatedWhen <= SeededWhen
                    && contentItem.UpdatedWhen == contentItem.CreatedWhen);
        }

        /// <summary>
        /// SharePermission is the free-text note that explains a PermissionGranted basis, and it
        /// is meaningless under the other two. A row carrying one without the basis, or the basis
        /// without one, is a row the read surface renders as a contradiction.
        /// </summary>
        [Fact]
        public async Task ShouldCarryAPermissionNoteOnlyWhereThePermissionWasGranted()
        {
            // when
            IReadOnlyList<ContentItem> seedContentItems = await BuildSeedAsync();

            // then
            foreach (ContentItem contentItem in seedContentItems)
            {
                bool hasPermissionNote =
                    string.IsNullOrWhiteSpace(contentItem.SharePermission) == false;

                hasPermissionNote.Should().Be(
                    contentItem.ShareabilityBasis == ShareabilityBasis.PermissionGranted,
                    because:
                        $"a {contentItem.ShareabilityBasis} row has no permission to detail");
            }
        }
    }
}
