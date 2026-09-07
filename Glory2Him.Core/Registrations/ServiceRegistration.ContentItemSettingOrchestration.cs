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
        /// coordinates — the content item setting service and the content item service — and
        /// <c>ILoggingBroker</c>, which every exception path here writes through.
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
