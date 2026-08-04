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
using Glory2Him.WebApp.Models.Foundations.Users;
using Glory2Him.WebApp.Models.Views.Users.Exceptions;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Glory2Him.WebApp.Tests.Unit.Services.Views.Users
{
    public partial class UsersViewServiceTests
    {
        [Fact]
        public async Task ShouldDisableUserByLockingOutIndefinitely()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid(), UserName = "someone" };
            GivenUserExists(user, roles: new List<string> { "Users" });

            this.identityBrokerMock.Setup(broker =>
                broker.UpdateUserAsync(It.IsAny<AppUser>()))
                    .ReturnsAsync(IdentityResult.Success);

            this.identityBrokerMock.Setup(broker =>
                broker.SetLockoutEnabledAsync(It.IsAny<AppUser>(), It.IsAny<bool>()))
                    .ReturnsAsync(IdentityResult.Success);

            this.identityBrokerMock.Setup(broker =>
                broker.SetLockoutEndDateAsync(It.IsAny<AppUser>(), It.IsAny<DateTimeOffset?>()))
                    .ReturnsAsync(IdentityResult.Success);

            // when
            await this.usersViewService.SetUserDisabledAsync(user.Id, isDisabled: true);

            // then
            user.IsDisabled.Should().BeTrue();

            this.identityBrokerMock.Verify(broker =>
                broker.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue),
                    Times.Once);
        }

        [Fact]
        public async Task ShouldAddUserToRoleWhenSettingRoleTrue()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid() };
            GivenUserExists(user, roles: new List<string>());

            this.identityBrokerMock.Setup(broker =>
                broker.InsertUserToRoleAsync(user, AdministratorsRole))
                    .ReturnsAsync(IdentityResult.Success);

            // when
            await this.usersViewService.SetUserRoleAsync(
                user.Id, AdministratorsRole, isInRole: true);

            // then
            this.identityBrokerMock.Verify(broker =>
                broker.InsertUserToRoleAsync(user, AdministratorsRole),
                    Times.Once);
        }

        [Fact]
        public async Task ShouldRemoveUserFromRoleWhenSettingRoleFalse()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid() };
            GivenUserExists(user, roles: new List<string> { AdministratorsRole });
            GivenAdministratorCount(2);

            this.identityBrokerMock.Setup(broker =>
                broker.DeleteUserFromRoleAsync(user, AdministratorsRole))
                    .ReturnsAsync(IdentityResult.Success);

            // when
            await this.usersViewService.SetUserRoleAsync(
                user.Id, AdministratorsRole, isInRole: false);

            // then
            this.identityBrokerMock.Verify(broker =>
                broker.DeleteUserFromRoleAsync(user, AdministratorsRole),
                    Times.Once);
        }

        [Fact]
        public async Task ShouldDeleteUser()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid() };
            GivenUserExists(user, roles: new List<string> { "Users" });

            this.identityBrokerMock.Setup(broker =>
                broker.DeleteUserAsync(user))
                    .ReturnsAsync(IdentityResult.Success);

            // when
            await this.usersViewService.DeleteUserAsync(user.Id);

            // then
            this.identityBrokerMock.Verify(broker =>
                broker.DeleteUserAsync(user),
                    Times.Once);
        }

        [Fact]
        public async Task ShouldRefuseToDeleteTheLastAdministrator()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid(), UserName = "onlyadmin" };
            GivenUserExists(user, roles: new List<string> { AdministratorsRole });
            GivenAdministratorCount(1);

            // when
            Func<Task> deletingUser = async () =>
                await this.usersViewService.DeleteUserAsync(user.Id);

            // then
            await deletingUser.Should().ThrowAsync<UsersViewValidationException>()
                .Where(exception => exception.Message.Contains("last administrator"));

            this.identityBrokerMock.Verify(broker =>
                broker.DeleteUserAsync(It.IsAny<AppUser>()),
                    Times.Never);
        }

        [Fact]
        public async Task ShouldRefuseToDisableTheLastAdministrator()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid(), UserName = "onlyadmin" };
            GivenUserExists(user, roles: new List<string> { AdministratorsRole });
            GivenAdministratorCount(1);

            // when
            Func<Task> disablingUser = async () =>
                await this.usersViewService.SetUserDisabledAsync(user.Id, isDisabled: true);

            // then
            await disablingUser.Should().ThrowAsync<UsersViewValidationException>();

            user.IsDisabled.Should().BeFalse();

            this.identityBrokerMock.Verify(broker =>
                broker.UpdateUserAsync(It.IsAny<AppUser>()),
                    Times.Never);
        }

        [Fact]
        public async Task ShouldRefuseToRemoveTheLastAdministratorFromTheRole()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid(), UserName = "onlyadmin" };
            GivenUserExists(user, roles: new List<string> { AdministratorsRole });
            GivenAdministratorCount(1);

            // when
            Func<Task> removingRole = async () =>
                await this.usersViewService.SetUserRoleAsync(
                    user.Id, AdministratorsRole, isInRole: false);

            // then
            await removingRole.Should().ThrowAsync<UsersViewValidationException>();

            this.identityBrokerMock.Verify(broker =>
                broker.DeleteUserFromRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>()),
                    Times.Never);
        }

        [Fact]
        public async Task ShouldRefuseToLockOutTheLastAdministrator()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid(), UserName = "onlyadmin" };
            GivenUserExists(user, roles: new List<string> { AdministratorsRole });
            GivenAdministratorCount(1);

            // when
            Func<Task> lockingUser = async () =>
                await this.usersViewService.SetUserLockedOutAsync(user.Id, isLockedOut: true);

            // then
            await lockingUser.Should().ThrowAsync<UsersViewValidationException>();

            this.identityBrokerMock.Verify(broker =>
                broker.SetLockoutEndDateAsync(It.IsAny<AppUser>(), It.IsAny<DateTimeOffset?>()),
                    Times.Never);
        }

        [Fact]
        public async Task ShouldStillAllowRemovingANonAdministratorRoleFromTheLastAdministrator()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid() };
            GivenUserExists(user, roles: new List<string> { AdministratorsRole });
            GivenAdministratorCount(1);

            this.identityBrokerMock.Setup(broker =>
                broker.DeleteUserFromRoleAsync(user, "Users"))
                    .ReturnsAsync(IdentityResult.Success);

            // when
            await this.usersViewService.SetUserRoleAsync(user.Id, "Users", isInRole: false);

            // then
            this.identityBrokerMock.Verify(broker =>
                broker.DeleteUserFromRoleAsync(user, "Users"),
                    Times.Once);
        }
    }
}
