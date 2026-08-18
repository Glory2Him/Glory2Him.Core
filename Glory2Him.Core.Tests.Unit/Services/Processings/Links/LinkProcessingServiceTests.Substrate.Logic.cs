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

            // twice, and the count is the whole point: once inside the do-work to carry the
            // completion fact, once here to mint the reply. Handing the fact's own envelope
            // back as the reply would make both share an EventId and collapse the causation
            // chain downstream processes correlate on — and would still satisfy the
            // equivalence assertion above, since the mock returns one shared instance
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(inboundEnvelope, addedLink),
                Times.Exactly(2));

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

            SetupGroupTipRead(storageLink);
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

            // one for the completion fact, one for the reply — see the note in the add test
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(inboundEnvelope, updatedLink),
                Times.Exactly(2));

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

            // one for the completion fact, one for the reply — see the note in the add test
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(inboundEnvelope, removedLink),
                Times.Exactly(2));

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

        [Fact]
        public async Task ShouldForwardEnvelopeSecurityContextOnRetrievingLinkByIdAsync()
        {
            // given: the read posture on the event path must run against the INBOUND
            // envelope's SecurityContext — that context is the original caller, and it is
            // the one the integrity signature covers (§14.6 rule 4). A non-public row owned
            // by the caller is the case that proves the forwarding: it can only be returned
            // by resolving that exact context to that exact user id. Every other retrieve
            // test short-circuits earlier — on isPubliclyVisible or on IsDeleted — so none
            // of them would notice the handler minting a fresh context or passing null.
            Guid inputLinkId = Guid.NewGuid();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();
            string actorUserId = GetRandomString();

            Link storageLink = CreateRandomNonPublicLink(createdBy: actorUserId);
            storageLink.Id = inputLinkId;
            Link expectedLink = storageLink.DeepClone();
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: new Link { Id = inputLinkId },
                securityContext: securityContext);

            var replyEnvelope = new EventEnvelope<Link>
            {
                Content = storageLink,
                SecurityContext = securityContext,
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinkByIdAsync(inputLinkId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLink);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // stubbed on the exact inbound context instance — a different context resolves
            // to null, the ownership check fails, and the handler throws not-found instead
            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(actorUserId);

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
            actualEnvelope!.Content.Should().BeEquivalentTo(expectedLink);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(securityContext),
                Times.Once);

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
