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

using System.Linq;
using FluentAssertions;
using Glory2Him.WebApp.Models.Foundations.Users;
using Glory2Him.WebApp.Tests.Acceptance.Brokers;
using Xunit;

namespace Glory2Him.WebApp.Tests.Acceptance.Data
{
    /// <summary>
    /// What the username rename of #378 actually did to a populated store, read back off
    /// <see cref="UserNameRenameMigrationRehearsal"/>.
    ///
    /// <para>Every assertion here is about SQL, which no other test in the solution executes
    /// against rows. The service-level tests prove the application refuses an email as a username;
    /// these prove the migration repairs the ones already stored, and refuses the database it
    /// cannot repair.</para>
    /// </summary>
    // ApiBroker's collection fixture DROPS EVERY CATALOGUE in AcceptanceDatabaseBroker.DatabaseNames
    // in its constructor — including the two this rehearsal creates. Without this attribute the
    // class forms its own xUnit collection and runs concurrently with that drop: measured, the drop
    // and this rehearsal's CREATE DATABASE were 439 ms apart on one run, with the host's own create
    // interleaved between them. RoleVocabularyMigrationTests carries the same attribute for the
    // same reason.
    [Collection(nameof(ApiTestCollection))]
    public class UserNameRenameMigrationTests
        : IClassFixture<UserNameRenameMigrationRehearsal>
    {
        private readonly UserNameRenameMigrationRehearsal rehearsal;

        public UserNameRenameMigrationTests(UserNameRenameMigrationRehearsal rehearsal) =>
            this.rehearsal = rehearsal;

        [Fact]
        public void ShouldClaimTheEmailLocalPartWhenItIsFreeAndLegal()
        {
            // given . when
            string actualUserName =
                this.rehearsal.UserNamesById[UserNameRenameMigrationRehearsal.ClaimsLocalPartId];

            // then
            actualUserName.Should().Be("pat");
        }

        [Fact]
        public void ShouldLeaveALegalUserNameExactlyAsItWas()
        {
            // given . when
            string actualUserName =
                this.rehearsal.UserNamesById[UserNameRenameMigrationRehearsal.UntouchedId];

            // then
            actualUserName.Should().Be("already.legal-1_x");
        }

        // UserNameIndex is unique, so two accounts whose addresses share a local part cannot both
        // claim it. Neither may — taking it for whichever row the engine happened to reach first
        // would make the outcome depend on plan order.
        [Fact]
        public void ShouldGiveNeitherSideOfALocalPartCollisionTheContestedName()
        {
            // given . when
            string first =
                this.rehearsal.UserNamesById[UserNameRenameMigrationRehearsal.CollidesOnLocalPartId];

            string second =
                this.rehearsal.UserNamesById[UserNameRenameMigrationRehearsal.RivalForLocalPartId];

            // then
            first.Should().NotBe("chris");
            second.Should().NotBe("chris");
            first.Should().StartWith("user-");
            second.Should().StartWith("user-");
            first.Should().NotBe(second);
        }

        // The defect that shipped. The email's local part carries U+00E9, which the database
        // collation sorts inside the a-z range — so a collation-ordered LIKE class admitted it and
        // the migration minted a username Identity's ASCII-only set refuses.
        [Fact]
        public void ShouldNotMintAUserNameFromAnAccentedEmailLocalPart()
        {
            // given . when
            string actualUserName =
                this.rehearsal.UserNamesById[UserNameRenameMigrationRehearsal.AccentedLocalPartId];

            // then
            actualUserName.Should().StartWith("user-");
        }

        // The invariant the whole migration exists to establish, asserted over every row rather
        // than case by case: after it runs, nothing in the store is a name the application would
        // refuse to write.
        [Fact]
        public void ShouldLeaveEveryUserNameSpellableInTheInstalledCharacterSet()
        {
            // given
            string allowedCharacters =
                UserNameRule.WithoutProhibitedCharacter(
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+");

            // when
            var actualUserNames = this.rehearsal.UserNamesById.Values.ToList();

            // then
            actualUserNames.Should().NotBeEmpty();

            actualUserNames.Should().OnlyContain(userName =>
                userName.Length > 0 && userName.All(character =>
                    allowedCharacters.Contains(character)));
        }

        // Both halves or neither. NormalizedUserName is what the sign-in lookup matches, so a row
        // renamed in UserName alone would look repaired and match nothing.
        [Fact]
        public void ShouldCarryEveryRenameIntoTheNormalizedColumnToo()
        {
            // given . when
            var actual = this.rehearsal.UserNamesById;
            var actualNormalized = this.rehearsal.NormalizedUserNamesById;

            // then
            actual.Should().NotBeEmpty();

            foreach (var renamed in actual)
            {
                actualNormalized[renamed.Key].Should().Be(renamed.Value.ToUpperInvariant());
            }
        }

        // The population the ISNULL in NeedsRename admits: NormalizedUserName never populated, so
        // the ADMIN exclusion answers UNKNOWN. Both rows want "drew", and UserNameIndex is unique
        // — so if the rival subquery cannot see them, the migration dies on the index.
        [Fact]
        public void ShouldResolveACollisionBetweenTwoRowsWhoseNormalizedNameWasNeverSet()
        {
            // given . when
            string first =
                this.rehearsal.UserNamesById[UserNameRenameMigrationRehearsal.NullNormalizedFirstId];

            string second =
                this.rehearsal.UserNamesById[UserNameRenameMigrationRehearsal.NullNormalizedSecondId];

            // then
            first.Should().NotBe("drew");
            second.Should().NotBe("drew");
            first.Should().NotBe(second);
        }

        // The blank-address test must not over-refuse. A wholly non-Latin address resolves through
        // the sign-in lookup exactly like any other, so the account is renamed like any other; an
        // earlier form of the test refused the whole deploy over it.
        [Fact]
        public void ShouldRenameRatherThanRefuseAnAccountWhoseAddressIsNonLatin()
        {
            // given . when
            string actualUserName =
                this.rehearsal.UserNamesById[UserNameRenameMigrationRehearsal.NonLatinAddressId];

            // then: renamed, and the store migrated at all - a refusal would have failed every
            // test in this class, since the whole rehearsal shares one migration.
            actualUserName.Should().StartWith("user-");
        }

        // A rename takes the username away, so the address is all that is left to sign in with.
        // A store holding an account with neither must stop the deploy rather than be repaired
        // into a lockout.
        [Fact]
        public void ShouldRefuseToMigrateAStoreHoldingAnAccountWithNoEmailToFallBackOn()
        {
            // given . when
            string actualMessage = this.rehearsal.BlockedFailureMessage;

            // then
            actualMessage.Should().Contain("Issue #378");
            actualMessage.Should().Contain("lock these accounts out");

            // Named, not merely counted — an operator stopped by this cannot start the host to go
            // looking for the rows themselves. Compared case-insensitively because SQL Server
            // renders a uniqueidentifier upper-case and Guid.ToString() renders it lower.
            actualMessage.Should().ContainEquivalentOf(
                UserNameRenameMigrationRehearsal.BlockedWithoutEmailId.ToString(),
                because: "the throw has to say which account is blocking the deploy");

            // And counted as well as named, because the list is capped: a deploy blocked by
            // hundreds of accounts must not read as though it were blocked by thirty-one.
            actualMessage.Should().Contain(
                $"{UserNameRenameMigrationRehearsal.BlockedAccounts} account(s)");

            // And the count includes the row whose address is non-empty but invisible, which takes
            // the strip branch of the emptiness test rather than its IS NULL branch. Without it,
            // deleting the strip entirely would leave every blocked-store assertion green.
            actualMessage.Should().ContainEquivalentOf(
                UserNameRenameMigrationRehearsal.BlockedInvisibleAddressId.ToString(),
                because: "an address made only of invisible characters is not one anybody can type");
        }
    }
}
