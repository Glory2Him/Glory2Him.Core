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

using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.Approvals;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Approvals
{
    public partial class ApprovalServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveApprovalByIdAsync()
        {
            // given
            Approval randomApproval = CreateRandomApproval();
            Approval storageApproval = randomApproval;
            Approval expectedApproval = storageApproval;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(
                    randomApproval.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApproval);

            // when
            Approval actualApproval =
                await this.approvalService.RetrieveApprovalByIdAsync(
                    randomApproval.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualApproval.Should().BeEquivalentTo(expectedApproval);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByIdAsync(
                    randomApproval.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
