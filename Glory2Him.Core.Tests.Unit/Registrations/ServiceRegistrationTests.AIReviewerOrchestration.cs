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
using Glory2Him.Core.Services.Orchestrations.AIReviewers;
using Microsoft.Extensions.DependencyInjection;

namespace Glory2Him.Core.Tests.Unit.Registrations
{
    public partial class ServiceRegistrationTests
    {
        // Resolution, not registration — the same distinction the approval orchestration's own
        // test draws, and for a sharper reason here. A missing descriptor for a service an
        // ORCHESTRATION binds to fails at first request rather than at startup: MVC cannot
        // activate AIReviewersController and reports an InvalidOperationException at delivery
        // time, on a route nobody exercises until somebody opens a moderation panel.
        //
        // This half of the wiring is Core's. The host's half — the AddScoped line in
        // CoreRegistration — is caught by the acceptance suite, and neither test covers the
        // other's file.
        [Fact]
        public void ShouldResolveAIReviewerOrchestrationServiceFromTheDocumentedRegistrations()
        {
            // given: exactly what the XML doc on AddAIReviewerOrchestrationService asks for —
            // the Approval foundation (which also supplies the workflow seam this binds to) and
            // the AI reviewer assignment foundation
            IServiceCollection services = CreateServicesWithBrokerStubs();
            services.AddApprovalService();
            services.AddAIReviewerAssignmentService();

            // when
            IServiceCollection returnedServices = services.AddAIReviewerOrchestrationService();
            ServiceProvider provider = services.BuildServiceProvider();

            IAIReviewerOrchestrationService aiReviewerOrchestrationService =
                provider.GetRequiredService<IAIReviewerOrchestrationService>();

            // then
            returnedServices.Should().BeSameAs(services);

            aiReviewerOrchestrationService.Should().NotBeNull(
                because: "the documented registrations must be sufficient to build the graph, " +
                    "not merely to describe it");
        }
    }
}
