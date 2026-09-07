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

using Xeptions;

namespace Glory2Him.Core.Models.Orchestrations.ContentItemSettings.Exceptions
{
    /// <summary>
    /// An add request arriving over the event substrate was not usable: the envelope, its content
    /// or its metadata was missing, or its signature did not verify against this address and the
    /// request direction (§14.6 rule 4).
    ///
    /// <para>The orchestration verifies for itself rather than leaning on the foundation's
    /// identical check further down, because this handler READS a second entity — the content
    /// item the row names — before the foundation is reached. Doing that on the word of an
    /// unverified envelope would be acting on a payload nothing has vouched for.</para>
    /// </summary>
    public class InvalidContentItemSettingEventOrchestrationException : Xeption
    {
        public InvalidContentItemSettingEventOrchestrationException(string message)
            : base(message)
        { }
    }
}
