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
using System.Threading.Tasks;
using Glory2Him.WebApp.Tests.Acceptance.Models.Comments;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    public partial class ApiBroker
    {
        private const string commentsRelativeUrl = "api/comments";

        public async ValueTask<Comment> PostCommentAsync(Comment comment) =>
            await this.apiFactoryClient.PostContentAsync(commentsRelativeUrl, comment);

        public async ValueTask<List<Comment>> GetAllCommentsAsync() =>
            await this.apiFactoryClient.GetContentAsync<List<Comment>>($"{commentsRelativeUrl}/");

        public async ValueTask<List<Comment>> GetSpecificCommentByIdAsync(Guid commentId) =>
            await this.apiFactoryClient.GetContentAsync<List<Comment>>(
                $"{commentsRelativeUrl}?$filter=Id eq {commentId}");

        public async ValueTask<Comment> GetCommentByIdAsync(Guid commentId) =>
            await this.apiFactoryClient.GetContentAsync<Comment>($"{commentsRelativeUrl}/{commentId}");

        public async ValueTask<Comment> DeleteCommentByIdAsync(Guid commentId) =>
            await this.apiFactoryClient.DeleteContentAsync<Comment>($"{commentsRelativeUrl}/{commentId}");

        public async ValueTask<Comment> HardDeleteCommentByIdAsync(Guid commentId) =>
            await this.apiFactoryClient.DeleteContentAsync<Comment>($"{commentsRelativeUrl}/{commentId}/hard");

        public async ValueTask<Comment> TransitionCommentApprovalAsync(Comment comment) =>
            await this.apiFactoryClient.PostContentAsync($"{commentsRelativeUrl}/approve", comment);

        public async ValueTask<Comment> SubmitCommentByIdAsync(Guid commentId) =>
            await this.apiFactoryClient.PostContentAsync<object, Comment>(
                $"{commentsRelativeUrl}/{commentId}/submit",
                content: new object());

        public async ValueTask<Comment> PutCommentAsync(Comment comment) =>
            await this.apiFactoryClient.PutContentAsync(commentsRelativeUrl, comment);
    }
}
