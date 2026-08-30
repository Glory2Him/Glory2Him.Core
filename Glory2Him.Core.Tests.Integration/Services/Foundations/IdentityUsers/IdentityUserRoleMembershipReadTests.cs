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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Brokers.Storages.Identity;
using Glory2Him.Core.Models.Foundations.IdentityUsers;
using Glory2Him.Core.Tests.Integration.Brokers;
using Xunit;

namespace Glory2Him.Core.Tests.Integration.Services.Foundations.IdentityUsers
{
    /// <summary>
    /// Proves <see cref="IdentityCoreStorageBroker.SelectIdentityUsersInRolesAsync"/> against a
    /// real SQL Server catalogue rather than the mocked context the unit suite uses (issue
    /// #351): the two-table join and the upper-cased role match, the disabled-account exclusion,
    /// and the read staying single-store with no join across it and Core's own schema.
    /// </summary>
    [Collection(IdentityCoreIntegrationCollection.Name)]
    public sealed class IdentityUserRoleMembershipReadTests
    {
        private readonly IIdentityCoreStorageBroker identityCoreStorageBroker;
        private readonly IdentityCoreQueryBroker broker;

        public IdentityUserRoleMembershipReadTests(IdentityCoreQueryBroker broker)
        {
            this.broker = broker;
            this.identityCoreStorageBroker = broker.IdentityCoreStorageBroker;
        }

        [Fact]
        public async Task ShouldReturnOnlyActiveMembersOfTheRequestedRolesAsync()
        {
            // given: two roles, an active member of each, an active non-member, and a disabled
            // member of the requested role — every branch SelectIdentityUsersInRolesAsync joins
            Guid requestedRoleId = Guid.NewGuid();
            Guid otherRoleId = Guid.NewGuid();
            string requestedRoleName = $"Reviewer-{Guid.NewGuid():N}";
            string otherRoleName = $"Other-{Guid.NewGuid():N}";

            Guid activeMemberId = Guid.NewGuid();
            Guid disabledMemberId = Guid.NewGuid();
            Guid nonMemberId = Guid.NewGuid();

            await this.broker.SeedRoleAsync(requestedRoleId, requestedRoleName);
            await this.broker.SeedRoleAsync(otherRoleId, otherRoleName);

            await this.broker.SeedUserAsync(
                activeMemberId, userName: "active-member", isDisabled: false);

            await this.broker.SeedUserAsync(
                disabledMemberId, userName: "disabled-member", isDisabled: true);

            await this.broker.SeedUserAsync(
                nonMemberId, userName: "non-member", isDisabled: false);

            await this.broker.SeedUserRoleAsync(activeMemberId, requestedRoleId);
            await this.broker.SeedUserRoleAsync(disabledMemberId, requestedRoleId);
            await this.broker.SeedUserRoleAsync(nonMemberId, otherRoleId);

            // when: matched on the upper-cased name, the way the orchestration's tier names do
            List<IdentityUser> matches =
                await this.identityCoreStorageBroker.SelectIdentityUsersInRolesAsync(
                    new[] { requestedRoleName.ToUpperInvariant() },
                    TestContext.Current.CancellationToken);

            // then
            matches.Should().ContainSingle(user => user.Id == activeMemberId);
            matches.Should().NotContain(user => user.Id == disabledMemberId);
            matches.Should().NotContain(user => user.Id == nonMemberId);
        }
    }
}
