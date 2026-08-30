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
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Links
{
    public partial class LinkServiceTests
    {
        [Fact]
        public async Task ShouldHardRemoveLinkByIdAndReplyOnHardRemovingLinkByIdEventAsync()
        {
            // given
            Link storageLink = CreateRandomLink();
            Link deletedLink = storageLink.DeepClone();
            Link expectedLink = deletedLink.DeepClone();

            var requestEnvelope = new EventEnvelope<Link>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators),
                Content = new Link { Id = storageLink.Id },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.LinkOnHardRemovingLinkByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    storageLink.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageLink);

            this.storageBrokerMock.Setup(broker =>
                broker.DeleteLinkAsync(storageLink, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(deletedLink);

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(Guid.NewGuid());

            this.eventBrokerMock.Setup(broker =>
                broker.PublishLinkAsync(
                    It.IsAny<EventEnvelope<Link>>(),
                    LinkEventOperation.HardRemoved))
                    .Returns(new ValueTask<EventPublishResult<Link>>(
                        new EventPublishResult<Link>()));

            // when
            EventEnvelope<Link>? actualReplyEnvelope =
                await this.linkService.OnHardRemovingLinkByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedLink);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.LinkOnHardRemovingLinkByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    storageLink.Id,
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
                        processedEvent.EventId == requestEnvelope.Metadata.EventId
                            && processedEvent.ReceiverName ==
                                EventBrokerIdentifiers.LinkOnHardRemovingLinkByIdSubscriptionName),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.LinkOnHardRemovingLinkByIdSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldSkipHardRemoveAndReplyNullWhenHardRemovingLinkByIdEventAlreadyProcessedAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<Link>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new Link { Id = Guid.NewGuid() },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.LinkOnHardRemovingLinkByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<Link>? actualReplyEnvelope =
                await this.linkService.OnHardRemovingLinkByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: no work, no fact, no reply — the duplicate is acknowledged silently
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.LinkOnHardRemovingLinkByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
