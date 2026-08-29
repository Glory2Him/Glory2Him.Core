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

using Glory2Him.Core.Brokers.Storages.Identity;
using Glory2Him.Core.Services.Foundations.IdentityUsers;
using Microsoft.Extensions.DependencyInjection;

namespace Glory2Him.Core.Registrations
{
    public static partial class ServiceRegistration
    {
        /// <summary>
        /// Registers the read-only identity-store window (design §12.7.1) — the broker over the
        /// <c>Glory2HimSecurityConnection</c> database and the foundation that reads role
        /// membership through it.
        /// </summary>
        /// <remarks>
        /// <para>Registered together because they are useless apart: the service exists only to
        /// read through this broker, and the broker exposes nothing anybody else should call.
        /// Both are internal, so a host outside Core's friend set could not supply either itself
        /// — which is why the approval orchestration's registration depends on this one having
        /// run rather than on the host wiring them by hand.</para>
        ///
        /// <para>The broker is a <c>DbContext</c>, so a host with per-request semantics should
        /// register it scoped in its own composition root instead — as
        /// <c>Glory2Him.WebApp</c> does — for the same captive-dependency reason the other
        /// foundation registrations here carry.</para>
        /// </remarks>
        public static IServiceCollection AddIdentityUserService(this IServiceCollection services)
        {
            services.AddSingleton<IIdentityCoreStorageBroker, IdentityCoreStorageBroker>();
            services.AddSingleton<IIdentityUserService, IdentityUserService>();

            return services;
        }
    }
}
