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
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingRoles
{
    public partial class ApprovalSettingRoleServiceTests
    {
        [Fact]
        public async Task ShouldReplyWithApprovalSettingRoleOnRetrievingApprovalSettingRoleByIdEventAsync()
        {
            // given
            ApprovalSettingRole randomApprovalSettingRole = CreateRandomApprovalSettingRole();
            ApprovalSettingRole storageApprovalSettingRole = randomApprovalSettingRole;
            ApprovalSettingRole expectedApprovalSettingRole = storageApprovalSettingRole;

            var requestEnvelope = new EventEnvelope<ApprovalSettingRole>
            {
                Content = new ApprovalSettingRole { Id = randomApprovalSettingRole.Id }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingRoleByIdAsync(
                    randomApprovalSettingRole.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalSettingRole);

            // when
            EventEnvelope<ApprovalSettingRole>? actualReplyEnvelope =
                await this.approvalSettingRoleService.OnRetrievingApprovalSettingRoleByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedApprovalSettingRole);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingRoleByIdAsync(
                    randomApprovalSettingRole.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.eventEnvelopeFactoryMock.Verify(factory =>
                factory.CreateNextAsync(requestEnvelope, storageApprovalSettingRole),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
