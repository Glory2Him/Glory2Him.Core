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

        [Fact]
        public async Task ShouldGetEveryVersionOfAGroupAsync()
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
                // when
                List<ContentItem> actualContentItems =
                    await this.apiBroker.GetContentItemsByGroupIdAsync(groupId);

                // then
                actualContentItems.Select(contentItem => contentItem.Id)
                    .Should().Contain(new[] { firstVersion.Id, secondVersion.Id });
            }
            finally
            {
                await this.apiBroker.RemoveCoreContentItemByIdAsync(secondVersion.Id);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(firstVersion.Id);
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
    }
}
