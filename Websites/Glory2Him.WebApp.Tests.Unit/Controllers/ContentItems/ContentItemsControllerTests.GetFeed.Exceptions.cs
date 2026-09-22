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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;
using RESTFulSense.Models;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.ContentItems
{
    /// <summary>
    /// The failure taxonomy of the feed read, and the validation arm is the one that matters
    /// here: THE PAGE IS CALLER-SUPPLIED. A take of zero, a take above the cap and a negative
    /// skip are the caller's mistake and answer 400 — without this arm the processing service's
    /// validation exception escapes the action and ASP.NET files a server fault for bad input,
    /// which is the defect the sibling <c>Groups/{groupId}</c> route was repaired for.
    /// </summary>
    public partial class ContentItemsControllerTests
    {
        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldReturnBadRequestOnGetFeedIfValidationErrorOccurredAsync(
            Xeption validationException)
        {
            // given
            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<IReadOnlyList<ContentItem>>(expectedBadRequestObjectResult);

            this.contentItemProcessingServiceMock.Setup(service =>
                service.RetrieveContentItemFeedAsync(
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<IReadOnlyList<ContentItem>> actualActionResult =
                await this.contentItemsController.GetContentItemFeed(
                    skip: 0,
                    take: 0,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.contentItemProcessingServiceMock.Verify(service =>
                service.RetrieveContentItemFeedAsync(
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.contentItemProcessingServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldReturnFailedDependencyOnGetFeedIfDependencyErrorOccurredAsync(
            Xeption dependencyException)
        {
            // given: 424, and the inner exception alone travels outward — no reason, no row
            // state and no caller identity (§SEC14.5 rule 2)
            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(dependencyException.InnerException);

            var expectedActionResult =
                new ActionResult<IReadOnlyList<ContentItem>>(expectedFailedDependencyObjectResult);

            this.contentItemProcessingServiceMock.Setup(service =>
                service.RetrieveContentItemFeedAsync(
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dependencyException);

            // when
            ActionResult<IReadOnlyList<ContentItem>> actualActionResult =
                await this.contentItemsController.GetContentItemFeed(
                    skip: 0,
                    take: 10,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.contentItemProcessingServiceMock.Verify(service =>
                service.RetrieveContentItemFeedAsync(
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.contentItemProcessingServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ServerExceptions))]
        public async Task ShouldReturnInternalServerErrorOnGetFeedIfServerErrorOccurredAsync(
            Xeption serverException)
        {
            // given
            InternalServerErrorObjectResult expectedInternalServerErrorObjectResult =
                InternalServerError(serverException);

            var expectedActionResult =
                new ActionResult<IReadOnlyList<ContentItem>>(
                    expectedInternalServerErrorObjectResult);

            this.contentItemProcessingServiceMock.Setup(service =>
                service.RetrieveContentItemFeedAsync(
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serverException);

            // when
            ActionResult<IReadOnlyList<ContentItem>> actualActionResult =
                await this.contentItemsController.GetContentItemFeed(
                    skip: 0,
                    take: 10,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.contentItemProcessingServiceMock.Verify(service =>
                service.RetrieveContentItemFeedAsync(
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.contentItemProcessingServiceMock.VerifyNoOtherCalls();
        }
    }
}
