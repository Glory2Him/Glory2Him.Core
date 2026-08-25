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
        /// Pins WHICH member the public route calls. RetrieveAllPublicContentItemsAsync consults no security context; RetrieveAllContentItemsAsync widens with the caller. The two return the same type and the same shape, so a wiring slip between them compiles, passes every attribute test, and leaks unapproved drafts to anonymous visitors.
        /// </summary>
        [Fact]
        public async Task ShouldReturnContentItemsOnGetPublicAsync()
        {
            // given
            IQueryable<ContentItem> randomContentItems = CreateRandomContentItems();
            IQueryable<ContentItem> expectedContentItems = randomContentItems;

            var expectedObjectResult = new OkObjectResult(expectedContentItems);
            var expectedActionResult = new ActionResult<IQueryable<ContentItem>>(expectedObjectResult);

            contentItemProcessingServiceMock
                .Setup(service => service.RetrieveAllPublicContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(randomContentItems);

            // when
            ActionResult<IQueryable<ContentItem>> actualActionResult =
                await contentItemsController.GetPublicContentItems(default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            contentItemProcessingServiceMock
                .Verify(service => service.RetrieveAllPublicContentItemsAsync(It.IsAny<CancellationToken>()),
                    Times.Once);

            // The whole point of this assertion. A call to the widening read instead would be caught here rather than in production.
            contentItemProcessingServiceMock.VerifyNoOtherCalls();
        }
    }
}
