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
using System.Threading.Tasks;
using Glory2Him.WebApp.Models.Views.Users;
using Glory2Him.WebApp.Services.Views.Users;
using Microsoft.AspNetCore.Components;

namespace Glory2Him.WebApp.Components.Pages.Admin
{
    public class UsersPageBase : ComponentBase
    {
        public const string AdministratorsRole = "Administrators";

        [Inject]
        public IUsersViewService UsersViewService { get; set; } = default!;

        protected bool IsLoading { get; private set; } = true;

        protected bool HasError { get; private set; }

        protected string ErrorMessage { get; private set; } = string.Empty;

        protected List<UserView> Users { get; private set; } = new List<UserView>();

        protected UserView? SelectedUser { get; private set; }

        protected bool IsManageModalVisible { get; private set; }

        protected bool IsDeleteDialogVisible { get; private set; }

        protected override async Task OnInitializedAsync() =>
            await LoadUsersAsync();

        private async Task LoadUsersAsync()
        {
            IsLoading = true;
            HasError = false;

            try
            {
                Users = await UsersViewService.RetrieveAllUsersAsync();
            }
            catch
            {
                HasError = true;
                ErrorMessage = "We could not load users right now. Please try again later.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected void OpenManageModal(UserView user)
        {
            SelectedUser = user;
            IsManageModalVisible = true;
        }

        protected void CloseManageModal()
        {
            IsManageModalVisible = false;
            SelectedUser = null;
        }

        protected void OpenDeleteDialog(UserView user)
        {
            SelectedUser = user;
            IsDeleteDialogVisible = true;
        }

        protected void CloseDeleteDialog()
        {
            IsDeleteDialogVisible = false;
            SelectedUser = null;
        }

        protected async Task ToggleDisabledAsync(UserView user)
        {
            await GuardAsync(() =>
                UsersViewService.SetUserDisabledAsync(user.Id, !user.IsDisabled).AsTask());
        }

        protected async Task ToggleAdministratorAsync(UserView user)
        {
            bool isCurrentlyAdmin = user.Roles.Contains(AdministratorsRole);

            await GuardAsync(() =>
                UsersViewService.SetUserRoleAsync(
                    user.Id, AdministratorsRole, !isCurrentlyAdmin).AsTask());
        }

        protected async Task ConfirmDeleteAsync()
        {
            if (SelectedUser is null)
            {
                return;
            }

            Guid userId = SelectedUser.Id;
            IsDeleteDialogVisible = false;

            await GuardAsync(() => UsersViewService.DeleteUserAsync(userId).AsTask());
        }

        private async Task GuardAsync(Func<Task> operation)
        {
            try
            {
                await operation();
                await LoadUsersAsync();
                CloseManageModal();
            }
            catch
            {
                HasError = true;
                ErrorMessage = "The action could not be completed. Please try again.";
            }
        }
    }
}
