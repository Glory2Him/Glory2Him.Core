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
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItemAssociations
{
    public partial class ContentItemAssociationServiceTests
    {
        [Fact]
        public async Task ShouldRemoveContentItemAssociationByIdAndReplyOnRemovingContentItemAssociationByIdEventAsync()
        {
            // given
            string randomDeletionReason = GetRandomString();
            ContentItemAssociation storageContentItemAssociation = CreateRandomContentItemAssociation();
            storageContentItemAssociation.IsDeleted = false;
            ContentItemAssociation auditedContentItemAssociation = storageContentItemAssociation.DeepClone();
            ContentItemAssociation removedContentItemAssociation = auditedContentItemAssociation.DeepClone();
            ContentItemAssociation expectedContentItemAssociation = removedContentItemAssociation.DeepClone();

            var requestEnvelope = new EventEnvelope<ContentItemAssociation>
            {
                Content = new ContentItemAssociation
                {
                    Id = storageContentItemAssociation.Id,
                    DeletionReason = randomDeletionReason
                },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentItemAssociationOnRemovingContentItemAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    storageContentItemAssociation.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItemAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedContentItemAssociation);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateContentItemAssociationAsync(auditedContentItemAssociation, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(removedContentItemAssociation);

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(Guid.NewGuid());

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemAssociationAsync(
                    It.IsAny<EventEnvelope<ContentItemAssociation>>(),
                    ContentItemAssociationEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<ContentItemAssociation>>(
                        new EventPublishResult<ContentItemAssociation>()));

            // when
            EventEnvelope<ContentItemAssociation>? actualReplyEnvelope =
                await this.contentItemAssociationService.OnRemovingContentItemAssociationByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedContentItemAssociation);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentItemAssociationOnRemovingContentItemAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    storageContentItemAssociation.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageContentItemAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateContentItemAssociationAsync(auditedContentItemAssociation, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishContentItemAssociationAsync(
                    It.IsAny<EventEnvelope<ContentItemAssociation>>(),
                    ContentItemAssociationEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.EventId == requestEnvelope.Metadata.EventId
                            && processedEvent.ReceiverName ==
                                EventBrokerIdentifiers
                                    .ContentItemAssociationOnRemovingContentItemAssociationByIdSubscriptionName),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .ContentItemAssociationOnRemovingContentItemAssociationByIdSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldSkipRemoveAndReplyNullWhenRemovingContentItemAssociationByIdEventAlreadyProcessedAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<ContentItemAssociation>
            {
                Content = new ContentItemAssociation { Id = Guid.NewGuid() },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentItemAssociationOnRemovingContentItemAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<ContentItemAssociation>? actualReplyEnvelope =
                await this.contentItemAssociationService.OnRemovingContentItemAssociationByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: no work, no fact, no reply — the duplicate is acknowledged silently
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentItemAssociationOnRemovingContentItemAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReplyWithExistingContentItemAssociationOnRemovingContentItemAssociationByIdEventWhenAlreadyDeletedAsync()
        {
            // given
            ContentItemAssociation alreadyDeletedContentItemAssociation = CreateRandomContentItemAssociation();
            alreadyDeletedContentItemAssociation.IsDeleted = true;
            ContentItemAssociation expectedContentItemAssociation = alreadyDeletedContentItemAssociation.DeepClone();

            var requestEnvelope = new EventEnvelope<ContentItemAssociation>
            {
                Content = new ContentItemAssociation { Id = alreadyDeletedContentItemAssociation.Id },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentItemAssociationOnRemovingContentItemAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    alreadyDeletedContentItemAssociation.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(alreadyDeletedContentItemAssociation);

            // when
            EventEnvelope<ContentItemAssociation>? actualReplyEnvelope =
                await this.contentItemAssociationService.OnRemovingContentItemAssociationByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: already deleted — no mutation happened, so no fact is published and
            // nothing is recorded as processed; the existing entity is returned as the reply.
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedContentItemAssociation);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentItemAssociationOnRemovingContentItemAssociationByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    alreadyDeletedContentItemAssociation.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
