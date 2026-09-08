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
using Glory2Him.Core.Services.Foundations.AIReviewerAssignments;
using Microsoft.Extensions.DependencyInjection;

namespace Glory2Him.Core.Tests.Unit.Registrations
{
    public partial class ServiceRegistrationTests
    {
        [Fact]
        public void ShouldRegisterAIReviewerAssignmentServiceAsSingleton()
        {
            // given
            IServiceCollection services = CreateServicesWithBrokerStubs();

            // when
            IServiceCollection returnedServices = services.AddAIReviewerAssignmentService();
            ServiceProvider provider = services.BuildServiceProvider();

            IAIReviewerAssignmentService firstAIReviewerAssignmentService =
                provider.GetRequiredService<IAIReviewerAssignmentService>();

            IAIReviewerAssignmentService secondAIReviewerAssignmentService =
                provider.GetRequiredService<IAIReviewerAssignmentService>();

            // then
            returnedServices.Should().BeSameAs(services);
            firstAIReviewerAssignmentService.Should().BeOfType<AIReviewerAssignmentService>();
            secondAIReviewerAssignmentService.Should().BeSameAs(firstAIReviewerAssignmentService);
        }

        /// <summary>
        /// ONE OBJECT BEHIND TWO DOORS. The workflow's return-to-pending seam (§8.8 rule 1, §8.6
        /// HR-4) is the same implementation as the public service, and the second registration
        /// resolves THROUGH the first so the container never makes a second of them.
        ///
        /// <para><b>What it catches.</b> Registering the implementation type against both service
        /// types — the obvious spelling, and the wrong one: the container keys on the SERVICE
        /// type, so that produces two singletons. Nothing in the graph would fail to build, and
        /// the two would simply be different objects with independent state.</para>
        /// </summary>
        [Fact]
        public void ShouldRegisterTheAIReviewerAssignmentWorkflowSeamAsTheSameSingleton()
        {
            // given
            IServiceCollection services = CreateServicesWithBrokerStubs();

            // when
            services.AddAIReviewerAssignmentService();
            ServiceProvider provider = services.BuildServiceProvider();

            IAIReviewerAssignmentService publicAIReviewerAssignmentService =
                provider.GetRequiredService<IAIReviewerAssignmentService>();

            IAIReviewerAssignmentWorkflowService aiReviewerAssignmentWorkflowService =
                provider.GetRequiredService<IAIReviewerAssignmentWorkflowService>();

            // then
            aiReviewerAssignmentWorkflowService.Should().BeSameAs(publicAIReviewerAssignmentService);
        }
    }
}
