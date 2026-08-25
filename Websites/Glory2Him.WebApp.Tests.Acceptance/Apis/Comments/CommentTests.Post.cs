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

using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.WebApp.Tests.Acceptance.Models.Comments;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Comments
{
    public partial class CommentApiTests
    {
        [Fact]
        public async Task ShouldPostCommentAsync()
        {
            // given
            Comment randomComment = CreateRandomComment();
            Comment inputComment = randomComment;
            Comment expectedComment = inputComment;

            try
            {
                // when
                await this.apiBroker.PostCommentAsync(inputComment);

                Comment actualComment =
                    await this.apiBroker.GetCommentByIdAsync(inputComment.Id);

                // then
                actualComment.Should().BeEquivalentTo(expectedComment, options => options
                    .Excluding(property => property.CreatedBy)
                    .Excluding(property => property.CreatedWhen)
                    .Excluding(property => property.UpdatedBy)
                    .Excluding(property => property.UpdatedWhen));
            }
            finally
            {
                await this.apiBroker.RemoveCoreCommentByIdAsync(inputComment.Id);
            }
        }
    }
}
