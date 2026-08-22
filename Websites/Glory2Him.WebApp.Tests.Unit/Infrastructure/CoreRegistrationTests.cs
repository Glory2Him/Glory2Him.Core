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
using FluentAssertions;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Services.Foundations.ApprovalComments;
using Glory2Him.Core.Services.Foundations.ApprovalReviews;
using Glory2Him.Core.Services.Foundations.Tags;
using Glory2Him.WebApp.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Glory2Him.WebApp.Tests.Unit.Infrastructure
{
    public class CoreRegistrationTests
    {
        // Every one of these captures the caller's identity or a DbContext, so a longer lifetime
        // would serve one request's user or connection to the next. EventEnvelopeBroker is the
        // subtle one: it builds an EventEnvelopeClient in its constructor, that client resolves
        // its service graph once, and the SecurityBroker underneath reads HttpContext.User in ITS
        // constructor — so a singleton stamps every envelope in the process, and therefore every
        // audit field, with whichever principal happened to be current when it was first built.
        [Theory]
        [InlineData(typeof(IEventEnvelopeBroker))]
        [InlineData(typeof(ISecurityAuditBroker))]
        [InlineData(typeof(IAccessBroker))]
        [InlineData(typeof(IStorageBroker))]
        [InlineData(typeof(ITagService))]
        [InlineData(typeof(IApprovalCommentService))]
        [InlineData(typeof(IApprovalReviewService))]
        public void ShouldRegisterRequestBoundServicesAsScoped(Type serviceType)
        {
            // given
            IServiceCollection services = new ServiceCollection();

            // when
            services.AddCoreServices();

            ServiceDescriptor descriptor =
                services.Single(service => service.ServiceType == serviceType);

            // then
            descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
        }

        [Fact]
        public void ShouldRegisterTagServiceOnce()
        {
            // given
            IServiceCollection services = new ServiceCollection();

            // when
            IServiceCollection returnedServices = services.AddCoreServices();

            // then
            returnedServices.Should().BeSameAs(services);

            services.Count(service => service.ServiceType == typeof(ITagService))
                .Should().Be(1);
        }

        // BUILDS the graph rather than inspecting descriptors. Every other test in this file
        // asserts that something was registered, which a service whose constructor demands
        // something nobody registered still satisfies — the failure lands at the first
        // ACTIVATION instead, and since the substrate went live that is a handler mid-delivery
        // rather than a startup crash.
        //
        // The substrate binds handlers through IServiceScopeFactory and resolves them per
        // delivery, so an unresolvable service is not a boot failure that CI would catch. It is
        // an exception thrown while handling an event, after the publisher has already
        // committed its write.
        [Fact]
        public void ShouldResolveEveryCoreServiceItRegisters()
        {
            // given: the host's own ambient registrations, which AddCoreServices assumes rather
            // than provides — IConfiguration comes from the WebApplicationBuilder in Program.cs
            IServiceCollection services = new ServiceCollection();

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    // Never connected to — EF builds the options eagerly but opens nothing, and
                    // this test only asks whether the graph can be CONSTRUCTED.
                    ["ConnectionStrings:Glory2HimConnectionString"] =
                        "Server=(localdb)\\MSSQLLocalDB;Database=RegistrationResolutionProbe;",
                })
                .Build();

            services.AddSingleton(configuration);
            services.AddHttpContextAccessor();
            services.AddLogging();
            services.AddCoreServices();

            Type[] coreServiceTypes = services
                .Select(service => service.ServiceType)
                .Where(serviceType => serviceType.IsInterface)
                .Where(serviceType =>
                    serviceType.Namespace?.StartsWith("Glory2Him.Core") == true)
                .Distinct()
                .ToArray();

            using ServiceProvider provider = services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateScopes = true,
                });

            using IServiceScope scope = provider.CreateScope();

            // when: only CONTAINER failures count. A service whose constructor then fails to
            // reach SQL is an environment problem in a unit test and says nothing about the
            // registrations — but a dependency nobody registered, a captive scoped service, or
            // a cycle are all defects that ship.
            string[] containerFailureMarkers =
            [
                "Unable to resolve service for type",
                "A circular dependency was detected",
                "Cannot consume scoped service",
            ];

            var unresolvable = coreServiceTypes
                .Select(serviceType =>
                {
                    try
                    {
                        scope.ServiceProvider.GetRequiredService(serviceType);

                        return null;
                    }
                    catch (Exception exception)
                    {
                        return containerFailureMarkers.Any(marker =>
                            exception.Message.Contains(marker, StringComparison.Ordinal))
                                ? $"{serviceType.Name}: {exception.Message}"
                                : null;
                    }
                })
                .Where(failure => failure is not null)
                .ToArray();

            // then
            unresolvable.Should().BeEquivalentTo(Array.Empty<string>(),
                because: "a registered service that cannot be built is a defect the substrate " +
                    "surfaces mid-delivery, not at startup");
        }
    }
}
