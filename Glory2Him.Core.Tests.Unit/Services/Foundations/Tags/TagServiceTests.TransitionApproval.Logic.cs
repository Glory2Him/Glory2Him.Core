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
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Tags
{
    public partial class TagServiceTests
    {
        [Fact]
        public async Task ShouldTransitionTagApprovalAsync()
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Tag storageTag = CreateApprovableStorageTag();
            Tag inputTag = CreateApprovalDecision(storageTag.Id);

            Tag approvedTag = storageTag.DeepClone();
            approvedTag.ApprovalStatus = inputTag.ApprovalStatus;
            approvedTag.IsPublished = inputTag.IsPublished;
            approvedTag.PublishDate = inputTag.PublishDate;
            approvedTag.IsApprovedByBypass = false;
            approvedTag.ApprovedByBypassReason = null;

            Tag auditAppliedTag = approvedTag.DeepClone();
            Tag updatedTag = auditAppliedTag.DeepClone();
            Tag expectedTag = updatedTag.DeepClone();

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            SetupTagStorageRead(storageTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Tag>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedTag);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateTagAsync(
                    auditAppliedTag,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(updatedTag);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishTagAsync(
                    It.IsAny<EventEnvelope<Tag>>(),
                    TagEventOperation.Approved))
                        .Returns(new ValueTask<EventPublishResult<Tag>>(
                            new EventPublishResult<Tag>()));

            // when
            Tag actualTag =
                await this.tagService.TransitionTagApprovalAsync(
                    inputTag,
                    TestContext.Current.CancellationToken);

            // then
            actualTag.Should().BeEquivalentTo(expectedTag);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectTagByIdAsync(
                        inputTag.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyModifyAuditValuesAsync(
                        It.IsAny<Tag>(),
                        It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateTagAsync(
                        auditAppliedTag,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            // the operation's OWN fact — never Modified. See ShouldNeverPublishModified...
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishTagAsync(
                        It.IsAny<EventEnvelope<Tag>>(),
                        TagEventOperation.Approved),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .TagOnApprovingTagSubscriptionName),
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

            Tag storageTag = CreateApprovableStorageTag();
            Tag inputTag = CreateRejectionDecision(storageTag.Id);

            // when
            await CaptureSavedTagOnTransitionAsync(storageTag, inputTag);

            // then
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishTagAsync(
                        It.IsAny<EventEnvelope<Tag>>(),
                        TagEventOperation.Rejected),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishTagAsync(
                        It.IsAny<EventEnvelope<Tag>>(),
                        TagEventOperation.Approved),
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

            Tag storageTag = CreateApprovableStorageTag();
            Tag inputTag = CreateApprovalDecision(storageTag.Id);

            // when
            await CaptureSavedTagOnTransitionAsync(storageTag, inputTag);

            // then
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishTagAsync(
                        It.IsAny<EventEnvelope<Tag>>(),
                        TagEventOperation.Modified),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishTagAsync(
                        It.IsAny<EventEnvelope<Tag>>(),
                        TagEventOperation.Approved),
                Times.Once);
        }

        [Fact]
        public async Task ShouldSaveOnlyTheApprovalFieldsFromTheCallerOnApproveAsync()
        {
            // given: the caller sends a FULLY populated entity whose every non-approval field
            // differs from storage. Approve owns IApproval and nothing else, so the saved row
            // must take the approval values from the caller and everything else from storage
            // (issue #111 case 2: field scope respected). Asserting the whole row against the
            // pre-act snapshot — excluding only the fields approve owns — catches a stray write
            // on ANY other field, without naming entity-specific columns.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            Tag storageTag = CreateApprovableStorageTag();
            Tag expectedStorageTag = storageTag.DeepClone();

            // a fully random caller copy (differs from storage on every field), pinned only to
            // the id and a valid approval outcome
            Tag inputTag = CreateRandomTag();
            inputTag.Id = storageTag.Id;
            inputTag.ApprovalStatus = ApprovalStatus.Approved;
            inputTag.IsPublished = true;
            inputTag.PublishDate = GetRandomDateTimeOffset();

            // when
            Tag savedTag = await CaptureSavedTagOnTransitionAsync(storageTag, inputTag);

            // then
            savedTag.Should().NotBeNull();

            // the fields the operation owns came from the caller
            savedTag.ApprovalStatus.Should().Be(inputTag.ApprovalStatus);
            savedTag.IsPublished.Should().Be(inputTag.IsPublished);
            savedTag.PublishDate.Should().Be(inputTag.PublishDate);

            // everything else came from STORAGE — asserted against the pre-act snapshot, so
            // copying any caller field onto the row fails here. The bypass pair is derived
            // (false / null here) and excluded from the storage comparison.
            savedTag.Should().BeEquivalentTo(
                expectedStorageTag,
                options => options
                    .Excluding(tag => tag.ApprovalStatus)
                    .Excluding(tag => tag.IsPublished)
                    .Excluding(tag => tag.PublishDate)
                    .Excluding(tag => tag.IsApprovedByBypass)
                    .Excluding(tag => tag.ApprovedByBypassReason));
        }

        // ── The bypass record is DERIVED, not copied ─────────────────────────────────────────

        [Fact]
        public async Task ShouldIgnoreTheCallersBypassRecordOnApproveAsync()
        {
            // given: the caller claims a bypass it was never granted. The decision came back
            // permitted WITHOUT one, so the saved row must say so — otherwise the flag means
            // "the caller said so" rather than "the rules were waived".
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            Tag storageTag = CreateApprovableStorageTag();
            storageTag.IsApprovedByBypass = false;
            storageTag.ApprovedByBypassReason = null;

            Tag inputTag = CreateApprovalDecision(storageTag.Id);
            inputTag.IsApprovedByBypass = true;
            inputTag.ApprovedByBypassReason = "caller supplied";

            SetupAccessBrokerToPermit();

            // when
            Tag savedTag = await CaptureSavedTagOnTransitionAsync(storageTag, inputTag);

            // then
            savedTag.Should().NotBeNull();
            savedTag.IsApprovedByBypass.Should().BeFalse();
            savedTag.ApprovedByBypassReason.Should().BeNull();

            savedTag.ApprovalStatus.Should().Be(inputTag.ApprovalStatus);
            savedTag.IsPublished.Should().Be(inputTag.IsPublished);
            savedTag.PublishDate.Should().Be(inputTag.PublishDate);
        }

        [Fact]
        public async Task ShouldRecordTheBypassOnTheRowWhenTheDecisionWaivedTheConditionsAsync()
        {
            // given: the mirror image — the caller claims nothing and the DECISION reports a
            // bypass. The flag has to travel from the verdict onto the row.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            Tag storageTag = CreateApprovableStorageTag();
            storageTag.IsApprovedByBypass = false;
            storageTag.ApprovedByBypassReason = null;

            Tag inputTag = CreateApprovalDecision(storageTag.Id);
            inputTag.IsApprovedByBypass = false;
            inputTag.ApprovedByBypassReason = null;

            SetupAccessBrokerToPermitByBypass();

            // when
            Tag savedTag = await CaptureSavedTagOnTransitionAsync(storageTag, inputTag);

            // then
            savedTag.Should().NotBeNull();
            savedTag.IsApprovedByBypass.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldClearAnEarlierBypassRecordWhenTheRowIsApprovedNormallyAsync()
        {
            // given: a row bypass-approved once already, amended since, and now approved on its
            // merits. A row that met its conditions this time must stop claiming they were
            // waived, or the flag accumulates for the rest of its life.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publisher);

            Tag storageTag = CreateApprovableStorageTag();
            storageTag.IsApprovedByBypass = true;
            storageTag.ApprovedByBypassReason = "an earlier bypass";

            Tag inputTag = CreateApprovalDecision(storageTag.Id);

            SetupAccessBrokerToPermit();

            // when
            Tag savedTag = await CaptureSavedTagOnTransitionAsync(storageTag, inputTag);

            // then
            savedTag.Should().NotBeNull();
            savedTag.IsApprovedByBypass.Should().BeFalse();
            savedTag.ApprovedByBypassReason.Should().BeNull();
        }
    }
}
