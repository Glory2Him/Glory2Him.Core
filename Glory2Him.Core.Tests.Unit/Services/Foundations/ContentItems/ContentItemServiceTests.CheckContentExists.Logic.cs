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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    public partial class ContentItemServiceTests
    {
        [Fact]
        public async Task ShouldReturnTrueOnCheckContentExistsWhenMatchingContentItemExistsInAnotherGroupAsync()
        {
            // given: the probe is deliberately unfiltered by visibility — another user's
            // non-public draft still counts as a duplicate
            ContentType contentType = ContentType.Quote;
            string contentHash = GetRandomString();
            Guid excludedContentItemGroupId = Guid.NewGuid();

            ContentItem matchingContentItem = CreateRandomContentItem();
            matchingContentItem.ContentType = contentType;
            matchingContentItem.ContentHash = contentHash;
            matchingContentItem.ContentItemGroupId = Guid.NewGuid();
            matchingContentItem.IsDeleted = false;
            matchingContentItem.ApprovalStatus = ApprovalStatus.Draft;
            matchingContentItem.IsPublished = false;

            ContentItem differentHashContentItem = CreateRandomContentItem();
            differentHashContentItem.ContentType = contentType;
            differentHashContentItem.ContentHash = GetRandomString();
            differentHashContentItem.IsDeleted = false;

            ContentItem differentTypeContentItem = CreateRandomContentItem();
            differentTypeContentItem.ContentType = ContentType.Story;
            differentTypeContentItem.ContentHash = contentHash;
            differentTypeContentItem.IsDeleted = false;

            IQueryable<ContentItem> storageContentItems = new List<ContentItem>
            {
                matchingContentItem,
                differentHashContentItem,
                differentTypeContentItem
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItems);

            // when
            bool actualResult =
                await this.contentItemService.CheckContentItemContentExistsAsync(
                    contentType,
                    contentHash,
                    excludedContentItemGroupId,
                    TestContext.Current.CancellationToken);

            // then
            actualResult.Should().BeTrue();

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.Is(SameCheckRequestAs(contentType, contentHash))),
                Times.Once);

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
        public async Task ShouldReturnFalseOnCheckContentExistsWhenOnlyMatchIsInExcludedGroupAsync()
        {
            // given: the caller's own group is excluded — a later version reverting to
            // earlier wording of the same group is not a duplicate
            ContentType contentType = ContentType.Quote;
            string contentHash = GetRandomString();
            Guid excludedContentItemGroupId = Guid.NewGuid();

            ContentItem excludedGroupContentItem = CreateRandomContentItem();
            excludedGroupContentItem.ContentType = contentType;
            excludedGroupContentItem.ContentHash = contentHash;
            excludedGroupContentItem.ContentItemGroupId = excludedContentItemGroupId;
            excludedGroupContentItem.IsDeleted = false;

            IQueryable<ContentItem> storageContentItems = new List<ContentItem>
            {
                excludedGroupContentItem
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItems);

            // when
            bool actualResult =
                await this.contentItemService.CheckContentItemContentExistsAsync(
                    contentType,
                    contentHash,
                    excludedContentItemGroupId,
                    TestContext.Current.CancellationToken);

            // then
            actualResult.Should().BeFalse();

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.Is(SameCheckRequestAs(contentType, contentHash))),
                Times.Once);

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
        public async Task ShouldReturnFalseOnCheckContentExistsWhenOnlyMatchIsSoftDeletedAsync()
        {
            // given: a soft-deleted row no longer occupies the duplicate slot
            ContentType contentType = ContentType.Quote;
            string contentHash = GetRandomString();

            ContentItem deletedMatchingContentItem = CreateRandomContentItem();
            deletedMatchingContentItem.ContentType = contentType;
            deletedMatchingContentItem.ContentHash = contentHash;
            deletedMatchingContentItem.IsDeleted = true;

            IQueryable<ContentItem> storageContentItems = new List<ContentItem>
            {
                deletedMatchingContentItem
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItems);

            // when
            bool actualResult =
                await this.contentItemService.CheckContentItemContentExistsAsync(
                    contentType,
                    contentHash,
                    excludedContentItemGroupId: null,
                    TestContext.Current.CancellationToken);

            // then
            actualResult.Should().BeFalse();

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.Is(SameCheckRequestAs(contentType, contentHash))),
                Times.Once);

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
        public async Task ShouldReturnFalseOnCheckContentExistsWhenNoContentItemMatchesAsync()
        {
            // given
            ContentType contentType = ContentType.Quote;
            string contentHash = GetRandomString();

            ContentItem differentHashContentItem = CreateRandomContentItem();
            differentHashContentItem.ContentType = contentType;
            differentHashContentItem.ContentHash = GetRandomString();
            differentHashContentItem.IsDeleted = false;

            ContentItem differentTypeContentItem = CreateRandomContentItem();
            differentTypeContentItem.ContentType = ContentType.Story;
            differentTypeContentItem.ContentHash = contentHash;
            differentTypeContentItem.IsDeleted = false;

            IQueryable<ContentItem> storageContentItems = new List<ContentItem>
            {
                differentHashContentItem,
                differentTypeContentItem
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItems);

            // when
            bool actualResult =
                await this.contentItemService.CheckContentItemContentExistsAsync(
                    contentType,
                    contentHash,
                    excludedContentItemGroupId: null,
                    TestContext.Current.CancellationToken);

            // then
            actualResult.Should().BeFalse();

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.Is(SameCheckRequestAs(contentType, contentHash))),
                Times.Once);

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
