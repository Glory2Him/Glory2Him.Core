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
using Glory2Him.WebApp.Brokers.Identities;
using Glory2Him.WebApp.Brokers.Loggings;
using Glory2Him.WebApp.Models.Foundations.Users;
using Glory2Him.WebApp.Services.Views.Users;
using Moq;
using Tynamix.ObjectFiller;

namespace Glory2Him.WebApp.Tests.Unit.Services.Views.Users
{
    public partial class UsersViewServiceTests
    {
        private const string AdministratorsRole = "Administrators";

        private readonly Mock<IIdentityBroker> identityBrokerMock;
        private readonly Mock<ILoggingBroker> loggingBrokerMock;
        private readonly IUsersViewService usersViewService;

        public UsersViewServiceTests()
        {
            this.identityBrokerMock = new Mock<IIdentityBroker>();
            this.loggingBrokerMock = new Mock<ILoggingBroker>();

            this.usersViewService = new UsersViewService(
                identityBroker: this.identityBrokerMock.Object,
                loggingBroker: this.loggingBrokerMock.Object);
        }

        private static string GetRandomString() =>
            new MnemonicString().GetValue();

        private static List<AppUser> CreateRandomAppUsers(int count) =>
            Enumerable.Range(0, count).Select(_ => new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = GetRandomString(),
                Email = $"{GetRandomString()}@glory2him.local",
                IsDisabled = false,
            }).ToList();

        // Every by-id path starts by reading the user and its roles, so the two go together.
        private void GivenUserExists(AppUser user, List<string> roles)
        {
            this.identityBrokerMock.Setup(broker =>
                broker.SelectUserByIdAsync(user.Id))
                    .ReturnsAsync(user);

            this.identityBrokerMock.Setup(broker =>
                broker.SelectUserRolesAsync(user))
                    .ReturnsAsync(roles);
        }

        private void GivenAdministratorCount(int count) =>
            GivenUsersInRoleCount(AdministratorsRole, count);

        private void GivenUsersInRoleCount(string roleName, int count)
        {
            this.identityBrokerMock.Setup(broker =>
                broker.SelectUsersInRoleAsync(roleName))
                    .ReturnsAsync(CreateRandomAppUsers(count));
        }
    }
}
