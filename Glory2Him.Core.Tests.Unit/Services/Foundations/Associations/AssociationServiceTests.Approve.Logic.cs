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
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        [Fact]
        public async Task ShouldApproveAssociationAsync()
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Reviewer);

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Association storageAssociation = CreateApprovableStorageAssociation();
            Association inputAssociation = CreateApprovalDecision(storageAssociation.Id);

            Association approvedAssociation = storageAssociation.DeepClone();
            approvedAssociation.ApprovalStatus = inputAssociation.ApprovalStatus;
            approvedAssociation.IsPublished = inputAssociation.IsPublished;
            approvedAssociation.PublishDate = inputAssociation.PublishDate;

            Association auditAppliedAssociation = approvedAssociation.DeepClone();
            Association updatedAssociation = auditAppliedAssociation.DeepClone();
            Association expectedAssociation = updatedAssociation.DeepClone();

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    inputAssociation.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Association>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedAssociation);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAssociationAsync(
                    auditAppliedAssociation,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(updatedAssociation);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    AssociationEventOperation.Approved))
                        .Returns(new ValueTask<EventPublishResult<Association>>(
                            new EventPublishResult<Association>()));

            // when
            Association actualAssociation =
                await this.associationService.ApproveAssociationAsync(
                    inputAssociation,
                    TestContext.Current.CancellationToken);

            // then
            actualAssociation.Should().BeEquivalentTo(expectedAssociation);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectAssociationByIdAsync(
                        inputAssociation.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyModifyAuditValuesAsync(
                        It.IsAny<Association>(),
                        It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateAssociationAsync(
                        auditAppliedAssociation,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            // the operation's OWN fact — never Modified. See ShouldNeverPublishModified...
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishAssociationAsync(
                        It.IsAny<EventEnvelope<Association>>(),
                        AssociationEventOperation.Approved),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .AssociationOnApprovingAssociationSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.AtLeastOnce);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldSaveOnlyTheApprovalFieldsFromTheCallerOnApproveAsync()
        {
            // given: the caller sends a FULLY populated entity whose every other field differs
            // from storage. Approve owns IApproval and nothing else, so the saved row must take
            // the three approval values from the caller and everything else from storage.
            //
            // Without this test the operation could quietly behave like a general modify, which
            // is exactly what the narrow operations exist to prevent — a reviewer approving a
            // row would silently overwrite its content in the same write.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Reviewer);

            Association storageAssociation = CreateApprovableStorageAssociation();

            Association inputAssociation = CreateRandomAssociation();
            inputAssociation.Id = storageAssociation.Id;
            inputAssociation.ApprovalStatus = ApprovalStatus.Approved;
            inputAssociation.IsPublished = true;
            inputAssociation.PublishDate = GetRandomDateTimeOffset();

            Association savedAssociation = null;

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    inputAssociation.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Association>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((Association entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Association entity, CancellationToken _) =>
                        {
                            savedAssociation = entity.DeepClone();

                            return entity;
                        });

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    AssociationEventOperation.Approved))
                        .Returns(new ValueTask<EventPublishResult<Association>>(
                            new EventPublishResult<Association>()));

            // when
            await this.associationService.ApproveAssociationAsync(
                inputAssociation,
                TestContext.Current.CancellationToken);

            // then
            savedAssociation.Should().NotBeNull();

            // the three fields the operation owns came from the caller
            savedAssociation.ApprovalStatus.Should().Be(inputAssociation.ApprovalStatus);
            savedAssociation.IsPublished.Should().Be(inputAssociation.IsPublished);
            savedAssociation.PublishDate.Should().Be(inputAssociation.PublishDate);

            // everything else came from STORAGE, not from the caller
            savedAssociation.EntityAType.Should().Be(storageAssociation.EntityAType);
            savedAssociation.EntityAKeyId.Should().Be(storageAssociation.EntityAKeyId);
            savedAssociation.EntityAGroupId.Should().Be(storageAssociation.EntityAGroupId);
            savedAssociation.EntityAScope.Should().Be(storageAssociation.EntityAScope);
            savedAssociation.EntityAContentType.Should().Be(storageAssociation.EntityAContentType);
            savedAssociation.EntityBType.Should().Be(storageAssociation.EntityBType);
            savedAssociation.EntityBKeyId.Should().Be(storageAssociation.EntityBKeyId);
            savedAssociation.EntityBGroupId.Should().Be(storageAssociation.EntityBGroupId);
            savedAssociation.EntityBScope.Should().Be(storageAssociation.EntityBScope);
            savedAssociation.EntityBContentType.Should().Be(storageAssociation.EntityBContentType);
            savedAssociation.UserId.Should().Be(storageAssociation.UserId);
            savedAssociation.SortOrder.Should().Be(storageAssociation.SortOrder);
            savedAssociation.ConfidenceScore.Should().Be(storageAssociation.ConfidenceScore);
            savedAssociation.ConfidenceReason.Should().Be(storageAssociation.ConfidenceReason);
            savedAssociation.SourceBatchId.Should().Be(storageAssociation.SourceBatchId);
            savedAssociation.ModelVersion.Should().Be(storageAssociation.ModelVersion);
            savedAssociation.CreatedBy.Should().Be(storageAssociation.CreatedBy);
            savedAssociation.CreatedWhen.Should().Be(storageAssociation.CreatedWhen);
        }
    }
}
