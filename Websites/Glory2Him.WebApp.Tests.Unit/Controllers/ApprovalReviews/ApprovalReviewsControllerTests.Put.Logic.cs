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

using System.Threading;
using System.Threading.Tasks;
using Force.DeepCloner;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.ApprovalReviews
{
    public partial class ApprovalReviewsControllerTests
    {
        [Fact]
        public async Task ShouldReturnOkOnPutAsync()
        {
            // given
            ApprovalReview randomApprovalReview = CreateRandomApprovalReview();
            ApprovalReview inputApprovalReview = randomApprovalReview;
            ApprovalReview storageApprovalReview = inputApprovalReview.DeepClone();
            ApprovalReview expectedApprovalReview = storageApprovalReview.DeepClone();

            var expectedObjectResult =
                new OkObjectResult(expectedApprovalReview);

            var expectedActionResult =
                new ActionResult<ApprovalReview>(expectedObjectResult);

            approvalReviewServiceMock
                .Setup(service => service.ModifyApprovalReviewAsync(
                    inputApprovalReview,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalReview);

            // when
            ActionResult<ApprovalReview> actualActionResult =
                await approvalReviewsController.PutApprovalReviewAsync(randomApprovalReview, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            approvalReviewServiceMock
                .Verify(service => service.ModifyApprovalReviewAsync(
                    inputApprovalReview,
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            approvalReviewServiceMock.VerifyNoOtherCalls();
        }
    }
}
