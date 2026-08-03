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
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.BibleReferences
{
    public partial class BibleReferenceServiceTests
    {
        [Fact]
        public async Task ShouldRemoveBibleReferenceByIdAsync()
        {
            // given
            BibleReference randomBibleReference = CreateRandomBibleReference();
            randomBibleReference.IsDeleted = false;
            BibleReference storageBibleReference = randomBibleReference;

            BibleReference auditedBibleReference = storageBibleReference.DeepClone();
            auditedBibleReference.IsDeleted = true;

            BibleReference expectedBibleReference = auditedBibleReference.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    randomBibleReference.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageBibleReference);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageBibleReference.CreatedBy);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageBibleReference, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedBibleReference);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateBibleReferenceAsync(auditedBibleReference, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedBibleReference);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishBibleReferenceAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(),
                    BibleReferenceEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<BibleReference>>(
                        new EventPublishResult<BibleReference>()));

            // when
            BibleReference actualBibleReference =
                await this.bibleReferenceService.RemoveBibleReferenceByIdAsync(
                    randomBibleReference.Id,
                    deletionReason: null,
                    TestContext.Current.CancellationToken);

            // then
            actualBibleReference.Should().BeEquivalentTo(expectedBibleReference);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    randomBibleReference.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageBibleReference, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateBibleReferenceAsync(auditedBibleReference, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishBibleReferenceAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(),
                    BibleReferenceEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.BibleReferenceOnRemovingBibleReferenceByIdSubscriptionName),
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
        public async Task ShouldRemoveBibleReferenceByIdWithDeletionReasonAsync()
        {
            // given
            string someDeletionReason = GetRandomString();
            BibleReference randomBibleReference = CreateRandomBibleReference();
            randomBibleReference.IsDeleted = false;
            BibleReference storageBibleReference = randomBibleReference;

            BibleReference auditedBibleReference = storageBibleReference.DeepClone();
            auditedBibleReference.IsDeleted = true;
            auditedBibleReference.DeletionReason = someDeletionReason;

            BibleReference expectedBibleReference = auditedBibleReference.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    randomBibleReference.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageBibleReference);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageBibleReference.CreatedBy);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageBibleReference, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedBibleReference);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateBibleReferenceAsync(auditedBibleReference, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedBibleReference);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishBibleReferenceAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(),
                    BibleReferenceEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<BibleReference>>(
                        new EventPublishResult<BibleReference>()));

            // when
            BibleReference actualBibleReference =
                await this.bibleReferenceService.RemoveBibleReferenceByIdAsync(
                    randomBibleReference.Id,
                    deletionReason: someDeletionReason,
                    TestContext.Current.CancellationToken);

            // then
            actualBibleReference.Should().BeEquivalentTo(expectedBibleReference);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    randomBibleReference.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageBibleReference, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateBibleReferenceAsync(auditedBibleReference, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishBibleReferenceAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(),
                    BibleReferenceEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.BibleReferenceOnRemovingBibleReferenceByIdSubscriptionName),
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
            BibleReference alreadyDeletedBibleReference = CreateRandomBibleReference();
            alreadyDeletedBibleReference.IsDeleted = true;
            Guid someBibleReferenceId = alreadyDeletedBibleReference.Id;
            BibleReference expectedBibleReference = alreadyDeletedBibleReference;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    someBibleReferenceId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(alreadyDeletedBibleReference);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(alreadyDeletedBibleReference.CreatedBy);

            // when
            BibleReference actualBibleReference =
                await this.bibleReferenceService.RemoveBibleReferenceByIdAsync(
                    someBibleReferenceId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualBibleReference.Should().BeEquivalentTo(expectedBibleReference);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    someBibleReferenceId,
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
        public async Task ShouldRemoveSomeoneElsesBibleReferenceByIdWhenUserIsAdminAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            string randomActorUserId = GetRandomString();
            BibleReference randomBibleReference = CreateRandomBibleReference();
            randomBibleReference.IsDeleted = false;
            BibleReference storageBibleReference = randomBibleReference;

            BibleReference auditedBibleReference = storageBibleReference.DeepClone();
            auditedBibleReference.IsDeleted = true;

            BibleReference expectedBibleReference = auditedBibleReference.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    randomBibleReference.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageBibleReference);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageBibleReference, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedBibleReference);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateBibleReferenceAsync(auditedBibleReference, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedBibleReference);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishBibleReferenceAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(),
                    BibleReferenceEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<BibleReference>>(
                        new EventPublishResult<BibleReference>()));

            // when
            BibleReference actualBibleReference =
                await this.bibleReferenceService.RemoveBibleReferenceByIdAsync(
                    randomBibleReference.Id,
                    deletionReason: null,
                    TestContext.Current.CancellationToken);

            // then
            actualBibleReference.Should().BeEquivalentTo(expectedBibleReference);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    randomBibleReference.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageBibleReference, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateBibleReferenceAsync(auditedBibleReference, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishBibleReferenceAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(),
                    BibleReferenceEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.BibleReferenceOnRemovingBibleReferenceByIdSubscriptionName),
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
