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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Force.DeepCloner;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.ContentItems
{
    public partial class ContentItemProcessingServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveOnlyCanonicallyVisibleContentItemsOnRetrieveAllPublicAsync()
        {
            // given: the public projection is caller-independent — no envelope is minted
            // and no security context is consulted, so drafts, future-scheduled rows and
            // deleted rows drop out for every caller, privileged or not (§14.1)
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            ContentItem publicContentItem = CreateRandomPubliclyVisibleContentItem(
                contentItemId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            ContentItem publicNoDateContentItem = CreateRandomPubliclyVisibleContentItem(
                contentItemId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: false);

            ContentItem nonPublicContentItem = CreateRandomNonPublicContentItem(
                createdBy: GetRandomString());

            ContentItem futureContentItem = CreateRandomPubliclyVisibleContentItem(
                contentItemId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            futureContentItem.PublishDate = currentDateTime.AddDays(1);
            ContentItem deletedContentItem = CreateRandomDeletedContentItem(currentDateTime);

            IQueryable<ContentItem> storageContentItems = new[]
            {
                publicContentItem,
                publicNoDateContentItem,
                nonPublicContentItem,
                futureContentItem,
                deletedContentItem
            }.AsQueryable();

            IQueryable<ContentItem> expectedContentItems = new[]
            {
                publicContentItem.DeepClone(),
                publicNoDateContentItem.DeepClone()
            }.AsQueryable();

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItems);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            IQueryable<ContentItem> actualContentItems =
                await this.contentItemProcessingService.RetrieveAllPublicContentItemsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualContentItems.Should().BeEquivalentTo(expectedContentItems);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            // caller-independent: no envelope, no identity lookup, and no fact published
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();

            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
