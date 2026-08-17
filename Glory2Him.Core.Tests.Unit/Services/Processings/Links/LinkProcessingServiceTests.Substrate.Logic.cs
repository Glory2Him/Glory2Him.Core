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
        [Fact]
        public async Task ShouldAddLinkOnAddingLinkAsync()
        {
            // given: the event path converges on the same do-work as the direct call — the
            // inbound envelope's SecurityContext carries the original caller for the gate,
            // and the reply is the created link's envelope
            Link inputLink = CreateRandomLink();
            Guid linkId = Guid.NewGuid();
            Guid groupId = Guid.NewGuid();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: inputLink,
                securityContext: CreateAuthenticatedSecurityContext());

            var addedLink = new Link
            {
                Id = linkId,
                Name = inputLink.Name,
                Url = inputLink.Url,
                LinkType = inputLink.LinkType,
                GroupId = groupId,
                Version = 1,
                IsLatestVersion = true,
                ApprovalStatus = ApprovalStatus.Draft
            };

            this.identifierBrokerMock.SetupSequence(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(linkId)
                    .ReturnsAsync(groupId);

            this.linkServiceMock.Setup(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(addedLink);

            EventEnvelope<Link> outboundEnvelope = SetupCompletionFactPublish(
                inboundEnvelope: inboundEnvelope,
                resultLink: addedLink,
                operation: LinkProcessingEventOperation.Added);

            // when
            EventEnvelope<Link>? actualEnvelope =
                await this.linkProcessingService.OnAddingLinkAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualEnvelope.Should().BeEquivalentTo(outboundEnvelope);

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    inboundEnvelope,
                    "LinkProcessingAdding",
                    EnvelopeDirection.Request),
                Times.Once);

            this.linkServiceMock.Verify(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()),
                Times.Once);

            VerifyCompletionFactPublished(
                outboundEnvelope: outboundEnvelope,
                operation: LinkProcessingEventOperation.Added);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.linkServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldModifyLinkOnModifyingLinkAsync()
        {
            // given
            Link inputLink = CreateRandomLink();
            string actorUserId = GetRandomString();

            Link storageLink = CreateRandomStorageLink(
                linkId: inputLink.Id,
                approvalStatus: ApprovalStatus.Draft,
                createdBy: actorUserId);

            Link updatedLink = storageLink.DeepClone();
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: inputLink,
                securityContext: securityContext);

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinkByIdAsync(inputLink.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLink);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(actorUserId);

            this.linkServiceMock.Setup(service =>
                service.ModifyLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(updatedLink);

            EventEnvelope<Link> outboundEnvelope = SetupCompletionFactPublish(
                inboundEnvelope: inboundEnvelope,
                resultLink: updatedLink,
                operation: LinkProcessingEventOperation.Modified);

            // when
            EventEnvelope<Link>? actualEnvelope =
                await this.linkProcessingService.OnModifyingLinkAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualEnvelope.Should().BeEquivalentTo(outboundEnvelope);

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    inboundEnvelope,
                    "LinkProcessingModifying",
                    EnvelopeDirection.Request),
                Times.Once);

            VerifyCompletionFactPublished(
                outboundEnvelope: outboundEnvelope,
                operation: LinkProcessingEventOperation.Modified);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRemoveLinkOnRemovingLinkByIdAsync()
        {
            // given: the request payload is the remove instruction — the link's Id and the
            // optional DeletionReason
            Guid inputLinkId = Guid.NewGuid();
            string inputDeletionReason = GetRandomString();
            string actorUserId = GetRandomString();

            Link storageLink = CreateRandomStorageLink(
                linkId: inputLinkId,
                approvalStatus: ApprovalStatus.Draft,
                createdBy: actorUserId);

            var removeRequest = new Link
            {
                Id = inputLinkId,
                DeletionReason = inputDeletionReason
            };

            Link removedLink = storageLink.DeepClone();
            removedLink.IsDeleted = true;
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: removeRequest,
                securityContext: securityContext);

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
            EventEnvelope<Link>? actualEnvelope =
                await this.linkProcessingService.OnRemovingLinkByIdAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualEnvelope.Should().BeEquivalentTo(outboundEnvelope);

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    inboundEnvelope,
                    "LinkProcessingRemovingById",
                    EnvelopeDirection.Request),
                Times.Once);

            this.linkServiceMock.Verify(service =>
                service.RemoveLinkByIdAsync(
                    inputLinkId,
                    inputDeletionReason,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            VerifyCompletionFactPublished(
                outboundEnvelope: outboundEnvelope,
                operation: LinkProcessingEventOperation.Removed);

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveLinkOnRetrievingLinkByIdAndPublishNoFactAsync()
        {
            // given: a read is naturally idempotent and publishes no completion fact — the
            // reply envelope is the whole outcome
            Guid inputLinkId = Guid.NewGuid();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            Link storageLink = CreateRandomPubliclyVisibleLink(
                linkId: inputLinkId,
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: new Link { Id = inputLinkId },
                securityContext: new SecurityContext { IsAuthenticated = false });

            var replyEnvelope = new EventEnvelope<Link>
            {
                Content = storageLink,
                SecurityContext = inboundEnvelope.SecurityContext,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinkByIdAsync(inputLinkId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLink);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateNextAsync(inboundEnvelope, storageLink))
                    .ReturnsAsync(replyEnvelope);

            // when
            EventEnvelope<Link>? actualEnvelope =
                await this.linkProcessingService.OnRetrievingLinkByIdAsync(
                    inboundEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualEnvelope.Should().BeEquivalentTo(replyEnvelope);

            this.envelopeIntegrityBrokerMock.Verify(broker =>
                broker.VerifyAsync(
                    inboundEnvelope,
                    "LinkProcessingRetrievingById",
                    EnvelopeDirection.Request),
                Times.Once);

            this.linkServiceMock.Verify(service =>
                service.RetrieveLinkByIdAsync(inputLinkId, It.IsAny<CancellationToken>()),
                Times.Once);

            // being a read, no completion fact is published
            this.eventBrokerMock.VerifyNoOtherCalls();

            this.envelopeIntegrityBrokerMock.VerifyNoOtherCalls();
            this.linkServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
