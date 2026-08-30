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
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Brokers.Integrities;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.ApprovalComments;
using Glory2Him.Core.Services.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Services.Foundations.ApprovalReviews;
using Glory2Him.Core.Services.Foundations.IdentityUsers;
using Glory2Him.Core.Services.Foundations.Approvals;
using Glory2Him.Core.Services.Orchestrations.Approvals;
using Moq;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    public partial class ApprovalOrchestrationServiceTests
    {
        private readonly Mock<IApprovalWorkflowService> approvalServiceMock;
        private readonly Mock<IApprovalReviewWorkflowService> approvalReviewServiceMock;
        private readonly Mock<IApprovalCommentService> approvalCommentServiceMock;
        private readonly Mock<IApprovalReviewRequestService> approvalReviewRequestServiceMock;
        private readonly Mock<IApprovalReviewRequestWorkflowService> approvalReviewRequestWorkflowServiceMock;
        private readonly Mock<IIdentityUserService> identityUserServiceMock;
        private readonly Mock<IAccessBroker> accessBrokerMock;
        private readonly Mock<IEventEnvelopeBroker> eventEnvelopeBrokerMock;
        private readonly Mock<IEventBroker> eventBrokerMock;
        private readonly Mock<IEnvelopeIntegrityBroker> envelopeIntegrityBrokerMock;
        private readonly Mock<ILoggingBroker> loggingBrokerMock;
        private readonly IApprovalOrchestrationService approvalOrchestrationService;
        private SecurityContext ambientSecurityContext;

        public ApprovalOrchestrationServiceTests()
        {
            this.approvalServiceMock = new Mock<IApprovalWorkflowService>();
            this.approvalReviewServiceMock = new Mock<IApprovalReviewWorkflowService>();
            this.approvalCommentServiceMock = new Mock<IApprovalCommentService>();
            this.approvalReviewRequestServiceMock = new Mock<IApprovalReviewRequestService>();

            this.approvalReviewRequestWorkflowServiceMock =
                new Mock<IApprovalReviewRequestWorkflowService>();

            this.identityUserServiceMock = new Mock<IIdentityUserService>();
            this.accessBrokerMock = new Mock<IAccessBroker>();
            this.eventEnvelopeBrokerMock = new Mock<IEventEnvelopeBroker>();
            this.eventBrokerMock = new Mock<IEventBroker>();
            this.envelopeIntegrityBrokerMock = new Mock<IEnvelopeIntegrityBroker>();
            this.loggingBrokerMock = new Mock<ILoggingBroker>();

            // The publisher tier by default, because that is who reaches the verdict at all.
            // Tests about the gate override it explicitly.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publishers);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.IsAny<Approval>()))
                    .Returns((Approval content) =>
                        new ValueTask<EventEnvelope<Approval>>(
                            new EventEnvelope<Approval>
                            {
                                Content = content,
                                SecurityContext = this.ambientSecurityContext,
                                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                            }));

            // The withdraw path mints an ApprovalReviewRequest envelope rather than an Approval
            // one - it is keyed on the request row, not on the entity behind it.
            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.IsAny<ApprovalReviewRequest>()))
                    .Returns((ApprovalReviewRequest content) =>
                        new ValueTask<EventEnvelope<ApprovalReviewRequest>>(
                            new EventEnvelope<ApprovalReviewRequest>
                            {
                                Content = content,
                                SecurityContext = this.ambientSecurityContext,
                                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                            }));

            // The name resolver reads no approval, so it mints its envelope over the id list
            // itself - there is no entity to hang one off, and the envelope is wanted for its
            // security context alone.
            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.IsAny<IReadOnlyList<string>>()))
                    .Returns((IReadOnlyList<string> content) =>
                        new ValueTask<EventEnvelope<IReadOnlyList<string>>>(
                            new EventEnvelope<IReadOnlyList<string>>
                            {
                                Content = content,
                                SecurityContext = this.ambientSecurityContext,
                                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
                            }));

            // Valid by default. Tests about verification override it; every other
            // test would otherwise be asserting the guard rather than its own subject.
            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<It.IsAnyType>>(),
                    It.IsAny<string>(),
                    It.IsAny<EnvelopeDirection>()))
                        .ReturnsAsync(true);

            this.approvalOrchestrationService = new ApprovalOrchestrationService(
                approvalService: this.approvalServiceMock.Object,
                approvalReviewWorkflowService: this.approvalReviewServiceMock.Object,
                approvalCommentService: this.approvalCommentServiceMock.Object,
                approvalReviewRequestService: this.approvalReviewRequestServiceMock.Object,

                approvalReviewRequestWorkflowService:
                    this.approvalReviewRequestWorkflowServiceMock.Object,

                identityUserService: this.identityUserServiceMock.Object,
                accessBroker: this.accessBrokerMock.Object,
                eventEnvelopeBroker: this.eventEnvelopeBrokerMock.Object,
                eventBroker: this.eventBrokerMock.Object,
                envelopeIntegrityBroker: this.envelopeIntegrityBrokerMock.Object,
                loggingBroker: this.loggingBrokerMock.Object);
        }

        public static TheoryData<Xeption> ApprovalDependencyValidationExceptions()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            return new TheoryData<Xeption>
            {
                new Glory2Him.Core.Models.Foundations.Approvals.Exceptions
                    .ApprovalValidationException(message: randomMessage, innerException: innerException),

                new Glory2Him.Core.Models.Foundations.Approvals.Exceptions
                    .ApprovalDependencyValidationException(message: randomMessage, innerException: innerException),
            };
        }

        public static TheoryData<Xeption> ApprovalDependencyExceptions()
        {
            string randomMessage = GetRandomString();
            var innerException = new Xeption(message: randomMessage);

            return new TheoryData<Xeption>
            {
                new Glory2Him.Core.Models.Foundations.Approvals.Exceptions
                    .ApprovalDependencyException(message: randomMessage, innerException: innerException),

                new Glory2Him.Core.Models.Foundations.Approvals.Exceptions
                    .ApprovalServiceException(message: randomMessage, innerException: innerException),
            };
        }

        // Every role set that must NOT reach the verdict. The verdict names resolved policy, so
        // it is the moderation view (§16.7.2) — a contributor with no review standing is refused
        // even for their own content.
        public static TheoryData<string[]> NonModerationRoleSets() =>
            new TheoryData<string[]>
            {
                new string[0],
                new[] { Roles.ContentItemReadOnly },
            };

        private static ApprovalEntityMatch CreateApprovalMatch(
            ApprovalStatus approvalStatus = ApprovalStatus.Submitted,
            Guid? approvalId = null) =>
            new ApprovalEntityMatch
            {
                Id = approvalId ?? Guid.NewGuid(),
                ApprovalStatus = approvalStatus,
                IsDeleted = false,
            };

        // A conditions verdict with nothing blocking. Counts are pinned rather than drawn so a
        // test asserting the verdict carries them through cannot pass on a coincidence.
        private static ApprovalConditionsVerdict CreateMetConditions(
            int approvalCount = 2,
            int requiredNumberOfApprovals = 2,
            int unresolvedApprovalCommentCount = 0) =>
            new ApprovalConditionsVerdict
            {
                AreConditionsMet = true,
                ShouldAutoApprove = false,
                ShouldResetStaleReviewsOnChange = false,
                BlockReason = AccessDenialReason.None,
                BlockReasons = new List<AccessDenialReason>(),
                ApprovalCount = approvalCount,
                RequiredNumberOfApprovals = requiredNumberOfApprovals,
                UnresolvedApprovalCommentCount = unresolvedApprovalCommentCount,
                Explanation = GetRandomString(),
            };

        private static ApprovalConditionsVerdict CreateBlockedConditions(
            IReadOnlyList<AccessDenialReason> blockReasons,
            int approvalCount = 1,
            int requiredNumberOfApprovals = 3,
            int unresolvedApprovalCommentCount = 2) =>
            new ApprovalConditionsVerdict
            {
                AreConditionsMet = false,
                ShouldAutoApprove = false,
                ShouldResetStaleReviewsOnChange = false,
                BlockReason = blockReasons[0],
                BlockReasons = blockReasons,
                ApprovalCount = approvalCount,
                RequiredNumberOfApprovals = requiredNumberOfApprovals,
                UnresolvedApprovalCommentCount = unresolvedApprovalCommentCount,
                Explanation = GetRandomString(),
            };

        private static AccessVerdict PermittedVerdict() =>
            new AccessVerdict
            {
                IsPermitted = true,
                DenialReason = AccessDenialReason.None,
                IsBypassUsed = false,
                BypassedBlockReason = AccessDenialReason.None,
                Explanation = GetRandomString(),
            };

        // The refusing verdict's Explanation is a distinct token so a leak guard can assert it
        // never reaches the caller — the verdict returns composed messages, never this.
        private static AccessVerdict RefusedVerdict(AccessDenialReason denialReason) =>
            new AccessVerdict
            {
                IsPermitted = false,
                DenialReason = denialReason,
                IsBypassUsed = false,
                BypassedBlockReason = AccessDenialReason.None,
                Explanation = "refused",
            };

        // The verdict asks the decision question TWICE — once plainly, once with the bypass
        // requested — and the two answers drive different fields. Setting them together keeps a
        // test from accidentally proving that one answer drove both.
        private void SetupAccessDecisions(
            AccessVerdict decisionVerdict,
            AccessVerdict bypassVerdict)
        {
            this.accessBrokerMock.Setup(broker =>
                broker.MayDecideApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<ApprovalDecision>(),
                    false,
                    It.IsAny<string>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(decisionVerdict);

            this.accessBrokerMock.Setup(broker =>
                broker.MayDecideApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<ApprovalDecision>(),
                    true,
                    It.IsAny<string>(),
                    It.IsAny<SecurityContext>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(bypassVerdict);
        }

        private void SetupApprovalProbe(ApprovalEntityMatch approvalMatch) =>
            this.approvalServiceMock.Setup(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(approvalMatch);

        private void SetupConditions(ApprovalConditionsVerdict conditionsVerdict) =>
            this.accessBrokerMock.Setup(broker =>
                broker.EvaluateApprovalConditionsByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(conditionsVerdict);

        private static SecurityContext CreateAuthenticatedSecurityContext(params string[] roles) =>
            new SecurityContext
            {
                IsAuthenticated = true,
                Roles = roles
            };

        private static string GetRandomString() =>
            new MnemonicString(wordCount: GetRandomNumber()).GetValue();

        private static int GetRandomNumber() =>
            new IntRange(min: 2, max: 10).GetValue();

        private static Expression<Func<Xeption, bool>> SameExceptionAs(Xeption expectedException) =>
            actualException => actualException.SameExceptionAs(expectedException);
    }
}
