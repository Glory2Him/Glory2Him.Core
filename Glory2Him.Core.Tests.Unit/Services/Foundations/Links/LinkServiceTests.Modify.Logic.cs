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
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Links
{
    public partial class LinkServiceTests
    {
        [Fact]
        public async Task ShouldModifyLinkAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Link randomLink = CreateRandomModifyLink(randomDateTimeOffset, randomUserId);
            Link inputLink = randomLink;
            Link auditAppliedLink = inputLink.DeepClone();
            Link storageLink = auditAppliedLink.DeepClone();
            storageLink.UpdatedWhen = storageLink.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            Link auditPreservedLink = auditAppliedLink.DeepClone();
            Link updatedLink = auditPreservedLink.DeepClone();
            Link expectedLink = updatedLink.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputLink, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedLink);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    auditAppliedLink.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    auditAppliedLink,
                    storageLink))
                        .ReturnsAsync(auditPreservedLink);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateLinkAsync(auditPreservedLink, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(updatedLink);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishLinkAsync(
                    It.IsAny<EventEnvelope<Link>>(),
                    LinkEventOperation.Modified))
                    .Returns(new ValueTask<EventPublishResult<Link>>(
                        new EventPublishResult<Link>()));

            // when
            Link actualLink =
                await this.linkService.ModifyLinkAsync(
                    inputLink,
                    TestContext.Current.CancellationToken);

            // then
            actualLink.Should().BeEquivalentTo(expectedLink);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyModifyAuditValuesAsync(inputLink, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectLinkByIdAsync(
                        auditAppliedLink.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                        auditAppliedLink,
                        storageLink),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateLinkAsync(auditPreservedLink, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishLinkAsync(
                        It.IsAny<EventEnvelope<Link>>(),
                        LinkEventOperation.Modified),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.LinkOnModifyingLinkSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldModifyWhenOwnerMovesStatusToSubmittedAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Link randomLink = CreateRandomModifyLink(randomDateTimeOffset, randomUserId);
            randomLink.ApprovalStatus = ApprovalStatus.Submitted;
            Link inputLink = randomLink;
            Link auditAppliedLink = inputLink.DeepClone();
            Link storageLink = auditAppliedLink.DeepClone();
            storageLink.UpdatedWhen = storageLink.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            storageLink.ApprovalStatus = ApprovalStatus.Draft;
            Link auditPreservedLink = auditAppliedLink.DeepClone();
            Link updatedLink = auditPreservedLink.DeepClone();
            Link expectedLink = updatedLink.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputLink, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditAppliedLink);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    auditAppliedLink.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    auditAppliedLink,
                    storageLink))
                        .ReturnsAsync(auditPreservedLink);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateLinkAsync(auditPreservedLink, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(updatedLink);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishLinkAsync(
                    It.IsAny<EventEnvelope<Link>>(),
                    LinkEventOperation.Modified))
                    .Returns(new ValueTask<EventPublishResult<Link>>(
                        new EventPublishResult<Link>()));

            // when
            Link actualLink =
                await this.linkService.ModifyLinkAsync(
                    inputLink,
                    TestContext.Current.CancellationToken);

            // then
            actualLink.Should().BeEquivalentTo(expectedLink);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.Exactly(3));

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyModifyAuditValuesAsync(inputLink, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectLinkByIdAsync(
                        auditAppliedLink.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                        auditAppliedLink,
                        storageLink),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateLinkAsync(auditPreservedLink, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishLinkAsync(
                        It.IsAny<EventEnvelope<Link>>(),
                        LinkEventOperation.Modified),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.LinkOnModifyingLinkSubscriptionName),
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
