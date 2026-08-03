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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Tags
{
    public partial class TagServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            var invalidTagId = Guid.Empty;

            var invalidTagException = new InvalidTagException(
                message: "Tag is invalid, fix the errors and try again.");

            invalidTagException.UpsertDataList(
                key: "Id",
                value: "Id is required");

            var expectedTagValidationException = new TagValidationException(
                message: "Tag validation error occurred, fix the errors and try again.",
                innerException: invalidTagException);

            // when
            ValueTask<Glory2Him.Core.Models.Foundations.Tags.Tag> retrieveTagByIdTask =
                this.tagService.RetrieveTagByIdAsync(
                    invalidTagId,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    retrieveTagByIdTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfTagNotFoundAndLogItAsync()
        {
            // given
            Guid someTagId = Guid.NewGuid();
            Tag nullTag = null;

            var notFoundTagException =
                new NotFoundTagException(
                    message: $"Tag not found with id: {someTagId}.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: notFoundTagException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    someTagId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(nullTag);

            // when
            ValueTask<Tag> retrieveTagByIdTask =
                this.tagService.RetrieveTagByIdAsync(
                    someTagId,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    retrieveTagByIdTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(
                    someTagId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfTagIsSoftDeletedAndLogItAsync()
        {
            // given: even an Admin caller gets not-found for a soft-deleted row —
            // deleted beats privilege
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            Tag storageTag = CreateRandomTag();
            storageTag.IsDeleted = true;
            Guid tagId = storageTag.Id;

            var notFoundTagException =
                new NotFoundTagException(
                    message: $"Tag not found with id: {tagId}.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: notFoundTagException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    tagId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageTag);

            // when
            ValueTask<Tag> retrieveTagByIdTask =
                this.tagService.RetrieveTagByIdAsync(
                    tagId,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    retrieveTagByIdTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(
                    tagId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogInformationAsync(
                    $"Tag read denied. Tag {tagId} is " +
                        "soft-deleted; reported to the caller as not found."),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfNotVisibleAndUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext invalidSecurityContext)
        {
            // given
            this.ambientSecurityContext = invalidSecurityContext;
            Tag storageTag = CreateRandomTag();
            storageTag.IsDeleted = false;
            storageTag.ApprovalStatus = ApprovalStatus.Draft;
            storageTag.IsPublished = false;
            Guid tagId = storageTag.Id;

            var notFoundTagException =
                new NotFoundTagException(
                    message: $"Tag not found with id: {tagId}.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: notFoundTagException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    tagId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageTag);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            // when
            ValueTask<Tag> retrieveTagByIdTask =
                this.tagService.RetrieveTagByIdAsync(
                    tagId,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    retrieveTagByIdTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(
                    tagId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogWarningAsync(
                    $"Tag read denied. Tag {tagId} is not " +
                        "publicly visible and the caller is not authenticated; reported to " +
                        "the caller as not found."),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfNotVisibleAndUserIsNotOwnerAndLogItAsync()
        {
            // given
            string randomActorUserId = GetRandomString();
            Tag storageTag = CreateRandomTag();
            storageTag.IsDeleted = false;
            storageTag.ApprovalStatus = ApprovalStatus.Draft;
            storageTag.IsPublished = false;
            Guid tagId = storageTag.Id;

            var notFoundTagException =
                new NotFoundTagException(
                    message: $"Tag not found with id: {tagId}.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: notFoundTagException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    tagId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageTag);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomActorUserId);

            // when
            ValueTask<Tag> retrieveTagByIdTask =
                this.tagService.RetrieveTagByIdAsync(
                    tagId,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    retrieveTagByIdTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(
                    tagId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogWarningAsync(
                    $"Tag read denied. Tag {tagId} " +
                        $"is not publicly visible and user \"{randomActorUserId}\" is neither the " +
                        "owner nor in a review role; reported to the caller as not found."),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
