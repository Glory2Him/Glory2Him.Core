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
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingRoles
{
    public partial class ApprovalSettingRoleServiceTests
    {
        [Fact]
        public async Task ShouldRemoveApprovalSettingRoleByIdAndReplyOnRemovingApprovalSettingRoleByIdEventAsync()
        {
            // given
            string randomDeletionReason = GetRandomString();
            ApprovalSettingRole storageApprovalSettingRole = CreateRandomApprovalSettingRole();
            storageApprovalSettingRole.IsDeleted = false;
            ApprovalSettingRole auditedApprovalSettingRole = storageApprovalSettingRole.DeepClone();
            ApprovalSettingRole removedApprovalSettingRole = auditedApprovalSettingRole.DeepClone();
            ApprovalSettingRole expectedApprovalSettingRole = removedApprovalSettingRole.DeepClone();

            var requestEnvelope = new EventEnvelope<ApprovalSettingRole>
            {
                Content = new ApprovalSettingRole
                {
                    Id = storageApprovalSettingRole.Id,
                    DeletionReason = randomDeletionReason
                },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingRoleOnRemovingApprovalSettingRoleByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingRoleByIdAsync(
                    storageApprovalSettingRole.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalSettingRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApprovalSettingRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedApprovalSettingRole);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalSettingRoleAsync(auditedApprovalSettingRole, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(removedApprovalSettingRole);

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(Guid.NewGuid());

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalSettingRoleAsync(
                    It.IsAny<EventEnvelope<ApprovalSettingRole>>(),
                    ApprovalSettingRoleEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<ApprovalSettingRole>>(
                        new EventPublishResult<ApprovalSettingRole>()));

            // when
            EventEnvelope<ApprovalSettingRole>? actualReplyEnvelope =
                await this.approvalSettingRoleService.OnRemovingApprovalSettingRoleByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedApprovalSettingRole);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingRoleOnRemovingApprovalSettingRoleByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingRoleByIdAsync(
                    storageApprovalSettingRole.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApprovalSettingRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalSettingRoleAsync(auditedApprovalSettingRole, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalSettingRoleAsync(
                    It.IsAny<EventEnvelope<ApprovalSettingRole>>(),
                    ApprovalSettingRoleEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.EventId == requestEnvelope.Metadata.EventId
                            && processedEvent.ReceiverName ==
                                EventBrokerIdentifiers.ApprovalSettingRoleOnRemovingApprovalSettingRoleByIdSubscriptionName),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalSettingRoleOnRemovingApprovalSettingRoleByIdSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldSkipRemoveAndReplyNullWhenRemovingApprovalSettingRoleByIdEventAlreadyProcessedAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<ApprovalSettingRole>
            {
                Content = new ApprovalSettingRole { Id = Guid.NewGuid() },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingRoleOnRemovingApprovalSettingRoleByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<ApprovalSettingRole>? actualReplyEnvelope =
                await this.approvalSettingRoleService.OnRemovingApprovalSettingRoleByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: no work, no fact, no reply — the duplicate is acknowledged silently
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingRoleOnRemovingApprovalSettingRoleByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReplyWithExistingApprovalSettingRoleOnRemovingApprovalSettingRoleByIdEventWhenAlreadyDeletedAsync()
        {
            // given
            ApprovalSettingRole alreadyDeletedApprovalSettingRole = CreateRandomApprovalSettingRole();
            alreadyDeletedApprovalSettingRole.IsDeleted = true;
            ApprovalSettingRole expectedApprovalSettingRole = alreadyDeletedApprovalSettingRole.DeepClone();

            var requestEnvelope = new EventEnvelope<ApprovalSettingRole>
            {
                Content = new ApprovalSettingRole { Id = alreadyDeletedApprovalSettingRole.Id },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingRoleOnRemovingApprovalSettingRoleByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingRoleByIdAsync(
                    alreadyDeletedApprovalSettingRole.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(alreadyDeletedApprovalSettingRole);

            // when
            EventEnvelope<ApprovalSettingRole>? actualReplyEnvelope =
                await this.approvalSettingRoleService.OnRemovingApprovalSettingRoleByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: already deleted — no mutation happened, so no fact is published and
            // nothing is recorded as processed; the existing entity is returned as the reply.
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedApprovalSettingRole);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingRoleOnRemovingApprovalSettingRoleByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingRoleByIdAsync(
                    alreadyDeletedApprovalSettingRole.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
