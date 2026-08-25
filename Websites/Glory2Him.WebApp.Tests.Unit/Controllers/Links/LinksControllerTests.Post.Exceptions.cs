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
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Processings.Links.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;
using RESTFulSense.Models;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.Links
{
    public partial class LinksControllerTests
    {
        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldReturnBadRequestOnPostIfValidationErrorOccurredAsync(Xeption validationException)
        {
            // given
            Link someLink = CreateRandomLink();

            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<Link>(expectedBadRequestObjectResult);

            this.linkProcessingServiceMock.Setup(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(validationException);

            // when
            ActionResult<Link> actualActionResult =
                await this.linksController.PostLinkAsync(someLink, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.linkProcessingServiceMock.Verify(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.linkProcessingServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(ServerExceptions))]
        public async Task ShouldReturnInternalServerErrorOnPostIfServerErrorOccurredAsync(
            Xeption validationException)
        {
            // given
            Link someLink = CreateRandomLink();

            InternalServerErrorObjectResult expectedInternalServerErrorObjectResult =
                InternalServerError(validationException);

            var expectedActionResult =
                new ActionResult<Link>(expectedInternalServerErrorObjectResult);

            this.linkProcessingServiceMock.Setup(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(validationException);

            // when
            ActionResult<Link> actualActionResult =
                await this.linksController.PostLinkAsync(someLink, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.linkProcessingServiceMock.Verify(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.linkProcessingServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReturnUnauthorizedOnPostIfUnauthorizedErrorOccurredAsync()
        {
            // given
            Link someLink = CreateRandomLink();
            string someMessage = GetRandomString();

            var unauthorizedLinkProcessingException =
                new UnauthorizedLinkProcessingException(
                    message: someMessage);

            var linkProcessingValidationException =
                new LinkProcessingValidationException(
                    message: someMessage,
                    innerException: unauthorizedLinkProcessingException);

            UnauthorizedObjectResult expectedUnauthorizedObjectResult =
                Unauthorized(unauthorizedLinkProcessingException);

            var expectedActionResult =
                new ActionResult<Link>(expectedUnauthorizedObjectResult);

            this.linkProcessingServiceMock.Setup(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(linkProcessingValidationException);

            // when
            ActionResult<Link> actualActionResult =
                await this.linksController.PostLinkAsync(someLink, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.linkProcessingServiceMock.Verify(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.linkProcessingServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldReturnFailedDependencyOnPostIfDependencyErrorOccurredAsync(
            Xeption dependencyException)
        {
            // given
            Link someLink = CreateRandomLink();

            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(dependencyException.InnerException);

            var expectedActionResult =
                new ActionResult<Link>(expectedFailedDependencyObjectResult);

            this.linkProcessingServiceMock.Setup(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(dependencyException);

            // when
            ActionResult<Link> actualActionResult =
                await this.linksController.PostLinkAsync(someLink, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.linkProcessingServiceMock.Verify(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()),
                    Times.Once);

            this.linkProcessingServiceMock.VerifyNoOtherCalls();
        }
    }
}
