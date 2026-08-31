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

using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Registrations;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.WebApp.Data;
using Microsoft.EntityFrameworkCore;

public partial class Program
{
    internal static Action<WebApplicationBuilder>? TestConfigurationOverrides { get; set; } = null;

    internal static void ConfigurationOverridesForTesting(WebApplicationBuilder builder)
    {
        TestConfigurationOverrides?.Invoke(builder);
    }

    // Core's StorageBroker configures itself in OnConfiguring with no EnableRetryOnFailure, and a
    // sleeping LocalDB instance throws on the first connect — the same transient the identity seed
    // below already retries around. Neither of these is worth taking the whole portal down for: a
    // failure here disables the Core endpoints, while login, the SPA and every other API keep
    // serving. Both operations are idempotent, so the next start retries them anyway.
    internal static async Task InitializeCoreAsync(WebApplication app)
    {
        const int maxAttempts = 5;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                MigrateCoreDatabase(app);
                await ContentItemSettingSeedData.SeedAsync(app.Services);
                await ReactionSeedData.SeedAsync(app.Services);
                await RegisterCoreEventSubstrateAsync(app);

                return;
            }
            catch (Exception coreInitializationException) when (attempt < maxAttempts)
            {
                app.Logger.LogWarning(
                    coreInitializationException,
                    "Core initialization attempt {Attempt}/{MaxAttempts} failed; retrying.",
                    attempt,
                    maxAttempts);

                await Task.Delay(TimeSpan.FromSeconds(2));
            }
            catch (Exception coreInitializationException)
            {
                app.Logger.LogError(
                    coreInitializationException,
                    "Core initialization failed after {MaxAttempts} attempts; the Core endpoints "
                        + "will not serve until this is resolved.",
                    maxAttempts);
            }
        }
    }

    private static void MigrateCoreDatabase(WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();

        var storageBroker =
            (StorageBroker)scope.ServiceProvider.GetRequiredService<IStorageBroker>();

        storageBroker.Database.Migrate();
    }

    // Brings the whole substrate up, not just its addresses: the participant, all 165 event
    // addresses, and every one of the 109 subscriptions. RegisterAsync does all three, so this
    // is a superset of the participant-and-addresses call it replaces.
    //
    // Until this existed the substrate was dormant — every handler on every service was
    // unreachable in the only deployment there is, and the reactive half of the approval
    // workflow did not run at all. This is the call that makes a published fact reach the
    // handler subscribed to it.
    //
    // It is safe to run here, under the same retry and the same non-fatal posture as the
    // migration above, because registration is idempotent: participant, addresses and listeners
    // are all written through a RetrieveOrAdd against a stable id, so a restart re-registers
    // nothing. A failure still leaves the portal serving and disables only the Core endpoints.
    private static async Task RegisterCoreEventSubstrateAsync(WebApplication app)
    {
        var eventSubscriptionRegistration =
            app.Services.GetRequiredService<IEventSubscriptionRegistration>();

        await eventSubscriptionRegistration.RegisterAsync();
        IsCoreEventSubstrateRegistered = true;
    }

    /// <summary>
    /// Whether the event substrate came up on this host. False until
    /// <see cref="RegisterCoreEventSubstrateAsync"/> completes.
    /// </summary>
    /// <remarks>
    /// Recorded because startup registration is deliberately non-fatal: a failure is retried,
    /// logged and then swallowed, so a broken event store disables the Core endpoints rather
    /// than taking the portal down. That posture leaves nothing observable, and the acceptance
    /// suite would stay green with the substrate completely dead — which is what it was before
    /// this host registered it at all.
    /// </remarks>
    internal static bool IsCoreEventSubstrateRegistered { get; private set; }
}
