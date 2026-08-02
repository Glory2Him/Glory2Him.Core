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
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    public partial class ContentItemServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveAllContentItemsAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            IQueryable<ContentItem> randomContentItems = CreateRandomContentItems();

            foreach (ContentItem contentItem in randomContentItems)
            {
                contentItem.IsDeleted = false;
            }

            IQueryable<ContentItem> storageContentItems = randomContentItems;
            IQueryable<ContentItem> expectedContentItems = storageContentItems;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItems);

            // when
            IQueryable<ContentItem> actualContentItems =
                await this.contentItemService.RetrieveAllContentItemsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualContentItems.Should().BeEquivalentTo(expectedContentItems);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllContentItemsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveAllOnlyPublicContentItemsWhenCallerIsAnonymousAsync()
        {
            // given
            this.ambientSecurityContext = new SecurityContext { IsAuthenticated = false };
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ContentItem publicContentItem = CreateRandomContentItem();
            publicContentItem.IsDeleted = false;
            publicContentItem.ApprovalStatus = ApprovalStatus.Approved;
            publicContentItem.IsPublished = true;
            publicContentItem.PublishDate = null;

            ContentItem pastPublishedContentItem = CreateRandomContentItem();
            pastPublishedContentItem.IsDeleted = false;
            pastPublishedContentItem.ApprovalStatus = ApprovalStatus.Approved;
            pastPublishedContentItem.IsPublished = true;
            pastPublishedContentItem.PublishDate = randomDateTimeOffset.AddDays(GetRandomNegativeNumber());

            ContentItem draftContentItem = CreateRandomContentItem();
            draftContentItem.IsDeleted = false;
            draftContentItem.ApprovalStatus = ApprovalStatus.Draft;
            draftContentItem.IsPublished = false;

            ContentItem futurePublishedContentItem = CreateRandomContentItem();
            futurePublishedContentItem.IsDeleted = false;
            futurePublishedContentItem.ApprovalStatus = ApprovalStatus.Approved;
            futurePublishedContentItem.IsPublished = true;
            futurePublishedContentItem.PublishDate = randomDateTimeOffset.AddDays(GetRandomNumber());

            ContentItem deletedContentItem = CreateRandomContentItem();
            deletedContentItem.IsDeleted = true;
            deletedContentItem.ApprovalStatus = ApprovalStatus.Approved;
            deletedContentItem.IsPublished = true;
            deletedContentItem.PublishDate = null;

            IQueryable<ContentItem> storageContentItems = new List<ContentItem>
            {
                publicContentItem,
                pastPublishedContentItem,
                draftContentItem,
                futurePublishedContentItem,
                deletedContentItem
            }.AsQueryable();

            IQueryable<ContentItem> expectedContentItems = new List<ContentItem>
            {
                publicContentItem,
                pastPublishedContentItem
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItems);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            IQueryable<ContentItem> actualContentItems =
                await this.contentItemService.RetrieveAllContentItemsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualContentItems.Should().BeEquivalentTo(expectedContentItems);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllContentItemsAsync(It.IsAny<CancellationToken>()),
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
        public async Task ShouldRetrieveAllPublicAndOwnContentItemsWhenUserHasNoReviewRoleAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            string randomActorUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ContentItem publicContentItem = CreateRandomContentItem();
            publicContentItem.IsDeleted = false;
            publicContentItem.ApprovalStatus = ApprovalStatus.Approved;
            publicContentItem.IsPublished = true;
            publicContentItem.PublishDate = null;

            ContentItem ownDraftContentItem = CreateRandomContentItem();
            ownDraftContentItem.IsDeleted = false;
            ownDraftContentItem.ApprovalStatus = ApprovalStatus.Draft;
            ownDraftContentItem.IsPublished = false;
            ownDraftContentItem.CreatedBy = randomActorUserId;

            ContentItem othersDraftContentItem = CreateRandomContentItem();
            othersDraftContentItem.IsDeleted = false;
            othersDraftContentItem.ApprovalStatus = ApprovalStatus.Draft;
            othersDraftContentItem.IsPublished = false;

            ContentItem ownDeletedContentItem = CreateRandomContentItem();
            ownDeletedContentItem.IsDeleted = true;
            ownDeletedContentItem.CreatedBy = randomActorUserId;

            IQueryable<ContentItem> storageContentItems = new List<ContentItem>
            {
                publicContentItem,
                ownDraftContentItem,
                othersDraftContentItem,
                ownDeletedContentItem
            }.AsQueryable();

            IQueryable<ContentItem> expectedContentItems = new List<ContentItem>
            {
                publicContentItem,
                ownDraftContentItem
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItems);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            IQueryable<ContentItem> actualContentItems =
                await this.contentItemService.RetrieveAllContentItemsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualContentItems.Should().BeEquivalentTo(expectedContentItems);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllContentItemsAsync(It.IsAny<CancellationToken>()),
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
        public async Task ShouldRetrieveAllNonDeletedContentItemsWhenUserHasReviewRoleAsync(
            string reviewRole)
        {
            // given: a review-role caller sees every non-deleted row — no clock, no
            // user-id resolution
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(reviewRole);

            ContentItem publicContentItem = CreateRandomContentItem();
            publicContentItem.IsDeleted = false;
            publicContentItem.ApprovalStatus = ApprovalStatus.Approved;
            publicContentItem.IsPublished = true;
            publicContentItem.PublishDate = null;

            ContentItem draftContentItem = CreateRandomContentItem();
            draftContentItem.IsDeleted = false;
            draftContentItem.ApprovalStatus = ApprovalStatus.Draft;
            draftContentItem.IsPublished = false;

            ContentItem futurePublishedContentItem = CreateRandomContentItem();
            futurePublishedContentItem.IsDeleted = false;
            futurePublishedContentItem.ApprovalStatus = ApprovalStatus.Approved;
            futurePublishedContentItem.IsPublished = true;
            futurePublishedContentItem.PublishDate = GetRandomDateTimeOffset().AddDays(GetRandomNumber());

            ContentItem deletedContentItem = CreateRandomContentItem();
            deletedContentItem.IsDeleted = true;

            IQueryable<ContentItem> storageContentItems = new List<ContentItem>
            {
                publicContentItem,
                draftContentItem,
                futurePublishedContentItem,
                deletedContentItem
            }.AsQueryable();

            IQueryable<ContentItem> expectedContentItems = new List<ContentItem>
            {
                publicContentItem,
                draftContentItem,
                futurePublishedContentItem
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItems);

            // when
            IQueryable<ContentItem> actualContentItems =
                await this.contentItemService.RetrieveAllContentItemsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualContentItems.Should().BeEquivalentTo(expectedContentItems);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllContentItemsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
