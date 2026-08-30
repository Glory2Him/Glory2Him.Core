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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Events.Foundations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    public partial class ContentItemServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnSubmitIfIdIsInvalidAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            var invalidContentItemException =
                new InvalidContentItemException(
                    message: "Content item is invalid, fix the errors and try again.");

            invalidContentItemException.UpsertDataList(
                key: nameof(ContentItem.Id),
                value: "Id is required");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemException);

            // when
            ValueTask<ContentItem> submitTask =
                this.contentItemService.SubmitContentItemByIdAsync(
                    Guid.Empty,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedContentItemValidationException);

            // an invalid id never reaches storage
            this.storageBrokerMock.Verify(broker =>
                    broker.SelectContentItemByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowUnauthorizedOnSubmitIfCallerIsNotAuthenticatedAsync(
            SecurityContext unauthenticatedContext)
        {
            // given
            this.ambientSecurityContext = unauthenticatedContext;

            // when
            ValueTask<ContentItem> submitTask =
                this.contentItemService.SubmitContentItemByIdAsync(
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<ContentItemValidationException>(submitTask.AsTask);

            // then: the contribution gate refuses before any row is read
            this.storageBrokerMock.Verify(broker =>
                    broker.SelectContentItemByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.ContentItemReadOnly)]
        public async Task ShouldThrowUnauthorizedOnSubmitIfCallerIsBlockedFromContributingAsync(
            string blockingRole)
        {
            // given: a read-only caller is blocked from every write, submit included, before the
            // row is even read
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(blockingRole);

            var unauthorizedContentItemException =
                new UnauthorizedContentItemException(
                    message: "The current user is blocked from contributing content items.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedContentItemException);

            // when
            ValueTask<ContentItem> submitTask =
                this.contentItemService.SubmitContentItemByIdAsync(
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedContentItemValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectContentItemByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnSubmitIfTheRowIsMissingAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Guid contentItemId = Guid.NewGuid();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(
                    contentItemId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((ContentItem)null);

            var notFoundContentItemException =
                new NotFoundContentItemException(
                    message: $"Content item not found with id: {contentItemId}.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentItemException);

            // when
            ValueTask<ContentItem> submitTask =
                this.contentItemService.SubmitContentItemByIdAsync(
                    contentItemId,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedContentItemValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateContentItemAsync(
                        It.IsAny<ContentItem>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnSubmitIfTheRowIsSoftDeletedAsync()
        {
            // given: a soft-removed row is reported as not-found, matching the read posture
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            ContentItem storageContentItem = CreateSubmittableStorageContentItem();
            storageContentItem.IsDeleted = true;

            SetupContentItemStorageRead(storageContentItem);

            var notFoundContentItemException =
                new NotFoundContentItemException(
                    message: $"Content item not found with id: {storageContentItem.Id}.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentItemException);

            // when
            ValueTask<ContentItem> submitTask =
                this.contentItemService.SubmitContentItemByIdAsync(
                    storageContentItem.Id,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedContentItemValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateContentItemAsync(
                        It.IsAny<ContentItem>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(NonPublisherRoleSets))]
        public async Task ShouldThrowUnauthorizedOnSubmitIfCallerIsNeitherOwnerNorPublisherAsync(
            string[] roles)
        {
            // given: a caller who neither owns the row nor holds the publisher tier may not
            // submit it. A reviewer is included among the role sets: they hold write permission
            // on content, but moving a submission status is never theirs (§8.6 HR-3).
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(roles);

            ContentItem storageContentItem = CreateSubmittableStorageContentItem();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync($"not-the-owner-{Guid.NewGuid()}");

            SetupContentItemStorageRead(storageContentItem);

            var unauthorizedContentItemException =
                new UnauthorizedContentItemException(
                    message: "The current user is not allowed to submit this content item.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedContentItemException);

            // when
            ValueTask<ContentItem> submitTask =
                this.contentItemService.SubmitContentItemByIdAsync(
                    storageContentItem.Id,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedContentItemValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateContentItemAsync(
                        It.IsAny<ContentItem>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(ApprovalStatus.Submitted)]
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        [InlineData(ApprovalStatus.Dismissed)]
        public async Task ShouldThrowValidationExceptionOnSubmitIfTheStoredRowIsNotDraftAsync(
            ApprovalStatus storageStatus)
        {
            // given: only a Draft may be submitted (issue #111 case 7). A row already Submitted
            // or Approved is not a fresh submission — re-submitting one would either re-open a
            // decided item or re-announce a pending one. The caller is the owner, so this proves
            // the state gate stands on its own, after authorization passes.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            ContentItem storageContentItem = CreateSubmittableStorageContentItem();
            storageContentItem.ApprovalStatus = storageStatus;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageContentItem.CreatedBy);

            SetupContentItemStorageRead(storageContentItem);

            var invalidContentItemException =
                new InvalidContentItemException(
                    message: "Content item cannot be submitted from status " +
                        $"{storageStatus}.");

            var expectedContentItemValidationException =
                new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemException);

            // when
            ValueTask<ContentItem> submitTask =
                this.contentItemService.SubmitContentItemByIdAsync(
                    storageContentItem.Id,
                    TestContext.Current.CancellationToken);

            ContentItemValidationException actualException =
                await Assert.ThrowsAsync<ContentItemValidationException>(submitTask.AsTask);

            // then: nothing written, nothing announced
            actualException.Should().BeEquivalentTo(expectedContentItemValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateContentItemAsync(
                        It.IsAny<ContentItem>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishContentItemAsync(
                        It.IsAny<EventEnvelope<ContentItem>>(),
                        It.IsAny<ContentItemEventOperation>()),
                Times.Never);
        }
    }
}
