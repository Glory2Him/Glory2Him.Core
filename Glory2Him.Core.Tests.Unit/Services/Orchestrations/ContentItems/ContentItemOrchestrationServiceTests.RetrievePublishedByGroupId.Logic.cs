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
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldRetrievePublishedContentItemByGroupIdIfPublishedIsPubliclyVisibleAsync(
            bool hasPublishDate)
        {
            // given: the group's published row is the one the public currently reads — it
            // is found independently of IsLatestVersion, so it stays readable while a
            // newer draft moves through review (§3.4.1); the caller here is anonymous
            Guid randomGroupId = Guid.NewGuid();
            Guid inputGroupId = randomGroupId;
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            ContentItem publishedContentItem = CreateRandomPubliclyVisibleContentItem(
                contentItemId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: hasPublishDate);

            publishedContentItem.GroupId = inputGroupId;
            publishedContentItem.IsLatestVersion = false;

            ContentItem draftLatestContentItem = CreateRandomNonPublicContentItem(
                createdBy: GetRandomString());

            draftLatestContentItem.GroupId = inputGroupId;
            draftLatestContentItem.IsLatestVersion = true;

            ContentItem otherGroupPublishedContentItem = CreateRandomPubliclyVisibleContentItem(
                contentItemId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            ContentItem expectedContentItem = publishedContentItem.DeepClone();

            IQueryable<ContentItem> storageContentItems = new[]
            {
                publishedContentItem,
                draftLatestContentItem,
                otherGroupPublishedContentItem
            }.AsQueryable();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: new ContentItem { GroupId = inputGroupId },
                securityContext: new SecurityContext { IsAuthenticated = false });

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(inputGroupId))))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItems);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ContentItem actualContentItem =
                await this.contentItemOrchestrationService.RetrievePublishedContentItemByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(inputGroupId))),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            // a public read never identifies the caller and, being a read, publishes no fact
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRetrieveFutureScheduledPublishedContentItemByGroupIdIfActorIsOwnerAsync()
        {
            // given: a published row scheduled in the future misses canonical visibility
            // until its PublishDate passes — the owner may still preview it
            Guid randomGroupId = Guid.NewGuid();
            Guid inputGroupId = randomGroupId;
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();
            string actorUserId = GetRandomString();

            ContentItem publishedContentItem = CreateRandomPubliclyVisibleContentItem(
                contentItemId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            publishedContentItem.GroupId = inputGroupId;
            publishedContentItem.PublishDate = currentDateTime.AddDays(1);
            publishedContentItem.CreatedBy = actorUserId;
            ContentItem expectedContentItem = publishedContentItem.DeepClone();

            IQueryable<ContentItem> storageContentItems = new[]
            {
                publishedContentItem
            }.AsQueryable();

            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: new ContentItem { GroupId = inputGroupId },
                securityContext: securityContext);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(inputGroupId))))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItems);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(actorUserId);

            // when
            ContentItem actualContentItem =
                await this.contentItemOrchestrationService.RetrievePublishedContentItemByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(securityContext),
                Times.Once);

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(Roles.Reviewer)]
        [InlineData(Roles.ContentItemReviewer)]
        [InlineData(Roles.Publisher)]
        [InlineData(Roles.ContentItemPublisher)]
        [InlineData(Roles.Admin)]
        public async Task ShouldRetrieveFutureScheduledPublishedContentItemByGroupIdIfActorHasReviewRoleAsync(
            string reviewRole)
        {
            // given: the moderation roles (§16.6) preview anyone's future-scheduled
            // published row for review and audit
            Guid randomGroupId = Guid.NewGuid();
            Guid inputGroupId = randomGroupId;
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            ContentItem publishedContentItem = CreateRandomPubliclyVisibleContentItem(
                contentItemId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            publishedContentItem.GroupId = inputGroupId;
            publishedContentItem.PublishDate = currentDateTime.AddDays(1);
            ContentItem expectedContentItem = publishedContentItem.DeepClone();

            IQueryable<ContentItem> storageContentItems = new[]
            {
                publishedContentItem
            }.AsQueryable();

            SecurityContext securityContext = CreateAuthenticatedSecurityContext(reviewRole);

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: new ContentItem { GroupId = inputGroupId },
                securityContext: securityContext);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(inputGroupId))))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItems);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(GetRandomString());

            // when
            ContentItem actualContentItem =
                await this.contentItemOrchestrationService.RetrievePublishedContentItemByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(securityContext),
                Times.Once);

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
