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
using Glory2Him.Core.Models.Events.Processings;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.ContentItems
{
    public partial class ContentItemProcessingServiceTests
    {
        [Fact]
        public async Task ShouldAddContentItemAndReplyOnAddingContentItemEventAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;
            string normalizedContent = NormalizeContent(inputContentItem.Content);
            string contentHash = ComputeContentHash(inputContentItem.Content);
            ContentItem addedContentItem = inputContentItem.DeepClone();
            addedContentItem.ContentHash = contentHash;
            ContentItem expectedContentItem = addedContentItem.DeepClone();

            EventEnvelope<ContentItem> requestEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            var expectedReplyEnvelope = new EventEnvelope<ContentItem>
            {
                Content = addedContentItem,
                SecurityContext = requestEnvelope.SecurityContext,

                Metadata = new EventMetadata
                {
                    EventId = Guid.NewGuid(),
                    CausationId = requestEnvelope.Metadata.EventId.ToString()
                }
            };

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync(normalizedContent))
                    .ReturnsAsync(contentHash);

            this.contentItemServiceMock.Setup(service =>
                service.CheckContentItemContentExistsAsync(
                    inputContentItem.ContentType,
                    contentHash,
                    null,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(Guid.NewGuid());

            this.contentItemServiceMock.Setup(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(addedContentItem);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateNextAsync(requestEnvelope, addedContentItem))
                    .ReturnsAsync(expectedReplyEnvelope);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishContentItemProcessingAsync(
                    expectedReplyEnvelope,
                    ContentItemProcessingEventOperation.Added))
                        .ReturnsAsync(new EventPublishResult<ContentItem>());

            // when
            EventEnvelope<ContentItem>? actualReplyEnvelope =
                await this.contentItemProcessingService.OnAddingContentItemAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().NotBeNull();
            actualReplyEnvelope.Should().BeSameAs(expectedReplyEnvelope);
            actualReplyEnvelope!.Content.Should().BeEquivalentTo(expectedContentItem);

            this.hashBrokerMock.Verify(broker =>
                broker.ComputeSha256HashAsync(normalizedContent),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.CheckContentItemContentExistsAsync(
                    inputContentItem.ContentType,
                    contentHash,
                    null,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Once);

            this.identifierBrokerMock.Verify(broker =>
                broker.GetIdentifierAsync(),
                Times.Exactly(2));

            // twice on the event path: once for the completion fact inside the do-work,
            // once for the reply envelope the substrate hands back to the requester
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(requestEnvelope, addedContentItem),
                Times.Exactly(2));

            this.eventBrokerMock.Verify(broker =>
                broker.PublishContentItemProcessingAsync(
                    expectedReplyEnvelope,
                    ContentItemProcessingEventOperation.Added),
                Times.Once);

            this.eventBrokerMock.VerifyNoOtherCalls();

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        // §3.4.2 rule 6 on the event path. The requester is answered exactly as a genuine add
        // answers — the reply envelope carries the same shape of content item — while nothing
        // is written and, critically, the completion fact is NOT published. The reply is
        // recorded as this delivery's response and reaches the requester alone; the fact would
        // tell every subscriber a row exists that does not. That asymmetry is the whole reason
        // the acknowledgement is safe to send here.
        [Fact]
        public async Task ShouldAcknowledgeDuplicateContentOnAddingContentItemEventWithoutCreatingItAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;
            string normalizedContent = NormalizeContent(inputContentItem.Content);
            string contentHash = ComputeContentHash(inputContentItem.Content);
            Guid contentItemId = Guid.NewGuid();
            Guid groupId = Guid.NewGuid();
            string randomActor = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            EventEnvelope<ContentItem> requestEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            var expectedAcknowledgedContentItem = new ContentItem
            {
                Id = contentItemId,
                ContentType = inputContentItem.ContentType,
                Title = inputContentItem.Title,
                Author = inputContentItem.Author,
                Content = inputContentItem.Content,
                ShareabilityBasis = inputContentItem.ShareabilityBasis,
                SharePermission = inputContentItem.SharePermission,
                PublishDate = null,
                ContentHash = contentHash,
                GroupId = groupId,
                Version = 1,
                IsPublished = false,
                ApprovalStatus = ApprovalStatus.Draft,
                IsDeleted = false,

                // Asserted, not waved at. The reply must carry stamps because a genuine reply
                // does; a mock that handed the item back untouched would let the stamping be
                // deleted without a test noticing, and the audit columns are part of what makes
                // the two arms indistinguishable.
                CreatedBy = randomActor,
                UpdatedBy = randomActor,
                CreatedWhen = randomDateTimeOffset,
                UpdatedWhen = randomDateTimeOffset
            };

            var expectedReplyEnvelope = new EventEnvelope<ContentItem>
            {
                Content = expectedAcknowledgedContentItem,
                SecurityContext = requestEnvelope.SecurityContext,

                Metadata = new EventMetadata
                {
                    EventId = Guid.NewGuid(),
                    CausationId = requestEnvelope.Metadata.EventId.ToString()
                }
            };

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync(normalizedContent))
                    .ReturnsAsync(contentHash);

            this.contentItemServiceMock.Setup(service =>
                service.CheckContentItemContentExistsAsync(
                    inputContentItem.ContentType,
                    contentHash,
                    null,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            this.identifierBrokerMock.SetupSequence(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(contentItemId)
                    .ReturnsAsync(groupId);

            // THE STAMPS DO NOT COME OFF THE REQUEST ENVELOPE, and this is the path where that
            // matters. The genuine arm's foundation mints its own envelope off the ambient
            // context; during event delivery that is not the envelope's requester. So the quiet
            // arm mints one the same way, and this setup matches ONLY that envelope's context —
            // a service that stamped from requestEnvelope.SecurityContext would find no setup,
            // get a null item back, and fail here rather than shipping a reply a requester could
            // tell apart from a real one.
            EventEnvelope<ContentItem> auditEnvelope = CreateEventEnvelope(
                contentItem: new ContentItem(),
                securityContext: CreateAuthenticatedSecurityContext());

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is<ContentItem>(item => item.Id == contentItemId)))
                    .ReturnsAsync(auditEnvelope);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(
                    It.IsAny<ContentItem>(),
                    auditEnvelope.SecurityContext))
                        .ReturnsAsync((ContentItem contentItem, SecurityContext _) =>
                        {
                            contentItem.CreatedBy = randomActor;
                            contentItem.UpdatedBy = randomActor;
                            contentItem.CreatedWhen = randomDateTimeOffset;
                            contentItem.UpdatedWhen = randomDateTimeOffset;

                            return contentItem;
                        });

            ContentItem? repliedContentItem = null;

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateNextAsync(requestEnvelope, It.IsAny<ContentItem>()))
                    .Callback<EventEnvelope<ContentItem>, ContentItem>(
                        (_, contentItem) => repliedContentItem = contentItem)
                    .ReturnsAsync(expectedReplyEnvelope);

            // when
            EventEnvelope<ContentItem>? actualReplyEnvelope =
                await this.contentItemProcessingService.OnAddingContentItemAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            // then
            actualReplyEnvelope.Should().BeSameAs(expectedReplyEnvelope);
            repliedContentItem.Should().BeEquivalentTo(expectedAcknowledgedContentItem);

            this.hashBrokerMock.Verify(broker =>
                broker.ComputeSha256HashAsync(normalizedContent),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.CheckContentItemContentExistsAsync(
                    inputContentItem.ContentType,
                    contentHash,
                    null,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.identifierBrokerMock.Verify(broker =>
                broker.GetIdentifierAsync(),
                Times.Exactly(2));

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(
                    It.IsAny<ContentItem>(),
                    auditEnvelope.SecurityContext),
                Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.Is<ContentItem>(item => item.Id == contentItemId)),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Never);

            // ONCE, not twice: the reply is minted, the completion fact is not
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(requestEnvelope, It.IsAny<ContentItem>()),
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
