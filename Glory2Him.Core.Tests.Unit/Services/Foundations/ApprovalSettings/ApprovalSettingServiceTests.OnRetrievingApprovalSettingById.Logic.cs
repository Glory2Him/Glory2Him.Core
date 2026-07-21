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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettings
{
    public partial class ApprovalSettingServiceTests
    {
        [Fact]
        public async Task ShouldReplyWithApprovalSettingOnRetrievingApprovalSettingByIdEventAsync()
        {
            // given
            ApprovalSetting randomApprovalSetting = CreateRandomApprovalSetting();
            ApprovalSetting storageApprovalSetting = randomApprovalSetting;
            ApprovalSetting expectedApprovalSetting = storageApprovalSetting;

            var requestEnvelope = new EventEnvelope<ApprovalSetting>
            {
                Content = new ApprovalSetting { Id = randomApprovalSetting.Id }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    randomApprovalSetting.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalSetting);

            // when
            EventEnvelope<ApprovalSetting>? actualReplyEnvelope =
                await this.approvalSettingService.OnRetrievingApprovalSettingByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedApprovalSetting);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingByIdAsync(
                    randomApprovalSetting.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.eventEnvelopeFactoryMock.Verify(factory =>
                factory.CreateNextAsync(requestEnvelope, storageApprovalSetting),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
