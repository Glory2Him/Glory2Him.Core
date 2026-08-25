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
using Glory2Him.Core.Models.Foundations.Links;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.Links
{
    public partial class LinksControllerTests
    {
        [Fact]
        public async Task ShouldReturnLinkOnGetPublishedByGroupAsync()
        {
            // given
            Link randomLink = CreateRandomLink();
            Link expectedLink = randomLink;
            Guid inputGroupId = Guid.NewGuid();

            var expectedObjectResult = new OkObjectResult(expectedLink);
            var expectedActionResult = new ActionResult<Link>(expectedObjectResult);

            linkProcessingServiceMock
                .Setup(service => service.RetrievePublishedLinkByGroupIdAsync(inputGroupId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(randomLink);

            // when
            ActionResult<Link> actualActionResult =
                await linksController.GetPublishedLinkByGroupIdAsync(inputGroupId, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            linkProcessingServiceMock
                .Verify(service => service.RetrievePublishedLinkByGroupIdAsync(inputGroupId, It.IsAny<CancellationToken>()),
                    Times.Once);

            linkProcessingServiceMock.VerifyNoOtherCalls();
        }
    }
}
