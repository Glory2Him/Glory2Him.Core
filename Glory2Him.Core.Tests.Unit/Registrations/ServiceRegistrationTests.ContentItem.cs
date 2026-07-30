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

using FluentAssertions;
using Glory2Him.Core.Brokers.DateTimes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Identifiers;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Registrations;
using Glory2Him.Core.Services.Foundations.ContentItems;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Registrations
{
    public partial class ServiceRegistrationTests
    {
        [Fact]
        public void ShouldRegisterContentItemServiceAsSingleton()
        {
            // given
            IServiceCollection services = CreateServicesWithBrokerStubs();

            // when
            IServiceCollection returnedServices = services.AddContentItemService();
            ServiceProvider provider = services.BuildServiceProvider();
            IContentItemService firstContentItemService = provider.GetRequiredService<IContentItemService>();
            IContentItemService secondContentItemService = provider.GetRequiredService<IContentItemService>();

            // then
            returnedServices.Should().BeSameAs(services);
            firstContentItemService.Should().BeOfType<ContentItemService>();
            secondContentItemService.Should().BeSameAs(firstContentItemService);
        }
    }
}
