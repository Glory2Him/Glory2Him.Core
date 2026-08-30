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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Securities;
using Glory2Him.WebApp.Tests.Acceptance.Brokers;
using Xunit;

namespace Glory2Him.WebApp.Tests.Acceptance.Data
{
    /// <summary>
    /// The rename migration, run against a POPULATED pre-#368 Identity store.
    ///
    /// <para><b>Why this needs a test the rest of the suite cannot give it.</b>
    /// <c>PluraliseRoleNamesAndCollapseAdmin</c> is the only code in #368 that rewrites live
    /// security grants: it renames role rows in place, moves every <c>Admin</c> membership onto
    /// the <c>Administrators</c> row, and drops <c>Admin</c>. The acceptance suite creates its
    /// Identity catalogue empty and migrates it on the way up, so every one of those statements
    /// runs against zero rows there — a syntax error would surface, and nothing else would. The
    /// one scenario the migration exists for is the one scenario booting the host never
    /// constructs.</para>
    ///
    /// <para>The failure this guards against is silent. If a rename does not carry its
    /// memberships, every holder keeps a role and loses the capability it granted, and nothing
    /// throws: the row simply is not the row the gates now compose, so each gate answers as
    /// though the caller were unprivileged. Exactly the symptom
    /// <see cref="Apis.CoreRoleSeedingTests"/> describes for a role that was never seeded, and
    /// exactly as invisible.</para>
    ///
    /// <para>So the rehearsal builds the store as it stood before #368 — both administrator
    /// roles, the singular scoped and content-type names, and users whose grants span the cases
    /// that differ — applies the one migration, and reads the result back. See
    /// <see cref="RoleVocabularyMigrationRehearsal"/> for the arrangement itself.</para>
    /// </summary>
    [Collection(nameof(ApiTestCollection))]
    public class RoleVocabularyMigrationTests : IClassFixture<RoleVocabularyMigrationRehearsal>
    {
        private readonly RoleVocabularyMigrationRehearsal rehearsal;

        public RoleVocabularyMigrationTests(RoleVocabularyMigrationRehearsal rehearsal) =>
            this.rehearsal = rehearsal;

        public static TheoryData<string, string> RenamedRoleNames()
        {
            var renamedRoleNames = new TheoryData<string, string>
            {
                { "Reviewer", Roles.Reviewers },
                { "Publisher", Roles.Publishers },
                { "Tag-Reviewer", Roles.ReviewersFor(EntityType.Tag) },
                { "Tag-Publisher", Roles.PublishersFor(EntityType.Tag) },
                { "ContentItem-Reviewer", Roles.ReviewersFor(EntityType.ContentItem) },
                {
                    "ContentItem-Story-Reviewer",
                    Roles.ReviewersFor(EntityType.ContentItem, ContentType.Story)
                },
                {
                    "ContentItem-Testimony-Publisher",
                    Roles.PublishersFor(EntityType.ContentItem, ContentType.Testimony)
                }
            };

            return renamedRoleNames;
        }

        /// <summary>
        /// The block role is the one capability that does not move, at either tier (§18.6), so a
        /// migration that pluralised it would be as wrong as one that missed a rename.
        /// </summary>
        public static TheoryData<string> UnchangedRoleNames()
        {
            var unchangedRoleNames = new TheoryData<string>
            {
                Roles.ReadOnly,
                Roles.ReadOnlyFor(EntityType.Tag),
                Roles.ReadOnlyFor(EntityType.ContentItem)
            };

            return unchangedRoleNames;
        }

        [Theory]
        [MemberData(nameof(RenamedRoleNames))]
        public void ShouldRenameEachRoleInPlace(string oldRoleName, string newRoleName)
        {
            // given, when
            IReadOnlyDictionary<string, int> actualRoles = this.rehearsal.MigratedRoleMemberCounts;

            // then
            actualRoles.Should().ContainKey(newRoleName,
                because: $"'{oldRoleName}' is renamed to '{newRoleName}', not left behind");

            actualRoles.Should().NotContainKey(oldRoleName,
                because: $"'{oldRoleName}' names a capability no gate composes any more, and a "
                    + "row nothing checks is a grant an administrator can still hand out");
        }

        [Theory]
        [MemberData(nameof(UnchangedRoleNames))]
        public void ShouldLeaveTheBlockRoleSingular(string roleName)
        {
            // given, when
            IReadOnlyDictionary<string, int> actualRoles = this.rehearsal.MigratedRoleMemberCounts;

            // then
            actualRoles.Should().ContainKey(roleName,
                because: "ReadOnly names a state its holder is in rather than a group of "
                    + "people, so it stays singular at every tier (§18.6)");
        }

        /// <summary>
        /// The whole point of renaming in place rather than re-seeding: <c>AspNetUserRoles</c>
        /// keys on <c>RoleId</c>, so rewriting the name carries the membership with it.
        /// </summary>
        [Fact]
        public void ShouldCarryEveryScopedMembershipAcrossTheRename()
        {
            // given
            string expectedTagReviewersRole = Roles.ReviewersFor(EntityType.Tag);
            string expectedTagPublishersRole = Roles.PublishersFor(EntityType.Tag);

            string expectedNarrowRole =
                Roles.ReviewersFor(EntityType.ContentItem, ContentType.Story);

            // when
            IReadOnlyList<string> actualScopedReviewerRoles =
                this.rehearsal.RolesHeldBy(RoleVocabularyMigrationRehearsal.ScopedReviewerId);

            IReadOnlyList<string> actualNarrowReviewerRoles =
                this.rehearsal.RolesHeldBy(RoleVocabularyMigrationRehearsal.NarrowReviewerId);

            // then
            actualScopedReviewerRoles.Should().BeEquivalentTo(
                new[] { expectedTagReviewersRole, expectedTagPublishersRole },
                because: "an in-place rename changes the row's name and not its id, so both "
                    + "grants survive it untouched");

            actualNarrowReviewerRoles.Should().BeEquivalentTo(
                new[] { expectedNarrowRole },
                because: "the content-type tier is renamed by the same suffix rewrite as the "
                    + "entity tier, so its holders carry across too");
        }

        /// <summary>
        /// <c>Admin</c> is the one row that cannot simply be renamed — <c>Administrators</c>
        /// already exists and <c>NormalizedName</c> is unique — so its members are moved and the
        /// row is dropped. This is the step that must not lose anybody.
        /// </summary>
        [Fact]
        public void ShouldMoveEveryAdminHolderOntoTheAdministratorsRole()
        {
            // given, when
            IReadOnlyList<string> actualCoreOnlyAdministratorRoles =
                this.rehearsal.RolesHeldBy(RoleVocabularyMigrationRehearsal.CoreOnlyAdministratorId);

            IReadOnlyList<string> actualSiteAdministratorRoles =
                this.rehearsal.RolesHeldBy(RoleVocabularyMigrationRehearsal.SiteAdministratorId);

            // then
            actualCoreOnlyAdministratorRoles.Should().BeEquivalentTo(
                new[] { Roles.Administrators },
                because: "somebody who held only Core's 'Admin' must come out holding the one "
                    + "administrator role — dropping the row without moving them would strip "
                    + "the moderation tier from every moderator on the site");

            actualSiteAdministratorRoles.Should().BeEquivalentTo(
                new[] { Roles.Administrators },
                because: "somebody who already held both must end up with one row and not two "
                    + "— AspNetUserRoles is keyed on (UserId, RoleId), so a merge that did not "
                    + "check would fail the migration outright");
        }

        [Fact]
        public void ShouldDropTheAdminRoleAndEverythingHangingOffIt()
        {
            // given, when
            IReadOnlyDictionary<string, int> actualRoles = this.rehearsal.MigratedRoleMemberCounts;

            // then
            actualRoles.Should().NotContainKey("Admin",
                because: "there is no Admin role after #368; leaving the row would let an "
                    + "administrator grant a name that opens nothing");

            this.rehearsal.MigratedAdminRoleClaimCount.Should().Be(0,
                because: "a claim on a dropped role is unreachable state, and leaving it "
                    + "behind would block the drop on the foreign key besides");
        }

        /// <summary>
        /// The sweep in the migration is by SUFFIX rather than by a listed set, so this asks the
        /// question the per-name theories cannot: that nothing was missed. A name minted by a
        /// release later than the migration is caught by a suffix and missed by a list.
        /// </summary>
        [Fact]
        public void ShouldLeaveNoSingularCapabilityNameBehind()
        {
            // given, when
            IReadOnlyList<string> actualSurvivingSingularRoles =
                this.rehearsal.MigratedRoleMemberCounts.Keys
                    .Where(IsSingularCapabilityName)
                    .OrderBy(roleName => roleName)
                    .ToList();

            // then
            actualSurvivingSingularRoles.Should().BeEmpty(
                because: "every capability is plural after #368, and a singular row that "
                    + "survives still holds whatever memberships it had — its holders keep a "
                    + "role and silently lose what it granted");
        }

        private static bool IsSingularCapabilityName(string roleName) =>
            roleName is "Admin" or "Reviewer" or "Publisher"
                || roleName.EndsWith("-Reviewer", System.StringComparison.Ordinal)
                || roleName.EndsWith("-Publisher", System.StringComparison.Ordinal);
    }
}
