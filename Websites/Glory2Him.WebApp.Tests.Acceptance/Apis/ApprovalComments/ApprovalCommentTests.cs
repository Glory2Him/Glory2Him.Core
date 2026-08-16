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
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.WebApp.Tests.Acceptance.Brokers;
using Glory2Him.WebApp.Tests.Acceptance.Models.ApprovalComments;
using Tynamix.ObjectFiller;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ApprovalComments
{
    [Collection(nameof(ApiTestCollection))]
    public partial class ApprovalCommentApiTests
    {
        private readonly ApiBroker apiBroker;

        public ApprovalCommentApiTests(ApiBroker apiBroker)
        {
            this.apiBroker = apiBroker;

            // The acting caller is shared client state, so it is reset here rather than left to
            // whichever test ran last.
            this.apiBroker.ActAsSeededAdministrator();
        }

        private static int GetRandomNumber() =>
            new IntRange(min: 2, max: 5).GetValue();

        private static ApprovalComment UpdateApprovalCommentWithRandomValues(
            ApprovalComment inputApprovalComment)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            ApprovalComment updatedApprovalComment = CreateRandomApprovalComment(inputApprovalComment.ApprovalId);
            updatedApprovalComment.Id = inputApprovalComment.Id;
            updatedApprovalComment.CreatedWhen = inputApprovalComment.CreatedWhen;
            updatedApprovalComment.CreatedBy = inputApprovalComment.CreatedBy;
            updatedApprovalComment.UpdatedWhen = now;
            updatedApprovalComment.IsResolved = inputApprovalComment.IsResolved;
            updatedApprovalComment.IsDeleted = inputApprovalComment.IsDeleted;
            updatedApprovalComment.DeletionReason = inputApprovalComment.DeletionReason;

            return updatedApprovalComment;
        }

        /// <summary>
        /// Opens a round and posts one comment on it, returning both so the test can tear the
        /// pair down in the order the foreign key demands.
        /// </summary>
        private async ValueTask<(Approval Approval, ApprovalComment ApprovalComment)>
            PostRandomApprovalCommentOnOpenApprovalAsync()
        {
            Approval approval =
                await this.apiBroker.InsertOpenApprovalAsync(Guid.NewGuid().ToString());

            ApprovalComment randomApprovalComment = CreateRandomApprovalComment(approval.Id);

            ApprovalComment createdApprovalComment =
                await this.apiBroker.PostApprovalCommentAsync(randomApprovalComment);

            return (approval, createdApprovalComment);
        }

        private async ValueTask<List<ApprovalComment>> PostRandomApprovalCommentsAsync(Guid approvalId)
        {
            int randomNumber = GetRandomNumber();
            var randomApprovalComments = new List<ApprovalComment>();

            for (int index = 0; index < randomNumber; index++)
            {
                randomApprovalComments.Add(
                    await this.apiBroker.PostApprovalCommentAsync(
                        CreateRandomApprovalComment(approvalId)));
            }

            return randomApprovalComments;
        }

        /// <summary>
        /// Tears a comment and its round down in one call, in the only order the foreign key
        /// allows. Physical removal on both, so nothing is left in the dev database.
        /// </summary>
        private async ValueTask RemoveApprovalCommentAndApprovalAsync(
            Guid approvalCommentId,
            Guid approvalId)
        {
            await this.apiBroker.RemoveCoreApprovalCommentByIdAsync(approvalCommentId);
            await this.apiBroker.RemoveApprovalByIdAsync(approvalId);
        }

        private static ApprovalComment CreateRandomApprovalComment(Guid approvalId) =>
            CreateRandomApprovalCommentFiller(approvalId).Create();

        private static Filler<ApprovalComment> CreateRandomApprovalCommentFiller(Guid approvalId)
        {
            string user = Guid.NewGuid().ToString();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var filler = new Filler<ApprovalComment>();

            filler.Setup()
                .OnType<DateTimeOffset>().Use(now)
                .OnType<DateTimeOffset?>().Use(now)

                // The parent round is the one thing a comment may not invent: it is a real
                // foreign key, and the access gate reads the row behind it.
                .OnProperty(approvalComment => approvalComment.ApprovalId).Use(approvalId)

                // Outstanding at birth, so the resolve transition has something to settle — a
                // resolution that changes nothing is refused rather than treated as idempotent.
                .OnProperty(approvalComment => approvalComment.IsResolved).Use(false)

                .OnProperty(approvalComment => approvalComment.IsDeleted).Use(false)
                .OnProperty(approvalComment => approvalComment.DeletionReason).Use((string)null)
                .OnProperty(approvalComment => approvalComment.DeletedBy).Use((string)null)
                .OnProperty(approvalComment => approvalComment.DeletedWhen).Use((DateTimeOffset?)null)

                .OnProperty(approvalComment => approvalComment.CreatedWhen).Use(now)
                .OnProperty(approvalComment => approvalComment.CreatedBy).Use(user)
                .OnProperty(approvalComment => approvalComment.UpdatedWhen).Use(now)
                .OnProperty(approvalComment => approvalComment.UpdatedBy).Use(user);

            return filler;
        }
    }
}
