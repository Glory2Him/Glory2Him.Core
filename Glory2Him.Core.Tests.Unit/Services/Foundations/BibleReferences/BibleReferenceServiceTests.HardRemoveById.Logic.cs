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
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.BibleReferences
{
    public partial class BibleReferenceServiceTests
    {
        [Fact]
        public async Task ShouldHardRemoveBibleReferenceByIdAsync()
        {
            // given
            BibleReference randomBibleReference = CreateRandomBibleReference();
            BibleReference storageBibleReference = randomBibleReference;
            BibleReference expectedBibleReference = storageBibleReference.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    randomBibleReference.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageBibleReference);

            this.storageBrokerMock.Setup(broker =>
                broker.DeleteBibleReferenceAsync(storageBibleReference, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedBibleReference);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishBibleReferenceAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(),
                    BibleReferenceEventOperation.HardRemoved))
                    .Returns(new ValueTask<EventPublishResult<BibleReference>>(
                        new EventPublishResult<BibleReference>()));

            // when
            BibleReference actualBibleReference =
                await this.bibleReferenceService.HardRemoveBibleReferenceByIdAsync(
                    randomBibleReference.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualBibleReference.Should().BeEquivalentTo(expectedBibleReference);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    randomBibleReference.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteBibleReferenceAsync(storageBibleReference, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishBibleReferenceAsync(
                    It.IsAny<EventEnvelope<BibleReference>>(),
                    BibleReferenceEventOperation.HardRemoved),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.BibleReferenceOnHardRemovingBibleReferenceByIdSubscriptionName),
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
