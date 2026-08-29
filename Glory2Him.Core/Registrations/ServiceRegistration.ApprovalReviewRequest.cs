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

using Glory2Him.Core.Services.Foundations.ApprovalReviewRequests;
using Microsoft.Extensions.DependencyInjection;

namespace Glory2Him.Core.Registrations
{
    public static partial class ServiceRegistration
    {
        /// <summary>
        /// Registers the ApprovalReviewRequest foundation service and its event handlers with the
        /// container.
        /// </summary>
        /// <remarks>
        /// <para>The singleton lifetime matches every sibling foundation registration here and
        /// carries the same caveat: this service takes <c>IStorageBroker</c>, so a host that
        /// registers that scoped — as any host with a per-request <c>DbContext</c> will — would
        /// capture one DbContext for the life of the process. <c>Glory2Him.WebApp</c> does not use
        /// this registration for that reason; it wires the service scoped in its own
        /// <c>CoreRegistration</c>.</para>
        ///
        /// <para>The caller is responsible for registering the brokers the service depends
        /// on.</para>
        /// </remarks>
        public static IServiceCollection AddApprovalReviewRequestService(this IServiceCollection services)
        {
            // ONE object behind two doors. Registering the same implementation against two
            // service types would make two of them, because the container keys on the service
            // type rather than the implementation. The second door resolves THROUGH the first,
            // so the implementation type never enters the container as a service in its own
            // right.
            services.AddSingleton<IApprovalReviewRequestService, ApprovalReviewRequestService>();

            // Registered HERE rather than left to the host, because the approval orchestration
            // will take this seam (§7.9 rule 6) and the interface is internal — a host outside
            // Core's friend set could not supply it itself.
            services.AddSingleton<IApprovalReviewRequestWorkflowService>(provider =>
                (ApprovalReviewRequestService)provider
                    .GetRequiredService<IApprovalReviewRequestService>());

            return services;
        }
    }
}
