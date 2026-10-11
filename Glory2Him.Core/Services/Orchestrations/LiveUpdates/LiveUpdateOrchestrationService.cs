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

using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Services.Foundations.ContentItems;
using Glory2Him.Core.Services.Foundations.LiveUpdates;

namespace Glory2Him.Core.Services.Orchestrations.LiveUpdates
{
    /// <summary>
    /// A count message spans two entities — the <c>LiveUpdate</c> it sends and the
    /// <c>ContentItem</c> whose canonically visible version it names — so the forwarder is an
    /// orchestration (§ARC12.1 rule 2). Its two services are foundation services, the same kind.
    ///
    /// <para>The host's winning setting arrives through <c>IAccessBroker</c> and leaves the entity
    /// count where it is (§APR8.6.1 rule 3). It holds no <c>IEventBroker</c>, because it publishes
    /// nothing, and no <c>IEventEnvelopeBroker</c>, because it mints nothing and reads no caller
    /// (§EVN26 rule 4).</para>
    /// </summary>
    internal partial class LiveUpdateOrchestrationService : ILiveUpdateOrchestrationService
    {
        private readonly IContentItemService contentItemService;
        private readonly ILiveUpdateService liveUpdateService;
        private readonly IAccessBroker accessBroker;
        private readonly IEnvelopeIntegrityBroker envelopeIntegrityBroker;
        private readonly ILoggingBroker loggingBroker;

        public LiveUpdateOrchestrationService(
            IContentItemService contentItemService,
            ILiveUpdateService liveUpdateService,
            IAccessBroker accessBroker,
            IEnvelopeIntegrityBroker envelopeIntegrityBroker,
            ILoggingBroker loggingBroker)
        {
            this.contentItemService = contentItemService;
            this.liveUpdateService = liveUpdateService;
            this.accessBroker = accessBroker;
            this.envelopeIntegrityBroker = envelopeIntegrityBroker;
            this.loggingBroker = loggingBroker;
        }
    }
}
