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
using Glory2Him.Core.Registrations;
using Glory2Him.Core.Services.Foundations.ContentItemSettings;
using Microsoft.Extensions.DependencyInjection;

namespace Glory2Him.Core.Tests.Unit.Registrations
{
    public partial class ServiceRegistrationTests
    {
        [Fact]
        public void ShouldRegisterContentItemSettingServiceAsSingleton()
        {
            // given
            IServiceCollection services = CreateServicesWithBrokerStubs();

            // when
            IServiceCollection returnedServices = services.AddContentItemSettingService();
            ServiceProvider provider = services.BuildServiceProvider();
            IContentItemSettingService firstContentItemSettingService = provider.GetRequiredService<IContentItemSettingService>();
            IContentItemSettingService secondContentItemSettingService = provider.GetRequiredService<IContentItemSettingService>();

            // then
            returnedServices.Should().BeSameAs(services);
            firstContentItemSettingService.Should().BeOfType<ContentItemSettingService>();
            secondContentItemSettingService.Should().BeSameAs(firstContentItemSettingService);
        }
    }
}
