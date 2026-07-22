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
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettings
{
    public partial class ApprovalSettingServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveAllApprovalSettingsAsync()
        {
            // given
            IQueryable<ApprovalSetting> randomApprovalSettings = CreateRandomApprovalSettings();
            IQueryable<ApprovalSetting> storageApprovalSettings = randomApprovalSettings;
            IQueryable<ApprovalSetting> expectedApprovalSettings = storageApprovalSettings;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalSettingsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageApprovalSettings);

            // when
            IQueryable<ApprovalSetting> actualApprovalSettings =
                await this.approvalSettingService.RetrieveAllApprovalSettingsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSettings.Should().BeEquivalentTo(expectedApprovalSettings);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalSettingsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
