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
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using FluentAssertions;
using Glory2Him.WebApp.Components.Pages.Admin;
using Glory2Him.WebApp.Models.Views.Users;
using Glory2Him.WebApp.Models.Views.Users.Exceptions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xeptions;

namespace Glory2Him.WebApp.Tests.Unit.Components.Pages.Admin
{
    public partial class UsersPageComponentTests
    {
        [Fact]
        public void ShouldShowSpinnerWhileLoading()
        {
            // given
            var pendingSource = new TaskCompletionSource<List<UserView>>();

            this.usersViewServiceMock.Setup(service =>
                service.RetrieveAllUsersAsync())
                    .Returns(new ValueTask<List<UserView>>(pendingSource.Task));

            // when
            IRenderedComponent<UsersPage> renderedPage = Render<UsersPage>();

            // then
            renderedPage.FindAll("div.spinner-border").Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public void ShouldRenderUsersInDataTable()
        {
            // given
            List<UserView> users = CreateRandomUsers(count: 3);

            this.usersViewServiceMock.Setup(service =>
                service.RetrieveAllUsersAsync())
                    .ReturnsAsync(users);

            // when
            IRenderedComponent<UsersPage> renderedPage = Render<UsersPage>();

            // then (column 0 is the avatar; "Username" is the first titled column)
            renderedPage.FindAll("thead th")[1].TextContent.Trim().Should().Be("Username");

            foreach (UserView user in users)
            {
                renderedPage.Markup.Should().Contain(user.UserName);
            }

            this.usersViewServiceMock.Verify(service =>
                service.RetrieveAllUsersAsync(),
                    Times.Once);
        }

        [Fact]
        public void ShouldRenderRolesAsBadges()
        {
            // given
            List<UserView> users = CreateRandomUsers(count: 1);
            users[0].Roles = new List<string> { "Administrators" };

            this.usersViewServiceMock.Setup(service =>
                service.RetrieveAllUsersAsync())
                    .ReturnsAsync(users);

            // when
            IRenderedComponent<UsersPage> renderedPage = Render<UsersPage>();

            // then
            renderedPage.Find("span.badge.text-bg-primary").TextContent
                .Should().Contain("Administrators");
        }

        [Fact]
        public void ShouldRenderErrorAlertWhenServiceThrows()
        {
            // given
            var serviceException =
                new UsersViewServiceException(
                    message: "Service error",
                    innerException: new Xeption());

            this.usersViewServiceMock.Setup(service =>
                service.RetrieveAllUsersAsync())
                    .ThrowsAsync(serviceException);

            // when
            IRenderedComponent<UsersPage> renderedPage = Render<UsersPage>();

            // then
            renderedPage.Find("div.alert-danger").Should().NotBeNull();
        }

        [Fact]
        public void ShouldRenderEmptyStateWhenNoUsers()
        {
            // given
            this.usersViewServiceMock.Setup(service =>
                service.RetrieveAllUsersAsync())
                    .ReturnsAsync(new List<UserView>());

            // when
            IRenderedComponent<UsersPage> renderedPage = Render<UsersPage>();

            // then
            renderedPage.Find("div.alert-info").TextContent.Should().Contain("No users");
        }

        [Fact]
        public void ShouldNavigateToUserDetailPageWhenViewClicked()
        {
            // given
            List<UserView> users = CreateRandomUsers(count: 1);

            this.usersViewServiceMock.Setup(service =>
                service.RetrieveAllUsersAsync())
                    .ReturnsAsync(users);

            IRenderedComponent<UsersPage> renderedPage = Render<UsersPage>();
            var navigationManager = Services.GetRequiredService<NavigationManager>();

            // when
            renderedPage.FindAll("button")
                .First(button => button.TextContent.Trim() == "View")
                .Click();

            // then
            navigationManager.Uri.Should().EndWith($"Admin/Users/{users[0].Id}");
        }

        [Fact]
        public void ShouldNotManageUsersFromTheListItself()
        {
            // given
            List<UserView> users = CreateRandomUsers(count: 1);

            this.usersViewServiceMock.Setup(service =>
                service.RetrieveAllUsersAsync())
                    .ReturnsAsync(users);

            // when
            IRenderedComponent<UsersPage> renderedPage = Render<UsersPage>();

            // then (managing a user happens on its own page, never in a modal over the list)
            renderedPage.FindAll("div.modal").Should().BeEmpty();

            renderedPage.FindAll("button")
                .Should().NotContain(button => button.TextContent.Trim() == "Delete");
        }
    }
}
