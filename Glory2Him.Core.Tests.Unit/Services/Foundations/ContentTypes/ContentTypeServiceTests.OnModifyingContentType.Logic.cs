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
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentTypes
{
    public partial class ContentTypeServiceTests
    {
        [Fact]
        public async Task ShouldModifyContentTypeAndReplyOnModifyingContentTypeEventAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ContentType randomContentType = CreateRandomModifyContentType(randomDateTimeOffset, randomUserId);
            ContentType inputContentType = randomContentType;
            ContentType auditAppliedContentType = inputContentType.DeepClone();
            ContentType storageContentType = auditAppliedContentType.DeepClone();
            storageContentType.UpdatedWhen = storageContentType.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            ContentType auditPreservedContentType = auditAppliedContentType.DeepClone();
            ContentType updatedContentType = auditPreservedContentType.DeepClone();
            ContentType expectedContentType = updatedContentType.DeepClone();

            var requestEnvelope = new EventEnvelope<ContentType>
            {
                Content = inputContentType,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentTypeOnModifyingContentTypeSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputContentType, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedContentType);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentTypeByIdAsync(
                    auditAppliedContentType.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentType);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    auditAppliedContentType,
                    storageContentType))
                        .ReturnsAsync(auditPreservedContentType);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateContentTypeAsync(auditPreservedContentType, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(updatedContentType);

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(Guid.NewGuid());

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentTypeAsync(
                    It.IsAny<EventEnvelope<ContentType>>(),
                    ContentTypeEventOperation.Modified))
                    .Returns(new ValueTask<EventPublishResult<ContentType>>(
                        new EventPublishResult<ContentType>()));

            // when
            EventEnvelope<ContentType>? actualReplyEnvelope =
                await this.contentTypeService.OnModifyingContentTypeAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedContentType);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentTypeOnModifyingContentTypeSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentTypeByIdAsync(
                    auditAppliedContentType.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateContentTypeAsync(auditPreservedContentType, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishContentTypeAsync(
                    It.IsAny<EventEnvelope<ContentType>>(),
                    ContentTypeEventOperation.Modified),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.EventId == requestEnvelope.Metadata.EventId
                            && processedEvent.ReceiverName ==
                                EventBrokerIdentifiers.ContentTypeOnModifyingContentTypeSubscriptionName),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ContentTypeOnModifyingContentTypeSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldSkipModifyAndReplyNullWhenModifyingContentTypeEventAlreadyProcessedAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<ContentType>
            {
                Content = new ContentType { Id = Guid.NewGuid() },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentTypeOnModifyingContentTypeSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<ContentType>? actualReplyEnvelope =
                await this.contentTypeService.OnModifyingContentTypeAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: no work, no fact, no reply — the duplicate is acknowledged silently
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ContentTypeOnModifyingContentTypeSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
