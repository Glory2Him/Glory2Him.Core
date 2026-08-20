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
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Glory2Him.Core.Services.Orchestrations.Approvals;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.Approvals
{
    public partial class ApprovalOrchestrationServiceTests
    {
        // Every entity fact handler supplies its EntityType as a LITERAL, so nothing but a test
        // stands between a copy-paste slip and a Tag fact driving a ContentItem's approval — the
        // probe keys on (EntityType, EntityId), so the wrong type resolves a DIFFERENT row and
        // the workflow silently moves the wrong entity's approval.
        //
        // Each route pins its own expectations rather than deriving them from the handler name:
        // computing "TagAdded" from OnTagAddedAsync would put the same value on both sides of
        // the assertion and prove only that the test can do string surgery.
        //
        // The last column says which flow the handler must reach. The two are distinguished by
        // what a DRAFT approval makes them do: the Added flow stops at a Draft (§9.7.3 rule 1),
        // while the Modified flow reads the conditions regardless (§9.7.4). A handler wired to
        // the wrong one therefore fails on the conditions read rather than passing quietly.
        private static readonly (string HandlerName, EntityType EntityType, string EventName, bool IsModifiedFlow)[]
            SubstrateEntityFactRoutes =
            {
                (nameof(IApprovalOrchestrationService.OnTagAddedAsync),
                    EntityType.Tag, "TagAdded", false),

                (nameof(IApprovalOrchestrationService.OnTagModifiedAsync),
                    EntityType.Tag, "TagModified", true),

                (nameof(IApprovalOrchestrationService.OnContentItemAddedAsync),
                    EntityType.ContentItem, "ContentItemProcessingAdded", false),

                (nameof(IApprovalOrchestrationService.OnContentItemModifiedAsync),
                    EntityType.ContentItem, "ContentItemProcessingModified", true),

                (nameof(IApprovalOrchestrationService.OnLinkAddedAsync),
                    EntityType.Link, "LinkProcessingAdded", false),

                (nameof(IApprovalOrchestrationService.OnLinkModifiedAsync),
                    EntityType.Link, "LinkProcessingModified", true),

                (nameof(IApprovalOrchestrationService.OnCommentAddedAsync),
                    EntityType.Comment, "CommentAdded", false),

                (nameof(IApprovalOrchestrationService.OnCommentModifiedAsync),
                    EntityType.Comment, "CommentModified", true),

                (nameof(IApprovalOrchestrationService.OnReactionAddedAsync),
                    EntityType.Reaction, "ReactionAdded", false),

                (nameof(IApprovalOrchestrationService.OnReactionModifiedAsync),
                    EntityType.Reaction, "ReactionModified", true),

                (nameof(IApprovalOrchestrationService.OnBibleReferenceAddedAsync),
                    EntityType.BibleReference, "BibleReferenceAdded", false),

                (nameof(IApprovalOrchestrationService.OnBibleReferenceModifiedAsync),
                    EntityType.BibleReference, "BibleReferenceModified", true),

                (nameof(IApprovalOrchestrationService.OnAssociationAddedAsync),
                    EntityType.Association, "AssociationAdded", false),

                (nameof(IApprovalOrchestrationService.OnAssociationModifiedAsync),
                    EntityType.Association, "AssociationModified", true),
            };

        public static TheoryData<string, EntityType, string, bool> SubstrateEntityFactHandlers()
        {
            var handlers = new TheoryData<string, EntityType, string, bool>();

            foreach (var route in SubstrateEntityFactRoutes)
            {
                handlers.Add(
                    route.HandlerName, route.EntityType, route.EventName, route.IsModifiedFlow);
            }

            return handlers;
        }

        // The refusal theories care only about WHICH handler, never about what it would have
        // routed — nothing downstream of the guard runs. Drawn from the same table rather than
        // listed a second time, so a fifteenth handler cannot be covered for routing and
        // silently left uncovered for refusal.
        public static TheoryData<string> SubstrateEntityFactHandlerNames()
        {
            var handlerNames = new TheoryData<string>();

            foreach (var route in SubstrateEntityFactRoutes)
            {
                handlerNames.Add(route.HandlerName);
            }

            return handlerNames;
        }

        [Theory]
        [MemberData(nameof(SubstrateEntityFactHandlers))]
        public async Task ShouldDriveTheNamedEntityTypeAndContentIdIntoTheFlowOnEntityFactAsync(
            string handlerName,
            EntityType expectedEntityType,
            string expectedEventName,
            bool isModifiedFlow)
        {
            // given: the probe is the first thing either flow does and the only place the two
            // values the handler supplies are observable together, so it is what the routing is
            // asserted on. The approval's own id is deliberately NOT the entity id — a handler
            // that passed the wrong one would otherwise satisfy an assertion naming either.
            var entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();

            // Draft, because that is what separates the two flows: the Added flow ends here and
            // the Modified flow reads on. Neither writes, which is why this suite can prove
            // routing without asserting any storage state.
            Approval storageApproval = CreateSubstrateApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: expectedEntityType);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Draft, approvalId));
            SetupSubstrateApprovalRow(storageApproval);
            SetupConditions(CreateMetConditions());

            // when
            object actualReply = await InvokeSubstrateEntityFactAsync(
                handlerName: handlerName,
                entityId: entityId);

            // then: the signature is checked against the address this handler serves, and as a
            // request — a reply direction verifying here would let a signed response be replayed
            // as an inbound fact.
            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<It.IsAnyType>>(),
                    expectedEventName,
                    EnvelopeDirection.Request),
                Times.Once);

            this.approvalServiceMock.Verify(service =>
                service.FindApprovalByEntityAsync(
                    expectedEntityType,
                    entityId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // The flow the handler chose, read off the one call the two flows disagree about.
            this.accessBrokerMock.Verify(broker =>
                broker.EvaluateApprovalConditionsByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                isModifiedFlow ? Times.Once() : Times.Never());

            // A fact is a notification. Replying would put this service's name on another
            // service's fact, so the responder shape is answered with nothing.
            actualReply.Should().BeNull();

            this.approvalServiceMock.Verify(service =>
                service.AddApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.approvalServiceMock.Verify(service =>
                service.ModifyApprovalAsync(
                    It.IsAny<Approval>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(SubstrateEntityFactHandlerNames))]
        public async Task ShouldRefuseAnEntityFactWhoseSignatureDoesNotVerifyAsync(
            string handlerName)
        {
            // given: this is the trust boundary. The envelope carries the SecurityContext the
            // whole workflow reasons from, so an unverifiable one is refused rather than
            // believed — and refused BEFORE the flow, because resolving an approval already
            // creates a row (§9.7.2) and an auto-approval already publishes a command.
            var entityId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();

            // Armed to succeed. Everything downstream is set up as if the fact were genuine, so
            // a handler that verified after acting would resolve, evaluate and approve — and
            // fail on the "nothing happened" assertions rather than on a missing stub.
            Approval storageApproval = CreateSubstrateApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: ApprovalStatus.Submitted);

            SetupApprovalProbe(CreateApprovalMatch(ApprovalStatus.Submitted, approvalId));
            SetupSubstrateApprovalRow(storageApproval);
            SetupConditions(CreateMetConditions());
            SetupSubstrateFailingVerification();

            var expectedInvalidException =
                new InvalidApprovalOrchestrationException(
                    message: "Approval event is invalid. Integrity verification failed.");

            // when
            InvalidApprovalOrchestrationException actualException =
                await Assert.ThrowsAsync<InvalidApprovalOrchestrationException>(async () =>
                    await InvokeSubstrateEntityFactAsync(
                        handlerName: handlerName,
                        entityId: entityId));

            // then
            actualException.Should().BeEquivalentTo(expectedInvalidException);

            // The flow was never entered: the probe is its first act, and it did not run.
            this.approvalServiceMock.Verify(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.accessBrokerMock.Verify(broker =>
                broker.EvaluateApprovalConditionsByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            // And nothing was written or announced.
            this.approvalServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(SubstrateEntityFactHandlerNames))]
        public async Task ShouldRefuseANullEntityFactEnvelopeAsync(
            string handlerName)
        {
            // given: an envelope is the delivery, so a null one is not an empty fact but a
            // broken one. It is refused where it arrives rather than dereferenced for its
            // content id, which would surface as a null reference from inside the flow.
            var entityId = Guid.NewGuid();

            var expectedInvalidException =
                new InvalidApprovalOrchestrationException(
                    message: "Approval is invalid, fix the errors and try again.");

            // when
            InvalidApprovalOrchestrationException actualException =
                await Assert.ThrowsAsync<InvalidApprovalOrchestrationException>(async () =>
                    await InvokeSubstrateEntityFactAsync(
                        handlerName: handlerName,
                        entityId: entityId,
                        isEnvelopeNull: true));

            // then: refused before the signature is even asked about — there is nothing to sign.
            actualException.Should().BeEquivalentTo(expectedInvalidException);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.approvalServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(SubstrateEntityFactHandlerNames))]
        public async Task ShouldRefuseAnEntityFactCarryingNoContentAsync(
            string handlerName)
        {
            // given: the content is where the row's identity comes from. An envelope without one
            // names no entity, so there is no approval it could belong to — and the flow it
            // would otherwise enter creates a row on a key that names nothing (§9.7.2 rule 1),
            // which only a submit could ever move and no submit can arrive for.
            var entityId = Guid.NewGuid();

            var expectedInvalidException =
                new InvalidApprovalOrchestrationException(
                    message: "Approval is invalid, fix the errors and try again.");

            // when
            InvalidApprovalOrchestrationException actualException =
                await Assert.ThrowsAsync<InvalidApprovalOrchestrationException>(async () =>
                    await InvokeSubstrateEntityFactAsync(
                        handlerName: handlerName,
                        entityId: entityId,
                        isContentNull: true));

            // then
            actualException.Should().BeEquivalentTo(expectedInvalidException);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.approvalServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldKeyTheReviewFactOnItsApprovalIdRatherThanItsOwnIdOnApprovalReviewAddedAsync()
        {
            // given: a review names the round it belongs to directly, so the flow is keyed on
            // ApprovalId. Keying on the review's own Id would read an approval that does not
            // exist — or, worse, one that happens to exist under that id and belongs to
            // somebody else's content.
            //
            // The two ids are pinned separately and the store answers for the APPROVAL id only,
            // so a handler reaching for the review's own id gets nothing back and the flow's
            // not-found guard fires rather than the assertion quietly agreeing.
            var approvalReviewId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            Approval storageApproval = CreateSubstrateApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link);

            var inputApprovalReview = new ApprovalReview
            {
                Id = approvalReviewId,
                ApprovalId = approvalId,
            };

            this.approvalServiceMock.Setup(service =>
                service.RetrieveApprovalByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval);

            // when
            EventEnvelope<ApprovalReview> actualReply =
                await this.approvalOrchestrationService.OnApprovalReviewAddedAsync(
                    envelope: CreateSubstrateEnvelope(inputApprovalReview),
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<It.IsAnyType>>(),
                    "ApprovalReviewAdded",
                    EnvelopeDirection.Request),
                Times.Once);

            this.approvalServiceMock.Verify(service =>
                service.RetrieveApprovalByIdAsync(
                    approvalId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.approvalServiceMock.Verify(service =>
                service.RetrieveApprovalByIdAsync(
                    approvalReviewId,
                    It.IsAny<CancellationToken>()),
                Times.Never);

            // The review flow does not go through the entity probe at all — the approval is
            // named, so resolving one from an entity key would be a second lookup for something
            // already in hand.
            this.approvalServiceMock.Verify(service =>
                service.FindApprovalByEntityAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            actualReply.Should().BeNull();

            this.approvalServiceMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRefuseAReviewFactWhoseSignatureDoesNotVerifyAsync()
        {
            // given: the review fact reaches the same trust boundary the entity facts do, and
            // has more riding on it — the round it names is evaluated, and a met threshold ends
            // it in an approval and a published command. Everything downstream is armed, so a
            // handler verifying after acting fails loudly here.
            var approvalId = Guid.NewGuid();
            var entityId = Guid.NewGuid();

            Approval storageApproval = CreateSubstrateApproval(
                approvalId: approvalId,
                entityId: entityId,
                entityType: EntityType.Link,
                approvalStatus: ApprovalStatus.Submitted);

            var inputApprovalReview = new ApprovalReview
            {
                Id = Guid.NewGuid(),
                ApprovalId = approvalId,
            };

            SetupSubstrateApprovalRow(storageApproval);
            SetupConditions(CreateMetConditions());
            SetupSubstrateFailingVerification();

            var expectedInvalidException =
                new InvalidApprovalOrchestrationException(
                    message: "Approval event is invalid. Integrity verification failed.");

            // when
            ValueTask<EventEnvelope<ApprovalReview>> onApprovalReviewAddedTask =
                this.approvalOrchestrationService.OnApprovalReviewAddedAsync(
                    envelope: CreateSubstrateEnvelope(inputApprovalReview),
                    cancellationToken: TestContext.Current.CancellationToken);

            InvalidApprovalOrchestrationException actualException =
                await Assert.ThrowsAsync<InvalidApprovalOrchestrationException>(
                    onApprovalReviewAddedTask.AsTask);

            // then: the round was never read, so nothing was evaluated and nothing decided.
            actualException.Should().BeEquivalentTo(expectedInvalidException);

            this.approvalServiceMock.Verify(service =>
                service.RetrieveApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.approvalServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRefuseANullReviewFactEnvelopeAsync()
        {
            // given
            var expectedInvalidException =
                new InvalidApprovalOrchestrationException(
                    message: "Approval is invalid, fix the errors and try again.");

            // when
            ValueTask<EventEnvelope<ApprovalReview>> onApprovalReviewAddedTask =
                this.approvalOrchestrationService.OnApprovalReviewAddedAsync(
                    envelope: null,
                    cancellationToken: TestContext.Current.CancellationToken);

            InvalidApprovalOrchestrationException actualException =
                await Assert.ThrowsAsync<InvalidApprovalOrchestrationException>(
                    onApprovalReviewAddedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedInvalidException);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.approvalServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRefuseAReviewFactCarryingNoContentAsync()
        {
            // given: without content there is no ApprovalId, and the round this fact is about is
            // exactly what that field names.
            var expectedInvalidException =
                new InvalidApprovalOrchestrationException(
                    message: "Approval is invalid, fix the errors and try again.");

            // when
            ValueTask<EventEnvelope<ApprovalReview>> onApprovalReviewAddedTask =
                this.approvalOrchestrationService.OnApprovalReviewAddedAsync(
                    envelope: CreateSubstrateEnvelope<ApprovalReview>(content: null),
                    cancellationToken: TestContext.Current.CancellationToken);

            InvalidApprovalOrchestrationException actualException =
                await Assert.ThrowsAsync<InvalidApprovalOrchestrationException>(
                    onApprovalReviewAddedTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedInvalidException);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.approvalServiceMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
        }

        // The handler under test is reached BY NAME, so a theory row naming one handler can never
        // quietly exercise its neighbour: a mis-wired arm answers with the wrong EntityType or
        // the wrong flow and fails on the row's own pinned expectations.
        private async ValueTask<object> InvokeSubstrateEntityFactAsync(
            string handlerName,
            Guid entityId,
            bool isEnvelopeNull = false,
            bool isContentNull = false)
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            IApprovalOrchestrationService service = this.approvalOrchestrationService;

            switch (handlerName)
            {
                case nameof(IApprovalOrchestrationService.OnTagAddedAsync):
                    return await service.OnTagAddedAsync(
                        CreateSubstrateFactEnvelope(
                            new Tag { Id = entityId }, isEnvelopeNull, isContentNull),
                        cancellationToken);

                case nameof(IApprovalOrchestrationService.OnTagModifiedAsync):
                    return await service.OnTagModifiedAsync(
                        CreateSubstrateFactEnvelope(
                            new Tag { Id = entityId }, isEnvelopeNull, isContentNull),
                        cancellationToken);

                case nameof(IApprovalOrchestrationService.OnContentItemAddedAsync):
                    return await service.OnContentItemAddedAsync(
                        CreateSubstrateFactEnvelope(
                            new ContentItem { Id = entityId }, isEnvelopeNull, isContentNull),
                        cancellationToken);

                case nameof(IApprovalOrchestrationService.OnContentItemModifiedAsync):
                    return await service.OnContentItemModifiedAsync(
                        CreateSubstrateFactEnvelope(
                            new ContentItem { Id = entityId }, isEnvelopeNull, isContentNull),
                        cancellationToken);

                case nameof(IApprovalOrchestrationService.OnLinkAddedAsync):
                    return await service.OnLinkAddedAsync(
                        CreateSubstrateFactEnvelope(
                            new Link { Id = entityId }, isEnvelopeNull, isContentNull),
                        cancellationToken);

                case nameof(IApprovalOrchestrationService.OnLinkModifiedAsync):
                    return await service.OnLinkModifiedAsync(
                        CreateSubstrateFactEnvelope(
                            new Link { Id = entityId }, isEnvelopeNull, isContentNull),
                        cancellationToken);

                case nameof(IApprovalOrchestrationService.OnCommentAddedAsync):
                    return await service.OnCommentAddedAsync(
                        CreateSubstrateFactEnvelope(
                            new Comment { Id = entityId }, isEnvelopeNull, isContentNull),
                        cancellationToken);

                case nameof(IApprovalOrchestrationService.OnCommentModifiedAsync):
                    return await service.OnCommentModifiedAsync(
                        CreateSubstrateFactEnvelope(
                            new Comment { Id = entityId }, isEnvelopeNull, isContentNull),
                        cancellationToken);

                case nameof(IApprovalOrchestrationService.OnReactionAddedAsync):
                    return await service.OnReactionAddedAsync(
                        CreateSubstrateFactEnvelope(
                            new Reaction { Id = entityId }, isEnvelopeNull, isContentNull),
                        cancellationToken);

                case nameof(IApprovalOrchestrationService.OnReactionModifiedAsync):
                    return await service.OnReactionModifiedAsync(
                        CreateSubstrateFactEnvelope(
                            new Reaction { Id = entityId }, isEnvelopeNull, isContentNull),
                        cancellationToken);

                case nameof(IApprovalOrchestrationService.OnBibleReferenceAddedAsync):
                    return await service.OnBibleReferenceAddedAsync(
                        CreateSubstrateFactEnvelope(
                            new BibleReference { Id = entityId }, isEnvelopeNull, isContentNull),
                        cancellationToken);

                case nameof(IApprovalOrchestrationService.OnBibleReferenceModifiedAsync):
                    return await service.OnBibleReferenceModifiedAsync(
                        CreateSubstrateFactEnvelope(
                            new BibleReference { Id = entityId }, isEnvelopeNull, isContentNull),
                        cancellationToken);

                case nameof(IApprovalOrchestrationService.OnAssociationAddedAsync):
                    return await service.OnAssociationAddedAsync(
                        CreateSubstrateFactEnvelope(
                            new Association { Id = entityId }, isEnvelopeNull, isContentNull),
                        cancellationToken);

                case nameof(IApprovalOrchestrationService.OnAssociationModifiedAsync):
                    return await service.OnAssociationModifiedAsync(
                        CreateSubstrateFactEnvelope(
                            new Association { Id = entityId }, isEnvelopeNull, isContentNull),
                        cancellationToken);

                default:
                    throw new InvalidOperationException(
                        $"No substrate handler is wired for {handlerName}.");
            }
        }

        private static EventEnvelope<TEntity> CreateSubstrateFactEnvelope<TEntity>(
            TEntity content,
            bool isEnvelopeNull,
            bool isContentNull)
            where TEntity : class
        {
            if (isEnvelopeNull)
            {
                return null;
            }

            return CreateSubstrateEnvelope(isContentNull ? null : content);
        }

        // Metadata is populated because the guard refuses a missing one too, and a fact arriving
        // without it would refuse for a reason no test here is about.
        private static EventEnvelope<TEntity> CreateSubstrateEnvelope<TEntity>(TEntity content) =>
            new EventEnvelope<TEntity>
            {
                Content = content,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() },
            };

        private static Approval CreateSubstrateApproval(
            Guid approvalId,
            Guid entityId,
            EntityType entityType,
            ApprovalStatus approvalStatus = ApprovalStatus.Draft) =>
            new Approval
            {
                Id = approvalId,
                EntityType = entityType,
                EntityId = entityId,
                ApprovalStatus = approvalStatus,
                IsDeleted = false,
            };

        private void SetupSubstrateApprovalRow(Approval storageApproval) =>
            this.approvalServiceMock.Setup(service =>
                service.RetrieveApprovalByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval);

        // Overrides the fixture's verifying default. Every other test would otherwise be
        // asserting the guard rather than its own subject, which is why the default is true.
        private void SetupSubstrateFailingVerification() =>
            this.envelopeIntegrityBrokerMock.Setup(broker =>
                broker.VerifyAsync(
                    It.IsAny<EventEnvelope<It.IsAnyType>>(),
                    It.IsAny<string>(),
                    It.IsAny<EnvelopeDirection>()))
                        .ReturnsAsync(false);
    }
}
