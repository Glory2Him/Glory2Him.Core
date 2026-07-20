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

using Glory2Him.WebApp.Models.Views.Posts.Exceptions;
using Xeptions;

namespace Glory2Him.WebApp.Services.Views.Posts
{
    public partial class PostsViewService
    {
        private delegate ValueTask<T> ReturningPostsFunction<T>();
        private delegate ValueTask ReturningNothingFunction();

        private async ValueTask<T> TryCatch<T>(ReturningPostsFunction<T> returningPostsFunction)
        {
            try
            {
                return await returningPostsFunction();
            }
            catch (Exception exception)
            {
                var failedPostsViewServiceException =
                    new FailedPostsViewServiceException(
                        message: "Failed posts view service error occurred, contact support.",
                        innerException: exception);

                throw await CreateAndLogServiceExceptionAsync(failedPostsViewServiceException);
            }
        }

        private async ValueTask TryCatch(ReturningNothingFunction returningNothingFunction)
        {
            try
            {
                await returningNothingFunction();
            }
            catch (Exception exception)
            {
                var failedPostsViewServiceException =
                    new FailedPostsViewServiceException(
                        message: "Failed posts view service error occurred, contact support.",
                        innerException: exception);

                throw await CreateAndLogServiceExceptionAsync(failedPostsViewServiceException);
            }
        }

        private async ValueTask<PostsViewServiceException> CreateAndLogServiceExceptionAsync(
            Xeption exception)
        {
            var postsViewServiceException =
                new PostsViewServiceException(
                    message: "Posts view service error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(postsViewServiceException);

            return postsViewServiceException;
        }
    }
}
