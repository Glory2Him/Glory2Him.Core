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
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Services.Foundations.AIReviewerAssignments;
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

        // Only the WORKFLOW seam. The caller-facing IAIReviewerAssignmentService left this
        // service with the three operations that used it — asking Berean, asking again,
        // withdrawing it — which are IAIReviewerOrchestrationService's contract now and are
        // covered by its own fixture. What remains here is the workflow's own return-to-pending,
        // performed under the system identity on nobody's behalf.
        //
        // Which is why the "not through the caller-facing foundation" assertions the reset and
        // edit paths used to carry are gone rather than restated: this service can no longer
        // reach that seam at all, so the compiler makes the point the assertions were making.
        private readonly Mock<IAIReviewerAssignmentWorkflowService>
            aiReviewerAssignmentWorkflowServiceMock;

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

            // A ROUND WITH NO COMMENTS, unless a test says otherwise. Moq's default for
            // ValueTask<IQueryable<T>> is an EMPTY queryable, but for ValueTask<IReadOnlyList<T>>
            // it is NULL - so when this read stopped handing back a queryable, every test that
            // never mentioned comments began dereferencing null. Stated here once rather than
            // left to a default that differs by return type.
            this.approvalCommentServiceMock.Setup(service =>
                service.RetrieveApprovalCommentsByApprovalIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(
                            (IReadOnlyList<global::Glory2Him.Core.Models.Foundations.ApprovalComments.ApprovalComment>)
                                new List<global::Glory2Him.Core.Models.Foundations.ApprovalComments.ApprovalComment>());
            this.approvalReviewRequestServiceMock = new Mock<IApprovalReviewRequestService>();

            this.approvalReviewRequestWorkflowServiceMock =
                new Mock<IApprovalReviewRequestWorkflowService>();

            this.aiReviewerAssignmentWorkflowServiceMock =
                new Mock<IAIReviewerAssignmentWorkflowService>();

            this.identityUserServiceMock = new Mock<IIdentityUserService>();

            // Nobody is blocked unless a test says so. Without this the veto read would answer
            // null and every candidates and request test would fault on the subtraction rather
            // than on its own subject.
            SetupBlockedUsers();
            this.accessBrokerMock = new Mock<IAccessBroker>();
            this.eventEnvelopeBrokerMock = new Mock<IEventEnvelopeBroker>();
            this.eventBrokerMock = new Mock<IEventBroker>();
            this.envelopeIntegrityBrokerMock = new Mock<IEnvelopeIntegrityBroker>();
            this.loggingBrokerMock = new Mock<ILoggingBroker>();

            // EVERY ENTITY COMMAND IS DELIVERED unless a test says otherwise. PublishCommandAsync
            // now inspects what it publishes (§EVN23), and Moq's default for
            // ValueTask<EventPublishResult<T>> is NULL rather than an empty result - the same
            // return-type trap the comments read above records. The decide tests only ever
            // VERIFIED the publish, so without this every one of them would fault on the
            // inspection instead of on its own subject.
            SetupDeliveredEntityCommands();

            // The subject is VISIBLE unless a test says otherwise. Without this every gate
            // added for §9.7.6 rule 3 would read the mock's default false and refuse, and a
            // suite about thresholds and tiers would be answering "the entity was taken down".
            SetupEntityVisibility(isEntityVisible: true);

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

                aiReviewerAssignmentWorkflowService:
                    this.aiReviewerAssignmentWorkflowServiceMock.Object,

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
            int unresolvedApprovalCommentCount = 0,
            bool shouldAutoApprove = false) =>
            new ApprovalConditionsVerdict
            {
                AreConditionsMet = true,
                ShouldAutoApprove = shouldAutoApprove,
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

        private void SetupApprovalProbe(ApprovalEntityMatch approvalMatch) =>
            this.approvalServiceMock.Setup(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(approvalMatch);

        // The GATHERING seam — what the reset and edit paths read, and deliberately not the
        // caller-facing round-keyed read the AI reviewer's own orchestration uses. That one is
        // identity-filtered and answers null for anyone outside the review tier, which is the
        // ordinary editor; this one is a property of the approval. It also carries the staleness
        // predicate itself, so the orchestration receives an id only when there is something to
        // take back — a round with no assignment and a round whose assignment is already pending
        // both arrive here as the same null, and which is which is pinned where the predicate
        // lives (AccessBrokerTests.FindResettableAIReviewerAssignmentId.Logic.cs).
        //
        // Keyed on the approval rather than It.IsAny so a test cannot pass by answering a
        // question about a different round.
        private void SetupResettableAIReviewerAssignment(
            Guid approvalId,
            Guid? aiReviewerAssignmentId) =>
            this.accessBrokerMock.Setup(broker =>
                broker.FindResettableAIReviewerAssignmentIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(aiReviewerAssignmentId);

        // The workflow seam echoes back a pending row, so a test can assert on the returned row
        // and on the argument and know they are the same thing.
        private void SetupAIReviewerAssignmentReturnToPending() =>
            this.aiReviewerAssignmentWorkflowServiceMock.Setup(service =>
                service.ReturnStaleAIReviewerAssignmentToPendingAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Guid aiReviewerAssignmentId, CancellationToken _) =>
                            new AIReviewerAssignment { Id = aiReviewerAssignmentId });

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

        // One delivered result per address PublishEntityApprovalCommandAsync can reach. Fully
        // qualified rather than imported: this file names seven entity models it otherwise has
        // no business knowing about, and several collide with test-local names.
        private void SetupDeliveredEntityCommands()
        {
            this.eventBrokerMock.Setup(broker =>
                broker.PublishTagAsync(
                    It.IsAny<EventEnvelope<global::Glory2Him.Core.Models.Foundations.Tags.Tag>>(),
                    It.IsAny<global::Glory2Him.Core.Models.Events.Foundations.TagEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<
                            global::Glory2Him.Core.Models.Foundations.Tags.Tag>>(
                                new EventPublishResult<
                                    global::Glory2Him.Core.Models.Foundations.Tags.Tag>()));

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemProcessingAsync(
                    It.IsAny<EventEnvelope<
                        global::Glory2Him.Core.Models.Foundations.ContentItems.ContentItem>>(),
                    It.IsAny<global::Glory2Him.Core.Models.Events.Processings
                        .ContentItemProcessingEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<
                            global::Glory2Him.Core.Models.Foundations.ContentItems.ContentItem>>(
                                new EventPublishResult<
                                    global::Glory2Him.Core.Models.Foundations.ContentItems
                                        .ContentItem>()));

            this.eventBrokerMock.Setup(broker =>
                broker.PublishLinkProcessingAsync(
                    It.IsAny<EventEnvelope<global::Glory2Him.Core.Models.Foundations.Links.Link>>(),
                    It.IsAny<global::Glory2Him.Core.Models.Events.Processings
                        .LinkProcessingEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<
                            global::Glory2Him.Core.Models.Foundations.Links.Link>>(
                                new EventPublishResult<
                                    global::Glory2Him.Core.Models.Foundations.Links.Link>()));

            this.eventBrokerMock.Setup(broker =>
                broker.PublishCommentAsync(
                    It.IsAny<EventEnvelope<
                        global::Glory2Him.Core.Models.Foundations.Comments.Comment>>(),
                    It.IsAny<global::Glory2Him.Core.Models.Events.Foundations
                        .CommentEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<
                            global::Glory2Him.Core.Models.Foundations.Comments.Comment>>(
                                new EventPublishResult<
                                    global::Glory2Him.Core.Models.Foundations.Comments.Comment>()));

            this.eventBrokerMock.Setup(broker =>
                broker.PublishReactionAsync(
                    It.IsAny<EventEnvelope<
                        global::Glory2Him.Core.Models.Foundations.Reactions.Reaction>>(),
                    It.IsAny<global::Glory2Him.Core.Models.Events.Foundations
                        .ReactionEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<
                            global::Glory2Him.Core.Models.Foundations.Reactions.Reaction>>(
                                new EventPublishResult<
                                    global::Glory2Him.Core.Models.Foundations.Reactions
                                        .Reaction>()));

            this.eventBrokerMock.Setup(broker =>
                broker.PublishBibleReferenceAsync(
                    It.IsAny<EventEnvelope<global::Glory2Him.Core.Models.Foundations
                        .BibleReferences.BibleReference>>(),
                    It.IsAny<global::Glory2Him.Core.Models.Events.Foundations
                        .BibleReferenceEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<
                            global::Glory2Him.Core.Models.Foundations.BibleReferences
                                .BibleReference>>(
                                    new EventPublishResult<
                                        global::Glory2Him.Core.Models.Foundations.BibleReferences
                                            .BibleReference>()));

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<
                        global::Glory2Him.Core.Models.Foundations.Associations.Association>>(),
                    It.IsAny<global::Glory2Him.Core.Models.Events.Foundations
                        .AssociationEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<
                            global::Glory2Him.Core.Models.Foundations.Associations.Association>>(
                                new EventPublishResult<
                                    global::Glory2Him.Core.Models.Foundations.Associations
                                        .Association>()));
        }
    }
}
