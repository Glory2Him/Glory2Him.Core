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
        public async Task ShouldAddContentItemAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;
            string normalizedContent = NormalizeContent(inputContentItem.Content);
            string expectedContentHash = ComputeContentHash(inputContentItem.Content);
            Guid contentItemId = Guid.NewGuid();
            Guid groupId = Guid.NewGuid();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            var expectedMappedContentItem = new ContentItem
            {
                Id = contentItemId,
                ContentType = inputContentItem.ContentType,
                Title = inputContentItem.Title,
                Author = inputContentItem.Author,
                Content = inputContentItem.Content,

                // the caller's publish date does not ride in on the add — a fresh row has
                // none until approve grants one, which is why it lands unpublished in Draft
                PublishDate = null,
                ContentHash = expectedContentHash,
                GroupId = groupId,

                // version 1 of a brand-new group, which is the whole of what makes it the
                // tip: the tip is the highest Version in the group, not a stored flag
                Version = 1,
                IsPublished = false,
                ApprovalStatus = ApprovalStatus.Draft,
                IsDeleted = false
            };

            ContentItem addedContentItem = expectedMappedContentItem.DeepClone();
            ContentItem expectedContentItem = addedContentItem.DeepClone();

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync(normalizedContent))
                    .ReturnsAsync(expectedContentHash);

            this.contentItemServiceMock.Setup(service =>
                service.CheckContentItemContentExistsAsync(
                    inputContentItem.ContentType,
                    expectedContentHash,
                    null,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            this.identifierBrokerMock.SetupSequence(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(contentItemId)
                    .ReturnsAsync(groupId);

            ContentItem? capturedContentItem = null;

            this.contentItemServiceMock.Setup(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
                    .Callback<ContentItem, CancellationToken>((contentItem, cancellationToken) =>
                        capturedContentItem = contentItem)
                    .ReturnsAsync(addedContentItem);

            EventEnvelope<ContentItem> outboundEnvelope = SetupCompletionFactPublish(
                inboundEnvelope: inboundEnvelope,
                resultContentItem: addedContentItem,
                operation: ContentItemProcessingEventOperation.Added);

            // when
            ContentItem actualContentItem =
                await this.contentItemProcessingService.AddContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);
            capturedContentItem.Should().BeEquivalentTo(expectedMappedContentItem);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(inputContentItem),
                Times.Once);

            this.hashBrokerMock.Verify(broker =>
                broker.ComputeSha256HashAsync(normalizedContent),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.CheckContentItemContentExistsAsync(
                    inputContentItem.ContentType,
                    expectedContentHash,
                    null,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.identifierBrokerMock.Verify(broker =>
                broker.GetIdentifierAsync(),
                Times.Exactly(2));

            this.contentItemServiceMock.Verify(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(inboundEnvelope, addedContentItem),
                Times.Once);

            VerifyCompletionFactPublished(
                outboundEnvelope: outboundEnvelope,
                operation: ContentItemProcessingEventOperation.Added);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldComputeContentHashPerFrozenContractOnAddAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;
            inputContentItem.Content = "  Hello \n\n\t WORLD  ";

            // SHA-256 of "hello world" — pins the frozen normalization contract (§3.4.2):
            // trim ends, collapse whitespace runs to one space, lowercase, lowercase hex.
            // The hash broker mock only matches the exact normalized text, so this test
            // fails if normalization drifts; HashBrokerTests pins the hashing itself.
            string expectedContentHash =
                "b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9";

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync("hello world"))
                    .ReturnsAsync(expectedContentHash);

            this.contentItemServiceMock.Setup(service =>
                service.CheckContentItemContentExistsAsync(
                    inputContentItem.ContentType,
                    expectedContentHash,
                    null,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            ContentItem? capturedContentItem = null;

            this.contentItemServiceMock.Setup(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
                    .Callback<ContentItem, CancellationToken>((contentItem, cancellationToken) =>
                        capturedContentItem = contentItem)
                    .ReturnsAsync(inputContentItem);

            // when
            await this.contentItemProcessingService.AddContentItemAsync(
                inputContentItem,
                TestContext.Current.CancellationToken);

            // then
            capturedContentItem!.ContentHash.Should().Be(expectedContentHash);
        }

        [Fact]
        public async Task ShouldNotCarryPublishDateOnAddAsync()
        {
            // given: PublishDate is an IApproval member, so under §9.7.1 rule 2's subtraction
            // rule it is not content — and the add surface may carry an ApprovalStatus of
            // Draft or Submitted and nothing else: never IsPublished, never PublishDate. The
            // new row already lands unpublished and in Draft; taking the caller's publish date
            // as well would let them schedule their own publication on the way in, without
            // ever meeting the approve gate that owns it.
            ContentItem inputContentItem = CreateRandomContentItem();
            inputContentItem.PublishDate = GetRandomDateTimeOffset();
            string normalizedContent = NormalizeContent(inputContentItem.Content);
            string expectedContentHash = ComputeContentHash(inputContentItem.Content);
            Guid contentItemId = Guid.NewGuid();
            Guid groupId = Guid.NewGuid();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            var expectedMappedContentItem = new ContentItem
            {
                Id = contentItemId,
                ContentType = inputContentItem.ContentType,
                Title = inputContentItem.Title,
                Author = inputContentItem.Author,
                Content = inputContentItem.Content,
                PublishDate = null,
                ContentHash = expectedContentHash,
                GroupId = groupId,

                // version 1 of a brand-new group, which is the whole of what makes it the
                // tip: the tip is the highest Version in the group, not a stored flag
                Version = 1,
                IsPublished = false,
                ApprovalStatus = ApprovalStatus.Draft,
                IsDeleted = false
            };

            ContentItem addedContentItem = expectedMappedContentItem.DeepClone();

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync(normalizedContent))
                    .ReturnsAsync(expectedContentHash);

            this.contentItemServiceMock.Setup(service =>
                service.CheckContentItemContentExistsAsync(
                    inputContentItem.ContentType,
                    expectedContentHash,
                    null,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            this.identifierBrokerMock.SetupSequence(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(contentItemId)
                    .ReturnsAsync(groupId);

            ContentItem? capturedContentItem = null;

            this.contentItemServiceMock.Setup(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
                    .Callback<ContentItem, CancellationToken>((contentItem, cancellationToken) =>
                        capturedContentItem = contentItem)
                    .ReturnsAsync(addedContentItem);

            SetupCompletionFactPublish(
                inboundEnvelope: inboundEnvelope,
                resultContentItem: addedContentItem,
                operation: ContentItemProcessingEventOperation.Added);

            // when
            await this.contentItemProcessingService.AddContentItemAsync(
                inputContentItem,
                TestContext.Current.CancellationToken);

            // then
            capturedContentItem.Should().BeEquivalentTo(expectedMappedContentItem);
            capturedContentItem!.PublishDate.Should().BeNull();
        }

        [Fact]
        public async Task ShouldCreateContentItemOnAddIfMatchingContentIsDeletedOrOtherContentTypeAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;
            string normalizedContent = NormalizeContent(inputContentItem.Content);
            string contentHash = ComputeContentHash(inputContentItem.Content);

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync(normalizedContent))
                    .ReturnsAsync(contentHash);

            // the foundation probe already excludes soft-deleted rows and other content
            // types, so a matching row in either state reports no duplicate
            this.contentItemServiceMock.Setup(service =>
                service.CheckContentItemContentExistsAsync(
                    inputContentItem.ContentType,
                    contentHash,
                    null,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            this.contentItemServiceMock.Setup(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(inputContentItem);

            // when
            ContentItem actualContentItem =
                await this.contentItemProcessingService.AddContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().NotBeNull();

            this.contentItemServiceMock.Verify(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
