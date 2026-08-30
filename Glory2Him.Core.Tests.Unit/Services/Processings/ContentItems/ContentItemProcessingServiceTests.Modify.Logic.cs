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
using Glory2Him.Core.Models.Events.Processings;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.ContentItems
{
    public partial class ContentItemProcessingServiceTests
    {
        [Theory]
        [InlineData(ApprovalStatus.Draft)]
        [InlineData(ApprovalStatus.Submitted)]
        [InlineData(ApprovalStatus.Dismissed)]
        public async Task ShouldModifyContentItemInPlaceOnModifyIfActorIsOwnerAsync(
            ApprovalStatus approvalStatus)
        {
            // given: the owner edits a non-terminal item — same row, same version
            // (design §3.4 rules 4-5); only the permitted fields are mapped onto the
            // entity loaded from storage (§12.4.1 BR6-7) and CreatedBy never changes.
            // Approved and Rejected are absent: both are terminal and fork instead,
            // which ShouldForkNewVersionOnModifyIfTerminalItemIsModifiedByOwnerAsync covers
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
            expectedMappedContentItem.ShareabilityBasis = inputContentItem.ShareabilityBasis;
            expectedMappedContentItem.SharePermission = inputContentItem.SharePermission;
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

            SetupGroupTip(storageContentItem, isTheGroupTip: true);

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
                    storageContentItem.GroupId,
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
                operation: ContentItemProcessingEventOperation.Modified);

            // when
            ContentItem actualContentItem =
                await this.contentItemProcessingService.ModifyContentItemAsync(
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

            VerifyGroupTipResolved();

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
                    storageContentItem.GroupId,
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
                operation: ContentItemProcessingEventOperation.Modified);

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
            expectedMappedContentItem.ShareabilityBasis = inputContentItem.ShareabilityBasis;
            expectedMappedContentItem.SharePermission = inputContentItem.SharePermission;
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

            SetupGroupTip(storageContentItem, isTheGroupTip: true);

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
                    storageContentItem.GroupId,
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
                operation: ContentItemProcessingEventOperation.Modified);

            // when
            await this.contentItemProcessingService.ModifyContentItemAsync(
                inputContentItem,
                TestContext.Current.CancellationToken);

            // then
            capturedContentItem.Should().BeEquivalentTo(expectedMappedContentItem);
            capturedContentItem!.PublishDate.Should().Be(storedPublishDate);
        }

        [Fact]
        public async Task ShouldCarryTheCallerContentTypeToTheFoundationOnModifyInPlaceAsync()
        {
            // given: the modify carries the caller's ContentType through to the foundation so
            // two invariants hold together. First, ContentType is create-only (§12.4.1 rule
            // 7a): passing the caller's value lets the foundation's storage-pin SEE a
            // reclassification attempt and reject it — if the row silently kept the stored type
            // instead, the foundation would compare stored-against-stored, see no change, and a
            // mismatched-ContentType modify would slip through. Second, the duplicate-content
            // probe (§3.4.2) is keyed on the CALLER's ContentType, so the type that is persisted
            // must be the same type that was dedup-checked; otherwise the probe checks one type
            // while the row lands as another, and a contributor can seed a global duplicate by
            // sending a colliding hash under a ContentType the probe will not match. The caller
            // here sends a DIFFERENT ContentType from storage precisely so a regression that
            // stopped carrying it would be caught.
            ContentItem inputContentItem = CreateRandomContentItem();
            inputContentItem.ContentType = ContentType.Story;
            string normalizedContent = NormalizeContent(inputContentItem.Content);
            string expectedContentHash = ComputeContentHash(inputContentItem.Content);
            string actorUserId = GetRandomString();

            ContentItem storageContentItem = CreateRandomStorageContentItem(
                contentItemId: inputContentItem.Id,
                approvalStatus: ApprovalStatus.Draft,
                createdBy: actorUserId);

            // the stored row is a DIFFERENT content type from what the caller sends. The
            // service copies onto the retrieved row in place, so its ContentType is snapshotted
            // here before the act — reading it back afterwards would see the mapped value.
            storageContentItem.ContentType = ContentType.Testimony;
            ContentType storedContentTypeBeforeAct = storageContentItem.ContentType;

            ContentItem updatedContentItem = storageContentItem.DeepClone();

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

            SetupGroupTip(storageContentItem, isTheGroupTip: true);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(securityContext))
                    .ReturnsAsync(actorUserId);

            this.hashBrokerMock.Setup(broker =>
                broker.ComputeSha256HashAsync(normalizedContent))
                    .ReturnsAsync(expectedContentHash);

            // the duplicate probe is keyed on the caller's ContentType (this is what the
            // production code passes); the persisted type must match it
            this.contentItemServiceMock.Setup(service =>
                service.CheckContentItemContentExistsAsync(
                    inputContentItem.ContentType,
                    expectedContentHash,
                    storageContentItem.GroupId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            ContentItem? capturedContentItem = null;

            this.contentItemServiceMock.Setup(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
                    .Callback<ContentItem, CancellationToken>((contentItem, cancellationToken) =>
                        capturedContentItem = contentItem.DeepClone())
                    .ReturnsAsync(updatedContentItem);

            SetupCompletionFactPublish(
                inboundEnvelope: inboundEnvelope,
                resultContentItem: updatedContentItem,
                operation: ContentItemProcessingEventOperation.Modified);

            // when
            await this.contentItemProcessingService.ModifyContentItemAsync(
                inputContentItem,
                TestContext.Current.CancellationToken);

            // then: the entity handed to the foundation carries the CALLER's content type — the
            // same type the duplicate probe was keyed on — so the foundation can pin the
            // reclassification and the dedup check cannot desync from what is persisted. A
            // regression that stopped carrying it would hand the foundation the STORED type
            // instead, and this assertion would fail.
            capturedContentItem.Should().NotBeNull();
            capturedContentItem!.ContentType.Should().Be(inputContentItem.ContentType);
            capturedContentItem.ContentType.Should().NotBe(storedContentTypeBeforeAct);
        }

        [Theory]
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        public async Task ShouldForkNewVersionOnModifyIfTerminalItemIsModifiedByOwnerAsync(
            ApprovalStatus terminalApprovalStatus)
        {
            // given: a terminal item is immutable in place, even to its owner — the edit
            // forks a new row with Version + 1, and that higher Version IS what makes it the
            // group tip. The fork is a SINGLE insert now: nothing is written to the previous
            // row, so there is no second write to fail and leave the group tip-less (#265).
            // The new version starts unpublished in Draft (design §3.4 rules 7-12, rule 16).
            // Rejected forks for the same reason Approved does: the row records a decision,
            // and editing it in place would rewrite what was decided. A Rejected row was
            // never published, so the fork simply leaves the group with no public row until
            // the new version is approved.
            ContentItem randomContentItem = CreateRandomContentItem();
            ContentItem inputContentItem = randomContentItem;
            string normalizedContent = NormalizeContent(inputContentItem.Content);
            string expectedContentHash = ComputeContentHash(inputContentItem.Content);
            string actorUserId = GetRandomString();
            Guid newVersionContentItemId = Guid.NewGuid();

            ContentItem storageContentItem = CreateRandomStorageContentItem(
                contentItemId: inputContentItem.Id,
                approvalStatus: terminalApprovalStatus,
                createdBy: actorUserId);

            var expectedNewVersionContentItem = new ContentItem
            {
                Id = newVersionContentItemId,
                ContentType = inputContentItem.ContentType,
                Title = inputContentItem.Title,
                Author = inputContentItem.Author,
                Content = inputContentItem.Content,
                ShareabilityBasis = inputContentItem.ShareabilityBasis,
                SharePermission = inputContentItem.SharePermission,

                // the fork is still the modify operation, so the caller's publish date does
                // not ride in on it — a fresh draft has none until approve grants one
                PublishDate = null,
                ContentHash = expectedContentHash,
                GroupId = storageContentItem.GroupId,
                Version = storageContentItem.Version + 1,
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

            SetupGroupTip(storageContentItem, isTheGroupTip: true);

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
                    storageContentItem.GroupId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(newVersionContentItemId);

            ContentItem? capturedNewVersionContentItem = null;

            this.contentItemServiceMock.Setup(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
                    .Callback<ContentItem, CancellationToken>((contentItem, cancellationToken) =>
                        capturedNewVersionContentItem = contentItem)
                    .ReturnsAsync(addedContentItem);

            EventEnvelope<ContentItem> outboundEnvelope = SetupCompletionFactPublish(
                inboundEnvelope: inboundEnvelope,
                resultContentItem: addedContentItem,
                operation: ContentItemProcessingEventOperation.Modified);

            // when
            ContentItem actualContentItem =
                await this.contentItemProcessingService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);
            capturedNewVersionContentItem.Should().NotBeNull();
            capturedNewVersionContentItem.Should().BeEquivalentTo(expectedNewVersionContentItem);

            // The guarantee the demotion used to carry, stated against the DERIVED tip: the new
            // row outranks the stored one, so the group's tip — the highest Version among its
            // live rows — resolves to the new row and to nothing else. No write to the previous
            // row was needed to make that true.
            capturedNewVersionContentItem!.Version.Should()
                .BeGreaterThan(storageContentItem.Version);

            var groupAfterFork = new List<ContentItem>
            {
                storageContentItem,
                capturedNewVersionContentItem
            };

            groupAfterFork
                .Where(contentItem =>
                    contentItem.GroupId == storageContentItem.GroupId
                        && contentItem.IsDeleted == false)
                .OrderByDescending(contentItem => contentItem.Version)
                .First()
                .Should().BeSameAs(capturedNewVersionContentItem);

            this.contentItemServiceMock.Verify(service =>
                service.RetrieveContentItemByIdAsync(inputContentItem.Id, It.IsAny<CancellationToken>()),
                Times.Once);

            VerifyGroupTipResolved();

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
                    storageContentItem.GroupId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.identifierBrokerMock.Verify(broker =>
                broker.GetIdentifierAsync(),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Once);

            // the fork is one write. The stored row is left exactly as it was found, which is
            // what makes the tip-less state of #265 unreachable rather than merely unlikely.
            this.contentItemServiceMock.Verify(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateAsync(inputContentItem),
                Times.Once);

            // the fork announces the amend exactly once
            this.eventEnvelopeBrokerMock.Verify(broker =>
                broker.CreateNextAsync(inboundEnvelope, addedContentItem),
                Times.Once);

            VerifyCompletionFactPublished(
                outboundEnvelope: outboundEnvelope,
                operation: ContentItemProcessingEventOperation.Modified);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.hashBrokerMock.VerifyNoOtherCalls();
            // the new row is numbered from the group high-water mark, not the tip (#271)
            this.contentItemServiceMock.Verify(service =>
                service.FindHighestVersionInGroupAsync(
                    storageContentItem.GroupId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(ApprovalStatus.Draft, Roles.Reviewers)]
        [InlineData(ApprovalStatus.Submitted, Roles.ContentItemReviewers)]
        [InlineData(ApprovalStatus.Submitted, Roles.Publishers)]
        [InlineData(ApprovalStatus.Draft, Roles.ContentItemPublishers)]
        [InlineData(ApprovalStatus.Dismissed, Roles.Administrators)]
        public async Task ShouldModifyContentItemInPlaceOnModifyIfActorHasModifyRoleAsync(
            ApprovalStatus approvalStatus,
            string modifyingRole)
        {
            // given: while an item is not yet decided, a reviewer, Publishers or Administrators
            // (global or ContentItem-scoped) may modify it in place alongside the owner;
            // the item stays on the same row and their identity lands on UpdatedBy
            // downstream. A terminal item is deliberately absent — it belongs to its owner
            // alone, which ShouldThrowValidationExceptionOnModifyIfActorIsNotPermitted covers
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

            SetupGroupTip(storageContentItem, isTheGroupTip: true);

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
                    storageContentItem.GroupId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            this.contentItemServiceMock.Setup(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(updatedContentItem);

            // when
            ContentItem actualContentItem =
                await this.contentItemProcessingService.ModifyContentItemAsync(
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

            SetupGroupTip(storageContentItem, isTheGroupTip: true);

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
                    storageContentItem.GroupId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            this.contentItemServiceMock.Setup(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItem);

            // when
            ContentItem actualContentItem =
                await this.contentItemProcessingService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().NotBeNull();

            this.contentItemServiceMock.Verify(service =>
                service.ModifyContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory]
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        public async Task ShouldKeepTheStoredContentTypeOnModifyWhenForkingANewVersionAsync(
            ApprovalStatus terminalApprovalStatus)
        {
            // given: ContentType is create-only, and a version fork carries it forward unchanged
            // — it is preserved, never re-chosen (§12.4.1 rule 7a). The in-place edit is held to
            // that by the foundation's pin against the stored row, but a fork is an ADD: it has no
            // stored row of its own to be pinned against. That made the fork the one path that
            // could relabel an item — a Story landing as a Testimony with its content never
            // validated against the target type's rules, its %ContentItem%-%ContentType%-Reviewers
            // /-Publishers tier changed under it (§18.6 rule 5), and its duplicate bucket moved
            // (§3.4.2).
            //
            // The caller here sends a DIFFERENT ContentType from the stored tip, which is what the
            // shared fork test cannot express: the filler ignores ContentType, so both rows carry
            // the same default there and the caller's value is indistinguishable from storage's.
            ContentItem inputContentItem = CreateRandomContentItem();
            inputContentItem.ContentType = ContentType.Testimony;
            string normalizedContent = NormalizeContent(inputContentItem.Content);
            string expectedContentHash = ComputeContentHash(inputContentItem.Content);
            string actorUserId = GetRandomString();
            Guid newVersionContentItemId = Guid.NewGuid();

            ContentItem storageContentItem = CreateRandomStorageContentItem(
                contentItemId: inputContentItem.Id,
                approvalStatus: terminalApprovalStatus,
                createdBy: actorUserId);

            storageContentItem.ContentType = ContentType.Story;
            ContentType storedContentType = storageContentItem.ContentType;

            ContentItem addedContentItem = storageContentItem.DeepClone();
            addedContentItem.Id = newVersionContentItemId;
            addedContentItem.Version = storageContentItem.Version + 1;
            addedContentItem.ApprovalStatus = ApprovalStatus.Draft;
            addedContentItem.IsPublished = false;
            addedContentItem.PublishDate = null;

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

            // the duplicate rule is scoped per (ContentType, ContentHash) (§3.4.2), so on a fork
            // the probe is keyed on the STORED type — the type the row will actually land with.
            // Keyed on the caller's instead it would check one bucket while the row lands in
            // another, which is how a contributor seeds a duplicate the probe will never match.
            this.contentItemServiceMock.Setup(service =>
                service.CheckContentItemContentExistsAsync(
                    storedContentType,
                    expectedContentHash,
                    storageContentItem.GroupId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            this.identifierBrokerMock.Setup(broker =>
                broker.GetIdentifierAsync())
                    .ReturnsAsync(newVersionContentItemId);

            ContentItem? capturedNewVersionContentItem = null;

            this.contentItemServiceMock.Setup(service =>
                service.AddContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
                    .Callback<ContentItem, CancellationToken>((contentItem, cancellationToken) =>
                        capturedNewVersionContentItem = contentItem.DeepClone())
                    .ReturnsAsync(addedContentItem);

            SetupCompletionFactPublish(
                inboundEnvelope: inboundEnvelope,
                resultContentItem: addedContentItem,
                operation: ContentItemProcessingEventOperation.Modified);

            // when
            await this.contentItemProcessingService.ModifyContentItemAsync(
                inputContentItem,
                TestContext.Current.CancellationToken);

            // then: the forked row carries the STORED type, not the one the caller asked for. The
            // content fields are still the caller's — the fork is an edit, and it is only the
            // create-only control field that is refused them.
            capturedNewVersionContentItem.Should().NotBeNull();
            capturedNewVersionContentItem!.ContentType.Should().Be(storedContentType);
            capturedNewVersionContentItem.ContentType.Should().NotBe(inputContentItem.ContentType);
            capturedNewVersionContentItem.Title.Should().Be(inputContentItem.Title);
            capturedNewVersionContentItem.Content.Should().Be(inputContentItem.Content);

            // and the probe was keyed on the type that landed, never on the caller's
            this.contentItemServiceMock.Verify(service =>
                service.CheckContentItemContentExistsAsync(
                    storedContentType,
                    expectedContentHash,
                    storageContentItem.GroupId,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.contentItemServiceMock.Verify(service =>
                service.CheckContentItemContentExistsAsync(
                    inputContentItem.ContentType,
                    It.IsAny<string>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
