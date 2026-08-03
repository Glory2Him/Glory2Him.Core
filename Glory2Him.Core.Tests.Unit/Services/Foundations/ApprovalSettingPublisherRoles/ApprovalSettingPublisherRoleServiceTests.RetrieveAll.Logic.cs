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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        public async Task ShouldRetrieveAllApprovalSettingPublisherRolesAsync()
        {
            // given: an authenticated caller sees every non-deleted row
            IQueryable<ApprovalSettingPublisherRole> randomApprovalSettingPublisherRoles = CreateRandomApprovalSettingPublisherRoles();

            foreach (ApprovalSettingPublisherRole approvalSettingPublisherRole in randomApprovalSettingPublisherRoles)
            {
                approvalSettingPublisherRole.IsDeleted = false;
            }

            IQueryable<ApprovalSettingPublisherRole> storageApprovalSettingPublisherRoles = randomApprovalSettingPublisherRoles;
            IQueryable<ApprovalSettingPublisherRole> expectedApprovalSettingPublisherRoles = storageApprovalSettingPublisherRoles;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalSettingPublisherRolesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageApprovalSettingPublisherRoles);

            // when
            IQueryable<ApprovalSettingPublisherRole> actualApprovalSettingPublisherRoles =
                await this.approvalSettingPublisherRoleService.RetrieveAllApprovalSettingPublisherRolesAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSettingPublisherRoles.Should().BeEquivalentTo(expectedApprovalSettingPublisherRoles);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalSettingPublisherRolesAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(AuthenticatedRoleSets))]
        public async Task ShouldRetrieveAllNonDeletedApprovalSettingPublisherRolesWhenUserIsAuthenticatedAsync(
            string[] roles)
        {
            // given: every signed-in caller sees the live policy, but never a deleted row
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(roles);

            ApprovalSettingPublisherRole liveApprovalSettingPublisherRole =
                CreateRandomApprovalSettingPublisherRole();

            liveApprovalSettingPublisherRole.IsDeleted = false;

            ApprovalSettingPublisherRole deletedApprovalSettingPublisherRole =
                CreateRandomApprovalSettingPublisherRole();

            deletedApprovalSettingPublisherRole.IsDeleted = true;

            IQueryable<ApprovalSettingPublisherRole> storageApprovalSettingPublisherRoles =
                new List<ApprovalSettingPublisherRole>
                {
                    liveApprovalSettingPublisherRole,
                    deletedApprovalSettingPublisherRole
                }.AsQueryable();

            IQueryable<ApprovalSettingPublisherRole> expectedApprovalSettingPublisherRoles =
                new List<ApprovalSettingPublisherRole>
                {
                    liveApprovalSettingPublisherRole
                }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalSettingPublisherRolesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageApprovalSettingPublisherRoles);

            // when
            IQueryable<ApprovalSettingPublisherRole> actualApprovalSettingPublisherRoles =
                await this.approvalSettingPublisherRoleService.RetrieveAllApprovalSettingPublisherRolesAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSettingPublisherRoles.Should().BeEquivalentTo(expectedApprovalSettingPublisherRoles);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalSettingPublisherRolesAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldRetrieveNoApprovalSettingPublisherRolesWhenCallerIsAnonymousAsync(
            SecurityContext invalidSecurityContext)
        {
            // given: an anonymous collection read filters to nothing rather than erroring,
            // so it never reveals how many policy rows exist
            this.ambientSecurityContext = invalidSecurityContext;

            IQueryable<ApprovalSettingPublisherRole> storageApprovalSettingPublisherRoles =
                CreateRandomApprovalSettingPublisherRoles();

            foreach (ApprovalSettingPublisherRole approvalSettingPublisherRole in storageApprovalSettingPublisherRoles)
            {
                approvalSettingPublisherRole.IsDeleted = false;
            }

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllApprovalSettingPublisherRolesAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageApprovalSettingPublisherRoles);

            // when
            IQueryable<ApprovalSettingPublisherRole> actualApprovalSettingPublisherRoles =
                await this.approvalSettingPublisherRoleService.RetrieveAllApprovalSettingPublisherRolesAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSettingPublisherRoles.Should().BeEmpty();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllApprovalSettingPublisherRolesAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
