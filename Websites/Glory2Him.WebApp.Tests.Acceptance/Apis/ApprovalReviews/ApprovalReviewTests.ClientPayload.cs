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
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Foundations.Approvals;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ApprovalReviews
{
    /// <summary>
    /// THE PAYLOAD THE BROWSER ACTUALLY SENDS, byte for byte, because the typed broker cannot
    /// send it: a React client has no <c>DateTimeOffset</c> to leave at default, and the shape it
    /// posts is whatever its model says. The audit fields are the server's to stamp
    /// (<c>ApplyAddAuditValuesAsync</c>), so the honest client payload carries NONE of them —
    /// and a client that sends them empty is refused in model binding, before any service sees
    /// it, with a ProblemDetails body that names no <c>message</c>. That refusal reached a
    /// moderator as "Your review could not be recorded. Please try again." with no way to learn
    /// why.
    /// </summary>
    public partial class ApprovalReviewApiTests
    {
        [Fact]
        public async Task ShouldAcceptTheClientsVoteWithNoAuditFieldsAtAllAsync()
        {
            // given: an open round, and the payload the review panel composes
            Approval approval =
                await this.apiBroker.InsertOpenApprovalAsync(Guid.NewGuid().ToString());

            var reviewId = Guid.NewGuid();

            string clientJson = $$"""
                {
                    "id": "{{reviewId}}",
                    "approvalId": "{{approval.Id}}",
                    "statusId": 2,
                    "comment": "",
                    "isDeleted": false
                }
                """;

            try
            {
                // when
                HttpStatusCode actualStatusCode =
                    await this.apiBroker.PostApprovalReviewRawAsync(clientJson);

                // then
                actualStatusCode.Should().Be(HttpStatusCode.Created);
            }
            finally
            {
                await RemoveApprovalReviewAndApprovalAsync(reviewId, approval.Id);
            }
        }

        /// <summary>
        /// The shape that WAS being sent: audit fields present and empty. Pinned as refused so a
        /// client that regresses to it fails here rather than in a moderator's toast — and so the
        /// refusal's nature is on record: model binding, not policy.
        /// </summary>
        [Fact]
        public async Task ShouldRefuseAVoteCarryingEmptyAuditFieldsBeforeAnyPolicyIsReadAsync()
        {
            // given
            Approval approval =
                await this.apiBroker.InsertOpenApprovalAsync(Guid.NewGuid().ToString());

            var reviewId = Guid.NewGuid();

            string emptyAuditJson = $$"""
                {
                    "id": "{{reviewId}}",
                    "approvalId": "{{approval.Id}}",
                    "statusId": 2,
                    "comment": "",
                    "createdBy": "",
                    "createdWhen": "",
                    "updatedBy": "",
                    "updatedWhen": "",
                    "isDeleted": false
                }
                """;

            try
            {
                // when
                HttpStatusCode actualStatusCode =
                    await this.apiBroker.PostApprovalReviewRawAsync(emptyAuditJson);

                // then
                actualStatusCode.Should().Be(HttpStatusCode.BadRequest);
            }
            finally
            {
                await RemoveApprovalReviewAndApprovalAsync(reviewId, approval.Id);
            }
        }
    }
}
