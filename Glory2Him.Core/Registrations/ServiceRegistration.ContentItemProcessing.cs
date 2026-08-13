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

using Glory2Him.Core.Services.Processings.ContentItems;
using Microsoft.Extensions.DependencyInjection;

namespace Glory2Him.Core.Registrations
{
    public static partial class ServiceRegistration
    {
        /// <summary>
        /// Registers the ContentItem processing service with the container.
        /// </summary>
        /// <remarks>
        /// Registered as a singleton to match the foundation services it composes (see
        /// <see cref="AddContentItemService"/>): future processing event subscriptions
        /// bind into the singleton <c>IEventBroker</c> as method groups, so a shorter
        /// lifetime would be captured by that singleton and outlive its scope.
        /// The caller is responsible for registering the foundation service and brokers
        /// this service depends on.
        /// </remarks>
        public static IServiceCollection AddContentItemProcessingService(this IServiceCollection services)
        {
            services.AddSingleton<IContentItemProcessingService, ContentItemProcessingService>();

            return services;
        }
    }
}
