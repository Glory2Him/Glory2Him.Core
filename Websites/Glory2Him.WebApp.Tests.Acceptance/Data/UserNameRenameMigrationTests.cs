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
        }
    }
}
