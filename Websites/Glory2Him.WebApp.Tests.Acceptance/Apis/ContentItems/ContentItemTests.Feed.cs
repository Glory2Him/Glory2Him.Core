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
