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
    /// <summary>
    /// The public-groups read authors its condition as a query-shaping function and hands it to
    /// the storage broker (§ARC12.2.1 rule 3). The mocked broker APPLIES the function it is
    /// handed to the in-memory set each test seeds (rule 5), so these tests execute the
    /// condition rather than merely observing that one was passed. That the same function
    /// translates to SQL is proven against the real catalogue in the integration suite
    /// (rule 6).
    /// </summary>
    public partial class ContentItemServiceTests
    {
        [Fact]
        public async Task ShouldAnswerTheGroupOfAPubliclyVisibleVersionAsync()
        {
            // given: the asked-for row is visible with no publish date at all, and a second
            // visible row is in storage but not asked for
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            ContentItem visibleContentItem =
                CreateCanonicallyVisibleContentItem(randomDateTimeOffset);

            visibleContentItem.PublishDate = null;

            ContentItem unaskedVisibleContentItem =
                CreateCanonicallyVisibleContentItem(randomDateTimeOffset);

            var storageContentItems = new List<ContentItem>
            {
                visibleContentItem,
                unaskedVisibleContentItem
            };

            IReadOnlyList<Guid> inputContentItemIds = new[] { visibleContentItem.Id };

            var expectedPublicContentItemGroups = new[]
            {
                new PublicContentItemGroup(
                    ContentItemId: visibleContentItem.Id,
                    GroupId: visibleContentItem.GroupId,
                    ContentType: visibleContentItem.ContentType)
            };

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            SetupPublicContentItemGroupsStorage(storageContentItems);

            // when
            IReadOnlyList<PublicContentItemGroup> actualPublicContentItemGroups =
                await this.contentItemService.RetrievePublicContentItemGroupsAsync(
                    contentItemIds: inputContentItemIds,
                    cancellationToken: cancellationToken);

            // then
            actualPublicContentItemGroups.Should().BeEquivalentTo(
                expectedPublicContentItemGroups);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemsAsync(
                    It.IsAny<Func<IQueryable<ContentItem>, IQueryable<PublicContentItemGroup>>>(),
                    cancellationToken),
                Times.Once);

            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldAnswerAVersionWhoseGroupHasAVisibleVersionAsync()
        {
            // given: v1 is the group's visible version and is not asked for; the draft v2 of
            // the same group is. The answer is at group level, so the draft answers.
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ContentItem visibleContentItem =
                CreateCanonicallyVisibleContentItem(randomDateTimeOffset);

            ContentItem draftContentItem = CreateRandomContentItem(randomDateTimeOffset);
            draftContentItem.GroupId = visibleContentItem.GroupId;
            draftContentItem.ContentType = visibleContentItem.ContentType;
            draftContentItem.Version = visibleContentItem.Version + 1;

            var storageContentItems = new List<ContentItem>
            {
                visibleContentItem,
                draftContentItem
            };

            IReadOnlyList<Guid> inputContentItemIds = new[] { draftContentItem.Id };

            var expectedPublicContentItemGroups = new[]
            {
                new PublicContentItemGroup(
                    ContentItemId: draftContentItem.Id,
                    GroupId: visibleContentItem.GroupId,
                    ContentType: draftContentItem.ContentType)
            };

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            SetupPublicContentItemGroupsStorage(storageContentItems);

            // when
            IReadOnlyList<PublicContentItemGroup> actualPublicContentItemGroups =
                await this.contentItemService.RetrievePublicContentItemGroupsAsync(
                    contentItemIds: inputContentItemIds,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualPublicContentItemGroups.Should().BeEquivalentTo(
                expectedPublicContentItemGroups);
        }

        // THE CONDITION RUNS HERE, over the seeded set - never a canned answer. A stub that
        // returned a fixed list would pass whether or not the function it was handed still
        // carried any of its terms.
        private void SetupPublicContentItemGroupsStorage(
            IReadOnlyList<ContentItem> storageContentItems)
        {
            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemsAsync(
                    It.IsAny<Func<IQueryable<ContentItem>, IQueryable<PublicContentItemGroup>>>(),
                    It.IsAny<CancellationToken>()))
                        .Returns((
                            Func<IQueryable<ContentItem>, IQueryable<PublicContentItemGroup>> query,
                            CancellationToken _) =>
                                new ValueTask<IReadOnlyList<PublicContentItemGroup>>(
                                    query(storageContentItems.AsQueryable()).ToList()));
        }

        // Every §SEC14.1 term satisfied: live, Approved, published, and published at the current
        // moment exactly - the boundary "not after the current moment" admits. The content type
        // is drawn explicitly because the filler leaves it at its default on every row, which
        // would let a test that answers the wrong row's type pass.
        private static ContentItem CreateCanonicallyVisibleContentItem(DateTimeOffset currentDateTime)
        {
            ContentItem contentItem = CreateRandomContentItem(currentDateTime);
            contentItem.ContentType = GetRandomNonDefaultContentType();
            contentItem.ApprovalStatus = ApprovalStatus.Approved;
            contentItem.IsPublished = true;
            contentItem.PublishDate = currentDateTime;

            return contentItem;
        }

        private static ContentType GetRandomNonDefaultContentType()
        {
            ContentType[] contentTypes = Enum.GetValues<ContentType>()
                .Where(contentType => contentType != default)
                .ToArray();

            return contentTypes[Random.Shared.Next(contentTypes.Length)];
        }
    }
}
