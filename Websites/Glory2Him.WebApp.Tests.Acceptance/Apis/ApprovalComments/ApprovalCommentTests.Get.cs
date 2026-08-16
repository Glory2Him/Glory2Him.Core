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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.WebApp.Tests.Acceptance.Models.ApprovalComments;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ApprovalComments
{
    public partial class ApprovalCommentApiTests
    {
        [Fact]
        public async Task ShouldGetAllApprovalCommentsAsync()
        {
            // given
            Approval randomApproval =
                await this.apiBroker.InsertOpenApprovalAsync(Guid.NewGuid().ToString());

            List<ApprovalComment> randomApprovalComments =
                await PostRandomApprovalCommentsAsync(randomApproval.Id);

            List<ApprovalComment> expectedApprovalComments = randomApprovalComments;

            try
            {
                // when
                List<ApprovalComment> actualApprovalComments =
                    await this.apiBroker.GetAllApprovalCommentsAsync();

                // then
                foreach (ApprovalComment expectedApprovalComment in expectedApprovalComments)
                {
                    ApprovalComment actualApprovalComment = actualApprovalComments
                        .Single(approvalComment => approvalComment.Id == expectedApprovalComment.Id);

                    actualApprovalComment.Should().BeEquivalentTo(
                        expectedApprovalComment,
                        options => options
                            .Excluding(property => property.CreatedBy)
                            .Excluding(property => property.CreatedWhen)
                            .Excluding(property => property.UpdatedBy)
                            .Excluding(property => property.UpdatedWhen));
                }
            }
            finally
            {
                // Cleanup is driven off what was POSTED, not off what the read returned, runs
                // even when an assertion throws, and removes the rows rather than soft-deleting
                // them. The round goes last — the foreign key is NoAction.
                foreach (ApprovalComment postedApprovalComment in randomApprovalComments)
                {
                    await this.apiBroker.RemoveCoreApprovalCommentByIdAsync(postedApprovalComment.Id);
                }

                await this.apiBroker.RemoveApprovalByIdAsync(randomApproval.Id);
            }
        }
    }
}
