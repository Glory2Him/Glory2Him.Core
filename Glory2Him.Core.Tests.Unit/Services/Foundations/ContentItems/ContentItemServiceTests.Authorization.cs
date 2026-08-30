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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    public partial class ContentItemServiceTests
    {
        // ── The content-type tier (design §18.6 rule 4) ──────────────────────────────
        //
        // ContentItem is the only entity with three role tiers, because it is the only one
        // carrying a ContentType. The tiers widen from narrow to broad —
        // ContentItem-Story-Reviewers ⊂ ContentItem-Reviewers ⊂ Reviewer — and rule 4 binds
        // BOTH directions: holding any of them satisfies a check for that content type, and
        // the narrow role never satisfies a check for a different one. Every test below
        // pairs a "may" with a "may not" so neither half can rot.

        public static TheoryData<string> TestimonyScopedReviewRoles() =>
            new TheoryData<string>
            {
                Roles.ReviewersFor(EntityType.ContentItem, ContentType.Testimony),
                Roles.PublishersFor(EntityType.ContentItem, ContentType.Testimony)
            };

        [Theory]
        [MemberData(nameof(TestimonyScopedReviewRoles))]
        public async Task ShouldRetrieveNonPublicContentItemByIdWhenTheRoleMatchesTheContentTypeAsync(
            string contentTypeScopedRole)
        {
            // given: the caller is not the owner and holds only a narrow role for this
            // item's content type — rule 4's first half, which the flat check used to fail
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(contentTypeScopedRole);

            string randomActorUserId = GetRandomString();
            ContentItem storageContentItem = CreateRandomContentItem();
            storageContentItem.ContentType = ContentType.Testimony;
            storageContentItem.IsDeleted = false;
            storageContentItem.ApprovalStatus = ApprovalStatus.Draft;
            storageContentItem.IsPublished = false;
            ContentItem expectedContentItem = storageContentItem;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    storageContentItem.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItem);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            ContentItem actualContentItem =
                await this.contentItemService.RetrieveContentItemByIdAsync(
                    storageContentItem.Id,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);
        }

        [Fact]
        public async Task ShouldDenyRetrieveByIdWhenTheRoleIsForADifferentContentTypeAsync()
        {
            // given: rule 4's second half — a Testimony reviewer has no authority over a
            // Story, and the denial is reported as not-found so a probe learns nothing
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.ReviewersFor(EntityType.ContentItem, ContentType.Testimony));

            string randomActorUserId = GetRandomString();
            ContentItem storageContentItem = CreateRandomContentItem();
            storageContentItem.ContentType = ContentType.Story;
            storageContentItem.IsDeleted = false;
            storageContentItem.ApprovalStatus = ApprovalStatus.Draft;
            storageContentItem.IsPublished = false;
            Guid contentItemId = storageContentItem.Id;

            var notFoundContentItemException = new NotFoundContentItemException(
                message: $"Content item not found with id: {contentItemId}.");

            var expectedContentItemValidationException = new ContentItemValidationException(
                message: "Content item validation error occurred, fix the errors and try again.",
                innerException: notFoundContentItemException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    contentItemId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItem);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            ValueTask<ContentItem> retrieveContentItemByIdTask =
                this.contentItemService.RetrieveContentItemByIdAsync(
                    contentItemId,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    retrieveContentItemByIdTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogWarningAsync(
                    $"Content item read denied. Content item {contentItemId} " +
                        $"is not publicly visible and user \"{randomActorUserId}\" is neither the " +
                        "owner nor in a review role; reported to the caller as not found."),
                Times.Once);
        }

        [Fact]
        public async Task ShouldRetrieveOnlyTheContentTypesTheNarrowRoleCoversOnRetrieveAllAsync()
        {
            // given: the collection twin of the two tests above. A narrow role must widen
            // the caller's view to its own content type and to nothing else — the danger
            // being a blanket "reviewers see everything" branch handing over every draft.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.ReviewersFor(EntityType.ContentItem, ContentType.Testimony));

            string randomActorUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ContentItem publicStoryContentItem = CreateRandomContentItem();
            publicStoryContentItem.ContentType = ContentType.Story;
            publicStoryContentItem.IsDeleted = false;
            publicStoryContentItem.ApprovalStatus = ApprovalStatus.Approved;
            publicStoryContentItem.IsPublished = true;
            publicStoryContentItem.PublishDate = null;

            ContentItem testimonyDraftContentItem = CreateRandomContentItem();
            testimonyDraftContentItem.ContentType = ContentType.Testimony;
            testimonyDraftContentItem.IsDeleted = false;
            testimonyDraftContentItem.ApprovalStatus = ApprovalStatus.Draft;
            testimonyDraftContentItem.IsPublished = false;

            ContentItem storyDraftContentItem = CreateRandomContentItem();
            storyDraftContentItem.ContentType = ContentType.Story;
            storyDraftContentItem.IsDeleted = false;
            storyDraftContentItem.ApprovalStatus = ApprovalStatus.Draft;
            storyDraftContentItem.IsPublished = false;

            ContentItem deletedTestimonyContentItem = CreateRandomContentItem();
            deletedTestimonyContentItem.ContentType = ContentType.Testimony;
            deletedTestimonyContentItem.IsDeleted = true;

            IQueryable<ContentItem> storageContentItems = new List<ContentItem>
            {
                publicStoryContentItem,
                testimonyDraftContentItem,
                storyDraftContentItem,
                deletedTestimonyContentItem
            }.AsQueryable();

            // the public story (visible to anyone) and the testimony draft (their tier) —
            // but NOT the story draft, and not the deleted row: removal beats privilege
            IQueryable<ContentItem> expectedContentItems = new List<ContentItem>
            {
                publicStoryContentItem,
                testimonyDraftContentItem
            }.AsQueryable();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storageContentItems);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            IQueryable<ContentItem> actualContentItems =
                await this.contentItemService.RetrieveAllContentItemsAsync(
                    TestContext.Current.CancellationToken);

            // then
            actualContentItems.Should().BeEquivalentTo(expectedContentItems);
        }

        [Fact]
        public async Task ShouldAllowModifyWhenTheRoleMatchesTheContentTypeAsync()
        {
            // given: the write gate has to honour the narrow tier too, or a content-type
            // reviewer could read an item for review and then not be able to amend it
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.ReviewersFor(EntityType.ContentItem, ContentType.Testimony));

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string actorUserId = GetRandomString();
            string ownerUserId = GetRandomString();

            ContentItem inputContentItem =
                CreateRandomModifyContentItem(randomDateTimeOffset, actorUserId);

            inputContentItem.ContentType = ContentType.Testimony;
            inputContentItem.CreatedBy = ownerUserId;

            ContentItem storageContentItem = inputContentItem.DeepClone();
            storageContentItem.UpdatedWhen = storageContentItem.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            ContentItem updatedContentItem = inputContentItem.DeepClone();
            ContentItem expectedContentItem = updatedContentItem.DeepClone();

            SetupPassingModifyPathBrokers(
                inputContentItem, storageContentItem, updatedContentItem, actorUserId, randomDateTimeOffset);

            // when
            ContentItem actualContentItem =
                await this.contentItemService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateContentItemAsync(inputContentItem, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldDenyModifyWhenTheRoleIsForADifferentContentTypeAsync()
        {
            // given: rule 4's second half on the write gate
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.ReviewersFor(EntityType.ContentItem, ContentType.Testimony));

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string actorUserId = GetRandomString();
            string ownerUserId = GetRandomString();

            ContentItem inputContentItem =
                CreateRandomModifyContentItem(randomDateTimeOffset, actorUserId);

            inputContentItem.ContentType = ContentType.Story;
            inputContentItem.CreatedBy = ownerUserId;

            ContentItem storageContentItem = inputContentItem.DeepClone();
            storageContentItem.UpdatedWhen = storageContentItem.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var unauthorizedContentItemException = new UnauthorizedContentItemException(
                message: "The current user is not allowed to modify this content item.");

            var expectedContentItemValidationException = new ContentItemValidationException(
                message: "Content item validation error occurred, fix the errors and try again.",
                innerException: unauthorizedContentItemException);

            SetupFailingModifyPathBrokers(
                inputContentItem, storageContentItem, actorUserId, randomDateTimeOffset);

            // when
            ValueTask<ContentItem> modifyContentItemTask =
                this.contentItemService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualContentItemValidationException =
                await Assert.ThrowsAsync<ContentItemValidationException>(
                    modifyContentItemTask.AsTask);

            // then
            actualContentItemValidationException.Should().BeEquivalentTo(
                expectedContentItemValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateContentItemAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldWriteTheSubmissionStatusOnModifyWhenAContentTypePublisherMovesItAsync()
        {
            // given: the §9.2 carve-out is gated on the Publisher TIER, and the narrow
            // content-type publisher is part of that tier for its own content type
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.PublishersFor(EntityType.ContentItem, ContentType.Testimony));

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string actorUserId = GetRandomString();
            string ownerUserId = GetRandomString();

            ContentItem inputContentItem =
                CreateRandomModifyContentItem(randomDateTimeOffset, actorUserId);

            inputContentItem.ContentType = ContentType.Testimony;
            inputContentItem.CreatedBy = ownerUserId;
            inputContentItem.ApprovalStatus = ApprovalStatus.Draft;

            ContentItem storageContentItem = inputContentItem.DeepClone();
            storageContentItem.UpdatedWhen = storageContentItem.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            inputContentItem.ApprovalStatus = ApprovalStatus.Submitted;

            ContentItem updatedContentItem = inputContentItem.DeepClone();
            ContentItem expectedContentItem = updatedContentItem.DeepClone();

            SetupPassingModifyPathBrokers(
                inputContentItem, storageContentItem, updatedContentItem, actorUserId, randomDateTimeOffset);

            // when
            ContentItem actualContentItem =
                await this.contentItemService.ModifyContentItemAsync(
                    inputContentItem,
                    TestContext.Current.CancellationToken);

            // then
            actualContentItem.Should().BeEquivalentTo(expectedContentItem);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateContentItemAsync(inputContentItem, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldDenyTheSubmissionStatusMoveWhenThePublisherRoleIsForADifferentContentTypeAsync()
        {
            // given: a Testimony publisher who also holds the coarse reviewer role passes
            // the write gate on a Story, and must still not move its status
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(
                Roles.ContentItemReviewers,
                Roles.PublishersFor(EntityType.ContentItem, ContentType.Testimony));

            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string actorUserId = GetRandomString();
            string ownerUserId = GetRandomString();

            ContentItem invalidContentItem =
                CreateRandomModifyContentItem(randomDateTimeOffset, actorUserId);

            invalidContentItem.ContentType = ContentType.Story;
            invalidContentItem.CreatedBy = ownerUserId;
            invalidContentItem.ApprovalStatus = ApprovalStatus.Draft;

            ContentItem storageContentItem = invalidContentItem.DeepClone();
            storageContentItem.UpdatedWhen = storageContentItem.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            invalidContentItem.ApprovalStatus = ApprovalStatus.Submitted;

            var invalidContentItemException = new InvalidContentItemException(
                message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.AddData(
                key: nameof(ContentItem.ApprovalStatus),
                values: "Value is not the same as storage approval status");

            SetupFailingModifyPathBrokers(
                invalidContentItem, storageContentItem, actorUserId, randomDateTimeOffset);

            // when . then
            await AssertModifyIsRefusedAsync(invalidContentItem, invalidContentItemException);
        }
    }
}
