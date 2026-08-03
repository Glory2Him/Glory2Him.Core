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
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingPublisherRoles
{
    public partial class ApprovalSettingPublisherRoleServiceTests
    {
        [Fact]
        public async Task ShouldReplyWithApprovalSettingPublisherRoleOnRetrievingApprovalSettingPublisherRoleByIdEventAsync()
        {
            // given: the shared do-work runs the visibility posture against the request
            // envelope's caller — a live row needs only an authenticated one
            ApprovalSettingPublisherRole randomApprovalSettingPublisherRole = CreateRandomApprovalSettingPublisherRole();
            ApprovalSettingPublisherRole storageApprovalSettingPublisherRole = randomApprovalSettingPublisherRole;
            storageApprovalSettingPublisherRole.IsDeleted = false;
            ApprovalSettingPublisherRole expectedApprovalSettingPublisherRole = storageApprovalSettingPublisherRole;

            var requestEnvelope = new EventEnvelope<ApprovalSettingPublisherRole>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new ApprovalSettingPublisherRole { Id = randomApprovalSettingPublisherRole.Id }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    randomApprovalSettingPublisherRole.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalSettingPublisherRole);

            // when
            EventEnvelope<ApprovalSettingPublisherRole>? actualReplyEnvelope =
                await this.approvalSettingPublisherRoleService.OnRetrievingApprovalSettingPublisherRoleByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedApprovalSettingPublisherRole);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    randomApprovalSettingPublisherRole.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(requestEnvelope, storageApprovalSettingPublisherRole),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
