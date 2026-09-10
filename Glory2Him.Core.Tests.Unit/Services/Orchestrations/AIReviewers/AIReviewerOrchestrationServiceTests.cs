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
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Brokers.EventEnvelopes;
using Glory2Him.Core.Brokers.Loggings;
using Glory2Him.Core.Brokers.Securities;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Services.Foundations.Approvals;
using Glory2Him.Core.Services.Orchestrations.AIReviewers;
using Moq;
using Tynamix.ObjectFiller;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.AIReviewers
{
    /// <summary>
    /// Berean's own orchestration (design §8.6.2, issue #354 Track A) — asking for it, asking
    /// again once it has finished, withdrawing it, and reporting where it stands.
    ///
    /// <para>ITS OWN FIXTURE, and that is the point. These tests ran against
    /// <c>ApprovalOrchestrationServiceTests</c> while the three operations hung off the approval
    /// round's contract, which meant every one of them was arranged through a fixture built for
    /// thirteen dependencies and a reviewer-scope gather the AI path never read. The five
    /// dependencies below are the whole of what this service has, so an arrangement that is not
    /// needed here cannot be made here — the fixture states the seam rather than hiding it.</para>
    /// </summary>
    public partial class AIReviewerOrchestrationServiceTests
    {
        private readonly Mock<IApprovalWorkflowService> approvalServiceMock;
        private readonly Mock<IAIReviewerAssignmentService> aiReviewerAssignmentServiceMock;
        private readonly Mock<IAccessBroker> accessBrokerMock;
        private readonly Mock<IEventEnvelopeBroker> eventEnvelopeBrokerMock;
        private readonly Mock<ILoggingBroker> loggingBrokerMock;
        private readonly IAIReviewerOrchestrationService aiReviewerOrchestrationService;
        private SecurityContext ambientSecurityContext;

        public AIReviewerOrchestrationServiceTests()
        {
            this.approvalServiceMock = new Mock<IApprovalWorkflowService>();
            this.aiReviewerAssignmentServiceMock = new Mock<IAIReviewerAssignmentService>();
            this.accessBrokerMock = new Mock<IAccessBroker>();
            this.eventEnvelopeBrokerMock = new Mock<IEventEnvelopeBroker>();
            this.loggingBrokerMock = new Mock<ILoggingBroker>();

            // The subject is VISIBLE unless a test says otherwise. Without this the §14.5 rule 3
            // gate would read the mock's default false and refuse, and a suite about offers and
            // tiers would be answering "the entity was taken down".
            SetupEntityVisibility(isEntityVisible: true);

            // The publisher tier by default, because that is the ordinary moderator. Tests about
            // the gate override it explicitly.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publishers);

            // The ambient caller the tier gate runs against. Only the Approval envelope is minted
            // on this service — every operation here is keyed on the entity behind the round, and
            // none of them touches an ApprovalReviewRequest row.
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

            this.aiReviewerOrchestrationService = new AIReviewerOrchestrationService(
                approvalService: this.approvalServiceMock.Object,
                aiReviewerAssignmentService: this.aiReviewerAssignmentServiceMock.Object,
                accessBroker: this.accessBrokerMock.Object,
                eventEnvelopeBroker: this.eventEnvelopeBrokerMock.Object,
                loggingBroker: this.loggingBrokerMock.Object);
        }

        // The wrapper sentences this service actually says, pinned rather than restated at each
        // call site. They are its OWN — the approval round's chain says "Content item association
        // orchestration ..." because that chain was copied from the Association orchestration and
        // never reworded, and there was no reason to carry that mistake across.
        private const string ExpectedValidationMessage =
            "AI reviewer orchestration validation error occurred, " +
                "fix the errors and try again.";

        private const string ExpectedDependencyValidationMessage =
            "AI reviewer orchestration dependency validation error occurred, " +
                "fix the errors and try again.";

        private const string ExpectedDependencyMessage =
            "AI reviewer orchestration dependency error occurred, contact support.";

        private const string ExpectedShapeMessage =
            "AI reviewer orchestration request is invalid, fix the errors and try again.";

        // Every role set that must NOT reach any of the three operations. §7.9 rule 2 admits the
        // whole review tier, so the caller who is turned away is one holding no review standing
        // at all — including one carrying only a ReadOnly block.
        public static TheoryData<string[]> NonModerationRoleSets() =>
            new TheoryData<string[]>
            {
                new string[0],
                new[] { Roles.ContentItemReadOnly },
            };

        // The round the resolver finds behind the entity. This is the WHOLE of what these three
        // operations read off a round — its id and its status — which is why this service
        // resolves an ApprovalEntityMatch rather than riding the human flow's reviewer-scope
        // gather (see ResolveAIReviewerApprovalAsync's own note).
        private void SetupResolvedRound(
            Guid approvalId,
            ApprovalStatus approvalStatus = ApprovalStatus.Submitted) =>
            SetupApprovalProbe(new ApprovalEntityMatch
            {
                Id = approvalId,
                ApprovalStatus = approvalStatus,
                IsDeleted = false,
            });

        private void SetupApprovalProbe(ApprovalEntityMatch approvalMatch) =>
            this.approvalServiceMock.Setup(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(approvalMatch);

        // §9.7.6 rule 3 / §14.5 rule 3: whether the entity behind the approval can be seen at
        // all. Deleted and absent are one answer, so one bool covers both.
        private void SetupEntityVisibility(bool isEntityVisible) =>
            this.accessBrokerMock.Setup(broker =>
                broker.IsEntityVisibleAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(isEntityVisible);

        private void SetupEntityApprovalStatus(ApprovalStatus? entityApprovalStatus) =>
            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveEntityApprovalStatusAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(entityApprovalStatus);

        // §8.6.2's switch, answered off IAccessBroker's own narrow member rather than gathered
        // with a reviewer scope. Unstubbed it answers null, which the service reads as "not
        // offered" — so a test wanting the feature ON has to say so, and one about the
        // fail-closed reading says nothing at all.
        private void SetupAIReviewerOffer(bool isOffered) =>
            this.accessBrokerMock.Setup(broker =>
                broker.ResolveAIReviewerPolicyByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new AIReviewerPolicyVerdict { IsOffered = isOffered });

        // The round's ONE live assignment, or the absence of one. Keyed on the approval rather
        // than It.IsAny so a test cannot pass by answering a question about a different round.
        private void SetupStoredAIReviewerAssignment(
            Guid approvalId,
            AIReviewerAssignment storageAssignment) =>
            this.aiReviewerAssignmentServiceMock.Setup(service =>
                service.RetrieveAIReviewerAssignmentByApprovalIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageAssignment);

        // The foundation echoes back what it wrote, so a test can assert on the returned row and
        // on the argument and know they are the same thing.
        private void SetupAIReviewerAssignmentWrites()
        {
            this.aiReviewerAssignmentServiceMock.Setup(service =>
                service.AddAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((AIReviewerAssignment assignment, CancellationToken _) =>
                            assignment);

            this.aiReviewerAssignmentServiceMock.Setup(service =>
                service.ModifyAIReviewerAssignmentAsync(
                    It.IsAny<AIReviewerAssignment>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((AIReviewerAssignment assignment, CancellationToken _) =>
                            assignment);
        }

        private static AIReviewerAssignment CreateAIReviewerAssignment(
            Guid approvalId,
            bool isAIReviewCompleted = false,
            bool isAIReviewCommentsPresent = false) =>
            new AIReviewerAssignment
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
                IsAIReviewCompleted = isAIReviewCompleted,
                IsAIReviewCommentsPresent = isAIReviewCommentsPresent,
            };

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
