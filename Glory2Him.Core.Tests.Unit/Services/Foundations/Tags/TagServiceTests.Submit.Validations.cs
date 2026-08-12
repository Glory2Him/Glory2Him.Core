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
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Tags
{
    public partial class TagServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnSubmitIfIdIsInvalidAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            var invalidTagException =
                new InvalidTagException(
                    message: "Tag is invalid, fix the errors and try again.");

            invalidTagException.UpsertDataList(
                key: nameof(Tag.Id),
                value: "Id is required");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: invalidTagException);

            // when
            ValueTask<Tag> submitTask =
                this.tagService.SubmitTagByIdAsync(
                    Guid.Empty,
                    TestContext.Current.CancellationToken);

            TagValidationException actualException =
                await Assert.ThrowsAsync<TagValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedTagValidationException);

            // an invalid id never reaches storage
            this.storageBrokerMock.Verify(broker =>
                    broker.SelectTagByIdAsync(
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
            ValueTask<Tag> submitTask =
                this.tagService.SubmitTagByIdAsync(
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<TagValidationException>(submitTask.AsTask);

            // then: the contribution gate refuses before any row is read
            this.storageBrokerMock.Verify(broker =>
                    broker.SelectTagByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.TagReadOnly)]
        public async Task ShouldThrowUnauthorizedOnSubmitIfCallerIsBlockedFromContributingAsync(
            string blockingRole)
        {
            // given: a read-only caller is blocked from every write, submit included, before the
            // row is even read
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(blockingRole);

            var unauthorizedTagException =
                new UnauthorizedTagException(
                    message: "The current user is blocked from contributing tags.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedTagException);

            // when
            ValueTask<Tag> submitTask =
                this.tagService.SubmitTagByIdAsync(
                    Guid.NewGuid(),
                    TestContext.Current.CancellationToken);

            TagValidationException actualException =
                await Assert.ThrowsAsync<TagValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedTagValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.SelectTagByIdAsync(
                        It.IsAny<Guid>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnSubmitIfTheRowIsMissingAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            Guid tagId = Guid.NewGuid();

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    tagId,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Tag)null);

            var notFoundTagException =
                new NotFoundTagException(
                    message: $"Tag not found with id: {tagId}.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: notFoundTagException);

            // when
            ValueTask<Tag> submitTask =
                this.tagService.SubmitTagByIdAsync(
                    tagId,
                    TestContext.Current.CancellationToken);

            TagValidationException actualException =
                await Assert.ThrowsAsync<TagValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedTagValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateTagAsync(
                        It.IsAny<Tag>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnSubmitIfTheRowIsSoftDeletedAsync()
        {
            // given: a soft-removed row is reported as not-found, matching the read posture
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            Tag storageTag = CreateSubmittableStorageTag();
            storageTag.IsDeleted = true;

            SetupTagStorageRead(storageTag);

            var notFoundTagException =
                new NotFoundTagException(
                    message: $"Tag not found with id: {storageTag.Id}.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: notFoundTagException);

            // when
            ValueTask<Tag> submitTask =
                this.tagService.SubmitTagByIdAsync(
                    storageTag.Id,
                    TestContext.Current.CancellationToken);

            TagValidationException actualException =
                await Assert.ThrowsAsync<TagValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedTagValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateTagAsync(
                        It.IsAny<Tag>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [MemberData(nameof(NonPublisherRoleSets))]
        public async Task ShouldThrowUnauthorizedOnSubmitIfCallerIsNeitherOwnerNorPublisherAsync(
            string[] roles)
        {
            // given: a caller who neither owns the row nor holds the publisher tier may not
            // submit it. A Reviewer is included among the role sets: they hold write permission
            // on content, but moving a submission status is never theirs (§8.6 HR-3).
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(roles);

            Tag storageTag = CreateSubmittableStorageTag();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync($"not-the-owner-{Guid.NewGuid()}");

            SetupTagStorageRead(storageTag);

            var unauthorizedTagException =
                new UnauthorizedTagException(
                    message: "The current user is not allowed to submit this tag.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedTagException);

            // when
            ValueTask<Tag> submitTask =
                this.tagService.SubmitTagByIdAsync(
                    storageTag.Id,
                    TestContext.Current.CancellationToken);

            TagValidationException actualException =
                await Assert.ThrowsAsync<TagValidationException>(submitTask.AsTask);

            // then
            actualException.Should().BeEquivalentTo(expectedTagValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateTagAsync(
                        It.IsAny<Tag>(),
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

            Tag storageTag = CreateSubmittableStorageTag();
            storageTag.ApprovalStatus = storageStatus;

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(storageTag.CreatedBy);

            SetupTagStorageRead(storageTag);

            var invalidTagException =
                new InvalidTagException(
                    message: "Tag cannot be submitted from status " +
                        $"{storageStatus}.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: invalidTagException);

            // when
            ValueTask<Tag> submitTask =
                this.tagService.SubmitTagByIdAsync(
                    storageTag.Id,
                    TestContext.Current.CancellationToken);

            TagValidationException actualException =
                await Assert.ThrowsAsync<TagValidationException>(submitTask.AsTask);

            // then: nothing written, nothing announced
            actualException.Should().BeEquivalentTo(expectedTagValidationException);

            this.storageBrokerMock.Verify(broker =>
                    broker.UpdateTagAsync(
                        It.IsAny<Tag>(),
                        It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                    broker.PublishTagAsync(
                        It.IsAny<EventEnvelope<Tag>>(),
                        It.IsAny<TagEventOperation>()),
                Times.Never);
        }
    }
}
