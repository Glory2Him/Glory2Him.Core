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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.ContentItems
{
    public partial class ContentItemProcessingServiceTests
    {
        /// <summary>
        /// Criterion 5's structural half: THE FEED READ RESOLVES NO SECURITY CONTEXT AT ALL.
        /// No envelope is minted, no user id is looked up and the clock is not consulted here —
        /// so there is nothing on this path that a caller's roles could widen, which is a
        /// stronger statement than "the answers happened to match".
        /// </summary>
        [Fact]
        public async Task ShouldRetrieveContentItemFeedWithoutConsultingASecurityContextAsync()
        {
            // given
            int inputSkip = GetRandomNumber();
            int inputTake = GetRandomNumber();

            IReadOnlyList<ContentItem> foundationContentItems = new List<ContentItem>
            {
                CreateRandomContentItem(),
                CreateRandomContentItem()
            };

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemFeedAsync(
                    inputSkip,
                    inputTake,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(foundationContentItems);

            // when
            IReadOnlyList<ContentItem> actualContentItems =
                await this.contentItemProcessingService.RetrieveContentItemFeedAsync(
                    skip: inputSkip,
                    take: inputTake,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualContentItems.Should().BeEquivalentTo(foundationContentItems);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemFeedAsync(
                    inputSkip,
                    inputTake,
                    It.IsAny<CancellationToken>()),
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
