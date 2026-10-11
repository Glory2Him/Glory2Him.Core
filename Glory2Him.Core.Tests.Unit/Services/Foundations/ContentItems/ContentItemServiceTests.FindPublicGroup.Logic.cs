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
    /// The public-group read authors its condition as a query-shaping function and hands it to
    /// the storage broker (§ARC12.2.1 rule 3). The mocked broker APPLIES the function it is
    /// handed to the in-memory set each test seeds (rule 5), so these tests execute the
    /// condition rather than merely observing that one was passed.
    /// </summary>
    public partial class ContentItemServiceTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData(1)]
        [InlineData(0)]
        public async Task ShouldFindThePubliclyVisibleVersionOfAGroupAsync(
            int? secondsBeforeTheCurrentMoment)
        {
            // given: one case per publish date §SEC14.1 admits - none at all, earlier than the
            // current moment, and the current moment exactly. The first reds a read that drops
            // the null term, the last a read that turns "<=" into "<".
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            ContentItem visibleContentItem =
                CreateCanonicallyVisibleContentItem(randomDateTimeOffset);

            visibleContentItem.PublishDate = secondsBeforeTheCurrentMoment is int seconds
                ? randomDateTimeOffset.AddSeconds(-seconds)
                : null;

            Guid inputGroupId = visibleContentItem.GroupId;
            var storageContentItems = new List<ContentItem> { visibleContentItem };

            var expectedPublicContentItemGroup = new PublicContentItemGroup(
                ContentItemId: visibleContentItem.Id,
                GroupId: visibleContentItem.GroupId,
                ContentType: visibleContentItem.ContentType);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            SetupPublicContentItemGroupLookupOver(storageContentItems, cancellationToken);

            // when
            PublicContentItemGroup? actualPublicContentItemGroup =
                await this.contentItemService.FindPublicContentItemGroupAsync(
                    groupId: inputGroupId,
                    cancellationToken: cancellationToken);

            // then
            actualPublicContentItemGroup.Should().BeEquivalentTo(
                expectedPublicContentItemGroup);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            VerifyPublicContentItemGroupLookupAsked(cancellationToken, Times.Once());

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
        public async Task ShouldFindOnlyTheVisibleVersionOfAGroupAsync()
        {
            // given: one group holding its visible version beside one version of each kind that
            // can sit next to it - a draft, a soft-deleted version and an Approved version that
            // is unpublished. They are stored ahead of the visible version, so a read that
            // takes the group's first row rather than its visible one answers a hidden version.
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            ContentItem visibleContentItem =
                CreateCanonicallyVisibleContentItem(randomDateTimeOffset);

            ContentItem draftContentItem = CreateRandomContentItem(randomDateTimeOffset);

            ContentItem deletedContentItem =
                CreateCanonicallyVisibleContentItem(randomDateTimeOffset);

            deletedContentItem.IsDeleted = true;

            ContentItem unpublishedContentItem =
                CreateCanonicallyVisibleContentItem(randomDateTimeOffset);

            unpublishedContentItem.IsPublished = false;

            var hiddenContentItems = new[]
            {
                draftContentItem,
                deletedContentItem,
                unpublishedContentItem
            };

            for (int index = 0; index < hiddenContentItems.Length; index++)
            {
                hiddenContentItems[index].GroupId = visibleContentItem.GroupId;
                hiddenContentItems[index].ContentType = visibleContentItem.ContentType;
                hiddenContentItems[index].Version = visibleContentItem.Version + index + 1;
            }

            Guid inputGroupId = visibleContentItem.GroupId;

            var storageContentItems = new List<ContentItem>
            {
                draftContentItem,
                deletedContentItem,
                unpublishedContentItem,
                visibleContentItem
            };

            var expectedPublicContentItemGroup = new PublicContentItemGroup(
                ContentItemId: visibleContentItem.Id,
                GroupId: visibleContentItem.GroupId,
                ContentType: visibleContentItem.ContentType);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            SetupPublicContentItemGroupLookupOver(storageContentItems, cancellationToken);

            // when
            PublicContentItemGroup? actualPublicContentItemGroup =
                await this.contentItemService.FindPublicContentItemGroupAsync(
                    groupId: inputGroupId,
                    cancellationToken: cancellationToken);

            // then
            actualPublicContentItemGroup.Should().BeEquivalentTo(
                expectedPublicContentItemGroup);

            VerifyPublicContentItemGroupLookupAsked(cancellationToken, Times.Once());
        }

        [Theory]
        [MemberData(nameof(CanonicalVisibilityTerms))]
        public async Task ShouldFindNothingForAGroupWithNoVisibleVersionAsync(string failedTerm)
        {
            // given: a group whose one version satisfies every §SEC14.1 term but one, so
            // dropping or inverting any single term lets that version back in
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            ContentItem hiddenContentItem =
                CreateCanonicallyVisibleContentItem(randomDateTimeOffset);

            FailCanonicalVisibilityTerm(hiddenContentItem, failedTerm, randomDateTimeOffset);

            Guid inputGroupId = hiddenContentItem.GroupId;
            var storageContentItems = new List<ContentItem> { hiddenContentItem };

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            SetupPublicContentItemGroupLookupOver(storageContentItems, cancellationToken);

            // when
            PublicContentItemGroup? actualPublicContentItemGroup =
                await this.contentItemService.FindPublicContentItemGroupAsync(
                    groupId: inputGroupId,
                    cancellationToken: cancellationToken);

            // then
            actualPublicContentItemGroup.Should().BeNull();
            VerifyPublicContentItemGroupLookupAsked(cancellationToken, Times.Once());
        }

        [Fact]
        public async Task ShouldFindNothingFromAnotherGroupAsync()
        {
            // given: the asked-for group holds only a draft, and another group holds a
            // canonically visible version. A read that asked §SEC14.1 of any version rather than
            // of this group's versions would answer the other group's.
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            ContentItem draftContentItem = CreateRandomContentItem(randomDateTimeOffset);

            ContentItem otherGroupVisibleContentItem =
                CreateCanonicallyVisibleContentItem(randomDateTimeOffset);

            Guid inputGroupId = draftContentItem.GroupId;

            var storageContentItems = new List<ContentItem>
            {
                draftContentItem,
                otherGroupVisibleContentItem
            };

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            SetupPublicContentItemGroupLookupOver(storageContentItems, cancellationToken);

            // when
            PublicContentItemGroup? actualPublicContentItemGroup =
                await this.contentItemService.FindPublicContentItemGroupAsync(
                    groupId: inputGroupId,
                    cancellationToken: cancellationToken);

            // then
            actualPublicContentItemGroup.Should().BeNull();
            VerifyPublicContentItemGroupLookupAsked(cancellationToken, Times.Once());
        }

        public static TheoryData<string> CanonicalVisibilityTerms() =>
            new TheoryData<string>
            {
                "Deleted",
                "NotApproved",
                "Unpublished",
                "PublishedAfterTheCurrentMoment"
            };

        private static void FailCanonicalVisibilityTerm(
            ContentItem contentItem,
            string failedTerm,
            DateTimeOffset currentDateTime)
        {
            switch (failedTerm)
            {
                case "Deleted":
                    contentItem.IsDeleted = true;
                    break;

                case "NotApproved":
                    contentItem.ApprovalStatus = ApprovalStatus.Submitted;
                    break;

                case "Unpublished":
                    contentItem.IsPublished = false;
                    break;

                case "PublishedAfterTheCurrentMoment":
                    contentItem.PublishDate = currentDateTime.AddSeconds(1);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(failedTerm), failedTerm, null);
            }
        }

        // The mock stands in for the storage client: it APPLIES the query-shaping function the
        // service authored to the seeded set, so each test proves the condition by executing it
        // (§ARC12.2.1 rule 5). The function is matched by any value because a lambda cannot be
        // compared for equality — what it selects is the assertion, not its identity.
        private void SetupPublicContentItemGroupLookupOver(
            IEnumerable<ContentItem> storageContentItems,
            CancellationToken cancellationToken) =>
            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemsAsync(
                    It.IsAny<Func<IQueryable<ContentItem>, IQueryable<PublicContentItemGroup>>>(),
                    cancellationToken))
                        .ReturnsAsync((
                            Func<IQueryable<ContentItem>, IQueryable<PublicContentItemGroup>> query,
                            CancellationToken _) =>
                                (IReadOnlyList<PublicContentItemGroup>)
                                    query(storageContentItems.AsQueryable()).ToList());

        private void VerifyPublicContentItemGroupLookupAsked(
            CancellationToken cancellationToken,
            Times times) =>
            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemsAsync(
                    It.IsAny<Func<IQueryable<ContentItem>, IQueryable<PublicContentItemGroup>>>(),
                    cancellationToken),
                times);
    }
}
