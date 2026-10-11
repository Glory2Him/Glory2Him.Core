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
