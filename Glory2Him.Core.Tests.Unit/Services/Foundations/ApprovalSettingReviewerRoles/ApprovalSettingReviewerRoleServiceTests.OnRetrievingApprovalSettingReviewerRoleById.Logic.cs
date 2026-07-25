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
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingReviewerRoles
{
    public partial class ApprovalSettingReviewerRoleServiceTests
    {
        [Fact]
        public async Task ShouldReplyWithApprovalSettingReviewerRoleOnRetrievingApprovalSettingReviewerRoleByIdEventAsync()
        {
            // given
            ApprovalSettingReviewerRole randomApprovalSettingReviewerRole = CreateRandomApprovalSettingReviewerRole();
            ApprovalSettingReviewerRole storageApprovalSettingReviewerRole = randomApprovalSettingReviewerRole;
            ApprovalSettingReviewerRole expectedApprovalSettingReviewerRole = storageApprovalSettingReviewerRole;

            var requestEnvelope = new EventEnvelope<ApprovalSettingReviewerRole>
            {
                Content = new ApprovalSettingReviewerRole { Id = randomApprovalSettingReviewerRole.Id }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    randomApprovalSettingReviewerRole.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalSettingReviewerRole);

            // when
            EventEnvelope<ApprovalSettingReviewerRole>? actualReplyEnvelope =
                await this.approvalSettingReviewerRoleService.OnRetrievingApprovalSettingReviewerRoleByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedApprovalSettingReviewerRole);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    randomApprovalSettingReviewerRole.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.eventEnvelopeFactoryMock.Verify(factory =>
                factory.CreateNextAsync(requestEnvelope, storageApprovalSettingReviewerRole),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
