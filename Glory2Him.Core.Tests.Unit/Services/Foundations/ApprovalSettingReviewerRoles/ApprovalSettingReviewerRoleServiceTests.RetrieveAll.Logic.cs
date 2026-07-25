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
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingReviewerRoles
{
    public partial class ApprovalSettingReviewerRoleServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveAllApprovalSettingReviewerRolesAsync()
        {
            // given
            IQueryable<ApprovalSettingReviewerRole> randomApprovalSettingReviewerRoles = CreateRandomApprovalSettingReviewerRoles();
            IQueryable<ApprovalSettingReviewerRole> storageApprovalSettingReviewerRoles = randomApprovalSettingReviewerRoles;
            IQueryable<ApprovalSettingReviewerRole> expectedApprovalSettingReviewerRoles = storageApprovalSettingReviewerRoles;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalSettingReviewerRolesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageApprovalSettingReviewerRoles);

            // when
            IQueryable<ApprovalSettingReviewerRole> actualApprovalSettingReviewerRoles =
                await this.approvalSettingReviewerRoleService.RetrieveAllApprovalSettingReviewerRolesAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSettingReviewerRoles.Should().BeEquivalentTo(expectedApprovalSettingReviewerRoles);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalSettingReviewerRolesAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
