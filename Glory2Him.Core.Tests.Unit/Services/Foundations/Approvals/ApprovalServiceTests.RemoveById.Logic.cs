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
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;
using Glory2Him.Core.Models.Enums;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Approvals
{
    public partial class ApprovalServiceTests
    {
        [Fact]
        public async Task ShouldRemoveApprovalByIdAsync()
        {
            // given
            Approval randomApproval = CreateRandomApproval();
            randomApproval.IsDeleted = false;
            Approval storageApproval = randomApproval;

            Approval auditedApproval = storageApproval.DeepClone();
            auditedApproval.IsDeleted = true;

            Approval expectedApproval = auditedApproval.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(
                    randomApproval.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageApproval.CreatedBy);

            // The gate reads the ENTITY's author now, not the approval's: the workflow
            // owns approval rows, so Approval.CreatedBy records the system. Same person,
            // resolved from the row that really has an owner.
            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveEntityAuthorAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval.CreatedBy);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApproval, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedApproval);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalAsync(auditedApproval, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedApproval);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalAsync(
                    It.IsAny<EventEnvelope<Approval>>(),
                    ApprovalEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<Approval>>(
                        new EventPublishResult<Approval>()));

            // when
            Approval actualApproval =
                await this.approvalService.RemoveApprovalByIdAsync(
                    randomApproval.Id,
                    deletionReason: null,
                    TestContext.Current.CancellationToken);

            // then
            actualApproval.Should().BeEquivalentTo(expectedApproval);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByIdAsync(
                    randomApproval.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApproval, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalAsync(auditedApproval, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalAsync(
                    It.IsAny<EventEnvelope<Approval>>(),
                    ApprovalEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalOnRemovingApprovalByIdSubscriptionName),
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
        public async Task ShouldRemoveApprovalByIdWithDeletionReasonAsync()
        {
            // given
            string someDeletionReason = GetRandomString();
            Approval randomApproval = CreateRandomApproval();
            randomApproval.IsDeleted = false;
            Approval storageApproval = randomApproval;

            Approval auditedApproval = storageApproval.DeepClone();
            auditedApproval.IsDeleted = true;
            auditedApproval.DeletionReason = someDeletionReason;

            Approval expectedApproval = auditedApproval.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(
                    randomApproval.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageApproval.CreatedBy);

            // The gate reads the ENTITY's author now, not the approval's: the workflow
            // owns approval rows, so Approval.CreatedBy records the system. Same person,
            // resolved from the row that really has an owner.
            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveEntityAuthorAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval.CreatedBy);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApproval, It.IsAny<SecurityContext>(), someDeletionReason))
                    .ReturnsAsync(auditedApproval);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalAsync(auditedApproval, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedApproval);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalAsync(
                    It.IsAny<EventEnvelope<Approval>>(),
                    ApprovalEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<Approval>>(
                        new EventPublishResult<Approval>()));

            // when
            Approval actualApproval =
                await this.approvalService.RemoveApprovalByIdAsync(
                    randomApproval.Id,
                    deletionReason: someDeletionReason,
                    TestContext.Current.CancellationToken);

            // then
            actualApproval.Should().BeEquivalentTo(expectedApproval);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByIdAsync(
                    randomApproval.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApproval, It.IsAny<SecurityContext>(), someDeletionReason),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalAsync(auditedApproval, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalAsync(
                    It.IsAny<EventEnvelope<Approval>>(),
                    ApprovalEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalOnRemovingApprovalByIdSubscriptionName),
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
            Approval alreadyDeletedApproval = CreateRandomApproval();
            alreadyDeletedApproval.IsDeleted = true;
            Guid someApprovalId = alreadyDeletedApproval.Id;
            Approval expectedApproval = alreadyDeletedApproval;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(
                    someApprovalId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(alreadyDeletedApproval);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(alreadyDeletedApproval.CreatedBy);

            // The gate reads the ENTITY's author now, not the approval's: the workflow
            // owns approval rows, so Approval.CreatedBy records the system. Same person,
            // resolved from the row that really has an owner.
            this.accessBrokerMock.Setup(broker =>
                broker.RetrieveEntityAuthorAsync(
                    It.IsAny<EntityType>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(alreadyDeletedApproval.CreatedBy);

            // when
            Approval actualApproval =
                await this.approvalService.RemoveApprovalByIdAsync(
                    someApprovalId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualApproval.Should().BeEquivalentTo(expectedApproval);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByIdAsync(
                    someApprovalId,
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
        public async Task ShouldRemoveSomeoneElsesApprovalByIdWhenUserIsAdminAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            string randomActorUserId = GetRandomString();
            Approval randomApproval = CreateRandomApproval();
            randomApproval.IsDeleted = false;
            Approval storageApproval = randomApproval;

            Approval auditedApproval = storageApproval.DeepClone();
            auditedApproval.IsDeleted = true;

            Approval expectedApproval = auditedApproval.DeepClone();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalByIdAsync(
                    randomApproval.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageApproval);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApproval, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(auditedApproval);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateApprovalAsync(auditedApproval, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedApproval);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishApprovalAsync(
                    It.IsAny<EventEnvelope<Approval>>(),
                    ApprovalEventOperation.Removed))
                    .Returns(new ValueTask<EventPublishResult<Approval>>(
                        new EventPublishResult<Approval>()));

            // when
            Approval actualApproval =
                await this.approvalService.RemoveApprovalByIdAsync(
                    randomApproval.Id,
                    deletionReason: null,
                    TestContext.Current.CancellationToken);

            // then
            actualApproval.Should().BeEquivalentTo(expectedApproval);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalByIdAsync(
                    randomApproval.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyRemoveAuditValuesAsync(storageApproval, It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateApprovalAsync(auditedApproval, It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishApprovalAsync(
                    It.IsAny<EventEnvelope<Approval>>(),
                    ApprovalEventOperation.Removed),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers.ApprovalOnRemovingApprovalByIdSubscriptionName),
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
