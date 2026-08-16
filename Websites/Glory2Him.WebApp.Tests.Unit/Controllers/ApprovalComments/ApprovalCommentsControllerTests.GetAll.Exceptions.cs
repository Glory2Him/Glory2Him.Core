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
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalComments.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;
using RESTFulSense.Models;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.ApprovalComments
{
    public partial class ApprovalCommentsControllerTests
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
                new ActionResult<IQueryable<ApprovalComment>>(expectedInternalServerErrorObjectResult);

            this.approvalCommentServiceMock.Setup(service =>
                service.RetrieveAllApprovalCommentsAsync(
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serverException);

            // when
            ActionResult<IQueryable<ApprovalComment>> actualActionResult =
                await this.approvalCommentsController.Get(default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalCommentServiceMock.Verify(service =>
                service.RetrieveAllApprovalCommentsAsync(
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalCommentServiceMock.VerifyNoOtherCalls();
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
                new ActionResult<IQueryable<ApprovalComment>>(expectedFailedDependencyObjectResult);

            this.approvalCommentServiceMock.Setup(service =>
                service.RetrieveAllApprovalCommentsAsync(
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dependencyException);

            // when
            ActionResult<IQueryable<ApprovalComment>> actualActionResult =
                await this.approvalCommentsController.Get(default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);

            this.approvalCommentServiceMock.Verify(service =>
                service.RetrieveAllApprovalCommentsAsync(
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalCommentServiceMock.VerifyNoOtherCalls();
        }
    }
}
