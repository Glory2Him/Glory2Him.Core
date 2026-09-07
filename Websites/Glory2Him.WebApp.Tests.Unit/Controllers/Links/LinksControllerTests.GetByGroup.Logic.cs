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
using Glory2Him.Core.Models.Foundations.Links;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.Links
{
    public partial class LinksControllerTests
    {
        /// <summary>
        /// Pins the group collection read to its own member.
        /// </summary>
        [Fact]
        public async Task ShouldReturnLinksOnGetByGroupAsync()
        {
            // given
            Guid inputGroupId = Guid.NewGuid();
            IReadOnlyList<Link> randomLinks = CreateRandomLinks().ToList();
            IReadOnlyList<Link> expectedLinks = randomLinks;

            var expectedObjectResult = new OkObjectResult(expectedLinks);
            var expectedActionResult = new ActionResult<IReadOnlyList<Link>>(expectedObjectResult);

            linkProcessingServiceMock
                .Setup(service => service.RetrieveLinksByGroupIdAsync(inputGroupId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(randomLinks);

            // when
            ActionResult<IReadOnlyList<Link>> actualActionResult =
                await linksController.GetLinksByGroupId(inputGroupId, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            linkProcessingServiceMock
                .Verify(service => service.RetrieveLinksByGroupIdAsync(inputGroupId, It.IsAny<CancellationToken>()),
                    Times.Once);

            // The whole point of this assertion. The group reads are three near-identical signatures; this keeps them apart.
            linkProcessingServiceMock.VerifyNoOtherCalls();
        }
    }
}
