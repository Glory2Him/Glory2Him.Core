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
using Glory2Him.Core.Models.Foundations.Links;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.Links
{
    public partial class LinkProcessingServiceTests
    {
        /// <summary>
        /// The group's rows come back from the GROUP-KEYED foundation read, which owns both the
        /// narrowing and the §14.7 visibility filter. This layer used to narrow the collection
        /// read's live queryable and filter the result a second time; the visibility scenarios
        /// that pinned that second filter now live in
        /// <c>LinkServiceTests.RetrieveByGroupId.Logic</c>, at the one seam that still applies it.
        ///
        /// <para>What is left here is what this layer still owns: the group id it passes down,
        /// and the set it hands back untouched.</para>
        /// </summary>
        [Fact]
        public async Task ShouldRetrieveGroupLinksFromTheGroupKeyedReadOnRetrieveByGroupIdAsync()
        {
            // given
            Guid inputGroupId = Guid.NewGuid();
            IReadOnlyList<Link> foundationLinks = CreateRandomLinks().ToList();
            IReadOnlyList<Link> expectedLinks = foundationLinks;

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinksByGroupIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(foundationLinks);

            // when
            IReadOnlyList<Link> actualLinks =
                await this.linkProcessingService.RetrieveLinksByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            // then
            actualLinks.Should().BeSameAs(expectedLinks);

            // the caller's group, unaltered, and the caller's token with it
            this.linkServiceMock.Verify(service =>
                service.RetrieveLinksByGroupIdAsync(
                    inputGroupId, It.IsAny<CancellationToken>()),
                Times.Once);

            // NO ENVELOPE IS MINTED. The filter that needed one moved down a layer, and minting
            // a second envelope here would only re-run it over the set it already produced.
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();

            // the visibility filter is no longer applied at this layer, so neither the clock nor
            // the caller's identity is consulted here
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.linkServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
