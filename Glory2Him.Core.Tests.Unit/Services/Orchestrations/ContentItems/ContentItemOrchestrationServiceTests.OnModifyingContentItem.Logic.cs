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
using Glory2Him.Core.Models.Events.Orchestrations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.ContentItems
{
    public partial class ContentItemOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldModifyContentItemAndReplyOnModifyingContentItemEventAsync()
        {
            // given: the ContentItemOrchestration-Modifying request path converges on the same do-work
            // as the direct ModifyContentItemAsync — the envelope carries the original
            // caller for the gate and the permission matrix, and the reply is the next
            // envelope in the causation chain wrapping the modified entity
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;
            string normalizedContent = NormalizeContent(inputContentItem.Content);
            string contentHash = ComputeContentHash(inputContentItem.Content);
            string actorUserId = GetRandomString();

            ContentItem storageContentItem = CreateRandomStorageContentItem(
                contentItemId: inputContentItem.Id,
                approvalStatus: ApprovalStatus.Draft,
                createdBy: actorUserId);

            ContentItem updatedContentItem = storageContentItem.DeepClone();
            updatedContentItem.Content = inputContentItem.Content;
            updatedContentItem.ContentHash = contentHash;
            ContentItem expectedContentItem = updatedContentItem.DeepClone();

            EventEnvelope<ContentItem> requestEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            var expectedReplyEnvelope = new EventEnvelope<ContentItem>
            {
                Content = updatedContentItem,
                SecurityContext = requestEnvelope.SecurityContext,

                Metadata = new EventMetadata
                {
                    EventId = Guid.NewGuid(),
                    CausationId = requestEnvelope.Metadata.EventId.ToString()
                }
            };

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(inputContentItem.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(requestEnvelope.SecurityContext))
                    .ReturnsAsync(actorUserId);

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync(normalizedContent))
                    .ReturnsAsync(contentHash);

            this.contentItemServiceMock.Setup(service =>
                service.CheckContentItemContentExistsAsync(
                    inputContentItem.ContentType,
                    contentHash,
                    storageContentItem.GroupId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            this.contentItemServiceMock.Setup(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(updatedContentItem);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateNextAsync(requestEnvelope, updatedContentItem))
                    .ReturnsAsync(expectedReplyEnvelope);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemOrchestrationAsync(
                    expectedReplyEnvelope,
                    ContentItemOrchestrationEventOperation.Modified))
                        .ReturnsAsync(new EventPublishResult<ContentItem>());

            // when
            EventEnvelope<ContentItem>? actualReplyEnvelope =
                await this.contentItemOrchestrationService.OnModifyingContentItemAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope.Should().BeSameAs(expectedReplyEnvelope);
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedContentItem);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(inputContentItem.Id, It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(requestEnvelope.SecurityContext),
                Times.Once);

            this.hashBrokerMock.Verify(broker =>
                broker.ComputeSha256HashAsync(normalizedContent),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.CheckContentItemContentExistsAsync(
                    inputContentItem.ContentType,
                    contentHash,
                    storageContentItem.GroupId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Once);

            // twice on the event path: once for the completion fact inside the do-work,
            // once for the reply envelope the substrate hands back to the requester
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(requestEnvelope, updatedContentItem),
                Times.Exactly(2));

            this.eventBrokerMock.Verify(broker =>
                broker.PublishContentItemOrchestrationAsync(
                    expectedReplyEnvelope,
                    ContentItemOrchestrationEventOperation.Modified),
                Times.Once);

            this.eventBrokerMock.VerifyNoOtherCalls();

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
