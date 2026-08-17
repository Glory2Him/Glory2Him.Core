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
        public async Task ShouldRetrieveOnlyGroupVersionsOnRetrieveByGroupIdAsync()
        {
            // given: a group read is the whole version chain of one logical link (§15.1),
            // so rows of other groups are excluded before the visibility filter runs
            Guid inputGroupId = Guid.NewGuid();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            Link firstGroupVersion = CreateRandomPubliclyVisibleLink(
                linkId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            firstGroupVersion.GroupId = inputGroupId;

            Link secondGroupVersion = CreateRandomPubliclyVisibleLink(
                linkId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: false);

            secondGroupVersion.GroupId = inputGroupId;

            Link otherGroupLink = CreateRandomPubliclyVisibleLink(
                linkId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            otherGroupLink.GroupId = Guid.NewGuid();

            IQueryable<Link> storageLinks = new[]
            {
                firstGroupVersion,
                secondGroupVersion,
                otherGroupLink
            }.AsQueryable();

            IQueryable<Link> expectedLinks = new[]
            {
                firstGroupVersion.DeepClone(),
                secondGroupVersion.DeepClone()
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
            IQueryable<Link> actualLinks =
                await this.linkProcessingService.RetrieveLinksByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            // then
            actualLinks.Should().BeEquivalentTo(expectedLinks);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(inputGroupId))),
                Times.Once);

            this.linkServiceMock.Verify(service =>
                service.RetrieveAllLinksAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.linkServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldApplyPerCallerVisibilityOnRetrieveByGroupIdAsync()
        {
            // given: the group read applies the same per-caller filter as the general
            // collection read — deleted rows are gone for everyone, and an owner also sees
            // their own non-public versions of the group
            Guid inputGroupId = Guid.NewGuid();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();
            string actorUserId = GetRandomString();

            Link publicVersion = CreateRandomPubliclyVisibleLink(
                linkId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            publicVersion.GroupId = inputGroupId;

            Link ownDraftVersion = CreateRandomNonPublicLink(createdBy: actorUserId);
            ownDraftVersion.GroupId = inputGroupId;

            Link otherDraftVersion = CreateRandomNonPublicLink(createdBy: GetRandomString());
            otherDraftVersion.GroupId = inputGroupId;

            Link deletedVersion = CreateRandomDeletedLink(currentDateTime);
            deletedVersion.GroupId = inputGroupId;
            deletedVersion.CreatedBy = actorUserId;

            IQueryable<Link> storageLinks = new[]
            {
                publicVersion,
                ownDraftVersion,
                otherDraftVersion,
                deletedVersion
            }.AsQueryable();

            IQueryable<Link> expectedLinks = new[]
            {
                publicVersion.DeepClone(),
                ownDraftVersion.DeepClone()
            }.AsQueryable();

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
            IQueryable<Link> actualLinks =
                await this.linkProcessingService.RetrieveLinksByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            // then
            actualLinks.Should().BeEquivalentTo(expectedLinks);
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
