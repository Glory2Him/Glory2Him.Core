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
using Glory2Him.Core.Services.Foundations.ApprovalReviewRequests;
using Microsoft.Extensions.DependencyInjection;

namespace Glory2Him.Core.Tests.Unit.Registrations
{
    public partial class ServiceRegistrationTests
    {
        [Fact]
        public void ShouldRegisterApprovalReviewRequestServiceAsSingleton()
        {
            // given
            IServiceCollection services = CreateServicesWithBrokerStubs();

            // when
            IServiceCollection returnedServices = services.AddApprovalReviewRequestService();
            ServiceProvider provider = services.BuildServiceProvider();

            IApprovalReviewRequestService firstApprovalReviewRequestService =
                provider.GetRequiredService<IApprovalReviewRequestService>();

            IApprovalReviewRequestService secondApprovalReviewRequestService =
                provider.GetRequiredService<IApprovalReviewRequestService>();

            // then
            returnedServices.Should().BeSameAs(services);
            firstApprovalReviewRequestService.Should().BeOfType<ApprovalReviewRequestService>();
            secondApprovalReviewRequestService.Should().BeSameAs(firstApprovalReviewRequestService);
        }
    }
}
