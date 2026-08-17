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

        /// <summary>
        /// The same route WITH the reason on the query string. Separate from the call above so a
        /// test can prove the parameter is bound and reaches the column — a unit test passes it as
        /// a direct argument and never exercises model binding at all (exposer skill §1.8).
        /// </summary>
        public async ValueTask<ApprovalReview> DeleteApprovalReviewByIdAsync(
            Guid approvalReviewId,
            string deletionReason) =>
            await this.apiFactoryClient.DeleteContentAsync<ApprovalReview>(
                $"{approvalReviewsRelativeUrl}/{approvalReviewId}"
                    + $"?deletionReason={Uri.EscapeDataString(deletionReason)}");

        public async ValueTask<ApprovalReview> HardDeleteApprovalReviewByIdAsync(Guid approvalReviewId) =>
            await this.apiFactoryClient.DeleteContentAsync<ApprovalReview>(
                $"{approvalReviewsRelativeUrl}/{approvalReviewId}/hard");

        /// <summary>
        /// Dismissal takes the id and nothing else — there is no flag and no un-dismiss, which is
        /// why this needs no counterpart to the comment exposer's missing-flag helper.
        /// </summary>

        /// <summary>
        /// Posts a review as RAW JSON, so a test can send members the typed acceptance model does
        /// not carry — specifically the <c>Approval</c> navigation the Core entity exposes. Returns
        /// the status code rather than a deserialised entity, because the interesting outcomes are
        /// a refusal and a side effect rather than a body.
        /// </summary>
        public async ValueTask<System.Net.HttpStatusCode> PostApprovalReviewRawAsync(string json)
        {
            using var content = new System.Net.Http.StringContent(
                json,
                System.Text.Encoding.UTF8,
                "application/json");

            System.Net.Http.HttpResponseMessage response =
                await this.httpClient.PostAsync(approvalReviewsRelativeUrl, content);

            return response.StatusCode;
        }

        public async ValueTask<ApprovalReview> DismissApprovalReviewAsync(Guid approvalReviewId) =>
            await this.apiFactoryClient.PostContentAsync<object, ApprovalReview>(
                $"{approvalReviewsRelativeUrl}/{approvalReviewId}/dismiss",
                content: new object());
    }
}
