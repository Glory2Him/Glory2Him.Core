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

using Glory2Him.Core.Services.Foundations.ContentItemAssociations;
using Microsoft.Extensions.DependencyInjection;

namespace Glory2Him.Core.Registrations
{
    public static partial class ServiceRegistration
    {
        /// <summary>
        /// Registers the ContentItemAssociation foundation service and its event handlers with the container.
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
        public static IServiceCollection AddContentItemAssociationService(this IServiceCollection services)
        {
            services.AddSingleton<IContentItemAssociationService, ContentItemAssociationService>();

            return services;
        }
    }
}
