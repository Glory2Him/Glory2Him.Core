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
using Glory2Him.Core.Services.Orchestrations.ApprovalReviewers;
using Microsoft.Extensions.DependencyInjection;

namespace Glory2Him.Core.Tests.Unit.Registrations
{
    public partial class ServiceRegistrationTests
    {
        // Resolution, not registration — the same distinction the approval round's own test draws
        // and for the same reason. A missing descriptor for something an ORCHESTRATION binds to
        // fails at first request rather than at startup: MVC cannot activate ApprovalsController
        // and reports an InvalidOperationException at delivery time, on routes nobody exercises
        // until somebody opens a moderation panel.
        //
        // This half of the wiring is Core's. The host's half — the AddScoped line in
        // CoreRegistration — is caught by the acceptance suite, and neither test covers the
        // other's file.
        [Fact]
        public void ShouldResolveApprovalReviewerOrchestrationServiceFromTheDocumentedRegistrations()
        {
            // given: exactly what the XML doc on AddApprovalReviewerOrchestrationService asks
            // for — the invitation rows, the comment thread, the identity-store window, and the
            // Approval foundation (which also supplies the workflow seam the repair binds to)
            IServiceCollection services = CreateServicesWithBrokerStubs();
            services.AddApprovalReviewRequestService();
            services.AddApprovalCommentService();
            services.AddIdentityUserService();
            services.AddApprovalService();

            // when
            IServiceCollection returnedServices =
                services.AddApprovalReviewerOrchestrationService();

            ServiceProvider provider = services.BuildServiceProvider();

            IApprovalReviewerOrchestrationService approvalReviewerOrchestrationService =
                provider.GetRequiredService<IApprovalReviewerOrchestrationService>();

            // then
            returnedServices.Should().BeSameAs(services);

            approvalReviewerOrchestrationService.Should().NotBeNull(
                because: "the documented registrations must be sufficient to build the graph, " +
                    "not merely to describe it");
        }
    }
}
