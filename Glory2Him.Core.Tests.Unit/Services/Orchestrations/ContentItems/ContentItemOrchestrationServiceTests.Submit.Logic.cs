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
using Glory2Him.Core.Models.Orchestrations.ContentItems;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Orchestrations.ContentItems
{
    public partial class ContentItemOrchestrationServiceTests
    {
        [Fact]
        public async Task ShouldSubmitContentItemAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;
            string normalizedContent = NormalizeContent(inputContentItem.Content);
            string expectedContentHash = ComputeContentHash(inputContentItem.Content);
            Guid contentItemId = Guid.NewGuid();
            Guid contentItemGroupId = Guid.NewGuid();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            var expectedMappedContentItem = new ContentItem
            {
                Id = contentItemId,
                ContentTypeId = inputContentItem.ContentTypeId,
                Title = inputContentItem.Title,
                Author = inputContentItem.Author,
                Content = inputContentItem.Content,
                PublishDate = inputContentItem.PublishDate,
                ContentHash = expectedContentHash,
                ContentItemGroupId = contentItemGroupId,
                Version = 1,
                IsLatestVersion = true,
                IsPublished = false,
                ApprovalStatus = ApprovalStatus.Draft,
                IsDeleted = false
            };

            ContentItem addedContentItem = expectedMappedContentItem.DeepClone();

            var expectedContentItemSubmissionResult = new ContentItemSubmissionResult
            {
                IsCreated = true,
                ContentItem = addedContentItem.DeepClone(),
                Message = "Thank you for your submission."
            };

            this.eventEnvelopeFactoryMock.Setup(factory =>
                factory.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync(normalizedContent))
                    .ReturnsAsync(expectedContentHash);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Enumerable.Empty<ContentItem>().AsQueryable());

            this.identifierBrokerMock.SetupSequence(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(contentItemId)
                    .ReturnsAsync(contentItemGroupId);

            ContentItem? capturedContentItem = null;

            this.contentItemServiceMock.Setup(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
                    .Callback<ContentItem, CancellationToken>((contentItem, cancellationToken) =>
                        capturedContentItem = contentItem)
                    .ReturnsAsync(addedContentItem);

            // when
            ContentItemSubmissionResult actualContentItemSubmissionResult =
                await this.contentItemOrchestrationService.SubmitContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItemSubmissionResult.Should().BeEquivalentTo(expectedContentItemSubmissionResult);
            capturedContentItem.Should().BeEquivalentTo(expectedMappedContentItem);

            this.eventEnvelopeFactoryMock.Verify(factory =>
                factory.CreateAsync(inputContentItem),
                Times.Once);

            this.hashBrokerMock.Verify(broker =>
                broker.ComputeSha256HashAsync(normalizedContent),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.identifierBrokerMock.Verify(broker =>
                broker.GetIdentifierAsync(),
                Times.Exactly(2));

            this.contentItemServiceMock.Verify(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventEnvelopeFactoryMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldComputeContentHashPerFrozenContractOnSubmitAsync()
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

            this.eventEnvelopeFactoryMock.Setup(factory =>
                factory.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync("hello world"))
                    .ReturnsAsync(expectedContentHash);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Enumerable.Empty<ContentItem>().AsQueryable());

            ContentItem? capturedContentItem = null;

            this.contentItemServiceMock.Setup(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
                    .Callback<ContentItem, CancellationToken>((contentItem, cancellationToken) =>
                        capturedContentItem = contentItem)
                    .ReturnsAsync(inputContentItem);

            // when
            await this.contentItemOrchestrationService.SubmitContentItemAsync(
                inputContentItem,
                TestContext.Current.CancellationToken);

            // then
            capturedContentItem!.ContentHash.Should().Be(expectedContentHash);
        }

        [Fact]
        public async Task ShouldReturnPoliteAcknowledgementWithoutCreatingOnSubmitIfDuplicateContentExistsAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;
            string normalizedContent = NormalizeContent(inputContentItem.Content);
            string contentHash = ComputeContentHash(inputContentItem.Content);
            ContentItem duplicateContentItem = CreateRandomContentItem();
            duplicateContentItem.ContentTypeId = inputContentItem.ContentTypeId;
            duplicateContentItem.ContentHash = contentHash;
            duplicateContentItem.IsDeleted = false;

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            var expectedContentItemSubmissionResult = new ContentItemSubmissionResult
            {
                IsCreated = false,
                ContentItem = null,
                Message = "Thank you for your submission."
            };

            this.eventEnvelopeFactoryMock.Setup(factory =>
                factory.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync(normalizedContent))
                    .ReturnsAsync(contentHash);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new[] { duplicateContentItem }.AsQueryable());

            // when
            ContentItemSubmissionResult actualContentItemSubmissionResult =
                await this.contentItemOrchestrationService.SubmitContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItemSubmissionResult.Should().BeEquivalentTo(expectedContentItemSubmissionResult);

            this.eventEnvelopeFactoryMock.Verify(factory =>
                factory.CreateAsync(inputContentItem),
                Times.Once);

            this.hashBrokerMock.Verify(broker =>
                broker.ComputeSha256HashAsync(normalizedContent),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventEnvelopeFactoryMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldCreateContentItemOnSubmitIfMatchingContentIsDeletedOrOtherContentTypeAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;
            string normalizedContent = NormalizeContent(inputContentItem.Content);
            string contentHash = ComputeContentHash(inputContentItem.Content);

            ContentItem deletedMatchingContentItem = CreateRandomContentItem();
            deletedMatchingContentItem.ContentTypeId = inputContentItem.ContentTypeId;
            deletedMatchingContentItem.ContentHash = contentHash;
            deletedMatchingContentItem.IsDeleted = true;

            ContentItem otherContentTypeContentItem = CreateRandomContentItem();
            otherContentTypeContentItem.ContentTypeId = Guid.NewGuid();
            otherContentTypeContentItem.ContentHash = contentHash;
            otherContentTypeContentItem.IsDeleted = false;

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            this.eventEnvelopeFactoryMock.Setup(factory =>
                factory.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync(normalizedContent))
                    .ReturnsAsync(contentHash);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new[] { deletedMatchingContentItem, otherContentTypeContentItem }.AsQueryable());

            this.contentItemServiceMock.Setup(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(inputContentItem);

            // when
            ContentItemSubmissionResult actualContentItemSubmissionResult =
                await this.contentItemOrchestrationService.SubmitContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItemSubmissionResult.IsCreated.Should().BeTrue();

            this.contentItemServiceMock.Verify(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
