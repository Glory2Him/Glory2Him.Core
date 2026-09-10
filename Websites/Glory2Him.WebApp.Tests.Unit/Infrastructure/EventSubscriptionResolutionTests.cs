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
using FluentAssertions;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Registrations;
using Glory2Him.WebApp.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Glory2Him.WebApp.Tests.Unit.Infrastructure
{
    /// <summary>
    /// Resolves every service the event subscriptions bind to out of the REAL production
    /// container, on the real delivery path.
    ///
    /// <para>This is additive to <c>CoreRegistrationTests.ShouldResolveEveryCoreServiceItRegisters</c>
    /// and replaces nothing. That test walks what <c>AddCoreServices</c> registers — including
    /// the three aliased workflow interfaces no subscription ever binds to. This one walks the
    /// other direction: what the subscriptions DEPEND on, which is the set that fails
    /// mid-delivery rather than at startup when a registration goes missing.</para>
    ///
    /// <para>The gap it closes is in
    /// <c>Glory2Him.Core.Tests.Integration/Registrations/EventSubscriptionWiringTests</c>, which
    /// runs the real registration class against a MOCKED <c>IServiceProvider</c>. That proves the
    /// address-to-handler map is internally consistent; it cannot prove the application's own
    /// container can build the services those handlers resolve. The fixture's hand-maintained
    /// <c>Provide&lt;...&gt;</c> list is free to drift from <c>CoreRegistration</c> without
    /// failing anything, and the drift surfaces at runtime as a resolution failure on the first
    /// publish — after the publisher has already committed its write.</para>
    ///
    /// <para>Three hand-maintained copies of the bound set exist:
    /// <c>EventSubscriptionRegistrationTests</c>' provider stubs, the integration fixture's
    /// <c>Provide&lt;...&gt;</c> list, and <c>CoreRegistration</c> itself. This test ties only
    /// the production one to the bound set — it derives the bound set from the registration
    /// class rather than restating it, so it cannot drift, but the fixture's list still can for
    /// the command-address services no <c>IsSuccess</c>-asserting publish reaches.</para>
    /// </summary>
    public class EventSubscriptionResolutionTests
    {
        // The count is deliberately exact. Without it this test passes just as well when the
        // registration binds nothing at all — every assertion below is over a set that would
        // then be empty. Adding a subscription means bumping this number, which is the point:
        // it forces the new binding past the resolution check rather than around it.
        private const int ExpectedBoundHandlerCount = 119;

        [Fact]
        public async Task ShouldResolveEveryServiceTheEventSubscriptionsBindToAsync()
        {
            // given: the production container, built as Program.cs builds it. The substrate is
            // registered against app.Services, so this collection is the one every delivery
            // resolves through.
            using ServiceProvider provider = BuildProductionServiceProvider();
            var resolutions = new List<BoundServiceResolution>();

            var recordingScopeFactory =
                new RecordingServiceScopeFactory(provider, resolutions);

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
                object delivery = boundHandler.DynamicInvoke(null, CancellationToken.None);
                ObserveAbortedDelivery(delivery);
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
                because: "a subscription bound to a service the host never registered throws " +
                    "mid-delivery, after the publisher has committed — nothing else in the " +
                    "suite looks at the composition root from the subscriptions' side");
        }

        private static ServiceProvider BuildProductionServiceProvider()
        {
            IServiceCollection services = new ServiceCollection();

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    // Never connected to — EF builds the options eagerly but opens nothing, and
                    // this test only asks whether the graph can be CONSTRUCTED.
                    ["ConnectionStrings:Glory2HimConnectionString"] =
                        "Server=(localdb)\\MSSQLLocalDB;Database=SubscriptionResolutionProbe;",
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
        // because the handler is an async lambda. Reading the exception marks it observed.
        private static void ObserveAbortedDelivery(object delivery)
        {
            var asTask = delivery?.GetType().GetMethod("AsTask", Type.EmptyTypes);

            if (asTask is not null)
            {
                _ = ((Task)asTask.Invoke(delivery, Array.Empty<object>())).Exception;
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
                    this.rootProvider.CreateScope(),
                    this.resolutions);
        }

        private sealed class RecordingServiceScope : IServiceScope, IAsyncDisposable
        {
            private readonly IServiceScope innerScope;

            public RecordingServiceScope(
                IServiceScope innerScope,
                List<BoundServiceResolution> resolutions)
            {
                this.innerScope = innerScope;

                this.ServiceProvider =
                    new RecordingServiceProvider(innerScope.ServiceProvider, resolutions);
            }

            public IServiceProvider ServiceProvider { get; }

            public void Dispose() =>
                this.innerScope.Dispose();

            public ValueTask DisposeAsync()
            {
                this.innerScope.Dispose();

                return ValueTask.CompletedTask;
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

            // Only CONTAINER failures count, the same line CoreRegistrationTests draws: a
            // constructor that then fails to reach SQL is an environment problem in a unit test
            // and says nothing about the registrations. A missing registration does not throw at
            // all — GetService hands back null — so the one defect this test exists to find is
            // caught outside that filter either way.
            private static readonly string[] containerFailureMarkers =
            [
                "Unable to resolve service for type",
                "A circular dependency was detected",
                "Cannot consume scoped service",
            ];

            public object GetService(Type serviceType)
            {
                string failure = null;

                try
                {
                    if (this.scopedProvider.GetService(serviceType) is null)
                    {
                        failure = "no registration in the production service collection";
                    }
                }
                // An aliased registration resolves one interface and CASTS to the
                // implementation, so a bad alias throws InvalidCastException rather than
                // anything the markers would match. Caught by type, as CoreRegistrationTests
                // catches it, or a broken alias slips through.
                catch (InvalidCastException invalidCastException)
                {
                    failure = invalidCastException.Message;
                }
                catch (Exception exception)
                {
                    failure = containerFailureMarkers.Any(marker =>
                        exception.Message.Contains(marker, StringComparison.Ordinal))
                            ? exception.Message
                            : null;
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
