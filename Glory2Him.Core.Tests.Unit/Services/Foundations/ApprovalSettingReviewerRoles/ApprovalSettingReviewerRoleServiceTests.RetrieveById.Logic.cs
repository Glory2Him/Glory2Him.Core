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
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingReviewerRoles
{
    public partial class ApprovalSettingReviewerRoleServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveApprovalSettingReviewerRoleByIdAsync()
        {
            // given: any authenticated caller may read a non-deleted policy row
            ApprovalSettingReviewerRole randomApprovalSettingReviewerRole = CreateRandomApprovalSettingReviewerRole();
            ApprovalSettingReviewerRole storageApprovalSettingReviewerRole = randomApprovalSettingReviewerRole;
            storageApprovalSettingReviewerRole.IsDeleted = false;
            ApprovalSettingReviewerRole expectedApprovalSettingReviewerRole = storageApprovalSettingReviewerRole;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    randomApprovalSettingReviewerRole.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalSettingReviewerRole);

            // when
            ApprovalSettingReviewerRole actualApprovalSettingReviewerRole =
                await this.approvalSettingReviewerRoleService.RetrieveApprovalSettingReviewerRoleByIdAsync(
                    randomApprovalSettingReviewerRole.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSettingReviewerRole.Should().BeEquivalentTo(expectedApprovalSettingReviewerRole);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    randomApprovalSettingReviewerRole.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.Reviewer)]
        [InlineData(Roles.Admin)]
        public async Task ShouldRetrieveApprovalSettingReviewerRoleByIdWhenCallerIsAuthenticatedAsync(
            string role)
        {
            // given: policy is readable by every signed-in caller — the rules a
            // submission is judged by are not privileged information
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(role);
            ApprovalSettingReviewerRole randomApprovalSettingReviewerRole = CreateRandomApprovalSettingReviewerRole();
            ApprovalSettingReviewerRole storageApprovalSettingReviewerRole = randomApprovalSettingReviewerRole;
            storageApprovalSettingReviewerRole.IsDeleted = false;
            ApprovalSettingReviewerRole expectedApprovalSettingReviewerRole = storageApprovalSettingReviewerRole;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    randomApprovalSettingReviewerRole.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalSettingReviewerRole);

            // when
            ApprovalSettingReviewerRole actualApprovalSettingReviewerRole =
                await this.approvalSettingReviewerRoleService.RetrieveApprovalSettingReviewerRoleByIdAsync(
                    randomApprovalSettingReviewerRole.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSettingReviewerRole.Should().BeEquivalentTo(expectedApprovalSettingReviewerRole);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    randomApprovalSettingReviewerRole.Id,
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
