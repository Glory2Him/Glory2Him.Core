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
using Glory2Him.Core.Brokers.Events;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Registrations;
using Glory2Him.Core.Services.Foundations.Approvals;
using Glory2Him.Core.Services.Foundations.ContentItems;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Collections.Generic;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Services.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Services.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Services.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Services.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Services.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Services.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Services.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Services.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Services.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Services.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Events.Processings;
using Glory2Him.Core.Services.Orchestrations.Approvals;
using Glory2Him.Core.Services.Processings.ContentItems;
using Glory2Him.Core.Services.Processings.Links;

namespace Glory2Him.Core.Tests.Unit.Registrations
{
    public partial class EventSubscriptionRegistrationTests
    {
        private readonly Mock<IEventBroker> eventBrokerMock;
        private readonly Mock<IServiceScopeFactory> serviceScopeFactoryMock;
        private readonly Mock<IContentItemService> contentItemServiceMock;
        private readonly Mock<IApprovalService> approvalServiceMock;
        private readonly Mock<IBibleReferenceService> bibleReferenceServiceMock;
        private readonly Mock<ITagService> tagServiceMock;
        private readonly Mock<ILinkService> linkServiceMock;
        private readonly Mock<IReactionService> reactionServiceMock;
        private readonly Mock<ICommentService> commentServiceMock;
        private readonly Mock<IApprovalCommentService> approvalCommentServiceMock;
        private readonly Mock<IApprovalReviewService> approvalReviewServiceMock;
        private readonly Mock<IApprovalSettingService> approvalSettingServiceMock;
        private readonly Mock<IAssociationService> associationServiceMock;
        private readonly Mock<IContentItemSettingService> contentItemSettingServiceMock;
        private readonly Mock<IContentItemProcessingService> contentItemProcessingServiceMock;
        private readonly Mock<ILinkProcessingService> linkProcessingServiceMock;
        private readonly Mock<IApprovalOrchestrationService> approvalOrchestrationServiceMock;
        private readonly IEventSubscriptionRegistration eventSubscriptionRegistration;

        public EventSubscriptionRegistrationTests()
        {
            this.eventBrokerMock = new Mock<IEventBroker>();
            this.contentItemServiceMock = new Mock<IContentItemService>();
            this.approvalServiceMock = new Mock<IApprovalService>();
            this.bibleReferenceServiceMock = new Mock<IBibleReferenceService>();
            this.tagServiceMock = new Mock<ITagService>();
            this.linkServiceMock = new Mock<ILinkService>();
            this.reactionServiceMock = new Mock<IReactionService>();
            this.commentServiceMock = new Mock<ICommentService>();
            this.approvalCommentServiceMock = new Mock<IApprovalCommentService>();
            this.approvalReviewServiceMock = new Mock<IApprovalReviewService>();
            this.approvalSettingServiceMock = new Mock<IApprovalSettingService>();
            this.associationServiceMock = new Mock<IAssociationService>();
            this.contentItemSettingServiceMock = new Mock<IContentItemSettingService>();
            this.contentItemProcessingServiceMock = new Mock<IContentItemProcessingService>();
            this.linkProcessingServiceMock = new Mock<ILinkProcessingService>();
            this.approvalOrchestrationServiceMock = new Mock<IApprovalOrchestrationService>();

            // The registration no longer holds services; it opens a scope per delivery and
            // resolves from it. The provider hands back the same mocks, so every assertion
            // below still reads as "this subscription reaches this service's method" — and now
            // proves the scope resolution does it, which a held method group never could.
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(p => p.GetService(typeof(IContentItemService)))
                .Returns(this.contentItemServiceMock.Object);
            serviceProviderMock.Setup(p => p.GetService(typeof(IApprovalService)))
                .Returns(this.approvalServiceMock.Object);
            serviceProviderMock.Setup(p => p.GetService(typeof(IBibleReferenceService)))
                .Returns(this.bibleReferenceServiceMock.Object);
            serviceProviderMock.Setup(p => p.GetService(typeof(ITagService)))
                .Returns(this.tagServiceMock.Object);
            serviceProviderMock.Setup(p => p.GetService(typeof(ILinkService)))
                .Returns(this.linkServiceMock.Object);
            serviceProviderMock.Setup(p => p.GetService(typeof(IReactionService)))
                .Returns(this.reactionServiceMock.Object);
            serviceProviderMock.Setup(p => p.GetService(typeof(ICommentService)))
                .Returns(this.commentServiceMock.Object);
            serviceProviderMock.Setup(p => p.GetService(typeof(IApprovalCommentService)))
                .Returns(this.approvalCommentServiceMock.Object);
            serviceProviderMock.Setup(p => p.GetService(typeof(IApprovalReviewService)))
                .Returns(this.approvalReviewServiceMock.Object);
            serviceProviderMock.Setup(p => p.GetService(typeof(IApprovalSettingService)))
                .Returns(this.approvalSettingServiceMock.Object);
            serviceProviderMock.Setup(p => p.GetService(typeof(IAssociationService)))
                .Returns(this.associationServiceMock.Object);
            serviceProviderMock.Setup(p => p.GetService(typeof(IContentItemSettingService)))
                .Returns(this.contentItemSettingServiceMock.Object);
            serviceProviderMock.Setup(p => p.GetService(typeof(IContentItemProcessingService)))
                .Returns(this.contentItemProcessingServiceMock.Object);
            serviceProviderMock.Setup(p => p.GetService(typeof(ILinkProcessingService)))
                .Returns(this.linkProcessingServiceMock.Object);
            serviceProviderMock.Setup(p => p.GetService(typeof(IApprovalOrchestrationService)))
                .Returns(this.approvalOrchestrationServiceMock.Object);

            var serviceScopeMock = new Mock<IServiceScope>();
            serviceScopeMock.Setup(scope => scope.ServiceProvider)
                .Returns(serviceProviderMock.Object);

            this.serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            this.serviceScopeFactoryMock.Setup(factory => factory.CreateScope())
                .Returns(serviceScopeMock.Object);

            this.eventSubscriptionRegistration = new EventSubscriptionRegistration(
                eventBroker: this.eventBrokerMock.Object,
                serviceScopeFactory: this.serviceScopeFactoryMock.Object);
        }

        private void VerifyLinkProcessingSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            LinkProcessingEventOperation expectedOperation,
            Func<EventEnvelope<Link>, CancellationToken,
                ValueTask<EventEnvelope<Link>?>> expectedHandler)
        {
            VerifySubscription<Link>(
                expectedSubscriptionName: expectedSubscriptionName,
                expectedHandler: expectedHandler,
                capture: captured =>
                    this.eventBrokerMock.Verify(broker =>
                        broker.SubscribeToLinkProcessingEventAsync(
                            It.Is<EventSubscription>(subscription =>
                                subscription.Id == expectedSubscriptionId
                                    && subscription.Name == expectedSubscriptionName),
                            expectedOperation,
                            It.Is<Func<EventEnvelope<Link>, CancellationToken,
                                ValueTask<EventEnvelope<Link>?>>>(handler =>
                                    captured(handler)),
                            It.IsAny<CancellationToken>()),
                        Times.Once));
        }

        private void VerifyContentItemProcessingSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            ContentItemProcessingEventOperation expectedOperation,
            Func<EventEnvelope<ContentItem>, CancellationToken,
                ValueTask<EventEnvelope<ContentItem>?>> expectedHandler)
        {
            VerifySubscription<ContentItem>(
                expectedSubscriptionName: expectedSubscriptionName,
                expectedHandler: expectedHandler,
                capture: captured =>
                    this.eventBrokerMock.Verify(broker =>
                        broker.SubscribeToContentItemProcessingEventAsync(
                            It.Is<EventSubscription>(subscription =>
                                subscription.Id == expectedSubscriptionId
                                    && subscription.Name == expectedSubscriptionName),
                            expectedOperation,
                            It.Is<Func<EventEnvelope<ContentItem>, CancellationToken,
                                ValueTask<EventEnvelope<ContentItem>?>>>(handler =>
                                    captured(handler)),
                            It.IsAny<CancellationToken>()),
                        Times.Once));
        }

        private void VerifyContentItemSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            ContentItemEventOperation expectedOperation,
            Func<EventEnvelope<ContentItem>, CancellationToken,
                ValueTask<EventEnvelope<ContentItem>?>> expectedHandler)
        {
            VerifySubscription<ContentItem>(
                expectedSubscriptionName: expectedSubscriptionName,
                expectedHandler: expectedHandler,
                capture: captured =>
                    this.eventBrokerMock.Verify(broker =>
                        broker.SubscribeToContentItemEventAsync(
                            It.Is<EventSubscription>(subscription =>
                                subscription.Id == expectedSubscriptionId
                                    && subscription.Name == expectedSubscriptionName),
                            expectedOperation,
                            It.Is<Func<EventEnvelope<ContentItem>, CancellationToken,
                                ValueTask<EventEnvelope<ContentItem>?>>>(handler =>
                                    captured(handler)),
                            It.IsAny<CancellationToken>()),
                        Times.Once));
        }

        private void VerifyApprovalSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            ApprovalEventOperation expectedOperation,
            Func<EventEnvelope<Approval>, CancellationToken,
                ValueTask<EventEnvelope<Approval>?>> expectedHandler)
        {
            VerifySubscription<Approval>(
                expectedSubscriptionName: expectedSubscriptionName,
                expectedHandler: expectedHandler,
                capture: captured =>
                    this.eventBrokerMock.Verify(broker =>
                        broker.SubscribeToApprovalEventAsync(
                            It.Is<EventSubscription>(subscription =>
                                subscription.Id == expectedSubscriptionId
                                    && subscription.Name == expectedSubscriptionName),
                            expectedOperation,
                            It.Is<Func<EventEnvelope<Approval>, CancellationToken,
                                ValueTask<EventEnvelope<Approval>?>>>(handler =>
                                    captured(handler)),
                            It.IsAny<CancellationToken>()),
                        Times.Once));
        }

        private void VerifyBibleReferenceSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            BibleReferenceEventOperation expectedOperation,
            Func<EventEnvelope<BibleReference>, CancellationToken,
                ValueTask<EventEnvelope<BibleReference>?>> expectedHandler)
        {
            VerifySubscription<BibleReference>(
                expectedSubscriptionName: expectedSubscriptionName,
                expectedHandler: expectedHandler,
                capture: captured =>
                    this.eventBrokerMock.Verify(broker =>
                        broker.SubscribeToBibleReferenceEventAsync(
                            It.Is<EventSubscription>(subscription =>
                                subscription.Id == expectedSubscriptionId
                                    && subscription.Name == expectedSubscriptionName),
                            expectedOperation,
                            It.Is<Func<EventEnvelope<BibleReference>, CancellationToken,
                                ValueTask<EventEnvelope<BibleReference>?>>>(handler =>
                                    captured(handler)),
                            It.IsAny<CancellationToken>()),
                        Times.Once));
        }

        private void VerifyTagSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            TagEventOperation expectedOperation,
            Func<EventEnvelope<Tag>, CancellationToken,
                ValueTask<EventEnvelope<Tag>?>> expectedHandler)
        {
            VerifySubscription<Tag>(
                expectedSubscriptionName: expectedSubscriptionName,
                expectedHandler: expectedHandler,
                capture: captured =>
                    this.eventBrokerMock.Verify(broker =>
                        broker.SubscribeToTagEventAsync(
                            It.Is<EventSubscription>(subscription =>
                                subscription.Id == expectedSubscriptionId
                                    && subscription.Name == expectedSubscriptionName),
                            expectedOperation,
                            It.Is<Func<EventEnvelope<Tag>, CancellationToken,
                                ValueTask<EventEnvelope<Tag>?>>>(handler =>
                                    captured(handler)),
                            It.IsAny<CancellationToken>()),
                        Times.Once));
        }

        private void VerifyLinkSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            LinkEventOperation expectedOperation,
            Func<EventEnvelope<Link>, CancellationToken,
                ValueTask<EventEnvelope<Link>?>> expectedHandler)
        {
            VerifySubscription<Link>(
                expectedSubscriptionName: expectedSubscriptionName,
                expectedHandler: expectedHandler,
                capture: captured =>
                    this.eventBrokerMock.Verify(broker =>
                        broker.SubscribeToLinkEventAsync(
                            It.Is<EventSubscription>(subscription =>
                                subscription.Id == expectedSubscriptionId
                                    && subscription.Name == expectedSubscriptionName),
                            expectedOperation,
                            It.Is<Func<EventEnvelope<Link>, CancellationToken,
                                ValueTask<EventEnvelope<Link>?>>>(handler =>
                                    captured(handler)),
                            It.IsAny<CancellationToken>()),
                        Times.Once));
        }

        private void VerifyReactionSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            ReactionEventOperation expectedOperation,
            Func<EventEnvelope<Reaction>, CancellationToken,
                ValueTask<EventEnvelope<Reaction>?>> expectedHandler)
        {
            VerifySubscription<Reaction>(
                expectedSubscriptionName: expectedSubscriptionName,
                expectedHandler: expectedHandler,
                capture: captured =>
                    this.eventBrokerMock.Verify(broker =>
                        broker.SubscribeToReactionEventAsync(
                            It.Is<EventSubscription>(subscription =>
                                subscription.Id == expectedSubscriptionId
                                    && subscription.Name == expectedSubscriptionName),
                            expectedOperation,
                            It.Is<Func<EventEnvelope<Reaction>, CancellationToken,
                                ValueTask<EventEnvelope<Reaction>?>>>(handler =>
                                    captured(handler)),
                            It.IsAny<CancellationToken>()),
                        Times.Once));
        }

        private void VerifyCommentSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            CommentEventOperation expectedOperation,
            Func<EventEnvelope<Comment>, CancellationToken,
                ValueTask<EventEnvelope<Comment>?>> expectedHandler)
        {
            VerifySubscription<Comment>(
                expectedSubscriptionName: expectedSubscriptionName,
                expectedHandler: expectedHandler,
                capture: captured =>
                    this.eventBrokerMock.Verify(broker =>
                        broker.SubscribeToCommentEventAsync(
                            It.Is<EventSubscription>(subscription =>
                                subscription.Id == expectedSubscriptionId
                                    && subscription.Name == expectedSubscriptionName),
                            expectedOperation,
                            It.Is<Func<EventEnvelope<Comment>, CancellationToken,
                                ValueTask<EventEnvelope<Comment>?>>>(handler =>
                                    captured(handler)),
                            It.IsAny<CancellationToken>()),
                        Times.Once));
        }

        private void VerifyApprovalCommentSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            ApprovalCommentEventOperation expectedOperation,
            Func<EventEnvelope<ApprovalComment>, CancellationToken,
                ValueTask<EventEnvelope<ApprovalComment>?>> expectedHandler)
        {
            VerifySubscription<ApprovalComment>(
                expectedSubscriptionName: expectedSubscriptionName,
                expectedHandler: expectedHandler,
                capture: captured =>
                    this.eventBrokerMock.Verify(broker =>
                        broker.SubscribeToApprovalCommentEventAsync(
                            It.Is<EventSubscription>(subscription =>
                                subscription.Id == expectedSubscriptionId
                                    && subscription.Name == expectedSubscriptionName),
                            expectedOperation,
                            It.Is<Func<EventEnvelope<ApprovalComment>, CancellationToken,
                                ValueTask<EventEnvelope<ApprovalComment>?>>>(handler =>
                                    captured(handler)),
                            It.IsAny<CancellationToken>()),
                        Times.Once));
        }

        private void VerifyApprovalReviewSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            ApprovalReviewEventOperation expectedOperation,
            Func<EventEnvelope<ApprovalReview>, CancellationToken,
                ValueTask<EventEnvelope<ApprovalReview>?>> expectedHandler)
        {
            VerifySubscription<ApprovalReview>(
                expectedSubscriptionName: expectedSubscriptionName,
                expectedHandler: expectedHandler,
                capture: captured =>
                    this.eventBrokerMock.Verify(broker =>
                        broker.SubscribeToApprovalReviewEventAsync(
                            It.Is<EventSubscription>(subscription =>
                                subscription.Id == expectedSubscriptionId
                                    && subscription.Name == expectedSubscriptionName),
                            expectedOperation,
                            It.Is<Func<EventEnvelope<ApprovalReview>, CancellationToken,
                                ValueTask<EventEnvelope<ApprovalReview>?>>>(handler =>
                                    captured(handler)),
                            It.IsAny<CancellationToken>()),
                        Times.Once));
        }

        private void VerifyApprovalSettingSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            ApprovalSettingEventOperation expectedOperation,
            Func<EventEnvelope<ApprovalSetting>, CancellationToken,
                ValueTask<EventEnvelope<ApprovalSetting>?>> expectedHandler)
        {
            VerifySubscription<ApprovalSetting>(
                expectedSubscriptionName: expectedSubscriptionName,
                expectedHandler: expectedHandler,
                capture: captured =>
                    this.eventBrokerMock.Verify(broker =>
                        broker.SubscribeToApprovalSettingEventAsync(
                            It.Is<EventSubscription>(subscription =>
                                subscription.Id == expectedSubscriptionId
                                    && subscription.Name == expectedSubscriptionName),
                            expectedOperation,
                            It.Is<Func<EventEnvelope<ApprovalSetting>, CancellationToken,
                                ValueTask<EventEnvelope<ApprovalSetting>?>>>(handler =>
                                    captured(handler)),
                            It.IsAny<CancellationToken>()),
                        Times.Once));
        }

        private void VerifyAssociationSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            AssociationEventOperation expectedOperation,
            Func<EventEnvelope<Association>, CancellationToken,
                ValueTask<EventEnvelope<Association>?>> expectedHandler)
        {
            VerifySubscription<Association>(
                expectedSubscriptionName: expectedSubscriptionName,
                expectedHandler: expectedHandler,
                capture: captured =>
                    this.eventBrokerMock.Verify(broker =>
                        broker.SubscribeToAssociationEventAsync(
                            It.Is<EventSubscription>(subscription =>
                                subscription.Id == expectedSubscriptionId
                                    && subscription.Name == expectedSubscriptionName),
                            expectedOperation,
                            It.Is<Func<EventEnvelope<Association>, CancellationToken,
                                ValueTask<EventEnvelope<Association>?>>>(handler =>
                                    captured(handler)),
                            It.IsAny<CancellationToken>()),
                        Times.Once));
        }

        private void VerifyContentItemSettingSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            ContentItemSettingEventOperation expectedOperation,
            Func<EventEnvelope<ContentItemSetting>, CancellationToken,
                ValueTask<EventEnvelope<ContentItemSetting>?>> expectedHandler)
        {
            VerifySubscription<ContentItemSetting>(
                expectedSubscriptionName: expectedSubscriptionName,
                expectedHandler: expectedHandler,
                capture: captured =>
                    this.eventBrokerMock.Verify(broker =>
                        broker.SubscribeToContentItemSettingEventAsync(
                            It.Is<EventSubscription>(subscription =>
                                subscription.Id == expectedSubscriptionId
                                    && subscription.Name == expectedSubscriptionName),
                            expectedOperation,
                            It.Is<Func<EventEnvelope<ContentItemSetting>, CancellationToken,
                                ValueTask<EventEnvelope<ContentItemSetting>?>>>(handler =>
                                    captured(handler)),
                            It.IsAny<CancellationToken>()),
                        Times.Once));
        }

        [Fact]
        public async Task ShouldRegisterParticipantAddressesAndAllSubscriptionsAsync()
        {
            // when
            await this.eventSubscriptionRegistration.RegisterAsync(
                TestContext.Current.CancellationToken);

            // then
            this.eventBrokerMock.Verify(broker =>
                broker.RegisterEventParticipantAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.RegisterEventAddressesAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            VerifyContentItemSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers.ContentItemOnAddingContentItemSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers.ContentItemOnAddingContentItemSubscriptionName,
                expectedOperation: ContentItemEventOperation.Adding,
                expectedHandler: this.contentItemServiceMock.Object.OnAddingContentItemAsync);

            VerifyContentItemSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers.ContentItemOnModifyingContentItemSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers.ContentItemOnModifyingContentItemSubscriptionName,
                expectedOperation: ContentItemEventOperation.Modifying,
                expectedHandler: this.contentItemServiceMock.Object.OnModifyingContentItemAsync);

            VerifyContentItemSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ContentItemOnRemovingContentItemByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ContentItemOnRemovingContentItemByIdSubscriptionName,
                expectedOperation: ContentItemEventOperation.RemovingById,
                expectedHandler: this.contentItemServiceMock.Object.OnRemovingContentItemByIdAsync);

            VerifyContentItemSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ContentItemOnHardRemovingContentItemByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ContentItemOnHardRemovingContentItemByIdSubscriptionName,
                expectedOperation: ContentItemEventOperation.HardRemovingById,
                expectedHandler: this.contentItemServiceMock.Object.OnHardRemovingContentItemByIdAsync);

            VerifyContentItemSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ContentItemOnRetrievingContentItemByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ContentItemOnRetrievingContentItemByIdSubscriptionName,
                expectedOperation: ContentItemEventOperation.RetrievingById,
                expectedHandler: this.contentItemServiceMock.Object.OnRetrievingContentItemByIdAsync);

            VerifyContentItemSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ContentItemOnSubmittingContentItemSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ContentItemOnSubmittingContentItemSubscriptionName,
                expectedOperation: ContentItemEventOperation.Submitting,
                expectedHandler: this.contentItemServiceMock.Object.OnSubmittingContentItemAsync);

            VerifyContentItemSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ContentItemOnApprovingContentItemSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ContentItemOnApprovingContentItemSubscriptionName,
                expectedOperation: ContentItemEventOperation.Approving,
                expectedHandler: this.contentItemServiceMock.Object.OnApprovingContentItemAsync);

            VerifyApprovalSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers.ApprovalOnAddingApprovalSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers.ApprovalOnAddingApprovalSubscriptionName,
                expectedOperation: ApprovalEventOperation.Adding,
                expectedHandler: this.approvalServiceMock.Object.OnAddingApprovalAsync);

            VerifyApprovalSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers.ApprovalOnModifyingApprovalSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers.ApprovalOnModifyingApprovalSubscriptionName,
                expectedOperation: ApprovalEventOperation.Modifying,
                expectedHandler: this.approvalServiceMock.Object.OnModifyingApprovalAsync);

            VerifyApprovalSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalOnRemovingApprovalByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalOnRemovingApprovalByIdSubscriptionName,
                expectedOperation: ApprovalEventOperation.RemovingById,
                expectedHandler: this.approvalServiceMock.Object.OnRemovingApprovalByIdAsync);

            VerifyApprovalSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalOnHardRemovingApprovalByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalOnHardRemovingApprovalByIdSubscriptionName,
                expectedOperation: ApprovalEventOperation.HardRemovingById,
                expectedHandler: this.approvalServiceMock.Object.OnHardRemovingApprovalByIdAsync);

            VerifyApprovalSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalOnRetrievingApprovalByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalOnRetrievingApprovalByIdSubscriptionName,
                expectedOperation: ApprovalEventOperation.RetrievingById,
                expectedHandler: this.approvalServiceMock.Object.OnRetrievingApprovalByIdAsync);

            VerifyBibleReferenceSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.BibleReferenceOnAddingBibleReferenceSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.BibleReferenceOnAddingBibleReferenceSubscriptionName,
                expectedOperation: BibleReferenceEventOperation.Adding,
                expectedHandler: this.bibleReferenceServiceMock.Object.OnAddingBibleReferenceAsync);

            VerifyBibleReferenceSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.BibleReferenceOnModifyingBibleReferenceSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.BibleReferenceOnModifyingBibleReferenceSubscriptionName,
                expectedOperation: BibleReferenceEventOperation.Modifying,
                expectedHandler: this.bibleReferenceServiceMock.Object.OnModifyingBibleReferenceAsync);

            VerifyBibleReferenceSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.BibleReferenceOnRemovingBibleReferenceByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.BibleReferenceOnRemovingBibleReferenceByIdSubscriptionName,
                expectedOperation: BibleReferenceEventOperation.RemovingById,
                expectedHandler: this.bibleReferenceServiceMock.Object.OnRemovingBibleReferenceByIdAsync);

            VerifyBibleReferenceSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.BibleReferenceOnHardRemovingBibleReferenceByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.BibleReferenceOnHardRemovingBibleReferenceByIdSubscriptionName,
                expectedOperation: BibleReferenceEventOperation.HardRemovingById,
                expectedHandler: this.bibleReferenceServiceMock.Object.OnHardRemovingBibleReferenceByIdAsync);

            VerifyBibleReferenceSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.BibleReferenceOnRetrievingBibleReferenceByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.BibleReferenceOnRetrievingBibleReferenceByIdSubscriptionName,
                expectedOperation: BibleReferenceEventOperation.RetrievingById,
                expectedHandler: this.bibleReferenceServiceMock.Object.OnRetrievingBibleReferenceByIdAsync);

            VerifyBibleReferenceSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.BibleReferenceOnSubmittingBibleReferenceSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.BibleReferenceOnSubmittingBibleReferenceSubscriptionName,
                expectedOperation: BibleReferenceEventOperation.Submitting,
                expectedHandler: this.bibleReferenceServiceMock.Object.OnSubmittingBibleReferenceAsync);

            VerifyBibleReferenceSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.BibleReferenceOnApprovingBibleReferenceSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.BibleReferenceOnApprovingBibleReferenceSubscriptionName,
                expectedOperation: BibleReferenceEventOperation.Approving,
                expectedHandler: this.bibleReferenceServiceMock.Object.OnApprovingBibleReferenceAsync);

            VerifyTagSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers.TagOnAddingTagSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers.TagOnAddingTagSubscriptionName,
                expectedOperation: TagEventOperation.Adding,
                expectedHandler: this.tagServiceMock.Object.OnAddingTagAsync);

            VerifyTagSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers.TagOnModifyingTagSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers.TagOnModifyingTagSubscriptionName,
                expectedOperation: TagEventOperation.Modifying,
                expectedHandler: this.tagServiceMock.Object.OnModifyingTagAsync);

            VerifyTagSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers.TagOnRemovingTagByIdSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers.TagOnRemovingTagByIdSubscriptionName,
                expectedOperation: TagEventOperation.RemovingById,
                expectedHandler: this.tagServiceMock.Object.OnRemovingTagByIdAsync);

            VerifyTagSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.TagOnHardRemovingTagByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.TagOnHardRemovingTagByIdSubscriptionName,
                expectedOperation: TagEventOperation.HardRemovingById,
                expectedHandler: this.tagServiceMock.Object.OnHardRemovingTagByIdAsync);

            VerifyTagSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.TagOnRetrievingTagByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.TagOnRetrievingTagByIdSubscriptionName,
                expectedOperation: TagEventOperation.RetrievingById,
                expectedHandler: this.tagServiceMock.Object.OnRetrievingTagByIdAsync);

            VerifyTagSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.TagOnSubmittingTagSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.TagOnSubmittingTagSubscriptionName,
                expectedOperation: TagEventOperation.Submitting,
                expectedHandler: this.tagServiceMock.Object.OnSubmittingTagAsync);

            VerifyTagSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.TagOnApprovingTagSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.TagOnApprovingTagSubscriptionName,
                expectedOperation: TagEventOperation.Approving,
                expectedHandler: this.tagServiceMock.Object.OnApprovingTagAsync);

            VerifyLinkSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers.LinkOnAddingLinkSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers.LinkOnAddingLinkSubscriptionName,
                expectedOperation: LinkEventOperation.Adding,
                expectedHandler: this.linkServiceMock.Object.OnAddingLinkAsync);

            VerifyLinkSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers.LinkOnModifyingLinkSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers.LinkOnModifyingLinkSubscriptionName,
                expectedOperation: LinkEventOperation.Modifying,
                expectedHandler: this.linkServiceMock.Object.OnModifyingLinkAsync);

            VerifyLinkSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers.LinkOnRemovingLinkByIdSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers.LinkOnRemovingLinkByIdSubscriptionName,
                expectedOperation: LinkEventOperation.RemovingById,
                expectedHandler: this.linkServiceMock.Object.OnRemovingLinkByIdAsync);

            VerifyLinkSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.LinkOnHardRemovingLinkByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.LinkOnHardRemovingLinkByIdSubscriptionName,
                expectedOperation: LinkEventOperation.HardRemovingById,
                expectedHandler: this.linkServiceMock.Object.OnHardRemovingLinkByIdAsync);

            VerifyLinkSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.LinkOnRetrievingLinkByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.LinkOnRetrievingLinkByIdSubscriptionName,
                expectedOperation: LinkEventOperation.RetrievingById,
                expectedHandler: this.linkServiceMock.Object.OnRetrievingLinkByIdAsync);

            VerifyLinkSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.LinkOnSubmittingLinkSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.LinkOnSubmittingLinkSubscriptionName,
                expectedOperation: LinkEventOperation.Submitting,
                expectedHandler: this.linkServiceMock.Object.OnSubmittingLinkAsync);

            VerifyLinkSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.LinkOnApprovingLinkSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.LinkOnApprovingLinkSubscriptionName,
                expectedOperation: LinkEventOperation.Approving,
                expectedHandler: this.linkServiceMock.Object.OnApprovingLinkAsync);

            VerifyReactionSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers.ReactionOnAddingReactionSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers.ReactionOnAddingReactionSubscriptionName,
                expectedOperation: ReactionEventOperation.Adding,
                expectedHandler: this.reactionServiceMock.Object.OnAddingReactionAsync);

            VerifyReactionSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers.ReactionOnModifyingReactionSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers.ReactionOnModifyingReactionSubscriptionName,
                expectedOperation: ReactionEventOperation.Modifying,
                expectedHandler: this.reactionServiceMock.Object.OnModifyingReactionAsync);

            VerifyReactionSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers.ReactionOnRemovingReactionByIdSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers.ReactionOnRemovingReactionByIdSubscriptionName,
                expectedOperation: ReactionEventOperation.RemovingById,
                expectedHandler: this.reactionServiceMock.Object.OnRemovingReactionByIdAsync);

            VerifyReactionSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ReactionOnHardRemovingReactionByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ReactionOnHardRemovingReactionByIdSubscriptionName,
                expectedOperation: ReactionEventOperation.HardRemovingById,
                expectedHandler: this.reactionServiceMock.Object.OnHardRemovingReactionByIdAsync);

            VerifyReactionSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ReactionOnRetrievingReactionByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ReactionOnRetrievingReactionByIdSubscriptionName,
                expectedOperation: ReactionEventOperation.RetrievingById,
                expectedHandler: this.reactionServiceMock.Object.OnRetrievingReactionByIdAsync);

            VerifyReactionSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ReactionOnSubmittingReactionSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ReactionOnSubmittingReactionSubscriptionName,
                expectedOperation: ReactionEventOperation.Submitting,
                expectedHandler: this.reactionServiceMock.Object.OnSubmittingReactionAsync);

            VerifyReactionSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ReactionOnApprovingReactionSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ReactionOnApprovingReactionSubscriptionName,
                expectedOperation: ReactionEventOperation.Approving,
                expectedHandler: this.reactionServiceMock.Object.OnApprovingReactionAsync);

            VerifyCommentSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers.CommentOnAddingCommentSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers.CommentOnAddingCommentSubscriptionName,
                expectedOperation: CommentEventOperation.Adding,
                expectedHandler: this.commentServiceMock.Object.OnAddingCommentAsync);

            VerifyCommentSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers.CommentOnModifyingCommentSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers.CommentOnModifyingCommentSubscriptionName,
                expectedOperation: CommentEventOperation.Modifying,
                expectedHandler: this.commentServiceMock.Object.OnModifyingCommentAsync);

            VerifyCommentSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers.CommentOnRemovingCommentByIdSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers.CommentOnRemovingCommentByIdSubscriptionName,
                expectedOperation: CommentEventOperation.RemovingById,
                expectedHandler: this.commentServiceMock.Object.OnRemovingCommentByIdAsync);

            VerifyCommentSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.CommentOnHardRemovingCommentByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.CommentOnHardRemovingCommentByIdSubscriptionName,
                expectedOperation: CommentEventOperation.HardRemovingById,
                expectedHandler: this.commentServiceMock.Object.OnHardRemovingCommentByIdAsync);

            VerifyCommentSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.CommentOnRetrievingCommentByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.CommentOnRetrievingCommentByIdSubscriptionName,
                expectedOperation: CommentEventOperation.RetrievingById,
                expectedHandler: this.commentServiceMock.Object.OnRetrievingCommentByIdAsync);

            VerifyCommentSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.CommentOnSubmittingCommentSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.CommentOnSubmittingCommentSubscriptionName,
                expectedOperation: CommentEventOperation.Submitting,
                expectedHandler: this.commentServiceMock.Object.OnSubmittingCommentAsync);

            VerifyCommentSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.CommentOnApprovingCommentSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.CommentOnApprovingCommentSubscriptionName,
                expectedOperation: CommentEventOperation.Approving,
                expectedHandler: this.commentServiceMock.Object.OnApprovingCommentAsync);

            VerifyApprovalCommentSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalCommentOnAddingApprovalCommentSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalCommentOnAddingApprovalCommentSubscriptionName,
                expectedOperation: ApprovalCommentEventOperation.Adding,
                expectedHandler: this.approvalCommentServiceMock.Object.OnAddingApprovalCommentAsync);

            VerifyApprovalCommentSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalCommentOnModifyingApprovalCommentSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalCommentOnModifyingApprovalCommentSubscriptionName,
                expectedOperation: ApprovalCommentEventOperation.Modifying,
                expectedHandler: this.approvalCommentServiceMock.Object.OnModifyingApprovalCommentAsync);

            VerifyApprovalCommentSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalCommentOnRemovingApprovalCommentByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalCommentOnRemovingApprovalCommentByIdSubscriptionName,
                expectedOperation: ApprovalCommentEventOperation.RemovingById,
                expectedHandler: this.approvalCommentServiceMock.Object.OnRemovingApprovalCommentByIdAsync);

            VerifyApprovalCommentSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalCommentOnHardRemovingApprovalCommentByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalCommentOnHardRemovingApprovalCommentByIdSubscriptionName,
                expectedOperation: ApprovalCommentEventOperation.HardRemovingById,

                expectedHandler:
                    this.approvalCommentServiceMock.Object.OnHardRemovingApprovalCommentByIdAsync);

            VerifyApprovalCommentSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalCommentOnRetrievingApprovalCommentByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalCommentOnRetrievingApprovalCommentByIdSubscriptionName,
                expectedOperation: ApprovalCommentEventOperation.RetrievingById,

                expectedHandler:
                    this.approvalCommentServiceMock.Object.OnRetrievingApprovalCommentByIdAsync);

            VerifyApprovalCommentSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalCommentOnResolvingApprovalCommentSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalCommentOnResolvingApprovalCommentSubscriptionName,
                expectedOperation: ApprovalCommentEventOperation.Resolving,

                expectedHandler:
                    this.approvalCommentServiceMock.Object.OnResolvingApprovalCommentAsync);

            // The review flow's trigger: a recorded review may complete the round (§9.7.5).
            VerifyApprovalReviewSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers
                    .ApprovalOrchestrationOnApprovalReviewAddedSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers
                    .ApprovalOrchestrationOnApprovalReviewAddedSubscriptionName,
                expectedOperation: ApprovalReviewEventOperation.Added,
                expectedHandler: this.approvalOrchestrationServiceMock.Object
                    .OnApprovalReviewAddedAsync);

            // The other seven workflow-record fact addresses (§10.17(a)). Each can move a §8.5
            // predicate, so each has an ear.

            VerifyApprovalReviewSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers
                    .ApprovalOrchestrationOnApprovalReviewModifiedSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers
                    .ApprovalOrchestrationOnApprovalReviewModifiedSubscriptionName,
                expectedOperation: ApprovalReviewEventOperation.Modified,
                expectedHandler: this.approvalOrchestrationServiceMock.Object
                    .OnApprovalReviewModifiedAsync);

            VerifyApprovalReviewSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers
                    .ApprovalOrchestrationOnApprovalReviewRemovedSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers
                    .ApprovalOrchestrationOnApprovalReviewRemovedSubscriptionName,
                expectedOperation: ApprovalReviewEventOperation.Removed,
                expectedHandler: this.approvalOrchestrationServiceMock.Object
                    .OnApprovalReviewRemovedAsync);

            VerifyApprovalReviewSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers
                    .ApprovalOrchestrationOnApprovalReviewDismissedSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers
                    .ApprovalOrchestrationOnApprovalReviewDismissedSubscriptionName,
                expectedOperation: ApprovalReviewEventOperation.Dismissed,
                expectedHandler: this.approvalOrchestrationServiceMock.Object
                    .OnApprovalReviewDismissedAsync);

            VerifyApprovalCommentSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers
                    .ApprovalOrchestrationOnApprovalCommentAddedSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers
                    .ApprovalOrchestrationOnApprovalCommentAddedSubscriptionName,
                expectedOperation: ApprovalCommentEventOperation.Added,
                expectedHandler: this.approvalOrchestrationServiceMock.Object
                    .OnApprovalCommentAddedAsync);

            VerifyApprovalCommentSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers
                    .ApprovalOrchestrationOnApprovalCommentModifiedSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers
                    .ApprovalOrchestrationOnApprovalCommentModifiedSubscriptionName,
                expectedOperation: ApprovalCommentEventOperation.Modified,
                expectedHandler: this.approvalOrchestrationServiceMock.Object
                    .OnApprovalCommentModifiedAsync);

            VerifyApprovalCommentSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers
                    .ApprovalOrchestrationOnApprovalCommentResolvedSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers
                    .ApprovalOrchestrationOnApprovalCommentResolvedSubscriptionName,
                expectedOperation: ApprovalCommentEventOperation.Resolved,
                expectedHandler: this.approvalOrchestrationServiceMock.Object
                    .OnApprovalCommentResolvedAsync);

            VerifyApprovalCommentSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers
                    .ApprovalOrchestrationOnApprovalCommentRemovedSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers
                    .ApprovalOrchestrationOnApprovalCommentRemovedSubscriptionName,
                expectedOperation: ApprovalCommentEventOperation.Removed,
                expectedHandler: this.approvalOrchestrationServiceMock.Object
                    .OnApprovalCommentRemovedAsync);

            VerifyApprovalReviewSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalReviewOnAddingApprovalReviewSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalReviewOnAddingApprovalReviewSubscriptionName,
                expectedOperation: ApprovalReviewEventOperation.Adding,
                expectedHandler: this.approvalReviewServiceMock.Object.OnAddingApprovalReviewAsync);

            VerifyApprovalReviewSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalReviewOnModifyingApprovalReviewSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalReviewOnModifyingApprovalReviewSubscriptionName,
                expectedOperation: ApprovalReviewEventOperation.Modifying,
                expectedHandler: this.approvalReviewServiceMock.Object.OnModifyingApprovalReviewAsync);

            VerifyApprovalReviewSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalReviewOnRemovingApprovalReviewByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalReviewOnRemovingApprovalReviewByIdSubscriptionName,
                expectedOperation: ApprovalReviewEventOperation.RemovingById,
                expectedHandler: this.approvalReviewServiceMock.Object.OnRemovingApprovalReviewByIdAsync);

            VerifyApprovalReviewSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalReviewOnHardRemovingApprovalReviewByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalReviewOnHardRemovingApprovalReviewByIdSubscriptionName,
                expectedOperation: ApprovalReviewEventOperation.HardRemovingById,
                expectedHandler: this.approvalReviewServiceMock.Object.OnHardRemovingApprovalReviewByIdAsync);

            VerifyApprovalReviewSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalReviewOnRetrievingApprovalReviewByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalReviewOnRetrievingApprovalReviewByIdSubscriptionName,
                expectedOperation: ApprovalReviewEventOperation.RetrievingById,
                expectedHandler: this.approvalReviewServiceMock.Object.OnRetrievingApprovalReviewByIdAsync);

            VerifyApprovalReviewSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalReviewOnDismissingApprovalReviewSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalReviewOnDismissingApprovalReviewSubscriptionName,
                expectedOperation: ApprovalReviewEventOperation.Dismissing,
                expectedHandler: this.approvalReviewServiceMock.Object.OnDismissingApprovalReviewAsync);

            VerifyApprovalSettingSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalSettingOnAddingApprovalSettingSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalSettingOnAddingApprovalSettingSubscriptionName,
                expectedOperation: ApprovalSettingEventOperation.Adding,
                expectedHandler: this.approvalSettingServiceMock.Object.OnAddingApprovalSettingAsync);

            VerifyApprovalSettingSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalSettingOnModifyingApprovalSettingSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalSettingOnModifyingApprovalSettingSubscriptionName,
                expectedOperation: ApprovalSettingEventOperation.Modifying,
                expectedHandler: this.approvalSettingServiceMock.Object.OnModifyingApprovalSettingAsync);

            VerifyApprovalSettingSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalSettingOnRemovingApprovalSettingByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalSettingOnRemovingApprovalSettingByIdSubscriptionName,
                expectedOperation: ApprovalSettingEventOperation.RemovingById,
                expectedHandler: this.approvalSettingServiceMock.Object.OnRemovingApprovalSettingByIdAsync);

            VerifyApprovalSettingSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalSettingOnHardRemovingApprovalSettingByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalSettingOnHardRemovingApprovalSettingByIdSubscriptionName,
                expectedOperation: ApprovalSettingEventOperation.HardRemovingById,
                expectedHandler:
                    this.approvalSettingServiceMock.Object.OnHardRemovingApprovalSettingByIdAsync);

            VerifyApprovalSettingSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalSettingOnRetrievingApprovalSettingByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalSettingOnRetrievingApprovalSettingByIdSubscriptionName,
                expectedOperation: ApprovalSettingEventOperation.RetrievingById,
                expectedHandler:
                    this.approvalSettingServiceMock.Object.OnRetrievingApprovalSettingByIdAsync);

            VerifyAssociationSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers
                    .AssociationOnAddingAssociationSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers
                    .AssociationOnAddingAssociationSubscriptionName,
                expectedOperation: AssociationEventOperation.Adding,
                expectedHandler: this.associationServiceMock.Object
                    .OnAddingAssociationAsync);

            VerifyAssociationSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers
                    .AssociationOnModifyingAssociationSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers
                    .AssociationOnModifyingAssociationSubscriptionName,
                expectedOperation: AssociationEventOperation.Modifying,
                expectedHandler: this.associationServiceMock.Object
                    .OnModifyingAssociationAsync);

            VerifyAssociationSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers
                    .AssociationOnRemovingAssociationByIdSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers
                    .AssociationOnRemovingAssociationByIdSubscriptionName,
                expectedOperation: AssociationEventOperation.RemovingById,
                expectedHandler: this.associationServiceMock.Object
                    .OnRemovingAssociationByIdAsync);

            VerifyAssociationSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers
                    .AssociationOnHardRemovingAssociationByIdSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers
                    .AssociationOnHardRemovingAssociationByIdSubscriptionName,
                expectedOperation: AssociationEventOperation.HardRemovingById,
                expectedHandler: this.associationServiceMock.Object
                    .OnHardRemovingAssociationByIdAsync);

            VerifyAssociationSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers
                    .AssociationOnRetrievingAssociationByIdSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers
                    .AssociationOnRetrievingAssociationByIdSubscriptionName,
                expectedOperation: AssociationEventOperation.RetrievingById,
                expectedHandler: this.associationServiceMock.Object
                    .OnRetrievingAssociationByIdAsync);

            VerifyAssociationSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers
                    .AssociationOnApprovingAssociationSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers
                    .AssociationOnApprovingAssociationSubscriptionName,
                expectedOperation: AssociationEventOperation.Approving,
                expectedHandler: this.associationServiceMock.Object
                    .OnApprovingAssociationAsync);

            // A bypass no longer has a request address of its own: it folded into the widened
            // approval transition and is asked for by setting the bypass pair on the payload of
            // Association-Approving. It still has no fact address — the outcome goes out on
            // Association-Approved, because a bypass approval IS an approval to every subscriber.
            VerifyAssociationSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers
                    .AssociationOnSettingAssociationConfidenceSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers
                    .AssociationOnSettingAssociationConfidenceSubscriptionName,
                expectedOperation: AssociationEventOperation.SettingConfidence,
                expectedHandler: this.associationServiceMock.Object
                    .OnSettingAssociationConfidenceAsync);

            VerifyAssociationSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers
                    .AssociationOnSettingAssociationScopeSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers
                    .AssociationOnSettingAssociationScopeSubscriptionName,
                expectedOperation: AssociationEventOperation.SettingScope,
                expectedHandler: this.associationServiceMock.Object
                    .OnSettingAssociationScopeAsync);

            VerifyContentItemSettingSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ContentItemSettingOnAddingContentItemSettingSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ContentItemSettingOnAddingContentItemSettingSubscriptionName,
                expectedOperation: ContentItemSettingEventOperation.Adding,
                expectedHandler:
                    this.contentItemSettingServiceMock.Object.OnAddingContentItemSettingAsync);

            VerifyContentItemSettingSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ContentItemSettingOnModifyingContentItemSettingSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ContentItemSettingOnModifyingContentItemSettingSubscriptionName,
                expectedOperation: ContentItemSettingEventOperation.Modifying,
                expectedHandler:
                    this.contentItemSettingServiceMock.Object.OnModifyingContentItemSettingAsync);

            VerifyContentItemSettingSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ContentItemSettingOnRemovingContentItemSettingByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ContentItemSettingOnRemovingContentItemSettingByIdSubscriptionName,
                expectedOperation: ContentItemSettingEventOperation.RemovingById,
                expectedHandler:
                    this.contentItemSettingServiceMock.Object.OnRemovingContentItemSettingByIdAsync);

            VerifyContentItemSettingSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ContentItemSettingOnHardRemovingContentItemSettingByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ContentItemSettingOnHardRemovingContentItemSettingByIdSubscriptionName,
                expectedOperation: ContentItemSettingEventOperation.HardRemovingById,
                expectedHandler:
                    this.contentItemSettingServiceMock.Object.OnHardRemovingContentItemSettingByIdAsync);

            VerifyContentItemSettingSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ContentItemSettingOnRetrievingContentItemSettingByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ContentItemSettingOnRetrievingContentItemSettingByIdSubscriptionName,
                expectedOperation: ContentItemSettingEventOperation.RetrievingById,
                expectedHandler:
                    this.contentItemSettingServiceMock.Object.OnRetrievingContentItemSettingByIdAsync);

            // The publication swap: versioned entities are approved through the processing
            // service so the group's published slot is cleared before the promote (§9.7.7 r7).
            VerifyContentItemProcessingSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ContentItemProcessingOnApprovingContentItemSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ContentItemProcessingOnApprovingContentItemSubscriptionName,
                expectedOperation: ContentItemProcessingEventOperation.Approving,
                expectedHandler:
                    this.contentItemProcessingServiceMock.Object.OnApprovingContentItemAsync);

            VerifyContentItemProcessingSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ContentItemProcessingOnAddingContentItemSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ContentItemProcessingOnAddingContentItemSubscriptionName,
                expectedOperation: ContentItemProcessingEventOperation.Adding,
                expectedHandler:
                    this.contentItemProcessingServiceMock.Object.OnAddingContentItemAsync);

            VerifyContentItemProcessingSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ContentItemProcessingOnModifyingContentItemSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ContentItemProcessingOnModifyingContentItemSubscriptionName,
                expectedOperation: ContentItemProcessingEventOperation.Modifying,
                expectedHandler:
                    this.contentItemProcessingServiceMock.Object.OnModifyingContentItemAsync);

            VerifyContentItemProcessingSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ContentItemProcessingOnRemovingContentItemByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ContentItemProcessingOnRemovingContentItemByIdSubscriptionName,
                expectedOperation: ContentItemProcessingEventOperation.RemovingById,
                expectedHandler:
                    this.contentItemProcessingServiceMock.Object.OnRemovingContentItemByIdAsync);

            VerifyContentItemProcessingSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ContentItemProcessingOnRetrievingContentItemByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ContentItemProcessingOnRetrievingContentItemByIdSubscriptionName,
                expectedOperation: ContentItemProcessingEventOperation.RetrievingById,
                expectedHandler:
                    this.contentItemProcessingServiceMock.Object.OnRetrievingContentItemByIdAsync);

            // The publication swap: versioned entities are approved through the processing
            // service so the group's published slot is cleared before the promote (§9.7.7 r7).
            VerifyLinkProcessingSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.LinkProcessingOnApprovingLinkSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.LinkProcessingOnApprovingLinkSubscriptionName,
                expectedOperation: LinkProcessingEventOperation.Approving,
                expectedHandler:
                    this.linkProcessingServiceMock.Object.OnApprovingLinkAsync);

            VerifyLinkProcessingSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.LinkProcessingOnAddingLinkSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.LinkProcessingOnAddingLinkSubscriptionName,
                expectedOperation: LinkProcessingEventOperation.Adding,
                expectedHandler:
                    this.linkProcessingServiceMock.Object.OnAddingLinkAsync);

            VerifyLinkProcessingSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.LinkProcessingOnModifyingLinkSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.LinkProcessingOnModifyingLinkSubscriptionName,
                expectedOperation: LinkProcessingEventOperation.Modifying,
                expectedHandler:
                    this.linkProcessingServiceMock.Object.OnModifyingLinkAsync);

            VerifyLinkProcessingSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.LinkProcessingOnRemovingLinkByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.LinkProcessingOnRemovingLinkByIdSubscriptionName,
                expectedOperation: LinkProcessingEventOperation.RemovingById,
                expectedHandler:
                    this.linkProcessingServiceMock.Object.OnRemovingLinkByIdAsync);

            VerifyLinkProcessingSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.LinkProcessingOnRetrievingLinkByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.LinkProcessingOnRetrievingLinkByIdSubscriptionName,
                expectedOperation: LinkProcessingEventOperation.RetrievingById,
                expectedHandler:
                    this.linkProcessingServiceMock.Object.OnRetrievingLinkByIdAsync);

            // The approval workflow listens on each entity's TOP-LAYER fact (§10.17 rule 1):
            // the processing address for the two entities that have a processing service, the
            // foundation address for the five that do not. Asserting the address these bind to
            // is the point of these six — a ContentItem or Link bound to the foundation would
            // fire twice per version fork, and the other five bound anywhere else never fire.
            VerifyContentItemProcessingSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalOrchestrationOnContentItemAddedSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalOrchestrationOnContentItemAddedSubscriptionName,
                expectedOperation: ContentItemProcessingEventOperation.Added,
                expectedHandler:
                    this.approvalOrchestrationServiceMock.Object.OnContentItemAddedAsync);

            VerifyContentItemProcessingSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalOrchestrationOnContentItemModifiedSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalOrchestrationOnContentItemModifiedSubscriptionName,
                expectedOperation: ContentItemProcessingEventOperation.Modified,
                expectedHandler:
                    this.approvalOrchestrationServiceMock.Object.OnContentItemModifiedAsync);

            VerifyLinkProcessingSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalOrchestrationOnLinkAddedSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalOrchestrationOnLinkAddedSubscriptionName,
                expectedOperation: LinkProcessingEventOperation.Added,
                expectedHandler:
                    this.approvalOrchestrationServiceMock.Object.OnLinkAddedAsync);

            VerifyLinkProcessingSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalOrchestrationOnLinkModifiedSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalOrchestrationOnLinkModifiedSubscriptionName,
                expectedOperation: LinkProcessingEventOperation.Modified,
                expectedHandler:
                    this.approvalOrchestrationServiceMock.Object.OnLinkModifiedAsync);

            VerifyTagSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalOrchestrationOnTagAddedSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalOrchestrationOnTagAddedSubscriptionName,
                expectedOperation: TagEventOperation.Added,
                expectedHandler: this.approvalOrchestrationServiceMock.Object.OnTagAddedAsync);

            VerifyTagSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalOrchestrationOnTagModifiedSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalOrchestrationOnTagModifiedSubscriptionName,
                expectedOperation: TagEventOperation.Modified,
                expectedHandler: this.approvalOrchestrationServiceMock.Object.OnTagModifiedAsync);

            VerifyCommentSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalOrchestrationOnCommentAddedSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalOrchestrationOnCommentAddedSubscriptionName,
                expectedOperation: CommentEventOperation.Added,
                expectedHandler: this.approvalOrchestrationServiceMock.Object.OnCommentAddedAsync);

            VerifyCommentSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalOrchestrationOnCommentModifiedSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalOrchestrationOnCommentModifiedSubscriptionName,
                expectedOperation: CommentEventOperation.Modified,
                expectedHandler:
                    this.approvalOrchestrationServiceMock.Object.OnCommentModifiedAsync);

            VerifyReactionSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalOrchestrationOnReactionAddedSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalOrchestrationOnReactionAddedSubscriptionName,
                expectedOperation: ReactionEventOperation.Added,
                expectedHandler:
                    this.approvalOrchestrationServiceMock.Object.OnReactionAddedAsync);

            VerifyReactionSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalOrchestrationOnReactionModifiedSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalOrchestrationOnReactionModifiedSubscriptionName,
                expectedOperation: ReactionEventOperation.Modified,
                expectedHandler:
                    this.approvalOrchestrationServiceMock.Object.OnReactionModifiedAsync);

            VerifyBibleReferenceSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalOrchestrationOnBibleReferenceAddedSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalOrchestrationOnBibleReferenceAddedSubscriptionName,
                expectedOperation: BibleReferenceEventOperation.Added,
                expectedHandler:
                    this.approvalOrchestrationServiceMock.Object.OnBibleReferenceAddedAsync);

            VerifyBibleReferenceSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalOrchestrationOnBibleReferenceModifiedSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers
                        .ApprovalOrchestrationOnBibleReferenceModifiedSubscriptionName,
                expectedOperation: BibleReferenceEventOperation.Modified,
                expectedHandler:
                    this.approvalOrchestrationServiceMock.Object.OnBibleReferenceModifiedAsync);

            VerifyAssociationSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalOrchestrationOnAssociationAddedSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalOrchestrationOnAssociationAddedSubscriptionName,
                expectedOperation: AssociationEventOperation.Added,
                expectedHandler:
                    this.approvalOrchestrationServiceMock.Object.OnAssociationAddedAsync);

            VerifyAssociationSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalOrchestrationOnAssociationModifiedSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalOrchestrationOnAssociationModifiedSubscriptionName,
                expectedOperation: AssociationEventOperation.Modified,
                expectedHandler:
                    this.approvalOrchestrationServiceMock.Object.OnAssociationModifiedAsync);

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.approvalServiceMock.VerifyNoOtherCalls();
            this.contentItemProcessingServiceMock.VerifyNoOtherCalls();
            this.linkProcessingServiceMock.VerifyNoOtherCalls();
            this.approvalOrchestrationServiceMock.VerifyNoOtherCalls();
        }

        // The registration binds a scope-opening lambda, not a method group, so a delegate
        // IDENTITY check can no longer work — and identity was never the property worth
        // asserting anyway. What matters is that invoking whatever was bound reaches the right
        // method on the right service, resolved out of a scope. So the captured delegate is
        // invoked and the expectation is invoked beside it, and the two are compared by which
        // service method each one drove.
        private bool MatchesHandler<TEntity>(
            Func<EventEnvelope<TEntity>, CancellationToken, ValueTask<EventEnvelope<TEntity>?>> actual,
            Func<EventEnvelope<TEntity>, CancellationToken, ValueTask<EventEnvelope<TEntity>?>> expected)
        {
            var envelope = new EventEnvelope<TEntity>();

            string Drove(
                Func<EventEnvelope<TEntity>, CancellationToken, ValueTask<EventEnvelope<TEntity>?>> handler)
            {
                foreach (Mock mock in AllServiceMocks())
                {
                    mock.Invocations.Clear();
                }

                try
                {
                    handler(envelope, CancellationToken.None).AsTask().GetAwaiter().GetResult();
                }
                catch
                {
                    // A mocked handler may throw on a bare envelope; the invocation is still
                    // recorded, which is the only thing read here.
                }

                foreach (Mock mock in AllServiceMocks())
                {
                    foreach (IInvocation invocation in mock.Invocations)
                    {
                        return $"{mock.Object.GetType().Name}.{invocation.Method.Name}";
                    }
                }

                return "(nothing)";
            }

            string actualDrove = Drove(actual);
            string expectedDrove = Drove(expected);

            // Probing invoked the handlers, which recorded invocations on the mocks. Cleared
            // again so the comparison leaves no trace — the test asserts VerifyNoOtherCalls at
            // the end, and a probe's own footprints would read as a stray subscription.
            foreach (Mock mock in AllServiceMocks())
            {
                mock.Invocations.Clear();
            }

            return actualDrove != "(nothing)" && actualDrove == expectedDrove;
        }

        private void VerifySubscription<TEntity>(
            string expectedSubscriptionName,
            Func<EventEnvelope<TEntity>, CancellationToken, ValueTask<EventEnvelope<TEntity>?>> expectedHandler,
            Action<Func<Func<EventEnvelope<TEntity>, CancellationToken, ValueTask<EventEnvelope<TEntity>?>>, bool>> capture) =>
            capture(actual => MatchesHandler(actual, expectedHandler));

        private IEnumerable<Mock> AllServiceMocks() =>
            new Mock[]
            {
                this.contentItemServiceMock, this.approvalServiceMock,
                this.bibleReferenceServiceMock, this.tagServiceMock, this.linkServiceMock,
                this.reactionServiceMock, this.commentServiceMock,
                this.approvalCommentServiceMock, this.approvalReviewServiceMock,
                this.approvalSettingServiceMock, this.associationServiceMock,
                this.contentItemSettingServiceMock, this.contentItemProcessingServiceMock,
                this.linkProcessingServiceMock, this.approvalOrchestrationServiceMock
            };
    }
}
