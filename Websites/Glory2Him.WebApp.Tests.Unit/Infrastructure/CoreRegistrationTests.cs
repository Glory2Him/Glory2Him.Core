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
using System.Linq;
using FluentAssertions;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Services.Foundations.ApprovalComments;
using Glory2Him.Core.Services.Foundations.Tags;
using Glory2Him.WebApp.Infrastructure;
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
    }
}
