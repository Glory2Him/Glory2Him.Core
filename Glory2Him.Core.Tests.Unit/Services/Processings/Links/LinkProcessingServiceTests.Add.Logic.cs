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
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.Links
{
    public partial class LinkProcessingServiceTests
    {
        [Fact]
        public async Task ShouldAddLinkAsync()
        {
            // given: an add lands version 1 of a brand new group — the id and the group id
            // are both minted here, and being the group's only row it is also its tip, with
            // nothing written to say so. It starts unpublished and in Draft, and only the
            // caller's content fields are carried across.
            Link randomLink = CreateRandomLink();
            Link inputLink = randomLink;
            Guid linkId = Guid.NewGuid();
            Guid groupId = Guid.NewGuid();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: inputLink,
                securityContext: CreateAuthenticatedSecurityContext());

            var expectedMappedLink = new Link
            {
                Id = linkId,
                Name = inputLink.Name,
                Url = inputLink.Url,
                LinkType = inputLink.LinkType,

                // the caller's publish date does not ride in on the add — a fresh row has
                // none until approve grants one, which is why it lands unpublished in Draft
                PublishDate = null,
                GroupId = groupId,
                Version = 1,
                IsPublished = false,
                ApprovalStatus = ApprovalStatus.Draft,
                IsDeleted = false
            };

            Link addedLink = expectedMappedLink.DeepClone();
            Link expectedLink = addedLink.DeepClone();

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputLink))
                    .ReturnsAsync(inboundEnvelope);

            this.identifierBrokerMock.SetupSequence(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(linkId)
                    .ReturnsAsync(groupId);

            Link? capturedLink = null;

            this.linkServiceMock.Setup(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
                    .Callback<Link, CancellationToken>((link, cancellationToken) =>
                        capturedLink = link)
                    .ReturnsAsync(addedLink);

            EventEnvelope<Link> outboundEnvelope = SetupCompletionFactPublish(
                inboundEnvelope: inboundEnvelope,
                resultLink: addedLink,
                operation: LinkProcessingEventOperation.Added);

            // when
            Link actualLink =
                await this.linkProcessingService.AddLinkAsync(
                    inputLink,
                    TestContext.Current.CancellationToken);

            // then
            actualLink.Should().BeEquivalentTo(expectedLink);
            capturedLink.Should().BeEquivalentTo(expectedMappedLink);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(inputLink),
                Times.Once);

            this.identifierBrokerMock.Verify(broker =>
                broker.GetIdentifierAsync(),
                Times.Exactly(2));

            this.linkServiceMock.Verify(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(inboundEnvelope, addedLink),
                Times.Once);

            VerifyCompletionFactPublished(
                outboundEnvelope: outboundEnvelope,
                operation: LinkProcessingEventOperation.Added);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.linkServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldNotCarryPublishDateOnAddAsync()
        {
            // given: PublishDate is an IApproval member, so under §9.7.1 rule 2's subtraction
            // rule it is not content — and the add surface may carry an ApprovalStatus of
            // Draft or Submitted and nothing else: never IsPublished, never PublishDate. The
            // new row already lands unpublished and in Draft; taking the caller's publish date
            // as well would let them schedule their own publication on the way in, without
            // ever meeting the approve gate that owns it.
            Link inputLink = CreateRandomLink();
            inputLink.PublishDate = GetRandomDateTimeOffset();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: inputLink,
                securityContext: CreateAuthenticatedSecurityContext());

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputLink))
                    .ReturnsAsync(inboundEnvelope);

            Link? capturedLink = null;

            this.linkServiceMock.Setup(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
                    .Callback<Link, CancellationToken>((link, cancellationToken) =>
                        capturedLink = link)
                    .ReturnsAsync(inputLink);

            // when
            await this.linkProcessingService.AddLinkAsync(
                inputLink,
                TestContext.Current.CancellationToken);

            // then
            capturedLink!.PublishDate.Should().BeNull();
        }

        [Fact]
        public async Task ShouldNotCarryCallerControlFieldsOnAddAsync()
        {
            // given: every control field is set internally (§12.4.1 BR6). A caller who
            // arrives claiming an approved, published, already-versioned row must land a
            // fresh Draft on version 1 of a group this service minted, not the one they
            // named — otherwise the add path is a way around the whole approval workflow.
            Link inputLink = CreateRandomLink();
            inputLink.GroupId = Guid.NewGuid();
            inputLink.Version = GetRandomNumber();
            inputLink.IsPublished = true;
            inputLink.ApprovalStatus = ApprovalStatus.Approved;
            inputLink.IsDeleted = true;
            Guid linkId = Guid.NewGuid();
            Guid mintedGroupId = Guid.NewGuid();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: inputLink,
                securityContext: CreateAuthenticatedSecurityContext());

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputLink))
                    .ReturnsAsync(inboundEnvelope);

            this.identifierBrokerMock.SetupSequence(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(linkId)
                    .ReturnsAsync(mintedGroupId);

            Link? capturedLink = null;

            this.linkServiceMock.Setup(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
                    .Callback<Link, CancellationToken>((link, cancellationToken) =>
                        capturedLink = link)
                    .ReturnsAsync(inputLink);

            // when
            await this.linkProcessingService.AddLinkAsync(
                inputLink,
                TestContext.Current.CancellationToken);

            // then
            capturedLink!.Id.Should().Be(linkId);
            capturedLink.GroupId.Should().Be(mintedGroupId);
            // version 1 of a freshly minted group, so it is that group's tip by construction
            // — the caller's version number is discarded rather than trusted
            capturedLink.Version.Should().Be(1);
            capturedLink.IsPublished.Should().BeFalse();
            capturedLink.ApprovalStatus.Should().Be(ApprovalStatus.Draft);
            capturedLink.IsDeleted.Should().BeFalse();
        }
    }
}
