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
        public async Task ShouldAddContentItemAssociationAndReplyOnAddingContentItemAssociationEventAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ContentItemAssociation randomContentItemAssociation =
                CreateContentItemAssociationFiller(randomDateTimeOffset).Create();
            ContentItemAssociation inputContentItemAssociation = randomContentItemAssociation;
            ContentItemAssociation auditAppliedContentItemAssociation = inputContentItemAssociation.DeepClone();
            ContentItemAssociation storageContentItemAssociation = auditAppliedContentItemAssociation.DeepClone();
            ContentItemAssociation expectedContentItemAssociation = storageContentItemAssociation.DeepClone();

            var requestEnvelope = new EventEnvelope<ContentItemAssociation>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = inputContentItemAssociation,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentItemAssociationOnAddingContentItemAssociationSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedContentItemAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedContentItemAssociation.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertContentItemAssociationAsync(
                    auditAppliedContentItemAssociation,
                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItemAssociation);

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(Guid.NewGuid());

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemAssociationAsync(
                    It.IsAny<EventEnvelope<ContentItemAssociation>>(),
                    ContentItemAssociationEventOperation.Added))
                    .Returns(new ValueTask<EventPublishResult<ContentItemAssociation>>(
                        new EventPublishResult<ContentItemAssociation>()));

            // when
            EventEnvelope<ContentItemAssociation>? actualReplyEnvelope =
                await this.contentItemAssociationService.OnAddingContentItemAssociationAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedContentItemAssociation);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentItemAssociationOnAddingContentItemAssociationSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertContentItemAssociationAsync(
                    auditAppliedContentItemAssociation,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishContentItemAssociationAsync(
                    It.IsAny<EventEnvelope<ContentItemAssociation>>(),
                    ContentItemAssociationEventOperation.Added),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.EventId == requestEnvelope.Metadata.EventId
                            && processedEvent.ReceiverName ==
                                EventBrokerIdentifiers
                                    .ContentItemAssociationOnAddingContentItemAssociationSubscriptionName),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .ContentItemAssociationOnAddingContentItemAssociationSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldSkipAddAndReplyNullWhenAddingContentItemAssociationEventAlreadyProcessedAsync()
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
                    EventBrokerIdentifiers.ContentItemAssociationOnAddingContentItemAssociationSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<ContentItemAssociation>? actualReplyEnvelope =
                await this.contentItemAssociationService.OnAddingContentItemAssociationAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: no work, no fact, no reply — the duplicate is acknowledged silently
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentItemAssociationOnAddingContentItemAssociationSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
