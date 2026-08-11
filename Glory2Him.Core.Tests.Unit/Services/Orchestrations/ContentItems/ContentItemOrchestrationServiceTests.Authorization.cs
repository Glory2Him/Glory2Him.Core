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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.ContentItems
{
    public partial class ContentItemOrchestrationServiceTests
    {
        // ── The content-type tier (design §18.6 rule 4) ──────────────────────────────
        //
        // The orchestration enforces the same visibility posture as the foundation (§14.6:
        // no layer assumes another already gated the caller), so the narrow content-type
        // tier has to mean the same thing on both sides of the boundary. These mirror the
        // foundation's own pair of tests.

        [Fact]
        public async Task ShouldRetrieveOnlyTheContentTypesTheNarrowRoleCoversOnRetrieveAllAsync()
        {
            // given: a Testimony reviewer audits testimonies and nothing else — the danger
            // is a blanket "review role sees everything" branch handing over every draft
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            ContentItem publicStoryContentItem = CreateRandomPubliclyVisibleContentItem(
                contentItemId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            publicStoryContentItem.ContentType = ContentType.Story;

            ContentItem testimonyDraftContentItem =
                CreateRandomNonPublicContentItem(createdBy: GetRandomString());

            testimonyDraftContentItem.ContentType = ContentType.Testimony;

            ContentItem storyDraftContentItem =
                CreateRandomNonPublicContentItem(createdBy: GetRandomString());

            storyDraftContentItem.ContentType = ContentType.Story;

            ContentItem deletedTestimonyContentItem =
                CreateRandomDeletedContentItem(currentDateTime);

            deletedTestimonyContentItem.ContentType = ContentType.Testimony;

            IQueryable<ContentItem> storageContentItems = new[]
            {
                publicStoryContentItem,
                testimonyDraftContentItem,
                storyDraftContentItem,
                deletedTestimonyContentItem
            }.AsQueryable();

            // the public story anyone may see, plus the testimony draft their tier covers —
            // the story draft stays hidden, and the deleted row is gone for everyone
            IQueryable<ContentItem> expectedContentItems = new[]
            {
                publicStoryContentItem.DeepClone(),
                testimonyDraftContentItem.DeepClone()
            }.AsQueryable();

            SecurityContext securityContext = CreateAuthenticatedSecurityContext(
                Roles.ReviewerFor(EntityType.ContentItem, ContentType.Testimony));

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: new ContentItem(),
                securityContext: securityContext);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveAllRequest())))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItems);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(GetRandomString());

            // when
            IQueryable<ContentItem> actualContentItems =
                await this.contentItemOrchestrationService.RetrieveAllContentItemsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualContentItems.Should().BeEquivalentTo(expectedContentItems);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldRetrieveAllNonDeletedContentItemsOnRetrieveAllIfActorHasNarrowRolesForEveryContentTypeAsync()
        {
            // given: holding the narrow tier for every content type is equivalent to the
            // coarse role in reach — the set is a union, not a special case
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            ContentItem testimonyDraftContentItem =
                CreateRandomNonPublicContentItem(createdBy: GetRandomString());

            testimonyDraftContentItem.ContentType = ContentType.Testimony;

            ContentItem storyDraftContentItem =
                CreateRandomNonPublicContentItem(createdBy: GetRandomString());

            storyDraftContentItem.ContentType = ContentType.Story;

            IQueryable<ContentItem> storageContentItems = new[]
            {
                testimonyDraftContentItem,
                storyDraftContentItem
            }.AsQueryable();

            IQueryable<ContentItem> expectedContentItems = new[]
            {
                testimonyDraftContentItem.DeepClone(),
                storyDraftContentItem.DeepClone()
            }.AsQueryable();

            SecurityContext securityContext = CreateAuthenticatedSecurityContext(
                Roles.ReviewerFor(EntityType.ContentItem, ContentType.Testimony),
                Roles.ReviewerFor(EntityType.ContentItem, ContentType.Story));

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: new ContentItem(),
                securityContext: securityContext);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveAllRequest())))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItems);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(GetRandomString());

            // when
            IQueryable<ContentItem> actualContentItems =
                await this.contentItemOrchestrationService.RetrieveAllContentItemsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualContentItems.Should().BeEquivalentTo(expectedContentItems);
        }
    }
}
