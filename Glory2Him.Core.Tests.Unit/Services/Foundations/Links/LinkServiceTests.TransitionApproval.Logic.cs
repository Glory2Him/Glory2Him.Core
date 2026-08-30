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
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Links
{
    public partial class LinkServiceTests
    {
        [Fact]
        public async Task ShouldTransitionLinkApprovalAsync()
        {
            // given
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publishers);

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            Link storageLink = CreateApprovableStorageLink();
            Link inputLink = CreateApprovalDecision(storageLink.Id);

            Link approvedLink = storageLink.DeepClone();
            approvedLink.ApprovalStatus = inputLink.ApprovalStatus;
            approvedLink.IsPublished = inputLink.IsPublished;
            approvedLink.PublishDate = inputLink.PublishDate;
            approvedLink.IsApprovedByBypass = false;
            approvedLink.ApprovedByBypassReason = null;

            Link auditAppliedLink = approvedLink.DeepClone();
            Link updatedLink = auditAppliedLink.DeepClone();
            Link expectedLink = updatedLink.DeepClone();

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            SetupLinkStorageRead(storageLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Link>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync(auditAppliedLink);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateLinkAsync(
                    auditAppliedLink,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(updatedLink);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishLinkAsync(
                    It.IsAny<EventEnvelope<Link>>(),
                    LinkEventOperation.Approved))
                        .Returns(new ValueTask<EventPublishResult<Link>>(
                            new EventPublishResult<Link>()));

            // when
            Link actualLink =
                await this.linkService.TransitionLinkApprovalAsync(
                    inputLink,
                    TestContext.Current.CancellationToken);

            // then
            actualLink.Should().BeEquivalentTo(expectedLink);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectLinkByIdAsync(
                        inputLink.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.accessBrokerMock.Verify(broker =>
                    broker.MayDecideApprovalAsync(
                        It.IsAny<ApprovalDecisionQuery>(),
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.ApplyModifyAuditValuesAsync(
                        It.IsAny<Link>(),
                        It.IsAny<SecurityContext>()),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateLinkAsync(
                        auditAppliedLink,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            // the operation's OWN fact — never Modified. See ShouldNeverPublishModified...
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishLinkAsync(
                        It.IsAny<EventEnvelope<Link>>(),
                        LinkEventOperation.Approved),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .LinkOnApprovingLinkSubscriptionName),
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
                CreateAuthenticatedSecurityContext(Roles.Publishers);

            Link storageLink = CreateApprovableStorageLink();
            Link inputLink = CreateRejectionDecision(storageLink.Id);

            // when
            await CaptureSavedLinkOnTransitionAsync(storageLink, inputLink);

            // then
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishLinkAsync(
                        It.IsAny<EventEnvelope<Link>>(),
                        LinkEventOperation.Rejected),
                Times.Once);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishLinkAsync(
                        It.IsAny<EventEnvelope<Link>>(),
                        LinkEventOperation.Approved),
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
                CreateAuthenticatedSecurityContext(Roles.Publishers);

            Link storageLink = CreateApprovableStorageLink();
            Link inputLink = CreateApprovalDecision(storageLink.Id);

            // when
            await CaptureSavedLinkOnTransitionAsync(storageLink, inputLink);

            // then
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishLinkAsync(
                        It.IsAny<EventEnvelope<Link>>(),
                        LinkEventOperation.Modified),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishLinkAsync(
                        It.IsAny<EventEnvelope<Link>>(),
                        LinkEventOperation.Approved),
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
                CreateAuthenticatedSecurityContext(Roles.Publishers);

            Link storageLink = CreateApprovableStorageLink();
            Link expectedStorageLink = storageLink.DeepClone();

            // a fully random caller copy (differs from storage on every field), pinned only to
            // the id and a valid approval outcome
            Link inputLink = CreateRandomLink();
            inputLink.Id = storageLink.Id;
            inputLink.ApprovalStatus = ApprovalStatus.Approved;
            inputLink.IsPublished = true;
            inputLink.PublishDate = GetRandomDateTimeOffset();

            // when
            Link savedLink = await CaptureSavedLinkOnTransitionAsync(storageLink, inputLink);

            // then
            savedLink.Should().NotBeNull();

            // the fields the operation owns came from the caller
            savedLink.ApprovalStatus.Should().Be(inputLink.ApprovalStatus);
            savedLink.IsPublished.Should().Be(inputLink.IsPublished);
            savedLink.PublishDate.Should().Be(inputLink.PublishDate);

            // everything else came from STORAGE — asserted against the pre-act snapshot, so
            // copying any caller field onto the row fails here. The bypass pair is derived
            // (false / null here) and excluded from the storage comparison.
            savedLink.Should().BeEquivalentTo(
                expectedStorageLink,
                options => options
                    .Excluding(link => link.ApprovalStatus)
                    .Excluding(link => link.IsPublished)
                    .Excluding(link => link.PublishDate)
                    .Excluding(link => link.IsApprovedByBypass)
                    .Excluding(link => link.ApprovedByBypassReason));
        }

        // ── The bypass record is DERIVED, not copied ─────────────────────────────────────────

        [Fact]
        public async Task ShouldIgnoreTheCallersBypassRecordOnApproveAsync()
        {
            // given: the caller claims a bypass it was never granted. The decision came back
            // permitted WITHOUT one, so the saved row must say so — otherwise the flag means
            // "the caller said so" rather than "the rules were waived".
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publishers);

            Link storageLink = CreateApprovableStorageLink();
            storageLink.IsApprovedByBypass = false;
            storageLink.ApprovedByBypassReason = null;

            Link inputLink = CreateApprovalDecision(storageLink.Id);
            inputLink.IsApprovedByBypass = true;
            inputLink.ApprovedByBypassReason = "caller supplied";

            SetupAccessBrokerToPermit();

            // when
            Link savedLink = await CaptureSavedLinkOnTransitionAsync(storageLink, inputLink);

            // then
            savedLink.Should().NotBeNull();
            savedLink.IsApprovedByBypass.Should().BeFalse();
            savedLink.ApprovedByBypassReason.Should().BeNull();

            savedLink.ApprovalStatus.Should().Be(inputLink.ApprovalStatus);
            savedLink.IsPublished.Should().Be(inputLink.IsPublished);
            savedLink.PublishDate.Should().Be(inputLink.PublishDate);
        }

        [Fact]
        public async Task ShouldRecordTheBypassOnTheRowWhenTheDecisionWaivedTheConditionsAsync()
        {
            // given: the mirror image — the caller claims nothing and the DECISION reports a
            // bypass. The flag has to travel from the verdict onto the row.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publishers);

            Link storageLink = CreateApprovableStorageLink();
            storageLink.IsApprovedByBypass = false;
            storageLink.ApprovedByBypassReason = null;

            Link inputLink = CreateApprovalDecision(storageLink.Id);
            inputLink.IsApprovedByBypass = false;
            inputLink.ApprovedByBypassReason = null;

            SetupAccessBrokerToPermitByBypass();

            // when
            Link savedLink = await CaptureSavedLinkOnTransitionAsync(storageLink, inputLink);

            // then
            savedLink.Should().NotBeNull();
            savedLink.IsApprovedByBypass.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldClearAnEarlierBypassRecordWhenTheRowIsApprovedNormallyAsync()
        {
            // given: a row bypass-approved once already, amended since, and now approved on its
            // merits. A row that met its conditions this time must stop claiming they were
            // waived, or the flag accumulates for the rest of its life.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publishers);

            Link storageLink = CreateApprovableStorageLink();
            storageLink.IsApprovedByBypass = true;
            storageLink.ApprovedByBypassReason = "an earlier bypass";

            Link inputLink = CreateApprovalDecision(storageLink.Id);

            SetupAccessBrokerToPermit();

            // when
            Link savedLink = await CaptureSavedLinkOnTransitionAsync(storageLink, inputLink);

            // then
            savedLink.Should().NotBeNull();
            savedLink.IsApprovedByBypass.Should().BeFalse();
            savedLink.ApprovedByBypassReason.Should().BeNull();
        }
    }
}
