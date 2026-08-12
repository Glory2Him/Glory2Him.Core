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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.BibleReferences
{
    public partial class BibleReferenceServiceTests
    {
        [Fact]
        public async Task ShouldAddBibleReferenceAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            BibleReference randomBibleReference = CreateBibleReferenceFiller(randomDateTimeOffset).Create();
            BibleReference inputBibleReference = randomBibleReference;
            BibleReference auditAppliedBibleReference = inputBibleReference.DeepClone();
            BibleReference storageBibleReference = auditAppliedBibleReference.DeepClone();
            BibleReference expectedBibleReference = storageBibleReference.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputBibleReference, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedBibleReference);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedBibleReference.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertBibleReferenceAsync(auditAppliedBibleReference, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageBibleReference);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishBibleReferenceAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(), BibleReferenceEventOperation.Added))
                    .Returns(new ValueTask<EventPublishResult<BibleReference>>(
                        new EventPublishResult<BibleReference>()));

            // when
            BibleReference actualBibleReference =
                await this.bibleReferenceService.AddBibleReferenceAsync(
                    inputBibleReference,
                    TestContext.Current.CancellationToken);

            // then
            actualBibleReference.Should().BeEquivalentTo(expectedBibleReference);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyAddAuditValuesAsync(inputBibleReference, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.storageBrokerMock.Verify(broker =>
                    broker.InsertBibleReferenceAsync(auditAppliedBibleReference, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishBibleReferenceAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(),
                    BibleReferenceEventOperation.Added),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.BibleReferenceOnAddingBibleReferenceSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldAddIfApprovalStatusIsSubmittedAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            BibleReference randomBibleReference = CreateBibleReferenceFiller(randomDateTimeOffset).Create();
            randomBibleReference.ApprovalStatus = ApprovalStatus.Submitted;
            BibleReference inputBibleReference = randomBibleReference;
            BibleReference auditAppliedBibleReference = inputBibleReference.DeepClone();
            BibleReference storageBibleReference = auditAppliedBibleReference.DeepClone();
            BibleReference expectedBibleReference = storageBibleReference.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(inputBibleReference, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedBibleReference);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedBibleReference.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.InsertBibleReferenceAsync(auditAppliedBibleReference, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageBibleReference);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishBibleReferenceAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(), BibleReferenceEventOperation.Added))
                    .Returns(new ValueTask<EventPublishResult<BibleReference>>(
                        new EventPublishResult<BibleReference>()));

            // when
            BibleReference actualBibleReference =
                await this.bibleReferenceService.AddBibleReferenceAsync(
                    inputBibleReference,
                    TestContext.Current.CancellationToken);

            // then
            actualBibleReference.Should().BeEquivalentTo(expectedBibleReference);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyAddAuditValuesAsync(inputBibleReference, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.storageBrokerMock.Verify(broker =>
                    broker.InsertBibleReferenceAsync(auditAppliedBibleReference, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishBibleReferenceAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(),
                    BibleReferenceEventOperation.Added),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.BibleReferenceOnAddingBibleReferenceSubscriptionName),
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
