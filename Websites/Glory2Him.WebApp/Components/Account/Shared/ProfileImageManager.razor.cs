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

using Glory2Him.WebApp.Models.Views.Profiles;
using Glory2Him.WebApp.Models.Views.Profiles.Exceptions;
using Glory2Him.WebApp.Services.Views.Profiles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Glory2Him.WebApp.Components.Account.Shared
{
    public partial class ProfileImageManager
    {
        [Inject]
        public IProfileViewService ProfileViewService { get; set; } = default!;

        [Parameter]
        [EditorRequired]
        public Guid UserId { get; set; }

        [Parameter]
        [EditorRequired]
        public string Name { get; set; } = string.Empty;

        protected bool IsBusy { get; private set; }

        protected string? StatusMessage { get; private set; }

        protected string? ErrorMessage { get; private set; }

        protected bool HasImage => Profile?.HasProfileImage == true;

        protected string? ImageUrl => Profile?.ImageUrl;

        private ProfileView? Profile { get; set; }

        protected override async Task OnParametersSetAsync() =>
            await ReloadAsync();

        protected async Task OnFileSelectedAsync(InputFileChangeEventArgs args)
        {
            StatusMessage = null;
            ErrorMessage = null;
            IsBusy = true;

            try
            {
                IBrowserFile file = args.File;

                if (file.Size > Glory2Him.WebApp.Services.Views.Profiles.ProfileViewService.MaxUploadBytes)
                {
                    ErrorMessage = "The image is too large. Please choose a file up to 5 MB.";
                    return;
                }

                await using System.IO.Stream stream =
                    file.OpenReadStream(Glory2Him.WebApp.Services.Views.Profiles.ProfileViewService.MaxUploadBytes);

                await ProfileViewService.SetProfileImageAsync(
                    UserId, stream, file.Size, file.ContentType);

                await ReloadAsync();
                StatusMessage = "Your profile image has been updated.";
            }
            catch (ProfileViewValidationException validationException)
            {
                ErrorMessage = validationException.Message;
            }
            catch
            {
                ErrorMessage = "We could not update your image. Please try again.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected async Task RemoveAsync()
        {
            StatusMessage = null;
            ErrorMessage = null;
            IsBusy = true;

            try
            {
                await ProfileViewService.RemoveProfileImageAsync(UserId);
                await ReloadAsync();
                StatusMessage = "Your profile image has been removed.";
            }
            catch
            {
                ErrorMessage = "We could not remove your image. Please try again.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ReloadAsync() =>
            Profile = await ProfileViewService.RetrieveProfileByIdAsync(UserId);
    }
}
