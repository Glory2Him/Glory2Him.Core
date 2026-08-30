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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.ContentItems
{
    public partial class ContentItemProcessingServiceTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldRetrieveContentItemByIdIfContentItemIsPubliclyVisibleAsync(
            bool hasPublishDate)
        {
            // given: a version that satisfies canonical content visibility (§14.1) is
            // readable by anyone — here the caller is anonymous and never identified
            Guid randomContentItemId = Guid.NewGuid();
            Guid inputContentItemId = randomContentItemId;
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            ContentItem storageContentItem = CreateRandomPubliclyVisibleContentItem(
                contentItemId: inputContentItemId,
                currentDateTime: currentDateTime,
                hasPublishDate: hasPublishDate);

            ContentItem expectedContentItem = storageContentItem.DeepClone();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: storageContentItem,
                securityContext: new SecurityContext { IsAuthenticated = false });

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveRequestAs(inputContentItemId))))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(inputContentItemId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ContentItem actualContentItem =
                await this.contentItemProcessingService.RetrieveContentItemByIdAsync(
                    inputContentItemId,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.Is(SameRetrieveRequestAs(inputContentItemId))),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(inputContentItemId, It.IsAny<CancellationToken>()),
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

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.ContentItemReadOnly)]
        public async Task ShouldRetrieveContentItemByIdIfContentItemIsPubliclyVisibleAndActorHasBlockRoleAsync(
            string blockRole)
        {
            // given: the block roles only block contributions (§16.6) — a blocked user
            // still reads public content like everyone else
            Guid randomContentItemId = Guid.NewGuid();
            Guid inputContentItemId = randomContentItemId;
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            ContentItem storageContentItem = CreateRandomPubliclyVisibleContentItem(
                contentItemId: inputContentItemId,
                currentDateTime: currentDateTime,
                hasPublishDate: true);

            ContentItem expectedContentItem = storageContentItem.DeepClone();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: storageContentItem,
                securityContext: CreateAuthenticatedSecurityContext(blockRole));

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveRequestAs(inputContentItemId))))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(inputContentItemId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ContentItem actualContentItem =
                await this.contentItemProcessingService.RetrieveContentItemByIdAsync(
                    inputContentItemId,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(inputContentItemId, It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(ApprovalStatus.Draft)]
        [InlineData(ApprovalStatus.Submitted)]
        [InlineData(ApprovalStatus.Rejected)]
        [InlineData(ApprovalStatus.Dismissed)]
        [InlineData(ApprovalStatus.Approved)]
        public async Task ShouldRetrieveNonPublicContentItemOnRetrieveByIdIfActorIsOwnerAsync(
            ApprovalStatus approvalStatus)
        {
            // given: the owner follows their own item through the whole approval
            // workflow — an unpublished version of any status stays readable to them
            Guid randomContentItemId = Guid.NewGuid();
            Guid inputContentItemId = randomContentItemId;
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();
            string actorUserId = GetRandomString();

            ContentItem storageContentItem = CreateRandomStorageContentItem(
                contentItemId: inputContentItemId,
                approvalStatus: approvalStatus,
                createdBy: actorUserId);

            storageContentItem.IsPublished = false;
            ContentItem expectedContentItem = storageContentItem.DeepClone();
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: storageContentItem,
                securityContext: securityContext);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveRequestAs(inputContentItemId))))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(inputContentItemId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(actorUserId);

            // when
            ContentItem actualContentItem =
                await this.contentItemProcessingService.RetrieveContentItemByIdAsync(
                    inputContentItemId,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(inputContentItemId, It.IsAny<CancellationToken>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(securityContext),
                Times.Once);

            this.eventBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.Is(SameRetrieveRequestAs(inputContentItemId))),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(Roles.Reviewers)]
        [InlineData(Roles.ContentItemReviewers)]
        [InlineData(Roles.Publishers)]
        [InlineData(Roles.ContentItemPublishers)]
        [InlineData(Roles.Administrators)]
        public async Task ShouldRetrieveNonPublicContentItemOnRetrieveByIdIfActorHasReviewRoleAsync(
            string reviewRole)
        {
            // given: the moderation roles (§16.6) read non-public versions of anyone's
            // content for review and audit
            Guid randomContentItemId = Guid.NewGuid();
            Guid inputContentItemId = randomContentItemId;
            DateTimeOffset currentDateTime = GetRandomDateTimeOffset();

            ContentItem storageContentItem = CreateRandomStorageContentItem(
                contentItemId: inputContentItemId,
                approvalStatus: ApprovalStatus.Submitted,
                createdBy: GetRandomString());

            storageContentItem.IsPublished = false;
            ContentItem expectedContentItem = storageContentItem.DeepClone();
            SecurityContext securityContext = CreateAuthenticatedSecurityContext(reviewRole);

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: storageContentItem,
                securityContext: securityContext);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRetrieveRequestAs(inputContentItemId))))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(inputContentItemId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(GetRandomString());

            // when
            ContentItem actualContentItem =
                await this.contentItemProcessingService.RetrieveContentItemByIdAsync(
                    inputContentItemId,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(inputContentItemId, It.IsAny<CancellationToken>()),
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
