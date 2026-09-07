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
                ShareabilityBasis = inputContentItem.ShareabilityBasis,
                SharePermission = inputContentItem.SharePermission,

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

        // THE STATUS THE CONTRIBUTOR FILED UNDER IS WHAT LANDS (§9.7.1 rule 1): Submitted on the
        // common path, Draft when saving work in progress. Pinning it to Draft here — as this
        // path once did — filed every contribution as work in progress whatever the form said,
        // leaving the contributor no route into review but a second, separate submit.
        [Theory]
        [InlineData(ApprovalStatus.Draft)]
        [InlineData(ApprovalStatus.Submitted)]
        public async Task ShouldAddContentItemAtTheContributedStatusAsync(
            ApprovalStatus contributedApprovalStatus)
        {
            // given
            ContentItem inputContentItem = CreateRandomContentItem();
            inputContentItem.ApprovalStatus = contributedApprovalStatus;
            string normalizedContent = NormalizeContent(inputContentItem.Content);
            string expectedContentHash = ComputeContentHash(inputContentItem.Content);

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

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
            capturedContentItem!.ApprovalStatus.Should().Be(contributedApprovalStatus);

            // and the row still lands unpublished whichever status it carries — publication is
            // the approve operation's to grant, and no add surface may ask for it
            capturedContentItem.IsPublished.Should().BeFalse();
            capturedContentItem.PublishDate.Should().BeNull();
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
                ShareabilityBasis = inputContentItem.ShareabilityBasis,
                SharePermission = inputContentItem.SharePermission,
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

        // §3.4.2 rule 6, the add arm. The contributor is thanked, nothing is written, and
        // nothing in the answer says which of the two happened — the whole of the test is that
        // the acknowledgement is the SAME OBJECT the genuine add would have produced, minted
        // identifiers and audit stamps included. It used to be an already-exists error carrying
        // the sentence "A content item already exists with the same content.", which the SPA
        // put on the screen: anyone who could POST could ask whether a given piece of content
        // had been submitted, including content they may not read (#392, #412).
        [Fact]
        public async Task ShouldAcknowledgeDuplicateContentOnAddWithoutCreatingItAsync()
        {
            // given
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;
            string normalizedContent = NormalizeContent(inputContentItem.Content);
            string expectedContentHash = ComputeContentHash(inputContentItem.Content);
            Guid contentItemId = Guid.NewGuid();
            Guid groupId = Guid.NewGuid();
            string randomActor = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: CreateAuthenticatedSecurityContext());

            // the row the genuine arm would have handed to the foundation, field for field
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
                ContentHash = expectedContentHash,
                GroupId = groupId,
                Version = 1,
                IsPublished = false,

                // The CALLER'S, not a literal: the status is theirs to choose on add (§9.7.1
                // rule 1), so an acknowledgement that answered Draft while the genuine row would
                // have been Submitted is a field the two arms differ on — and a field they
                // differ on is the whole of the leak this arm exists to close.
                ApprovalStatus = inputContentItem.ApprovalStatus,
                IsDeleted = false,

                // stamped here because the foundation, which stamps them on the genuine arm,
                // is never reached — an answer missing them is an answer a caller can tell apart
                CreatedBy = randomActor,
                UpdatedBy = randomActor,
                CreatedWhen = randomDateTimeOffset,
                UpdatedWhen = randomDateTimeOffset
            };

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            // The envelope the quiet arm mints for its audit stamps, exactly as the foundation
            // mints one on the genuine arm. Its context is a DIFFERENT instance from the inbound
            // envelope's on purpose: the audit mock below matches only this one, so a service
            // that went back to stamping from the inbound envelope finds no setup and fails here.
            // On the event path those two really are different identities.
            EventEnvelope<ContentItem> auditEnvelope = CreateEventEnvelope(
                contentItem: new ContentItem(),
                securityContext: CreateAuthenticatedSecurityContext());

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is<ContentItem>(item => item.Id == contentItemId)))
                    .ReturnsAsync(auditEnvelope);

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync(normalizedContent))
                    .ReturnsAsync(expectedContentHash);

            this.contentItemServiceMock.Setup(service =>
                service.CheckContentItemContentExistsAsync(
                    inputContentItem.ContentType,
                    expectedContentHash,
                    null,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            this.identifierBrokerMock.SetupSequence(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(contentItemId)
                    .ReturnsAsync(groupId);

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

            // when
            ContentItem actualContentItem =
                await this.contentItemProcessingService.AddContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedAcknowledgedContentItem);

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

            // the same two identifiers a real add mints, so the answer cannot be told apart by
            // an empty Id or an empty GroupId either
            this.identifierBrokerMock.Verify(broker =>
                broker.GetIdentifierAsync(),
                Times.Exactly(2));

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(
                    It.IsAny<ContentItem>(),
                    auditEnvelope.SecurityContext),
                Times.Once);

            // NO ROW, which is the rule
            this.contentItemServiceMock.Verify(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Never);

            // AND NO FACT: the fact says a row was created and none was. The acknowledgement is
            // for the caller alone; subscribers are told about rows. Two envelopes are minted and
            // neither is published — the inbound one, and the one the stamps come off.
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(inputContentItem),
                Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(It.Is<ContentItem>(item => item.Id == contentItemId)),
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
