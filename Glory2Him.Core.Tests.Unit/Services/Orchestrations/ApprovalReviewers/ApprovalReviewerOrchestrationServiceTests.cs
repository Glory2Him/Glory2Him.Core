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
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.ApprovalComments;
using Glory2Him.Core.Services.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Services.Foundations.Approvals;
using Glory2Him.Core.Services.Foundations.IdentityUsers;
using Glory2Him.Core.Services.Orchestrations.ApprovalReviewers;
using Moq;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.ApprovalReviewers
{
    public partial class ApprovalReviewerOrchestrationServiceTests
    {
        private readonly Mock<IApprovalReviewRequestService> approvalReviewRequestServiceMock;
        private readonly Mock<IApprovalCommentService> approvalCommentServiceMock;
        private readonly Mock<IIdentityUserService> identityUserServiceMock;
        private readonly Mock<IApprovalWorkflowService> approvalServiceMock;
        private readonly Mock<IAccessBroker> accessBrokerMock;
        private readonly Mock<IEventEnvelopeBroker> eventEnvelopeBrokerMock;
        private readonly Mock<ILoggingBroker> loggingBrokerMock;
        private readonly IApprovalReviewerOrchestrationService approvalReviewerOrchestrationService;
        private SecurityContext ambientSecurityContext;

        public ApprovalReviewerOrchestrationServiceTests()
        {
            this.approvalReviewRequestServiceMock = new Mock<IApprovalReviewRequestService>();
            this.approvalCommentServiceMock = new Mock<IApprovalCommentService>();

            // A ROUND WITH NO COMMENTS, unless a test says otherwise. Moq's default for
            // ValueTask<IReadOnlyList<T>> is NULL rather than an empty list, so without this every
            // test that never mentions comments would dereference null. Stated here once rather
            // than left to a default that differs by return type.
            this.approvalCommentServiceMock.Setup(service =>
                service.RetrieveApprovalCommentsByApprovalIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((IReadOnlyList<ApprovalComment>)new List<ApprovalComment>());

            this.identityUserServiceMock = new Mock<IIdentityUserService>();

            // Nobody is blocked unless a test says so. Without this the veto read would answer
            // null and every candidates and request test would fault on the subtraction rather
            // than on its own subject.
            SetupBlockedUsers();

            this.approvalServiceMock = new Mock<IApprovalWorkflowService>();
            this.accessBrokerMock = new Mock<IAccessBroker>();
            this.eventEnvelopeBrokerMock = new Mock<IEventEnvelopeBroker>();
            this.loggingBrokerMock = new Mock<ILoggingBroker>();

            // The subject is VISIBLE unless a test says otherwise. Without this every §14.5 rule 3
            // gate would read the mock's default false and refuse, and a suite about tiers and
            // invitations would be answering "the entity was taken down".
            SetupEntityVisibility(isEntityVisible: true);

            // The publisher tier by default, because that is who reaches these operations at all.
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

            this.approvalReviewerOrchestrationService = new ApprovalReviewerOrchestrationService(
                approvalReviewRequestService: this.approvalReviewRequestServiceMock.Object,
                approvalCommentService: this.approvalCommentServiceMock.Object,
                identityUserService: this.identityUserServiceMock.Object,
                approvalService: this.approvalServiceMock.Object,
                accessBroker: this.accessBrokerMock.Object,
                eventEnvelopeBroker: this.eventEnvelopeBrokerMock.Object,
                loggingBroker: this.loggingBrokerMock.Object);
        }

        // The wrapper sentences this service actually says, pinned rather than restated at each
        // call site. They are its OWN — the approval round's chain says "Content item association
        // orchestration ..." because that chain was copied from the Association orchestration and
        // never reworded, and there was no reason to carry that mistake across, exactly as the AI
        // reviewer's own split did not.
        private const string ExpectedDependencyValidationMessage =
            "Approval reviewer orchestration dependency validation error occurred, " +
                "fix the errors and try again.";

        private const string ExpectedDependencyMessage =
            "Approval reviewer orchestration dependency error occurred, contact support.";

        // Every role set that must NOT reach any of these operations. §7.9 rule 2 admits the whole
        // review tier, so the caller who is turned away is one holding no review standing at all —
        // including one carrying only a ReadOnly block.
        public static TheoryData<string[]> NonModerationRoleSets() =>
            new TheoryData<string[]>
            {
                new string[0],
                new[] { Roles.ContentItemReadOnly },
            };

        // §9.7.6 rule 3 / §14.5 rule 3: whether the entity behind the approval can be seen at
        // all. Deleted and absent are one answer, so one bool covers both.
        private void SetupEntityVisibility(bool isEntityVisible) =>
            this.accessBrokerMock.Setup(broker =>
                broker.IsEntityVisibleAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(isEntityVisible);

        private void SetupEntityApprovalStatus(
            EntityType entityType,
            Guid entityId,
            ApprovalStatus? entityApprovalStatus) =>
            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveEntityApprovalStatusAsync(
                    entityType,
                    entityId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(entityApprovalStatus);

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
