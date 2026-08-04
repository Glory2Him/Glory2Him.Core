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

using System.Text;
using Glory2Him.WebApp.Components.CoreUI;
using Glory2Him.WebApp.Models.Views.Users;
using Glory2Him.WebApp.Models.Views.Users.Exceptions;
using Glory2Him.WebApp.Services.Views.Users;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace Glory2Him.WebApp.Components.Pages.Admin
{
    public class UserDetailPageBase : ComponentBase
    {
        public const string UsersRoute = "Admin/Users";

        [Inject]
        public IUsersViewService UsersViewService { get; set; } = default!;

        [Inject]
        public NavigationManager Navigation { get; set; } = default!;

        [Parameter]
        public Guid UserId { get; set; }

        protected bool IsLoading { get; private set; } = true;

        protected bool HasError { get; private set; }

        protected string ErrorMessage { get; private set; } = string.Empty;

        protected string? ActionError { get; private set; }

        protected string? ActionMessage { get; private set; }

        protected string? GeneratedLink { get; private set; }

        protected string? GeneratedLinkLabel { get; private set; }

        protected UserView? User { get; private set; }

        protected UserView EditModel { get; private set; } = new UserView();

        protected List<SelectOption> AvailableRoleOptions { get; private set; } =
            new List<SelectOption>();

        protected string? SelectedRoleToAdd { get; set; }

        protected bool IsDeleteDialogVisible { get; private set; }

        private List<string> allRoleNames = new List<string>();

        protected string DeleteMessage =>
            User is null
                ? "Are you sure?"
                : $"Permanently delete \"{User.UserName}\"? This cannot be undone. "
                    + "Disabling the account keeps its history and can be reversed.";

        protected override async Task OnParametersSetAsync()
        {
            IsLoading = true;
            HasError = false;

            try
            {
                this.allRoleNames = await UsersViewService.RetrieveAllRoleNamesAsync();

                await ReloadUserAsync();
            }
            catch (UsersViewValidationException usersViewValidationException)
            {
                HasError = true;
                ErrorMessage = usersViewValidationException.Message;
            }
            catch
            {
                HasError = true;
                ErrorMessage = "We could not load this user right now. Please try again later.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ReloadUserAsync()
        {
            User = await UsersViewService.RetrieveUserByIdAsync(UserId);

            // Edit a copy so an abandoned edit never leaves the displayed user half-changed.
            EditModel = new UserView
            {
                Id = User.Id,
                UserName = User.UserName,
                Email = User.Email,
                PhoneNumber = User.PhoneNumber,
                Name = User.Name,
                Surname = User.Surname,
                PreferredName = User.PreferredName,
                DateOfBirth = User.DateOfBirth,
            };

            AvailableRoleOptions =
                this.allRoleNames
                    .Where(role => !User.Roles.Contains(role))
                    .Select(role => new SelectOption { Text = role, Value = role })
                    .ToList();

            SelectedRoleToAdd = AvailableRoleOptions.FirstOrDefault()?.Value;
        }

        protected Task SaveProfileAsync() =>
            RunAsync(
                () => UsersViewService.ModifyUserAsync(EditModel).AsTask(),
                "Profile updated.");

        protected Task AddRoleAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedRoleToAdd))
            {
                return Task.CompletedTask;
            }

            string roleName = SelectedRoleToAdd;

            return RunAsync(
                () => UsersViewService.SetUserRoleAsync(UserId, roleName, isInRole: true).AsTask(),
                $"Added to {roleName}.");
        }

        protected Task RemoveRoleAsync(string roleName) =>
            RunAsync(
                () => UsersViewService.SetUserRoleAsync(UserId, roleName, isInRole: false).AsTask(),
                $"Removed from {roleName}.");

        protected Task ConfirmEmailAsync() =>
            RunAsync(
                () => UsersViewService.ConfirmUserEmailAsync(UserId).AsTask(),
                "Email confirmed.");

        protected Task SetLockedOutAsync(bool isLockedOut) =>
            RunAsync(
                () => UsersViewService.SetUserLockedOutAsync(UserId, isLockedOut).AsTask(),
                isLockedOut ? "User locked out." : "User unlocked.");

        protected Task ResetFailedCountAsync() =>
            RunAsync(
                () => UsersViewService.ResetAccessFailedCountAsync(UserId).AsTask(),
                "Failed login count reset.");

        protected Task SetTwoFactorAsync(bool isEnabled) =>
            RunAsync(
                () => UsersViewService.SetTwoFactorEnabledAsync(UserId, isEnabled).AsTask(),
                isEnabled ? "Two-factor enabled." : "Two-factor disabled.");

        protected Task SetDisabledAsync(bool isDisabled) =>
            RunAsync(
                () => UsersViewService.SetUserDisabledAsync(UserId, isDisabled).AsTask(),
                isDisabled ? "Account disabled." : "Account enabled.");

        protected Task GenerateConfirmationLinkAsync() =>
            GenerateLinkAsync(
                () => UsersViewService.GenerateEmailConfirmationTokenAsync(UserId).AsTask(),
                "Account/ConfirmEmail",
                includeUserId: true,
                "Email confirmation link — share this with the user:");

        protected Task GenerateResetLinkAsync() =>
            GenerateLinkAsync(
                () => UsersViewService.GeneratePasswordResetTokenAsync(UserId).AsTask(),
                "Account/ResetPassword",
                includeUserId: false,
                "Password reset link — share this with the user:");

        protected void OpenDeleteDialog()
        {
            ClearNotices();
            IsDeleteDialogVisible = true;
        }

        protected void CloseDeleteDialog() =>
            IsDeleteDialogVisible = false;

        protected async Task ConfirmDeleteAsync()
        {
            IsDeleteDialogVisible = false;
            ClearNotices();

            try
            {
                await UsersViewService.DeleteUserAsync(UserId);

                Navigation.NavigateTo(UsersRoute);
            }
            catch (UsersViewValidationException usersViewValidationException)
            {
                ActionError = usersViewValidationException.Message;
            }
            catch
            {
                ActionError = "The user could not be deleted. Please try again.";
            }
        }

        protected void GoBack() =>
            Navigation.NavigateTo(UsersRoute);

        private void ClearNotices()
        {
            ActionError = null;
            ActionMessage = null;
            GeneratedLink = null;
            GeneratedLinkLabel = null;
        }

        // Every action follows the same shape: clear the last notice, act, re-read the user so the
        // badges and roles reflect what just happened, then report the outcome.
        private async Task RunAsync(Func<Task> action, string successMessage)
        {
            ClearNotices();

            try
            {
                await action();
                await ReloadUserAsync();

                ActionMessage = successMessage;
            }
            catch (UsersViewValidationException usersViewValidationException)
            {
                ActionError = usersViewValidationException.Message;
            }
            catch
            {
                ActionError = "The action could not be completed. Please try again.";
            }
        }

        private async Task GenerateLinkAsync(
            Func<Task<string>> tokenFactory,
            string path,
            bool includeUserId,
            string label)
        {
            ClearNotices();

            try
            {
                string token = await tokenFactory();
                string code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                var queryParameters = new Dictionary<string, object?> { ["code"] = code };

                if (includeUserId)
                {
                    queryParameters["userId"] = UserId.ToString();
                }

                GeneratedLink = Navigation.GetUriWithQueryParameters(
                    Navigation.ToAbsoluteUri(path).AbsoluteUri,
                    queryParameters);

                GeneratedLinkLabel = label;
            }
            catch (UsersViewValidationException usersViewValidationException)
            {
                ActionError = usersViewValidationException.Message;
            }
            catch
            {
                ActionError = "The link could not be generated. Please try again.";
            }
        }
    }
}
