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
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;
using RESTFulSense.Models;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.BibleReferences
{
    public partial class BibleReferencesControllerTests
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
                new ActionResult<IQueryable<BibleReference>>(expectedInternalServerErrorObjectResult);

            this.bibleReferenceServiceMock.Setup(service =>
                service.RetrieveAllBibleReferencesAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(serverException);

            // when
            ActionResult<IQueryable<BibleReference>> actualActionResult =
                await this.bibleReferencesController.Get(default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.bibleReferenceServiceMock.Verify(service =>
                service.RetrieveAllBibleReferencesAsync(It.IsAny<CancellationToken>()),
                    Times.Once);

            this.bibleReferenceServiceMock.VerifyNoOtherCalls();
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
                new ActionResult<IQueryable<BibleReference>>(expectedFailedDependencyObjectResult);

            this.bibleReferenceServiceMock.Setup(service =>
                service.RetrieveAllBibleReferencesAsync(It.IsAny<CancellationToken>()))
                    .ThrowsAsync(dependencyException);

            // when
            ActionResult<IQueryable<BibleReference>> actualActionResult =
                await this.bibleReferencesController.Get(default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.bibleReferenceServiceMock.Verify(service =>
                service.RetrieveAllBibleReferencesAsync(It.IsAny<CancellationToken>()),
                    Times.Once);

            this.bibleReferenceServiceMock.VerifyNoOtherCalls();
        }
    }
}
