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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using FluentAssertions;
using Moq;

using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Registrations;
using Glory2Him.WebApp.Infrastructure;

namespace Glory2Him.WebApp.Tests.Unit.Infrastructure
{
    /// <summary>
    /// Resolves every service the event subscriptions bind to out of the REAL container the host
    /// builds, on the real delivery path.
    ///
    /// <para>This is additive to <c>CoreRegistrationTests.ShouldResolveEveryCoreServiceItRegisters</c>
    /// and replaces nothing. That test walks what <c>AddCoreServices</c> registers — including the
    /// three aliased workflow interfaces no subscription ever binds to. This one walks the other
    /// direction: what the subscriptions DEPEND on, which is the set that fails mid-delivery
    /// rather than at startup when a registration goes missing.</para>
    ///
    /// <para>The gap it closes is in
    /// <c>Glory2Him.Core.Tests.Integration/Registrations/EventSubscriptionWiringTests</c>, which
    /// runs the real registration class against a MOCKED <c>IServiceProvider</c>. That proves the
    /// address-to-handler map is internally consistent; it cannot prove the application's own
    /// container can build the services those handlers resolve. The fixture's hand-maintained
    /// <c>Provide&lt;...&gt;</c> list is free to drift from <c>CoreRegistration</c> without failing
    /// anything, and the drift surfaces at runtime as a resolution failure on the first publish —
    /// after the publisher has already committed its write.</para>
    ///
    /// <para>SCOPE: this builds the CORE slice of the host's container — <c>AddCoreServices</c>
    /// over the ambient registrations it assumes. <c>Program.cs</c> also calls
    /// <c>AddPortalIdentity</c>, <c>AddPortalBrokers</c> and <c>AddPortalViewServices</c> before
    /// it; those are deliberately not built here, because no Core service takes a portal type and
    /// standing them up would drag identity storage into a unit test. A subscription that grew a
    /// portal dependency would therefore be reported here as unresolvable rather than passing
    /// quietly — the failure direction that is safe to be wrong in.</para>
    ///
    /// <para>Three hand-maintained copies of the bound set exist:
    /// <c>EventSubscriptionRegistrationTests</c>' provider stubs, the integration fixture's
    /// <c>Provide&lt;...&gt;</c> list, and <c>CoreRegistration</c> itself. This test ties only the
    /// production one to the bound set — it derives the bound set from the registration class
    /// rather than restating it, so it cannot drift, but the fixture's list still can for the
    /// command-address services no <c>IsSuccess</c>-asserting publish reaches.</para>
    /// </summary>
    public class EventSubscriptionResolutionTests
    {
        // The count is deliberately exact. Without it this test passes just as well when the
        // registration binds nothing at all — every assertion below is over a set that would
        // then be empty. Adding a subscription means bumping this number, which is the point:
        // it forces the new binding past the resolution check rather than around it.
        private const int ExpectedBoundHandlerCount = 121;

        [Fact]
        public async Task ShouldResolveEveryServiceTheEventSubscriptionsBindToAsync()
        {
            // given: the container the host builds, so this is the graph every delivery really
            // resolves through
            using ServiceProvider provider = BuildCoreServiceProvider();
            var resolutions = new List<BoundServiceResolution>();

            var recordingScopeFactory = new RecordingServiceScopeFactory(
                rootProvider: provider,
                resolutions: resolutions);

            var eventBrokerMock = new Mock<IEventBroker>();

            var registration = new EventSubscriptionRegistration(
                eventBroker: eventBrokerMock.Object,
                serviceScopeFactory: recordingScopeFactory);

            await registration.RegisterAsync();

            // The handlers are taken off the broker's own invocations rather than listed here,
            // so this set is whatever the registration class actually bound — it cannot drift
            // from the production code the way a hand-written list would.
            Delegate[] boundHandlers = eventBrokerMock.Invocations
                .SelectMany(invocation => invocation.Arguments)
                .OfType<Delegate>()
                .ToArray();

            // when: each handler is driven exactly as a delivery drives it — a scope is opened
            // and the bound service is resolved out of it. The recorder resolves the type
            // against the real container, records the outcome, and then hands back null so the
            // GetRequiredService inside aborts the delivery before the handler body runs.
            foreach (Delegate boundHandler in boundHandlers)
            {
                await ObserveAbortedDeliveryAsync(
                    delivery: boundHandler.DynamicInvoke(null, CancellationToken.None));
            }

            string[] unresolvable = resolutions
                .Where(resolution => resolution.Failure is not null)
                .Select(resolution => $"{resolution.ServiceType.Name}: {resolution.Failure}")
                .Distinct()
                .ToArray();

            // then
            boundHandlers.Should().HaveCount(ExpectedBoundHandlerCount,
                because: "a subscription set that shrank silently would make every assertion " +
                    "below vacuous");

            resolutions.Should().HaveCount(ExpectedBoundHandlerCount,
                because: "every bound handler opens a scope and resolves its service, so one " +
                    "recorded resolution per handler is the only shape in which this test " +
                    "actually exercised the container");

            unresolvable.Should().BeEquivalentTo(Array.Empty<string>(),
                because: "a subscription bound to a service the host cannot build throws " +
                    "mid-delivery, after the publisher has committed — nothing else in the " +
                    "suite looks at the composition root from the subscriptions' side");
        }

        private static ServiceProvider BuildCoreServiceProvider()
        {
            var services = new ServiceCollection();

            // Explicitly typed rather than var: the registration below must land under
            // IConfiguration, and the builder chain returns IConfigurationRoot — inferring it
            // would register the wrong service type and nothing could resolve IConfiguration.
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    // Never connected to, and since §EVN24 that is actually true: EF builds its
                    // options in OnConfiguring and opens nothing, and EventBroker creates its
                    // EventHighway client on FIRST USE rather than in its constructor. This test
                    // only asks whether the graph can be CONSTRUCTED, so nothing here connects.
                    //
                    // It used to. The client's constructor calls Database.Migrate(), so before
                    // §EVN24 this probe CREATED and migrated a SubscriptionResolutionProbe
                    // catalogue on whatever server it named — a unit test issuing DDL, and the
                    // only reason it passed anywhere (§12.10 rule 11).
                    //
                    // Hence a LocalDB instance name nothing hosts, deliberately not MSSQLLocalDB,
                    // which LocalDB auto-creates. Not a TCP host either: localhost,1433 is a live
                    // SQL Server service container in the ubuntu build job, and an .invalid host
                    // can resolve under a wildcarding DNS resolver — a named instance nothing
                    // hosts is the shape that fails immediately on both operating systems without
                    // opening a socket, which matters at 17 resolutions.
                    ["ConnectionStrings:Glory2HimConnectionString"] =
                        "Server=(localdb)\\G2HNoSuchInstance;Database=SubscriptionResolutionProbe;",

                    ["ConnectionStrings:EventHighwayConnectionString"] =
                        "Server=(localdb)\\G2HNoSuchInstance;Database=SubscriptionResolutionProbe;",

                    // NOT optional, and not decoration. EnvelopeIntegrityBroker refuses to
                    // construct without a usable key (#392), and EVERY service the subscriptions
                    // bind to takes that broker — so with this section absent the real provider
                    // throws before it can build a single one of them. The probe would then
                    // record 121 resolutions that never resolved anything, and pass. A test-only
                    // secret: it proves nothing about production keying, it only lets the graph
                    // come up.
                    ["EventEnvelopeSigning:0:KeyId"] = "webapp-unit-test-key",

                    ["EventEnvelopeSigning:0:Key"] =
                        "d2ViYXBwLXVuaXQtdGVzdC1vbmx5LWhtYWMtc2hhMjU2LXNpZ25pbmcta2V5",

                    ["EventEnvelopeSigning:0:ActiveFrom"] = "2020-01-01T00:00:00+00:00",
                })
                .Build();

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

        // The abort lands in the returned ValueTask rather than on the DynamicInvoke call,
        // because the handler is an async lambda. Awaiting it observes the exception and lets
        // the scope's async disposal finish before the next handler runs.
        private static async Task ObserveAbortedDeliveryAsync(object delivery)
        {
            MethodInfo asTask = delivery?.GetType().GetMethod("AsTask", Type.EmptyTypes);

            if (asTask is null)
            {
                return;
            }

            try
            {
                await (Task)asTask.Invoke(delivery, Array.Empty<object>());
            }
            catch (Exception)
            {
                // The deliberate abort. What the resolution DID is already recorded; what the
                // aborted delivery then threw says nothing.
            }
        }

        private sealed class BoundServiceResolution
        {
            public Type ServiceType { get; init; }
            public string Failure { get; init; }
        }

        private sealed class RecordingServiceScopeFactory : IServiceScopeFactory
        {
            private readonly IServiceProvider rootProvider;
            private readonly List<BoundServiceResolution> resolutions;

            public RecordingServiceScopeFactory(
                IServiceProvider rootProvider,
                List<BoundServiceResolution> resolutions)
            {
                this.rootProvider = rootProvider;
                this.resolutions = resolutions;
            }

            public IServiceScope CreateScope() =>
                new RecordingServiceScope(
                    innerScope: this.rootProvider.CreateScope(),
                    resolutions: this.resolutions);
        }

        private sealed class RecordingServiceScope : IServiceScope, IAsyncDisposable
        {
            private readonly IServiceScope innerScope;

            public RecordingServiceScope(
                IServiceScope innerScope,
                List<BoundServiceResolution> resolutions)
            {
                this.innerScope = innerScope;

                this.ServiceProvider = new RecordingServiceProvider(
                    scopedProvider: innerScope.ServiceProvider,
                    resolutions: resolutions);
            }

            public IServiceProvider ServiceProvider { get; }

            public void Dispose() =>
                this.innerScope.Dispose();

            // Scoped uses `await using`, so the real delivery path disposes each scope
            // ASYNCHRONOUSLY. The container's own scope implements IAsyncDisposable; going
            // through Dispose instead would skip async cleanup on any async-disposable scoped
            // service, 121 times over.
            public async ValueTask DisposeAsync()
            {
                if (this.innerScope is IAsyncDisposable asyncDisposableScope)
                {
                    await asyncDisposableScope.DisposeAsync();

                    return;
                }

                this.innerScope.Dispose();
            }
        }

        // Deliberately does NOT implement ISupportRequiredService: GetRequiredService then goes
        // through GetService and throws on the null below, which is what aborts the delivery.
        private sealed class RecordingServiceProvider : IServiceProvider
        {
            private readonly IServiceProvider scopedProvider;
            private readonly List<BoundServiceResolution> resolutions;

            public RecordingServiceProvider(
                IServiceProvider scopedProvider,
                List<BoundServiceResolution> resolutions)
            {
                this.scopedProvider = scopedProvider;
                this.resolutions = resolutions;
            }

            public object GetService(Type serviceType)
            {
                // EVERY failure counts, with no marker-string filter. A curated list of
                // container-failure phrases silently forgives anything it does not recognise —
                // a constructor guard, a bad alias's InvalidCastException, a missing
                // configuration section — and "the graph did not come up" is the whole question
                // this test asks. A service that cannot be built is a defect however it says so.
                string failure = null;

                try
                {
                    if (this.scopedProvider.GetService(serviceType) is null)
                    {
                        failure = "no registration in the host's service collection";
                    }
                }
                catch (Exception exception)
                {
                    failure = $"{exception.GetType().Name}: {exception.Message}";
                }

                this.resolutions.Add(new BoundServiceResolution
                {
                    ServiceType = serviceType,
                    Failure = failure,
                });

                // Abort the delivery: the handler body must not run, only its resolution.
                return null;
            }
        }
    }
}
