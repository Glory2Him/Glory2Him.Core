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
        public async Task ShouldModifyContentItemAssociationAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ContentItemAssociation randomContentItemAssociation =
                CreateRandomModifyContentItemAssociation(randomDateTimeOffset, randomUserId);
            ContentItemAssociation inputContentItemAssociation = randomContentItemAssociation;
            ContentItemAssociation auditAppliedContentItemAssociation = inputContentItemAssociation.DeepClone();
            ContentItemAssociation storageContentItemAssociation = auditAppliedContentItemAssociation.DeepClone();
            storageContentItemAssociation.UpdatedWhen =
                storageContentItemAssociation.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            ContentItemAssociation auditPreservedContentItemAssociation =
                auditAppliedContentItemAssociation.DeepClone();
            ContentItemAssociation updatedContentItemAssociation = auditPreservedContentItemAssociation.DeepClone();
            ContentItemAssociation expectedContentItemAssociation = updatedContentItemAssociation.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedContentItemAssociation);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    auditAppliedContentItemAssociation.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItemAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    auditAppliedContentItemAssociation,
                    storageContentItemAssociation))
                        .ReturnsAsync(auditPreservedContentItemAssociation);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateContentItemAssociationAsync(
                    auditPreservedContentItemAssociation,
                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync(updatedContentItemAssociation);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemAssociationAsync(
                    It.IsAny<EventEnvelope<ContentItemAssociation>>(),
                    ContentItemAssociationEventOperation.Modified))
                    .Returns(new ValueTask<EventPublishResult<ContentItemAssociation>>(
                        new EventPublishResult<ContentItemAssociation>()));

            // when
            ContentItemAssociation actualContentItemAssociation =
                await this.contentItemAssociationService.ModifyContentItemAssociationAsync(
                    inputContentItemAssociation,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItemAssociation.Should().BeEquivalentTo(expectedContentItemAssociation);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyModifyAuditValuesAsync(inputContentItemAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectContentItemAssociationByIdAsync(
                        auditAppliedContentItemAssociation.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                        auditAppliedContentItemAssociation,
                        storageContentItemAssociation),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateContentItemAssociationAsync(
                        auditPreservedContentItemAssociation,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishContentItemAssociationAsync(
                        It.IsAny<EventEnvelope<ContentItemAssociation>>(),
                        ContentItemAssociationEventOperation.Modified),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .ContentItemAssociationOnModifyingContentItemAssociationSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
