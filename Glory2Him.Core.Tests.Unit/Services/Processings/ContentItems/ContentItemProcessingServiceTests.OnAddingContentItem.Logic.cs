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

    }
}
