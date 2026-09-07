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
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.ContentItems
{
    /// <summary>
    /// The failure taxonomy of the group collection read.
    ///
    /// <para>These exist because the route had none. It was added carrying only the dependency and
    /// service arms, so a caller's malformed group id escaped the action and ASP.NET answered 500 -
    /// a server fault, and a server-fault log, for bad input - while the sibling
    /// <c>Groups/{groupId}/Latest</c> route on the same controller answered 400 for the same thing.
    /// Nothing in the suite noticed, because no test exercised this route's failures.</para>
    ///
    /// <para>Driven by the shared <c>ValidationExceptions</c> data, so the processing and the
    /// DEPENDENCY validation exceptions are both covered - they are unrelated types and each needs
    /// its own arm.</para>
    ///
    /// <para>There is deliberately NO not-found case: an unknown group is an empty list on a
    /// collection read, not an error.</para>
    /// </summary>
    public partial class ContentItemsControllerTests
    {
        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldReturnBadRequestOnGetByGroupIfValidationErrorOccurredAsync(
            Xeption validationException)
        {
            // given
            Guid someGroupId = Guid.NewGuid();

            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<IReadOnlyList<ContentItem>>(expectedBadRequestObjectResult);

            this.contentItemProcessingServiceMock.Setup(service =>
                service.RetrieveContentItemsByGroupIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<IReadOnlyList<ContentItem>> actualActionResult =
                await this.contentItemsController.GetContentItemsByGroupId(someGroupId, default);

            // then: 400, not the 500 this route used to answer
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.contentItemProcessingServiceMock.Verify(service =>
                service.RetrieveContentItemsByGroupIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemProcessingServiceMock.VerifyNoOtherCalls();
        }
    }
}
