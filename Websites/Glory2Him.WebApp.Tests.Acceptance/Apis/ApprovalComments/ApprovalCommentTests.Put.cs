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
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.WebApp.Tests.Acceptance.Models.ApprovalComments;
using RESTFulSense.Exceptions;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ApprovalComments
{
    public partial class ApprovalCommentApiTests
    {
        [Fact]
        public async Task ShouldPutApprovalCommentAsync()
        {
            // given
            (Approval randomApproval, ApprovalComment randomApprovalComment) =
                await PostRandomApprovalCommentOnOpenApprovalAsync();

            ApprovalComment modifiedApprovalComment =
                UpdateApprovalCommentWithRandomValues(randomApprovalComment);

            try
            {
                // when
                await this.apiBroker.PutApprovalCommentAsync(modifiedApprovalComment);

                ApprovalComment actualApprovalComment =
                    await this.apiBroker.GetApprovalCommentByIdAsync(randomApprovalComment.Id);

                // then
                actualApprovalComment.Should().BeEquivalentTo(modifiedApprovalComment, options => options
                    .Excluding(property => property.UpdatedBy)
                    .Excluding(property => property.UpdatedWhen));
            }
            finally
            {
                await RemoveApprovalCommentAndApprovalAsync(
                    randomApprovalComment.Id,
                    randomApproval.Id);
            }
        }

        /// <summary>
        /// The rule has to hold on modify as well as add, or the text can simply be emptied one
        /// write later — the comment lands with substance, is blanked by the next PUT, and goes on
        /// holding its approval shut while saying nothing.
        ///
        /// <para>The second assertion is the one that matters: the stored text must be UNCHANGED.
        /// A refusal that had already written the blank would be no protection at all.</para>
        /// </summary>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ShouldReturnBadRequestOnPutIfCommentIsBlankedAsync(string blankComment)
        {
            // given
            (Approval randomApproval, ApprovalComment randomApprovalComment) =
                await PostRandomApprovalCommentOnOpenApprovalAsync();

            ApprovalComment blankedApprovalComment =
                UpdateApprovalCommentWithRandomValues(randomApprovalComment);

            blankedApprovalComment.Comment = blankComment;

            try
            {
                // when
                var putApprovalCommentTask =
                    this.apiBroker.PutApprovalCommentAsync(blankedApprovalComment).AsTask();

                // then
                await Assert.ThrowsAsync<HttpResponseBadRequestException>(() => putApprovalCommentTask);

                ApprovalComment actualApprovalComment =
                    await this.apiBroker.GetApprovalCommentByIdAsync(randomApprovalComment.Id);

                actualApprovalComment.Comment.Should().Be(randomApprovalComment.Comment);
            }
            finally
            {
                await RemoveApprovalCommentAndApprovalAsync(
                    randomApprovalComment.Id,
                    randomApproval.Id);
            }
        }
    }
}
