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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.Links
{
    public partial class LinkProcessingServiceTests
    {
        [Fact]
        public async Task ShouldRetrievePublishedVersionOnRetrievePublishedByGroupIdAsync()
        {
            // given: the row the public currently reads stays published while a newer draft
            // moves through review (§3.4.1), so it is found independently of where it sits
            // in the version chain — here it is deliberately NOT the tip
            Guid inputGroupId = Guid.NewGuid();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            Link publishedVersion = CreateRandomPubliclyVisibleLink(
                linkId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            publishedVersion.GroupId = inputGroupId;
            publishedVersion.Version = 1;

            Link draftTipVersion = CreateRandomNonPublicLink(createdBy: GetRandomString());
            draftTipVersion.GroupId = inputGroupId;
            draftTipVersion.Version = 2;

            // a foreign group's published row, enumerated FIRST — it satisfies every
            // predicate except the group, so dropping the GroupId term would return it
            // instead, and being publicly visible it would pass the read posture unnoticed
            Link foreignGroupPublishedVersion = CreateRandomPubliclyVisibleLink(
                linkId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            foreignGroupPublishedVersion.GroupId = Guid.NewGuid();

            Link expectedLink = publishedVersion.DeepClone();

            IQueryable<Link> storageLinks = new[]
            {
                foreignGroupPublishedVersion,
                draftTipVersion,
                publishedVersion
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
                await this.linkProcessingService.RetrievePublishedLinkByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            // then
            actualLink.Should().BeEquivalentTo(expectedLink);

            this.linkServiceMock.Verify(service =>
                service.RetrieveAllLinksAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveFutureScheduledPublishedVersionOnRetrievePublishedByGroupIdForOwnerAsync()
        {
            // given: a published row scheduled in the future is not canonically visible yet,
            // but its owner may still read it
            Guid inputGroupId = Guid.NewGuid();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();
            string actorUserId = GetRandomString();

            Link publishedVersion = CreateRandomPubliclyVisibleLink(
                linkId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            publishedVersion.GroupId = inputGroupId;
            publishedVersion.PublishDate = currentDateTime.AddDays(1);
            publishedVersion.CreatedBy = actorUserId;
            publishedVersion.ApprovalStatus = ApprovalStatus.Approved;
            Link expectedLink = publishedVersion.DeepClone();

            IQueryable<Link> storageLinks = new[] { publishedVersion }.AsQueryable();
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
                await this.linkProcessingService.RetrievePublishedLinkByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            // then
            actualLink.Should().BeEquivalentTo(expectedLink);
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
