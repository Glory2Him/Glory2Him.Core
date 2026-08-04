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

using Glory2Him.WebApp.Models.Views.Posts;
using Glory2Him.WebApp.Services.Views.Posts;
using Microsoft.AspNetCore.Components;

namespace Glory2Him.WebApp.Components.Pages.Admin
{
    public class PostDetailPageBase : ComponentBase
    {
        public const string PostsRoute = "Admin/Posts";

        [Inject]
        public IPostsViewService PostsViewService { get; set; } = default!;

        [Inject]
        public NavigationManager Navigation { get; set; } = default!;

        // Absent on the "new post" route, which is what tells the page it is creating.
        [Parameter]
        public string? PostId { get; set; }

        protected bool IsLoading { get; private set; } = true;

        protected bool HasError { get; private set; }

        protected string ErrorMessage { get; private set; } = string.Empty;

        protected string? ActionError { get; private set; }

        protected PostView EditModel { get; private set; } = new PostView();

        protected bool IsDeleteDialogVisible { get; private set; }

        protected bool IsEditing => !string.IsNullOrWhiteSpace(PostId);

        protected string HeadingText => IsEditing ? "Edit post" : "New post";

        protected string DeleteMessage =>
            $"Delete post \"{EditModel.Title}\"? This cannot be undone.";

        protected override async Task OnParametersSetAsync()
        {
            IsLoading = true;
            HasError = false;
            ActionError = null;

            try
            {
                EditModel = IsEditing
                    ? await PostsViewService.RetrievePostByIdAsync(PostId!)
                    : CreateDraft();
            }
            catch
            {
                HasError = true;
                ErrorMessage = "We could not load this post right now. Please try again later.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected async Task SavePostAsync()
        {
            ActionError = null;

            try
            {
                if (IsEditing)
                {
                    await PostsViewService.ModifyPostAsync(EditModel);
                }
                else
                {
                    await PostsViewService.AddPostAsync(EditModel);
                }

                Navigation.NavigateTo(PostsRoute);
            }
            catch
            {
                ActionError = "The post could not be saved. Please try again.";
            }
        }

        protected void OpenDeleteDialog()
        {
            ActionError = null;
            IsDeleteDialogVisible = true;
        }

        protected void CloseDeleteDialog() =>
            IsDeleteDialogVisible = false;

        protected async Task ConfirmDeleteAsync()
        {
            IsDeleteDialogVisible = false;

            try
            {
                await PostsViewService.RemovePostAsync(EditModel.Id);

                Navigation.NavigateTo(PostsRoute);
            }
            catch
            {
                ActionError = "The post could not be deleted. Please try again.";
            }
        }

        protected void GoBack() =>
            Navigation.NavigateTo(PostsRoute);

        // Sensible starting values so a new post renders like a real one straight away.
        private static PostView CreateDraft() =>
            new PostView
            {
                ImageUrl = "assets/images/blog/16by9/big/01.jpg",
                AuthorImageUrl = "assets/images/avatar/01.jpg",
                CategoryBadgeCss = "text-bg-primary",
                Category = "Faith",
                AuthorName = "Glory 2 Him",
                PublishedDate = DateTimeOffset.UtcNow,
                ReadMinutes = 3,
            };
    }
}
