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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Securities;
using Glory2Him.WebApp.Tests.Acceptance.Brokers;
using Glory2Him.WebApp.Tests.Acceptance.Models.ApprovalReviews;
using Tynamix.ObjectFiller;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ApprovalReviews
{
    /// <summary>
    /// The approval-review endpoints over real HTTP, against the real host, real LocalDB and a
    /// real EventHighway store. Nothing in the stack is substituted except authentication.
    ///
    /// <para>Unlike the comment exposer, almost nothing here is reachable by a bare authenticated
    /// caller: recording a verdict needs a review role (§8.9 — only reviewers review), and
    /// dismissal needs the publisher tier. So the fixture acts as a REVIEWER by default rather
    /// than the seeded administrator, and the tests that need another posture say so.</para>
    /// </summary>
    [Collection(nameof(ApiTestCollection))]
    public partial class ApprovalReviewApiTests
    {
        private readonly ApiBroker apiBroker;

        public ApprovalReviewApiTests(ApiBroker apiBroker)
        {
            this.apiBroker = apiBroker;

            // The acting caller is shared client state, so it is reset here rather than left to
            // whichever test ran last. A reviewer is the default because it is the posture most
            // of these operations require.
            this.reviewerUserId = this.apiBroker.ActAs(
                Guid.NewGuid().ToString(),
                Roles.Reviewers);
        }

        private readonly string reviewerUserId;

        private static int GetRandomNumber() =>
            new IntRange(min: 2, max: 5).GetValue();

        private static string GetRandomComment() =>
            new MnemonicString(wordCount: 4).GetValue();

        /// <summary>
        /// A verdict the API will accept. <c>CreatedBy</c> is deliberately left unset: the service
        /// binds it to the acting caller, and a value here would be either ignored or refused.
        /// </summary>
        private static ApprovalReview CreateRandomApprovalReview(Guid approvalId) =>
            new ApprovalReview
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                StatusId = (int)ApprovalStatus.Approved,
                Comment = GetRandomComment(),
                IsDeleted = false,
            };

        /// <summary>
        /// Opens a round and posts one review on it as the acting reviewer, returning both so the
        /// test can tear the pair down in the order the foreign key demands.
        /// </summary>
        private async ValueTask<(Approval Approval, ApprovalReview ApprovalReview)>
            PostRandomApprovalReviewOnOpenApprovalAsync()
        {
            Approval approval =
                await this.apiBroker.InsertOpenApprovalAsync(Guid.NewGuid().ToString());

            ApprovalReview randomApprovalReview = CreateRandomApprovalReview(approval.Id);

            ApprovalReview createdApprovalReview =
                await this.apiBroker.PostApprovalReviewAsync(randomApprovalReview);

            return (approval, createdApprovalReview);
        }

        /// <summary>
        /// Tears a review and its round down in one call, in the only order the foreign key
        /// allows. Physical removal on both, so nothing is left in the dev database.
        /// </summary>
        private async ValueTask RemoveApprovalReviewAndApprovalAsync(
            Guid approvalReviewId,
            Guid approvalId)
        {
            await this.apiBroker.RemoveCoreApprovalReviewByIdAsync(approvalReviewId);
            await this.apiBroker.RemoveApprovalByIdAsync(approvalId);
        }
    }
}
