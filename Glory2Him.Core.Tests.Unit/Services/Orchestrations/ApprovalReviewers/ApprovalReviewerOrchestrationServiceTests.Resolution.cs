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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.ApprovalReviewers
{
    /// <summary>
    /// How every operation here finds the round behind the entity it was handed (§12.5.4 business
    /// rule 2, §16.7.2, §9.7.2 rule 1).
    /// </summary>
    public partial class ApprovalReviewerOrchestrationServiceTests
    {
        /// <summary>
        /// ONE broker read, keyed on the ENTITY. The old shape resolved the approval through
        /// <c>IApprovalWorkflowService.FindApprovalByEntityAsync</c> and then gathered the scope
        /// by id, which read the same row twice; the by-entity overload does both at once.
        ///
        /// <para>An economy rather than a new capability, and the assertions say so both ways:
        /// the by-entity form is asked, and neither the foundation probe nor the by-id gather is
        /// asked at all. Without the second half this case passes on a service that still takes
        /// two reads and simply happens to answer.</para>
        /// </summary>
        [Fact]
        public async Task ShouldResolveTheRoundThroughTheEntityKeyedScopeReadAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewers);
            EntityType entityType = EntityType.ContentItem;
            Guid entityId = Guid.NewGuid();
            Guid approvalId = Guid.NewGuid();

            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveApprovalReviewerScopeByEntityAsync(
                    entityType,
                    entityId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new ApprovalReviewerScope
                        {
                            ApprovalId = approvalId,
                            ApprovalStatus = ApprovalStatus.Submitted,
                            EntityCreatedBy = "the-entity-owner",
                            RoleSubjects = Array.Empty<RoleSubject>(),
                            ActiveReviewerUserIds = Array.Empty<string>(),
                            RecordedReviewerUserIds = Array.Empty<string>(),
                            ActiveRequests = Array.Empty<ActiveReviewRequest>(),
                        });

            this.approvalReviewRequestServiceMock.Setup(service =>
                service.RetrieveApprovalReviewRequestsByApprovalIdAsync(
                    approvalId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<ApprovalReviewRequest>());

            // when
            IReadOnlyList<ApprovalReviewRequest> approvalReviewRequests =
                await this.approvalReviewerOrchestrationService
                    .RetrieveApprovalReviewRequestsAsync(
                        entityType,
                        entityId,
                        TestContext.Current.CancellationToken);

            // then
            approvalReviewRequests.Should().BeEmpty();

            this.accessBrokerMock.Verify(broker =>
                broker.RetrieveApprovalReviewerScopeByEntityAsync(
                    entityType, entityId, It.IsAny<CancellationToken>()),
                Times.Once);

            // and the two reads it replaced are not made at all
            this.approvalServiceMock.Verify(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.accessBrokerMock.Verify(broker =>
                broker.RetrieveApprovalReviewerScopeByIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
