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
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Brokers.DateTimes;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Services.Foundations.Associations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Glory2Him.Core.Tests.Integration.Brokers
{
    /// <summary>
    /// Stands up a real <see cref="StorageBroker"/> against LocalDB and wires it into a real
    /// <see cref="AssociationService"/>, with only the non-storage brokers faked.
    ///
    /// <para>The point is the collection read filter. Its unit tests run over
    /// <c>.AsQueryable()</c> on an in-memory array — LINQ to Objects — which executes the
    /// predicate as delegates and translates nothing. They prove the logic and say nothing
    /// about whether EF can turn it into SQL. This fixture is the only thing that answers
    /// that, because the filter closes over two <c>HashSet</c>s and dereferences a nullable
    /// enum inside the expression tree, over columns mapped with
    /// <c>HasConversion&lt;string&gt;()</c>.</para>
    /// </summary>
    public sealed class AssociationQueryBroker : IDisposable
    {
        private readonly StorageBroker storageBroker;

        public AssociationQueryBroker()
        {
            this.storageBroker = new StorageBroker(BuildTestConfiguration());
            EnsureSchema(this.storageBroker);

            DateTimeBrokerMock = new Mock<IDateTimeBroker>();
            SecurityAuditBrokerMock = new Mock<ISecurityAuditBroker>();
            EventEnvelopeBrokerMock = new Mock<IEventEnvelopeBroker>();

            AssociationService = new AssociationService(
                storageBroker: this.storageBroker,
                dateTimeBroker: DateTimeBrokerMock.Object,
                identifierBroker: new Mock<IIdentifierBroker>().Object,
                eventBroker: new Mock<IEventBroker>().Object,
                eventEnvelopeBroker: EventEnvelopeBrokerMock.Object,
                securityAuditBroker: SecurityAuditBrokerMock.Object,
                loggingBroker: new Mock<ILoggingBroker>().Object);
        }

        // Every database this fixture is willing to create or drop starts with this. The
        // guard below refuses to touch anything else, because a drop here is unrecoverable.
        private const string TestDatabasePrefix = "Glory2HimCoreIntegration_";

        // The connection string comes from a TEST-ONLY key.
        //
        // It would be natural to read `Glory2HimConnectionString` — that is what StorageBroker
        // itself reads — and to layer environment variables over the JSON so the server is
        // overridable. That combination is a database-destroying trap: `Glory2HimConnectionString`
        // is the production key, so any host, container or CI job that configures Glory2Him.Core
        // through the environment sets exactly the variable this fixture would then resolve, and
        // `EnsureDeleted` would drop whatever it points at. The test key cannot collide with it.
        private const string TestConnectionStringKey = "Glory2HimIntegrationConnectionString";

        private static IConfiguration BuildTestConfiguration()
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
                InitialCatalog = TestDatabasePrefix + Environment.ProcessId
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

        // The schema is built from the CURRENT model rather than by running migrations.
        //
        // That is deliberate on two counts. First, these tests are about whether EF can
        // translate a predicate against the mapping as it stands, so the model is the right
        // source of truth — running migrations would test the migration history instead.
        // Second, `Database.Migrate()` cannot run here at all: `ApprovalSetting` carries a
        // property with no migration behind it, so EF raises PendingModelChangesWarning and
        // refuses. That drift is pre-existing, unrelated to associations, and has its own
        // follow-up; this is not the place to paper over it silently, hence the note.
        //
        // This runs once because xUnit builds a collection fixture once — no static flag and
        // no ProcessExit hook. An earlier version used both; the hook did not fire under the
        // test host and left one orphaned database per run behind.
        private static void EnsureSchema(StorageBroker storageBroker)
        {
            GuardAgainstDroppingANonTestDatabase(storageBroker);

            // drops a stale catalogue from a previous run that reused this process id
            storageBroker.Database.EnsureDeleted();
            storageBroker.Database.EnsureCreated();
        }

        // Belt and braces. BuildTestConfiguration already makes it impossible to resolve
        // anything but a per-process test catalogue, but a drop is irreversible and silent, so
        // the name is checked immediately before the call rather than trusted from a distance.
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

        private void DropTestDatabase()
        {
            try
            {
                GuardAgainstDroppingANonTestDatabase(this.storageBroker);
                this.storageBroker.Database.EnsureDeleted();
            }
            catch
            {
                // best effort on the way out — an orphaned per-process catalogue is a
                // nuisance, and the next run with the same process id clears it, but
                // throwing from teardown would mask the run's real result
            }
        }

        internal IAssociationService AssociationService { get; }

        internal Mock<IDateTimeBroker> DateTimeBrokerMock { get; }

        internal Mock<ISecurityAuditBroker> SecurityAuditBrokerMock { get; }

        internal Mock<IEventEnvelopeBroker> EventEnvelopeBrokerMock { get; }

        /// <summary>
        /// Makes the caller the service sees. The collection read reaches the security
        /// context through the envelope the envelope broker mints, so that is what carries
        /// the roles.
        /// </summary>
        public void ActAs(string actorUserId, params string[] roles)
        {
            var securityContext = new SecurityContext
            {
                IsAuthenticated = true,
                Roles = roles
            };

            EventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.IsAny<Association>()))
                    .ReturnsAsync((Association content) =>
                        new EventEnvelope<Association>
                        {
                            Content = content,
                            SecurityContext = securityContext,
                            Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                        });

            SecurityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(actorUserId);

            DateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(DateTimeOffset.UtcNow);
        }

        public async ValueTask InsertAsync(params Association[] associations)
        {
            foreach (Association association in associations)
            {
                await this.storageBroker.InsertAssociationAsync(association, CancellationToken.None);
            }
        }

        /// <summary>
        /// Attempts an insert and returns the exception the database raised, or <c>null</c>
        /// when the row was accepted.
        ///
        /// <para>Detaching on failure is not tidiness. A rejected <c>SaveChanges</c> leaves the
        /// entity tracked in the <c>Added</c> state, and this fixture shares one context across
        /// the whole collection — the next save would retry the rejected row and fail a test
        /// that has nothing to do with it.</para>
        /// </summary>
        public async ValueTask<Exception> TryInsertAsync(Association association)
        {
            try
            {
                await this.storageBroker.InsertAssociationAsync(
                    association, CancellationToken.None);

                return null;
            }
            catch (Exception exception)
            {
                this.storageBroker.Entry(association).State = EntityState.Detached;

                return exception;
            }
        }

        /// <summary>
        /// Removes every row this fixture inserted. Each test seeds and clears its own rows so
        /// the database can be reused without cross-test interference.
        /// </summary>
        public async ValueTask ClearAsync(IEnumerable<Association> associations)
        {
            foreach (Association association in associations)
            {
                Association stored =
                    await this.storageBroker.SelectAssociationByIdAsync(
                        association.Id, CancellationToken.None);

                if (stored is not null)
                {
                    await this.storageBroker.DeleteAssociationAsync(stored, CancellationToken.None);
                }
            }
        }

        // xUnit disposes a collection fixture once, after the last test in the collection —
        // deterministic teardown, unlike the ProcessExit hook this replaced.
        public void Dispose()
        {
            DropTestDatabase();
            this.storageBroker.Dispose();
        }
    }

    /// <summary>
    /// Binds <see cref="AssociationQueryBroker"/> to a collection so xUnit builds it once,
    /// shares it across every test in the collection, and disposes it once at the end.
    ///
    /// <para>This also serialises those tests. They all query every row in the table, so
    /// running them concurrently would let one test's seeded rows appear in another's
    /// results.</para>
    /// </summary>
    [CollectionDefinition(AssociationIntegrationCollection.Name)]
    public sealed class AssociationIntegrationCollection
        : ICollectionFixture<AssociationQueryBroker>
    {
        public const string Name = "Association integration";
    }
}
