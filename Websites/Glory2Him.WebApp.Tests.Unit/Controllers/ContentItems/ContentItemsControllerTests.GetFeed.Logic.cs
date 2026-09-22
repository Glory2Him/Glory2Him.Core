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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.ContentItems
{
    public partial class ContentItemsControllerTests
    {
        /// <summary>
        /// Pins WHICH member the feed route calls, for the reason the public route's twin of
        /// this test states: <c>Get</c> widens with the caller and would serve the caller their
        /// own drafts on the front page. A wiring slip between the two compiles and passes every
        /// attribute test.
        /// </summary>
        [Fact]
        public async Task ShouldReturnContentItemsOnGetFeedAsync()
        {
            // given
            IReadOnlyList<ContentItem> randomContentItems = CreateRandomContentItems().ToList();
            IReadOnlyList<ContentItem> expectedContentItems = randomContentItems;

            var expectedObjectResult = new OkObjectResult(expectedContentItems);

            var expectedActionResult =
                new ActionResult<IReadOnlyList<ContentItem>>(expectedObjectResult);

            contentItemProcessingServiceMock
                .Setup(service => service.RetrieveContentItemFeedAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(randomContentItems);

            // when
            ActionResult<IReadOnlyList<ContentItem>> actualActionResult =
                await contentItemsController.GetContentItemFeed(
                    skip: 0,
                    take: 10,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            contentItemProcessingServiceMock
                .Verify(service => service.RetrieveContentItemFeedAsync(
                    0,
                    10,
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            // The whole point of this assertion: a call to the caller-widening read, or to the
            // public one, would be caught here rather than in production.
            contentItemProcessingServiceMock.VerifyNoOtherCalls();
        }
    }
}
