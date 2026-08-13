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
using Glory2Him.Core.Brokers.Storages.Sql;
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
    // failure here disables the tag endpoints, while login, the SPA and every other API keep
    // serving. Both operations are idempotent, so the next start retries them anyway.
    internal static async Task InitializeCoreAsync(WebApplication app)
    {
        const int maxAttempts = 5;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                MigrateCoreDatabase(app);
                await RegisterCoreEventAddressesAsync(app);

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
                    "Core initialization failed after {MaxAttempts} attempts; the tag endpoints "
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

    private static async Task RegisterCoreEventAddressesAsync(WebApplication app)
    {
        var eventBroker = app.Services.GetRequiredService<IEventBroker>();

        await eventBroker.RegisterEventParticipantAsync();
        await eventBroker.RegisterEventAddressesAsync();
    }
}
