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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    public partial class ContentItemServiceTests
    {
        // WHAT THIS TEST CAN AND CANNOT SAY. The feed's predicate, its order and its page are
        // ALL storage's — one expression, awaited in the broker — so the stub below hands back
        // a page without re-implementing any of them. A stub that re-implemented them would
        // pass whether or not the real read still carried them; that the read serves the
        // §SEC14.1 set, excludes Topic and Series, orders by effective publication moment and
        // pages without repeating is proved against a real catalogue through the route.
        //
        // What is proved HERE is the half this layer owns: the arguments it hands down, and the
        // fact that it consults nobody's identity to compose them.
        [Fact]
        public async Task ShouldRetrieveTheFeedPageAsOfTheCurrentMomentAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            int inputSkip = GetRandomNumber();
            int inputTake = GetRandomNumber();

            List<ContentItem> storageContentItems = new List<ContentItem>
            {
                CreateRandomContentItem(),
                CreateRandomContentItem()
            };

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemFeedPageAsync(
                    randomDateTimeOffset,
                    inputSkip,
                    inputTake,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItems);

            // when
            IReadOnlyList<ContentItem> actualContentItems =
                await this.contentItemService.RetrieveContentItemFeedAsync(
                    skip: inputSkip,
                    take: inputTake,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualContentItems.Should().BeEquivalentTo(storageContentItems);

            // THE MOMENT AND THE PAGE THE CALLER NAMED, unaltered. §SEC14.1's fourth term is
            // evaluated against the clock this service owns, which is the one thing about the
            // predicate that does not live in the broker.
            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemFeedPageAsync(
                    randomDateTimeOffset,
                    inputSkip,
                    inputTake,
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            // CALLER-INDEPENDENT, structurally: no envelope is minted, no identity is resolved
            // and no access decision is asked for, so there is nothing here that privilege
            // could widen.
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();

            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
