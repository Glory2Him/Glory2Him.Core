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
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;
using RESTFulSense.Models;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.ApprovalComments
{
    public partial class ApprovalCommentsControllerTests
    {
        [Fact]
        public async Task ShouldReturnCreatedOnPostAsync()
        {
            // given
            ApprovalComment randomApprovalComment = CreateRandomApprovalComment();
            ApprovalComment inputApprovalComment = randomApprovalComment;
            ApprovalComment addedApprovalComment = inputApprovalComment.DeepClone();
            ApprovalComment expectedApprovalComment = addedApprovalComment.DeepClone();

            var expectedObjectResult =
                new CreatedObjectResult(expectedApprovalComment);

            var expectedActionResult =
                new ActionResult<ApprovalComment>(expectedObjectResult);

            approvalCommentServiceMock
                .Setup(service => service.AddApprovalCommentAsync(
                    inputApprovalComment,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(addedApprovalComment);

            // when
            ActionResult<ApprovalComment> actualActionResult =
                await approvalCommentsController.PostApprovalCommentAsync(randomApprovalComment, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            approvalCommentServiceMock
                .Verify(service => service.AddApprovalCommentAsync(
                    inputApprovalComment,
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            approvalCommentServiceMock.VerifyNoOtherCalls();
        }
    }
}
