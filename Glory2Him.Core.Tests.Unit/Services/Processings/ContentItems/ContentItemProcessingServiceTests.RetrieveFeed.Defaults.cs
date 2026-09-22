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
        /// Criterion 9's validation row: ABSENT PARAMETERS ARE NOT A VALIDATION FAILURE.
        /// A null <c>skip</c> resolves to 0 and a null <c>take</c> to 50 — the first page at the
        /// cap — and NULL STOPS HERE, so nothing below this service can be asked "which page?"
        /// and receive no answer.
        ///
        /// <para>The numbers are written out rather than read from the service's constant. A
        /// test that reads the constant still passes after somebody changes it, which is the
        /// opposite of what pinning a default is for.</para>
        /// </summary>
        [Fact]
        public async Task ShouldServeTheFirstFeedPageAtTheCapIfSkipAndTakeAreAbsentAsync()
        {
            // given
            IReadOnlyList<ContentItem> foundationContentItems = new List<ContentItem>
            {
                CreateRandomContentItem()
            };

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemFeedAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(foundationContentItems);

            // when
            IReadOnlyList<ContentItem> actualContentItems =
                await this.contentItemProcessingService.RetrieveContentItemFeedAsync(
                    skip: null,
                    take: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualContentItems.Should().BeEquivalentTo(foundationContentItems);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemFeedAsync(
                    0,
                    50,
                    It.IsAny<CancellationToken>()),
                        Times.Once);

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
