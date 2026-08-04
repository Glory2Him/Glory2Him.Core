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
using Glory2Him.WebApp.Models.Foundations.Roles;
using Glory2Him.WebApp.Models.Foundations.Users;
using Glory2Him.WebApp.Models.Views.Users;
using Glory2Him.WebApp.Models.Views.Users.Exceptions;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Glory2Him.WebApp.Tests.Unit.Services.Views.Users
{
    public partial class UsersViewServiceTests
    {
        [Fact]
        public async Task ShouldReportUserNoLongerExistsWhenIdDoesNotResolve()
        {
            // given
            Guid missingUserId = Guid.NewGuid();

            this.identityBrokerMock.Setup(broker =>
                broker.SelectUserByIdAsync(missingUserId))
                    .ReturnsAsync((AppUser)null);

            // when
            Func<Task> retrievingUser = async () =>
                await this.usersViewService.RetrieveUserByIdAsync(missingUserId);

            // then
            await retrievingUser.Should().ThrowAsync<UsersViewValidationException>()
                .Where(exception => exception.Message.Contains("no longer exists"));
        }

        [Fact]
        public async Task ShouldRetrieveUserByIdWithLockoutState()
        {
            // given
            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = "someone",
                Email = "someone@glory2him.local",
                PhoneNumber = "0123",
                Name = "Some",
                Surname = "One",
                EmailConfirmed = true,
                AccessFailedCount = 3,
                TwoFactorEnabled = true,
            };

            GivenUserExists(user, roles: new List<string> { AdministratorsRole });

            this.identityBrokerMock.Setup(broker =>
                broker.SelectIsLockedOutAsync(user))
                    .ReturnsAsync(true);

            // when
            UserView actualUser = await this.usersViewService.RetrieveUserByIdAsync(user.Id);

            // then
            actualUser.UserName.Should().Be("someone");
            actualUser.PhoneNumber.Should().Be("0123");
            actualUser.DisplayName.Should().Be("Some One");
            actualUser.EmailConfirmed.Should().BeTrue();
            actualUser.AccessFailedCount.Should().Be(3);
            actualUser.TwoFactorEnabled.Should().BeTrue();
            actualUser.IsLockedOut.Should().BeTrue();
            actualUser.Roles.Should().ContainSingle(role => role == AdministratorsRole);
        }

        [Fact]
        public async Task ShouldRetrieveAllRoleNamesInOrder()
        {
            // given
            var roles = new List<AppRole>
            {
                new AppRole { Id = Guid.NewGuid(), Name = "Users" },
                new AppRole { Id = Guid.NewGuid(), Name = AdministratorsRole },
            };

            this.identityBrokerMock.Setup(broker =>
                broker.SelectAllRoles())
                    .Returns(roles.AsQueryable());

            // when
            List<string> actualRoleNames = await this.usersViewService.RetrieveAllRoleNamesAsync();

            // then
            actualRoleNames.Should().Equal(AdministratorsRole, "Users");
        }

        [Fact]
        public async Task ShouldModifyUserProfileAndPersonalDetails()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid(), UserName = "before" };
            GivenUserExists(user, roles: new List<string>());

            this.identityBrokerMock.Setup(broker =>
                broker.UpdateUserAsync(It.IsAny<AppUser>()))
                    .ReturnsAsync(IdentityResult.Success);

            this.identityBrokerMock.Setup(broker =>
                broker.SetUserNameAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
                    .ReturnsAsync(IdentityResult.Success);

            this.identityBrokerMock.Setup(broker =>
                broker.SetEmailAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
                    .ReturnsAsync(IdentityResult.Success);

            this.identityBrokerMock.Setup(broker =>
                broker.SetPhoneNumberAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
                    .ReturnsAsync(IdentityResult.Success);

            var editedUser = new UserView
            {
                Id = user.Id,
                UserName = "after",
                Email = "after@glory2him.local",
                PhoneNumber = "555",
                Name = "After",
                Surname = "Name",
                PreferredName = "Aft",
                DateOfBirth = new DateOnly(1990, 5, 1),
            };

            // when
            await this.usersViewService.ModifyUserAsync(editedUser);

            // then
            user.Name.Should().Be("After");
            user.Surname.Should().Be("Name");
            user.PreferredName.Should().Be("Aft");
            user.DateOfBirth.Should().Be(new DateOnly(1990, 5, 1));

            this.identityBrokerMock.Verify(broker =>
                broker.SetUserNameAsync(user, "after"),
                    Times.Once);

            this.identityBrokerMock.Verify(broker =>
                broker.SetEmailAsync(user, "after@glory2him.local"),
                    Times.Once);

            this.identityBrokerMock.Verify(broker =>
                broker.SetPhoneNumberAsync(user, "555"),
                    Times.Once);
        }

        [Fact]
        public async Task ShouldConfirmUserEmailUsingAFreshToken()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid() };
            GivenUserExists(user, roles: new List<string>());

            this.identityBrokerMock.Setup(broker =>
                broker.GenerateEmailConfirmationTokenAsync(user))
                    .ReturnsAsync("token");

            this.identityBrokerMock.Setup(broker =>
                broker.ConfirmEmailAsync(user, "token"))
                    .ReturnsAsync(IdentityResult.Success);

            // when
            await this.usersViewService.ConfirmUserEmailAsync(user.Id);

            // then
            this.identityBrokerMock.Verify(broker =>
                broker.ConfirmEmailAsync(user, "token"),
                    Times.Once);
        }

        [Fact]
        public async Task ShouldGeneratePasswordResetToken()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid() };
            GivenUserExists(user, roles: new List<string>());

            this.identityBrokerMock.Setup(broker =>
                broker.GeneratePasswordResetTokenAsync(user))
                    .ReturnsAsync("reset-token");

            // when
            string actualToken =
                await this.usersViewService.GeneratePasswordResetTokenAsync(user.Id);

            // then
            actualToken.Should().Be("reset-token");
        }

        [Fact]
        public async Task ShouldUnlockUserByClearingTheLockoutEndDate()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid() };
            GivenUserExists(user, roles: new List<string> { AdministratorsRole });

            this.identityBrokerMock.Setup(broker =>
                broker.SetLockoutEndDateAsync(user, null))
                    .ReturnsAsync(IdentityResult.Success);

            // when
            await this.usersViewService.SetUserLockedOutAsync(user.Id, isLockedOut: false);

            // then (unlocking is always safe, so it never consults the administrator count)
            this.identityBrokerMock.Verify(broker =>
                broker.SetLockoutEndDateAsync(user, null),
                    Times.Once);

            this.identityBrokerMock.Verify(broker =>
                broker.SelectUsersInRoleAsync(It.IsAny<string>()),
                    Times.Never);
        }

        [Fact]
        public async Task ShouldResetAccessFailedCount()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid(), AccessFailedCount = 5 };
            GivenUserExists(user, roles: new List<string>());

            this.identityBrokerMock.Setup(broker =>
                broker.ResetAccessFailedCountAsync(user))
                    .ReturnsAsync(IdentityResult.Success);

            // when
            await this.usersViewService.ResetAccessFailedCountAsync(user.Id);

            // then
            this.identityBrokerMock.Verify(broker =>
                broker.ResetAccessFailedCountAsync(user),
                    Times.Once);
        }

        [Fact]
        public async Task ShouldResetTheAuthenticatorKeyWhenTurningTwoFactorOff()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid() };
            GivenUserExists(user, roles: new List<string>());

            this.identityBrokerMock.Setup(broker =>
                broker.SetTwoFactorEnabledAsync(user, false))
                    .ReturnsAsync(IdentityResult.Success);

            this.identityBrokerMock.Setup(broker =>
                broker.ResetAuthenticatorKeyAsync(user))
                    .ReturnsAsync(IdentityResult.Success);

            // when
            await this.usersViewService.SetTwoFactorEnabledAsync(user.Id, isEnabled: false);

            // then
            this.identityBrokerMock.Verify(broker =>
                broker.ResetAuthenticatorKeyAsync(user),
                    Times.Once);
        }

        [Fact]
        public async Task ShouldKeepTheAuthenticatorKeyWhenTurningTwoFactorOn()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid() };
            GivenUserExists(user, roles: new List<string>());

            this.identityBrokerMock.Setup(broker =>
                broker.SetTwoFactorEnabledAsync(user, true))
                    .ReturnsAsync(IdentityResult.Success);

            // when
            await this.usersViewService.SetTwoFactorEnabledAsync(user.Id, isEnabled: true);

            // then
            this.identityBrokerMock.Verify(broker =>
                broker.ResetAuthenticatorKeyAsync(It.IsAny<AppUser>()),
                    Times.Never);
        }
    }
}
