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
        public async Task ShouldRemoveContentItemAssociationByIdAsync()
        {
            // given
            ContentItemAssociation randomContentItemAssociation = CreateRandomContentItemAssociation();
            randomContentItemAssociation.IsDeleted = false;
            ContentItemAssociation storageContentItemAssociation = randomContentItemAssociation;

            ContentItemAssociation auditedContentItemAssociation = storageContentItemAssociation.DeepClone();
            auditedContentItemAssociation.IsDeleted = true;

            ContentItemAssociation expectedContentItemAssociation = auditedContentItemAssociation.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    randomContentItemAssociation.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItemAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedContentItemAssociation);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateContentItemAssociationAsync(auditedContentItemAssociation, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedContentItemAssociation);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemAssociationAsync(
                    It.IsAny<EventEnvelope<ContentItemAssociation>>(),
                    ContentItemAssociationEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<ContentItemAssociation>>(
                        new EventPublishResult<ContentItemAssociation>()));

            // when
            ContentItemAssociation actualContentItemAssociation =
                await this.contentItemAssociationService.RemoveContentItemAssociationByIdAsync(
                    randomContentItemAssociation.Id,
                    deletionReason: null,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItemAssociation.Should().BeEquivalentTo(expectedContentItemAssociation);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    randomContentItemAssociation.Id,
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
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .ContentItemAssociationOnRemovingContentItemAssociationByIdSubscriptionName),
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
        public async Task ShouldRemoveContentItemAssociationByIdWithDeletionReasonAsync()
        {
            // given
            string someDeletionReason = GetRandomString();
            ContentItemAssociation randomContentItemAssociation = CreateRandomContentItemAssociation();
            randomContentItemAssociation.IsDeleted = false;
            ContentItemAssociation storageContentItemAssociation = randomContentItemAssociation;

            ContentItemAssociation auditedContentItemAssociation = storageContentItemAssociation.DeepClone();
            auditedContentItemAssociation.IsDeleted = true;
            auditedContentItemAssociation.DeletionReason = someDeletionReason;

            ContentItemAssociation expectedContentItemAssociation = auditedContentItemAssociation.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    randomContentItemAssociation.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItemAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedContentItemAssociation);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateContentItemAssociationAsync(auditedContentItemAssociation, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedContentItemAssociation);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemAssociationAsync(
                    It.IsAny<EventEnvelope<ContentItemAssociation>>(),
                    ContentItemAssociationEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<ContentItemAssociation>>(
                        new EventPublishResult<ContentItemAssociation>()));

            // when
            ContentItemAssociation actualContentItemAssociation =
                await this.contentItemAssociationService.RemoveContentItemAssociationByIdAsync(
                    randomContentItemAssociation.Id,
                    deletionReason: someDeletionReason,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItemAssociation.Should().BeEquivalentTo(expectedContentItemAssociation);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    randomContentItemAssociation.Id,
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
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .ContentItemAssociationOnRemovingContentItemAssociationByIdSubscriptionName),
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
        public async Task ShouldReturnEarlyOnRemoveByIdIfAlreadyDeletedAsync()
        {
            // given
            ContentItemAssociation alreadyDeletedContentItemAssociation = CreateRandomContentItemAssociation();
            alreadyDeletedContentItemAssociation.IsDeleted = true;
            Guid someContentItemAssociationId = alreadyDeletedContentItemAssociation.Id;
            ContentItemAssociation expectedContentItemAssociation = alreadyDeletedContentItemAssociation;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    someContentItemAssociationId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(alreadyDeletedContentItemAssociation);

            // when
            ContentItemAssociation actualContentItemAssociation =
                await this.contentItemAssociationService.RemoveContentItemAssociationByIdAsync(
                    someContentItemAssociationId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualContentItemAssociation.Should().BeEquivalentTo(expectedContentItemAssociation);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    someContentItemAssociationId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
