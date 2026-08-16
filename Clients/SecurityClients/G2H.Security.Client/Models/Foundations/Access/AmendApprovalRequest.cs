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

using System.Collections.Generic;

namespace G2H.Security.Client.Models.Foundations.Access
{
    /// <summary>
    /// Everything the "may this actor amend the approval record itself?" decision consults —
    /// which is only who is asking, and what the approval is about.
    ///
    /// <para>Distinct from <see cref="DecideApprovalRequest"/>, and deliberately much smaller.
    /// Deciding an approval applies a verdict and needs the policies, the review snapshot, the
    /// confidence score and the bypass flag. Amending the record is not a verdict: it is the
    /// write path §14.7 posture D rule 3 has reviewers move an approval's status through, and it
    /// has no business reading any of that.</para>
    ///
    /// <para><b>No <c>ApprovalState</c></b>, for a different reason than the dismissal decision's:
    /// there, the round is being re-opened underneath the operation; here, the round state is the
    /// very thing being moved. A window guard would refuse the operation's whole purpose.</para>
    ///
    /// <para><b>No <c>EntityCreatedBy</c>.</b> The owner branch of the gate is row-local — the
    /// service compares the actor against the stored <c>CreatedBy</c> itself, and that needs no
    /// cross-entity read. What is left here is the half a single-entity service cannot answer:
    /// is this actor in the review tier <i>for the entity behind this approval</i>, which for an
    /// association means either of its endpoints (§14.7 posture A′ rule 2).</para>
    /// </summary>
    public class AmendApprovalRequest
    {
        /// <summary>
        /// The user attempting to amend the approval.
        /// </summary>
        public required AccessActor Actor { get; init; }

        /// <summary>
        /// Every subject the actor could be authorised through — one for most entities, two for
        /// an association's endpoints. Holding a review-tier role for any one is enough.
        /// </summary>
        public required IReadOnlyList<RoleSubject> RoleSubjects { get; init; }
    }
}
