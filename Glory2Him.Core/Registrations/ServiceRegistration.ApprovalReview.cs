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

using Glory2Him.Core.Services.Foundations.ApprovalReviews;
using Microsoft.Extensions.DependencyInjection;

namespace Glory2Him.Core.Registrations
{
    public static partial class ServiceRegistration
    {
        /// <summary>
        /// Registers the ApprovalReview foundation service and its event handlers with the container.
        /// </summary>
        /// <remarks>
        /// <para>The singleton lifetime here is a HOLDOVER, and its original reason is gone.
        /// <see cref="EventSubscriptionRegistration"/> used to bind substrate handlers into the
        /// singleton <c>IEventBroker</c> as method groups, which captured whatever instance it
        /// resolved; it now resolves the service per delivery through an
        /// <c>IServiceScopeFactory</c>, so the substrate no longer forces any lifetime.</para>
        ///
        /// <para><b>A singleton here is a captive-dependency hazard.</b> This service takes
        /// <c>IStorageBroker</c>, and a host that registers that scoped — as any host with a
        /// per-request <c>DbContext</c> will — gets one DbContext captured for the life of the
        /// process. <c>Glory2Him.WebApp</c> does not use this registration for that reason: it
        /// wires the service scoped in its own <c>CoreRegistration</c>.</para>
        ///
        /// <para>Left as-is rather than changed silently, because the lifetime is part of this
        /// method's contract and no caller exercises it today. A host wanting per-request
        /// semantics should register the service itself.</para>
        ///
        /// <para>The caller is responsible for registering the brokers the service depends
        /// on.</para>
        /// </remarks>
        public static IServiceCollection AddApprovalReviewService(this IServiceCollection services)
        {
            // ONE object behind two doors. Registering the same implementation against two
            // service types would make two of them, because the container keys on the service
            // type rather than the implementation. The second door resolves THROUGH the first,
            // so the implementation type never enters the container as a service in its own
            // right — a host that also called a differently-lifetimed registration for it would
            // otherwise get whichever descriptor happened to land last.
            services.AddSingleton<IApprovalReviewService, ApprovalReviewService>();

            // Registered HERE rather than left to the host, because ApprovalOrchestrationService
            // takes this seam and the interface is internal — a host outside Core's friend set
            // could not supply it itself, so AddApprovalOrchestrationService would throw at
            // resolution with no route to a fix.
            services.AddSingleton<IApprovalReviewWorkflowService>(provider =>
                (ApprovalReviewService)provider.GetRequiredService<IApprovalReviewService>());

            return services;
        }
    }
}
