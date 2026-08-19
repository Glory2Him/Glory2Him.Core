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

using Glory2Him.Core.Services.Orchestrations.Approvals;
using Microsoft.Extensions.DependencyInjection;

namespace Glory2Him.Core.Registrations
{
    public static partial class ServiceRegistration
    {
        /// <summary>
        /// Registers the approval orchestration service with the container. The caller is
        /// responsible for registering the three approval foundation services and the brokers
        /// it depends on.
        ///
        /// <para>It takes no entity services: the decision reaches its entity as a command
        /// event the owning service already listens for, rather than as a call (§16.7.1).</para>
        /// </summary>
        public static IServiceCollection AddApprovalOrchestrationService(this IServiceCollection services)
        {
            services.AddSingleton<IApprovalOrchestrationService, ApprovalOrchestrationService>();

            return services;
        }
    }
}
