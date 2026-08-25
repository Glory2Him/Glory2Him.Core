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
using Force.DeepCloner;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.BibleReferences
{
    public partial class BibleReferencesControllerTests
    {
        [Fact]
        public async Task ShouldReturnRecordOnGetByIdsAsync()
        {
            // given
            BibleReference randomBibleReference = CreateRandomBibleReference();
            BibleReference storageBibleReference = randomBibleReference;
            BibleReference expectedBibleReference = storageBibleReference.DeepClone();

            var expectedObjectResult =
                new OkObjectResult(expectedBibleReference);

            var expectedActionResult =
                new ActionResult<BibleReference>(expectedObjectResult);

            bibleReferenceServiceMock
                .Setup(service => service.RetrieveBibleReferenceByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageBibleReference);

            // when
            ActionResult<BibleReference> actualActionResult =
                await bibleReferencesController.GetBibleReferenceByIdAsync(randomBibleReference.Id, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            bibleReferenceServiceMock
                .Verify(service => service.RetrieveBibleReferenceByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            bibleReferenceServiceMock.VerifyNoOtherCalls();
        }
    }
}
