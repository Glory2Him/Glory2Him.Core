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
using Force.DeepCloner;
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.ApprovalComments
{
    public partial class ApprovalCommentsControllerTests
    {
        [Fact]
        public async Task ShouldReturnRecordsOnGetAsync()
        {
            // given
            IQueryable<ApprovalComment> randomApprovalComments = CreateRandomApprovalComments();
            IQueryable<ApprovalComment> storageApprovalComments = randomApprovalComments.DeepClone();
            IQueryable<ApprovalComment> expectedApprovalComments = storageApprovalComments.DeepClone();

            var expectedObjectResult =
                new OkObjectResult(expectedApprovalComments);

            var expectedActionResult =
                new ActionResult<IQueryable<ApprovalComment>>(expectedObjectResult);

            approvalCommentServiceMock
                .Setup(service => service.RetrieveAllApprovalCommentsAsync(
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalComments);

            // when
            ActionResult<IQueryable<ApprovalComment>> actualActionResult =
                await approvalCommentsController.Get(default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            approvalCommentServiceMock
                .Verify(service => service.RetrieveAllApprovalCommentsAsync(
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            approvalCommentServiceMock.VerifyNoOtherCalls();
        }
    }
}
