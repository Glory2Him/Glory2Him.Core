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
using Force.DeepCloner;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ContentItemAssociations;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItemAssociations
{
    public partial class ContentItemAssociationServiceTests
    {
        [Fact]
        public async Task ShouldHardRemoveContentItemAssociationByIdAndReplyOnHardRemovingContentItemAssociationByIdEventAsync()
        {
            // given
            ContentItemAssociation storageContentItemAssociation = CreateRandomContentItemAssociation();
            ContentItemAssociation deletedContentItemAssociation = storageContentItemAssociation.DeepClone();
            ContentItemAssociation expectedContentItemAssociation = deletedContentItemAssociation.DeepClone();

            var requestEnvelope = new EventEnvelope<ContentItemAssociation>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin),
                Content = new ContentItemAssociation { Id = storageContentItemAssociation.Id },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers
                        .ContentItemAssociationOnHardRemovingContentItemAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    storageContentItemAssociation.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItemAssociation);

            this.storageBrokerMock.Setup(broker =>
                broker.DeleteContentItemAssociationAsync(storageContentItemAssociation, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(deletedContentItemAssociation);

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(Guid.NewGuid());

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemAssociationAsync(
                    It.IsAny<EventEnvelope<ContentItemAssociation>>(),
                    ContentItemAssociationEventOperation.HardRemoved))
                    .Returns(new ValueTask<EventPublishResult<ContentItemAssociation>>(
                        new EventPublishResult<ContentItemAssociation>()));

            // when
            EventEnvelope<ContentItemAssociation>? actualReplyEnvelope =
                await this.contentItemAssociationService.OnHardRemovingContentItemAssociationByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedContentItemAssociation);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers
                        .ContentItemAssociationOnHardRemovingContentItemAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    storageContentItemAssociation.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteContentItemAssociationAsync(storageContentItemAssociation, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishContentItemAssociationAsync(
                    It.IsAny<EventEnvelope<ContentItemAssociation>>(),
                    ContentItemAssociationEventOperation.HardRemoved),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.EventId == requestEnvelope.Metadata.EventId
                            && processedEvent.ReceiverName ==
                                EventBrokerIdentifiers
                                    .ContentItemAssociationOnHardRemovingContentItemAssociationByIdSubscriptionName),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .ContentItemAssociationOnHardRemovingContentItemAssociationByIdSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldSkipHardRemoveAndReplyNullWhenHardRemovingContentItemAssociationByIdEventAlreadyProcessedAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<ContentItemAssociation>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new ContentItemAssociation { Id = Guid.NewGuid() },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers
                        .ContentItemAssociationOnHardRemovingContentItemAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<ContentItemAssociation>? actualReplyEnvelope =
                await this.contentItemAssociationService.OnHardRemovingContentItemAssociationByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: no work, no fact, no reply — the duplicate is acknowledged silently
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers
                        .ContentItemAssociationOnHardRemovingContentItemAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
