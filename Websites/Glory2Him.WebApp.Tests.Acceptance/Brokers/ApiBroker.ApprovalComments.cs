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
using Glory2Him.WebApp.Tests.Acceptance.Models.ApprovalComments;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    public partial class ApiBroker
    {
        private const string approvalCommentsRelativeUrl = "api/approvalcomments";

        public async ValueTask<ApprovalComment> PostApprovalCommentAsync(ApprovalComment approvalComment) =>
            await this.apiFactoryClient.PostContentAsync(approvalCommentsRelativeUrl, approvalComment);

        public async ValueTask<List<ApprovalComment>> GetAllApprovalCommentsAsync() =>
            await this.apiFactoryClient.GetContentAsync<List<ApprovalComment>>(
                $"{approvalCommentsRelativeUrl}/");

        public async ValueTask<List<ApprovalComment>> GetSpecificApprovalCommentByIdAsync(
            Guid approvalCommentId) =>
            await this.apiFactoryClient.GetContentAsync<List<ApprovalComment>>(
                $"{approvalCommentsRelativeUrl}?$filter=Id eq {approvalCommentId}");

        public async ValueTask<ApprovalComment> GetApprovalCommentByIdAsync(Guid approvalCommentId) =>
            await this.apiFactoryClient.GetContentAsync<ApprovalComment>(
                $"{approvalCommentsRelativeUrl}/{approvalCommentId}");

        public async ValueTask<ApprovalComment> PutApprovalCommentAsync(ApprovalComment approvalComment) =>
            await this.apiFactoryClient.PutContentAsync(approvalCommentsRelativeUrl, approvalComment);

        public async ValueTask<ApprovalComment> DeleteApprovalCommentByIdAsync(Guid approvalCommentId) =>
            await this.apiFactoryClient.DeleteContentAsync<ApprovalComment>(
                $"{approvalCommentsRelativeUrl}/{approvalCommentId}");

        public async ValueTask<ApprovalComment> HardDeleteApprovalCommentByIdAsync(Guid approvalCommentId) =>
            await this.apiFactoryClient.DeleteContentAsync<ApprovalComment>(
                $"{approvalCommentsRelativeUrl}/{approvalCommentId}/hard");

        public async ValueTask<ApprovalComment> ResolveApprovalCommentAsync(
            Guid approvalCommentId,
            bool isResolved) =>
            await this.apiFactoryClient.PostContentAsync<object, ApprovalComment>(
                $"{approvalCommentsRelativeUrl}/{approvalCommentId}/resolve"
                    + $"?isResolved={isResolved.ToString().ToLowerInvariant()}",
                content: new object());

        /// <summary>
        /// The same route with the flag left off entirely. Exists only so a test can prove the
        /// request is refused: a plain <c>bool</c> that is merely absent binds to <c>false</c>,
        /// which would make this a silent un-resolve rather than an error.
        /// </summary>
        public async ValueTask<ApprovalComment> ResolveApprovalCommentWithNoFlagAsync(
            Guid approvalCommentId) =>
            await this.apiFactoryClient.PostContentAsync<object, ApprovalComment>(
                $"{approvalCommentsRelativeUrl}/{approvalCommentId}/resolve",
                content: new object());
    }
}
