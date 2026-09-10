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
    ///
    /// <para>Both the single-name and the MULTI-NAME shapes are covered, because only the second
    /// exercises the <c>IN</c> clause every real caller produces — a review tier is a SET of role
    /// names (§18.6) — and only it can reach the duplication and cross-engine casing hazards the
    /// second test describes.</para>
    ///
    /// <para><b>The EMPTY name set is deliberately absent from here.</b> It is decided one layer
    /// up: <c>IdentityUserService</c> fails closed and never calls the broker, which
    /// <c>IdentityUserServiceTests.ShouldReturnNoUsersWhenNoUsableRoleNamesAreGivenAsync</c>
    /// proves. Measuring what SQL happens to do with a set the broker is never given would be
    /// recording an accident as a contract — see the note on
    /// <see cref="IIdentityCoreStorageBroker.SelectIdentityUsersInRolesAsync"/>.</para>
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

        /// <summary>
        /// The MULTI-ROLE case — more than one name in the <c>IN</c> clause — which is the shape
        /// every real caller uses, because a review tier is composed of several role names
        /// (§18.6) and is handed to this read as a set.
        ///
        /// <para><b>Two hazards live here that the single-name case cannot reach.</b></para>
        ///
        /// <para>The first is CASING ACROSS THE STORE BOUNDARY. The service upper-cases the
        /// requested names in .NET with <c>ToUpperInvariant</c>; the broker matches them against
        /// <c>UPPER(Name)</c> evaluated by SQL Server under the catalogue's collation. Those are
        /// two different casing engines, and an ASCII-only name cannot tell them apart — it would
        /// be a test proving what was never in doubt. So one of the two requested roles is stored
        /// with non-ASCII lower-case letters whose upper-casing each engine has to agree on for
        /// the match to land at all; if a future collation change (or a switch to a case-
        /// sensitive one) broke that agreement, this test is where it surfaces rather than as a
        /// tier that silently resolves to nobody.</para>
        ///
        /// <para>The second is DUPLICATION. Someone holding BOTH requested roles has two rows in
        /// <c>AspNetUserRoles</c>. A read written as a join rather than as the id-set membership
        /// the broker uses would return that person twice, and a caller counting the result to
        /// decide whether a tier has enough reviewers would count one human as two.</para>
        /// </summary>
        [Fact]
        public async Task ShouldReturnMembersOfEveryRequestedRoleWithoutDuplicatingThemAsync()
        {
            // given: two REQUESTED roles - one whose name upper-cases only if SQL Server's UPPER
            // and .NET's ToUpperInvariant agree on non-ASCII letters - and one unrequested role
            string uniqueSuffix = $"{Guid.NewGuid():N}";
            Guid accentedRoleId = Guid.NewGuid();
            Guid plainRoleId = Guid.NewGuid();
            Guid unrequestedRoleId = Guid.NewGuid();

            // stored lower-cased and accented on purpose: 'ü' and 'ç' are the letters the two
            // casing engines have to agree about, and the request below never sends them as
            // stored - it sends what ToUpperInvariant makes of them
            string accentedRoleName = $"süreç-reviewers-{uniqueSuffix}";
            string plainRoleName = $"Tag-Publishers-{uniqueSuffix}";
            string unrequestedRoleName = $"Bystanders-{uniqueSuffix}";

            Guid accentedRoleMemberId = Guid.NewGuid();
            Guid plainRoleMemberId = Guid.NewGuid();
            Guid bothRolesMemberId = Guid.NewGuid();
            Guid unrequestedRoleMemberId = Guid.NewGuid();

            await this.broker.SeedRoleAsync(accentedRoleId, accentedRoleName);
            await this.broker.SeedRoleAsync(plainRoleId, plainRoleName);
            await this.broker.SeedRoleAsync(unrequestedRoleId, unrequestedRoleName);

            await this.broker.SeedUserAsync(
                accentedRoleMemberId, userName: $"accented-member-{uniqueSuffix}");

            await this.broker.SeedUserAsync(
                plainRoleMemberId, userName: $"plain-member-{uniqueSuffix}");

            await this.broker.SeedUserAsync(
                bothRolesMemberId, userName: $"both-roles-member-{uniqueSuffix}");

            await this.broker.SeedUserAsync(
                unrequestedRoleMemberId, userName: $"unrequested-member-{uniqueSuffix}");

            await this.broker.SeedUserRoleAsync(accentedRoleMemberId, accentedRoleId);
            await this.broker.SeedUserRoleAsync(plainRoleMemberId, plainRoleId);
            await this.broker.SeedUserRoleAsync(bothRolesMemberId, accentedRoleId);
            await this.broker.SeedUserRoleAsync(bothRolesMemberId, plainRoleId);
            await this.broker.SeedUserRoleAsync(unrequestedRoleMemberId, unrequestedRoleId);

            // when: both names in one IN clause, upper-cased the way IdentityUserService does it
            List<IdentityUser> matches =
                await this.identityCoreStorageBroker.SelectIdentityUsersInRolesAsync(
                    new[]
                    {
                        accentedRoleName.ToUpperInvariant(),
                        plainRoleName.ToUpperInvariant(),
                    },
                    TestContext.Current.CancellationToken);

            // then: every requested role contributes its members
            matches.Should().ContainSingle(user => user.Id == accentedRoleMemberId,
                because: "the accented name only matches if SQL's UPPER and .NET's " +
                    "ToUpperInvariant agree on 'ü' and 'ç'");

            matches.Should().ContainSingle(user => user.Id == plainRoleMemberId);

            // and: holding both requested roles is still one person
            matches.Should().ContainSingle(user => user.Id == bothRolesMemberId,
                because: "two membership rows for one account must not become two reviewers");

            // and: a role nobody asked for contributes nobody
            matches.Should().NotContain(user => user.Id == unrequestedRoleMemberId);
        }
    }
}
