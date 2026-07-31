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
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Glory2Him.Core.Registrations;
using Glory2Him.Core.Services.Foundations.Approvals;
using Glory2Him.Core.Services.Foundations.ContentItems;
using Glory2Him.Core.Services.Foundations.ContentTypes;
using Moq;
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
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles;
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles;
using Glory2Him.Core.Services.Foundations.ApprovalSettingReviewerRoles;
using Glory2Him.Core.Services.Foundations.ApprovalSettingPublisherRoles;
using Glory2Him.Core.Models.Foundations.ContentItemAssociations;
using Glory2Him.Core.Services.Foundations.ContentItemAssociations;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Services.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Events.Orchestrations;
using Glory2Him.Core.Services.Orchestrations.ContentItems;

namespace Glory2Him.Core.Tests.Unit.Registrations
{
    public partial class EventSubscriptionRegistrationTests
    {
        private readonly Mock<IEventBroker> eventBrokerMock;
        private readonly Mock<IContentTypeService> contentTypeServiceMock;
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
        private readonly Mock<IApprovalSettingReviewerRoleService> approvalSettingReviewerRoleServiceMock;
        private readonly Mock<IApprovalSettingPublisherRoleService> approvalSettingPublisherRoleServiceMock;
        private readonly Mock<IContentItemAssociationService> contentItemAssociationServiceMock;
        private readonly Mock<IContentItemSettingService> contentItemSettingServiceMock;
        private readonly Mock<IContentItemOrchestrationService> contentItemOrchestrationServiceMock;
        private readonly IEventSubscriptionRegistration eventSubscriptionRegistration;

        public EventSubscriptionRegistrationTests()
        {
            this.eventBrokerMock = new Mock<IEventBroker>();
            this.contentTypeServiceMock = new Mock<IContentTypeService>();
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
            this.approvalSettingReviewerRoleServiceMock = new Mock<IApprovalSettingReviewerRoleService>();
            this.approvalSettingPublisherRoleServiceMock = new Mock<IApprovalSettingPublisherRoleService>();
            this.contentItemAssociationServiceMock = new Mock<IContentItemAssociationService>();
            this.contentItemSettingServiceMock = new Mock<IContentItemSettingService>();
            this.contentItemOrchestrationServiceMock = new Mock<IContentItemOrchestrationService>();

            this.eventSubscriptionRegistration = new EventSubscriptionRegistration(
                eventBroker: this.eventBrokerMock.Object,
                contentTypeService: this.contentTypeServiceMock.Object,
                contentItemService: this.contentItemServiceMock.Object,
                approvalService: this.approvalServiceMock.Object,
                bibleReferenceService: this.bibleReferenceServiceMock.Object,
                tagService: this.tagServiceMock.Object,
                linkService: this.linkServiceMock.Object,
                reactionService: this.reactionServiceMock.Object,
                commentService: this.commentServiceMock.Object,
                approvalCommentService: this.approvalCommentServiceMock.Object,
                approvalReviewService: this.approvalReviewServiceMock.Object,
                approvalSettingService: this.approvalSettingServiceMock.Object,
                approvalSettingReviewerRoleService: this.approvalSettingReviewerRoleServiceMock.Object,
                approvalSettingPublisherRoleService: this.approvalSettingPublisherRoleServiceMock.Object,
                contentItemAssociationService: this.contentItemAssociationServiceMock.Object,
                contentItemSettingService: this.contentItemSettingServiceMock.Object,
                contentItemOrchestrationService: this.contentItemOrchestrationServiceMock.Object);
        }

        private void VerifyContentItemSubmissionSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            ContentItemSubmissionEventOperation expectedOperation,
            Func<EventEnvelope<ContentItem>, CancellationToken,
                ValueTask<EventEnvelope<ContentItem>?>> expectedHandler)
        {
            this.eventBrokerMock.Verify(broker =>
                broker.SubscribeToContentItemSubmissionEventAsync(
                    It.Is<EventSubscription>(subscription =>
                        subscription.Id == expectedSubscriptionId
                            && subscription.Name == expectedSubscriptionName),
                    expectedOperation,
                    It.Is<Func<EventEnvelope<ContentItem>, CancellationToken,
                        ValueTask<EventEnvelope<ContentItem>?>>>(handler =>
                            handler.Equals(expectedHandler)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private void VerifyContentTypeSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            ContentTypeEventOperation expectedOperation,
            Func<EventEnvelope<ContentType>, CancellationToken,
                ValueTask<EventEnvelope<ContentType>?>> expectedHandler)
        {
            this.eventBrokerMock.Verify(broker =>
                broker.SubscribeToContentTypeEventAsync(
                    It.Is<EventSubscription>(subscription =>
                        subscription.Id == expectedSubscriptionId
                            && subscription.Name == expectedSubscriptionName),
                    expectedOperation,
                    It.Is<Func<EventEnvelope<ContentType>, CancellationToken,
                        ValueTask<EventEnvelope<ContentType>?>>>(handler =>
                            handler.Equals(expectedHandler)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private void VerifyContentItemSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            ContentItemEventOperation expectedOperation,
            Func<EventEnvelope<ContentItem>, CancellationToken,
                ValueTask<EventEnvelope<ContentItem>?>> expectedHandler)
        {
            this.eventBrokerMock.Verify(broker =>
                broker.SubscribeToContentItemEventAsync(
                    It.Is<EventSubscription>(subscription =>
                        subscription.Id == expectedSubscriptionId
                            && subscription.Name == expectedSubscriptionName),
                    expectedOperation,
                    It.Is<Func<EventEnvelope<ContentItem>, CancellationToken,
                        ValueTask<EventEnvelope<ContentItem>?>>>(handler =>
                            handler.Equals(expectedHandler)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private void VerifyApprovalSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            ApprovalEventOperation expectedOperation,
            Func<EventEnvelope<Approval>, CancellationToken,
                ValueTask<EventEnvelope<Approval>?>> expectedHandler)
        {
            this.eventBrokerMock.Verify(broker =>
                broker.SubscribeToApprovalEventAsync(
                    It.Is<EventSubscription>(subscription =>
                        subscription.Id == expectedSubscriptionId
                            && subscription.Name == expectedSubscriptionName),
                    expectedOperation,
                    It.Is<Func<EventEnvelope<Approval>, CancellationToken,
                        ValueTask<EventEnvelope<Approval>?>>>(handler =>
                            handler.Equals(expectedHandler)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private void VerifyBibleReferenceSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            BibleReferenceEventOperation expectedOperation,
            Func<EventEnvelope<BibleReference>, CancellationToken,
                ValueTask<EventEnvelope<BibleReference>?>> expectedHandler)
        {
            this.eventBrokerMock.Verify(broker =>
                broker.SubscribeToBibleReferenceEventAsync(
                    It.Is<EventSubscription>(subscription =>
                        subscription.Id == expectedSubscriptionId
                            && subscription.Name == expectedSubscriptionName),
                    expectedOperation,
                    It.Is<Func<EventEnvelope<BibleReference>, CancellationToken,
                        ValueTask<EventEnvelope<BibleReference>?>>>(handler =>
                            handler.Equals(expectedHandler)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private void VerifyTagSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            TagEventOperation expectedOperation,
            Func<EventEnvelope<Tag>, CancellationToken,
                ValueTask<EventEnvelope<Tag>?>> expectedHandler)
        {
            this.eventBrokerMock.Verify(broker =>
                broker.SubscribeToTagEventAsync(
                    It.Is<EventSubscription>(subscription =>
                        subscription.Id == expectedSubscriptionId
                            && subscription.Name == expectedSubscriptionName),
                    expectedOperation,
                    It.Is<Func<EventEnvelope<Tag>, CancellationToken,
                        ValueTask<EventEnvelope<Tag>?>>>(handler =>
                            handler.Equals(expectedHandler)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private void VerifyLinkSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            LinkEventOperation expectedOperation,
            Func<EventEnvelope<Link>, CancellationToken,
                ValueTask<EventEnvelope<Link>?>> expectedHandler)
        {
            this.eventBrokerMock.Verify(broker =>
                broker.SubscribeToLinkEventAsync(
                    It.Is<EventSubscription>(subscription =>
                        subscription.Id == expectedSubscriptionId
                            && subscription.Name == expectedSubscriptionName),
                    expectedOperation,
                    It.Is<Func<EventEnvelope<Link>, CancellationToken,
                        ValueTask<EventEnvelope<Link>?>>>(handler =>
                            handler.Equals(expectedHandler)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private void VerifyReactionSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            ReactionEventOperation expectedOperation,
            Func<EventEnvelope<Reaction>, CancellationToken,
                ValueTask<EventEnvelope<Reaction>?>> expectedHandler)
        {
            this.eventBrokerMock.Verify(broker =>
                broker.SubscribeToReactionEventAsync(
                    It.Is<EventSubscription>(subscription =>
                        subscription.Id == expectedSubscriptionId
                            && subscription.Name == expectedSubscriptionName),
                    expectedOperation,
                    It.Is<Func<EventEnvelope<Reaction>, CancellationToken,
                        ValueTask<EventEnvelope<Reaction>?>>>(handler =>
                            handler.Equals(expectedHandler)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private void VerifyCommentSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            CommentEventOperation expectedOperation,
            Func<EventEnvelope<Comment>, CancellationToken,
                ValueTask<EventEnvelope<Comment>?>> expectedHandler)
        {
            this.eventBrokerMock.Verify(broker =>
                broker.SubscribeToCommentEventAsync(
                    It.Is<EventSubscription>(subscription =>
                        subscription.Id == expectedSubscriptionId
                            && subscription.Name == expectedSubscriptionName),
                    expectedOperation,
                    It.Is<Func<EventEnvelope<Comment>, CancellationToken,
                        ValueTask<EventEnvelope<Comment>?>>>(handler =>
                            handler.Equals(expectedHandler)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private void VerifyApprovalCommentSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            ApprovalCommentEventOperation expectedOperation,
            Func<EventEnvelope<ApprovalComment>, CancellationToken,
                ValueTask<EventEnvelope<ApprovalComment>?>> expectedHandler)
        {
            this.eventBrokerMock.Verify(broker =>
                broker.SubscribeToApprovalCommentEventAsync(
                    It.Is<EventSubscription>(subscription =>
                        subscription.Id == expectedSubscriptionId
                            && subscription.Name == expectedSubscriptionName),
                    expectedOperation,
                    It.Is<Func<EventEnvelope<ApprovalComment>, CancellationToken,
                        ValueTask<EventEnvelope<ApprovalComment>?>>>(handler =>
                            handler.Equals(expectedHandler)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private void VerifyApprovalReviewSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            ApprovalReviewEventOperation expectedOperation,
            Func<EventEnvelope<ApprovalReview>, CancellationToken,
                ValueTask<EventEnvelope<ApprovalReview>?>> expectedHandler)
        {
            this.eventBrokerMock.Verify(broker =>
                broker.SubscribeToApprovalReviewEventAsync(
                    It.Is<EventSubscription>(subscription =>
                        subscription.Id == expectedSubscriptionId
                            && subscription.Name == expectedSubscriptionName),
                    expectedOperation,
                    It.Is<Func<EventEnvelope<ApprovalReview>, CancellationToken,
                        ValueTask<EventEnvelope<ApprovalReview>?>>>(handler =>
                            handler.Equals(expectedHandler)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private void VerifyApprovalSettingSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            ApprovalSettingEventOperation expectedOperation,
            Func<EventEnvelope<ApprovalSetting>, CancellationToken,
                ValueTask<EventEnvelope<ApprovalSetting>?>> expectedHandler)
        {
            this.eventBrokerMock.Verify(broker =>
                broker.SubscribeToApprovalSettingEventAsync(
                    It.Is<EventSubscription>(subscription =>
                        subscription.Id == expectedSubscriptionId
                            && subscription.Name == expectedSubscriptionName),
                    expectedOperation,
                    It.Is<Func<EventEnvelope<ApprovalSetting>, CancellationToken,
                        ValueTask<EventEnvelope<ApprovalSetting>?>>>(handler =>
                            handler.Equals(expectedHandler)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private void VerifyApprovalSettingReviewerRoleSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            ApprovalSettingReviewerRoleEventOperation expectedOperation,
            Func<EventEnvelope<ApprovalSettingReviewerRole>, CancellationToken,
                ValueTask<EventEnvelope<ApprovalSettingReviewerRole>?>> expectedHandler)
        {
            this.eventBrokerMock.Verify(broker =>
                broker.SubscribeToApprovalSettingReviewerRoleEventAsync(
                    It.Is<EventSubscription>(subscription =>
                        subscription.Id == expectedSubscriptionId
                            && subscription.Name == expectedSubscriptionName),
                    expectedOperation,
                    It.Is<Func<EventEnvelope<ApprovalSettingReviewerRole>, CancellationToken,
                        ValueTask<EventEnvelope<ApprovalSettingReviewerRole>?>>>(handler =>
                            handler.Equals(expectedHandler)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private void VerifyApprovalSettingPublisherRoleSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            ApprovalSettingPublisherRoleEventOperation expectedOperation,
            Func<EventEnvelope<ApprovalSettingPublisherRole>, CancellationToken,
                ValueTask<EventEnvelope<ApprovalSettingPublisherRole>?>> expectedHandler)
        {
            this.eventBrokerMock.Verify(broker =>
                broker.SubscribeToApprovalSettingPublisherRoleEventAsync(
                    It.Is<EventSubscription>(subscription =>
                        subscription.Id == expectedSubscriptionId
                            && subscription.Name == expectedSubscriptionName),
                    expectedOperation,
                    It.Is<Func<EventEnvelope<ApprovalSettingPublisherRole>, CancellationToken,
                        ValueTask<EventEnvelope<ApprovalSettingPublisherRole>?>>>(handler =>
                            handler.Equals(expectedHandler)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private void VerifyContentItemAssociationSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            ContentItemAssociationEventOperation expectedOperation,
            Func<EventEnvelope<ContentItemAssociation>, CancellationToken,
                ValueTask<EventEnvelope<ContentItemAssociation>?>> expectedHandler)
        {
            this.eventBrokerMock.Verify(broker =>
                broker.SubscribeToContentItemAssociationEventAsync(
                    It.Is<EventSubscription>(subscription =>
                        subscription.Id == expectedSubscriptionId
                            && subscription.Name == expectedSubscriptionName),
                    expectedOperation,
                    It.Is<Func<EventEnvelope<ContentItemAssociation>, CancellationToken,
                        ValueTask<EventEnvelope<ContentItemAssociation>?>>>(handler =>
                            handler.Equals(expectedHandler)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private void VerifyContentItemSettingSubscription(
            Guid expectedSubscriptionId,
            string expectedSubscriptionName,
            ContentItemSettingEventOperation expectedOperation,
            Func<EventEnvelope<ContentItemSetting>, CancellationToken,
                ValueTask<EventEnvelope<ContentItemSetting>?>> expectedHandler)
        {
            this.eventBrokerMock.Verify(broker =>
                broker.SubscribeToContentItemSettingEventAsync(
                    It.Is<EventSubscription>(subscription =>
                        subscription.Id == expectedSubscriptionId
                            && subscription.Name == expectedSubscriptionName),
                    expectedOperation,
                    It.Is<Func<EventEnvelope<ContentItemSetting>, CancellationToken,
                        ValueTask<EventEnvelope<ContentItemSetting>?>>>(handler =>
                            handler.Equals(expectedHandler)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
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

            VerifyContentTypeSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers.ContentTypeOnAddingContentTypeSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers.ContentTypeOnAddingContentTypeSubscriptionName,
                expectedOperation: ContentTypeEventOperation.Adding,
                expectedHandler: this.contentTypeServiceMock.Object.OnAddingContentTypeAsync);

            VerifyContentTypeSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers.ContentTypeOnModifyingContentTypeSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers.ContentTypeOnModifyingContentTypeSubscriptionName,
                expectedOperation: ContentTypeEventOperation.Modifying,
                expectedHandler: this.contentTypeServiceMock.Object.OnModifyingContentTypeAsync);

            VerifyContentTypeSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers.ContentTypeOnRemovingContentTypeByIdSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers.ContentTypeOnRemovingContentTypeByIdSubscriptionName,
                expectedOperation: ContentTypeEventOperation.RemovingById,
                expectedHandler: this.contentTypeServiceMock.Object.OnRemovingContentTypeByIdAsync);

            VerifyContentTypeSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ContentTypeOnHardRemovingContentTypeByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ContentTypeOnHardRemovingContentTypeByIdSubscriptionName,
                expectedOperation: ContentTypeEventOperation.HardRemovingById,
                expectedHandler: this.contentTypeServiceMock.Object.OnHardRemovingContentTypeByIdAsync);

            VerifyContentTypeSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ContentTypeOnRetrievingContentTypeByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ContentTypeOnRetrievingContentTypeByIdSubscriptionName,
                expectedOperation: ContentTypeEventOperation.RetrievingById,
                expectedHandler: this.contentTypeServiceMock.Object.OnRetrievingContentTypeByIdAsync);

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

            VerifyApprovalSettingReviewerRoleSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnAddingApprovalSettingReviewerRoleSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnAddingApprovalSettingReviewerRoleSubscriptionName,
                expectedOperation: ApprovalSettingReviewerRoleEventOperation.Adding,

                expectedHandler:
                    this.approvalSettingReviewerRoleServiceMock.Object.OnAddingApprovalSettingReviewerRoleAsync);

            VerifyApprovalSettingReviewerRoleSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnModifyingApprovalSettingReviewerRoleSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnModifyingApprovalSettingReviewerRoleSubscriptionName,
                expectedOperation: ApprovalSettingReviewerRoleEventOperation.Modifying,

                expectedHandler:
                    this.approvalSettingReviewerRoleServiceMock.Object.OnModifyingApprovalSettingReviewerRoleAsync);

            VerifyApprovalSettingReviewerRoleSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnRemovingApprovalSettingReviewerRoleByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnRemovingApprovalSettingReviewerRoleByIdSubscriptionName,
                expectedOperation: ApprovalSettingReviewerRoleEventOperation.RemovingById,

                expectedHandler:
                    this.approvalSettingReviewerRoleServiceMock.Object.OnRemovingApprovalSettingReviewerRoleByIdAsync);

            VerifyApprovalSettingReviewerRoleSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers
                        .ApprovalSettingReviewerRoleOnHardRemovingApprovalSettingReviewerRoleByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers
                        .ApprovalSettingReviewerRoleOnHardRemovingApprovalSettingReviewerRoleByIdSubscriptionName,
                expectedOperation: ApprovalSettingReviewerRoleEventOperation.HardRemovingById,

                expectedHandler:
                    this.approvalSettingReviewerRoleServiceMock.Object.OnHardRemovingApprovalSettingReviewerRoleByIdAsync);

            VerifyApprovalSettingReviewerRoleSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers
                        .ApprovalSettingReviewerRoleOnRetrievingApprovalSettingReviewerRoleByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers
                        .ApprovalSettingReviewerRoleOnRetrievingApprovalSettingReviewerRoleByIdSubscriptionName,
                expectedOperation: ApprovalSettingReviewerRoleEventOperation.RetrievingById,

                expectedHandler:
                    this.approvalSettingReviewerRoleServiceMock.Object.OnRetrievingApprovalSettingReviewerRoleByIdAsync);

            VerifyApprovalSettingPublisherRoleSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnAddingApprovalSettingPublisherRoleSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnAddingApprovalSettingPublisherRoleSubscriptionName,
                expectedOperation: ApprovalSettingPublisherRoleEventOperation.Adding,

                expectedHandler:
                    this.approvalSettingPublisherRoleServiceMock.Object.OnAddingApprovalSettingPublisherRoleAsync);

            VerifyApprovalSettingPublisherRoleSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnModifyingApprovalSettingPublisherRoleSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnModifyingApprovalSettingPublisherRoleSubscriptionName,
                expectedOperation: ApprovalSettingPublisherRoleEventOperation.Modifying,

                expectedHandler:
                    this.approvalSettingPublisherRoleServiceMock.Object.OnModifyingApprovalSettingPublisherRoleAsync);

            VerifyApprovalSettingPublisherRoleSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnRemovingApprovalSettingPublisherRoleByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnRemovingApprovalSettingPublisherRoleByIdSubscriptionName,
                expectedOperation: ApprovalSettingPublisherRoleEventOperation.RemovingById,

                expectedHandler:
                    this.approvalSettingPublisherRoleServiceMock.Object.OnRemovingApprovalSettingPublisherRoleByIdAsync);

            VerifyApprovalSettingPublisherRoleSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers
                        .ApprovalSettingPublisherRoleOnHardRemovingApprovalSettingPublisherRoleByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers
                        .ApprovalSettingPublisherRoleOnHardRemovingApprovalSettingPublisherRoleByIdSubscriptionName,
                expectedOperation: ApprovalSettingPublisherRoleEventOperation.HardRemovingById,

                expectedHandler:
                    this.approvalSettingPublisherRoleServiceMock.Object.OnHardRemovingApprovalSettingPublisherRoleByIdAsync);

            VerifyApprovalSettingPublisherRoleSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers
                        .ApprovalSettingPublisherRoleOnRetrievingApprovalSettingPublisherRoleByIdSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers
                        .ApprovalSettingPublisherRoleOnRetrievingApprovalSettingPublisherRoleByIdSubscriptionName,
                expectedOperation: ApprovalSettingPublisherRoleEventOperation.RetrievingById,

                expectedHandler:
                    this.approvalSettingPublisherRoleServiceMock.Object.OnRetrievingApprovalSettingPublisherRoleByIdAsync);

            VerifyContentItemAssociationSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers
                    .ContentItemAssociationOnAddingContentItemAssociationSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers
                    .ContentItemAssociationOnAddingContentItemAssociationSubscriptionName,
                expectedOperation: ContentItemAssociationEventOperation.Adding,
                expectedHandler: this.contentItemAssociationServiceMock.Object
                    .OnAddingContentItemAssociationAsync);

            VerifyContentItemAssociationSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers
                    .ContentItemAssociationOnModifyingContentItemAssociationSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers
                    .ContentItemAssociationOnModifyingContentItemAssociationSubscriptionName,
                expectedOperation: ContentItemAssociationEventOperation.Modifying,
                expectedHandler: this.contentItemAssociationServiceMock.Object
                    .OnModifyingContentItemAssociationAsync);

            VerifyContentItemAssociationSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers
                    .ContentItemAssociationOnRemovingContentItemAssociationByIdSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers
                    .ContentItemAssociationOnRemovingContentItemAssociationByIdSubscriptionName,
                expectedOperation: ContentItemAssociationEventOperation.RemovingById,
                expectedHandler: this.contentItemAssociationServiceMock.Object
                    .OnRemovingContentItemAssociationByIdAsync);

            VerifyContentItemAssociationSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers
                    .ContentItemAssociationOnHardRemovingContentItemAssociationByIdSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers
                    .ContentItemAssociationOnHardRemovingContentItemAssociationByIdSubscriptionName,
                expectedOperation: ContentItemAssociationEventOperation.HardRemovingById,
                expectedHandler: this.contentItemAssociationServiceMock.Object
                    .OnHardRemovingContentItemAssociationByIdAsync);

            VerifyContentItemAssociationSubscription(
                expectedSubscriptionId: EventBrokerIdentifiers
                    .ContentItemAssociationOnRetrievingContentItemAssociationByIdSubscriptionId,
                expectedSubscriptionName: EventBrokerIdentifiers
                    .ContentItemAssociationOnRetrievingContentItemAssociationByIdSubscriptionName,
                expectedOperation: ContentItemAssociationEventOperation.RetrievingById,
                expectedHandler: this.contentItemAssociationServiceMock.Object
                    .OnRetrievingContentItemAssociationByIdAsync);

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

            VerifyContentItemSubmissionSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ContentItemOrchestrationOnSubmittingContentItemSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ContentItemOrchestrationOnSubmittingContentItemSubscriptionName,
                expectedOperation: ContentItemSubmissionEventOperation.Submitting,
                expectedHandler:
                    this.contentItemOrchestrationServiceMock.Object.OnSubmittingContentItemAsync);

            VerifyContentItemSubmissionSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ContentItemOrchestrationOnAmendingContentItemSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ContentItemOrchestrationOnAmendingContentItemSubscriptionName,
                expectedOperation: ContentItemSubmissionEventOperation.Amending,
                expectedHandler:
                    this.contentItemOrchestrationServiceMock.Object.OnAmendingContentItemAsync);

            VerifyContentItemSubmissionSubscription(
                expectedSubscriptionId:
                    EventBrokerIdentifiers.ContentItemOrchestrationOnWithdrawingContentItemSubscriptionId,
                expectedSubscriptionName:
                    EventBrokerIdentifiers.ContentItemOrchestrationOnWithdrawingContentItemSubscriptionName,
                expectedOperation: ContentItemSubmissionEventOperation.Withdrawing,
                expectedHandler:
                    this.contentItemOrchestrationServiceMock.Object.OnWithdrawingContentItemAsync);

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.contentTypeServiceMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.approvalServiceMock.VerifyNoOtherCalls();
            this.contentItemOrchestrationServiceMock.VerifyNoOtherCalls();
        }
    }
}
