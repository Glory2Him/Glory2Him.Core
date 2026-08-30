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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Processings;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.Links
{
    public partial class LinkProcessingServiceTests
    {
        [Theory]
        [InlineData(ApprovalStatus.Draft)]
        [InlineData(ApprovalStatus.Submitted)]
        [InlineData(ApprovalStatus.Rejected)]
        [InlineData(ApprovalStatus.Dismissed)]
        [InlineData(ApprovalStatus.Approved)]
        public async Task ShouldRemoveLinkOnRemoveByIdIfActorIsOwnerAsync(
            ApprovalStatus approvalStatus)
        {
            // given: the owner may remove their own link at any point of the approval
            // workflow — deletion is not an ApprovalStatus (§10.5), so the status of the
            // row is irrelevant to the decision and is left untouched by the soft delete
            Guid randomLinkId = Guid.NewGuid();
            Guid inputLinkId = randomLinkId;
            string randomDeletionReason = GetRandomString();
            string inputDeletionReason = randomDeletionReason;
            string actorUserId = GetRandomString();

            Link storageLink = CreateRandomStorageLink(
                linkId: inputLinkId,
                approvalStatus: approvalStatus,
                createdBy: actorUserId);

            Link removedLink = storageLink.DeepClone();
            removedLink.IsDeleted = true;
            removedLink.DeletionReason = inputDeletionReason;
            Link expectedLink = removedLink.DeepClone();
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: storageLink,
                securityContext: securityContext);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRemoveRequestAs(
                    inputLinkId,
                    inputDeletionReason))))
                        .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinkByIdAsync(inputLinkId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(actorUserId);

            this.linkServiceMock.Setup(service =>
                service.RemoveLinkByIdAsync(
                    inputLinkId,
                    inputDeletionReason,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(removedLink);

            EventEnvelope<Link> outboundEnvelope = SetupCompletionFactPublish(
                inboundEnvelope: inboundEnvelope,
                resultLink: removedLink,
                operation: LinkProcessingEventOperation.Removed);

            // when
            Link actualLink =
                await this.linkProcessingService.RemoveLinkByIdAsync(
                    inputLinkId,
                    inputDeletionReason,
                    TestContext.Current.CancellationToken);

            // then
            actualLink.Should().BeEquivalentTo(expectedLink);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.Is(SameRemoveRequestAs(
                    inputLinkId,
                    inputDeletionReason))),
                Times.Once);

            this.linkServiceMock.Verify(service =>
                service.RetrieveLinkByIdAsync(inputLinkId, It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(securityContext),
                Times.Once);

            this.linkServiceMock.Verify(service =>
                service.RemoveLinkByIdAsync(
                    inputLinkId,
                    inputDeletionReason,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.linkServiceMock.Verify(service =>
                service.ModifyLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(inboundEnvelope, removedLink),
                Times.Once);

            VerifyCompletionFactPublished(
                outboundEnvelope: outboundEnvelope,
                operation: LinkProcessingEventOperation.Removed);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.linkServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRemoveLinkOnRemoveByIdIfActorIsAdminAsync()
        {
            // given: removing content is a takedown, not a moderation step — an administrator may
            // remove anyone's link, and a reviewer or Publishers may not
            Guid inputLinkId = Guid.NewGuid();
            string inputDeletionReason = GetRandomString();

            Link storageLink = CreateRandomStorageLink(
                linkId: inputLinkId,
                approvalStatus: ApprovalStatus.Approved,
                createdBy: GetRandomString());

            Link removedLink = storageLink.DeepClone();
            removedLink.IsDeleted = true;
            SecurityContext securityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: storageLink,
                securityContext: securityContext);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRemoveRequestAs(
                    inputLinkId,
                    inputDeletionReason))))
                        .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinkByIdAsync(inputLinkId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(GetRandomString());

            this.linkServiceMock.Setup(service =>
                service.RemoveLinkByIdAsync(
                    inputLinkId,
                    inputDeletionReason,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(removedLink);

            SetupCompletionFactPublish(
                inboundEnvelope: inboundEnvelope,
                resultLink: removedLink,
                operation: LinkProcessingEventOperation.Removed);

            // when
            Link actualLink =
                await this.linkProcessingService.RemoveLinkByIdAsync(
                    inputLinkId,
                    inputDeletionReason,
                    TestContext.Current.CancellationToken);

            // then
            actualLink.Should().NotBeNull();

            this.linkServiceMock.Verify(service =>
                service.RemoveLinkByIdAsync(
                    inputLinkId,
                    inputDeletionReason,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
