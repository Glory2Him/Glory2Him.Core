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
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Approvals;
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
    /// <para>Fourteen of the fifteen services are mocked, and that is deliberate rather than a
    /// compromise: for the wiring question — which address a fact goes to, and which
    /// subscription is bound to it — what a handler DOES once reached is irrelevant, and mocking
    /// leaves all 102 real address-map lookups and listener registrations executing exactly as
    /// they do in a host.</para>
    ///
    /// <para>The fifteenth, <c>ApprovalOrchestrationService</c>, is REAL. It has to be, because
    /// a receiver re-verifies the envelope's signature against the event name it expects, and a
    /// mocked receiver never runs that check — so whether a delivered fact is ACCEPTED cannot be
    /// asked at all. Its own dependencies are mocked, but set up to let the approval flow run
    /// rather than fall over.</para>
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
        private readonly string masterConnectionString;

        public EventSubstrateBroker()
        {
            this.databaseName = TestDatabasePrefix + Environment.ProcessId;
            string template = ReadConnectionStringTemplate();
            IConfiguration configuration = BuildTestConfiguration(template, this.databaseName);

            // The drop MUST reach the same server the store is created on. Both are derived
            // from the one template for that reason — an independently written master
            // connection string would silently diverge the moment the template named another
            // server, creating the catalogue in one place and trying to drop it in another.
            this.masterConnectionString = WithCatalog(template, "master");

            // Dropped up front rather than only on the way out: a previous run that reused this
            // process id would otherwise leave its listeners registered, and RetrieveOrRegister
            // RETRIEVES rather than updates, so a stale row bound to a different handler would
            // survive and the routing tests would pass against wiring the source no longer has.
            //
            // This one is allowed to THROW, unlike the teardown drop. The teardown swallows so a
            // cleanup failure cannot mask the run's real result; here the drop is the premise the
            // whole suite rests on, and a silent failure buys a green run against a stale store —
            // exactly the false pass this fixture exists to make impossible.
            DropTestDatabase(
                this.masterConnectionString, this.databaseName, isBestEffort: false);

            var envelopeIntegrityBroker = new EnvelopeIntegrityBroker(configuration);
            EventBroker = new EventBroker(configuration, envelopeIntegrityBroker);

            // The orchestration is REAL, and it is the only service here that is. It has to be:
            // a receiver re-verifies the envelope's signature against the event name it expects
            // (ValidateEntityFactEnvelopeAsync), and a mocked receiver never runs that check. The
            // publisher-signs-X / receiver-verifies-Y defect class is invisible without it.
            //
            // Its own dependencies are mocked, but set up to let the flow RUN rather than fall
            // over — otherwise every delivery fails and "was this fact accepted?" cannot be
            // asked. The integrity broker is the same instance the publisher signs with, which
            // is the point: same key, same algorithm, so only the NAME can differ.
            ApprovalOrchestrationService = new ApprovalOrchestrationService(
                approvalService: BuildApprovalServiceMock().Object,
                approvalReviewService: new Mock<IApprovalReviewService>().Object,
                approvalCommentService: new Mock<IApprovalCommentService>().Object,
                accessBroker: BuildAccessBrokerMock().Object,
                eventEnvelopeBroker: new Mock<IEventEnvelopeBroker>().Object,
                eventBroker: EventBroker,
                envelopeIntegrityBroker: envelopeIntegrityBroker,
                loggingBroker: new Mock<ILoggingBroker>().Object);

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
                approvalOrchestrationService: ApprovalOrchestrationService);

            // Captured rather than thrown, so the failure lands on the test that asserts it
            // with a readable message instead of on a collection-fixture constructor.
            RegistrationException = TryRegister();

            // Registered a SECOND time, here rather than from a test. Idempotency is a property
            // of the fixture's construction, and proving it from a test would mean one test
            // mutating the substrate every other test in the collection shares, at a point in
            // the order xUnit does not guarantee.
            //
            // Doing it up front is also strictly stronger: every delivery assertion then runs
            // against a DOUBLY registered substrate, and each one pins an exact delivery count.
            // So if a second registration ever did duplicate a listener, the delivery tests
            // would catch it rather than silently measure a doubled substrate.
            SecondRegistrationException = TryRegister();
        }

        private Exception TryRegister()
        {
            try
            {
                Registration.RegisterAsync(CancellationToken.None)
                    .AsTask().GetAwaiter().GetResult();

                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        internal EventBroker EventBroker { get; }

        internal IApprovalOrchestrationService ApprovalOrchestrationService { get; }

        internal IEventSubscriptionRegistration Registration { get; }

        // Enough for ResolveApprovalAsync to reach a decision: no approval exists for the entity,
        // so one is created at Draft and handed straight back.
        private static Mock<IApprovalService> BuildApprovalServiceMock()
        {
            var approvalServiceMock = new Mock<IApprovalService>();

            approvalServiceMock
                .Setup(service => service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ApprovalEntityMatch)null);

            approvalServiceMock
                .Setup(service => service.AddApprovalAsync(
                    It.IsAny<Approval>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Approval approval, CancellationToken _) =>
                    new Approval
                    {
                        Id = Guid.NewGuid(),
                        EntityType = approval.EntityType,
                        EntityId = approval.EntityId,
                        ApprovalStatus = approval.ApprovalStatus
                    });

            return approvalServiceMock;
        }

        // A verdict that resolves and settles nothing: conditions unmet, no stale-review reset,
        // no auto-approve. The flow reads it, finds nothing to do, and returns — which is all
        // that is needed to establish the fact was ACCEPTED rather than refused at the seam.
        private static Mock<IAccessBroker> BuildAccessBrokerMock()
        {
            var accessBrokerMock = new Mock<IAccessBroker>();

            accessBrokerMock
                .Setup(broker => broker.EvaluateApprovalConditionsByIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ApprovalConditionsVerdict
                {
                    AreConditionsMet = false,
                    ShouldResetStaleReviewsOnChange = false,
                    ShouldAutoApprove = false,
                    BlockReason = AccessDenialReason.None,
                    BlockReasons = new List<AccessDenialReason>(),
                    UnresolvedApprovalCommentCount = 0,
                    ApprovalCount = 0,
                    RequiredNumberOfApprovals = 1,
                    Explanation = "Integration fixture: nothing to decide."
                });

            return accessBrokerMock;
        }

        /// <summary>
        /// The exception <c>RegisterAsync</c> threw while this fixture was being built, or
        /// <c>null</c> when every subscription registered. Asserted by the registration test.
        /// </summary>
        internal Exception RegistrationException { get; }

        /// <summary>
        /// The exception the SECOND <c>RegisterAsync</c> threw, or <c>null</c>. Registration is
        /// documented as idempotent and safe to call once at startup; a restart must not
        /// duplicate a participant, address or listener.
        /// </summary>
        internal Exception SecondRegistrationException { get; }

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

        /// <summary>
        /// Every subscription id the approval workflow owns — all fifteen it binds today.
        /// </summary>
        /// <remarks>
        /// Tests assert against this whole set rather than against the one id they expect,
        /// because §12.4.1's routing has TWO failure modes and issue #270 names both: "makes the
        /// workflow either never fire or <b>fire twice per edit</b>".
        ///
        /// <para>A <c>Contain</c> check sees neither of the doubling shapes, and an exact count
        /// on the ONE expected id sees only the first of them:</para>
        /// <list type="bullet">
        /// <item>the same listener row duplicated — the expected id appears twice; and</item>
        /// <item>a second subscription, with its OWN id, bound to the same address — the
        /// expected id still appears exactly once, so counting it proves nothing, yet the
        /// workflow is reached twice for one edit.</item>
        /// </list>
        ///
        /// <para>Intersecting the delivery set with every workflow id catches both: whatever the
        /// duplicate's id, it is in here.</para>
        /// </remarks>
        internal static readonly IReadOnlyList<Guid> WorkflowSubscriptionIds = new[]
        {
            EventBrokerIdentifiers.ApprovalOrchestrationOnApprovalReviewAddedSubscriptionId,
            EventBrokerIdentifiers.ApprovalOrchestrationOnTagAddedSubscriptionId,
            EventBrokerIdentifiers.ApprovalOrchestrationOnTagModifiedSubscriptionId,
            EventBrokerIdentifiers.ApprovalOrchestrationOnContentItemAddedSubscriptionId,
            EventBrokerIdentifiers.ApprovalOrchestrationOnContentItemModifiedSubscriptionId,
            EventBrokerIdentifiers.ApprovalOrchestrationOnLinkAddedSubscriptionId,
            EventBrokerIdentifiers.ApprovalOrchestrationOnLinkModifiedSubscriptionId,
            EventBrokerIdentifiers.ApprovalOrchestrationOnCommentAddedSubscriptionId,
            EventBrokerIdentifiers.ApprovalOrchestrationOnCommentModifiedSubscriptionId,
            EventBrokerIdentifiers.ApprovalOrchestrationOnReactionAddedSubscriptionId,
            EventBrokerIdentifiers.ApprovalOrchestrationOnReactionModifiedSubscriptionId,
            EventBrokerIdentifiers.ApprovalOrchestrationOnBibleReferenceAddedSubscriptionId,
            EventBrokerIdentifiers.ApprovalOrchestrationOnBibleReferenceModifiedSubscriptionId,
            EventBrokerIdentifiers.ApprovalOrchestrationOnAssociationAddedSubscriptionId,
            EventBrokerIdentifiers.ApprovalOrchestrationOnAssociationModifiedSubscriptionId
        };

        /// <summary>
        /// Every delivery this publish made to a subscription the approval workflow owns, in
        /// order and WITHOUT de-duplication — so a repeated id survives to be asserted on.
        /// </summary>
        internal static IReadOnlyList<Guid> WorkflowSubscriptionsReached(
            IReadOnlyList<Guid> subscriptionsReached) =>
                subscriptionsReached
                    .Where(reached => WorkflowSubscriptionIds.Contains(reached))
                    .ToList();

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

        private static string WithCatalog(string template, string catalog) =>
            new SqlConnectionStringBuilder(template) { InitialCatalog = catalog }
                .ConnectionString;

        private static IConfiguration BuildTestConfiguration(
            string template,
            string databaseName)
        {
            // One store per process, for the reason AssociationQueryBroker gives: a fixed name
            // means a CLI run alongside the IDE runner drops the database out from under the
            // other. EventHighway creates the schema itself on first use, so a name that does
            // not exist yet is not a problem — it is the normal case.
            string connectionString = WithCatalog(template, databaseName);

            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    // Resolved under the PRODUCTION key, but only from this in-memory layer —
                    // never from the ambient environment.
                    ["ConnectionStrings:EventHighwayConnectionString"] = connectionString,

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

        private static void DropTestDatabase(
            string masterConnectionString,
            string databaseName,
            bool isBestEffort)
        {
            GuardAgainstDroppingANonTestDatabase(databaseName);

            try
            {
                using var connection = new SqlConnection(masterConnectionString);
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
            catch (Exception exception) when (isBestEffort)
            {
                // Teardown only. An orphaned per-process catalogue is a nuisance, and the next
                // run with the same process id clears it, but throwing from teardown would mask
                // the run's real result. The startup drop passes isBestEffort: false and is
                // therefore NOT caught here — see the constructor for why that one must not be
                // swallowed.
                _ = exception;
            }
        }

        public void Dispose() =>
            DropTestDatabase(
                this.masterConnectionString, this.databaseName, isBestEffort: true);
    }

    [CollectionDefinition(EventSubstrateCollection.Name)]
    public sealed class EventSubstrateCollection
        : ICollectionFixture<EventSubstrateBroker>
    {
        public const string Name = "Event substrate integration";
    }
}
