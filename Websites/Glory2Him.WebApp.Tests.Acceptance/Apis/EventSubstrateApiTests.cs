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

using FluentAssertions;
using Glory2Him.WebApp.Tests.Acceptance.Brokers;
using Xunit;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis
{
    /// <summary>
    /// Proves the event substrate is actually running in the booted host.
    ///
    /// <para>Startup registration is deliberately non-fatal: a failure is retried, logged and
    /// then swallowed, so a broken event store disables the Core endpoints rather than taking
    /// the portal down. That is the right posture and it has a consequence — every other test
    /// in this suite passes just as well with the substrate completely dead. Nothing else here
    /// publishes an event, so nothing else would notice.</para>
    ///
    /// <para>Until this host called <c>RegisterAsync</c>, no subscription in
    /// <c>EventSubscriptionRegistration</c> was reachable in the only deployment that exists,
    /// and the reactive half of the approval workflow did not run at all. This test is what
    /// keeps that from quietly becoming true again.</para>
    /// </summary>
    [Collection(nameof(ApiTestCollection))]
    public class EventSubstrateApiTests
    {
        private readonly ApiBroker apiBroker;

        public EventSubstrateApiTests(ApiBroker apiBroker) =>
            this.apiBroker = apiBroker;

        [Fact]
        public void ShouldBringTheEventSubstrateUpAtStartup()
        {
            // given, when: the host booted and registered the participant, all 165 event
            // addresses and every one of the 109 subscriptions

            // then
            this.apiBroker.IsEventSubstrateRegistered.Should().BeTrue(
                because: "until this host called RegisterAsync the substrate was dormant — no " +
                    "subscription was reachable in the only deployment that exists, and the " +
                    "reactive half of the approval workflow did not run at all. Startup " +
                    "swallows a registration failure by design so a broken event store cannot " +
                    "take the portal down, which means nothing else in this suite would notice " +
                    "it going dormant again");
        }
    }
}
