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
using Glory2Him.WebApp.Models.Foundations.Users;
using Glory2Him.WebApp.Tests.Acceptance.Brokers;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Accounts
{
    /// <summary>
    /// Pins the character set the running host actually enforces (issue #378, design §18.3.1).
    ///
    /// <para>Everything else about the rule is tested a layer above Identity — the view services
    /// refuse <c>@</c> before <c>UserManager</c> is ever reached — so nothing else in the suite
    /// fails if <c>PortalRegistration</c>'s narrowing is deleted. That is not merely a coverage
    /// gap: <c>SeparateUserNamesFromEmailAddresses</c> decides which rows it owes a rename by
    /// asking whether a username is spellable in THIS set, and its final guard certifies the
    /// database against the same question. The migration's SQL and this option have to agree, and
    /// the SQL cannot read the option — so what keeps them together is an assertion that says out
    /// loud what the set is.</para>
    ///
    /// <para>If this test fails after a framework upgrade, the migration's character class needs
    /// the same edit.</para>
    /// </summary>
    [Collection(nameof(ApiTestCollection))]
    public class UserNameCharacterSetApiTests
    {
        private readonly ApiBroker apiBroker;

        public UserNameCharacterSetApiTests(ApiBroker apiBroker) =>
            this.apiBroker = apiBroker;

        [Fact]
        public void ShouldRunWithTheNarrowedUserNameCharacterSet()
        {
            // given
            var expectedCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._+";

            // when
            string actualCharacters = this.apiBroker.GetAllowedUserNameCharacters();

            // then: Identity's default, minus '@' and nothing else. The migration's SQL character
            // class [^-a-zA-Z0-9._+] is this same set, written the other way round.
            actualCharacters.Should().Be(expectedCharacters);
            actualCharacters.Should().NotContain(UserNameRule.ProhibitedCharacter.ToString());
        }

        // Not a restatement of the service-level tests: those prove OUR code refuses the value,
        // this proves the framework underneath refuses it too — which is what lets the migration
        // treat "spellable in this set" as the same question the application will ask.
        [Theory]
        [InlineData("someone@glory2him.local")]
        [InlineData("plain@nemail")]
        public async Task ShouldRefuseToCreateAUserWhoseNameCarriesAnAtSignAsync(string userName)
        {
            // given . when
            IdentityResult actualResult = await this.apiBroker.TryAddUserAsync(
                userName: userName,
                email: "character.set.probe@glory2him.local",
                password: "Test1!");

            // then
            actualResult.Succeeded.Should().BeFalse();

            actualResult.Errors.Should().Contain(identityError =>
                identityError.Code == "InvalidUserName");
        }

        // The availability endpoint answers three independent questions, and a caller that trusts
        // it rather than carrying its own copy of the rule has only those answers to go on.
        // "@" and "a@" are BOTH too short and prohibited; reporting only "too short" would tell
        // that caller two more characters would fix it, which is false.
        [Theory]
        [InlineData("@")]
        [InlineData("a@")]
        public async Task ShouldReportShortAndProhibitedTogetherAsync(string userName)
        {
            // given . when
            ApiBroker.UserNameAvailabilityResponse actual =
                await this.apiBroker.GetUserNameAvailabilityAsync(userName);

            // then
            actual.IsAvailable.Should().BeFalse();
            actual.IsTooShort.Should().BeTrue();
            actual.IsProhibited.Should().BeTrue();
            actual.ProhibitedReason.Should().Contain("@");
        }

        [Fact]
        public async Task ShouldReportALongProhibitedUserNameAsProhibitedOnlyAsync()
        {
            // given . when
            ApiBroker.UserNameAvailabilityResponse actual =
                await this.apiBroker.GetUserNameAvailabilityAsync("someone@glory2him.local");

            // then
            actual.IsAvailable.Should().BeFalse();
            actual.IsProhibited.Should().BeTrue();
            actual.IsTooShort.Should().BeFalse();
        }

        // A name that breaks neither rule must still come back clean, or the endpoint would be
        // refusing everything and the assertions above would pass for the wrong reason.
        [Fact]
        public async Task ShouldReportAPermittedUserNameAsNeitherShortNorProhibitedAsync()
        {
            // given . when
            ApiBroker.UserNameAvailabilityResponse actual =
                await this.apiBroker.GetUserNameAvailabilityAsync("probe.available-name");

            // then
            actual.IsProhibited.Should().BeFalse();
            actual.IsTooShort.Should().BeFalse();
            actual.ProhibitedReason.Should().BeNull();
            actual.IsAvailable.Should().BeTrue();
        }

        // The other half of the same guarantee: the set is narrowed, not broken. A name the rule
        // permits must still be creatable, or the migration's two passes would have nothing legal
        // to rename anything to.
        [Fact]
        public async Task ShouldStillAcceptAUserNameMadeOnlyOfPermittedCharactersAsync()
        {
            // given . when
            IdentityResult actualResult = await this.apiBroker.TryAddUserAsync(
                userName: "probe.name-1_x+y",
                email: "permitted.probe@glory2him.local",
                password: "Test1!");

            // then
            actualResult.Succeeded.Should().BeTrue();
        }
    }
}
