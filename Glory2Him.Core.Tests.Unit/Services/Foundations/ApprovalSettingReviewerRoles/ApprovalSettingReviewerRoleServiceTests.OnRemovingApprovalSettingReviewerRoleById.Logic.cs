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
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingReviewerRoles
{
    public partial class ApprovalSettingReviewerRoleServiceTests
    {
        [Fact]
        public async Task ShouldRemoveApprovalSettingReviewerRoleByIdAndReplyOnRemovingApprovalSettingReviewerRoleByIdEventAsync()
        {
            // given
            string randomDeletionReason = GetRandomString();
            ApprovalSettingReviewerRole storageApprovalSettingReviewerRole = CreateRandomApprovalSettingReviewerRole();
            storageApprovalSettingReviewerRole.IsDeleted = false;
            ApprovalSettingReviewerRole auditedApprovalSettingReviewerRole = storageApprovalSettingReviewerRole.DeepClone();
            ApprovalSettingReviewerRole removedApprovalSettingReviewerRole = auditedApprovalSettingReviewerRole.DeepClone();
            ApprovalSettingReviewerRole expectedApprovalSettingReviewerRole = removedApprovalSettingReviewerRole.DeepClone();

            var requestEnvelope = new EventEnvelope<ApprovalSettingReviewerRole>
            {
                Content = new ApprovalSettingReviewerRole
                {
                    Id = storageApprovalSettingReviewerRole.Id,
                    DeletionReason = randomDeletionReason
                },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnRemovingApprovalSettingReviewerRoleByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    storageApprovalSettingReviewerRole.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalSettingReviewerRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApprovalSettingReviewerRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedApprovalSettingReviewerRole);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalSettingReviewerRoleAsync(auditedApprovalSettingReviewerRole, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(removedApprovalSettingReviewerRole);

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(Guid.NewGuid());

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalSettingReviewerRoleAsync(
                    It.IsAny<EventEnvelope<ApprovalSettingReviewerRole>>(),
                    ApprovalSettingReviewerRoleEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<ApprovalSettingReviewerRole>>(
                        new EventPublishResult<ApprovalSettingReviewerRole>()));

            // when
            EventEnvelope<ApprovalSettingReviewerRole>? actualReplyEnvelope =
                await this.approvalSettingReviewerRoleService.OnRemovingApprovalSettingReviewerRoleByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedApprovalSettingReviewerRole);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnRemovingApprovalSettingReviewerRoleByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    storageApprovalSettingReviewerRole.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApprovalSettingReviewerRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalSettingReviewerRoleAsync(auditedApprovalSettingReviewerRole, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalSettingReviewerRoleAsync(
                    It.IsAny<EventEnvelope<ApprovalSettingReviewerRole>>(),
                    ApprovalSettingReviewerRoleEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.EventId == requestEnvelope.Metadata.EventId
                            && processedEvent.ReceiverName ==
                                EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnRemovingApprovalSettingReviewerRoleByIdSubscriptionName),
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnRemovingApprovalSettingReviewerRoleByIdSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldSkipRemoveAndReplyNullWhenRemovingApprovalSettingReviewerRoleByIdEventAlreadyProcessedAsync()
        {
            // given
            var requestEnvelope = new EventEnvelope<ApprovalSettingReviewerRole>
            {
                Content = new ApprovalSettingReviewerRole { Id = Guid.NewGuid() },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnRemovingApprovalSettingReviewerRoleByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(true);

            // when
            EventEnvelope<ApprovalSettingReviewerRole>? actualReplyEnvelope =
                await this.approvalSettingReviewerRoleService.OnRemovingApprovalSettingReviewerRoleByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: no work, no fact, no reply — the duplicate is acknowledged silently
            actualReplyEnvelope.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnRemovingApprovalSettingReviewerRoleByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldReplyWithExistingApprovalSettingReviewerRoleOnRemovingApprovalSettingReviewerRoleByIdEventWhenAlreadyDeletedAsync()
        {
            // given
            ApprovalSettingReviewerRole alreadyDeletedApprovalSettingReviewerRole = CreateRandomApprovalSettingReviewerRole();
            alreadyDeletedApprovalSettingReviewerRole.IsDeleted = true;
            ApprovalSettingReviewerRole expectedApprovalSettingReviewerRole = alreadyDeletedApprovalSettingReviewerRole.DeepClone();

            var requestEnvelope = new EventEnvelope<ApprovalSettingReviewerRole>
            {
                Content = new ApprovalSettingReviewerRole { Id = alreadyDeletedApprovalSettingReviewerRole.Id },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnRemovingApprovalSettingReviewerRoleByIdSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(false);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    alreadyDeletedApprovalSettingReviewerRole.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(alreadyDeletedApprovalSettingReviewerRole);

            // when
            EventEnvelope<ApprovalSettingReviewerRole>? actualReplyEnvelope =
                await this.approvalSettingReviewerRoleService.OnRemovingApprovalSettingReviewerRoleByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then: already deleted — no mutation happened, so no fact is published and
            // nothing is recorded as processed; the existing entity is returned as the reply.
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedApprovalSettingReviewerRole);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnRemovingApprovalSettingReviewerRoleByIdSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    alreadyDeletedApprovalSettingReviewerRole.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
