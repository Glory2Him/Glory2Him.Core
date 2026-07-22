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
        public async Task ShouldRemoveApprovalSettingRoleByIdAsync()
        {
            // given
            ApprovalSettingRole randomApprovalSettingRole = CreateRandomApprovalSettingRole();
            randomApprovalSettingRole.IsDeleted = false;
            ApprovalSettingRole storageApprovalSettingRole = randomApprovalSettingRole;

            ApprovalSettingRole auditedApprovalSettingRole = storageApprovalSettingRole.DeepClone();
            auditedApprovalSettingRole.IsDeleted = true;

            ApprovalSettingRole expectedApprovalSettingRole = auditedApprovalSettingRole.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingRoleByIdAsync(
                    randomApprovalSettingRole.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalSettingRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApprovalSettingRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedApprovalSettingRole);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalSettingRoleAsync(auditedApprovalSettingRole, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedApprovalSettingRole);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalSettingRoleAsync(
                    It.IsAny<EventEnvelope<ApprovalSettingRole>>(),
                    ApprovalSettingRoleEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<ApprovalSettingRole>>(
                        new EventPublishResult<ApprovalSettingRole>()));

            // when
            ApprovalSettingRole actualApprovalSettingRole =
                await this.approvalSettingRoleService.RemoveApprovalSettingRoleByIdAsync(
                    randomApprovalSettingRole.Id,
                    deletionReason: null,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSettingRole.Should().BeEquivalentTo(expectedApprovalSettingRole);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingRoleByIdAsync(
                    randomApprovalSettingRole.Id,
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
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalSettingRoleOnRemovingApprovalSettingRoleByIdSubscriptionName),
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
        public async Task ShouldRemoveApprovalSettingRoleByIdWithDeletionReasonAsync()
        {
            // given
            string someDeletionReason = GetRandomString();
            ApprovalSettingRole randomApprovalSettingRole = CreateRandomApprovalSettingRole();
            randomApprovalSettingRole.IsDeleted = false;
            ApprovalSettingRole storageApprovalSettingRole = randomApprovalSettingRole;

            ApprovalSettingRole auditedApprovalSettingRole = storageApprovalSettingRole.DeepClone();
            auditedApprovalSettingRole.IsDeleted = true;
            auditedApprovalSettingRole.DeletionReason = someDeletionReason;

            ApprovalSettingRole expectedApprovalSettingRole = auditedApprovalSettingRole.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingRoleByIdAsync(
                    randomApprovalSettingRole.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalSettingRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApprovalSettingRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedApprovalSettingRole);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalSettingRoleAsync(auditedApprovalSettingRole, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedApprovalSettingRole);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalSettingRoleAsync(
                    It.IsAny<EventEnvelope<ApprovalSettingRole>>(),
                    ApprovalSettingRoleEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<ApprovalSettingRole>>(
                        new EventPublishResult<ApprovalSettingRole>()));

            // when
            ApprovalSettingRole actualApprovalSettingRole =
                await this.approvalSettingRoleService.RemoveApprovalSettingRoleByIdAsync(
                    randomApprovalSettingRole.Id,
                    deletionReason: someDeletionReason,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSettingRole.Should().BeEquivalentTo(expectedApprovalSettingRole);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingRoleByIdAsync(
                    randomApprovalSettingRole.Id,
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
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalSettingRoleOnRemovingApprovalSettingRoleByIdSubscriptionName),
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
            ApprovalSettingRole alreadyDeletedApprovalSettingRole = CreateRandomApprovalSettingRole();
            alreadyDeletedApprovalSettingRole.IsDeleted = true;
            Guid someApprovalSettingRoleId = alreadyDeletedApprovalSettingRole.Id;
            ApprovalSettingRole expectedApprovalSettingRole = alreadyDeletedApprovalSettingRole;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingRoleByIdAsync(
                    someApprovalSettingRoleId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(alreadyDeletedApprovalSettingRole);

            // when
            ApprovalSettingRole actualApprovalSettingRole =
                await this.approvalSettingRoleService.RemoveApprovalSettingRoleByIdAsync(
                    someApprovalSettingRoleId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualApprovalSettingRole.Should().BeEquivalentTo(expectedApprovalSettingRole);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingRoleByIdAsync(
                    someApprovalSettingRoleId,
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
