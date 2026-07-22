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
using Glory2Him.Core.Models.Foundations.ContentItemAssociations;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItemAssociations
{
    public partial class ContentItemAssociationServiceTests
    {
        [Fact]
        public async Task ShouldHardRemoveContentItemAssociationByIdAsync()
        {
            // given
            ContentItemAssociation randomContentItemAssociation = CreateRandomContentItemAssociation();
            ContentItemAssociation storageContentItemAssociation = randomContentItemAssociation;
            ContentItemAssociation expectedContentItemAssociation = storageContentItemAssociation.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    randomContentItemAssociation.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItemAssociation);

            this.storageBrokerMock.Setup(broker =>
                broker.DeleteContentItemAssociationAsync(storageContentItemAssociation, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedContentItemAssociation);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemAssociationAsync(
                    It.IsAny<EventEnvelope<ContentItemAssociation>>(),
                    ContentItemAssociationEventOperation.HardRemoved))
                    .Returns(new ValueTask<EventPublishResult<ContentItemAssociation>>(
                        new EventPublishResult<ContentItemAssociation>()));

            // when
            ContentItemAssociation actualContentItemAssociation =
                await this.contentItemAssociationService.HardRemoveContentItemAssociationByIdAsync(
                    randomContentItemAssociation.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItemAssociation.Should().BeEquivalentTo(expectedContentItemAssociation);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    randomContentItemAssociation.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteContentItemAssociationAsync(storageContentItemAssociation, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishContentItemAssociationAsync(
                    It.IsAny<EventEnvelope<ContentItemAssociation>>(),
                    ContentItemAssociationEventOperation.HardRemoved),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .ContentItemAssociationOnHardRemovingContentItemAssociationByIdSubscriptionName),
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
