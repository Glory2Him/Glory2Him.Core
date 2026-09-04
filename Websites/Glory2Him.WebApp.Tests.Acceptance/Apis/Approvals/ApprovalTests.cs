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

using Glory2Him.WebApp.Tests.Acceptance.Brokers;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Approvals
{
    /// <summary>
    /// The approval reads a moderation screen is built on, over real HTTP against the real host.
    /// The verdict (§16.7.2) is the one every control on that screen hangs off — the block
    /// reasons, the bypass, the decision — so a verdict that does not answer is a screen that
    /// shows nothing, which is exactly the shape this suite exists to catch.
    /// </summary>
    [Collection(nameof(ApiTestCollection))]
    public partial class ApprovalApiTests
    {
        private readonly ApiBroker apiBroker;

        public ApprovalApiTests(ApiBroker apiBroker)
        {
            this.apiBroker = apiBroker;

            // The acting caller is shared client state, so it is reset here rather than left to
            // whichever test ran last. An administrator, because a moderation screen is theirs.
            this.apiBroker.ActAsSeededAdministrator();
        }
    }
}
