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
using System.Collections.Generic;
using System.Linq;
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
        public async Task ShouldModifyContentItemInPlaceOnModifyIfActorIsOwnerAsync(
            ApprovalStatus approvalStatus)
        {
            // given: the owner edits a not-yet-approved item — same row, same version
            // (design §3.4 rules 4-5); only the permitted fields are mapped onto the
            // entity loaded from storage (§12.4.1 BR6-7) and CreatedBy never changes
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;
            string normalizedContent = NormalizeContent(inputContentItem.Content);
            string expectedContentHash = ComputeContentHash(inputContentItem.Content);
            string actorUserId = GetRandomString();

            ContentItem storageContentItem = CreateRandomStorageContentItem(
                contentItemId: inputContentItem.Id,
                approvalStatus: approvalStatus,
                createdBy: actorUserId);

            ContentItem expectedMappedContentItem = storageContentItem.DeepClone();
            expectedMappedContentItem.ContentType = inputContentItem.ContentType;
            expectedMappedContentItem.Title = inputContentItem.Title;
            expectedMappedContentItem.Author = inputContentItem.Author;
            expectedMappedContentItem.Content = inputContentItem.Content;
            expectedMappedContentItem.PublishDate = inputContentItem.PublishDate;
            expectedMappedContentItem.ContentHash = expectedContentHash;
            ContentItem updatedContentItem = expectedMappedContentItem.DeepClone();
            ContentItem expectedContentItem = updatedContentItem.DeepClone();

            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: securityContext);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(inputContentItem.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(actorUserId);

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync(normalizedContent))
                    .ReturnsAsync(expectedContentHash);

            this.contentItemServiceMock.Setup(service =>
                service.CheckContentItemContentExistsAsync(
                    inputContentItem.ContentType,
                    expectedContentHash,
                    storageContentItem.ContentItemGroupId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            ContentItem? capturedContentItem = null;

            this.contentItemServiceMock.Setup(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
                    .Callback<ContentItem, CancellationToken>((contentItem, cancellationToken) =>
                        capturedContentItem = contentItem)
                    .ReturnsAsync(updatedContentItem);

            EventEnvelope<ContentItem> outboundEnvelope = SetupCompletionFactPublish(
                inboundEnvelope: inboundEnvelope,
                resultContentItem: updatedContentItem,
                operation: ContentItemOrchestrationEventOperation.Modified);

            // when
            ContentItem actualContentItem =
                await this.contentItemOrchestrationService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);
            capturedContentItem.Should().BeEquivalentTo(expectedMappedContentItem);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(inputContentItem),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(inputContentItem.Id, It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(securityContext),
                Times.Once);

            this.hashBrokerMock.Verify(broker =>
                broker.ComputeSha256HashAsync(normalizedContent),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.CheckContentItemContentExistsAsync(
                    inputContentItem.ContentType,
                    expectedContentHash,
                    storageContentItem.ContentItemGroupId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(inboundEnvelope, updatedContentItem),
                Times.Once);

            VerifyCompletionFactPublished(
                outboundEnvelope: outboundEnvelope,
                operation: ContentItemOrchestrationEventOperation.Modified);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldNotCarryPublishDateOnModifyInPlaceAsync()
        {
            // given: PublishDate is an IApproval member, so under §9.7.1 rule 2's
            // subtraction rule it is not content and the general modify must never carry
            // it — it belongs solely to the approve operation, which owns ApprovalStatus,
            // IsPublished and PublishDate as one unit. A caller who could set it through
            // modify would schedule their own publication without ever meeting that gate.
            ContentItem inputContentItem = CreateRandomContentItem();
            string normalizedContent = NormalizeContent(inputContentItem.Content);
            string expectedContentHash = ComputeContentHash(inputContentItem.Content);
            string actorUserId = GetRandomString();

            ContentItem storageContentItem = CreateRandomStorageContentItem(
                contentItemId: inputContentItem.Id,
                approvalStatus: ApprovalStatus.Draft,
                createdBy: actorUserId);

            DateTimeOffset storedPublishDate = GetRandomDateTimeOffset();
            storageContentItem.PublishDate = storedPublishDate;
            inputContentItem.PublishDate = storedPublishDate.AddDays(GetRandomNumber());

            // the mapped row keeps storage's publish date, not the caller's
            ContentItem expectedMappedContentItem = storageContentItem.DeepClone();
            expectedMappedContentItem.ContentType = inputContentItem.ContentType;
            expectedMappedContentItem.Title = inputContentItem.Title;
            expectedMappedContentItem.Author = inputContentItem.Author;
            expectedMappedContentItem.Content = inputContentItem.Content;
            expectedMappedContentItem.ContentHash = expectedContentHash;
            ContentItem updatedContentItem = expectedMappedContentItem.DeepClone();

            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: securityContext);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(inputContentItem.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(actorUserId);

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync(normalizedContent))
                    .ReturnsAsync(expectedContentHash);

            this.contentItemServiceMock.Setup(service =>
                service.CheckContentItemContentExistsAsync(
                    inputContentItem.ContentType,
                    expectedContentHash,
                    storageContentItem.ContentItemGroupId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            ContentItem? capturedContentItem = null;

            this.contentItemServiceMock.Setup(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
                    .Callback<ContentItem, CancellationToken>((contentItem, cancellationToken) =>
                        capturedContentItem = contentItem)
                    .ReturnsAsync(updatedContentItem);

            SetupCompletionFactPublish(
                inboundEnvelope: inboundEnvelope,
                resultContentItem: updatedContentItem,
                operation: ContentItemOrchestrationEventOperation.Modified);

            // when
            await this.contentItemOrchestrationService.ModifyContentItemAsync(
                inputContentItem,
                TestContext.Current.CancellationToken);

            // then
            capturedContentItem.Should().BeEquivalentTo(expectedMappedContentItem);
            capturedContentItem!.PublishDate.Should().Be(storedPublishDate);
        }

        [Fact]
        public async Task ShouldForkNewVersionOnModifyIfApprovedItemIsModifiedByOwnerAsync()
        {
            // given: an approved item is immutable to its owner — the edit forks a new row
            // with Version + 1 that becomes the latest, the previous latest is demoted
            // BEFORE the insert (one IsLatestVersion = true per group), and the new
            // version starts unpublished in Draft (design §3.4 rules 7-12)
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;
            string normalizedContent = NormalizeContent(inputContentItem.Content);
            string expectedContentHash = ComputeContentHash(inputContentItem.Content);
            string actorUserId = GetRandomString();
            Guid newVersionContentItemId = Guid.NewGuid();

            ContentItem storageContentItem = CreateRandomStorageContentItem(
                contentItemId: inputContentItem.Id,
                approvalStatus: ApprovalStatus.Approved,
                createdBy: actorUserId);

            ContentItem expectedDemotedContentItem = storageContentItem.DeepClone();
            expectedDemotedContentItem.IsLatestVersion = false;

            var expectedNewVersionContentItem = new ContentItem
            {
                Id = newVersionContentItemId,
                ContentType = inputContentItem.ContentType,
                Title = inputContentItem.Title,
                Author = inputContentItem.Author,
                Content = inputContentItem.Content,
                PublishDate = inputContentItem.PublishDate,
                ContentHash = expectedContentHash,
                ContentItemGroupId = storageContentItem.ContentItemGroupId,
                Version = storageContentItem.Version + 1,
                IsLatestVersion = true,
                IsPublished = false,
                ApprovalStatus = ApprovalStatus.Draft,
                IsDeleted = false
            };

            ContentItem addedContentItem = expectedNewVersionContentItem.DeepClone();
            ContentItem expectedContentItem = addedContentItem.DeepClone();
            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: securityContext);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(inputContentItem.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(actorUserId);

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync(normalizedContent))
                    .ReturnsAsync(expectedContentHash);

            this.contentItemServiceMock.Setup(service =>
                service.CheckContentItemContentExistsAsync(
                    inputContentItem.ContentType,
                    expectedContentHash,
                    storageContentItem.ContentItemGroupId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(newVersionContentItemId);

            var callOrder = new List<string>();
            ContentItem? capturedDemotedContentItem = null;
            ContentItem? capturedNewVersionContentItem = null;

            this.contentItemServiceMock.Setup(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
                    .Callback<ContentItem, CancellationToken>((contentItem, cancellationToken) =>
                    {
                        callOrder.Add("demote");
                        capturedDemotedContentItem = contentItem;
                    })
                    .ReturnsAsync(expectedDemotedContentItem);

            this.contentItemServiceMock.Setup(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
                    .Callback<ContentItem, CancellationToken>((contentItem, cancellationToken) =>
                    {
                        callOrder.Add("add");
                        capturedNewVersionContentItem = contentItem;
                    })
                    .ReturnsAsync(addedContentItem);

            EventEnvelope<ContentItem> outboundEnvelope = SetupCompletionFactPublish(
                inboundEnvelope: inboundEnvelope,
                resultContentItem: addedContentItem,
                operation: ContentItemOrchestrationEventOperation.Modified);

            // when
            ContentItem actualContentItem =
                await this.contentItemOrchestrationService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);
            capturedDemotedContentItem.Should().BeEquivalentTo(expectedDemotedContentItem);
            capturedNewVersionContentItem.Should().BeEquivalentTo(expectedNewVersionContentItem);
            callOrder.Should().Equal("demote", "add");

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(inputContentItem.Id, It.IsAny<CancellationToken>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(securityContext),
                Times.Once);

            this.hashBrokerMock.Verify(broker =>
                broker.ComputeSha256HashAsync(normalizedContent),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.CheckContentItemContentExistsAsync(
                    inputContentItem.ContentType,
                    expectedContentHash,
                    storageContentItem.ContentItemGroupId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.identifierBrokerMock.Verify(broker =>
                broker.GetIdentifierAsync(),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Once);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(inputContentItem),
                Times.Once);

            // the fork writes two foundation rows but announces the amend exactly once
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(inboundEnvelope, addedContentItem),
                Times.Once);

            VerifyCompletionFactPublished(
                outboundEnvelope: outboundEnvelope,
                operation: ContentItemOrchestrationEventOperation.Modified);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(ApprovalStatus.Draft, Roles.Reviewer)]
        [InlineData(ApprovalStatus.Submitted, Roles.ContentItemReviewer)]
        [InlineData(ApprovalStatus.Submitted, Roles.Publisher)]
        [InlineData(ApprovalStatus.Rejected, Roles.ContentItemPublisher)]
        [InlineData(ApprovalStatus.Dismissed, Roles.Admin)]
        public async Task ShouldModifyContentItemInPlaceOnModifyIfActorHasModifyRoleAsync(
            ApprovalStatus approvalStatus,
            string modifyingRole)
        {
            // given: while an item is not yet approved, a Reviewer, Publisher or Admin
            // (global or ContentItem-scoped) may modify it in place alongside the owner;
            // the item stays on the same row and their identity lands on UpdatedBy
            // downstream
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;
            string normalizedContent = NormalizeContent(inputContentItem.Content);
            string expectedContentHash = ComputeContentHash(inputContentItem.Content);

            ContentItem storageContentItem = CreateRandomStorageContentItem(
                contentItemId: inputContentItem.Id,
                approvalStatus: approvalStatus,
                createdBy: GetRandomString());

            ContentItem updatedContentItem = storageContentItem.DeepClone();
            SecurityContext securityContext = CreateAuthenticatedSecurityContext(modifyingRole);

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: securityContext);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(inputContentItem.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(GetRandomString());

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync(normalizedContent))
                    .ReturnsAsync(expectedContentHash);

            this.contentItemServiceMock.Setup(service =>
                service.CheckContentItemContentExistsAsync(
                    inputContentItem.ContentType,
                    expectedContentHash,
                    storageContentItem.ContentItemGroupId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            this.contentItemServiceMock.Setup(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(updatedContentItem);

            // when
            ContentItem actualContentItem =
                await this.contentItemOrchestrationService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().NotBeNull();

            this.contentItemServiceMock.Verify(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldModifyContentItemIfMatchingContentIsInOwnGroupAsync()
        {
            // given: the duplicate rule excludes the item's own group on modify (§3.4.2
            // rule 4) — a later version legitimately reverting to earlier wording of the
            // same group must not trip the duplicate error
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;
            string normalizedContent = NormalizeContent(inputContentItem.Content);
            string contentHash = ComputeContentHash(inputContentItem.Content);
            string actorUserId = GetRandomString();

            ContentItem storageContentItem = CreateRandomStorageContentItem(
                contentItemId: inputContentItem.Id,
                approvalStatus: ApprovalStatus.Draft,
                createdBy: actorUserId);

            SecurityContext securityContext = CreateAuthenticatedSecurityContext();

            EventEnvelope<ContentItem> inboundEnvelope = CreateEventEnvelope(
                contentItem: inputContentItem,
                securityContext: securityContext);

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(inputContentItem))
                    .ReturnsAsync(inboundEnvelope);

            this.contentItemServiceMock.Setup(service =>
                service.RetrieveContentItemByIdAsync(inputContentItem.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(actorUserId);

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync(normalizedContent))
                    .ReturnsAsync(contentHash);

            // the probe is asked to exclude the item's own group, so a match confined to
            // that group reports no duplicate
            this.contentItemServiceMock.Setup(service =>
                service.CheckContentItemContentExistsAsync(
                    inputContentItem.ContentType,
                    contentHash,
                    storageContentItem.ContentItemGroupId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            this.contentItemServiceMock.Setup(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            // when
            ContentItem actualContentItem =
                await this.contentItemOrchestrationService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().NotBeNull();

            this.contentItemServiceMock.Verify(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
