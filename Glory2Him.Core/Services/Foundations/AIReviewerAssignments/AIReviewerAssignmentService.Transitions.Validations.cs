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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments.Exceptions;

namespace Glory2Him.Core.Services.Foundations.AIReviewerAssignments
{
    internal partial class AIReviewerAssignmentService
    {
        private static void ValidateOnReturnStaleAIReviewerAssignmentToPending(
            Guid aiReviewerAssignmentId) =>
            Validate(
                message: "AI reviewer assignment is invalid, fix the errors and try again.",
                (Rule: IsInvalid(aiReviewerAssignmentId),
                    Parameter: nameof(AIReviewerAssignment.Id)));

        // Returning an assignment to pending belongs to the approval workflow alone (§8.8 rule 1,
        // §8.6 HR-4). A moderator who wants Berean asked again uses the public upsert, which
        // records THEM as having asked; this one records the system, and it means something
        // different — that the content moved under a pass nobody re-requested.
        //
        // Unreachable in practice, and deliberately kept — but narrower than it looks. The one
        // public seam, ReturnStaleAIReviewerAssignmentToPendingAsync, calls CreateSystemAsync
        // itself before delegating, so anything entering THERE mints a passing context by
        // construction and can never fail this. What it actually guards is a future second caller
        // of the private DoReturnStaleAIReviewerAssignmentToPendingAsync that supplies its own
        // envelope.
        private static void ValidateReturnToPendingIsTheWorkflowsOwnAct(
            SecurityContext securityContext)
        {
            if (securityContext.IsSystemIdentity is false)
            {
                throw new UnauthorizedAIReviewerAssignmentException(
                    message: "Returning a stale AI reviewer assignment to pending is the approval "
                        + "workflow's own act; no user may perform it.");
            }
        }

        // THE AUTHORIZATION BOUNDARY ON THE AUTOMATIC ADD (§8.6.2.1 security boundary rule 2).
        // There is no tier left to ask for on this path — the system identity holds no roles —
        // so this assertion is the whole of the gate rather than one clause of it.
        //
        // A separate method from the sibling above even though the two currently test the same
        // field: they refuse different acts and say so, and the moment one of them needs a rule
        // the other must not have, a shared validator could not give it one without changing
        // both.
        //
        // Unreachable through the public seam for the sibling's reason — that seam calls
        // CreateSystemAsync itself before delegating — so what it actually guards is a future
        // second caller of the private DoAddAutomaticAIReviewerAssignmentAsync supplying its own
        // envelope.
        private static void ValidateAutomaticAssignmentIsTheWorkflowsOwnAct(
            SecurityContext securityContext)
        {
            if (securityContext.IsSystemIdentity is false)
            {
                throw new UnauthorizedAIReviewerAssignmentException(
                    message: "Assigning the AI reviewer automatically is the approval workflow's "
                        + "own act; no user may perform it.");
            }
        }
    }
}
