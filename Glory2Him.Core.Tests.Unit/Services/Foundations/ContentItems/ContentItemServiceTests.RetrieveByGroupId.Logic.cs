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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    public partial class ContentItemServiceTests
    {
        // WHAT THESE TESTS CAN AND CANNOT SAY. The group narrowing is a STORAGE predicate —
        // SelectContentItemsByGroupIdAsync carries it, with the token — so the stubs below hand
        // back a group's rows without re-implementing "GroupId == groupId". A stub that
        // re-implemented it would pass whether or not the real read still carried it; that the
        // read is keyed on the group at all is proved against a real catalogue in
        // ContentItemNarrowReadTests.
        //
        // What is proved here is the half the SERVICE owns: the group it asks storage for is the
        // one the caller named, and the §14.7 visibility filter runs over what comes back. That
        // rule moved down from ContentItemProcessingService when the processing group read
        // stopped filtering a second time — the filter has one home, so it is tested at it.
        [Fact]
        public async Task ShouldRetrieveOnlyPublicGroupContentItemsWhenCallerIsAnonymousAsync()
        {
            // given
            Guid inputGroupId = Guid.NewGuid();
            this.ambientSecurityContext = new SecurityContext { IsAuthenticated = false };
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ContentItem publicContentItem = CreateRandomGroupContentItem(inputGroupId);
            publicContentItem.ApprovalStatus = ApprovalStatus.Approved;
            publicContentItem.IsPublished = true;
            publicContentItem.PublishDate = null;

            ContentItem pastPublishedContentItem = CreateRandomGroupContentItem(inputGroupId);
            pastPublishedContentItem.ApprovalStatus = ApprovalStatus.Approved;
            pastPublishedContentItem.IsPublished = true;
            pastPublishedContentItem.PublishDate = randomDateTimeOffset.AddDays(GetRandomNegativeNumber());

            ContentItem draftContentItem = CreateRandomGroupContentItem(inputGroupId);
            draftContentItem.ApprovalStatus = ApprovalStatus.Draft;
            draftContentItem.IsPublished = false;

            ContentItem futurePublishedContentItem = CreateRandomGroupContentItem(inputGroupId);
            futurePublishedContentItem.ApprovalStatus = ApprovalStatus.Approved;
            futurePublishedContentItem.IsPublished = true;
            futurePublishedContentItem.PublishDate = randomDateTimeOffset.AddDays(GetRandomNumber());

            ContentItem deletedContentItem = CreateRandomGroupContentItem(inputGroupId);
            deletedContentItem.IsDeleted = true;
            deletedContentItem.ApprovalStatus = ApprovalStatus.Approved;
            deletedContentItem.IsPublished = true;
            deletedContentItem.PublishDate = null;

            var storageContentItems = new List<ContentItem>
            {
                publicContentItem,
                pastPublishedContentItem,
                draftContentItem,
                futurePublishedContentItem,
                deletedContentItem
            };

            var expectedContentItems = new List<ContentItem>
            {
                publicContentItem,
                pastPublishedContentItem
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemsByGroupIdAsync(inputGroupId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItems);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            IReadOnlyList<ContentItem> actualContentItems =
                await this.contentItemService.RetrieveContentItemsByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItems.Should().BeEquivalentTo(expectedContentItems);

            // the group the caller named, unaltered — the one thing about the narrowing this
            // seam can still prove
            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemsByGroupIdAsync(inputGroupId, It.IsAny<CancellationToken>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrievePublicAndOwnGroupContentItemsWhenUserHasNoReviewRoleAsync()
        {
            // given: the owner follows their own versions of the group through the workflow,
            // while another caller's non-public version of the same group stays invisible
            Guid inputGroupId = Guid.NewGuid();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string actorUserId = GetRandomString();

            ContentItem publicContentItem = CreateRandomGroupContentItem(inputGroupId);
            publicContentItem.ApprovalStatus = ApprovalStatus.Approved;
            publicContentItem.IsPublished = true;
            publicContentItem.PublishDate = null;

            ContentItem ownDraftContentItem = CreateRandomGroupContentItem(inputGroupId);
            ownDraftContentItem.ApprovalStatus = ApprovalStatus.Draft;
            ownDraftContentItem.IsPublished = false;
            ownDraftContentItem.CreatedBy = actorUserId;

            ContentItem otherDraftContentItem = CreateRandomGroupContentItem(inputGroupId);
            otherDraftContentItem.ApprovalStatus = ApprovalStatus.Draft;
            otherDraftContentItem.IsPublished = false;

            ContentItem ownDeletedContentItem = CreateRandomGroupContentItem(inputGroupId);
            ownDeletedContentItem.IsDeleted = true;
            ownDeletedContentItem.CreatedBy = actorUserId;

            var storageContentItems = new List<ContentItem>
            {
                publicContentItem,
                ownDraftContentItem,
                otherDraftContentItem,
                ownDeletedContentItem
            };

            var expectedContentItems = new List<ContentItem>
            {
                publicContentItem,
                ownDraftContentItem
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemsByGroupIdAsync(inputGroupId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItems);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(actorUserId);

            // when
            IReadOnlyList<ContentItem> actualContentItems =
                await this.contentItemService.RetrieveContentItemsByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItems.Should().BeEquivalentTo(expectedContentItems);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemsByGroupIdAsync(inputGroupId, It.IsAny<CancellationToken>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ReviewRoles))]
        public async Task ShouldRetrieveAllNonDeletedGroupContentItemsWhenUserHasReviewRoleAsync(
            string reviewRole)
        {
            // given: a review-role caller audits every non-deleted version of the group —
            // drafts and future-scheduled rows included — with neither the clock nor the
            // caller's identity consulted
            Guid inputGroupId = Guid.NewGuid();
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);

            ContentItem publicContentItem = CreateRandomGroupContentItem(inputGroupId);
            publicContentItem.ApprovalStatus = ApprovalStatus.Approved;
            publicContentItem.IsPublished = true;
            publicContentItem.PublishDate = null;

            ContentItem draftContentItem = CreateRandomGroupContentItem(inputGroupId);
            draftContentItem.ApprovalStatus = ApprovalStatus.Draft;
            draftContentItem.IsPublished = false;

            ContentItem futurePublishedContentItem = CreateRandomGroupContentItem(inputGroupId);
            futurePublishedContentItem.ApprovalStatus = ApprovalStatus.Approved;
            futurePublishedContentItem.IsPublished = true;
            futurePublishedContentItem.PublishDate = GetRandomDateTimeOffset().AddDays(GetRandomNumber());

            ContentItem deletedContentItem = CreateRandomGroupContentItem(inputGroupId);
            deletedContentItem.IsDeleted = true;

            var storageContentItems = new List<ContentItem>
            {
                publicContentItem,
                draftContentItem,
                futurePublishedContentItem,
                deletedContentItem
            };

            var expectedContentItems = new List<ContentItem>
            {
                publicContentItem,
                draftContentItem,
                futurePublishedContentItem
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemsByGroupIdAsync(inputGroupId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItems);

            // when
            IReadOnlyList<ContentItem> actualContentItems =
                await this.contentItemService.RetrieveContentItemsByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItems.Should().BeEquivalentTo(expectedContentItems);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemsByGroupIdAsync(inputGroupId, It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // Every row a group read hands back belongs to the group, and IsDeleted is pinned rather
        // than drawn — a posture-sensitive test must never depend on the draw.
        private static ContentItem CreateRandomGroupContentItem(Guid groupId)
        {
            ContentItem contentItem = CreateRandomContentItem();
            contentItem.GroupId = groupId;
            contentItem.IsDeleted = false;

            return contentItem;
        }
        /// <summary>
        /// THE LINEAGE'S OWN ORDER. This is the half of the paging contract that lives down here:
        /// the exposer turns OData's <c>EnsureStableOrdering</c> off precisely so this ordering
        /// survives, and with it off nothing else supplies one - the storage read carries no
        /// ORDER BY, so without this the route would page in whatever order SQL happened to
        /// return.
        ///
        /// <para>Seeded deliberately OUT of order, and out of Id order too, so a read that
        /// forwarded the storage order or leaned on the entity key would fail rather than pass by
        /// coincidence.</para>
        /// </summary>
        [Theory]
        [MemberData(nameof(ReviewRoles))]
        public async Task ShouldOrderGroupMembersByVersionOnRetrieveByGroupIdAsync(
            string reviewRole)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);
            Guid inputGroupId = Guid.NewGuid();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            List<ContentItem> storageContentItems = new[] { 3, 1, 2 }
                .Select(version =>
                {
                    ContentItem contentItem =
                        CreateContentItemFiller(randomDateTimeOffset).Create();

                    contentItem.GroupId = inputGroupId;
                    contentItem.Version = version;
                    contentItem.IsDeleted = false;

                    return contentItem;
                })
                .ToList();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemsByGroupIdAsync(
                    inputGroupId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItems);

            // when
            IReadOnlyList<ContentItem> actualContentItems =
                await this.contentItemService.RetrieveContentItemsByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItems.Select(contentItem => contentItem.Version)
                .Should().ContainInOrder(1, 2, 3);
        }

    }
}
