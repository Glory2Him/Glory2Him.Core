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
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingPublisherRoles
{
    public partial class ApprovalSettingPublisherRoleServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveApprovalSettingPublisherRoleByIdAsync()
        {
            // given: an authenticated caller in no particular role may read the policy
            ApprovalSettingPublisherRole randomApprovalSettingPublisherRole = CreateRandomApprovalSettingPublisherRole();
            ApprovalSettingPublisherRole storageApprovalSettingPublisherRole = randomApprovalSettingPublisherRole;
            storageApprovalSettingPublisherRole.IsDeleted = false;
            ApprovalSettingPublisherRole expectedApprovalSettingPublisherRole = storageApprovalSettingPublisherRole;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    randomApprovalSettingPublisherRole.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalSettingPublisherRole);

            // when
            ApprovalSettingPublisherRole actualApprovalSettingPublisherRole =
                await this.approvalSettingPublisherRoleService.RetrieveApprovalSettingPublisherRoleByIdAsync(
                    randomApprovalSettingPublisherRole.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSettingPublisherRole.Should().BeEquivalentTo(expectedApprovalSettingPublisherRole);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    randomApprovalSettingPublisherRole.Id,
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
        public async Task ShouldRetrieveApprovalSettingPublisherRoleByIdWhenUserIsInAnyRoleAsync(
            string[] roles)
        {
            // given: the policy is visible to every signed-in caller so submitters can
            // see the rules — no role is required to read
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(roles);

            ApprovalSettingPublisherRole randomApprovalSettingPublisherRole =
                CreateRandomApprovalSettingPublisherRole();

            ApprovalSettingPublisherRole storageApprovalSettingPublisherRole = randomApprovalSettingPublisherRole;
            storageApprovalSettingPublisherRole.IsDeleted = false;
            ApprovalSettingPublisherRole expectedApprovalSettingPublisherRole = storageApprovalSettingPublisherRole;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    randomApprovalSettingPublisherRole.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalSettingPublisherRole);

            // when
            ApprovalSettingPublisherRole actualApprovalSettingPublisherRole =
                await this.approvalSettingPublisherRoleService.RetrieveApprovalSettingPublisherRoleByIdAsync(
                    randomApprovalSettingPublisherRole.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSettingPublisherRole.Should().BeEquivalentTo(expectedApprovalSettingPublisherRole);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    randomApprovalSettingPublisherRole.Id,
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
