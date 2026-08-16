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
using CoreApprovalComment = Glory2Him.Core.Models.Foundations.ApprovalComments.ApprovalComment;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ApprovalComments
{
    public partial class ApprovalCommentApiTests
    {
        /// <summary>
        /// Hard removal destroys the row, so the assertion goes past the API's own read posture
        /// and asks storage directly — a soft delete would still satisfy "the endpoint no longer
        /// returns it".
        /// </summary>
        [Fact]
        public async Task ShouldHardDeleteApprovalCommentByIdAsync()
        {
            // given
            (Approval randomApproval, ApprovalComment randomApprovalComment) =
                await PostRandomApprovalCommentOnOpenApprovalAsync();

            try
            {
                // when
                await this.apiBroker.HardDeleteApprovalCommentByIdAsync(randomApprovalComment.Id);

                CoreApprovalComment actualApprovalComment =
                    await this.apiBroker.GetCoreApprovalCommentByIdAsync(randomApprovalComment.Id);

                // then
                actualApprovalComment.Should().BeNull();
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
