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

namespace Glory2Him.Core.Tests.Unit.Services.Processings.ContentItems
{
    public partial class ContentItemProcessingServiceTests
    {
        [Fact]
        public async Task ShouldRetrieveOnlyPublicGroupContentItemsOnRetrieveByGroupIdIfCallerIsAnonymousAsync()
        {
            // given: an anonymous caller reads only the publicly visible versions of the
            // requested group — other groups' rows and the group's own non-public and
            // deleted versions all drop out of the set
            Guid randomGroupId = Guid.NewGuid();
            Guid inputGroupId = randomGroupId;
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            ContentItem publicGroupContentItem = CreateRandomPubliclyVisibleContentItem(
                contentItemId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            publicGroupContentItem.GroupId = inputGroupId;

            ContentItem nonPublicGroupContentItem = CreateRandomNonPublicContentItem(
                createdBy: GetRandomString());

            nonPublicGroupContentItem.GroupId = inputGroupId;
            ContentItem deletedGroupContentItem = CreateRandomDeletedContentItem(currentDateTime);
            deletedGroupContentItem.GroupId = inputGroupId;

            ContentItem otherGroupContentItem = CreateRandomPubliclyVisibleContentItem(
                contentItemId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            IQueryable<ContentItem> storageContentItems = new[]
            {
                publicGroupContentItem,
                nonPublicGroupContentItem,
                deletedGroupContentItem,
                otherGroupContentItem
            }.AsQueryable();

            IQueryable<ContentItem> expectedContentItems = new[]
            {
                publicGroupContentItem.DeepClone()
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
            IQueryable<ContentItem> actualContentItems =
                await this.contentItemProcessingService.RetrieveContentItemsByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItems.Should().BeEquivalentTo(expectedContentItems);

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
        public async Task ShouldRetrievePublicAndOwnGroupContentItemsOnRetrieveByGroupIdIfCallerIsAuthenticatedAsync()
        {
            // given: the owner follows their own group through the workflow — their own
            // non-public versions join the group's public ones, while another user's
            // non-public version of the same group stays invisible
            Guid randomGroupId = Guid.NewGuid();
            Guid inputGroupId = randomGroupId;
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();
            string actorUserId = GetRandomString();

            ContentItem publicGroupContentItem = CreateRandomPubliclyVisibleContentItem(
                contentItemId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            publicGroupContentItem.GroupId = inputGroupId;

            ContentItem ownNonPublicGroupContentItem = CreateRandomNonPublicContentItem(
                createdBy: actorUserId);

            ownNonPublicGroupContentItem.GroupId = inputGroupId;

            ContentItem otherNonPublicGroupContentItem = CreateRandomNonPublicContentItem(
                createdBy: GetRandomString());

            otherNonPublicGroupContentItem.GroupId = inputGroupId;

            ContentItem ownOtherGroupContentItem = CreateRandomNonPublicContentItem(
                createdBy: actorUserId);

            IQueryable<ContentItem> storageContentItems = new[]
            {
                publicGroupContentItem,
                ownNonPublicGroupContentItem,
                otherNonPublicGroupContentItem,
                ownOtherGroupContentItem
            }.AsQueryable();

            IQueryable<ContentItem> expectedContentItems = new[]
            {
                publicGroupContentItem.DeepClone(),
                ownNonPublicGroupContentItem.DeepClone()
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
            IQueryable<ContentItem> actualContentItems =
                await this.contentItemProcessingService.RetrieveContentItemsByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItems.Should().BeEquivalentTo(expectedContentItems);

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
        public async Task ShouldRetrieveAllNonDeletedGroupContentItemsOnRetrieveByGroupIdIfActorHasReviewRoleAsync(
            string reviewRole)
        {
            // given: a review-role caller (§16.6) audits every non-deleted version of the
            // group — drafts of anyone included — without the clock or the caller's
            // identity ever being consulted; other groups stay out of the set
            Guid randomGroupId = Guid.NewGuid();
            Guid inputGroupId = randomGroupId;
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            ContentItem publicGroupContentItem = CreateRandomPubliclyVisibleContentItem(
                contentItemId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            publicGroupContentItem.GroupId = inputGroupId;

            ContentItem nonPublicGroupContentItem = CreateRandomNonPublicContentItem(
                createdBy: GetRandomString());

            nonPublicGroupContentItem.GroupId = inputGroupId;
            ContentItem deletedGroupContentItem = CreateRandomDeletedContentItem(currentDateTime);
            deletedGroupContentItem.GroupId = inputGroupId;

            ContentItem otherGroupContentItem = CreateRandomNonPublicContentItem(
                createdBy: GetRandomString());

            IQueryable<ContentItem> storageContentItems = new[]
            {
                publicGroupContentItem,
                nonPublicGroupContentItem,
                deletedGroupContentItem,
                otherGroupContentItem
            }.AsQueryable();

            IQueryable<ContentItem> expectedContentItems = new[]
            {
                publicGroupContentItem.DeepClone(),
                nonPublicGroupContentItem.DeepClone()
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

            // when
            IQueryable<ContentItem> actualContentItems =
                await this.contentItemProcessingService.RetrieveContentItemsByGroupIdAsync(
                    inputGroupId,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItems.Should().BeEquivalentTo(expectedContentItems);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
