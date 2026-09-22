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
using System.Data.SqlTypes;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Securities;
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
        /// Criterion 4: PAGING THE FEED NEITHER SKIPS NOR REPEATS A ROW, and the two pages
        /// concatenated are the head of §DOM11.3's sequence — ASSERTED AS A SEQUENCE, not as a
        /// set. A disjointness-only test passes under an ordering that has been re-keyed behind
        /// the read's back, which is precisely what criterion 6's mutation check produces.
        ///
        /// <para>Two rows share an effective publication moment TO THE TICK and they straddle
        /// the page boundary, which is the case the <c>Id</c> terminator exists for: with no
        /// total order, <c>OFFSET</c>/<c>FETCH</c> may place either of them in either page and a
        /// row can be served twice or not at all.</para>
        ///
        /// <para>The expected sequence is read back from the feed's own unpaged answer rather
        /// than computed here. <c>Id DESC</c> breaks the tie in SQL Server's ordering of
        /// <c>uniqueidentifier</c>, which is not .NET's, so a hand-computed expectation would be
        /// asserting this test's opinion of that collation rather than the read's behaviour.</para>
        /// </summary>
        [Fact]
        public async Task ShouldPageTheFeedWithoutSkippingOrRepeatingARowAsync()
        {
            // given
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset sharedMoment = now.AddMinutes(-3);
            var arrangedContentItems = new List<CoreContentItem>();

            try
            {
                // Positions 0 and 1 of the order.
                arrangedContentItems.Add(await this.apiBroker.InsertFeedContentItemAsync(
                    createdWhen: now.AddMinutes(-1), publishDate: now.AddMinutes(-1)));

                arrangedContentItems.Add(await this.apiBroker.InsertFeedContentItemAsync(
                    createdWhen: now.AddMinutes(-2), publishDate: now.AddMinutes(-2)));

                // Positions 2 and 3 - the tie, and it lands either side of the boundary
                // between a first page of three and a second page of three.
                //
                // THE LOWER ID IS INSERTED FIRST, deliberately: Id DESC must then serve the
                // HIGHER one first, which is the opposite of the order the rows were written
                // in. Without that the assertion below would be satisfied by a read carrying
                // no terminator at all, whose tie order is whatever the plan happened to
                // produce. "Lower" is SQL Server's ordering of uniqueidentifier, which is not
                // .NET's - SqlGuid is the comparison the database will apply.
                (Guid lowerTiedId, Guid higherTiedId) = OrderIdsAsSqlServerWould(
                    Guid.NewGuid(), Guid.NewGuid());

                CoreContentItem lowerTiedContentItem =
                    await this.apiBroker.InsertFeedContentItemAsync(
                        createdWhen: sharedMoment,
                        publishDate: sharedMoment,
                        contentItemId: lowerTiedId);

                CoreContentItem higherTiedContentItem =
                    await this.apiBroker.InsertFeedContentItemAsync(
                        createdWhen: sharedMoment,
                        publishDate: sharedMoment,
                        contentItemId: higherTiedId);

                arrangedContentItems.Add(lowerTiedContentItem);
                arrangedContentItems.Add(higherTiedContentItem);

                // Positions 4 and 5.
                arrangedContentItems.Add(await this.apiBroker.InsertFeedContentItemAsync(
                    createdWhen: now.AddMinutes(-4), publishDate: now.AddMinutes(-4)));

                arrangedContentItems.Add(await this.apiBroker.InsertFeedContentItemAsync(
                    createdWhen: now.AddMinutes(-5), publishDate: now.AddMinutes(-5)));


                // when
                List<ContentItem> wholeSequence =
                    await this.apiBroker.GetContentItemFeedAsync(skip: 0, take: 6);

                List<ContentItem> firstPage =
                    await this.apiBroker.GetContentItemFeedAsync(skip: 0, take: 3);

                List<ContentItem> secondPage =
                    await this.apiBroker.GetContentItemFeedAsync(skip: 3, take: 3);

                // then
                List<Guid> pagedIds = firstPage.Concat(secondPage)
                    .Select(contentItem => contentItem.Id)
                    .ToList();

                // No row twice, and no row missing.
                pagedIds.Should().OnlyHaveUniqueItems();

                pagedIds.Should().BeEquivalentTo(
                    arrangedContentItems.Select(contentItem => contentItem.Id));

                // THE SEQUENCE, not the set: the two pages concatenated are the head of the
                // order, in the order.
                pagedIds.Should().Equal(
                    wholeSequence.Select(contentItem => contentItem.Id));

                // THE TERMINATOR ITSELF. The tie straddles the boundary - so the assertions
                // above were exercising the case they were arranged for - and it is broken by
                // Id DESCENDING, the higher id first, which is the reverse of the order the two
                // rows were written in.
                firstPage.Last().Id.Should().Be(higherTiedId);
                secondPage.First().Id.Should().Be(lowerTiedId);
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
        /// The two ids as SQL SERVER would order them, ascending. <c>uniqueidentifier</c> is
        /// compared on its last six bytes first and .NET's <c>Guid.CompareTo</c> is not that
        /// comparison, so a test reasoning about <c>ORDER BY Id</c> has to use the database's
        /// own semantics — which is what <see cref="SqlGuid"/> implements.
        /// </summary>
        private static (Guid Lower, Guid Higher) OrderIdsAsSqlServerWould(
            Guid firstId,
            Guid secondId) =>
            new SqlGuid(firstId).CompareTo(new SqlGuid(secondId)) < 0
                ? (firstId, secondId)
                : (secondId, firstId);

        /// <summary>
        /// Criterion 5: THE SAME FEED TO EVERY CALLER. Read anonymously, as the draft's own
        /// contributor, as a narrow reviewer, as a publisher and as an administrator, the
        /// answer is the identical set in the identical order.
        ///
        /// <para>The contributor's own draft is the load-bearing row: <c>GET api/ContentItems</c>
        /// deliberately DOES show it to them, so a feed wired to that caller-widening read would
        /// serve it here and every other assertion in this file would still pass. Nobody is
        /// refused and nobody is told a row was withheld — the row is simply not in the
        /// projection (§SEC14.5 rule 4).</para>
        /// </summary>
        [Fact]
        public async Task ShouldServeTheSameFeedToEveryCallerRegardlessOfRoleAsync()
        {
            // given
            DateTimeOffset now = DateTimeOffset.UtcNow;
            string contributorUserId = Guid.NewGuid().ToString();
            var arrangedContentItems = new List<CoreContentItem>();

            try
            {
                for (int index = 0; index < 3; index++)
                {
                    arrangedContentItems.Add(
                        await this.apiBroker.InsertFeedContentItemAsync(
                            createdWhen: now.AddMinutes(-index),
                            publishDate: now.AddMinutes(-index)));
                }

                CoreContentItem ownDraftContentItem =
                    await this.apiBroker.InsertFeedContentItemAsync(
                        createdWhen: now,
                        publishDate: null,
                        approvalStatus: ApprovalStatus.Draft,
                        isPublished: false,
                        authorUserId: contributorUserId);

                arrangedContentItems.Add(ownDraftContentItem);

                var expectedFeedIds = arrangedContentItems
                    .Where(contentItem => contentItem.Id != ownDraftContentItem.Id)
                    .Select(contentItem => contentItem.Id)
                    .ToList();

                // when
                this.apiBroker.ActAsAnonymous();

                List<ContentItem> anonymousFeed =
                    await this.apiBroker.GetContentItemFeedAsync(skip: 0, take: 10);

                this.apiBroker.ActAs(contributorUserId);

                List<ContentItem> contributorFeed =
                    await this.apiBroker.GetContentItemFeedAsync(skip: 0, take: 10);

                this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.ContentItemReviewers);

                List<ContentItem> narrowReviewerFeed =
                    await this.apiBroker.GetContentItemFeedAsync(skip: 0, take: 10);

                this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.Publishers);

                List<ContentItem> publisherFeed =
                    await this.apiBroker.GetContentItemFeedAsync(skip: 0, take: 10);

                this.apiBroker.ActAs(Guid.NewGuid().ToString(), Roles.Administrators);

                List<ContentItem> administratorFeed =
                    await this.apiBroker.GetContentItemFeedAsync(skip: 0, take: 10);

                // then
                anonymousFeed.Select(contentItem => contentItem.Id)
                    .Should().Equal(expectedFeedIds);

                // IDENTICAL, in order - not merely "also excludes the draft".
                contributorFeed.Select(contentItem => contentItem.Id)
                    .Should().Equal(anonymousFeed.Select(contentItem => contentItem.Id));

                narrowReviewerFeed.Select(contentItem => contentItem.Id)
                    .Should().Equal(anonymousFeed.Select(contentItem => contentItem.Id));

                publisherFeed.Select(contentItem => contentItem.Id)
                    .Should().Equal(anonymousFeed.Select(contentItem => contentItem.Id));

                administratorFeed.Select(contentItem => contentItem.Id)
                    .Should().Equal(anonymousFeed.Select(contentItem => contentItem.Id));
            }
            finally
            {
                this.apiBroker.ActAsSeededAdministrator();

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
