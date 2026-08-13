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
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        [Fact]
        public async Task ShouldRemoveAssociationByIdAsync()
        {
            // given
            Association randomAssociation = CreateRandomAssociation();
            randomAssociation.IsDeleted = false;
            Association storageAssociation = randomAssociation;

            Association auditedAssociation = storageAssociation.DeepClone();
            auditedAssociation.IsDeleted = true;

            Association expectedAssociation = auditedAssociation.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    randomAssociation.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageAssociation.CreatedBy);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedAssociation);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAssociationAsync(auditedAssociation, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedAssociation);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    AssociationEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<Association>>(
                        new EventPublishResult<Association>()));

            // when
            Association actualAssociation =
                await this.associationService.RemoveAssociationByIdAsync(
                    randomAssociation.Id,
                    deletionReason: null,
                    TestContext.Current.CancellationToken);

            // then
            actualAssociation.Should().BeEquivalentTo(expectedAssociation);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    randomAssociation.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAssociationAsync(auditedAssociation, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    AssociationEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .AssociationOnRemovingAssociationByIdSubscriptionName),
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
        public async Task ShouldRemoveAssociationByIdWithDeletionReasonAsync()
        {
            // given
            string someDeletionReason = GetRandomString();
            Association randomAssociation = CreateRandomAssociation();
            randomAssociation.IsDeleted = false;
            Association storageAssociation = randomAssociation;

            Association auditedAssociation = storageAssociation.DeepClone();
            auditedAssociation.IsDeleted = true;
            auditedAssociation.DeletionReason = someDeletionReason;

            Association expectedAssociation = auditedAssociation.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    randomAssociation.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageAssociation.CreatedBy);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageAssociation, It.IsAny<SecurityContext>(), someDeletionReason))
                    .ReturnsAsync(auditedAssociation);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAssociationAsync(auditedAssociation, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedAssociation);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    AssociationEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<Association>>(
                        new EventPublishResult<Association>()));

            // when
            Association actualAssociation =
                await this.associationService.RemoveAssociationByIdAsync(
                    randomAssociation.Id,
                    deletionReason: someDeletionReason,
                    TestContext.Current.CancellationToken);

            // then
            actualAssociation.Should().BeEquivalentTo(expectedAssociation);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    randomAssociation.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageAssociation, It.IsAny<SecurityContext>(), someDeletionReason),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAssociationAsync(auditedAssociation, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    AssociationEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .AssociationOnRemovingAssociationByIdSubscriptionName),
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
            Association alreadyDeletedAssociation = CreateRandomAssociation();
            alreadyDeletedAssociation.IsDeleted = true;
            Guid someAssociationId = alreadyDeletedAssociation.Id;
            Association expectedAssociation = alreadyDeletedAssociation;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(alreadyDeletedAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(alreadyDeletedAssociation.CreatedBy);

            // when
            Association actualAssociation =
                await this.associationService.RemoveAssociationByIdAsync(
                    someAssociationId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualAssociation.Should().BeEquivalentTo(expectedAssociation);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    someAssociationId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRemoveSomeoneElsesAssociationByIdWhenUserIsAdminAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            string randomActorUserId = GetRandomString();
            Association randomAssociation = CreateRandomAssociation();
            randomAssociation.IsDeleted = false;
            Association storageAssociation = randomAssociation;

            Association auditedAssociation = storageAssociation.DeepClone();
            auditedAssociation.IsDeleted = true;

            Association expectedAssociation = auditedAssociation.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    randomAssociation.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedAssociation);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAssociationAsync(auditedAssociation, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedAssociation);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    AssociationEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<Association>>(
                        new EventPublishResult<Association>()));

            // when
            Association actualAssociation =
                await this.associationService.RemoveAssociationByIdAsync(
                    randomAssociation.Id,
                    deletionReason: null,
                    TestContext.Current.CancellationToken);

            // then
            actualAssociation.Should().BeEquivalentTo(expectedAssociation);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    randomAssociation.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAssociationAsync(auditedAssociation, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    AssociationEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .AssociationOnRemovingAssociationByIdSubscriptionName),
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
