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

            this.identityBrokerMock.Setup(broker =>
                broker.SelectUserByIdAsync(user.Id))
                    .ReturnsAsync(user);

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

            this.identityBrokerMock.Setup(broker =>
                broker.SelectUserByIdAsync(user.Id))
                    .ReturnsAsync(user);

            this.identityBrokerMock.Setup(broker =>
                broker.InsertUserToRoleAsync(user, "Administrators"))
                    .ReturnsAsync(IdentityResult.Success);

            // when
            await this.usersViewService.SetUserRoleAsync(user.Id, "Administrators", isInRole: true);

            // then
            this.identityBrokerMock.Verify(broker =>
                broker.InsertUserToRoleAsync(user, "Administrators"),
                    Times.Once);
        }

        [Fact]
        public async Task ShouldRemoveUserFromRoleWhenSettingRoleFalse()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid() };

            this.identityBrokerMock.Setup(broker =>
                broker.SelectUserByIdAsync(user.Id))
                    .ReturnsAsync(user);

            this.identityBrokerMock.Setup(broker =>
                broker.DeleteUserFromRoleAsync(user, "Administrators"))
                    .ReturnsAsync(IdentityResult.Success);

            // when
            await this.usersViewService.SetUserRoleAsync(user.Id, "Administrators", isInRole: false);

            // then
            this.identityBrokerMock.Verify(broker =>
                broker.DeleteUserFromRoleAsync(user, "Administrators"),
                    Times.Once);
        }

        [Fact]
        public async Task ShouldDeleteUser()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid() };

            this.identityBrokerMock.Setup(broker =>
                broker.SelectUserByIdAsync(user.Id))
                    .ReturnsAsync(user);

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
    }
}
