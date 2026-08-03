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
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingReviewerRoles
{
    public partial class ApprovalSettingReviewerRoleServiceTests
    {
        [Fact]
        public async Task ShouldRemoveApprovalSettingReviewerRoleByIdAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            ApprovalSettingReviewerRole randomApprovalSettingReviewerRole = CreateRandomApprovalSettingReviewerRole();
            randomApprovalSettingReviewerRole.IsDeleted = false;
            ApprovalSettingReviewerRole storageApprovalSettingReviewerRole = randomApprovalSettingReviewerRole;

            ApprovalSettingReviewerRole auditedApprovalSettingReviewerRole = storageApprovalSettingReviewerRole.DeepClone();
            auditedApprovalSettingReviewerRole.IsDeleted = true;

            ApprovalSettingReviewerRole expectedApprovalSettingReviewerRole = auditedApprovalSettingReviewerRole.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    randomApprovalSettingReviewerRole.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalSettingReviewerRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApprovalSettingReviewerRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedApprovalSettingReviewerRole);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalSettingReviewerRoleAsync(auditedApprovalSettingReviewerRole, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedApprovalSettingReviewerRole);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalSettingReviewerRoleAsync(
                    It.IsAny<EventEnvelope<ApprovalSettingReviewerRole>>(),
                    ApprovalSettingReviewerRoleEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<ApprovalSettingReviewerRole>>(
                        new EventPublishResult<ApprovalSettingReviewerRole>()));

            // when
            ApprovalSettingReviewerRole actualApprovalSettingReviewerRole =
                await this.approvalSettingReviewerRoleService.RemoveApprovalSettingReviewerRoleByIdAsync(
                    randomApprovalSettingReviewerRole.Id,
                    deletionReason: null,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSettingReviewerRole.Should().BeEquivalentTo(expectedApprovalSettingReviewerRole);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    randomApprovalSettingReviewerRole.Id,
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
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnRemovingApprovalSettingReviewerRoleByIdSubscriptionName),
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
        public async Task ShouldRemoveApprovalSettingReviewerRoleByIdWithDeletionReasonAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            string someDeletionReason = GetRandomString();
            ApprovalSettingReviewerRole randomApprovalSettingReviewerRole = CreateRandomApprovalSettingReviewerRole();
            randomApprovalSettingReviewerRole.IsDeleted = false;
            ApprovalSettingReviewerRole storageApprovalSettingReviewerRole = randomApprovalSettingReviewerRole;

            ApprovalSettingReviewerRole auditedApprovalSettingReviewerRole = storageApprovalSettingReviewerRole.DeepClone();
            auditedApprovalSettingReviewerRole.IsDeleted = true;
            auditedApprovalSettingReviewerRole.DeletionReason = someDeletionReason;

            ApprovalSettingReviewerRole expectedApprovalSettingReviewerRole = auditedApprovalSettingReviewerRole.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    randomApprovalSettingReviewerRole.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApprovalSettingReviewerRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApprovalSettingReviewerRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedApprovalSettingReviewerRole);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalSettingReviewerRoleAsync(auditedApprovalSettingReviewerRole, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedApprovalSettingReviewerRole);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalSettingReviewerRoleAsync(
                    It.IsAny<EventEnvelope<ApprovalSettingReviewerRole>>(),
                    ApprovalSettingReviewerRoleEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<ApprovalSettingReviewerRole>>(
                        new EventPublishResult<ApprovalSettingReviewerRole>()));

            // when
            ApprovalSettingReviewerRole actualApprovalSettingReviewerRole =
                await this.approvalSettingReviewerRoleService.RemoveApprovalSettingReviewerRoleByIdAsync(
                    randomApprovalSettingReviewerRole.Id,
                    deletionReason: someDeletionReason,
                    TestContext.Current.CancellationToken);

            // then
            actualApprovalSettingReviewerRole.Should().BeEquivalentTo(expectedApprovalSettingReviewerRole);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    randomApprovalSettingReviewerRole.Id,
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
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalSettingReviewerRoleOnRemovingApprovalSettingReviewerRoleByIdSubscriptionName),
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            ApprovalSettingReviewerRole alreadyDeletedApprovalSettingReviewerRole = CreateRandomApprovalSettingReviewerRole();
            alreadyDeletedApprovalSettingReviewerRole.IsDeleted = true;
            Guid someApprovalSettingReviewerRoleId = alreadyDeletedApprovalSettingReviewerRole.Id;
            ApprovalSettingReviewerRole expectedApprovalSettingReviewerRole = alreadyDeletedApprovalSettingReviewerRole;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    someApprovalSettingReviewerRoleId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(alreadyDeletedApprovalSettingReviewerRole);

            // when
            ApprovalSettingReviewerRole actualApprovalSettingReviewerRole =
                await this.approvalSettingReviewerRoleService.RemoveApprovalSettingReviewerRoleByIdAsync(
                    someApprovalSettingReviewerRoleId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualApprovalSettingReviewerRole.Should().BeEquivalentTo(expectedApprovalSettingReviewerRole);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    someApprovalSettingReviewerRoleId,
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
