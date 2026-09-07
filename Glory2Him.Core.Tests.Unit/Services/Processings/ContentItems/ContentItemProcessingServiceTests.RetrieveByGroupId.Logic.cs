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
        /// <summary>
        /// The group's rows come back from the GROUP-KEYED foundation read, which owns both the
        /// narrowing and the §14.7 visibility filter. This layer used to narrow the collection
        /// read's live queryable and filter the result a second time; the visibility scenarios
        /// that pinned that second filter now live in
        /// <c>ContentItemServiceTests.RetrieveByGroupId.Logic</c>, at the one seam that still
        /// applies it.
        /// </summary>
        ///
        /// <remarks>
        /// <para>What is left here is what this layer still owns: the group id and the token it
        /// passes down, and the set it hands back untouched.</para>
        /// </remarks>
        [Fact]
        public async Task ShouldRetrieveGroupContentItemsFromTheGroupKeyedReadOnRetrieveByGroupIdAsync()
        {
            // given: the set the foundation hands up ALREADY carries the §14.7 decision, so it can
            // legitimately contain rows a filter at THIS layer would have taken out — a deleted row
            // and another caller's draft. Both are seeded on purpose: if that filter ever comes back
            // here, they go missing and this test goes red.
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();
            Guid inputGroupId = Guid.NewGuid();

            // a token of this test's own making, so the assertion below cannot be satisfied by a
            // dropped one the way It.IsAny<CancellationToken>() would
            using var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken inputCancellationToken = cancellationTokenSource.Token;

            ContentItem publicContentItem = CreateRandomPubliclyVisibleContentItem(
                contentItemId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            ContentItem otherCallersDraftContentItem =
                CreateRandomNonPublicContentItem(createdBy: GetRandomString());

            ContentItem deletedContentItem =
                CreateRandomDeletedContentItem(currentDateTime);

            IReadOnlyList<ContentItem> foundationContentItems = new List<ContentItem>
            {
                publicContentItem,
                otherCallersDraftContentItem,
                deletedContentItem
            };

            // deep clones, so "handed back untouched" is judged on the rows' VALUES rather than on
            // the service happening to return the very list instance it was given
            IReadOnlyList<ContentItem> expectedContentItems = new List<ContentItem>
            {
                publicContentItem.DeepClone(),
                otherCallersDraftContentItem.DeepClone(),
                deletedContentItem.DeepClone()
            };

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemsByGroupIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(foundationContentItems);

            // when
            IReadOnlyList<ContentItem> actualContentItems =
                await this.contentItemProcessingService.RetrieveContentItemsByGroupIdAsync(
                    inputGroupId,
                    inputCancellationToken);

            // then: same rows, same order, nothing added and nothing dropped
            actualContentItems.Should().BeEquivalentTo(
                expectedContentItems,
                options => options.WithStrictOrdering());

            // stated separately because it is the whole point: the two rows a visibility filter at
            // this layer would have removed are still in the set
            actualContentItems.Should().Contain(contentItem =>
                contentItem.Id == deletedContentItem.Id);

            actualContentItems.Should().Contain(contentItem =>
                contentItem.Id == otherCallersDraftContentItem.Id);

            // the caller's group AND the caller's token, both pinned literally — this is the one
            // assertion that would catch the token being dropped at the call that reaches storage
            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemsByGroupIdAsync(
                    inputGroupId, inputCancellationToken),
                Times.Once);

            // NO ENVELOPE IS MINTED. The filter that needed one moved down a layer, and minting a
            // second envelope here would only re-run it over the set it already produced.
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();

            // the visibility filter is no longer applied at this layer, so neither the clock nor
            // the caller's identity is consulted here
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
