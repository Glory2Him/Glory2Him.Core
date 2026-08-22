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
using Glory2Him.Core.Services.Orchestrations.Approvals;
using Microsoft.Extensions.DependencyInjection;

namespace Glory2Him.Core.Tests.Unit.Registrations
{
    public partial class ServiceRegistrationTests
    {
        // Resolution, not registration. Every other test in this folder asserts that a descriptor
        // was added, which a service whose constructor demands something nobody registered still
        // satisfies — the failure lands on the first caller to BUILD the graph instead.
        //
        // This one bit: ApprovalOrchestrationService took a new internal seam and Core's own
        // AddApprovalReviewService did not register it, so following the documented contract
        // above produced "Unable to resolve service for type IApprovalReviewWorkflowService"
        // with the whole suite green. A host outside Core's friend set could not have fixed it.
        [Fact]
        public void ShouldResolveApprovalOrchestrationServiceFromTheDocumentedRegistrations()
        {
            // given: exactly what the XML doc on AddApprovalOrchestrationService asks for —
            // the three approval foundation services and the brokers, nothing else
            IServiceCollection services = CreateServicesWithBrokerStubs();
            services.AddApprovalService();
            services.AddApprovalReviewService();
            services.AddApprovalCommentService();

            // when
            IServiceCollection returnedServices = services.AddApprovalOrchestrationService();
            ServiceProvider provider = services.BuildServiceProvider();

            IApprovalOrchestrationService approvalOrchestrationService =
                provider.GetRequiredService<IApprovalOrchestrationService>();

            // then
            returnedServices.Should().BeSameAs(services);

            approvalOrchestrationService.Should().NotBeNull(
                because: "the documented registrations must be sufficient to build the graph, " +
                    "not merely to describe it");
        }
    }
}
