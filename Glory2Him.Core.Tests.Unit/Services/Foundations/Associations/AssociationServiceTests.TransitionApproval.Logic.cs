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
using G2H.Security.Client.Models.Foundations.Access;
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
        public async Task ShouldTransitionAssociationApprovalAsync()
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publishers);

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

            // HR-2 is no longer a row-local comparison here: the self-approval bar is
            // governed by ApprovalSetting.AllowSelfApproval and answered by IAccessBroker,
            // which the fixture defaults to permitting. The service therefore never resolves
            // the actor id on this path.
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
                await this.associationService.TransitionAssociationApprovalAsync(
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

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.accessBrokerMock.VerifyNoOtherCalls();
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
                CreateAuthenticatedSecurityContext(Roles.Publishers);

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

            // HR-2 now lives behind the access broker, which the fixture leaves permissive —
            // this test is about which FIELDS are saved, not about who may save them.
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
            await this.associationService.TransitionAssociationApprovalAsync(
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

        // ── The bypass record is DERIVED, not copied ─────────────────────────────────────────
        //
        // Three of the four IApproval members approve owns are taken from the caller. These two
        // are not, and the exception is the whole point of them: the field exists to record that
        // the approval conditions were waived, and a caller who can set it can equally clear it
        // — un-recording the one event it is here to capture.

        [Fact]
        public async Task ShouldIgnoreTheCallersBypassRecordOnApproveAsync()
        {
            // given: the caller claims a bypass it was never granted. The decision came back
            // permitted WITHOUT one, so the saved row must say so — otherwise the flag means
            // "the caller said so" rather than "the rules were waived", and it is evidence of
            // nothing.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publishers);

            Association storageAssociation = CreateApprovableStorageAssociation();
            storageAssociation.IsApprovedByBypass = false;
            storageAssociation.ApprovedByBypassReason = null;

            Association inputAssociation = CreateApprovalDecision(storageAssociation.Id);
            inputAssociation.IsApprovedByBypass = true;
            inputAssociation.ApprovedByBypassReason = "caller supplied";

            SetupAccessBrokerToPermit();

            // when
            Association savedAssociation = await CaptureSavedAssociationOnApproveAsync(
                storageAssociation,
                inputAssociation);

            // then
            savedAssociation.Should().NotBeNull();

            // the decision, not the claim
            savedAssociation.IsApprovedByBypass.Should().BeFalse();
            savedAssociation.ApprovedByBypassReason.Should().BeNull();

            // and the three members approve DOES take from the caller still arrive, so this is
            // a statement about these two fields rather than about the copy being broken
            savedAssociation.ApprovalStatus.Should().Be(inputAssociation.ApprovalStatus);
            savedAssociation.IsPublished.Should().Be(inputAssociation.IsPublished);
            savedAssociation.PublishDate.Should().Be(inputAssociation.PublishDate);
        }

        [Fact]
        public async Task ShouldRecordTheBypassOnTheRowWhenTheDecisionWaivedTheConditionsAsync()
        {
            // given: the mirror image — the caller claims nothing and the DECISION reports a
            // bypass. The flag has to travel from the verdict onto the row, or a genuine bypass
            // leaves no trace at all and the field is dead weight.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publishers);

            Association storageAssociation = CreateApprovableStorageAssociation();
            storageAssociation.IsApprovedByBypass = false;
            storageAssociation.ApprovedByBypassReason = null;

            Association inputAssociation = CreateApprovalDecision(storageAssociation.Id);
            inputAssociation.IsApprovedByBypass = false;
            inputAssociation.ApprovedByBypassReason = null;

            SetupAccessBrokerToPermitByBypass(AccessDenialReason.BlockedByRejection);

            // when
            Association savedAssociation = await CaptureSavedAssociationOnApproveAsync(
                storageAssociation,
                inputAssociation);

            // then
            savedAssociation.Should().NotBeNull();
            savedAssociation.IsApprovedByBypass.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldClearAnEarlierBypassRecordWhenTheRowIsApprovedNormallyAsync()
        {
            // given: a row bypass-approved once already, amended since, and now approved on its
            // merits. Clearing is deliberate rather than incidental — a row that met its
            // conditions this time must stop claiming they were waived, or the flag accumulates
            // and every bypassed entity stays flagged for the rest of its life.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publishers);

            Association storageAssociation = CreateApprovableStorageAssociation();
            storageAssociation.IsApprovedByBypass = true;
            storageAssociation.ApprovedByBypassReason = "an earlier bypass";

            Association inputAssociation = CreateApprovalDecision(storageAssociation.Id);
            inputAssociation.IsApprovedByBypass = false;
            inputAssociation.ApprovedByBypassReason = null;

            SetupAccessBrokerToPermit();

            // when
            Association savedAssociation = await CaptureSavedAssociationOnApproveAsync(
                storageAssociation,
                inputAssociation);

            // then
            savedAssociation.Should().NotBeNull();
            savedAssociation.IsApprovedByBypass.Should().BeFalse();
            savedAssociation.ApprovedByBypassReason.Should().BeNull();
        }

        // Runs a permitted approve end to end and hands back a snapshot of the row that reached
        // the storage broker. The snapshot is taken INSIDE the callback: the service copies onto
        // the instance the storage read handed it, so reading that instance after the act would
        // compare the row with itself and pass however the operation behaved.
        private async ValueTask<Association> CaptureSavedAssociationOnApproveAsync(
            Association storageAssociation,
            Association inputAssociation)
        {
            Association savedAssociation = null;

            SetupStorageRead(storageAssociation);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Association>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((Association entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<Association, CancellationToken>(
                            (entity, _) => savedAssociation = entity.DeepClone())
                        .ReturnsAsync((Association entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishAssociationAsync(
                    It.IsAny<EventEnvelope<Association>>(),
                    It.IsAny<AssociationEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<Association>>(
                            new EventPublishResult<Association>()));

            await this.associationService.TransitionAssociationApprovalAsync(
                inputAssociation,
                TestContext.Current.CancellationToken);

            return savedAssociation;
        }
    }
}
