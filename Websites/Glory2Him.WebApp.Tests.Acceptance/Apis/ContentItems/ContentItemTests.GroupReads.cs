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
using RESTFulSense.Exceptions;
using CoreContentItem = Glory2Him.Core.Models.Foundations.ContentItems.ContentItem;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ContentItems
{
    /// <summary>
    /// The four reads that exist on no other exposer in this project: the public collection and
    /// the three version-group reads.
    ///
    /// <para>They need a group with more than one version to be interesting, and a group cannot
    /// be built through the API — the add derives <c>GroupId</c> and <c>Version</c>, and the
    /// transition that publishes a row arrives as an event. So the versions are arranged beneath
    /// HTTP and read back through it.</para>
    /// </summary>
    public partial class ContentItemApiTests
    {
        /// <summary>
        /// Proves the literal route wins. <c>api/ContentItems/Public</c> and
        /// <c>api/ContentItems/{contentItemId}</c> both match one segment, and attribute routing
        /// ranks a literal above a parameter — so this passing is the evidence that "Public" is
        /// not being bound as a malformed Guid.
        ///
        /// <para>It also proves the read is caller-INDEPENDENT: a submitted, unpublished version
        /// is invisible here even though the caller is an administrator who would see it through
        /// the widening collection read.</para>
        /// </summary>
        [Fact]
        public async Task ShouldServeOnlyPubliclyVisibleVersionsFromThePublicReadAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();
            Guid groupId = Guid.NewGuid();

            CoreContentItem publishedVersion = await this.apiBroker.InsertContentItemVersionAsync(
                groupId, version: 1, ApprovalStatus.Approved, isPublished: true, authorUserId);

            CoreContentItem draftVersion = await this.apiBroker.InsertContentItemVersionAsync(
                groupId, version: 2, ApprovalStatus.Submitted, isPublished: false, authorUserId);

            try
            {
                // when
                List<ContentItem> actualContentItems =
                    await this.apiBroker.GetPublicContentItemsAsync();

                // then
                actualContentItems.Should().Contain(contentItem =>
                    contentItem.Id == publishedVersion.Id);

                actualContentItems.Should().NotContain(contentItem =>
                    contentItem.Id == draftVersion.Id,
                    because: "the public read consults no security context, so an administrator "
                        + "receives exactly what an anonymous visitor would");
            }
            finally
            {
                await this.apiBroker.RemoveCoreContentItemByIdAsync(draftVersion.Id);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(publishedVersion.Id);
            }
        }

        /// <summary>
        /// The tip is the highest non-deleted <c>Version</c>, DERIVED rather than stored (§3.4.1)
        /// — which is why this test arranges the newer version second and expects it back, rather
        /// than setting a flag on it.
        ///
        /// <para>The tip may be an unapproved draft, and here it is: version 2 is Submitted while
        /// version 1 is the published row. That separation is the whole point of the two reads.</para>
        /// </summary>
        [Fact]
        public async Task ShouldGetTheGroupsLatestVersionEvenWhenItIsUnapprovedAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();
            Guid groupId = Guid.NewGuid();

            CoreContentItem publishedVersion = await this.apiBroker.InsertContentItemVersionAsync(
                groupId, version: 1, ApprovalStatus.Approved, isPublished: true, authorUserId);

            CoreContentItem latestVersion = await this.apiBroker.InsertContentItemVersionAsync(
                groupId, version: 2, ApprovalStatus.Submitted, isPublished: false, authorUserId);

            try
            {
                // when
                ContentItem actualLatest =
                    await this.apiBroker.GetLatestContentItemByGroupIdAsync(groupId);

                ContentItem actualPublished =
                    await this.apiBroker.GetPublishedContentItemByGroupIdAsync(groupId);

                // then
                actualLatest.Id.Should().Be(latestVersion.Id);
                actualLatest.Version.Should().Be(2);

                actualPublished.Id.Should().Be(publishedVersion.Id,
                    because: "a group keeps serving its published version while a newer draft is "
                        + "in review (§3.4.1)");
            }
            finally
            {
                await this.apiBroker.RemoveCoreContentItemByIdAsync(latestVersion.Id);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(publishedVersion.Id);
            }
        }

        [Fact]
        public async Task ShouldReturnNotFoundOnPublishedReadWhenTheGroupHasNoPublishedVersionAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();
            Guid groupId = Guid.NewGuid();

            CoreContentItem draftVersion = await this.apiBroker.InsertContentItemVersionAsync(
                groupId, version: 1, ApprovalStatus.Submitted, isPublished: false, authorUserId);

            try
            {
                // when
                var publishedReadTask =
                    this.apiBroker.GetPublishedContentItemByGroupIdAsync(groupId).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseNotFoundException>(() => publishedReadTask);
            }
            finally
            {
                await this.apiBroker.RemoveCoreContentItemByIdAsync(draftVersion.Id);
            }
        }

        /// <summary>
        /// Pins the VISIBLE SET of the group read, not just its contents. The route no longer
        /// hands the exposer a live queryable — the group-keyed foundation read materialises it
        /// with the caller's token — and this is the assertion that the set that reaches the wire
        /// did not move: the group's non-deleted versions, all of them, and nothing else.
        /// </summary>
        [Fact]
        public async Task ShouldServeExactlyTheGroupsNonDeletedVersionsFromTheGroupReadAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();
            Guid groupId = Guid.NewGuid();
            Guid otherGroupId = Guid.NewGuid();

            CoreContentItem publishedVersion = await this.apiBroker.InsertContentItemVersionAsync(
                groupId, version: 1, ApprovalStatus.Approved, isPublished: true, authorUserId);

            CoreContentItem draftVersion = await this.apiBroker.InsertContentItemVersionAsync(
                groupId, version: 2, ApprovalStatus.Submitted, isPublished: false, authorUserId);

            CoreContentItem deletedVersion = await this.apiBroker.InsertContentItemVersionAsync(
                groupId, version: 3, ApprovalStatus.Approved, isPublished: false, authorUserId,
                isDeleted: true);

            CoreContentItem otherGroupVersion = await this.apiBroker.InsertContentItemVersionAsync(
                otherGroupId, version: 1, ApprovalStatus.Approved, isPublished: true, authorUserId);

            try
            {
                // when
                List<ContentItem> actualContentItems =
                    await this.apiBroker.GetContentItemsByGroupIdAsync(groupId);

                // then
                actualContentItems.Select(contentItem => contentItem.Id)
                    .Should().BeEquivalentTo(new[] { publishedVersion.Id, draftVersion.Id },
                        because: "a takedown is gone for every caller and the read is keyed on "
                            + "one group, so neither the deleted version nor another group's row "
                            + "reaches the wire");
            }
            finally
            {
                await this.apiBroker.RemoveCoreContentItemByIdAsync(otherGroupVersion.Id);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(deletedVersion.Id);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(draftVersion.Id);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(publishedVersion.Id);
            }
        }

        /// <summary>
        /// Pins the RESTRICTED option set this route now advertises. Because the service hands the
        /// exposer a materialised set, <c>[EnableQuery]</c> composes over LINQ-to-Objects, and the
        /// two refused options diverge from the catalogue in DIFFERENT ways.
        ///
        /// <para><c>$filter</c> compares ordinally there, while the catalogue collates
        /// <c>SQL_Latin1_General_CP1_CI_AS</c>, so a filter that matches on the unkeyed collection
        /// read matches nothing here. <c>$orderby</c> is NOT ordinal - it follows the server's
        /// current culture through ICU - which disagrees with that collation in its own way and
        /// additionally makes the answer depend on how the HOST is configured. Both are refused
        /// outright rather than answered differently.</para>
        ///
        /// <para>The allow-list refuses more than those two: see the route's own remarks for the
        /// full set and for why <c>$select</c>, though safe on the merits, stays out.</para>
        ///
        /// <para>Both halves are asserted together on purpose: that the safe options still
        /// compose, and that the unsafe ones fail loudly. Dropping either half lets the route
        /// drift back to a silent wrong answer.</para>
        /// </summary>
        [Fact]
        public async Task ShouldComposeSafeODataOptionsAndRefuseTheAmbiguousOnesOnTheGroupReadAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();
            Guid groupId = Guid.NewGuid();

            CoreContentItem firstVersion = await this.apiBroker.InsertContentItemVersionAsync(
                groupId, version: 1, ApprovalStatus.Approved, isPublished: true, authorUserId);

            CoreContentItem secondVersion = await this.apiBroker.InsertContentItemVersionAsync(
                groupId, version: 2, ApprovalStatus.Submitted, isPublished: false, authorUserId);

            try
            {
                // when: $top composes over the materialised set
                List<ContentItem> actualContentItems =
                    await this.apiBroker.GetContentItemsByGroupIdAsync(groupId, odataQuery: "$top=1");

                var filterTask = this.apiBroker.GetContentItemsByGroupIdAsync(
                    groupId,
                    odataQuery: "$filter=contains(Title,'x')").AsTask();

                var orderByTask = this.apiBroker.GetContentItemsByGroupIdAsync(
                    groupId,
                    odataQuery: "$orderby=Title desc").AsTask();

                // then
                actualContentItems.Should().ContainSingle(
                    because: "$top carries no comparison, so it means the same in memory as it "
                        + "did in SQL and still pages this route");

                await Assert.ThrowsAsync<HttpResponseBadRequestException>(() => filterTask);
                await Assert.ThrowsAsync<HttpResponseBadRequestException>(() => orderByTask);
            }
            finally
            {
                await this.apiBroker.RemoveCoreContentItemByIdAsync(secondVersion.Id);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(firstVersion.Id);
            }
        }
    }
}
