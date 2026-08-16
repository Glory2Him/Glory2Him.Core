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
        public async Task ShouldPostApprovalCommentAsync()
        {
            // given
            Approval randomApproval =
                await this.apiBroker.InsertOpenApprovalAsync(Guid.NewGuid().ToString());

            ApprovalComment randomApprovalComment = CreateRandomApprovalComment(randomApproval.Id);
            ApprovalComment inputApprovalComment = randomApprovalComment;
            ApprovalComment expectedApprovalComment = inputApprovalComment;

            try
            {
                // when
                await this.apiBroker.PostApprovalCommentAsync(inputApprovalComment);

                ApprovalComment actualApprovalComment =
                    await this.apiBroker.GetApprovalCommentByIdAsync(inputApprovalComment.Id);

                // then
                actualApprovalComment.Should().BeEquivalentTo(expectedApprovalComment, options => options
                    .Excluding(property => property.CreatedBy)
                    .Excluding(property => property.CreatedWhen)
                    .Excluding(property => property.UpdatedBy)
                    .Excluding(property => property.UpdatedWhen));
            }
            finally
            {
                await RemoveApprovalCommentAndApprovalAsync(
                    inputApprovalComment.Id,
                    randomApproval.Id);
            }
        }

        /// <summary>
        /// The parent round is not decoration. A comment aimed at an approval that does not
        /// exist is refused at the access gate rather than left to the foreign key, so the
        /// caller gets an authorization answer and no row is written (§7.7 rule 1).
        /// </summary>
        [Fact]
        public async Task ShouldReturnUnauthorizedOnPostIfParentApprovalDoesNotExistAsync()
        {
            // given
            ApprovalComment randomApprovalComment = CreateRandomApprovalComment(Guid.NewGuid());

            // when
            var postApprovalCommentTask =
                this.apiBroker.PostApprovalCommentAsync(randomApprovalComment).AsTask();

            // then
            await Assert.ThrowsAsync<HttpResponseUnauthorizedException>(() => postApprovalCommentTask);
        }
    }
}
