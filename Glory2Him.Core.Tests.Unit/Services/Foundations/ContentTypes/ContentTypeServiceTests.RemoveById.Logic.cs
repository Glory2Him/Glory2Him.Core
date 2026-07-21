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
        public async Task ShouldRemoveContentTypeByIdAsync()
        {
            // given
            ContentType randomContentType = CreateRandomContentType();
            randomContentType.IsDeleted = false;
            ContentType storageContentType = randomContentType;

            ContentType auditedContentType = storageContentType.DeepClone();
            auditedContentType.IsDeleted = true;

            ContentType expectedContentType = auditedContentType.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentTypeByIdAsync(
                    randomContentType.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentType);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageContentType, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedContentType);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateContentTypeAsync(auditedContentType, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedContentType);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentTypeAsync(
                    It.IsAny<EventEnvelope<ContentType>>(),
                    ContentTypeEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<ContentType>>(
                        new EventPublishResult<ContentType>()));

            // when
            ContentType actualContentType =
                await this.contentTypeService.RemoveContentTypeByIdAsync(
                    randomContentType.Id,
                    deletionReason: null,
                    TestContext.Current.CancellationToken);

            // then
            actualContentType.Should().BeEquivalentTo(expectedContentType);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentTypeByIdAsync(
                    randomContentType.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageContentType, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateContentTypeAsync(auditedContentType, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishContentTypeAsync(
                    It.IsAny<EventEnvelope<ContentType>>(),
                    ContentTypeEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ContentTypeOnRemovingContentTypeByIdSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRemoveContentTypeByIdWithDeletionReasonAsync()
        {
            // given
            string someDeletionReason = GetRandomString();
            ContentType randomContentType = CreateRandomContentType();
            randomContentType.IsDeleted = false;
            ContentType storageContentType = randomContentType;

            ContentType auditedContentType = storageContentType.DeepClone();
            auditedContentType.IsDeleted = true;
            auditedContentType.DeletionReason = someDeletionReason;

            ContentType expectedContentType = auditedContentType.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentTypeByIdAsync(
                    randomContentType.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentType);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageContentType, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedContentType);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateContentTypeAsync(auditedContentType, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedContentType);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentTypeAsync(
                    It.IsAny<EventEnvelope<ContentType>>(),
                    ContentTypeEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<ContentType>>(
                        new EventPublishResult<ContentType>()));

            // when
            ContentType actualContentType =
                await this.contentTypeService.RemoveContentTypeByIdAsync(
                    randomContentType.Id,
                    deletionReason: someDeletionReason,
                    TestContext.Current.CancellationToken);

            // then
            actualContentType.Should().BeEquivalentTo(expectedContentType);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentTypeByIdAsync(
                    randomContentType.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageContentType, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateContentTypeAsync(auditedContentType, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishContentTypeAsync(
                    It.IsAny<EventEnvelope<ContentType>>(),
                    ContentTypeEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ContentTypeOnRemovingContentTypeByIdSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
