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
using Glory2Him.WebApp.Models.Views.Posts;
using Glory2Him.WebApp.Services.Views.Posts;
using Microsoft.AspNetCore.Components;

namespace Glory2Him.WebApp.Components.Pages.Admin
{
    public class PostsPageBase : ComponentBase
    {
        [Inject]
        public IPostsViewService PostsViewService { get; set; } = default!;

        protected bool IsLoading { get; private set; } = true;

        protected bool HasError { get; private set; }

        protected string ErrorMessage { get; private set; } = string.Empty;

        protected List<PostView> Posts { get; private set; } = new List<PostView>();

        protected PostView EditModel { get; private set; } = new PostView();

        protected bool IsEditing { get; private set; }

        protected bool IsEditModalVisible { get; private set; }

        protected bool IsDeleteDialogVisible { get; private set; }

        protected PostView? PostToDelete { get; private set; }

        protected string EditTitle => IsEditing ? "Edit post" : "New post";

        protected override async Task OnInitializedAsync() =>
            await LoadPostsAsync();

        private async Task LoadPostsAsync()
        {
            IsLoading = true;
            HasError = false;

            try
            {
                Posts = await PostsViewService.RetrieveAllPostsAsync();
            }
            catch
            {
                HasError = true;
                ErrorMessage = "We could not load posts right now. Please try again later.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected void OpenCreateModal()
        {
            IsEditing = false;
            EditModel = new PostView
            {
                ImageUrl = "assets/images/blog/16by9/big/01.jpg",
                AuthorImageUrl = "assets/images/avatar/01.jpg",
                CategoryBadgeCss = "text-bg-primary",
                Category = "Faith",
                AuthorName = "Glory 2 Him",
                PublishedDate = DateTimeOffset.UtcNow,
                ReadMinutes = 3,
            };

            IsEditModalVisible = true;
        }

        protected void OpenEditModal(PostView post)
        {
            IsEditing = true;

            // Edit a copy so a cancelled edit does not mutate the row in the list.
            EditModel = new PostView
            {
                Id = post.Id,
                Title = post.Title,
                Slug = post.Slug,
                Excerpt = post.Excerpt,
                ImageUrl = post.ImageUrl,
                Category = post.Category,
                CategoryBadgeCss = post.CategoryBadgeCss,
                AuthorName = post.AuthorName,
                AuthorImageUrl = post.AuthorImageUrl,
                PublishedDate = post.PublishedDate,
                ReadMinutes = post.ReadMinutes,
                IsFeatured = post.IsFeatured,
            };

            IsEditModalVisible = true;
        }

        protected void CloseEditModal() =>
            IsEditModalVisible = false;

        protected async Task SavePostAsync()
        {
            await GuardAsync(async () =>
            {
                if (IsEditing)
                {
                    await PostsViewService.ModifyPostAsync(EditModel);
                }
                else
                {
                    await PostsViewService.AddPostAsync(EditModel);
                }
            });

            IsEditModalVisible = false;
        }

        protected void OpenDeleteDialog(PostView post)
        {
            PostToDelete = post;
            IsDeleteDialogVisible = true;
        }

        protected void CloseDeleteDialog()
        {
            IsDeleteDialogVisible = false;
            PostToDelete = null;
        }

        protected async Task ConfirmDeleteAsync()
        {
            if (PostToDelete is null)
            {
                return;
            }

            string id = PostToDelete.Id;
            IsDeleteDialogVisible = false;
            PostToDelete = null;

            await GuardAsync(() => PostsViewService.RemovePostAsync(id).AsTask());
        }

        private async Task GuardAsync(Func<Task> operation)
        {
            try
            {
                await operation();
                await LoadPostsAsync();
            }
            catch
            {
                HasError = true;
                ErrorMessage = "The action could not be completed. Please try again.";
            }
        }
    }
}
