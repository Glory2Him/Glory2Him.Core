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


namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    public partial class ApiBroker
    {
        /// <summary>
        /// Whether the booted host brought the event substrate up at startup.
        /// </summary>
        /// <remarks>
        /// <para>Narrow, in the same spirit as the page-size accessor beside it: what leaves
        /// here is one fact, not the container.</para>
        ///
        /// <para>It reads the outcome rather than probing a publish, and that is deliberate. A
        /// publish from a test carries no ambient caller, so the approval workflow's own read is
        /// refused before it can do anything (#287) — a probe would fail for a reason that has
        /// nothing to do with whether the substrate is running. In production a fact is
        /// published during a request, where a caller exists.</para>
        /// </remarks>
        internal bool IsEventSubstrateRegistered =>
            Program.IsCoreEventSubstrateRegistered;
    }
}
