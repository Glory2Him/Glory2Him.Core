// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6
// ────────────────────────────────────────────────────────────────────────────────

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.WebApp.Models.Foundations.Users;
using Glory2Him.WebApp.Models.Views.Users;
using Moq;

namespace Glory2Him.WebApp.Tests.Unit.Services.Views.Users
{
    public partial class UsersViewServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveAllUsersWithTheirRoles()
        {
            // given
            List<AppUser> appUsers = CreateRandomAppUsers(count: 2);

            this.identityBrokerMock.Setup(broker =>
                broker.SelectAllUsers())
                    .Returns(appUsers.AsQueryable());

            this.identityBrokerMock.Setup(broker =>
                broker.SelectUserRolesAsync(It.IsAny<AppUser>()))
                    .ReturnsAsync(new List<string> { "Users" });

            // when
            List<UserView> actualUsers =
                await this.usersViewService.RetrieveAllUsersAsync();

            // then
            actualUsers.Should().HaveCount(2);
            actualUsers.Should().OnlyContain(user => user.Roles.Contains("Users"));

            this.identityBrokerMock.Verify(broker =>
                broker.SelectAllUsers(),
                    Times.Once);

            this.identityBrokerMock.Verify(broker =>
                broker.SelectUserRolesAsync(It.IsAny<AppUser>()),
                    Times.Exactly(2));

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldOrderUsersByUserName()
        {
            // given
            List<AppUser> appUsers = CreateRandomAppUsers(count: 3);
            appUsers[0].UserName = "Zeta";
            appUsers[1].UserName = "Alpha";
            appUsers[2].UserName = "Mike";

            this.identityBrokerMock.Setup(broker =>
                broker.SelectAllUsers())
                    .Returns(appUsers.AsQueryable());

            this.identityBrokerMock.Setup(broker =>
                broker.SelectUserRolesAsync(It.IsAny<AppUser>()))
                    .ReturnsAsync(new List<string>());

            // when
            List<UserView> actualUsers =
                await this.usersViewService.RetrieveAllUsersAsync();

            // then
            actualUsers.Select(user => user.UserName)
                .Should().ContainInOrder("Alpha", "Mike", "Zeta");
        }
    }
}
