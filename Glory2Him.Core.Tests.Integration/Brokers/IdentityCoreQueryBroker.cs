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
using System.Threading.Tasks;
using Glory2Him.Core.Brokers.Storages.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Glory2Him.Core.Tests.Integration.Brokers
{
    /// <summary>
    /// Stands up a real <see cref="IdentityCoreStorageBroker"/> against its own SECURITY
    /// database on LocalDB, giving §12.7.1's role-membership read (issue #351) a seeded store of
    /// its own rather than sharing the developer's <c>Glory2Him.Security</c>.
    ///
    /// <para><b>Seeding goes around the broker, not through it.</b> <see cref="
    /// IIdentityCoreStorageBroker"/> is deliberately Select-only — the identity store belongs to
    /// <c>Glory2Him.WebApp</c>'s <c>SecurityDbContext</c>, and Core writing to it would put two
    /// owners on one schema. A fixture that called <c>SaveChangesAsync</c> on the underlying
    /// <see cref="DbContext"/> to seed rows would work, but it would be exercising exactly the
    /// write path the interface exists to rule out. Raw ADO.NET keeps the seeding path
    /// completely outside the broker under test.</para>
    /// </summary>
    public sealed class IdentityCoreQueryBroker : IDisposable
    {
        // Every database this fixture is willing to create or drop starts with this. The guard
        // below refuses to touch anything else, because a drop here is unrecoverable.
        //
        // Named Glory2Him.Security_Integration_<process id>, matching the per-tier layout issue
        // #351 settled on for all three stores (Core, Events, Security) across both the
        // acceptance and integration tiers.
        private const string TestDatabasePrefix = "Glory2Him.Security_Integration_";

        // A TEST-ONLY key, for the same reason AssociationQueryBroker and EventSubstrateBroker
        // use one: the production key is Glory2HimSecurityConnection, and any host or CI job
        // that configures Core through the environment sets exactly that variable. A fixture
        // that resolved it would drop the identity store it points at. This key cannot collide
        // with it.
        private const string TestConnectionStringKey = "Glory2HimSecurityIntegrationConnectionString";

        private readonly IdentityCoreStorageBroker identityCoreStorageBroker;
        private readonly string connectionString;
        private readonly string databaseName;
        private readonly string masterConnectionString;

        public IdentityCoreQueryBroker()
        {
            this.databaseName = TestDatabasePrefix + Environment.ProcessId;
            string template = ReadConnectionStringTemplate();
            this.connectionString = WithCatalog(template, this.databaseName);
            this.masterConnectionString = WithCatalog(template, "master");

            // Dropped up front rather than only on the way out, for the same reason the other
            // two integration fixtures do it: a previous run that reused this process id would
            // otherwise leave stale rows behind, and this one is allowed to throw because a
            // silent failure here buys a green run against a stale store.
            DropTestDatabase(isBestEffort: false);

            IConfiguration configuration = BuildTestConfiguration(this.connectionString);
            this.identityCoreStorageBroker = new IdentityCoreStorageBroker(configuration);
            EnsureSchema(this.identityCoreStorageBroker);
        }

        internal IIdentityCoreStorageBroker IdentityCoreStorageBroker => this.identityCoreStorageBroker;

        private static IConfiguration BuildTestConfiguration(string connectionString) =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    // Resolved under the PRODUCTION key, but only from this in-memory layer —
                    // never from the ambient environment.
                    ["ConnectionStrings:Glory2HimSecurityConnection"] = connectionString
                })
                .Build();

        // The schema is built from the CURRENT model rather than migrated from WebApp's
        // SecurityDbContext, for the same reason AssociationQueryBroker gives: this fixture is
        // about whether IdentityCoreStorageBroker's narrow mapping — three tables, a handful of
        // columns — behaves against a real server, not about replaying the host's own Identity
        // migration history.
        private static void EnsureSchema(IdentityCoreStorageBroker storageBroker)
        {
            // drops a stale catalogue from a previous run that reused this process id
            storageBroker.Database.EnsureDeleted();
            storageBroker.Database.EnsureCreated();
        }

        /// <summary>
        /// Inserts a role row directly via ADO.NET — see the type summary for why this goes
        /// around the read-only broker rather than through it.
        /// </summary>
        public async ValueTask SeedRoleAsync(Guid roleId, string roleName)
        {
            await ExecuteAsync(
                "INSERT INTO AspNetRoles (Id, Name) VALUES (@Id, @Name)",
                ("@Id", roleId), ("@Name", roleName));
        }

        /// <summary>
        /// Inserts a user row directly via ADO.NET — see the type summary for why this goes
        /// around the read-only broker rather than through it.
        /// </summary>
        public async ValueTask SeedUserAsync(
            Guid userId,
            string userName,
            bool isDisabled = false,
            string name = "",
            string surname = "")
        {
            await ExecuteAsync(
                "INSERT INTO AspNetUsers (Id, UserName, IsDisabled, Name, Surname) " +
                "VALUES (@Id, @UserName, @IsDisabled, @Name, @Surname)",
                ("@Id", userId),
                ("@UserName", userName),
                ("@IsDisabled", isDisabled),
                ("@Name", name),
                ("@Surname", surname));
        }

        /// <summary>
        /// Inserts a user-role membership row directly via ADO.NET — see the type summary for
        /// why this goes around the read-only broker rather than through it.
        /// </summary>
        public async ValueTask SeedUserRoleAsync(Guid userId, Guid roleId)
        {
            await ExecuteAsync(
                "INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)",
                ("@UserId", userId), ("@RoleId", roleId));
        }

        private async ValueTask ExecuteAsync(string commandText, params (string Name, object Value)[] parameters)
        {
            using var connection = new SqlConnection(this.connectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand(commandText, connection);

            foreach ((string name, object value) in parameters)
            {
                command.Parameters.AddWithValue(name, value);
            }

            await command.ExecuteNonQueryAsync();
        }

        private static string WithCatalog(string template, string catalog) =>
            new SqlConnectionStringBuilder(template) { InitialCatalog = catalog }
                .ConnectionString;

        private static string ReadConnectionStringTemplate()
        {
            IConfiguration fileConfiguration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

            return fileConfiguration.GetConnectionString(TestConnectionStringKey)
                ?? throw new InvalidOperationException(
                    $"appsettings.json must define ConnectionStrings:{TestConnectionStringKey}.");
        }

        // Belt and braces. The database name is always built from TestDatabasePrefix above, but
        // a drop is irreversible and silent, so it is checked immediately before the call rather
        // than trusted from a distance.
        private void GuardAgainstDroppingANonTestDatabase()
        {
            bool isTestDatabase = this.databaseName.StartsWith(
                TestDatabasePrefix, StringComparison.Ordinal);

            if (isTestDatabase is false)
            {
                throw new InvalidOperationException(
                    $"Refusing to drop '{this.databaseName}': integration tests only ever create " +
                    $"and drop databases named '{TestDatabasePrefix}<process id>'.");
            }
        }

        private void DropTestDatabase(bool isBestEffort)
        {
            GuardAgainstDroppingANonTestDatabase();

            try
            {
                using var connection = new SqlConnection(this.masterConnectionString);
                connection.Open();

                using var command = new SqlCommand(
                    $"IF DB_ID(@database) IS NOT NULL BEGIN " +
                    $"ALTER DATABASE [{this.databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                    $"DROP DATABASE [{this.databaseName}]; END",
                    connection);

                command.Parameters.AddWithValue("@database", this.databaseName);
                command.ExecuteNonQuery();
            }
            catch (Exception exception) when (isBestEffort)
            {
                // Teardown only. An orphaned per-process catalogue is a nuisance, and the next
                // run with the same process id clears it, but throwing from teardown would mask
                // the run's real result. The startup drop passes isBestEffort: false and is
                // therefore NOT caught here.
                _ = exception;
            }
        }

        public void Dispose()
        {
            this.identityCoreStorageBroker.Dispose();
            DropTestDatabase(isBestEffort: true);
        }
    }

    [CollectionDefinition(IdentityCoreIntegrationCollection.Name)]
    public sealed class IdentityCoreIntegrationCollection
        : ICollectionFixture<IdentityCoreQueryBroker>
    {
        public const string Name = "Identity core integration";
    }
}
