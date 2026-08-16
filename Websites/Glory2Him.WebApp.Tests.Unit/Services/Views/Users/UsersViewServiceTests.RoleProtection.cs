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
using Glory2Him.Core.Models.Securities;
using Glory2Him.WebApp.Models.Foundations.Users;
using Glory2Him.WebApp.Models.Views.Users.Exceptions;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Glory2Him.WebApp.Tests.Unit.Services.Views.Users
{
    public partial class UsersViewServiceTests
    {
        [Fact]
        public async Task ShouldRefuseToRemoveTheLastHolderOfTheCoreAdministratorRole()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid(), UserName = "onlyadmin" };
            GivenUserExists(user, roles: new List<string> { Roles.Admin });
            GivenUsersInRoleCount(Roles.Admin, count: 1);

            // when
            Func<Task> removingRole = async () =>
                await this.usersViewService.SetUserRoleAsync(
                    user.Id, Roles.Admin, isInRole: false);

            // then
            await removingRole.Should().ThrowAsync<UsersViewValidationException>();

            this.identityBrokerMock.Verify(broker =>
                broker.DeleteUserFromRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>()),
                    Times.Never);
        }

        [Fact]
        public async Task ShouldAllowRemovingTheCoreAdministratorRoleWhenAnotherHolderRemains()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid() };
            GivenUserExists(user, roles: new List<string> { Roles.Admin });
            GivenUsersInRoleCount(Roles.Admin, count: 2);

            this.identityBrokerMock.Setup(broker =>
                broker.DeleteUserFromRoleAsync(user, Roles.Admin))
                    .ReturnsAsync(IdentityResult.Success);

            // when
            await this.usersViewService.SetUserRoleAsync(user.Id, Roles.Admin, isInRole: false);

            // then
            this.identityBrokerMock.Verify(broker =>
                broker.DeleteUserFromRoleAsync(user, Roles.Admin),
                    Times.Once);
        }

        /// <summary>
        /// Deleting the account is the other route to the same loss, and it must consider both
        /// administrator vocabularies — here the account holds only Core's.
        /// </summary>
        [Fact]
        public async Task ShouldRefuseToDeleteTheLastHolderOfTheCoreAdministratorRole()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid(), UserName = "onlyadmin" };
            GivenUserExists(user, roles: new List<string> { Roles.Admin });
            GivenUsersInRoleCount(Roles.Admin, count: 1);

            // when
            Func<Task> deletingUser = async () =>
                await this.usersViewService.DeleteUserAsync(user.Id);

            // then
            await deletingUser.Should().ThrowAsync<UsersViewValidationException>();

            this.identityBrokerMock.Verify(broker =>
                broker.DeleteUserAsync(It.IsAny<AppUser>()),
                    Times.Never);
        }

        /// <summary>
        /// Removing an unrelated role must not be blocked just because the account happens to be
        /// the last administrator — the guard is per role, not per account.
        /// </summary>
        [Fact]
        public async Task ShouldAllowRemovingAnUnprotectedRoleFromTheLastAdministrator()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid() };
            GivenUserExists(user, roles: new List<string> { Roles.Admin, Roles.TagReviewer });
            GivenUsersInRoleCount(Roles.Admin, count: 1);

            this.identityBrokerMock.Setup(broker =>
                broker.DeleteUserFromRoleAsync(user, Roles.TagReviewer))
                    .ReturnsAsync(IdentityResult.Success);

            // when
            await this.usersViewService.SetUserRoleAsync(
                user.Id, Roles.TagReviewer, isInRole: false);

            // then
            this.identityBrokerMock.Verify(broker =>
                broker.DeleteUserFromRoleAsync(user, Roles.TagReviewer),
                    Times.Once);
        }

        /// <summary>
        /// Identity resolves role names through <c>NormalizedName</c>, so a differently-cased
        /// name really does remove the role. Matching ordinally let the guard decide the role
        /// was not protected and wave the removal through.
        /// </summary>
        [Theory]
        [InlineData("admin")]
        [InlineData("ADMIN")]
        [InlineData("administrators")]
        public async Task ShouldRefuseToRemoveTheLastAdministratorWhateverTheCasing(
            string suppliedRoleName)
        {
            // given
            string canonicalRoleName =
                suppliedRoleName.Equals(Roles.Admin, StringComparison.OrdinalIgnoreCase)
                    ? Roles.Admin
                    : AdministratorsRole;

            var user = new AppUser { Id = Guid.NewGuid(), UserName = "onlyadmin" };
            GivenUserExists(user, roles: new List<string> { canonicalRoleName });
            GivenUsersInRoleCount(canonicalRoleName, count: 1);
            GivenUsersInRoleCount(suppliedRoleName, count: 1);

            // when
            Func<Task> removingRole = async () =>
                await this.usersViewService.SetUserRoleAsync(
                    user.Id, suppliedRoleName, isInRole: false);

            // then
            await removingRole.Should().ThrowAsync<UsersViewValidationException>();

            this.identityBrokerMock.Verify(broker =>
                broker.DeleteUserFromRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>()),
                    Times.Never);
        }

        /// <summary>
        /// An account that cannot sign in cannot administer the site, so it is not the reason it
        /// is safe to demote somebody else.
        /// </summary>
        [Fact]
        public async Task ShouldRefuseToRemoveTheLastUsableAdministratorWhenTheOtherIsDisabled()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid(), UserName = "realadmin" };
            var disabledAdministrator = new AppUser { Id = Guid.NewGuid(), IsDisabled = true };

            GivenUserExists(user, roles: new List<string> { Roles.Admin });
            GivenUsersInRole(Roles.Admin, user, disabledAdministrator);

            // when
            Func<Task> removingRole = async () =>
                await this.usersViewService.SetUserRoleAsync(
                    user.Id, Roles.Admin, isInRole: false);

            // then
            await removingRole.Should().ThrowAsync<UsersViewValidationException>();

            this.identityBrokerMock.Verify(broker =>
                broker.DeleteUserFromRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>()),
                    Times.Never);
        }

        [Fact]
        public async Task ShouldRefuseToRemoveTheLastUsableAdministratorWhenTheOtherIsLockedOut()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid(), UserName = "realadmin" };
            var lockedOutAdministrator = new AppUser { Id = Guid.NewGuid() };

            GivenUserExists(user, roles: new List<string> { Roles.Admin });
            GivenUsersInRole(Roles.Admin, user, lockedOutAdministrator);

            this.identityBrokerMock.Setup(broker =>
                broker.SelectIsLockedOutAsync(lockedOutAdministrator))
                    .ReturnsAsync(true);

            // when
            Func<Task> removingRole = async () =>
                await this.usersViewService.SetUserRoleAsync(
                    user.Id, Roles.Admin, isInRole: false);

            // then
            await removingRole.Should().ThrowAsync<UsersViewValidationException>();

            this.identityBrokerMock.Verify(broker =>
                broker.DeleteUserFromRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>()),
                    Times.Never);
        }

        [Fact]
        public async Task ShouldAllowRemovingAnAdministratorWhenAnotherUsableOneRemains()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid() };
            var otherAdministrator = new AppUser { Id = Guid.NewGuid() };

            GivenUserExists(user, roles: new List<string> { Roles.Admin });
            GivenUsersInRole(Roles.Admin, user, otherAdministrator);

            this.identityBrokerMock.Setup(broker =>
                broker.DeleteUserFromRoleAsync(user, Roles.Admin))
                    .ReturnsAsync(IdentityResult.Success);

            // when
            await this.usersViewService.SetUserRoleAsync(user.Id, Roles.Admin, isInRole: false);

            // then
            this.identityBrokerMock.Verify(broker =>
                broker.DeleteUserFromRoleAsync(user, Roles.Admin),
                    Times.Once);
        }

        /// <summary>
        /// The self-service delete endpoint reports failure to the caller, and it now reaches
        /// Identity through this method — so the result has to be checked here or that reporting
        /// is lost. Same defect class as the discarded role results in #232.
        /// </summary>
        [Fact]
        public async Task ShouldThrowWhenDeletingAUserIdentityRefuses()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid() };
            GivenUserExists(user, roles: new List<string>());

            IdentityResult failedResult = IdentityResult.Failed(
                new IdentityError { Description = "Concurrency failure." });

            this.identityBrokerMock.Setup(broker =>
                broker.DeleteUserAsync(user))
                    .ReturnsAsync(failedResult);

            // when
            Func<Task> deletingUser = async () =>
                await this.usersViewService.DeleteUserAsync(user.Id);

            // then
            await deletingUser.Should().ThrowAsync<UsersViewValidationException>()
                .Where(exception => exception.Message.Contains("Concurrency failure."));
        }

        [Fact]
        public async Task ShouldThrowWhenAddingARoleIdentityRefuses()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid() };
            GivenUserExists(user, roles: new List<string>());
            string someRoleName = GetRandomString();

            IdentityResult failedResult = IdentityResult.Failed(
                new IdentityError { Description = "Role does not exist." });

            this.identityBrokerMock.Setup(broker =>
                broker.InsertUserToRoleAsync(user, someRoleName))
                    .ReturnsAsync(failedResult);

            // when
            Func<Task> addingRole = async () =>
                await this.usersViewService.SetUserRoleAsync(user.Id, someRoleName, isInRole: true);

            // then
            await addingRole.Should().ThrowAsync<UsersViewValidationException>()
                .Where(exception => exception.Message.Contains("Role does not exist."));
        }

        [Fact]
        public async Task ShouldThrowWhenRemovingARoleIdentityRefuses()
        {
            // given
            var user = new AppUser { Id = Guid.NewGuid() };
            GivenUserExists(user, roles: new List<string>());
            string someRoleName = GetRandomString();

            IdentityResult failedResult = IdentityResult.Failed(
                new IdentityError { Description = "Role does not exist." });

            this.identityBrokerMock.Setup(broker =>
                broker.DeleteUserFromRoleAsync(user, someRoleName))
                    .ReturnsAsync(failedResult);

            // when
            Func<Task> removingRole = async () =>
                await this.usersViewService.SetUserRoleAsync(
                    user.Id, someRoleName, isInRole: false);

            // then
            await removingRole.Should().ThrowAsync<UsersViewValidationException>()
                .Where(exception => exception.Message.Contains("Role does not exist."));
        }
    }
}
