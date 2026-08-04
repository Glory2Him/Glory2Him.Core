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

using Glory2Him.WebApp.Models.Views.Users;
using Glory2Him.WebApp.Services.Views.Users;
using Microsoft.AspNetCore.Components;

namespace Glory2Him.WebApp.Components.Pages.Admin
{
    public class UsersPageBase : ComponentBase
    {
        [Inject]
        public IUsersViewService UsersViewService { get; set; } = default!;

        [Inject]
        public NavigationManager Navigation { get; set; } = default!;

        protected bool IsLoading { get; private set; } = true;

        protected bool HasError { get; private set; }

        protected string ErrorMessage { get; private set; } = string.Empty;

        protected List<UserView> Users { get; private set; } = new List<UserView>();

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

        // Everything a user can be changed to now lives on its own addressable page, so the list
        // only routes there.
        protected void ViewUser(Guid userId) =>
            Navigation.NavigateTo($"{UserDetailPageBase.UsersRoute}/{userId}");
    }
}
