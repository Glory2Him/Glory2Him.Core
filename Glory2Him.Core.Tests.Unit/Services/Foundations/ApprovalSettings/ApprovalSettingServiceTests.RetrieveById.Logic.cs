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
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettings
{
    public partial class ApprovalSettingServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveApprovalSettingByIdAsync()
        {
            // given
            ApprovalSetting randomApprovalSetting = CreateRandomApprovalSetting();
            ApprovalSetting storageApprovalSetting = randomApprovalSetting;
            storageApprovalSetting.IsDeleted = false;
            ApprovalSetting expectedApprovalSetting = storageApprovalSetting;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    randomApprovalSetting.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalSetting);

            // when
            ApprovalSetting actualApprovalSetting =
                await this.approvalSettingService.RetrieveApprovalSettingByIdAsync(
                    randomApprovalSetting.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSetting.Should().BeEquivalentTo(expectedApprovalSetting);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    randomApprovalSetting.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(AuthenticatedRoleSets))]
        public async Task ShouldRetrieveApprovalSettingByIdWhenUserIsAuthenticatedAsync(
            string[] roles)
        {
            // given: approval settings are the published rules of the submission process —
            // any signed-in caller may read them, whatever their role
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(roles);
            ApprovalSetting randomApprovalSetting = CreateRandomApprovalSetting();
            ApprovalSetting storageApprovalSetting = randomApprovalSetting;
            storageApprovalSetting.IsDeleted = false;
            ApprovalSetting expectedApprovalSetting = storageApprovalSetting;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    randomApprovalSetting.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalSetting);

            // when
            ApprovalSetting actualApprovalSetting =
                await this.approvalSettingService.RetrieveApprovalSettingByIdAsync(
                    randomApprovalSetting.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSetting.Should().BeEquivalentTo(expectedApprovalSetting);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    randomApprovalSetting.Id,
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
