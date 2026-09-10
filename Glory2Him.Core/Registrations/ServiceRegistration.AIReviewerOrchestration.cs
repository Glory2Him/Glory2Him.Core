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

using Glory2Him.Core.Services.Orchestrations.AIReviewers;
using Microsoft.Extensions.DependencyInjection;

namespace Glory2Him.Core.Registrations
{
    public static partial class ServiceRegistration
    {
        /// <summary>
        /// Registers the AI reviewer orchestration service with the container (design §8.6.2).
        /// The caller is responsible for registering what it reaches: the Approval foundation
        /// (<c>AddApprovalService</c>, which also supplies the workflow seam this binds to), the
        /// AI reviewer assignment foundation (<c>AddAIReviewerAssignmentService</c>), and the
        /// access, event-envelope and logging brokers.
        ///
        /// <para>Registered separately from <c>AddApprovalOrchestrationService</c> rather than
        /// folded into it: the two are separate contracts over separate resources, and a host
        /// that serves one is entitled to decline the other.</para>
        /// </summary>
        public static IServiceCollection AddAIReviewerOrchestrationService(this IServiceCollection services)
        {
            services.AddSingleton<IAIReviewerOrchestrationService, AIReviewerOrchestrationService>();

            return services;
        }
    }
}
