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
        public async Task ShouldRemoveLinkByIdAsync()
        {
            // given
            Link randomLink = CreateRandomLink();
            randomLink.IsDeleted = false;
            Link storageLink = randomLink;

            Link auditedLink = storageLink.DeepClone();
            auditedLink.IsDeleted = true;

            Link expectedLink = auditedLink.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    randomLink.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageLink.CreatedBy);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageLink, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedLink);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateLinkAsync(auditedLink, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedLink);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishLinkAsync(
                    It.IsAny<EventEnvelope<Link>>(),
                    LinkEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<Link>>(
                        new EventPublishResult<Link>()));

            // when
            Link actualLink =
                await this.linkService.RemoveLinkByIdAsync(
                    randomLink.Id,
                    deletionReason: null,
                    TestContext.Current.CancellationToken);

            // then
            actualLink.Should().BeEquivalentTo(expectedLink);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    randomLink.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageLink, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateLinkAsync(auditedLink, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishLinkAsync(
                    It.IsAny<EventEnvelope<Link>>(),
                    LinkEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.LinkOnRemovingLinkByIdSubscriptionName),
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
        public async Task ShouldRemoveLinkByIdWithDeletionReasonAsync()
        {
            // given
            string someDeletionReason = GetRandomString();
            Link randomLink = CreateRandomLink();
            randomLink.IsDeleted = false;
            Link storageLink = randomLink;

            Link auditedLink = storageLink.DeepClone();
            auditedLink.IsDeleted = true;
            auditedLink.DeletionReason = someDeletionReason;

            Link expectedLink = auditedLink.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    randomLink.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageLink.CreatedBy);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageLink, It.IsAny<SecurityContext>(), someDeletionReason))
                    .ReturnsAsync(auditedLink);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateLinkAsync(auditedLink, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedLink);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishLinkAsync(
                    It.IsAny<EventEnvelope<Link>>(),
                    LinkEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<Link>>(
                        new EventPublishResult<Link>()));

            // when
            Link actualLink =
                await this.linkService.RemoveLinkByIdAsync(
                    randomLink.Id,
                    deletionReason: someDeletionReason,
                    TestContext.Current.CancellationToken);

            // then
            actualLink.Should().BeEquivalentTo(expectedLink);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    randomLink.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageLink, It.IsAny<SecurityContext>(), someDeletionReason),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateLinkAsync(auditedLink, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishLinkAsync(
                    It.IsAny<EventEnvelope<Link>>(),
                    LinkEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.LinkOnRemovingLinkByIdSubscriptionName),
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
            Link alreadyDeletedLink = CreateRandomLink();
            alreadyDeletedLink.IsDeleted = true;
            Guid someLinkId = alreadyDeletedLink.Id;
            Link expectedLink = alreadyDeletedLink;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    someLinkId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(alreadyDeletedLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(alreadyDeletedLink.CreatedBy);

            // when
            Link actualLink =
                await this.linkService.RemoveLinkByIdAsync(
                    someLinkId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualLink.Should().BeEquivalentTo(expectedLink);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    someLinkId,
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
        public async Task ShouldRemoveSomeoneElsesLinkByIdWhenUserIsAdminAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            string randomActorUserId = GetRandomString();
            Link randomLink = CreateRandomLink();
            randomLink.IsDeleted = false;
            Link storageLink = randomLink;

            Link auditedLink = storageLink.DeepClone();
            auditedLink.IsDeleted = true;

            Link expectedLink = auditedLink.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    randomLink.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageLink, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedLink);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateLinkAsync(auditedLink, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedLink);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishLinkAsync(
                    It.IsAny<EventEnvelope<Link>>(),
                    LinkEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<Link>>(
                        new EventPublishResult<Link>()));

            // when
            Link actualLink =
                await this.linkService.RemoveLinkByIdAsync(
                    randomLink.Id,
                    deletionReason: null,
                    TestContext.Current.CancellationToken);

            // then
            actualLink.Should().BeEquivalentTo(expectedLink);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    randomLink.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageLink, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateLinkAsync(auditedLink, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishLinkAsync(
                    It.IsAny<EventEnvelope<Link>>(),
                    LinkEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.LinkOnRemovingLinkByIdSubscriptionName),
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
