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
using Glory2Him.WebApp.Tests.Acceptance.Models.ApprovalReviews;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    public partial class ApiBroker
    {
        private const string approvalReviewsRelativeUrl = "api/approvalreviews";

        public async ValueTask<ApprovalReview> PostApprovalReviewAsync(ApprovalReview approvalReview) =>
            await this.apiFactoryClient.PostContentAsync(approvalReviewsRelativeUrl, approvalReview);

        public async ValueTask<List<ApprovalReview>> GetAllApprovalReviewsAsync() =>
            await this.apiFactoryClient.GetContentAsync<List<ApprovalReview>>(
                $"{approvalReviewsRelativeUrl}/");

        public async ValueTask<List<ApprovalReview>> GetSpecificApprovalReviewByIdAsync(
            Guid approvalReviewId) =>
            await this.apiFactoryClient.GetContentAsync<List<ApprovalReview>>(
                $"{approvalReviewsRelativeUrl}?$filter=Id eq {approvalReviewId}");

        public async ValueTask<ApprovalReview> GetApprovalReviewByIdAsync(Guid approvalReviewId) =>
            await this.apiFactoryClient.GetContentAsync<ApprovalReview>(
                $"{approvalReviewsRelativeUrl}/{approvalReviewId}");

        public async ValueTask<ApprovalReview> PutApprovalReviewAsync(ApprovalReview approvalReview) =>
            await this.apiFactoryClient.PutContentAsync(approvalReviewsRelativeUrl, approvalReview);

        public async ValueTask<ApprovalReview> DeleteApprovalReviewByIdAsync(Guid approvalReviewId) =>
            await this.apiFactoryClient.DeleteContentAsync<ApprovalReview>(
                $"{approvalReviewsRelativeUrl}/{approvalReviewId}");

        public async ValueTask<ApprovalReview> HardDeleteApprovalReviewByIdAsync(Guid approvalReviewId) =>
            await this.apiFactoryClient.DeleteContentAsync<ApprovalReview>(
                $"{approvalReviewsRelativeUrl}/{approvalReviewId}/hard");

        /// <summary>
        /// Dismissal takes the id and nothing else — there is no flag and no un-dismiss, which is
        /// why this needs no counterpart to the comment exposer's missing-flag helper.
        /// </summary>
        public async ValueTask<ApprovalReview> DismissApprovalReviewAsync(Guid approvalReviewId) =>
            await this.apiFactoryClient.PostContentAsync<object, ApprovalReview>(
                $"{approvalReviewsRelativeUrl}/{approvalReviewId}/dismiss",
                content: new object());
    }
}
