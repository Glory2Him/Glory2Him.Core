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
using Glory2Him.Core.Models.Foundations.Comments;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.Comments
{
    public partial class CommentsControllerTests
    {
        [Fact]
        public async Task ShouldReturnRecordOnGetByIdsAsync()
        {
            // given
            Comment randomComment = CreateRandomComment();
            Comment storageComment = randomComment;
            Comment expectedComment = storageComment.DeepClone();

            var expectedObjectResult =
                new OkObjectResult(expectedComment);

            var expectedActionResult =
                new ActionResult<Comment>(expectedObjectResult);

            commentServiceMock
                .Setup(service => service.RetrieveCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageComment);

            // when
            ActionResult<Comment> actualActionResult =
                await commentsController.GetCommentByIdAsync(randomComment.Id, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            commentServiceMock
                .Verify(service => service.RetrieveCommentByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            commentServiceMock.VerifyNoOtherCalls();
        }
    }
}
