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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Securities;
using Glory2Him.WebApp.Tests.Acceptance.Brokers;
using Xunit;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis
{
    /// <summary>
    /// Proves the portal mints the whole Core role vocabulary at startup.
    ///
    /// <para>These assertions used to live in the Tag suite, because Tag was the first entity
    /// exposed and its roles were the only ones seeded. They are not Tag's: <c>SeedData</c> is
    /// the only place a role can be minted — <c>IIdentityBroker</c> assigns and never creates —
    /// so every entity's moderation tier depends on this one seed, and leaving the coverage
    /// filed under one entity is what let the other six go unnoticed.</para>
    ///
    /// <para>An unseeded role is a silent failure. Nothing throws: the row simply does not
    /// exist, so an administrator cannot grant it, nobody holds it, and every gate that would
    /// have admitted it answers as though the caller were unprivileged. The entity is
    /// unmoderatable and the only symptom is a 403 that looks correct.</para>
    /// </summary>
    [Collection(nameof(ApiTestCollection))]
    public class CoreRoleSeedingTests
    {
        private readonly ApiBroker apiBroker;

        public CoreRoleSeedingTests(ApiBroker apiBroker) =>
            this.apiBroker = apiBroker;

        public static TheoryData<EntityType> ScopedRoleEntityTypes()
        {
            var scopedRoleEntityTypes = new TheoryData<EntityType>();

            IEnumerable<EntityType> entityTypes = Enum.GetValues<EntityType>()
                .Where(entityType => entityType != EntityType.Association);

            foreach (EntityType entityType in entityTypes)
            {
                scopedRoleEntityTypes.Add(entityType);
            }

            return scopedRoleEntityTypes;
        }

        [Theory]
        [InlineData(Roles.Administrators)]
        [InlineData(Roles.Reviewers)]
        [InlineData(Roles.Publishers)]
        [InlineData(Roles.ReadOnly)]
        public async Task ShouldSeedGlobalCoreRoleAsync(string roleName)
        {
            // given, when
            bool actualRoleExists = await this.apiBroker.RoleExistsAsync(roleName);

            // then
            actualRoleExists.Should().BeTrue(
                because: $"'{roleName}' is a global tier every entity's gates consult");
        }

        [Theory]
        [MemberData(nameof(ScopedRoleEntityTypes))]
        public async Task ShouldSeedEveryScopedRoleForEntityTypeAsync(EntityType entityType)
        {
            // given
            string[] expectedRoleNames = new[]
            {
                Roles.ReadOnlyFor(entityType),
                Roles.ReviewersFor(entityType),
                Roles.PublishersFor(entityType)
            };

            foreach (string expectedRoleName in expectedRoleNames)
            {
                // when
                bool actualRoleExists = await this.apiBroker.RoleExistsAsync(expectedRoleName);

                // then
                actualRoleExists.Should().BeTrue(
                    because: $"'{expectedRoleName}' is the only way {entityType} moderation can "
                        + "be granted. Without the row an administrator cannot issue it, so the "
                        + "review tier answers 403 to everyone and looks correct doing it");
            }
        }

        /// <summary>
        /// The negative, and it is the one assertion here that is a rule rather than a count.
        /// </summary>
        [Theory]
        [InlineData(EntityType.Association)]
        public async Task ShouldNotSeedScopedRolesForEntityTypeAsync(EntityType entityType)
        {
            // given
            string[] unexpectedRoleNames = new[]
            {
                Roles.ReadOnlyFor(entityType),
                Roles.ReviewersFor(entityType),
                Roles.PublishersFor(entityType)
            };

            foreach (string unexpectedRoleName in unexpectedRoleNames)
            {
                // when
                bool actualRoleExists = await this.apiBroker.RoleExistsAsync(unexpectedRoleName);

                // then
                actualRoleExists.Should().BeFalse(
                    because: $"an {entityType} has no scoped roles of its own (design §14.7 "
                        + "posture A′, §18.6) — every scoped question is answered from its two "
                        + $"endpoints. Minting '{unexpectedRoleName}' would hand an "
                        + "administrator a grant that no gate in the codebase ever asks for, "
                        + "and which therefore silently does nothing");
            }
        }

        [Fact]
        public async Task ShouldGrantTheAdministratorsRoleToTheSeededAdministratorAsync()
        {
            // given, when
            IList<string> actualRoles = await this.apiBroker.GetSeededAdministratorRolesAsync();

            // then
            actualRoles.Should().Contain(Roles.Administrators,
                because: "'Administrators' is the one administrator role since #368 and it "
                    + "opens both surfaces, so an administrator who does not hold it can "
                    + "neither reach /api/admin nor approve or hard delete anything");
        }
    }
}
