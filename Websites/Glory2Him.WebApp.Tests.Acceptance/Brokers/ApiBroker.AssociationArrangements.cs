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
using CoreAssociation = Glory2Him.Core.Models.Foundations.Associations.Association;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    /// <summary>
    /// Association rows torn down beneath HTTP — the sibling of
    /// <c>ApiBroker.ReactionArrangements.cs</c>. No endpoint removes an association physically.
    /// </summary>
    public partial class ApiBroker
    {
        /// <summary>
        /// Physically removes an association if it is still there, and the approval round its
        /// write opened. A reader's reaction is written at <c>Submitted</c>, so the approval
        /// workflow opens a round on it as the request is served; leaving that round behind would
        /// leave the test's data behind.
        /// </summary>
        public async ValueTask RemoveCoreAssociationByIdAsync(Guid associationId)
        {
            Approval storedApproval =
                await this.storageBroker.SelectApprovalByEntityAsync(
                    EntityType.Association,
                    associationId);

            if (storedApproval is not null)
            {
                await this.storageBroker.DeleteApprovalAsync(storedApproval);
            }

            CoreAssociation storedAssociation =
                await this.storageBroker.SelectAssociationByIdAsync(associationId);

            if (storedAssociation is not null)
            {
                await this.storageBroker.DeleteAssociationAsync(storedAssociation);
            }
        }
    }
}
