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

using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using Glory2Him.WebApp.Models.Views.Users;
using Glory2Him.WebApp.Services.Views.Users;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tynamix.ObjectFiller;

namespace Glory2Him.WebApp.Tests.Unit.Components.Pages.Admin
{
    public partial class UsersPageComponentTests : BunitContext
    {
        private readonly Mock<IUsersViewService> usersViewServiceMock;

        public UsersPageComponentTests()
        {
            this.usersViewServiceMock = new Mock<IUsersViewService>();
            Services.AddSingleton(this.usersViewServiceMock.Object);
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        private static string GetRandomString() =>
            new MnemonicString().GetValue();

        private static List<UserView> CreateRandomUsers(int count) =>
            Enumerable.Range(0, count).Select(_ => new UserView
            {
                Id = Guid.NewGuid(),
                UserName = GetRandomString(),
                Email = $"{GetRandomString()}@glory2him.local",
                IsDisabled = false,
                Roles = new List<string> { "Users" },
            }).ToList();
    }
}
