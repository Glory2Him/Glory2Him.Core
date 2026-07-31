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
using Glory2Him.Core.Models.Events.Orchestrations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.ContentItems
{
    public partial class ContentItemOrchestrationServiceTests
    {
        [Theory]
        [InlineData(ApprovalStatus.Draft)]
        [InlineData(ApprovalStatus.Submitted)]
        [InlineData(ApprovalStatus.Rejected)]
        [InlineData(ApprovalStatus.Dismissed)]
        [InlineData(ApprovalStatus.Approved)]
        public async Task ShouldRemoveContentItemOnRemoveByIdIfActorIsOwnerAsync(
            ApprovalStatus approvalStatus)
        {
            // given: the owner may remove their own item at any point of the approval
            // workflow — deletion is not an ApprovalStatus (§10.5), so the status of the
            // row is irrelevant to the decision and is left untouched by the soft delete
            Guid randomContentItemId = Guid.NewGuid();
            Guid inputContentItemId = randomContentItemId;
            string randomDeletionReason = GetRandomString();
            string inputDeletionReason = randomDeletionReason;
            string actorUserId = GetRandomString();

            ContentItem storageContentItem = CreateRandomStorageContentItem(
                contentItemId: inputContentItemId,
                approvalStatus: approvalStatus,
                createdBy: actorUserId);

            ContentItem removedContentItem = storageContentItem.DeepClone();
            removedContentItem.IsDeleted = true;
            removedContentItem.DeletionReason = inputDeletionReason;
            ContentItem expectedContentItem = removedContentItem.DeepClone();
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: storageContentItem,
                securityContext: securityContext);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRemoveRequestAs(
                    inputContentItemId,
                    inputDeletionReason))))
                        .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(inputContentItemId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(actorUserId);

            this.contentItemServiceMock.Setup(service =>
                service.RemoveContentItemByIdAsync(
                    inputContentItemId,
                    inputDeletionReason,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(removedContentItem);

            EventEnvelope<ContentItem> outboundEnvelope = SetupCompletionFactPublish(
                inboundEnvelope: inboundEnvelope,
                resultContentItem: removedContentItem,
                operation: ContentItemOrchestrationEventOperation.Removed);

            // when
            ContentItem actualContentItem =
                await this.contentItemOrchestrationService.RemoveContentItemByIdAsync(
                    inputContentItemId,
                    inputDeletionReason,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.Is(SameRemoveRequestAs(
                    inputContentItemId,
                    inputDeletionReason))),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(inputContentItemId, It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(securityContext),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.RemoveContentItemByIdAsync(
                    inputContentItemId,
                    inputDeletionReason,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(inboundEnvelope, removedContentItem),
                Times.Once);

            VerifyCompletionFactPublished(
                outboundEnvelope: outboundEnvelope,
                operation: ContentItemOrchestrationEventOperation.Removed);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldRemoveContentItemOnRemoveByIdIfActorIsAdminAsync()
        {
            // given: an Admin may take down anyone's content (§16.6) — the owner check
            // fails but the Admin role carries the takedown on its own
            Guid randomContentItemId = Guid.NewGuid();
            Guid inputContentItemId = randomContentItemId;

            ContentItem storageContentItem = CreateRandomStorageContentItem(
                contentItemId: inputContentItemId,
                approvalStatus: ApprovalStatus.Approved,
                createdBy: GetRandomString());

            ContentItem removedContentItem = storageContentItem.DeepClone();
            removedContentItem.IsDeleted = true;
            SecurityContext securityContext = CreateAuthenticatedSecurityContext(Roles.Admin);

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: storageContentItem,
                securityContext: securityContext);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameRemoveRequestAs(inputContentItemId, null))))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(inputContentItemId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(GetRandomString());

            this.contentItemServiceMock.Setup(service =>
                service.RemoveContentItemByIdAsync(
                    inputContentItemId,
                    null,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(removedContentItem);

            EventEnvelope<ContentItem> outboundEnvelope = SetupCompletionFactPublish(
                inboundEnvelope: inboundEnvelope,
                resultContentItem: removedContentItem,
                operation: ContentItemOrchestrationEventOperation.Removed);

            // when
            ContentItem actualContentItem =
                await this.contentItemOrchestrationService.RemoveContentItemByIdAsync(
                    inputContentItemId,
                    deletionReason: null,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(removedContentItem);

            this.contentItemServiceMock.Verify(service =>
                service.RemoveContentItemByIdAsync(
                    inputContentItemId,
                    null,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.contentItemServiceMock.Verify(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Never);

            VerifyCompletionFactPublished(
                outboundEnvelope: outboundEnvelope,
                operation: ContentItemOrchestrationEventOperation.Removed);

            this.hashBrokerMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
