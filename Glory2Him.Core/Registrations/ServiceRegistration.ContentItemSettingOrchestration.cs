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

using Glory2Him.Core.Services.Orchestrations.ContentItemSettings;
using Microsoft.Extensions.DependencyInjection;

namespace Glory2Him.Core.Registrations
{
    public static partial class ServiceRegistration
    {
        /// <summary>
        /// Registers the content item setting orchestration service with the container. The caller
        /// is responsible for registering everything it takes: the two foundation services it
        /// coordinates — the content item setting service and the content item service —
        /// <c>IEnvelopeIntegrityBroker</c>, which the event-path handler verifies inbound
        /// envelopes through (§14.6 rule 4), and <c>ILoggingBroker</c>, which every exception
        /// path here writes through.
        ///
        /// <para><b>This service is now reached on TWO paths.</b> It is the layer an exposer binds
        /// to (§12.1), and since #456 it is also what <c>ContentItemSetting-Adding</c> resolves
        /// per delivery. A host that wires the substrate must register it, or that address throws
        /// mid-delivery and the failure is recorded against the listener rather than surfaced.
        /// Note that the substrate resolves it out of a scope per delivery, so a host serving
        /// concurrent deliveries wants a scoped lifetime rather than the singleton this helper
        /// registers — which is what <c>Glory2Him.WebApp</c>'s own <c>AddCoreServices</c> does
        /// instead of calling this. The singleton here matches the other twenty-two
        /// <c>ServiceRegistration</c> helpers and is left alone rather than made the one
        /// exception; the lifetime question belongs to all of them together.</para>
        /// </summary>
        public static IServiceCollection AddContentItemSettingOrchestrationService(
            this IServiceCollection services)
        {
            services.AddSingleton<
                IContentItemSettingOrchestrationService,
                ContentItemSettingOrchestrationService>();

            return services;
        }
    }
}
