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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.WebApp.Models.Foundations.Users;
using Glory2Him.WebApp.Tests.Acceptance.Brokers;
using Xunit;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Accounts
{
    /// <summary>
    /// Confirming an email change changes the EMAIL — and nothing else (issue #378, design
    /// §18.3.1).
    ///
    /// <para>This flow used to write the new address into the username as well, on the premise
    /// that "in our UI email and user name are one and the same". That premise is overruled: every
    /// display name in the system falls back to the username, so an account whose personal details
    /// are blank is shown to other people by it — and a username that is an address publishes that
    /// address wherever the site names who submitted or reviewed something.</para>
    ///
    /// <para>The account is arranged with NO name or surname deliberately. That is the population
    /// the rule exists for: an account with a name never reaches the username fallback at all, so
    /// arranging one would let the collapse come back without a single test noticing.</para>
    /// </summary>
    [Collection(nameof(ApiTestCollection))]
    public class ConfirmEmailChangeApiTests
    {
        private readonly ApiBroker apiBroker;

        public ConfirmEmailChangeApiTests(ApiBroker apiBroker) =>
            this.apiBroker = apiBroker;

        [Fact]
        public async Task ShouldChangeEmailWithoutRewritingUserNameAsync()
        {
            // given
            string originalUserName = $"member{Guid.NewGuid():N}"[..20];
            string originalEmail = $"{originalUserName}@old.example";
            string newEmail = $"{originalUserName}@new.example";

            AppUser arrangedUser = await this.apiBroker.AddUserAsync(
                userName: originalUserName,
                email: originalEmail,
                password: "Test1!");

            string encodedCode = await this.apiBroker.GenerateEncodedChangeEmailTokenAsync(
                userId: arrangedUser.Id,
                newEmail: newEmail);

            // when
            string actualMessage = await this.apiBroker.ConfirmEmailChangeAsync(
                userId: arrangedUser.Id,
                newEmail: newEmail,
                encodedCode: encodedCode);

            AppUser actualUser = await this.apiBroker.GetUserByIdAsync(arrangedUser.Id);

            // then
            actualMessage.Should().Contain("Thank you");
            actualUser.Email.Should().Be(newEmail);
            actualUser.UserName.Should().Be(originalUserName);
            actualUser.UserName.Should().NotContain("@");

            // The display name every other surface composes is the point of the rule, so it is
            // asserted here rather than left to be inferred from the username.
            actualUser.DisplayName.Should().Be(originalUserName);

            await this.apiBroker.RemoveUserAsync(arrangedUser.Id);
        }
    }
}
