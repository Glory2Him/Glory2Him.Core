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
using Glory2Him.Core.Models.Enums;
using Glory2Him.WebApp.Tests.Acceptance.Models.ContentItems;
using CoreContentItem = Glory2Him.Core.Models.Foundations.ContentItems.ContentItem;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ContentItems
{
    /// <summary>
    /// THE FEED, exercised through the route that serves it. These assertions sit at acceptance
    /// level rather than lower because the predicate, the order and the page are all composed in
    /// the STORAGE BROKER and awaited there — a unit test mocking the layer below would be
    /// asserting its own stub back.
    ///
    /// <para>Every fixture here pins its own dates rather than drawing them. The feed orders on
    /// <c>COALESCE(PublishDate, CreatedWhen) DESC</c>, so a fixture built from a random draw
    /// lands wherever the draw put it — and this repository's filler draws from year 0001, which
    /// is the bottom of a descending order rather than the head these tests assert on.</para>
    /// </summary>
    public partial class ContentItemApiTests
    {
        /// <summary>
        /// Criterion 1, first half: NOTHING OUTSIDE §SEC14.1 IS SERVED, ON ANY PAGE. Asserted on
        /// a first page AND on a page taken at a non-zero skip, because a predicate composed
        /// AFTER Skip rather than before it serves a hidden row on page two while page one looks
        /// clean, and a first-page-only assertion passes it.
        /// </summary>
        [Fact]
        public async Task ShouldServeOnlyCanonicallyVisibleContentItemsFromTheFeedAsync()
        {
            // given
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var arrangedContentItems = new List<CoreContentItem>();

            try
            {
                List<CoreContentItem> visibleContentItems = new List<CoreContentItem>();

                for (int index = 0; index < 6; index++)
                {
                    visibleContentItems.Add(
                        await this.apiBroker.InsertFeedContentItemAsync(
                            createdWhen: now.AddMinutes(-index),
                            publishDate: now.AddMinutes(-index)));
                }

                arrangedContentItems.AddRange(visibleContentItems);

                // One row failing each of §SEC14.1's four terms, and nothing else about them
                // differs: each is otherwise as visible as the six above.
                CoreContentItem deletedContentItem =
                    await this.apiBroker.InsertFeedContentItemAsync(
                        createdWhen: now,
                        publishDate: now,
                        isDeleted: true);

                CoreContentItem unapprovedContentItem =
                    await this.apiBroker.InsertFeedContentItemAsync(
                        createdWhen: now,
                        publishDate: now,
                        approvalStatus: ApprovalStatus.Submitted);

                CoreContentItem unpublishedContentItem =
                    await this.apiBroker.InsertFeedContentItemAsync(
                        createdWhen: now,
                        publishDate: null,
                        isPublished: false);

                CoreContentItem futureContentItem =
                    await this.apiBroker.InsertFeedContentItemAsync(
                        createdWhen: now,
                        publishDate: now.AddDays(1));

                var hiddenContentItemIds = new List<Guid>
                {
                    deletedContentItem.Id,
                    unapprovedContentItem.Id,
                    unpublishedContentItem.Id,
                    futureContentItem.Id
                };

                arrangedContentItems.Add(deletedContentItem);
                arrangedContentItems.Add(unapprovedContentItem);
                arrangedContentItems.Add(unpublishedContentItem);
                arrangedContentItems.Add(futureContentItem);

                // when
                List<ContentItem> firstFeedPage =
                    await this.apiBroker.GetContentItemFeedAsync(skip: 0, take: 3);

                List<ContentItem> laterFeedPage =
                    await this.apiBroker.GetContentItemFeedAsync(skip: 3, take: 3);

                // then
                firstFeedPage.Select(contentItem => contentItem.Id)
                    .Should().NotIntersectWith(hiddenContentItemIds);

                laterFeedPage.Select(contentItem => contentItem.Id)
                    .Should().NotIntersectWith(hiddenContentItemIds);

                // Without this the non-zero-skip arm could pass on an empty page and prove
                // nothing at all.
                firstFeedPage.Concat(laterFeedPage).Select(contentItem => contentItem.Id)
                    .Should().Contain(
                        visibleContentItems.Select(contentItem => contentItem.Id));
            }
            finally
            {
                foreach (CoreContentItem arrangedContentItem in arrangedContentItems)
                {
                    await this.apiBroker.RemoveCoreContentItemByIdAsync(arrangedContentItem.Id);
                }
            }
        }

        /// <summary>
        /// Criterion 2: A TOPIC OR A SERIES NEVER APPEARS IN THE FEED, AND IS NOT THEREBY MADE
        /// UNREACHABLE. §DOM3.8 rule 2 excludes both from THIS read and from this read only, so
        /// the fixture asserts the other half too — the excluded rows still answer on
        /// <c>GET api/ContentItems/Public</c>. A test asserting only absence could not tell
        /// "filtered out of one projection" from "not there at all", and the second is the
        /// failure that would make every topic unsearchable.
        ///
        /// <para>The non-zero-skip arm is here for criterion 1's reason: an exclusion composed
        /// AFTER Skip serves a topic on page two while page one looks clean.</para>
        /// </summary>
        [Fact]
        public async Task ShouldExcludeTopicAndSeriesContentItemsFromTheFeedAsync()
        {
            // given
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var arrangedContentItems = new List<CoreContentItem>();

            try
            {
                for (int index = 0; index < 4; index++)
                {
                    arrangedContentItems.Add(
                        await this.apiBroker.InsertFeedContentItemAsync(
                            createdWhen: now.AddMinutes(-index),
                            publishDate: now.AddMinutes(-index)));
                }

                // Canonically visible in every respect — only the content type keeps them out.
                CoreContentItem topicContentItem =
                    await this.apiBroker.InsertFeedContentItemAsync(
                        createdWhen: now,
                        publishDate: now,
                        contentType: ContentType.Topic);

                CoreContentItem seriesContentItem =
                    await this.apiBroker.InsertFeedContentItemAsync(
                        createdWhen: now,
                        publishDate: now,
                        contentType: ContentType.Series);

                arrangedContentItems.Add(topicContentItem);
                arrangedContentItems.Add(seriesContentItem);

                var excludedContentItemIds = new List<Guid>
                {
                    topicContentItem.Id,
                    seriesContentItem.Id
                };

                // when
                List<ContentItem> firstFeedPage =
                    await this.apiBroker.GetContentItemFeedAsync(skip: 0, take: 2);

                List<ContentItem> laterFeedPage =
                    await this.apiBroker.GetContentItemFeedAsync(skip: 2, take: 4);

                List<ContentItem> publicContentItems =
                    await this.apiBroker.GetPublicContentItemsAsync();

                // then
                firstFeedPage.Select(contentItem => contentItem.Id)
                    .Should().NotIntersectWith(excludedContentItemIds);

                laterFeedPage.Select(contentItem => contentItem.Id)
                    .Should().NotIntersectWith(excludedContentItemIds);

                // FILTERED OUT OF ONE PROJECTION, not gone: the same rows keep answering on the
                // public read, which is where a reader narrowing by category finds them.
                publicContentItems.Select(contentItem => contentItem.Id)
                    .Should().Contain(excludedContentItemIds);
            }
            finally
            {
                foreach (CoreContentItem arrangedContentItem in arrangedContentItems)
                {
                    await this.apiBroker.RemoveCoreContentItemByIdAsync(arrangedContentItem.Id);
                }
            }
        }

        /// <summary>
        /// Criterion 3: ORDERED BY EFFECTIVE PUBLICATION MOMENT DESCENDING —
        /// <c>COALESCE(PublishDate, CreatedWhen) DESC</c> (§DOM11.3).
        ///
        /// <para>The fixture distinguishes that order from BOTH orders it replaces, which is
        /// what makes it worth running. The undated row's <c>CreatedWhen</c> falls BETWEEN the
        /// two dated rows' publish moments, and the two dated rows were written in the opposite
        /// order to the one they publish in. So <c>CreatedWhen DESC</c> answers
        /// oldest-published-first, and <c>PublishDate DESC, CreatedWhen DESC</c> sinks the
        /// undated row to the bottom (SQL Server sorts NULL last under DESC). Only §DOM11.3's
        /// order interleaves it.</para>
        /// </summary>
        [Fact]
        public async Task ShouldOrderTheFeedByEffectivePublicationMomentDescendingAsync()
        {
            // given
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var arrangedContentItems = new List<CoreContentItem>();

            try
            {
                // Published an hour ago, WRITTEN ten hours ago - the newest by effective
                // moment and the oldest by CreatedWhen.
                CoreContentItem recentlyPublishedContentItem =
                    await this.apiBroker.InsertFeedContentItemAsync(
                        createdWhen: now.AddHours(-10),
                        publishDate: now.AddHours(-1));

                // NO PUBLISH DATE, so its created moment is its effective one - and that
                // moment sits between the two dated rows.
                CoreContentItem undatedContentItem =
                    await this.apiBroker.InsertFeedContentItemAsync(
                        createdWhen: now.AddHours(-2),
                        publishDate: null);

                // Published three hours ago, WRITTEN half an hour ago - the oldest by
                // effective moment and the newest by CreatedWhen.
                CoreContentItem earlierPublishedContentItem =
                    await this.apiBroker.InsertFeedContentItemAsync(
                        createdWhen: now.AddMinutes(-30),
                        publishDate: now.AddHours(-3));

                arrangedContentItems.Add(recentlyPublishedContentItem);
                arrangedContentItems.Add(undatedContentItem);
                arrangedContentItems.Add(earlierPublishedContentItem);

                // when
                List<ContentItem> feedPage =
                    await this.apiBroker.GetContentItemFeedAsync(skip: 0, take: 3);

                // then
                feedPage.Select(contentItem => contentItem.Id)
                    .Should().ContainInOrder(
                        recentlyPublishedContentItem.Id,
                        undatedContentItem.Id,
                        earlierPublishedContentItem.Id);
            }
            finally
            {
                foreach (CoreContentItem arrangedContentItem in arrangedContentItems)
                {
                    await this.apiBroker.RemoveCoreContentItemByIdAsync(arrangedContentItem.Id);
                }
            }
        }

        /// <summary>
        /// Criterion 1, second half: NOTHING INSIDE §SEC14.1 IS MISSING FROM THE HEAD. Stated
        /// over the page rather than over the catalogue, because the read is capped at 50 and
        /// no answer can carry a larger visible catalogue than that. This fixture states its own
        /// size — five rows — which is inside the cap, and pins its own dates so those five ARE
        /// the head of the order.
        /// </summary>
        [Fact]
        public async Task ShouldServeEveryCanonicallyVisibleContentItemOnTheFeedsFirstPageAsync()
        {
            // given
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var arrangedContentItems = new List<CoreContentItem>();

            try
            {
                for (int index = 0; index < 5; index++)
                {
                    arrangedContentItems.Add(
                        await this.apiBroker.InsertFeedContentItemAsync(
                            createdWhen: now.AddMinutes(-index),
                            publishDate: now.AddMinutes(-index)));
                }

                // when
                List<ContentItem> firstFeedPage =
                    await this.apiBroker.GetContentItemFeedAsync(skip: 0, take: 10);

                // then
                firstFeedPage.Select(contentItem => contentItem.Id)
                    .Should().BeEquivalentTo(
                        arrangedContentItems.Select(contentItem => contentItem.Id));
            }
            finally
            {
                foreach (CoreContentItem arrangedContentItem in arrangedContentItems)
                {
                    await this.apiBroker.RemoveCoreContentItemByIdAsync(arrangedContentItem.Id);
                }
            }
        }
    }
}
