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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Registrations;
using Glory2Him.Core.Services.Foundations.ApprovalComments;
using Glory2Him.Core.Services.Foundations.ApprovalReviews;
using Glory2Him.Core.Services.Foundations.Approvals;
using Glory2Him.Core.Services.Foundations.ApprovalSettings;
using Glory2Him.Core.Services.Foundations.Associations;
using Glory2Him.Core.Services.Foundations.BibleReferences;
using Glory2Him.Core.Services.Foundations.Comments;
using Glory2Him.Core.Services.Foundations.ContentItems;
using Glory2Him.Core.Services.Foundations.ContentItemSettings;
using Glory2Him.Core.Services.Foundations.Links;
using Glory2Him.Core.Services.Foundations.Reactions;
using Glory2Him.Core.Services.Foundations.Tags;
using Glory2Him.Core.Services.Orchestrations.Approvals;
using Glory2Him.Core.Services.Processings.ContentItems;
using Glory2Him.Core.Services.Processings.Links;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Glory2Him.Core.Tests.Integration.Brokers
{
    /// <summary>
    /// Stands up the REAL <see cref="EventBroker"/> against a real EventHighway store on
    /// LocalDB, and runs the REAL <see cref="EventSubscriptionRegistration"/> against it.
    ///
    /// <para>Every one of the fifteen SERVICES is mocked, and that is deliberate rather than a
    /// compromise. The subject here is the WIRING — the address a fact is published to, and the
    /// subscription that is bound to it — not what a handler does once it is reached. Mocking
    /// the services leaves all 102 real address-map lookups and all 102 real listener
    /// registrations executing exactly as they do in a host.</para>
    ///
    /// <para>This is the only thing in the suite that can see the substrate at all. The unit
    /// tests mock <c>IEventBroker</c>, which is precisely the boundary three separate wiring
    /// defects hid behind during PR #266 while 3,900 tests passed over them: an operation
    /// declared on an enum but absent from its address map (a raw indexer lookup, so
    /// <c>RegisterAsync</c> would throw and abort every subscription after it), a command
    /// published to the processing address while the subscription still bound the foundation
    /// handler, and handlers declared on a concrete class but not its interface. A mocked
    /// broker cannot tell a bound handler from an unbound one.</para>
    /// </summary>
    public sealed class EventSubstrateBroker : IDisposable
    {
        // Every database this fixture is willing to create or drop starts with this. The guard
        // below refuses to touch anything else, because a drop here is unrecoverable.
        private const string TestDatabasePrefix = "Glory2HimEventHighwayIntegration_";

        // A TEST-ONLY key, for the same reason AssociationQueryBroker uses one: the production
        // key is EventHighwayConnectionString, and any host or CI job that configures Core
        // through the environment sets exactly that variable. A fixture that resolved it would
        // drop the substrate store it points at. This key cannot collide with it.
        private const string TestConnectionStringKey = "EventHighwayIntegrationConnectionString";

        private readonly string databaseName;

        public EventSubstrateBroker()
        {
            this.databaseName = TestDatabasePrefix + Environment.ProcessId;
            IConfiguration configuration = BuildTestConfiguration(this.databaseName);

            // Dropped up front rather than only on the way out: a previous run that reused this
            // process id would otherwise leave its listeners registered, and registration is
            // idempotent by stable id, so the stale rows would silently survive.
            DropTestDatabase(this.databaseName);

            var envelopeIntegrityBroker = new EnvelopeIntegrityBroker(configuration);
            EventBroker = new EventBroker(configuration, envelopeIntegrityBroker);
            ApprovalOrchestrationServiceMock = new Mock<IApprovalOrchestrationService>();

            Registration = new EventSubscriptionRegistration(
                eventBroker: EventBroker,
                contentItemService: new Mock<IContentItemService>().Object,
                approvalService: new Mock<IApprovalService>().Object,
                bibleReferenceService: new Mock<IBibleReferenceService>().Object,
                tagService: new Mock<ITagService>().Object,
                linkService: new Mock<ILinkService>().Object,
                reactionService: new Mock<IReactionService>().Object,
                commentService: new Mock<ICommentService>().Object,
                approvalCommentService: new Mock<IApprovalCommentService>().Object,
                approvalReviewService: new Mock<IApprovalReviewService>().Object,
                approvalSettingService: new Mock<IApprovalSettingService>().Object,
                associationService: new Mock<IAssociationService>().Object,
                contentItemSettingService: new Mock<IContentItemSettingService>().Object,
                contentItemProcessingService: new Mock<IContentItemProcessingService>().Object,
                linkProcessingService: new Mock<ILinkProcessingService>().Object,
                approvalOrchestrationService: ApprovalOrchestrationServiceMock.Object);

            // Captured rather than thrown, so the failure lands on the test that asserts it
            // with a readable message instead of on a collection-fixture constructor.
            try
            {
                Registration.RegisterAsync(CancellationToken.None)
                    .AsTask().GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                RegistrationException = exception;
            }
        }

        internal EventBroker EventBroker { get; }

        internal IEventSubscriptionRegistration Registration { get; }

        internal Mock<IApprovalOrchestrationService> ApprovalOrchestrationServiceMock { get; }

        /// <summary>
        /// The exception <c>RegisterAsync</c> threw while this fixture was being built, or
        /// <c>null</c> when every subscription registered. Asserted by the registration test.
        /// </summary>
        internal Exception RegistrationException { get; }

        /// <summary>
        /// The subscription ids a publish actually reached. This is the whole point of running
        /// against a real broker: <c>EventPublishResult.Deliveries</c> carries one entry per
        /// listener that received the event, so which subscriptions a fact address reaches is
        /// an observation rather than a reading of the registration source.
        /// </summary>
        internal static IReadOnlyList<Guid> SubscriptionsReached<T>(
            EventPublishResult<T> publishResult) =>
                (publishResult.Deliveries ?? new List<EventDelivery<T>>())
                    .Select(delivery => delivery.SubscriptionId)
                    .ToList();

        private static IConfiguration BuildTestConfiguration(string databaseName)
        {
            IConfiguration fileConfiguration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

            string template =
                fileConfiguration.GetConnectionString(TestConnectionStringKey)
                    ?? throw new InvalidOperationException(
                        $"appsettings.json must define ConnectionStrings:{TestConnectionStringKey}.");

            // One store per process, for the reason AssociationQueryBroker gives: a fixed name
            // means a CLI run alongside the IDE runner drops the database out from under the
            // other. EventHighway creates the schema itself on first use, so a name that does
            // not exist yet is not a problem — it is the normal case.
            var connectionStringBuilder = new SqlConnectionStringBuilder(template)
            {
                InitialCatalog = databaseName
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    // Resolved under the PRODUCTION key, but only from this in-memory layer —
                    // never from the ambient environment.
                    ["ConnectionStrings:EventHighwayConnectionString"] =
                        connectionStringBuilder.ConnectionString,

                    // EnvelopeIntegrityBroker refuses a blank key or id at construction, and
                    // EventBroker signs on every publish, so the substrate cannot come up
                    // without one. A test-only secret: it proves nothing about production
                    // keying, it only lets the signature round-trip.
                    ["EventEnvelopeSigning:0:KeyId"] = "integration-test-key",

                    ["EventEnvelopeSigning:0:Key"] =
                        "aW50ZWdyYXRpb24tdGVzdC1vbmx5LWhtYWMtc2hhMjU2LXNpZ25pbmcta2V5",

                    ["EventEnvelopeSigning:0:ActiveFrom"] = "2020-01-01T00:00:00+00:00"
                })
                .Build();
        }

        // Belt and braces. BuildTestConfiguration already makes it impossible to resolve
        // anything but a per-process test catalogue, but a drop is irreversible and silent, so
        // the name is checked immediately before the call rather than trusted from a distance.
        private static void GuardAgainstDroppingANonTestDatabase(string databaseName)
        {
            bool isTestDatabase = databaseName.StartsWith(
                TestDatabasePrefix, StringComparison.Ordinal);

            if (isTestDatabase is false)
            {
                throw new InvalidOperationException(
                    $"Refusing to drop '{databaseName}': integration tests only ever create and " +
                    $"drop databases named '{TestDatabasePrefix}<process id>'.");
            }
        }

        private static void DropTestDatabase(string databaseName)
        {
            GuardAgainstDroppingANonTestDatabase(databaseName);

            try
            {
                using var connection = new SqlConnection(
                    "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True");

                connection.Open();

                // EventHighway owns this schema, so there is no DbContext here to call
                // EnsureDeleted on — the drop is issued directly. SINGLE_USER kills the live
                // connections EventHighway's own pool may still hold.
                using var command = new SqlCommand(
                    $"IF DB_ID(@database) IS NOT NULL BEGIN " +
                    $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                    $"DROP DATABASE [{databaseName}]; END",
                    connection);

                command.Parameters.AddWithValue("@database", databaseName);
                command.ExecuteNonQuery();
            }
            catch
            {
                // best effort — an orphaned per-process catalogue is a nuisance, and the next
                // run with the same process id clears it, but throwing from teardown would
                // mask the run's real result
            }
        }

        public void Dispose() =>
            DropTestDatabase(this.databaseName);
    }

    [CollectionDefinition(EventSubstrateCollection.Name)]
    public sealed class EventSubstrateCollection
        : ICollectionFixture<EventSubstrateBroker>
    {
        public const string Name = "Event substrate integration";
    }
}
