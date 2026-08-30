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
        // ── The entity-type tier (design §18.6) ─────────────────────────────────────
        //
        // A granular role grants capability for its own entity type and no other. Link's
        // review tier is two-deep, not three: only ContentItem carries a ContentType, so
        // there is no narrow content-type grant to resolve here (rule 5). What has to be
        // pinned instead is the boundary in the other direction — a role scoped to some
        // other entity type must buy nothing at all over links, and the shape of these
        // names (a shared `-Reviewers` suffix) is exactly what makes a sloppy suffix match
        // leak across types.

        public static TheoryData<string> OtherEntityTypeReviewRoles() =>
            new TheoryData<string>
            {
                Roles.ReviewersFor(EntityType.ContentItem),
                Roles.PublishersFor(EntityType.ContentItem),
                Roles.ReviewersFor(EntityType.Tag),
                Roles.ReviewersFor(EntityType.Comment),
                Roles.ReviewersFor(EntityType.Attachment),
                Roles.ReviewersFor(EntityType.ContentItem, ContentType.Testimony)
            };

        [Theory]
        [MemberData(nameof(OtherEntityTypeReviewRoles))]
        public async Task ShouldNotWidenRetrieveAllForARoleScopedToAnotherEntityTypeAsync(
            string otherEntityTypeRole)
        {
            // given: a ContentItem or Tag reviewer holds no authority over links, so the
            // set they read is the public one — their own rows aside
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            Link publicLink = CreateRandomPubliclyVisibleLink(
                linkId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            Link otherDraftLink = CreateRandomNonPublicLink(createdBy: GetRandomString());

            IQueryable<Link> storageLinks = new[]
            {
                publicLink,
                otherDraftLink
            }.AsQueryable();

            IQueryable<Link> expectedLinks = new[]
            {
                publicLink.DeepClone()
            }.AsQueryable();

            SecurityContext securityContext =
                CreateAuthenticatedSecurityContext(otherEntityTypeRole);

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
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(GetRandomString());

            // when
            IQueryable<Link> actualLinks =
                await this.linkProcessingService.RetrieveAllLinksAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualLinks.Should().BeEquivalentTo(expectedLinks);
        }

        [Theory]
        [MemberData(nameof(OtherEntityTypeReviewRoles))]
        public async Task ShouldNotRevealNonPublicLinkOnRetrieveByIdForARoleScopedToAnotherEntityTypeAsync(
            string otherEntityTypeRole)
        {
            // given: the single-row posture answers the same way — a role scoped elsewhere
            // is told not-found, never unauthorized, so it cannot probe for links either
            Guid inputLinkId = Guid.NewGuid();
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            Link storageLink = CreateRandomNonPublicLink(createdBy: GetRandomString());
            storageLink.Id = inputLinkId;

            SecurityContext securityContext =
                CreateAuthenticatedSecurityContext(otherEntityTypeRole);

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
            ValueTask<Link> retrieveLinkTask =
                this.linkProcessingService.RetrieveLinkByIdAsync(
                    inputLinkId,
                    TestContext.Current.CancellationToken);

            // then
            await Assert.ThrowsAsync<
                Core.Models.Processings.Links.Exceptions.LinkProcessingValidationException>(
                    retrieveLinkTask.AsTask);
        }

        [Fact]
        public async Task ShouldNotBlockLinkContributionForAReadOnlyRoleScopedToAnotherEntityTypeAsync()
        {
            // given: the block role is scoped the same way the grant is — a Tag-ReadOnly
            // caller is barred from tags, not from links, so their link add goes through
            Link inputLink = CreateRandomLink();
            Guid linkId = Guid.NewGuid();
            Guid groupId = Guid.NewGuid();

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: inputLink,
                securityContext: CreateAuthenticatedSecurityContext(
                    Roles.ReadOnlyFor(EntityType.Tag)));

            var addedLink = new Link
            {
                Id = linkId,
                GroupId = groupId,
                Version = 1,
                ApprovalStatus = ApprovalStatus.Draft
            };

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputLink))
                    .ReturnsAsync(inboundEnvelope);

            this.identifierBrokerMock.SetupSequence(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(linkId)
                    .ReturnsAsync(groupId);

            this.linkServiceMock.Setup(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(addedLink);

            SetupCompletionFactPublish(
                inboundEnvelope: inboundEnvelope,
                resultLink: addedLink,
                operation: Core.Models.Events.Processings.LinkProcessingEventOperation.Added);

            // when
            Link actualLink =
                await this.linkProcessingService.AddLinkAsync(
                    inputLink,
                    TestContext.Current.CancellationToken);

            // then
            actualLink.Should().NotBeNull();

            this.linkServiceMock.Verify(service =>
                service.AddLinkAsync(It.IsAny<Link>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
