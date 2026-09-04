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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.WebApp.Brokers.Accounts;
using Glory2Him.WebApp.Brokers.Loggings;
using Glory2Him.WebApp.Services.Views.Registrations;
using Moq;

namespace Glory2Him.WebApp.Tests.Unit.Services.Views.Registrations
{
    public class RegistrationViewServiceTests
    {
        private readonly Mock<IAccountBroker> accountBrokerMock;
        private readonly Mock<ILoggingBroker> loggingBrokerMock;
        private readonly IRegistrationViewService registrationViewService;

        public RegistrationViewServiceTests()
        {
            this.accountBrokerMock = new Mock<IAccountBroker>();
            this.loggingBrokerMock = new Mock<ILoggingBroker>();

            this.registrationViewService = new RegistrationViewService(
                accountBroker: this.accountBrokerMock.Object,
                loggingBroker: this.loggingBrokerMock.Object);
        }

        [Theory]
        [InlineData("")]
        [InlineData("ab")]
        public async Task ShouldReportUnavailableForTooShortUsername(string userName)
        {
            // given . when
            bool available = await this.registrationViewService.IsUsernameAvailableAsync(userName);

            // then (no broker lookup for obviously-invalid input)
            available.Should().BeFalse();

            this.accountBrokerMock.Verify(broker =>
                broker.UsernameExistsAsync(It.IsAny<string>()),
                    Times.Never);
        }

        [Fact]
        public async Task ShouldReportAvailableWhenUsernameFree()
        {
            // given
            this.accountBrokerMock.Setup(broker =>
                broker.UsernameExistsAsync("freename"))
                    .ReturnsAsync(false);

            // when
            bool available = await this.registrationViewService.IsUsernameAvailableAsync("freename");

            // then
            available.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldReportUnavailableWhenUsernameTaken()
        {
            // given
            this.accountBrokerMock.Setup(broker =>
                broker.UsernameExistsAsync("admin"))
                    .ReturnsAsync(true);

            // when
            bool available = await this.registrationViewService.IsUsernameAvailableAsync("admin");

            // then
            available.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldReportEmailInUse()
        {
            // given
            this.accountBrokerMock.Setup(broker =>
                broker.EmailExistsAsync("taken@g2h.org"))
                    .ReturnsAsync(true);

            // when
            bool inUse = await this.registrationViewService.IsEmailInUseAsync("taken@g2h.org");

            // then
            inUse.Should().BeTrue();
        }

        // Design §18.3.1 — a username may never be an email address, because every display name
        // in the system falls back to the username, so an account with no personal details set
        // publishes whatever is in it. The theory covers the plain address, the address the
        // "narrow" rule would have allowed (somebody ELSE's, which leaks just as effectively),
        // and a name that merely carries an '@' without being an address at all.
        [Theory]
        [InlineData("someone@glory2him.local")]
        [InlineData("a.colleague@another.example")]
        [InlineData("not@nemail")]
        public async Task ShouldReportUnavailableForUsernameCarryingAnAtSign(string userName)
        {
            // given . when
            bool available = await this.registrationViewService.IsUsernameAvailableAsync(userName);

            // then
            available.Should().BeFalse();

            // No lookup at all: it is not free-but-taken, it is a name nobody may hold, and
            // asking the store would invite "it is available" for the first person to try it.
            this.accountBrokerMock.Verify(broker =>
                broker.UsernameExistsAsync(It.IsAny<string>()),
                    Times.Never);
        }

        [Fact]
        public async Task ShouldNeverSuggestAUsernameCarryingAnAtSign()
        {
            // given: every candidate is free, so nothing is filtered for being taken and the
            // suggester is free to offer whatever it composes
            this.accountBrokerMock.Setup(broker =>
                broker.UsernameExistsAsync(It.IsAny<string>()))
                    .ReturnsAsync(false);

            // when: the person's own details already carry an address
            List<string> suggestions =
                await this.registrationViewService.SuggestUsernamesAsync(
                    name: "chris@work.example",
                    surname: "du Toit",
                    preferredName: "chris@work.example",
                    count: 6);

            // then: the suggester may not hand back the very thing the rule refuses
            suggestions.Should().NotBeEmpty();
            suggestions.Should().OnlyContain(suggestion => !suggestion.Contains('@'));
        }

        [Fact]
        public async Task ShouldSuggestAvailableUsernamesFromNameAndSurname()
        {
            // given: everything is free except the first, most-obvious combination
            this.accountBrokerMock.Setup(broker =>
                broker.UsernameExistsAsync(It.IsAny<string>()))
                    .ReturnsAsync(false);

            this.accountBrokerMock.Setup(broker =>
                broker.UsernameExistsAsync("christodutoit"))
                    .ReturnsAsync(true);

            // when
            List<string> suggestions =
                await this.registrationViewService.SuggestUsernamesAsync(
                    name: "Christo", surname: "du Toit", preferredName: null, count: 5);

            // then
            suggestions.Should().NotBeEmpty();
            suggestions.Should().NotContain("christodutoit"); // the taken one is filtered out
            suggestions.Should().OnlyContain(s => s.Length >= 3);
            suggestions.Should().OnlyHaveUniqueItems();
            suggestions.Count.Should().BeLessThanOrEqualTo(5);
            // name-based, lower-cased, no spaces
            suggestions.Should().Contain(s => s.Contains("christo") || s.Contains("dutoit"));
        }

        [Fact]
        public async Task ShouldIncludePreferredNameInSuggestions()
        {
            // given
            this.accountBrokerMock.Setup(broker =>
                broker.UsernameExistsAsync(It.IsAny<string>()))
                    .ReturnsAsync(false);

            // when
            List<string> suggestions =
                await this.registrationViewService.SuggestUsernamesAsync(
                    name: "Christo", surname: "du Toit", preferredName: "Chris", count: 6);

            // then
            suggestions.Should().Contain("chris");
        }
    }
}
