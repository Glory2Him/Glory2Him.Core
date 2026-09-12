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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using FluentAssertions;

using Glory2Him.Core.Brokers.Events;
using Glory2Him.WebApp.Infrastructure;

namespace Glory2Him.WebApp.Tests.Unit.Infrastructure
{
    /// <summary>
    /// Builds the CORE slice of the host's container and asks one question of it: can the event
    /// broker be built when no event store is reachable?
    ///
    /// <para>The subject is the host's OBJECT GRAPH, not broker logic — which is why this drives
    /// everything through the container and never constructs <c>EventBroker</c> directly, and why
    /// it does not collide with the rule that brokers hold no logic and get no unit tests. It sits
    /// beside <c>EventSubscriptionResolutionTests</c> for the same reason: both are
    /// composition-root checks (#490).</para>
    ///
    /// <para>Everything here runs with NO database anywhere, on either operating system
    /// (§12.10 rule 11). A unit suite that created or migrated a catalogue as a side effect would
    /// be the defect this file exists to keep out.</para>
    /// </summary>
    public class EventBrokerResolutionTests
    {
        // An endpoint no process on the machine can reach: a LocalDB instance name nothing hosts.
        //
        // Deliberately NOT MSSQLLocalDB, which LocalDB creates on first use — pointing an
        // "unreachable" probe at it makes a unit test create and migrate a catalogue on the
        // developer's own machine, which is exactly what §12.10 rule 11 forbids.
        //
        // Deliberately NOT a TCP host either. "localhost,1433" is a live SQL Server service
        // container in the ubuntu build job, and an ".invalid" host can still resolve under a
        // wildcarding DNS resolver; in both cases the endpoint stops being unreachable, and where
        // it does not, the probe pays a connect timeout per resolution. A named instance nothing
        // hosts fails immediately, without opening a socket.
        //
        // Pooling is OFF deliberately, and it is not a performance choice. SqlClient's connection
        // pool caches a failed connection attempt for a blocking period of several seconds and
        // rethrows the VERY SAME exception object to every attempt inside it — so with pooling on,
        // a broker that genuinely created its client again is indistinguishable from one that
        // remembered the first failure, and the check below would be reading the pool's memory
        // instead of the broker's.
        private const string UnreachableConnectionString =
            "Server=(localdb)\\G2HNoSuchInstance;Database=EventBrokerResolutionProbe;Pooling=False;";

        [Fact]
        public void ShouldResolveTheEventBrokerWhenTheEventStoreIsUnreachable()
        {
            // given
            using ServiceProvider provider = BuildCoreServiceProvider(
                eventHighwayConnectionString: UnreachableConnectionString);

            IEventBroker eventBroker = null;

            // when
            Exception resolutionException = Record.Exception(() =>
                eventBroker = provider.GetRequiredService<IEventBroker>());

            // then
            resolutionException.Should().BeNull(
                because: "a broker constructor may read configuration and build options, but it " +
                    "may not open a connection or issue DDL — every service the subscriptions " +
                    "bind to takes IEventBroker, so a broker that needs a database to be built " +
                    "makes the whole Core graph unbuildable without one (§EVN24)");

            eventBroker.Should().NotBeNull();
        }

        [Fact]
        public void ShouldResolveTheEventBrokerWhenTheEventStoreConnectionStringIsMissing()
        {
            // given: a host configured with no event store at all — the key is absent, so the
            // broker reads the empty string the null-coalesce leaves behind
            using ServiceProvider provider = BuildCoreServiceProvider(
                eventHighwayConnectionString: null);

            IEventBroker eventBroker = null;

            // when
            Exception resolutionException = Record.Exception(() =>
                eventBroker = provider.GetRequiredService<IEventBroker>());

            // then
            resolutionException.Should().BeNull(
                because: "an unconfigured event store is the operation's failure, not the " +
                    "container's — the graph still builds, and the portal still serves " +
                    "everything that does not touch the substrate");

            eventBroker.Should().NotBeNull();
        }

        [Fact]
        public async Task ShouldAttemptToCreateTheEventHighwayClientAgainAfterAFailedFirstUseAsync()
        {
            // given: an event store no process can reach, so first use fails however often it is
            // attempted — what is under test is whether it is ATTEMPTED a second time
            using ServiceProvider provider = BuildCoreServiceProvider(
                eventHighwayConnectionString: UnreachableConnectionString);

            var eventBroker = provider.GetRequiredService<IEventBroker>();

            // when
            Exception firstFailure = await Record.ExceptionAsync(async () =>
                await eventBroker.RegisterEventParticipantAsync(CancellationToken.None));

            Exception secondFailure = await Record.ExceptionAsync(async () =>
                await eventBroker.RegisterEventParticipantAsync(CancellationToken.None));

            // then
            firstFailure.Should().NotBeNull(
                because: "the event store is unreachable, so the operation that first needs the " +
                    "client must surface that — this is the failure the caller is meant to see");

            secondFailure.Should().NotBeNull();

            // A DISTINCT exception instance is the only thing observable without a database that
            // separates "created the client again and failed again" from "replayed the first
            // failure". A memoising creation — Lazy<T> in its default mode caches the exception
            // and rethrows the very same object — would hand back the first instance twice.
            secondFailure.Should().NotBeSameAs(firstFailure,
                because: "Program.InitializeCoreAsync retries RegisterCoreEventSubstrateAsync " +
                    "five times at two-second intervals and §EVN24 rests the whole deferral on " +
                    "that retry; an implementation that remembered the first failure would make " +
                    "the four remaining attempts dead code and leave the substrate permanently " +
                    "dormant on a host that started before its event store did");
        }

        private static ServiceProvider BuildCoreServiceProvider(
            string eventHighwayConnectionString)
        {
            var settings = new Dictionary<string, string>
            {
                // Core's own store, named for symmetry and never reached either: StorageBroker
                // builds its options in OnConfiguring and connects to nothing.
                ["ConnectionStrings:Glory2HimConnectionString"] = UnreachableConnectionString,

                // NOT optional, and not decoration. EnvelopeIntegrityBroker refuses to construct
                // without a usable key (#392), and EventBroker takes that broker — so with this
                // section absent the provider throws for a reason that has nothing to do with the
                // event store, and the test would pass while proving nothing. A test-only secret.
                ["EventEnvelopeSigning:0:KeyId"] = "webapp-unit-test-key",

                ["EventEnvelopeSigning:0:Key"] =
                    "d2ViYXBwLXVuaXQtdGVzdC1vbmx5LWhtYWMtc2hhMjU2LXNpZ25pbmcta2V5",

                ["EventEnvelopeSigning:0:ActiveFrom"] = "2020-01-01T00:00:00+00:00",
            };

            // A null connection string means the key is ABSENT, not empty — the shape a host that
            // was never configured for an event store actually has.
            if (eventHighwayConnectionString is not null)
            {
                settings["ConnectionStrings:EventHighwayConnectionString"] =
                    eventHighwayConnectionString;
            }

            // Explicitly typed rather than var: the registration below must land under
            // IConfiguration, and the builder chain returns IConfigurationRoot — inferring it
            // would register the wrong service type and nothing could resolve IConfiguration.
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var services = new ServiceCollection();

            services.AddSingleton(configuration);
            services.AddHttpContextAccessor();
            services.AddLogging();
            services.AddCoreServices();

            return services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateScopes = true,
                });
        }
    }
}
