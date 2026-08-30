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
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Links
{
    public partial class LinkServiceTests
    {
        [Fact]
        public async Task ShouldSubmitLinkByOwnerAsync()
        {
            // given: the owner submitting their own draft — no moderation role required
            Link storageLink = CreateSubmittableStorageLink();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Link submittedLink = storageLink.DeepClone();
            submittedLink.ApprovalStatus = ApprovalStatus.Submitted;

            Link auditAppliedLink = submittedLink.DeepClone();
            Link updatedLink = auditAppliedLink.DeepClone();
            Link expectedLink = updatedLink.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageLink.CreatedBy);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

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
                    LinkEventOperation.Submitted))
                        .Returns(new ValueTask<EventPublishResult<Link>>(
                            new EventPublishResult<Link>()));

            // when
            Link actualLink =
                await this.linkService.SubmitLinkByIdAsync(
                    storageLink.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualLink.Should().BeEquivalentTo(expectedLink);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectLinkByIdAsync(
                        storageLink.Id,
                        It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                    broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
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

            // the operation's OWN fact — never Modified
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishLinkAsync(
                        It.IsAny<EventEnvelope<Link>>(),
                        LinkEventOperation.Submitted),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.InsertProcessedEventAsync(
                    It.Is<ProcessedEvent>(processedEvent =>
                        processedEvent.ReceiverName ==
                            EventBrokerIdentifiers
                                .LinkOnSubmittingLinkSubscriptionName),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            // submit never consults the cross-entity decision — that is the approve's gate
            this.accessBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldSubmitLinkByPublisherWhoIsNotTheOwnerAsync()
        {
            // given: the publisher tier may move a submission status too — the same set the §9.2
            // modify carve-out admits. The caller is NOT the owner, so this proves the
            // publisher-tier branch rather than the ownership branch.
            Link storageLink = CreateSubmittableStorageLink();

            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Publishers);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync($"someone-else-{Guid.NewGuid()}");

            SetupLinkStorageRead(storageLink);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Link>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((Link entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateLinkAsync(
                    It.IsAny<Link>(),
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Link entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishLinkAsync(
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<LinkEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<Link>>(
                            new EventPublishResult<Link>()));

            // when
            await this.linkService.SubmitLinkByIdAsync(
                storageLink.Id,
                TestContext.Current.CancellationToken);

            // then
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishLinkAsync(
                        It.IsAny<EventEnvelope<Link>>(),
                        LinkEventOperation.Submitted),
                Times.Once);
        }

        [Fact]
        public async Task ShouldSaveOnlyTheStatusFieldOnSubmitAsync()
        {
            // given: submit owns ONLY the approval status. It drives Draft -> Submitted and must
            // leave every other field exactly as stored — a content edit is the general modify's
            // job, not submit's. Asserting the whole row against the pre-act snapshot, excluding
            // only the one field submit owns, catches any stray write.
            Link storageLink = CreateSubmittableStorageLink();
            Link expectedStorageLink = storageLink.DeepClone();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageLink.CreatedBy);

            // when
            Link savedLink = await CaptureSavedLinkOnSubmitAsync(storageLink);

            // then
            savedLink.Should().NotBeNull();
            savedLink.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);

            savedLink.Should().BeEquivalentTo(
                expectedStorageLink,
                options => options.Excluding(link => link.ApprovalStatus));
        }

        [Fact]
        public async Task ShouldNeverPublishModifiedOnSubmitAsync()
        {
            // given: like every transition, submit publishes its own fact and never Modified —
            // the approval workflow's cycle-breaker (design §9.7.1, issue #111 case 1).
            Link storageLink = CreateSubmittableStorageLink();

            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageLink.CreatedBy);

            // when
            await CaptureSavedLinkOnSubmitAsync(storageLink);

            // then
            this.eventBrokerMock.Verify(broker =>
                    broker.PublishLinkAsync(
                        It.IsAny<EventEnvelope<Link>>(),
                        LinkEventOperation.Modified),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishLinkAsync(
                        It.IsAny<EventEnvelope<Link>>(),
                        LinkEventOperation.Submitted),
                Times.Once);
        }

        // Runs a permitted submit end to end (owner already set up by the caller) and hands back
        // a snapshot of the row that reached the storage broker.
        private async ValueTask<Link> CaptureSavedLinkOnSubmitAsync(Link storageLink)
        {
            Link savedLink = null;

            SetupLinkStorageRead(storageLink);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(
                    It.IsAny<Link>(),
                    It.IsAny<SecurityContext>()))
                        .ReturnsAsync((Link entity, SecurityContext _) => entity);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateLinkAsync(
                    It.IsAny<Link>(),
                    It.IsAny<CancellationToken>()))
                        .Callback<Link, CancellationToken>(
                            (entity, _) => savedLink = entity.DeepClone())
                        .ReturnsAsync((Link entity, CancellationToken _) => entity);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishLinkAsync(
                    It.IsAny<EventEnvelope<Link>>(),
                    It.IsAny<LinkEventOperation>()))
                        .Returns(new ValueTask<EventPublishResult<Link>>(
                            new EventPublishResult<Link>()));

            await this.linkService.SubmitLinkByIdAsync(
                storageLink.Id,
                TestContext.Current.CancellationToken);

            return savedLink;
        }
    }
}
