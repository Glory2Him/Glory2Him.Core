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
using System.IO;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    /// <summary>
    /// The five databases this suite runs against, and their whole lifecycle.
    ///
    /// <para>The acceptance host boots the real portal, so it touches every store the portal
    /// has: Core's schema, the EventHighway substrate, and Identity. Before this existed the
    /// suite resolved all three from the host's own <c>appsettings.json</c> and therefore ran
    /// against the developer's <c>Glory2Him.Core</c>, <c>Glory2Him.Events</c> and
    /// <c>Glory2Him.Security</c>. Entity teardown was sound, but the event ledger is written by
    /// the system under test and nothing deletes from it, so it grew by roughly 180
    /// <c>ProcessedEvents</c> rows per run, forever (#302). The fix is where the suite writes,
    /// not whether.</para>
    ///
    /// <para><b>Identity is isolated too</b>, which #302 did not ask for. It accumulates
    /// nothing — <c>SeedData</c> is idempotent — but it is still written to, and the suite reads
    /// the seeded administrator back out of it. Leaving it shared would have kept the run
    /// dependent on whatever roles the developer happens to hold locally, which is a flake
    /// waiting to happen rather than a growth problem.</para>
    /// </summary>
    internal static class AcceptanceDatabaseBroker
    {
        /// <summary>
        /// Every catalogue this suite creates is named <c>Glory2Him.&lt;Store&gt;_Acceptance_
        /// &lt;process id&gt;</c> — e.g. <c>Glory2Him.Core_Acceptance_1234</c> — matching the
        /// per-tier layout issue #351 settled on for both the acceptance and integration suites.
        ///
        /// <para>The guard below checks EXACT membership in <see cref="DatabaseNames"/> rather
        /// than a shared string prefix: issue #351 also gave the normal-use databases the same
        /// leading segment (<c>Glory2Him.Core</c>, <c>Glory2Him.Events</c>,
        /// <c>Glory2Him.Security</c>), so a prefix check alone could no longer tell a test
        /// catalogue apart from a production one.</para>
        /// </summary>
        private const string TestDatabaseSegment = "_Acceptance_";

        /// <summary>
        /// A TEST-ONLY key, and the reason matters.
        ///
        /// <para>It would be natural to read the production keys — <c>Glory2HimConnectionString</c>,
        /// <c>EventHighwayConnectionString</c>, <c>Glory2HimSecurityConnection</c> — and layer
        /// environment variables over the JSON so the server is overridable. That combination is
        /// a database-destroying trap: any host, container or CI job that configures the portal
        /// through the environment sets exactly the variables this class would then resolve, and
        /// the drop below would take whatever they point at. A key nothing in production reads
        /// cannot collide with one.</para>
        /// </summary>
        private const string TestConnectionStringKey = "Glory2HimAcceptanceConnectionString";

        /// <summary>
        /// One template, every catalogue. The integration suite carries a key per store because
        /// its two stores could in principle sit on different servers; here they cannot — the
        /// portal boots them all from one <c>appsettings.json</c> against one LocalDB instance —
        /// so a single template is both simpler and impossible to diverge. Pointing the suite at
        /// another server is one edit.
        /// </summary>
        private static readonly string ConnectionStringTemplate = ReadConnectionStringTemplate();

        /// <summary>
        /// A fixed catalogue name means two concurrent runs — a CLI run alongside the IDE test
        /// runner, or two agent worktrees — drop the database out from under each other, since
        /// the drop below kills live connections. One catalogue per process removes the shared
        /// resource rather than trying to synchronise access to it.
        /// </summary>
        private static readonly string[] DatabaseNames = new[]
        {
            CatalogFor("Core"),
            CatalogFor("Events"),
            CatalogFor("Security"),
            CatalogFor("SecurityRehearsal"),
            CatalogFor("SecurityFallbackRehearsal")
        };

        // The drop MUST reach the same server the catalogues are created on. Both are derived
        // from the one template for that reason — an independently written master connection
        // string would silently diverge the moment the template named another server, creating
        // the catalogues in one place and trying to drop them in another.
        private static readonly string MasterConnectionString =
            WithCatalog(ConnectionStringTemplate, "master");

        /// <summary>
        /// A FOURTH Identity catalogue, and the host never sees it — it is deliberately absent
        /// from <see cref="ConnectionStringOverrides"/> below.
        ///
        /// <para>It exists because a data migration can only be tested against data, and the
        /// suite's own Security catalogue is created empty and migrated on the way up, so every
        /// statement in a rename migration matches zero rows there. A test that needs a
        /// POPULATED pre-migration store has to build one, and it must not build it in the
        /// catalogue the running host is reading.</para>
        ///
        /// <para>It is listed in <see cref="DatabaseNames"/> so it inherits the same
        /// create-clean-and-drop lifecycle as the other three rather than growing a second
        /// one — including the startup drop, which is what makes a rehearsal deterministic
        /// after a previous run that reused this process id.</para>
        /// </summary>
        internal static string SecurityRehearsalConnectionString =>
            WithCatalog(ConnectionStringTemplate, DatabaseNames[3]);

        /// <summary>
        /// A FIFTH, for the one branch the rehearsal above cannot reach from its own arrangement.
        ///
        /// <para>The rename migration takes a different path when <c>Administrators</c> does not
        /// already exist — it renames <c>Admin</c> into it rather than merging and dropping — and
        /// a single store cannot be in both states at once. Two pre-states, two catalogues.</para>
        /// </summary>
        internal static string SecurityFallbackRehearsalConnectionString =>
            WithCatalog(ConnectionStringTemplate, DatabaseNames[4]);

        /// <summary>
        /// The resolved per-run connection strings, keyed by the PRODUCTION configuration keys
        /// the portal actually reads. <see cref="TestWebApplicationFactory"/> layers these over
        /// the host's configuration in memory — never through the ambient environment, which is
        /// what keeps the test key and the production keys from meeting.
        ///
        /// <para>Nothing here creates a schema. Core's is built by <c>Database.Migrate()</c> in
        /// <c>Program.InitializeCoreAsync</c>, Identity's by <c>Database.MigrateAsync()</c> in
        /// <c>SeedData.MigrateAsync</c>, and EventHighway builds its own store on first use — so
        /// a per-run catalogue only has to let those run, which is the whole of the change on the
        /// creation side.</para>
        /// </summary>
        internal static IDictionary<string, string> ConnectionStringOverrides =>
            new Dictionary<string, string>
            {
                ["ConnectionStrings:Glory2HimConnectionString"] =
                    WithCatalog(ConnectionStringTemplate, DatabaseNames[0]),

                ["ConnectionStrings:EventHighwayConnectionString"] =
                    WithCatalog(ConnectionStringTemplate, DatabaseNames[1]),

                ["ConnectionStrings:Glory2HimSecurityConnection"] =
                    WithCatalog(ConnectionStringTemplate, DatabaseNames[2])
            };

        /// <summary>
        /// Drops every catalogue in <see cref="DatabaseNames"/>.
        ///
        /// <para>Called twice per run, and the two calls differ in exactly one way.</para>
        ///
        /// <para><b>At startup</b> (<paramref name="isBestEffort"/> false) it clears a stale
        /// catalogue left by a previous run that reused this process id. That drop is the premise
        /// the whole suite rests on — a stale Core row or a stale EventHighway listener would buy
        /// a green run against state the source no longer has — so it is allowed to throw.</para>
        ///
        /// <para><b>At teardown</b> (<paramref name="isBestEffort"/> true) it swallows. An
        /// orphaned per-process catalogue is a nuisance and the next run with the same process id
        /// clears it, whereas throwing from teardown would mask the run's real result.</para>
        /// </summary>
        internal static void DropDatabases(bool isBestEffort)
        {
            // The host has been torn down by the time the teardown call runs, but ADO.NET keeps
            // its pooled connections to all three catalogues alive behind it. SINGLE_USER below
            // would evict them anyway; clearing first means the drop is not racing a pool that
            // is still handing connections out.
            SqlConnection.ClearAllPools();

            foreach (string databaseName in DatabaseNames)
            {
                DropDatabase(databaseName, isBestEffort);
            }
        }

        private static void DropDatabase(string databaseName, bool isBestEffort)
        {
            GuardAgainstDroppingANonTestDatabase(databaseName);

            try
            {
                using var connection = new SqlConnection(MasterConnectionString);
                connection.Open();

                // Issued directly rather than through EF's EnsureDeleted: EventHighway owns its
                // schema and has no DbContext here, and by teardown the host that owned the other
                // two contexts is already gone. One mechanism for all three beats two.
                //
                // SINGLE_USER kills whatever connections survived the pool clear above.
                using var command = new SqlCommand(
                    $"IF DB_ID(@database) IS NOT NULL BEGIN " +
                    $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                    $"DROP DATABASE [{databaseName}]; END",
                    connection);

                command.Parameters.AddWithValue("@database", databaseName);
                command.ExecuteNonQuery();
            }
            catch (Exception dropException) when (isBestEffort)
            {
                // Teardown only. The startup call passes isBestEffort: false and is therefore NOT
                // caught here — see the summary above for why that one must not be swallowed.
                _ = dropException;
            }
        }

        // Belt and braces. The template resolution above already makes it impossible to name
        // anything but a per-process test catalogue, but a drop is irreversible and silent, so
        // the name is checked immediately before the call rather than trusted from a distance.
        //
        // Checked against the exact set this run computed, not a prefix: see the remark on
        // TestDatabaseSegment above for why a prefix is no longer a safeguard on its own.
        private static void GuardAgainstDroppingANonTestDatabase(string databaseName)
        {
            bool isTestDatabase = Array.IndexOf(DatabaseNames, databaseName) >= 0;

            if (isTestDatabase is false)
            {
                throw new InvalidOperationException(
                    $"Refusing to drop '{databaseName}': acceptance tests only ever create and " +
                    $"drop databases named 'Glory2Him.<Store>{TestDatabaseSegment}<process id>'.");
            }
        }

        private static string CatalogFor(string store) =>
            $"Glory2Him.{store}{TestDatabaseSegment}{Environment.ProcessId}";

        private static string WithCatalog(string template, string databaseName) =>
            new SqlConnectionStringBuilder(template) { InitialCatalog = databaseName }
                .ConnectionString;

        private static string ReadConnectionStringTemplate()
        {
            string settingsPath =
                Path.Combine(TestProjectPaths.ProjectDirectory, "appsettings.json");

            // Optional, so a missing FILE and a missing KEY both arrive at the one message
            // below rather than one of them surfacing as a bare FileNotFoundException. Both mean
            // the same thing to whoever hits it: this project is not supplying the template.
            IConfiguration fileConfiguration = new ConfigurationBuilder()
                .AddJsonFile(settingsPath, optional: true)
                .AddEnvironmentVariables()
                .Build();

            string template = fileConfiguration.GetConnectionString(TestConnectionStringKey);

            if (string.IsNullOrWhiteSpace(template))
            {
                throw new InvalidOperationException(
                    $"'{settingsPath}' must define "
                        + $"ConnectionStrings:{TestConnectionStringKey}. Without it this suite "
                        + "has no per-run catalogue to resolve, and the only alternative — "
                        + "falling back to the host's own connection strings — is the developer "
                        + "database this fixture exists to stay out of (#302).");
            }

            return template;
        }
    }
}
