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
        /// The service is registered as a singleton because
        /// <see cref="EventSubscriptionRegistration"/> binds its substrate handlers into the
        /// singleton <c>IEventBroker</c> as method groups. A scoped or transient registration
        /// would be captured by that singleton and outlive the scope it was resolved from; if
        /// the host needs a shorter lifetime, wire the subscription through an
        /// <c>IServiceScopeFactory</c> lambda instead of a method group.
        /// The caller is responsible for registering the brokers the service depends on.
        /// </remarks>
        public static IServiceCollection AddApprovalReviewService(this IServiceCollection services)
        {
            // ONE object behind two doors. Registering the same implementation against two
            // service types would make two of them, because the container keys on the service
            // type rather than the implementation.
            services.AddSingleton<ApprovalReviewService>();

            services.AddSingleton<IApprovalReviewService>(provider =>
                provider.GetRequiredService<ApprovalReviewService>());

            // Registered HERE rather than left to the host, because ApprovalOrchestrationService
            // takes this seam and the interface is internal — a host outside Core's friend set
            // could not supply it itself, so AddApprovalOrchestrationService would throw at
            // resolution with no route to a fix.
            services.AddSingleton<IApprovalReviewWorkflowService>(provider =>
                provider.GetRequiredService<ApprovalReviewService>());

            return services;
        }
    }
}
