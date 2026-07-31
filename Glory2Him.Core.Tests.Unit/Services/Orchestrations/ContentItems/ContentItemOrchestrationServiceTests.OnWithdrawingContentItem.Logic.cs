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
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.ContentItems
{
    public partial class ContentItemOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldWithdrawContentItemAndReplyOnWithdrawingContentItemEventAsync()
        {
            // given: the ContentItem-Withdrawing request path converges on the same do-work
            // as the direct WithdrawingContentItemAsync — the request payload carries the
            // withdraw instruction (Id and DeletionReason), the envelope carries the original
            // caller for the gate and the owner/Admin rule, and the reply is the next
            // envelope in the causation chain wrapping the withdrawn entity
            Guid randomContentItemId = Guid.NewGuid();
            string randomDeletionReason = GetRandomString();
            string actorUserId = GetRandomString();

            var withdrawRequest = new ContentItem
            {
                Id = randomContentItemId,
                DeletionReason = randomDeletionReason
            };

            ContentItem storageContentItem = CreateRandomStorageContentItem(
                contentItemId: randomContentItemId,
                approvalStatus: ApprovalStatus.Submitted,
                createdBy: actorUserId);

            ContentItem withdrawnContentItem = storageContentItem.DeepClone();
            withdrawnContentItem.IsDeleted = true;
            withdrawnContentItem.DeletionReason = randomDeletionReason;
            ContentItem expectedContentItem = withdrawnContentItem.DeepClone();

            EventEnvelope<ContentItem> requestEnvelope = CreateEventEnvelope(
                contentItem: withdrawRequest,
                securityContext: CreateAuthenticatedSecurityContext());

            var expectedReplyEnvelope = new EventEnvelope<ContentItem>
            {
                Content = withdrawnContentItem,
                SecurityContext = requestEnvelope.SecurityContext,

                Metadata = new EventMetadata
                {
                    EventId = Guid.NewGuid(),
                    CausationId = requestEnvelope.Metadata.EventId.ToString()
                }
            };

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(randomContentItemId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(requestEnvelope.SecurityContext))
                    .ReturnsAsync(actorUserId);

            this.contentItemServiceMock.Setup(service =>
                service.RemoveContentItemByIdAsync(
                    randomContentItemId,
                    randomDeletionReason,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(withdrawnContentItem);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateNextAsync(requestEnvelope, withdrawnContentItem))
                    .ReturnsAsync(expectedReplyEnvelope);

            // when
            EventEnvelope<ContentItem>? actualReplyEnvelope =
                await this.contentItemOrchestrationService.OnWithdrawingContentItemAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope.Should().BeSameAs(expectedReplyEnvelope);
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedContentItem);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(randomContentItemId, It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(requestEnvelope.SecurityContext),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.RemoveContentItemByIdAsync(
                    randomContentItemId,
                    randomDeletionReason,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(requestEnvelope, withdrawnContentItem),
                Times.Once);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
