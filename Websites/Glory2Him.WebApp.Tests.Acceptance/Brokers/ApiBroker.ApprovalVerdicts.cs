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
using System.Linq;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.WebApp.Tests.Acceptance.Models.Approvals;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    /// <summary>
    /// The verdict read (§16.7.2) over HTTP, and the one arrangement it needs: finding the
    /// approval a read may have opened beneath it, by the entity it keys on — the caller never
    /// learns the approval's id any other way, which is the whole reason the read is keyed on
    /// the entity.
    /// </summary>
    public partial class ApiBroker
    {
        private const string approvalsRelativeUrl = "api/approvals";

        public async ValueTask<ApprovalVerdict> GetApprovalVerdictAsync(
            EntityType entityType,
            Guid entityId) =>
            await this.apiFactoryClient.GetContentAsync<ApprovalVerdict>(
                $"{approvalsRelativeUrl}/{entityType}/{entityId}/Verdict");

        /// <summary>
        /// The decision (§16.7.3), keyed by the entity like the verdict. Everything rides the
        /// query string, and the bypass is a REQUEST the outcome answers — what lands on the
        /// row is decided against the policy and the caller's tier, never copied from here.
        /// </summary>
        public async ValueTask<ApprovalOutcome> PostApprovalDecisionAsync(
            EntityType entityType,
            Guid entityId,
            string decision,
            bool isBypassRequested = false,
            string bypassReason = null)
        {
            string url =
                $"{approvalsRelativeUrl}/{entityType}/{entityId}/Decision"
                    + $"?decision={decision}&isBypassRequested={isBypassRequested}";

            if (string.IsNullOrWhiteSpace(bypassReason) is false)
            {
                url += $"&bypassReason={Uri.EscapeDataString(bypassReason)}";
            }

            return await this.apiFactoryClient.PostContentAsync<object, ApprovalOutcome>(
                url,
                content: new object());
        }

        /// <summary>
        /// Unfiltered on purpose, like the orchestration's own probe (§9.7.2 rule 3): the key
        /// is occupied by a soft-deleted approval too, and a teardown has to reach it.
        /// </summary>
        public async ValueTask<Approval> GetCoreApprovalByEntityAsync(
            EntityType entityType,
            Guid entityId)
        {
            IQueryable<Approval> approvals = await this.storageBroker.SelectAllApprovalsAsync();

            return await approvals.FirstOrDefaultAsync(approval =>
                approval.EntityType == entityType && approval.EntityId == entityId);
        }
    }
}
