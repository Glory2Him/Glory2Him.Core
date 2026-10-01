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
        public async Task ShouldLeaveOutAVersionThatIsNotVisibleThoughItsGroupIsAsync()
        {
            // given: one group holding its visible version beside one version of each kind that
            // can sit next to it - soft-deleted, Approved but unpublished, and a draft. Each is
            // asked for, and none answers: a version is answered only when it is itself
            // visible, so the draft of a public item reads exactly as an id that names nothing
            // (§SEC14.5 rules 1 and 3).
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ContentItem visibleContentItem =
                CreateCanonicallyVisibleContentItem(randomDateTimeOffset);

            ContentItem deletedContentItem =
                CreateCanonicallyVisibleContentItem(randomDateTimeOffset);

            deletedContentItem.IsDeleted = true;

            ContentItem unpublishedContentItem =
                CreateCanonicallyVisibleContentItem(randomDateTimeOffset);

            unpublishedContentItem.IsPublished = false;

            ContentItem draftContentItem = CreateRandomContentItem(randomDateTimeOffset);

            var groupContentItems = new[]
            {
                deletedContentItem,
                unpublishedContentItem,
                draftContentItem
            };

            for (int index = 0; index < groupContentItems.Length; index++)
            {
                groupContentItems[index].GroupId = visibleContentItem.GroupId;
                groupContentItems[index].ContentType = visibleContentItem.ContentType;
                groupContentItems[index].Version = visibleContentItem.Version + index + 1;
            }

            var storageContentItems = new List<ContentItem>
            {
                visibleContentItem,
                deletedContentItem,
                unpublishedContentItem,
                draftContentItem
            };

            IReadOnlyList<Guid> inputContentItemIds = new[]
            {
                deletedContentItem.Id,
                unpublishedContentItem.Id,
                draftContentItem.Id,
                visibleContentItem.Id
            };

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
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualPublicContentItemGroups.Should().BeEquivalentTo(
                expectedPublicContentItemGroups);
        }

        [Fact]
        public async Task ShouldLeaveOutAGroupWithNoVisibleVersionAsync()
        {
            // given: four groups, each with one version that satisfies every §SEC14.1 term but
            // one - so dropping or inverting any single term lets its group back in. A fifth,
            // visible group is stored but not asked for: a sibling test that forgot to match the
            // GROUP would let it vouch for all four.
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ContentItem deletedContentItem =
                CreateCanonicallyVisibleContentItem(randomDateTimeOffset);

            deletedContentItem.IsDeleted = true;

            ContentItem unapprovedContentItem =
                CreateCanonicallyVisibleContentItem(randomDateTimeOffset);

            unapprovedContentItem.ApprovalStatus = ApprovalStatus.Submitted;

            ContentItem unpublishedContentItem =
                CreateCanonicallyVisibleContentItem(randomDateTimeOffset);

            unpublishedContentItem.IsPublished = false;

            ContentItem futurePublishedContentItem =
                CreateCanonicallyVisibleContentItem(randomDateTimeOffset);

            futurePublishedContentItem.PublishDate = randomDateTimeOffset.AddSeconds(1);

            ContentItem unaskedVisibleContentItem =
                CreateCanonicallyVisibleContentItem(randomDateTimeOffset);

            var storageContentItems = new List<ContentItem>
            {
                deletedContentItem,
                unapprovedContentItem,
                unpublishedContentItem,
                futurePublishedContentItem,
                unaskedVisibleContentItem
            };

            IReadOnlyList<Guid> inputContentItemIds = new[]
            {
                deletedContentItem.Id,
                unapprovedContentItem.Id,
                unpublishedContentItem.Id,
                futurePublishedContentItem.Id
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
            actualPublicContentItemGroups.Should().BeEmpty();
        }

        [Fact]
        public async Task ShouldLeaveOutAnIdThatNamesNothingAsync()
        {
            // given: one id names a visible version, the other names no row at all
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Guid unknownContentItemId = Guid.NewGuid();

            ContentItem visibleContentItem =
                CreateCanonicallyVisibleContentItem(randomDateTimeOffset);

            var storageContentItems = new List<ContentItem> { visibleContentItem };

            IReadOnlyList<Guid> inputContentItemIds = new[]
            {
                unknownContentItemId,
                visibleContentItem.Id
            };

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
                    cancellationToken: TestContext.Current.CancellationToken);

            // then: left out, not refused - no exception, and nothing logged
            actualPublicContentItemGroups.Should().BeEquivalentTo(
                expectedPublicContentItemGroups);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldAnswerADuplicatedIdOnceAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ContentItem visibleContentItem =
                CreateCanonicallyVisibleContentItem(randomDateTimeOffset);

            var storageContentItems = new List<ContentItem> { visibleContentItem };

            IReadOnlyList<Guid> inputContentItemIds = new[]
            {
                visibleContentItem.Id,
                visibleContentItem.Id
            };

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
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualPublicContentItemGroups.Should().BeEquivalentTo(
                expectedPublicContentItemGroups);
        }

        [Fact]
        public async Task ShouldRetrievePublicGroupsWithoutReadingTheCallerAsync()
        {
            // given: a visible group, and a group whose only version is Submitted - a row an
            // administrator could see through the caller-filtered reads, and an anonymous
            // visitor could not. Here both must receive the same answer.
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ContentItem visibleContentItem =
                CreateCanonicallyVisibleContentItem(randomDateTimeOffset);

            ContentItem submittedContentItem =
                CreateCanonicallyVisibleContentItem(randomDateTimeOffset);

            submittedContentItem.ApprovalStatus = ApprovalStatus.Submitted;
            submittedContentItem.IsPublished = false;
            submittedContentItem.PublishDate = null;

            var storageContentItems = new List<ContentItem>
            {
                visibleContentItem,
                submittedContentItem
            };

            IReadOnlyList<Guid> inputContentItemIds = new[]
            {
                visibleContentItem.Id,
                submittedContentItem.Id
            };

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
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators);

            IReadOnlyList<PublicContentItemGroup> administratorPublicContentItemGroups =
                await this.contentItemService.RetrievePublicContentItemGroupsAsync(
                    contentItemIds: inputContentItemIds,
                    cancellationToken: TestContext.Current.CancellationToken);

            this.ambientSecurityContext = new SecurityContext { IsAuthenticated = false };

            IReadOnlyList<PublicContentItemGroup> anonymousPublicContentItemGroups =
                await this.contentItemService.RetrievePublicContentItemGroupsAsync(
                    contentItemIds: inputContentItemIds,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            administratorPublicContentItemGroups.Should().BeEquivalentTo(
                expectedPublicContentItemGroups);

            anonymousPublicContentItemGroups.Should().BeEquivalentTo(
                expectedPublicContentItemGroups);

            // CALLER-INDEPENDENT, structurally: no envelope is minted, no identity resolved and
            // no access decision asked for, so there is nothing a privilege could widen
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
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
