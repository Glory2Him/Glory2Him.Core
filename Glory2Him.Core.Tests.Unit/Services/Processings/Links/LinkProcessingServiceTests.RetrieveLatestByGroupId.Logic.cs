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
        public async Task ShouldRetrieveLatestVersionOnRetrieveLatestByGroupIdAsync()
        {
            // given: the edit tip of the group (§3.4.1) — the highest-versioned non-deleted
            // row in it. Nothing marks the tip, so the rows below carry distinct Version
            // numbers and the tip is whichever of them wins on that number alone.
            Guid inputGroupId = Guid.NewGuid();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            Link latestVersion = CreateRandomPubliclyVisibleLink(
                linkId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            latestVersion.GroupId = inputGroupId;
            latestVersion.Version = 2;

            Link supersededVersion = CreateRandomPubliclyVisibleLink(
                linkId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            supersededVersion.GroupId = inputGroupId;
            supersededVersion.Version = 1;

            // a foreign group's own tip, enumerated FIRST and carrying the HIGHEST version
            // in the table — it satisfies every predicate except the group, so dropping the
            // GroupId term would now return it in preference to the right answer, and being
            // publicly visible it would sail straight through the read posture
            Link foreignGroupLatestVersion = CreateRandomPubliclyVisibleLink(
                linkId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            foreignGroupLatestVersion.GroupId = Guid.NewGuid();
            foreignGroupLatestVersion.Version = 99;

            Link expectedLink = latestVersion.DeepClone();

            IQueryable<Link> storageLinks = new[]
            {
                foreignGroupLatestVersion,
                supersededVersion,
                latestVersion
            }.AsQueryable();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: new Link { GroupId = inputGroupId },
                securityContext: new SecurityContext { IsAuthenticated = false });

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(inputGroupId))))
                    .ReturnsAsync(inboundEnvelope);

            this.linkServiceMock.Setup(service =>
                service.RetrieveAllLinksAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageLinks);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            Link actualLink =
                await this.linkProcessingService.RetrieveLatestLinkByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            // then
            actualLink.Should().BeEquivalentTo(expectedLink);

            // it won on Version, within its group, and nothing else
            actualLink.Version.Should().BeGreaterThan(supersededVersion.Version);
            actualLink.GroupId.Should().Be(inputGroupId);

            this.linkServiceMock.Verify(service =>
                service.RetrieveAllLinksAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveNonPublicLatestVersionOnRetrieveLatestByGroupIdIfCallerIsOwnerAsync()
        {
            // given: the edit tip may still be an unapproved draft — its owner reads it
            Guid inputGroupId = Guid.NewGuid();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();
            string actorUserId = GetRandomString();

            // the group's only live row, so it is the tip by having nothing above it
            Link latestVersion = CreateRandomNonPublicLink(createdBy: actorUserId);
            latestVersion.GroupId = inputGroupId;
            latestVersion.Version = 1;
            Link expectedLink = latestVersion.DeepClone();

            IQueryable<Link> storageLinks = new[] { latestVersion }.AsQueryable();
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: new Link { GroupId = inputGroupId },
                securityContext: securityContext);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(inputGroupId))))
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
            Link actualLink =
                await this.linkProcessingService.RetrieveLatestLinkByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            // then
            actualLink.Should().BeEquivalentTo(expectedLink);
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
