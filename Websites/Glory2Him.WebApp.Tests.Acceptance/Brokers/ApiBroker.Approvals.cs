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
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Orchestrations.AIReviewers;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    /// <summary>
    /// Arrangement for the approve endpoints, shared by every approvable entity.
    ///
    /// <para>The approve decision reads the APPROVAL row's status, not the entity's, and no
    /// endpoint in this host creates that row — submitting an entity writes only its own
    /// <c>ApprovalStatus</c>, and the round would normally be opened by the approval
    /// orchestration reacting to the published fact. So the round has to be arranged beneath
    /// HTTP. These are real rows written through the host's own storage broker, read back by the
    /// production <c>AccessBroker</c> through the production <c>StorageBroker</c>.</para>
    ///
    /// <para>Entity ROWS are arranged per entity, in <c>ApiBroker.&lt;Entity&gt;Arrangements.cs</c>.
    /// What lives here is only what every approvable entity shares.</para>
    ///
    /// <para>The AI reviewer calls at the foot of this file address their OWN resource —
    /// <c>api/AIReviewers</c>, not a sub-resource of the round — and are kept here because they
    /// share this file's arrangement of a submitted round rather than because they share its
    /// prefix. Their own constant says so.</para>
    /// </summary>
    public partial class ApiBroker
    {
        /// <summary>
        /// Opens a submitted approval round against any entity.
        ///
        /// <para>The <paramref name="entityType"/> is a PARAMETER rather than a constant, and
        /// that is load-bearing: the approve decision resolves the entity behind the approval
        /// and composes the reviewer's expected role from its type, so a round arranged under
        /// the wrong type is decided against the wrong role and the test passes or fails for a
        /// reason it never meant to express. This arrangement was written for Tag and hard-coded
        /// it; the second exposer to need it is what turned the constant into an argument.</para>
        /// </summary>
        public async ValueTask<Approval> InsertSubmittedApprovalAsync(
            EntityType entityType,
            Guid entityId,
            string authorUserId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var approval = new Approval
            {
                Id = Guid.NewGuid(),
                EntityType = entityType,
                EntityId = entityId,
                ApprovalStatus = ApprovalStatus.Submitted,
                CreatedBy = authorUserId,
                CreatedWhen = now,
                UpdatedBy = authorUserId,
                UpdatedWhen = now
            };

            return await this.storageBroker.InsertApprovalAsync(approval);
        }

        /// <summary>
        /// The approving caller must hold no active review of their own — a publisher who filed
        /// a review has spent their vote on the round — so the reviewer here is a third party.
        /// </summary>
        public async ValueTask<ApprovalReview> InsertApprovedReviewAsync(
            Guid approvalId,
            string reviewerUserId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var approvalReview = new ApprovalReview
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                StatusId = ApprovalStatus.Approved,
                Comment = "Arranged by the acceptance suite.",
                IsDeleted = false,
                CreatedBy = reviewerUserId,
                CreatedWhen = now,
                UpdatedBy = reviewerUserId,
                UpdatedWhen = now
            };

            return await this.storageBroker.InsertApprovalReviewAsync(approvalReview);
        }

        public async ValueTask RemoveApprovalReviewAsync(ApprovalReview approvalReview) =>
            await this.storageBroker.DeleteApprovalReviewAsync(approvalReview);

        /// <summary>
        /// An outstanding invitation on a round (§7.9) — somebody who was asked and has not
        /// answered. Arranged beneath HTTP like the round itself: the POST that would create one
        /// gates on the invited person holding the review tier in the SECURITY store, which is a
        /// different arrangement entirely from the one this row exists to serve.
        /// </summary>
        public async ValueTask<ApprovalReviewRequest> InsertPendingReviewRequestAsync(
            Guid approvalId,
            string requestedUserId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var approvalReviewRequest = new ApprovalReviewRequest
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                RequestedUserId = requestedUserId,
                RequestedUserDisplayName = "Arranged Invitee",
                IsDeleted = false,
                CreatedBy = Guid.NewGuid().ToString(),
                CreatedWhen = now,
                UpdatedBy = Guid.NewGuid().ToString(),
                UpdatedWhen = now
            };

            return await this.storageBroker.InsertApprovalReviewRequestAsync(
                approvalReviewRequest);
        }

        /// <summary>
        /// The stored row, read straight from Core. The caller-facing GET filters deleted rows
        /// out, which is what a test about a RETIREMENT needs to see past — a soft delete and a
        /// hard one look identical through that read, and only one of them is §7.9 rule 8.
        /// </summary>
        public async ValueTask<ApprovalReviewRequest> GetCoreApprovalReviewRequestByIdAsync(
            Guid approvalReviewRequestId) =>
            await this.storageBroker.SelectApprovalReviewRequestByIdAsync(
                approvalReviewRequestId);

        public async ValueTask RemoveApprovalReviewRequestAsync(
            ApprovalReviewRequest approvalReviewRequest)
        {
            ApprovalReviewRequest stored =
                await this.storageBroker.SelectApprovalReviewRequestByIdAsync(
                    approvalReviewRequest.Id);

            if (stored is not null)
            {
                await this.storageBroker.DeleteApprovalReviewRequestAsync(stored);
            }
        }

        /// <summary>
        /// Who has been asked and has not yet answered, through the exposer the moderation panel
        /// calls (§16.7.4). Requesting tier only, so the caller has to be one.
        /// </summary>
        public async ValueTask<List<ApprovalReviewRequest>> GetApprovalReviewRequestsAsync(
            EntityType entityType,
            Guid entityId) =>
            await this.apiFactoryClient.GetContentAsync<List<ApprovalReviewRequest>>(
                $"api/approvals/{entityType}/{entityId}/ReviewRequests");

        public async ValueTask RemoveApprovalAsync(Approval approval) =>
            await this.storageBroker.DeleteApprovalAsync(approval);

        // ITS OWN RESOURCE, and therefore its own prefix. Berean used to hang off the approval
        // round as api/Approvals/{entityType}/{entityId}/AIReviewer; it has its own exposer now
        // (AIReviewersController), and "api/[controller]" over that name yields this. Lower-cased
        // to match every other constant in these brokers — ASP.NET routing is case-insensitive,
        // so the casing here is a house style rather than a claim about the route.
        //
        // The entity key is still the whole address: a moderation panel knows the item it is
        // showing and has never been handed an assignment's id.
        private const string aiReviewersRelativeUrl = "api/aiReviewers";

        /// <summary>
        /// Berean's status on a round (§8.6.2), keyed on the entity like every other read in this
        /// family — a moderation panel knows the item it is showing and never the approval's id.
        /// </summary>
        public async ValueTask<AIReviewerStatus> GetAIReviewerStatusAsync(
            EntityType entityType,
            Guid entityId) =>
            await this.apiFactoryClient.GetContentAsync<AIReviewerStatus>(
                $"{aiReviewersRelativeUrl}/{entityType}/{entityId}");

        /// <summary>
        /// The UPSERT. No query values and no body — the entity key is the whole request, and what
        /// the same call MEANS (create, reset, or nothing to do) is decided from the round's own
        /// state rather than from anything sent here.
        /// </summary>
        public async ValueTask<AIReviewerAssignment> PostAIReviewerAsync(
            EntityType entityType,
            Guid entityId) =>
            await this.apiFactoryClient.PostContentAsync<object, AIReviewerAssignment>(
                relativeUrl: $"{aiReviewersRelativeUrl}/{entityType}/{entityId}",
                content: new { });

        public async ValueTask<AIReviewerAssignment> DeleteAIReviewerAsync(
            EntityType entityType,
            Guid entityId) =>
            await this.apiFactoryClient.DeleteContentAsync<AIReviewerAssignment>(
                $"{aiReviewersRelativeUrl}/{entityType}/{entityId}");

        /// <summary>
        /// The same route, answered with its STATUS CODE rather than a deserialised row. The
        /// withdrawal is idempotent and answers <c>204</c> when nothing was assigned, and a
        /// no-content response has no body for the typed call above to read — which makes the code
        /// the only thing that distinguishes "removed it" from "there was nothing to remove".
        /// </summary>
        public async ValueTask<HttpStatusCode> DeleteAIReviewerReturningStatusAsync(
            EntityType entityType,
            Guid entityId)
        {
            HttpResponseMessage response = await this.httpClient.DeleteAsync(
                $"{aiReviewersRelativeUrl}/{entityType}/{entityId}");

            return response.StatusCode;
        }

        /// <summary>
        /// The stored row by its own id — read BENEATH the endpoints, because that is the only way
        /// to see a withdrawn assignment. Removal is a soft delete, and the round-keyed read every
        /// endpoint uses is filtered to live rows, so an assertion about what withdrawal did to
        /// the row cannot be made through the API that made it.
        /// </summary>
        public async ValueTask<AIReviewerAssignment> GetCoreAIReviewerAssignmentByIdAsync(
            Guid aiReviewerAssignmentId) =>
            await this.storageBroker.SelectAIReviewerAssignmentByIdAsync(aiReviewerAssignmentId);

        /// <summary>
        /// Physical teardown, for the same reason <c>ApprovalSetting</c>'s is physical: the row
        /// must not outlive the test, and a soft-deleted one still occupies its round's slot for
        /// nothing.
        /// </summary>
        public async ValueTask RemoveCoreAIReviewerAssignmentByIdAsync(Guid aiReviewerAssignmentId)
        {
            AIReviewerAssignment storedAssignment =
                await this.storageBroker.SelectAIReviewerAssignmentByIdAsync(
                    aiReviewerAssignmentId);

            if (storedAssignment is not null)
            {
                await this.storageBroker.DeleteAIReviewerAssignmentAsync(storedAssignment);
            }
        }
    }
}
