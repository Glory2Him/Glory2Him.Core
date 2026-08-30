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

using System;
using System.Collections.Generic;
using Glory2Him.Core.Brokers.Storages.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Glory2Him.Core.Tests.Integration.Brokers
{
    /// <summary>
    /// The LocalDB catalogue an integration fixture owns: how it is named, how its schema is
    /// created, and the guard that stands between <c>EnsureDeleted</c> and anything that is
    /// not a test database.
    ///
    /// <para>Shared because a second fixture needing a real <see cref="StorageBroker"/> should
    /// not re-derive any of it. Every rule here was arrived at by something going wrong once,
    /// and the comments record which.</para>
    /// </summary>
    internal static class IntegrationDatabase
    {
        // Every database these fixtures are willing to create or drop starts with this. The
        // guard below refuses to touch anything else, because a drop here is unrecoverable.
        //
        // Named Glory2Him.Core_Integration_<process id>, matching the per-tier layout issue
        // #351 settled on for all three stores (Core, Events, Security) across both the
        // acceptance and integration tiers.
        private const string TestDatabasePrefix = "Glory2Him.Core_Integration_";

        // The connection string comes from a TEST-ONLY key.
        //
        // It would be natural to read `Glory2HimConnectionString` — that is what StorageBroker
        // itself reads — and to layer environment variables over the JSON so the server is
        // overridable. That combination is a database-destroying trap: `Glory2HimConnectionString`
        // is the production key, so any host, container or CI job that configures Glory2Him.Core
        // through the environment sets exactly the variable this fixture would then resolve, and
        // `EnsureDeleted` would drop whatever it points at. The test key cannot collide with it.
        private const string TestConnectionStringKey = "Glory2HimCoreIntegrationConnectionString";

        /// <summary>
        /// Resolves a per-process catalogue for the calling fixture.
        /// </summary>
        /// <param name="catalogueSuffix">
        /// Distinguishes one fixture's catalogue from another's. Each fixture creates and
        /// DROPS its own schema, so two sharing a catalogue would delete each other's rows
        /// mid-run — xUnit only serialises within a collection, not across them.
        /// </param>
        public static IConfiguration BuildConfiguration(string catalogueSuffix = "")
        {
            IConfiguration fileConfiguration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

            string template =
                fileConfiguration.GetConnectionString(TestConnectionStringKey)
                    ?? throw new InvalidOperationException(
                        $"appsettings.json must define ConnectionStrings:{TestConnectionStringKey}.");

            // A fixed database name means two concurrent runs — a CLI run alongside the IDE
            // test runner, or two agent worktrees — drop the database out from under each
            // other, since EF's drop kills live connections. One database per process removes
            // the shared resource rather than trying to synchronise access to it.
            var connectionStringBuilder = new SqlConnectionStringBuilder(template)
            {
                InitialCatalog = TestDatabasePrefix + Environment.ProcessId + catalogueSuffix
            };

            // StorageBroker reads the production key, so the resolved value is handed over
            // under that name — but only from this in-memory layer, never from the ambient
            // environment.
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["ConnectionStrings:Glory2HimConnectionString"] =
                        connectionStringBuilder.ConnectionString
                })
                .Build();
        }

        /// <summary>
        /// Builds the schema from the CURRENT model rather than by running migrations, because
        /// these tests are about whether the mapping AS IT STANDS behaves — whether EF can
        /// translate a predicate against it, and whether the constraints it declares are
        /// really created. Running migrations would test the migration history instead, which
        /// is a different question and has its own coverage: the history is replayed onto an
        /// empty database by the build's migration-script step rather than here.
        ///
        /// <para>This runs once because xUnit builds a collection fixture once — no static
        /// flag and no ProcessExit hook. An earlier version used both; the hook did not fire
        /// under the test host and left one orphaned database per run behind.</para>
        /// </summary>
        public static void EnsureSchema(StorageBroker storageBroker)
        {
            GuardAgainstDroppingANonTestDatabase(storageBroker);

            // drops a stale catalogue from a previous run that reused this process id
            storageBroker.Database.EnsureDeleted();
            storageBroker.Database.EnsureCreated();
        }

        /// <summary>
        /// Best effort on the way out. An orphaned per-process catalogue is a nuisance, and
        /// the next run with the same process id clears it, but throwing from teardown would
        /// mask the run's real result.
        /// </summary>
        public static void Drop(StorageBroker storageBroker)
        {
            try
            {
                GuardAgainstDroppingANonTestDatabase(storageBroker);
                storageBroker.Database.EnsureDeleted();
            }
            catch
            {
                // deliberately swallowed — see the summary above
            }
        }

        // Belt and braces. BuildConfiguration already makes it impossible to resolve anything
        // but a per-process test catalogue, but a drop is irreversible and silent, so the name
        // is checked immediately before the call rather than trusted from a distance.
        private static void GuardAgainstDroppingANonTestDatabase(StorageBroker storageBroker)
        {
            string databaseName = storageBroker.Database.GetDbConnection().Database;

            bool isTestDatabase = databaseName.StartsWith(
                TestDatabasePrefix, StringComparison.Ordinal);

            if (isTestDatabase is false)
            {
                throw new InvalidOperationException(
                    $"Refusing to drop '{databaseName}': integration tests only ever create and " +
                    $"drop databases named '{TestDatabasePrefix}<process id>'.");
            }
        }
    }
}
