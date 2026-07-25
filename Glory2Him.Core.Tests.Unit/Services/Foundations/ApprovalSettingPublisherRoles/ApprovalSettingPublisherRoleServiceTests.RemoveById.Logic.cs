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
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingPublisherRoles
{
    public partial class ApprovalSettingPublisherRoleServiceTests
    {
        [Fact]
        public async Task ShouldRemoveApprovalSettingPublisherRoleByIdAsync()
        {
            // given
            ApprovalSettingPublisherRole randomApprovalSettingPublisherRole = CreateRandomApprovalSettingPublisherRole();
            randomApprovalSettingPublisherRole.IsDeleted = false;
            ApprovalSettingPublisherRole storageApprovalSettingPublisherRole = randomApprovalSettingPublisherRole;

            ApprovalSettingPublisherRole auditedApprovalSettingPublisherRole = storageApprovalSettingPublisherRole.DeepClone();
            auditedApprovalSettingPublisherRole.IsDeleted = true;

            ApprovalSettingPublisherRole expectedApprovalSettingPublisherRole = auditedApprovalSettingPublisherRole.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    randomApprovalSettingPublisherRole.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalSettingPublisherRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApprovalSettingPublisherRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedApprovalSettingPublisherRole);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalSettingPublisherRoleAsync(auditedApprovalSettingPublisherRole, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedApprovalSettingPublisherRole);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalSettingPublisherRoleAsync(
                    It.IsAny<EventEnvelope<ApprovalSettingPublisherRole>>(),
                    ApprovalSettingPublisherRoleEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<ApprovalSettingPublisherRole>>(
                        new EventPublishResult<ApprovalSettingPublisherRole>()));

            // when
            ApprovalSettingPublisherRole actualApprovalSettingPublisherRole =
                await this.approvalSettingPublisherRoleService.RemoveApprovalSettingPublisherRoleByIdAsync(
                    randomApprovalSettingPublisherRole.Id,
                    deletionReason: null,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSettingPublisherRole.Should().BeEquivalentTo(expectedApprovalSettingPublisherRole);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    randomApprovalSettingPublisherRole.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApprovalSettingPublisherRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalSettingPublisherRoleAsync(auditedApprovalSettingPublisherRole, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalSettingPublisherRoleAsync(
                    It.IsAny<EventEnvelope<ApprovalSettingPublisherRole>>(),
                    ApprovalSettingPublisherRoleEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnRemovingApprovalSettingPublisherRoleByIdSubscriptionName),
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
        public async Task ShouldRemoveApprovalSettingPublisherRoleByIdWithDeletionReasonAsync()
        {
            // given
            string someDeletionReason = GetRandomString();
            ApprovalSettingPublisherRole randomApprovalSettingPublisherRole = CreateRandomApprovalSettingPublisherRole();
            randomApprovalSettingPublisherRole.IsDeleted = false;
            ApprovalSettingPublisherRole storageApprovalSettingPublisherRole = randomApprovalSettingPublisherRole;

            ApprovalSettingPublisherRole auditedApprovalSettingPublisherRole = storageApprovalSettingPublisherRole.DeepClone();
            auditedApprovalSettingPublisherRole.IsDeleted = true;
            auditedApprovalSettingPublisherRole.DeletionReason = someDeletionReason;

            ApprovalSettingPublisherRole expectedApprovalSettingPublisherRole = auditedApprovalSettingPublisherRole.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    randomApprovalSettingPublisherRole.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalSettingPublisherRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApprovalSettingPublisherRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedApprovalSettingPublisherRole);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalSettingPublisherRoleAsync(auditedApprovalSettingPublisherRole, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedApprovalSettingPublisherRole);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalSettingPublisherRoleAsync(
                    It.IsAny<EventEnvelope<ApprovalSettingPublisherRole>>(),
                    ApprovalSettingPublisherRoleEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<ApprovalSettingPublisherRole>>(
                        new EventPublishResult<ApprovalSettingPublisherRole>()));

            // when
            ApprovalSettingPublisherRole actualApprovalSettingPublisherRole =
                await this.approvalSettingPublisherRoleService.RemoveApprovalSettingPublisherRoleByIdAsync(
                    randomApprovalSettingPublisherRole.Id,
                    deletionReason: someDeletionReason,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSettingPublisherRole.Should().BeEquivalentTo(expectedApprovalSettingPublisherRole);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    randomApprovalSettingPublisherRole.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApprovalSettingPublisherRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalSettingPublisherRoleAsync(auditedApprovalSettingPublisherRole, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalSettingPublisherRoleAsync(
                    It.IsAny<EventEnvelope<ApprovalSettingPublisherRole>>(),
                    ApprovalSettingPublisherRoleEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalSettingPublisherRoleOnRemovingApprovalSettingPublisherRoleByIdSubscriptionName),
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
            ApprovalSettingPublisherRole alreadyDeletedApprovalSettingPublisherRole = CreateRandomApprovalSettingPublisherRole();
            alreadyDeletedApprovalSettingPublisherRole.IsDeleted = true;
            Guid someApprovalSettingPublisherRoleId = alreadyDeletedApprovalSettingPublisherRole.Id;
            ApprovalSettingPublisherRole expectedApprovalSettingPublisherRole = alreadyDeletedApprovalSettingPublisherRole;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    someApprovalSettingPublisherRoleId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(alreadyDeletedApprovalSettingPublisherRole);

            // when
            ApprovalSettingPublisherRole actualApprovalSettingPublisherRole =
                await this.approvalSettingPublisherRoleService.RemoveApprovalSettingPublisherRoleByIdAsync(
                    someApprovalSettingPublisherRoleId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualApprovalSettingPublisherRole.Should().BeEquivalentTo(expectedApprovalSettingPublisherRole);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingPublisherRoleByIdAsync(
                    someApprovalSettingPublisherRoleId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
