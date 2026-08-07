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
                CreateAuthenticatedSecurityContext(Roles.Publisher);

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

            // approve refuses the author (HR-2), so the actor must be somebody else
            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(GetRandomString());

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
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            Association storageAssociation = CreateApprovableStorageAssociation();
            storageAssociation.EntityAType = EntityType.ContentItem;
            storageAssociation.EntityAContentType = ContentType.Story;
            storageAssociation.EntityAScope = Scope.AllVersions;
            storageAssociation.EntityBType = EntityType.Tag;
            storageAssociation.EntityBContentType = null;
            storageAssociation.EntityBScope = Scope.AllVersions;

            // The service copies the approval fields ONTO the instance the storage broker
            // hands back, so `storageAssociation` is mutated in place by the act. Asserting
            // against it directly would compare the row with itself and pass however the
            // operation behaved — this snapshot is what lets the assertions below fail.
            Association expectedStorageAssociation = storageAssociation.DeepClone();

            // The caller's copy differs from storage on every field approve does not own, and
            // the differences are SET rather than drawn: the fillers pin the endpoint pair and
            // draw the rest, so a drawn value could coincide with storage and quietly turn the
            // assertion for that field into a tautology.
            Association inputAssociation = CreateRandomAssociation();
            inputAssociation.Id = storageAssociation.Id;
            inputAssociation.ApprovalStatus = ApprovalStatus.Approved;
            inputAssociation.IsPublished = true;
            inputAssociation.PublishDate = GetRandomDateTimeOffset();

            inputAssociation.EntityAType = EntityType.Link;
            inputAssociation.EntityAKeyId = Guid.NewGuid();
            inputAssociation.EntityAGroupId = Guid.NewGuid();
            inputAssociation.EntityAContentType = null;
            inputAssociation.EntityAScope = Scope.ThisVersionOnly;
            inputAssociation.EntityBType = EntityType.ContentItem;
            inputAssociation.EntityBKeyId = Guid.NewGuid();
            inputAssociation.EntityBGroupId = Guid.NewGuid();
            inputAssociation.EntityBContentType = ContentType.Testimony;
            inputAssociation.EntityBScope = Scope.ThisVersionOnly;

            inputAssociation.UserId = $"caller-{Guid.NewGuid()}";
            inputAssociation.SortOrder = expectedStorageAssociation.SortOrder + 1;
            inputAssociation.ConfidenceScore = expectedStorageAssociation.ConfidenceScore + 1;
            inputAssociation.ConfidenceReason = $"caller-{Guid.NewGuid()}";
            inputAssociation.SourceBatchId = Guid.NewGuid();
            inputAssociation.ModelVersion = $"caller-{Guid.NewGuid()}";
            inputAssociation.CreatedBy = $"caller-{Guid.NewGuid()}";
            inputAssociation.CreatedWhen =
                expectedStorageAssociation.CreatedWhen.AddDays(1);

            Association savedAssociation = null;

            // approve refuses the author (HR-2), so the actor must be somebody else
            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(GetRandomString());

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

            // everything else came from STORAGE, not from the caller — asserted against the
            // pre-act snapshot, so copying a caller field onto the row fails here
            savedAssociation.EntityAType.Should().Be(expectedStorageAssociation.EntityAType);
            savedAssociation.EntityAKeyId.Should().Be(expectedStorageAssociation.EntityAKeyId);
            savedAssociation.EntityAGroupId.Should().Be(expectedStorageAssociation.EntityAGroupId);
            savedAssociation.EntityAScope.Should().Be(expectedStorageAssociation.EntityAScope);
            savedAssociation.EntityAContentType.Should().Be(expectedStorageAssociation.EntityAContentType);
            savedAssociation.EntityBType.Should().Be(expectedStorageAssociation.EntityBType);
            savedAssociation.EntityBKeyId.Should().Be(expectedStorageAssociation.EntityBKeyId);
            savedAssociation.EntityBGroupId.Should().Be(expectedStorageAssociation.EntityBGroupId);
            savedAssociation.EntityBScope.Should().Be(expectedStorageAssociation.EntityBScope);
            savedAssociation.EntityBContentType.Should().Be(expectedStorageAssociation.EntityBContentType);
            savedAssociation.UserId.Should().Be(expectedStorageAssociation.UserId);
            savedAssociation.SortOrder.Should().Be(expectedStorageAssociation.SortOrder);
            savedAssociation.ConfidenceScore.Should().Be(expectedStorageAssociation.ConfidenceScore);
            savedAssociation.ConfidenceReason.Should().Be(expectedStorageAssociation.ConfidenceReason);
            savedAssociation.SourceBatchId.Should().Be(expectedStorageAssociation.SourceBatchId);
            savedAssociation.ModelVersion.Should().Be(expectedStorageAssociation.ModelVersion);
            savedAssociation.CreatedBy.Should().Be(expectedStorageAssociation.CreatedBy);
            savedAssociation.CreatedWhen.Should().Be(expectedStorageAssociation.CreatedWhen);
        }
    }
}
