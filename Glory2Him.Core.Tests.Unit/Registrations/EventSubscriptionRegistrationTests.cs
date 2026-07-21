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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Glory2Him.Core.Registrations;
using Glory2Him.Core.Services.Foundations.ContentItems;
using Glory2Him.Core.Services.Foundations.ContentTypes;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Registrations
{
    public partial class EventSubscriptionRegistrationTests
    {
        private readonly Mock<IEventBroker> eventBrokerMock;
        private readonly Mock<IContentTypeService> contentTypeServiceMock;
        private readonly Mock<IContentItemService> contentItemServiceMock;
        private readonly IEventSubscriptionRegistration eventSubscriptionRegistration;

        public EventSubscriptionRegistrationTests()
        {
            this.eventBrokerMock = new Mock<IEventBroker>();
            this.contentTypeServiceMock = new Mock<IContentTypeService>();
            this.contentItemServiceMock = new Mock<IContentItemService>();

            this.eventSubscriptionRegistration = new EventSubscriptionRegistration(
                eventBroker: this.eventBrokerMock.Object,
                contentTypeService: this.contentTypeServiceMock.Object,
                contentItemService: this.contentItemServiceMock.Object);
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

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.contentTypeServiceMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
        }
    }
}
