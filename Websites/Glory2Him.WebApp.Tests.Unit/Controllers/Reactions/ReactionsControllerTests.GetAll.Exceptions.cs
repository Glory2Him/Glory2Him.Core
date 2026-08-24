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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.Reactions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;
using RESTFulSense.Models;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.Reactions
{
    public partial class ReactionsControllerTests
    {
        [Theory]
        [MemberData(nameof(ServerExceptions))]
        public async Task ShouldReturnInternalServerErrorOnGetIfServerErrorOccurredAsync(
            Xeption serverException)
        {
            // given
            InternalServerErrorObjectResult expectedInternalServerErrorObjectResult =
                InternalServerError(serverException);

            var expectedActionResult =
                new ActionResult<IQueryable<Reaction>>(expectedInternalServerErrorObjectResult);

            this.reactionServiceMock.Setup(service =>
                service.RetrieveAllReactionsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(serverException);

            // when
            ActionResult<IQueryable<Reaction>> actualActionResult =
                await this.reactionsController.Get(default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.reactionServiceMock.Verify(service =>
                service.RetrieveAllReactionsAsync(It.IsAny<CancellationToken>()),
                    Times.Once);

            this.reactionServiceMock.VerifyNoOtherCalls();
        }
        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldReturnFailedDependencyOnGetIfDependencyErrorOccurredAsync(
            Xeption dependencyException)
        {
            // given
            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(dependencyException.InnerException);

            var expectedActionResult =
                new ActionResult<IQueryable<Reaction>>(expectedFailedDependencyObjectResult);

            this.reactionServiceMock.Setup(service =>
                service.RetrieveAllReactionsAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(dependencyException);

            // when
            ActionResult<IQueryable<Reaction>> actualActionResult =
                await this.reactionsController.Get(default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.reactionServiceMock.Verify(service =>
                service.RetrieveAllReactionsAsync(It.IsAny<CancellationToken>()),
                    Times.Once);

            this.reactionServiceMock.VerifyNoOtherCalls();
        }
    }
}
