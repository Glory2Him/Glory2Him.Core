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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    public partial class ContentItemServiceTests
    {
        [Fact]
        public async Task ShouldApproveContentItemAsync()
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ContentItem storageContentItem = CreateApprovableStorageContentItem();
            ContentItem inputContentItem = CreateApprovalDecision(storageContentItem.Id);

            ContentItem approvedContentItem = storageContentItem.DeepClone();
            approvedContentItem.ApprovalStatus = inputContentItem.ApprovalStatus;
            approvedContentItem.IsPublished = inputContentItem.IsPublished;
            approvedContentItem.PublishDate = inputContentItem.PublishDate;
            approvedContentItem.IsApprovedByBypass = false;
            approvedContentItem.ApprovedByBypassReason = null;

            ContentItem auditAppliedContentItem = approvedContentItem.DeepClone();
            ContentItem updatedContentItem = auditAppliedContentItem.DeepClone();
            ContentItem expectedContentItem = updatedContentItem.DeepClone();

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            SetupContentItemStorageRead(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedContentItem);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateContentItemAsync(
                    auditAppliedContentItem,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(updatedContentItem);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemAsync(
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    ContentItemEventOperation.Approved))
                        .Returns(new ValueTask<EventPublishResult<ContentItem>>(
                            new EventPublishResult<ContentItem>()));

            // when
            ContentItem actualContentItem =
                await this.contentItemService.ApproveContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectContentItemByIdAsync(
                        inputContentItem.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyModifyAuditValuesAsync(
                        It.IsAny<ContentItem>(),
                        It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateContentItemAsync(
                        auditAppliedContentItem,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            // the operation's OWN fact — never Modified. See ShouldNeverPublishModified...
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishContentItemAsync(
                        It.IsAny<EventEnvelope<ContentItem>>(),
                        ContentItemEventOperation.Approved),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .ContentItemOnApprovingContentItemSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                    broker.GetCurrentDateTimeOffsetAsync(),
                Times.AtLeastOnce);

            this.accessBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldPublishRejectedWhenTheDecisionRejectsOnApproveAsync()
        {
            // given: the fact follows the DECISION, not the verb. A rejection announced on the
            // Approved address would tell every subscriber the row is live.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            ContentItem storageContentItem = CreateApprovableStorageContentItem();
            ContentItem inputContentItem = CreateRejectionDecision(storageContentItem.Id);

            SetupContentItemStorageRead(storageContentItem);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((ContentItem entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateContentItemAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ContentItem entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemAsync(
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<ContentItemEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<ContentItem>>(
                            new EventPublishResult<ContentItem>()));

            // when
            await this.contentItemService.ApproveContentItemAsync(
                inputContentItem,
                TestContext.Current.CancellationToken);

            // then
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishContentItemAsync(
                        It.IsAny<EventEnvelope<ContentItem>>(),
                        ContentItemEventOperation.Rejected),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishContentItemAsync(
                        It.IsAny<EventEnvelope<ContentItem>>(),
                        ContentItemEventOperation.Approved),
                Times.Never);
        }

        [Fact]
        public async Task ShouldNeverPublishModifiedOnApproveAsync()
        {
            // given: the transitions exist to keep the approval workflow's cycle-breaker intact
            // (design §9.7.1). The workflow subscribes to Modified and causes Approved, so an
            // approve that published Modified would re-enter the handler that caused it. This is
            // issue #111 case 1: assert the published operation explicitly, both ways.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            ContentItem storageContentItem = CreateApprovableStorageContentItem();
            ContentItem inputContentItem = CreateApprovalDecision(storageContentItem.Id);

            SetupContentItemStorageRead(storageContentItem);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((ContentItem entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateContentItemAsync(
                    It.IsAny<ContentItem>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ContentItem entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemAsync(
                    It.IsAny<EventEnvelope<ContentItem>>(),
                    It.IsAny<ContentItemEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<ContentItem>>(
                            new EventPublishResult<ContentItem>()));

            // when
            await this.contentItemService.ApproveContentItemAsync(
                inputContentItem,
                TestContext.Current.CancellationToken);

            // then
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishContentItemAsync(
                        It.IsAny<EventEnvelope<ContentItem>>(),
                        ContentItemEventOperation.Modified),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishContentItemAsync(
                        It.IsAny<EventEnvelope<ContentItem>>(),
                        ContentItemEventOperation.Approved),
                Times.Once);
        }

        [Fact]
        public async Task ShouldSaveOnlyTheApprovalFieldsFromTheCallerOnApproveAsync()
        {
            // given: the caller sends a FULLY populated entity whose content and every other
            // field differs from storage. Approve owns IApproval and nothing else, so the saved
            // row must take the three approval values from the caller and everything else from
            // storage (issue #111 case 2: field scope respected — approving with a mutated Title
            // or Content must not change them).
            //
            // Without this test the operation could quietly behave like a general modify, which
            // is exactly what the narrow operations exist to prevent — a publisher approving a
            // row would silently overwrite its content in the same write.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            ContentItem storageContentItem = CreateApprovableStorageContentItem();

            // The service copies the approval fields ONTO the instance the storage broker hands
            // back, so `storageContentItem` is mutated in place by the act. Asserting against it
            // directly would compare the row with itself and pass however the operation behaved
            // — this snapshot is what lets the assertions below fail.
            ContentItem expectedStorageContentItem = storageContentItem.DeepClone();

            // The caller's copy differs from storage on every field approve does not own, and
            // the differences are SET rather than drawn, so a drawn value could coincide with
            // storage and quietly turn the assertion for that field into a tautology.
            ContentItem inputContentItem = CreateApprovalDecision(storageContentItem.Id);
            inputContentItem.Content = $"caller-{Guid.NewGuid()}";
            inputContentItem.Title = $"caller-{Guid.NewGuid()}";
            inputContentItem.ContentType = ContentType.Testimony;
            inputContentItem.CreatedBy = $"caller-{Guid.NewGuid()}";
            inputContentItem.GroupId = Guid.NewGuid();

            // when
            ContentItem savedContentItem =
                await CaptureSavedContentItemOnApproveAsync(storageContentItem, inputContentItem);

            // then
            savedContentItem.Should().NotBeNull();

            // the three fields the operation owns came from the caller
            savedContentItem.ApprovalStatus.Should().Be(inputContentItem.ApprovalStatus);
            savedContentItem.IsPublished.Should().Be(inputContentItem.IsPublished);
            savedContentItem.PublishDate.Should().Be(inputContentItem.PublishDate);

            // everything else came from STORAGE, not from the caller — asserted against the
            // pre-act snapshot, so copying a caller field onto the row fails here
            savedContentItem.Content.Should().Be(expectedStorageContentItem.Content);
            savedContentItem.Title.Should().Be(expectedStorageContentItem.Title);
            savedContentItem.ContentType.Should().Be(expectedStorageContentItem.ContentType);
            savedContentItem.CreatedBy.Should().Be(expectedStorageContentItem.CreatedBy);
            savedContentItem.GroupId.Should().Be(
                expectedStorageContentItem.GroupId);
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
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            ContentItem storageContentItem = CreateApprovableStorageContentItem();
            storageContentItem.IsApprovedByBypass = false;
            storageContentItem.ApprovedByBypassReason = null;

            ContentItem inputContentItem = CreateApprovalDecision(storageContentItem.Id);
            inputContentItem.IsApprovedByBypass = true;
            inputContentItem.ApprovedByBypassReason = "caller supplied";

            SetupAccessBrokerToPermit();

            // when
            ContentItem savedContentItem =
                await CaptureSavedContentItemOnApproveAsync(storageContentItem, inputContentItem);

            // then
            savedContentItem.Should().NotBeNull();

            // the decision, not the claim
            savedContentItem.IsApprovedByBypass.Should().BeFalse();
            savedContentItem.ApprovedByBypassReason.Should().BeNull();

            // and the three members approve DOES take from the caller still arrive, so this is a
            // statement about these two fields rather than about the copy being broken
            savedContentItem.ApprovalStatus.Should().Be(inputContentItem.ApprovalStatus);
            savedContentItem.IsPublished.Should().Be(inputContentItem.IsPublished);
            savedContentItem.PublishDate.Should().Be(inputContentItem.PublishDate);
        }

        [Fact]
        public async Task ShouldRecordTheBypassOnTheRowWhenTheDecisionWaivedTheConditionsAsync()
        {
            // given: the mirror image — the caller claims nothing and the DECISION reports a
            // bypass. The flag has to travel from the verdict onto the row, or a genuine bypass
            // leaves no trace at all and the field is dead weight.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            ContentItem storageContentItem = CreateApprovableStorageContentItem();
            storageContentItem.IsApprovedByBypass = false;
            storageContentItem.ApprovedByBypassReason = null;

            ContentItem inputContentItem = CreateApprovalDecision(storageContentItem.Id);
            inputContentItem.IsApprovedByBypass = false;
            inputContentItem.ApprovedByBypassReason = null;

            SetupAccessBrokerToPermitByBypass();

            // when
            ContentItem savedContentItem =
                await CaptureSavedContentItemOnApproveAsync(storageContentItem, inputContentItem);

            // then
            savedContentItem.Should().NotBeNull();
            savedContentItem.IsApprovedByBypass.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldClearAnEarlierBypassRecordWhenTheRowIsApprovedNormallyAsync()
        {
            // given: a row bypass-approved once already, amended since, and now approved on its
            // merits. Clearing is deliberate rather than incidental — a row that met its
            // conditions this time must stop claiming they were waived, or the flag accumulates
            // and every bypassed item stays flagged for the rest of its life.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            ContentItem storageContentItem = CreateApprovableStorageContentItem();
            storageContentItem.IsApprovedByBypass = true;
            storageContentItem.ApprovedByBypassReason = "an earlier bypass";

            ContentItem inputContentItem = CreateApprovalDecision(storageContentItem.Id);

            SetupAccessBrokerToPermit();

            // when
            ContentItem savedContentItem =
                await CaptureSavedContentItemOnApproveAsync(storageContentItem, inputContentItem);

            // then
            savedContentItem.Should().NotBeNull();
            savedContentItem.IsApprovedByBypass.Should().BeFalse();
            savedContentItem.ApprovedByBypassReason.Should().BeNull();
        }
    }
}
