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

using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations;

namespace Glory2Him.Core.Services.Orchestrations.Associations
{
    internal partial interface IAssociationOrchestrationService
    {
        /// <summary>
        /// Suggests an association between two endpoints — the retrieve-or-add flow of design
        /// §7.4. The caller supplies only the raw endpoints (<c>EntityAType</c>/<c>EntityAKeyId</c>,
        /// <c>EntityBType</c>/<c>EntityBKeyId</c>, and <c>UserId</c> for a reaction); the
        /// orchestration resolves each endpoint against its foundation service and DERIVES the
        /// scope, group id and content type — none of which it accepts from the caller, because
        /// the content type is an authorization input and a caller-set scope could claim
        /// <c>AllVersions</c> on an entity with no group.
        ///
        /// <para>It then looks the canonical pair up over the unfiltered store and branches:
        /// an unoccupied pair is inserted (<c>Created</c>); an occupied one is returned as-is —
        /// <c>AlreadyApproved</c> for an approved row, <c>AlreadyPending</c> for any other
        /// non-deleted state (pending and rejected are indistinguishable to the caller by
        /// design); the caller's own soft-deleted row is resurrected to <c>Draft</c>
        /// (<c>Restored</c>), while a moderator-deleted row is never resurrected, so a takedown
        /// cannot be laundered by resubmitting. The result carries a status and the row id and
        /// NOTHING else — the row body would leak another user's authorship.</para>
        /// </summary>
        ValueTask<AssociationSuggestionResult> AddAssociationAsync(
            Association association,
            CancellationToken cancellationToken = default);
    }
}
