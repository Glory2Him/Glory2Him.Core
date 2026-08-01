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
        [Fact]
        public async Task ShouldRetrieveOnlyPublicContentItemsOnRetrieveAllIfCallerIsAnonymousAsync()
        {
            // given: an anonymous caller sees the canonical visible set alone (§14.1) —
            // drafts, future-scheduled rows and deleted rows all drop out of the set,
            // and the caller is never identified
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            ContentItem publicContentItem = CreateRandomPubliclyVisibleContentItem(
                contentItemId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            ContentItem publicNoDateContentItem = CreateRandomPubliclyVisibleContentItem(
                contentItemId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: false);

            ContentItem nonPublicContentItem = CreateRandomNonPublicContentItem(
                createdBy: GetRandomString());

            ContentItem futureContentItem = CreateRandomPubliclyVisibleContentItem(
                contentItemId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            futureContentItem.PublishDate = currentDateTime.AddDays(1);
            ContentItem deletedContentItem = CreateRandomDeletedContentItem(currentDateTime);

            IQueryable<ContentItem> storageContentItems = new[]
            {
                publicContentItem,
                publicNoDateContentItem,
                nonPublicContentItem,
                futureContentItem,
                deletedContentItem
            }.AsQueryable();

            IQueryable<ContentItem> expectedContentItems = new[]
            {
                publicContentItem.DeepClone(),
                publicNoDateContentItem.DeepClone()
            }.AsQueryable();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: new ContentItem(),
                securityContext: new SecurityContext { IsAuthenticated = false });

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveAllRequest())))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItems);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            IQueryable<ContentItem> actualContentItems =
                await this.contentItemOrchestrationService.RetrieveAllContentItemsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualContentItems.Should().BeEquivalentTo(expectedContentItems);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.Is(SameRetrieveAllRequest())),
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
        public async Task ShouldRetrievePublicAndOwnContentItemsOnRetrieveAllIfCallerIsAuthenticatedAsync()
        {
            // given: an authenticated caller without a review role follows their own items
            // through the workflow — their own rows in any state join the public set, while
            // other users' non-public rows stay invisible
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();
            string actorUserId = GetRandomString();

            ContentItem publicContentItem = CreateRandomPubliclyVisibleContentItem(
                contentItemId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            ContentItem ownNonPublicContentItem = CreateRandomNonPublicContentItem(
                createdBy: actorUserId);

            ContentItem otherNonPublicContentItem = CreateRandomNonPublicContentItem(
                createdBy: GetRandomString());

            ContentItem ownDeletedContentItem = CreateRandomDeletedContentItem(currentDateTime);
            ownDeletedContentItem.CreatedBy = actorUserId;

            IQueryable<ContentItem> storageContentItems = new[]
            {
                publicContentItem,
                ownNonPublicContentItem,
                otherNonPublicContentItem,
                ownDeletedContentItem
            }.AsQueryable();

            IQueryable<ContentItem> expectedContentItems = new[]
            {
                publicContentItem.DeepClone(),
                ownNonPublicContentItem.DeepClone()
            }.AsQueryable();

            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

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
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(actorUserId);

            // when
            IQueryable<ContentItem> actualContentItems =
                await this.contentItemOrchestrationService.RetrieveAllContentItemsAsync(
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
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ShouldRetrieveOnlyPublicContentItemsOnRetrieveAllIfActorUserIdIsUnresolvedAsync(
            string? unresolvedActorUserId)
        {
            // given: an authenticated caller whose identity cannot be resolved must not
            // accidentally match rows whose CreatedBy is also blank — they read as public
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            ContentItem publicContentItem = CreateRandomPubliclyVisibleContentItem(
                contentItemId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            ContentItem blankOwnerNonPublicContentItem = CreateRandomNonPublicContentItem(
                createdBy: string.Empty);

            IQueryable<ContentItem> storageContentItems = new[]
            {
                publicContentItem,
                blankOwnerNonPublicContentItem
            }.AsQueryable();

            IQueryable<ContentItem> expectedContentItems = new[]
            {
                publicContentItem.DeepClone()
            }.AsQueryable();

            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

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
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(unresolvedActorUserId!);

            // when
            IQueryable<ContentItem> actualContentItems =
                await this.contentItemOrchestrationService.RetrieveAllContentItemsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualContentItems.Should().BeEquivalentTo(expectedContentItems);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(securityContext),
                Times.Once);

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.Verify(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

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
        public async Task ShouldRetrieveAllNonDeletedContentItemsOnRetrieveAllIfActorHasReviewRoleAsync(
            string reviewRole)
        {
            // given: a review-role caller (§16.6) audits the whole pipeline — drafts and
            // future-scheduled rows of anyone stay in the set; only deleted rows are gone,
            // and neither the clock nor the caller's identity is ever consulted
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            ContentItem publicContentItem = CreateRandomPubliclyVisibleContentItem(
                contentItemId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            ContentItem nonPublicContentItem = CreateRandomNonPublicContentItem(
                createdBy: GetRandomString());

            ContentItem futureContentItem = CreateRandomPubliclyVisibleContentItem(
                contentItemId: Guid.NewGuid(),
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            futureContentItem.PublishDate = currentDateTime.AddDays(1);
            ContentItem deletedContentItem = CreateRandomDeletedContentItem(currentDateTime);

            IQueryable<ContentItem> storageContentItems = new[]
            {
                publicContentItem,
                nonPublicContentItem,
                futureContentItem,
                deletedContentItem
            }.AsQueryable();

            IQueryable<ContentItem> expectedContentItems = new[]
            {
                publicContentItem.DeepClone(),
                nonPublicContentItem.DeepClone(),
                futureContentItem.DeepClone()
            }.AsQueryable();

            SecurityContext securityContext = CreateAuthenticatedSecurityContext(reviewRole);

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: new ContentItem(),
                securityContext: securityContext);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveAllRequest())))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItems);

            // when
            IQueryable<ContentItem> actualContentItems =
                await this.contentItemOrchestrationService.RetrieveAllContentItemsAsync(
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
