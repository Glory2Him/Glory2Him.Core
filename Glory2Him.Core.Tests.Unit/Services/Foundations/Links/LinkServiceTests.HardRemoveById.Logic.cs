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
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Links
{
    public partial class LinkServiceTests
    {
        [Fact]
        public async Task ShouldHardRemoveLinkByIdAsync()
        {
            // given
            Link randomLink = CreateRandomLink();
            Link storageLink = randomLink;
            Link expectedLink = storageLink.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    randomLink.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageLink);

            this.storageBrokerMock.Setup(broker =>
                broker.DeleteLinkAsync(storageLink, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedLink);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishLinkAsync(
                    It.IsAny<EventEnvelope<Link>>(),
                    LinkEventOperation.HardRemoved))
                    .Returns(new ValueTask<EventPublishResult<Link>>(
                        new EventPublishResult<Link>()));

            // when
            Link actualLink =
                await this.linkService.HardRemoveLinkByIdAsync(
                    randomLink.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualLink.Should().BeEquivalentTo(expectedLink);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    randomLink.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteLinkAsync(storageLink, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishLinkAsync(
                    It.IsAny<EventEnvelope<Link>>(),
                    LinkEventOperation.HardRemoved),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.LinkOnHardRemovingLinkByIdSubscriptionName),
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
