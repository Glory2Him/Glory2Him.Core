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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.ContentItems
{
    public partial class ContentItemsControllerTests
    {
        [Fact]
        public async Task ShouldRemoveRecordOnDeleteByIdsAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem storageContentItem = randomContentItem;
            ContentItem expectedContentItem = storageContentItem.DeepClone();
            string inputDeletionReason = GetRandomString();

            var expectedObjectResult =
                new OkObjectResult(expectedContentItem);

            var expectedActionResult =
                new ActionResult<ContentItem>(expectedObjectResult);

            contentItemProcessingServiceMock
                .Setup(service => service.RemoveContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    inputDeletionReason,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItem);

            // when
            ActionResult<ContentItem> actualActionResult =
                await contentItemsController.DeleteContentItemByIdAsync(randomContentItem.Id, inputDeletionReason, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            contentItemProcessingServiceMock
                .Verify(service => service.RemoveContentItemByIdAsync(
                    It.IsAny<Guid>(),
                    inputDeletionReason,
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            contentItemProcessingServiceMock.VerifyNoOtherCalls();
        }
    }
}
