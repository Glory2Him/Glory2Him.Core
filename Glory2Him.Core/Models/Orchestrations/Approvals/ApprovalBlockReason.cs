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

using G2H.Security.Client.Models.Foundations.Access;

namespace Glory2Him.Core.Models.Orchestrations.Approvals
{
    /// <summary>
    /// One reason an approval cannot be granted right now, in a shape a UI can both render and
    /// branch on (design §16.7.2).
    ///
    /// <para>Both halves are needed. <see cref="Message"/> alone would force a UI to match on
    /// English to do anything but print it; <see cref="Code"/> alone would force every consumer
    /// to carry its own copy of the wording, which is how two screens end up describing the same
    /// block differently.</para>
    ///
    /// <para>The message is composed in Core rather than by the decision function: a policy
    /// engine that owns user-facing English also owns presentation, and fixes one language into
    /// a package shared by every consumer.</para>
    /// </summary>
    public class ApprovalBlockReason
    {
        /// <summary>
        /// The stable reason code. A UI branches on this — it does not change when the wording
        /// does, and it survives translation.
        /// </summary>
        public required AccessDenialReason Code { get; init; }

        /// <summary>
        /// The reason in readable English, already carrying its own numbers where it has any —
        /// "1 of 3 required approvals recorded", not "approval threshold not met".
        /// </summary>
        public required string Message { get; init; }
    }
}
