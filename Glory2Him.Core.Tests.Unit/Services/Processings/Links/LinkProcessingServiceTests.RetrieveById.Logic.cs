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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.Links
{
    public partial class LinkProcessingServiceTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldRetrievePublicLinkOnRetrieveByIdForAnonymousCallerAsync(
            bool hasPublishDate)
        {
            // given: a version that satisfies canonical content visibility (§14.1) is
            // readable by anyone — reads carry no contribution gate, so the caller is never
            // even identified
            Guid inputLinkId = Guid.NewGuid();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            Link storageLink = CreateRandomPubliclyVisibleLink(
                linkId: inputLinkId,
                currentDateTime: currentDateTime,
                hasPublishDate: hasPublishDate);

            Link expectedLink = storageLink.DeepClone();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: new Link { Id = inputLinkId },
                securityContext: new SecurityContext { IsAuthenticated = false });

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveRequestAs(inputLinkId))))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinkByIdAsync(inputLinkId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLink);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            Link actualLink =
                await this.linkProcessingService.RetrieveLinkByIdAsync(
                    inputLinkId,
                    TestContext.Current.CancellationToken);

            // then
            actualLink.Should().BeEquivalentTo(expectedLink);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.Is(SameRetrieveRequestAs(inputLinkId))),
                Times.Once);

            this.linkServiceMock.Verify(service =>
                service.RetrieveLinkByIdAsync(inputLinkId, It.IsAny<CancellationToken>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            // a read publishes no completion fact and never identifies a caller it does
            // not need to identify
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.linkServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveNonPublicLinkOnRetrieveByIdIfCallerIsOwnerAsync()
        {
            // given: a non-public version is readable by its owner, who follows their own
            // link through the workflow
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

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveRequestAs(inputLinkId))))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinkByIdAsync(inputLinkId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLink);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(actorUserId);

            // when
            Link actualLink =
                await this.linkProcessingService.RetrieveLinkByIdAsync(
                    inputLinkId,
                    TestContext.Current.CancellationToken);

            // then
            actualLink.Should().BeEquivalentTo(expectedLink);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(securityContext),
                Times.Once);

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(Roles.Reviewer)]
        [InlineData(Roles.LinkReviewer)]
        [InlineData(Roles.Publisher)]
        [InlineData(Roles.LinkPublisher)]
        [InlineData(Roles.Admin)]
        public async Task ShouldRetrieveNonPublicLinkOnRetrieveByIdIfCallerHasReviewRoleAsync(
            string reviewRole)
        {
            // given: a non-public version is readable by the moderation roles for review
            // and audit (§16.6, §18.6) — Link has no narrow content-type tier, so a
            // Link-Reviewer covers every link there is
            Guid inputLinkId = Guid.NewGuid();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            Link storageLink = CreateRandomNonPublicLink(createdBy: GetRandomString());
            storageLink.Id = inputLinkId;
            Link expectedLink = storageLink.DeepClone();
            SecurityContext securityContext = CreateAuthenticatedSecurityContext(reviewRole);

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: new Link { Id = inputLinkId },
                securityContext: securityContext);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveRequestAs(inputLinkId))))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinkByIdAsync(inputLinkId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLink);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(GetRandomString());

            // when
            Link actualLink =
                await this.linkProcessingService.RetrieveLinkByIdAsync(
                    inputLinkId,
                    TestContext.Current.CancellationToken);

            // then
            actualLink.Should().BeEquivalentTo(expectedLink);
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrievePublicLinkOnRetrieveByIdForBlockedCallerAsync()
        {
            // given: the block roles block contributions, not reads — a ReadOnly caller
            // still reads canonically visible content
            Guid inputLinkId = Guid.NewGuid();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            Link storageLink = CreateRandomPubliclyVisibleLink(
                linkId: inputLinkId,
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            Link expectedLink = storageLink.DeepClone();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: new Link { Id = inputLinkId },
                securityContext: CreateAuthenticatedSecurityContext(Roles.LinkReadOnly));

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveRequestAs(inputLinkId))))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinkByIdAsync(inputLinkId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLink);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            Link actualLink =
                await this.linkProcessingService.RetrieveLinkByIdAsync(
                    inputLinkId,
                    TestContext.Current.CancellationToken);

            // then
            actualLink.Should().BeEquivalentTo(expectedLink);
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
