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
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RESTFulSense.Clients.Extensions;
using RESTFulSense.Models;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Controllers.ApprovalReviewRequests
{
    public partial class ApprovalReviewRequestsControllerTests
    {
        // Four of the five refusals below arrive as ApprovalOrchestrationValidationException and
        // are told apart ONLY by their inner exception. A catch ladder whose clauses were
        // reordered would still compile, still throw the same outer type, and answer 400 where it
        // should answer 404 or 401 — which is why each case names its inner exception rather than
        // sharing one theory of outer types.
        [Theory]
        [MemberData(nameof(NotFoundExceptions))]
        public async Task ShouldReturnNotFoundOnDeleteIfTheRequestDoesNotExistAsync(
            Xeption validationException)
        {
            // given
            NotFoundObjectResult expectedNotFoundObjectResult =
                NotFound(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<ApprovalReviewRequest>(expectedNotFoundObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<ApprovalReviewRequest> actualActionResult =
                await this.approvalReviewRequestsController
                    .DeleteApprovalReviewRequestByIdAsync(Guid.NewGuid(), null, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);
        }

        [Theory]
        [MemberData(nameof(UnauthorizedExceptions))]
        public async Task ShouldReturnUnauthorizedOnDeleteIfCallerIsOutsideTheRequestingTierAsync(
            Xeption validationException)
        {
            // given
            UnauthorizedObjectResult expectedUnauthorizedObjectResult =
                Unauthorized(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<ApprovalReviewRequest>(expectedUnauthorizedObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<ApprovalReviewRequest> actualActionResult =
                await this.approvalReviewRequestsController
                    .DeleteApprovalReviewRequestByIdAsync(Guid.NewGuid(), null, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);
        }

        // Includes the refusal a withdrawal gets once its invitation has been ANSWERED
        // (design 7.9 rule 5) - the one refusal on this endpoint a moderator can reach by
        // ordinary use, from a panel a few seconds stale.
        [Theory]
        [MemberData(nameof(BadRequestExceptions))]
        public async Task ShouldReturnBadRequestOnDeleteIfValidationErrorOccurredAsync(
            Xeption validationException)
        {
            // given
            BadRequestObjectResult expectedBadRequestObjectResult =
                BadRequest(validationException.InnerException);

            var expectedActionResult =
                new ActionResult<ApprovalReviewRequest>(expectedBadRequestObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(validationException);

            // when
            ActionResult<ApprovalReviewRequest> actualActionResult =
                await this.approvalReviewRequestsController
                    .DeleteApprovalReviewRequestByIdAsync(Guid.NewGuid(), null, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldReturnFailedDependencyOnDeleteIfDependencyErrorOccurredAsync(
            Xeption dependencyException)
        {
            // given
            FailedDependencyObjectResult expectedFailedDependencyObjectResult =
                FailedDependency(dependencyException.InnerException);

            var expectedActionResult =
                new ActionResult<ApprovalReviewRequest>(expectedFailedDependencyObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dependencyException);

            // when
            ActionResult<ApprovalReviewRequest> actualActionResult =
                await this.approvalReviewRequestsController
                    .DeleteApprovalReviewRequestByIdAsync(Guid.NewGuid(), null, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);
        }

        [Theory]
        [MemberData(nameof(ServerExceptions))]
        public async Task ShouldReturnInternalServerErrorOnDeleteIfServerErrorOccurredAsync(
            Xeption serverException)
        {
            // given
            InternalServerErrorObjectResult expectedInternalServerErrorObjectResult =
                InternalServerError(serverException);

            var expectedActionResult =
                new ActionResult<ApprovalReviewRequest>(
                    expectedInternalServerErrorObjectResult);

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(serverException);

            // when
            ActionResult<ApprovalReviewRequest> actualActionResult =
                await this.approvalReviewRequestsController
                    .DeleteApprovalReviewRequestByIdAsync(Guid.NewGuid(), null, default);

            // then
            actualActionResult.ShouldBeEquivalentTo(expectedActionResult);
        }
    }
}
