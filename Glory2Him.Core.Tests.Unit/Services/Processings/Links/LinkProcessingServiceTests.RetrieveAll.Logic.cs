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
using System.Linq;
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
        [Fact]
        public async Task ShouldRetrieveOnlyPublicLinksOnRetrieveAllIfCallerIsAnonymousAsync()
        {
            // given: an anonymous caller sees the canonical visible set alone (§14.1) —
            // drafts, future-scheduled rows and deleted rows all drop out of the set,
            // and the caller is never identified
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            Link publicLink = CreateRandomPubliclyVisibleLink(
                linkId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            Link publicNoDateLink = CreateRandomPubliclyVisibleLink(
                linkId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: false);

            Link nonPublicLink = CreateRandomNonPublicLink(createdBy: GetRandomString());

            Link futureLink = CreateRandomPubliclyVisibleLink(
                linkId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            futureLink.PublishDate = currentDateTime.AddDays(1);
            Link deletedLink = CreateRandomDeletedLink(currentDateTime);

            IQueryable<Link> storageLinks = new[]
            {
                publicLink,
                publicNoDateLink,
                nonPublicLink,
                futureLink,
                deletedLink
            }.AsQueryable();

            IQueryable<Link> expectedLinks = new[]
            {
                publicLink.DeepClone(),
                publicNoDateLink.DeepClone()
            }.AsQueryable();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: new Link(),
                securityContext: new SecurityContext { IsAuthenticated = false });

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveAllRequest())))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveAllLinksAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLinks);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            IQueryable<Link> actualLinks =
                await this.linkProcessingService.RetrieveAllLinksAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualLinks.Should().BeEquivalentTo(expectedLinks);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.Is(SameRetrieveAllRequest())),
                Times.Once);

            this.linkServiceMock.Verify(service =>
                service.RetrieveAllLinksAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            // a public read never identifies the caller and, being a read, publishes no fact
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.linkServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrievePublicAndOwnLinksOnRetrieveAllIfCallerIsAuthenticatedAsync()
        {
            // given: an authenticated caller without a review role follows their own links
            // through the workflow — their own rows in any state join the public set, while
            // other users' non-public rows stay invisible and deleted rows stay gone
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();
            string actorUserId = GetRandomString();

            Link publicLink = CreateRandomPubliclyVisibleLink(
                linkId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            Link ownNonPublicLink = CreateRandomNonPublicLink(createdBy: actorUserId);
            Link otherNonPublicLink = CreateRandomNonPublicLink(createdBy: GetRandomString());
            Link ownDeletedLink = CreateRandomDeletedLink(currentDateTime);
            ownDeletedLink.CreatedBy = actorUserId;

            IQueryable<Link> storageLinks = new[]
            {
                publicLink,
                ownNonPublicLink,
                otherNonPublicLink,
                ownDeletedLink
            }.AsQueryable();

            IQueryable<Link> expectedLinks = new[]
            {
                publicLink.DeepClone(),
                ownNonPublicLink.DeepClone()
            }.AsQueryable();

            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: new Link(),
                securityContext: securityContext);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveAllRequest())))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveAllLinksAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLinks);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(actorUserId);

            // when
            IQueryable<Link> actualLinks =
                await this.linkProcessingService.RetrieveAllLinksAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualLinks.Should().BeEquivalentTo(expectedLinks);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(securityContext),
                Times.Once);

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ShouldRetrieveOnlyPublicLinksOnRetrieveAllIfActorUserIdIsUnresolvedAsync(
            string? unresolvedActorUserId)
        {
            // given: an authenticated caller whose identity cannot be resolved must not
            // accidentally match rows whose CreatedBy is also blank. Link.CreatedBy defaults
            // to string.Empty, so without the IsNullOrWhiteSpace guard the ownership term
            // would be "" == "" and every unstamped non-public row would fall out to a
            // caller who owns none of them.
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            Link publicLink = CreateRandomPubliclyVisibleLink(
                linkId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            Link blankOwnerNonPublicLink = CreateRandomNonPublicLink(createdBy: string.Empty);
            Link otherNonPublicLink = CreateRandomNonPublicLink(createdBy: GetRandomString());

            IQueryable<Link> storageLinks = new[]
            {
                publicLink,
                blankOwnerNonPublicLink,
                otherNonPublicLink
            }.AsQueryable();

            IQueryable<Link> expectedLinks = new[]
            {
                publicLink.DeepClone()
            }.AsQueryable();

            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: new Link(),
                securityContext: securityContext);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveAllRequest())))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveAllLinksAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLinks);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(unresolvedActorUserId!);

            // when
            IQueryable<Link> actualLinks =
                await this.linkProcessingService.RetrieveAllLinksAsync(
                    TestContext.Current.CancellationToken);

            // then: the blank-owner row stays hidden despite the blank actor id
            actualLinks.Should().BeEquivalentTo(expectedLinks);
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(Roles.Reviewers)]
        [InlineData(Roles.LinkReviewers)]
        [InlineData(Roles.Publishers)]
        [InlineData(Roles.LinkPublishers)]
        [InlineData(Roles.Administrators)]
        public async Task ShouldRetrieveEveryNonDeletedLinkOnRetrieveAllIfCallerHasReviewRoleAsync(
            string reviewRole)
        {
            // given: a review-role caller audits the whole pipeline — every non-deleted row,
            // including drafts and future-scheduled ones. The clock and the caller's
            // identity are never consulted; only the deleted row still drops out.
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            Link publicLink = CreateRandomPubliclyVisibleLink(
                linkId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            Link nonPublicLink = CreateRandomNonPublicLink(createdBy: GetRandomString());

            Link futureLink = CreateRandomPubliclyVisibleLink(
                linkId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            futureLink.PublishDate = currentDateTime.AddDays(1);
            Link deletedLink = CreateRandomDeletedLink(currentDateTime);

            IQueryable<Link> storageLinks = new[]
            {
                publicLink,
                nonPublicLink,
                futureLink,
                deletedLink
            }.AsQueryable();

            IQueryable<Link> expectedLinks = new[]
            {
                publicLink.DeepClone(),
                nonPublicLink.DeepClone(),
                futureLink.DeepClone()
            }.AsQueryable();

            SecurityContext securityContext = CreateAuthenticatedSecurityContext(reviewRole);

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: new Link(),
                securityContext: securityContext);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveAllRequest())))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveAllLinksAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLinks);

            // when
            IQueryable<Link> actualLinks =
                await this.linkProcessingService.RetrieveAllLinksAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualLinks.Should().BeEquivalentTo(expectedLinks);

            // the review branch returns before the clock or the identity are needed
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
