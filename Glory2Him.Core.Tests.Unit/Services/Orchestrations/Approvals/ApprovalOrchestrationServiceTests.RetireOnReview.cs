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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Securities;
using Moq;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests.Exceptions;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    /// <summary>
    /// 7.9 rule 6 - a review being recorded is how an invitation gets answered, so the request
    /// the reviewer was asked through stops being outstanding.
    /// </summary>
    public partial class ApprovalOrchestrationServiceTests
    {
        private EventEnvelope<ApprovalReview> CreateReviewAddedEnvelope(
            Guid approvalId,
            string createdBy) =>
            new EventEnvelope<ApprovalReview>
            {
                Content = new ApprovalReview
                {
                    Id = Guid.NewGuid(),
                    ApprovalId = approvalId,
                    CreatedBy = createdBy,
                },
                SecurityContext = this.ambientSecurityContext,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

        [Fact]
        public async Task ShouldRetireTheInvitationWhenItsTargetRecordsAReviewAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();

            this.approvalServiceMock.Setup(service =>
                service.RetrieveApprovalByIdAsync(approvalId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(CreateSubstrateApproval(
                        approvalId: approvalId,
                        entityId: Guid.NewGuid(),
                        entityType: EntityType.ContentItem));
            Guid invitedId = Guid.NewGuid();
            Guid requestId = Guid.NewGuid();

            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveApprovalReviewerScopeByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new ApprovalReviewerScope
                        {
                            ApprovalId = approvalId,
                            ApprovalStatus = ApprovalStatus.Submitted,
                            EntityCreatedBy = "somebody-else",
                            RoleSubjects = Array.Empty<G2H.Security.Client.Models.Foundations.Access.RoleSubject>(),
                            ActiveReviewerUserIds = Array.Empty<string>(),
                            RecordedReviewerUserIds = Array.Empty<string>(),

                            ActiveRequests = new[]
                            {
                                new ActiveReviewRequest
                                {
                                    Id = requestId,
                                    RequestedUserId = invitedId.ToString(),
                                }
                            },
                        });

            // when
            await this.approvalOrchestrationService.OnApprovalReviewAddedAsync(
                CreateReviewAddedEnvelope(approvalId, invitedId.ToString()),
                TestContext.Current.CancellationToken);

            // then: retired through the WORKFLOW seam, which mints the system identity itself -
            // DeletedBy must say "answered", not name whoever triggered the delivery
            this.approvalReviewRequestWorkflowServiceMock.Verify(service =>
                service.RetireAnsweredApprovalReviewRequestAsync(
                    requestId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// The common case: most reviews are recorded by people who were never formally asked, so
        /// there is nothing to retire and the flow stays silent.
        /// </summary>
        [Fact]
        public async Task ShouldNotRetireAnythingWhenTheReviewerWasNeverInvitedAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();

            this.approvalServiceMock.Setup(service =>
                service.RetrieveApprovalByIdAsync(approvalId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(CreateSubstrateApproval(
                        approvalId: approvalId,
                        entityId: Guid.NewGuid(),
                        entityType: EntityType.ContentItem));

            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveApprovalReviewerScopeByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new ApprovalReviewerScope
                        {
                            ApprovalId = approvalId,
                            ApprovalStatus = ApprovalStatus.Submitted,
                            EntityCreatedBy = "somebody-else",
                            RoleSubjects = Array.Empty<G2H.Security.Client.Models.Foundations.Access.RoleSubject>(),
                            ActiveReviewerUserIds = Array.Empty<string>(),
                            RecordedReviewerUserIds = Array.Empty<string>(),
                            ActiveRequests = Array.Empty<ActiveReviewRequest>(),
                        });

            // when
            await this.approvalOrchestrationService.OnApprovalReviewAddedAsync(
                CreateReviewAddedEnvelope(approvalId, Guid.NewGuid().ToString()),
                TestContext.Current.CancellationToken);

            // then
            this.approvalReviewRequestWorkflowServiceMock.Verify(service =>
                service.RetireAnsweredApprovalReviewRequestAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// The hook runs AFTER the signature check. Retiring on the strength of an envelope whose
        /// signature has not been verified would let anyone reaching the address clear the panel.
        /// </summary>
        [Fact]
        public async Task ShouldNotRetireAnythingWhenTheEnvelopeFailsVerificationAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();

            this.approvalServiceMock.Setup(service =>
                service.RetrieveApprovalByIdAsync(approvalId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(CreateSubstrateApproval(
                        approvalId: approvalId,
                        entityId: Guid.NewGuid(),
                        entityType: EntityType.ContentItem));

            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<It.IsAnyType>>(),
                    It.IsAny<string>(),
                    It.IsAny<EnvelopeDirection>()))
                        .ReturnsAsync(false);

            // when
            await Assert.ThrowsAnyAsync<Exception>(() =>
                this.approvalOrchestrationService.OnApprovalReviewAddedAsync(
                    CreateReviewAddedEnvelope(approvalId, Guid.NewGuid().ToString()),
                    TestContext.Current.CancellationToken).AsTask());

            // then
            this.approvalReviewRequestWorkflowServiceMock.Verify(service =>
                service.RetireAnsweredApprovalReviewRequestAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// Retiring the invitation is bookkeeping; re-testing the round is the workflow. A
        /// failure in the first must not cancel the second, or a vote that carried a round over
        /// the line is counted by nothing and the item sits blocked with its conditions provably
        /// met. Nothing re-drives it afterwards, so the round would stay stuck until somebody
        /// edited the content to produce a fresh fact.
        /// </summary>
        [Fact]
        public async Task ShouldStillReTestTheRoundWhenRetiringTheInvitationFailsAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            Guid invitedId = Guid.NewGuid();
            Guid requestId = Guid.NewGuid();

            this.approvalServiceMock.Setup(service =>
                service.RetrieveApprovalByIdAsync(approvalId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(CreateSubstrateApproval(
                        approvalId: approvalId,
                        entityId: Guid.NewGuid(),
                        entityType: EntityType.ContentItem));

            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveApprovalReviewerScopeByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new ApprovalReviewerScope
                        {
                            ApprovalId = approvalId,
                            ApprovalStatus = ApprovalStatus.Submitted,
                            EntityCreatedBy = "somebody-else",
                            RoleSubjects = Array.Empty<G2H.Security.Client.Models.Foundations.Access.RoleSubject>(),
                            ActiveReviewerUserIds = Array.Empty<string>(),
                            RecordedReviewerUserIds = Array.Empty<string>(),

                            ActiveRequests = new[]
                            {
                                new ActiveReviewRequest
                                {
                                    Id = requestId,
                                    RequestedUserId = invitedId.ToString(),
                                }
                            },
                        });

            var retirementException =
                new ApprovalReviewRequestDependencyException(
                    message: "storage was unavailable",
                    innerException: new Xeption());

            this.approvalReviewRequestWorkflowServiceMock.Setup(service =>
                service.RetireAnsweredApprovalReviewRequestAsync(
                    requestId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(retirementException);

            // when
            await this.approvalOrchestrationService.OnApprovalReviewAddedAsync(
                CreateReviewAddedEnvelope(approvalId, invitedId.ToString()),
                TestContext.Current.CancellationToken);

            // then: the round was still read, which is the first thing the re-test does. Without
            // the isolation this assertion fails because the exception escapes the hook and
            // takes ProcessApprovalInputsChangedAsync with it.
            this.approvalServiceMock.Verify(service =>
                service.RetrieveApprovalByIdAsync(approvalId, It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);

            // and: the failure is not silent. It is the only trace left of a retirement that did
            // not happen, since the delivery reports success once the hook is contained.
            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(retirementException),
                Times.Once);
        }

        /// <summary>
        /// The hook's FIRST statement is an access-broker read, and that broker catches nothing,
        /// so a storage outage arrives as a raw exception rather than one of the
        /// ApprovalReviewRequest types. An earlier narrow filter let exactly this case walk past
        /// and cancel the re-test - the one failure the isolation exists to prevent.
        /// </summary>
        [Fact]
        public async Task ShouldStillReTestTheRoundWhenTheRetirementLookupFaultsAsync()
        {
            // given
            Guid approvalId = Guid.NewGuid();
            Guid invitedId = Guid.NewGuid();

            this.approvalServiceMock.Setup(service =>
                service.RetrieveApprovalByIdAsync(approvalId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(CreateSubstrateApproval(
                        approvalId: approvalId,
                        entityId: Guid.NewGuid(),
                        entityType: EntityType.ContentItem));

            // a storage fault, shaped like what the broker really lets through
            var storageFailure = new InvalidOperationException("the database was unavailable");

            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveApprovalReviewerScopeByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(storageFailure);

            // when
            await this.approvalOrchestrationService.OnApprovalReviewAddedAsync(
                CreateReviewAddedEnvelope(approvalId, invitedId.ToString()),
                TestContext.Current.CancellationToken);

            // then: the round was still read, which is the first thing the re-test does
            this.approvalServiceMock.Verify(service =>
                service.RetrieveApprovalByIdAsync(approvalId, It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(storageFailure),
                Times.Once);
        }

    }
}
