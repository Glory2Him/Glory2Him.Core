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
using FluentAssertions;
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
        /// <summary>
        /// The withdrawn row is of no use to a caller — it is the record of something now gone,
        /// and the panel refreshes from the round rather than from this.
        /// </summary>
        [Fact]
        public async Task ShouldReturnNoContentOnDeleteAsync()
        {
            // given
            Guid someRequestId = Guid.NewGuid();
            string someDeletionReason = GetRandomString();

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new ApprovalReviewRequest { Id = someRequestId });

            // when
            ActionResult<ApprovalReviewRequest> actualActionResult =
                await this.approvalReviewRequestsController
                    .DeleteApprovalReviewRequestByIdAsync(
                        someRequestId,
                        someDeletionReason,
                        default);

            // then
            actualActionResult.Result.Should().BeOfType<NoContentResult>();

            this.approvalOrchestrationServiceMock.Verify(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    someRequestId,
                    someDeletionReason,
                    It.IsAny<CancellationToken>()),
                        Times.Once);

            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// Null is the ordinary "no reason supplied" case and must reach the orchestration as
        /// null — coercing it to an empty string would make a length check treat it as blank.
        /// </summary>
        [Fact]
        public async Task ShouldPassAnAbsentDeletionReasonThroughAsNullOnDeleteAsync()
        {
            // given
            Guid someRequestId = Guid.NewGuid();

            this.approvalOrchestrationServiceMock.Setup(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new ApprovalReviewRequest { Id = someRequestId });

            // when
            await this.approvalReviewRequestsController
                .DeleteApprovalReviewRequestByIdAsync(someRequestId, null, default);

            // then
            this.approvalOrchestrationServiceMock.Verify(service =>
                service.WithdrawApprovalReviewRequestAsync(
                    someRequestId,
                    null,
                    It.IsAny<CancellationToken>()),
                        Times.Once);
        }
    }
}
