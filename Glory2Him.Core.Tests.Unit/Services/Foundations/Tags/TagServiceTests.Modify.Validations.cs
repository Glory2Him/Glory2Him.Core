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
using Force.DeepCloner;
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
        public async Task ShouldThrowValidationExceptionOnModifyIfTagIsNullAndLogItAsync()
        {
            // given
            Tag nullTag = null;

            var nullTagException =
                new NullTagException(message: "Tag is null.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: nullTagException);

            // when
            ValueTask<Tag> modifyTagTask =
                this.tagService.ModifyTagAsync(
                    nullTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    modifyTagTask.AsTask);

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

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ShouldThrowValidationExceptionOnModifyIfTagIsInvalidAndLogItAsync(
            string invalidText)
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            DateTimeOffset startDate = randomDateTimeOffset.AddSeconds(-90);
            DateTimeOffset endDate = randomDateTimeOffset;

            var invalidTag = new Tag
            {
                Id = Guid.Empty,
                Name = invalidText,
                CreatedBy = invalidText,
                UpdatedBy = invalidText,
                CreatedWhen = default,
                UpdatedWhen = default
            };

            var invalidTagException =
                new InvalidTagException(
                    message: "Tag is invalid, fix the errors and try again.");

            invalidTagException.AddData(
                key: nameof(Tag.Id),
                values: "Id is required");

            invalidTagException.AddData(
                key: nameof(Tag.Name),
                values: "Text is required");

            invalidTagException.AddData(
                key: nameof(Tag.CreatedBy),
                values: "Text is required");

            invalidTagException.AddData(
                key: nameof(Tag.UpdatedBy),
                values: "Text is required");

            invalidTagException.AddData(
                key: nameof(Tag.CreatedWhen),
                values: "Date is required");

            invalidTagException.AddData(
                key: nameof(Tag.UpdatedWhen),
                values: new[]
                {
                    "Date is required",
                    "Date is the same as CreatedWhen",

                    "Date is not recent. Expected a value between " +
                        $"{startDate} and {endDate} but found {default(DateTimeOffset)}"
                });

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: invalidTagException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidText);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Tag> modifyTagTask =
                this.tagService.ModifyTagAsync(
                    invalidTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    modifyTagTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfTagNotFoundAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Tag randomTag = CreateRandomModifyTag(randomDateTimeOffset, randomUserId);
            Tag nonExistentTag = randomTag;
            Tag noTag = null;

            var notFoundTagException = new NotFoundTagException(
                message: $"Tag not found with id: {nonExistentTag.Id}.");

            var expectedTagValidationException = new TagValidationException(
                message: "Tag validation error occurred, fix the errors and try again.",
                innerException: notFoundTagException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentTag, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(nonExistentTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    nonExistentTag.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noTag);

            // when
            ValueTask<Tag> modifyTagTask =
                this.tagService.ModifyTagAsync(
                    nonExistentTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    modifyTagTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentTag, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(
                    nonExistentTag.Id,
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
        public async Task ShouldThrowValidationExceptionOnModifyIfStorageCreatedWhenNotSameAsInputAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Tag randomTag = CreateRandomModifyTag(randomDateTimeOffset, randomUserId);
            Tag invalidTag = randomTag;
            Tag storageTag = randomTag.DeepClone();
            storageTag.CreatedWhen = GetRandomDateTimeOffset();
            storageTag.UpdatedWhen = GetRandomDateTimeOffset();

            var invalidTagException = new InvalidTagException(
                message: "Tag is invalid, fix the errors and try again.");

            invalidTagException.AddData(
                key: nameof(Tag.CreatedWhen),
                values: $"Date is not the same as {nameof(Tag.CreatedWhen)}");

            var expectedTagValidationException = new TagValidationException(
                message: "Tag validation error occurred, fix the errors and try again.",
                innerException: invalidTagException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    invalidTag.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidTag,
                    storageTag))
                        .ReturnsAsync(invalidTag);

            // when
            ValueTask<Tag> modifyTagTask =
                this.tagService.ModifyTagAsync(
                    invalidTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    modifyTagTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(
                    invalidTag.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidTag,
                    storageTag),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfStorageCreatedByNotSameAsInputAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Tag randomTag = CreateRandomModifyTag(randomDateTimeOffset, randomUserId);
            Tag invalidTag = randomTag;
            Tag storageTag = randomTag.DeepClone();
            storageTag.CreatedBy = GetRandomString();
            storageTag.UpdatedWhen = storageTag.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var invalidTagException =
                new InvalidTagException(
                    message: "Tag is invalid, fix the errors and try again.");

            invalidTagException.AddData(
                key: nameof(Tag.CreatedBy),
                values: $"Text is not the same as {nameof(Tag.CreatedBy)}");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: invalidTagException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    invalidTag.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidTag,
                    storageTag))
                        .ReturnsAsync(invalidTag);

            // when
            ValueTask<Tag> modifyTagTask =
                this.tagService.ModifyTagAsync(
                    invalidTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    modifyTagTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(
                    invalidTag.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidTag,
                    storageTag),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfStorageUpdatedWhenSameAsInputAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Tag randomTag = CreateRandomModifyTag(randomDateTimeOffset, randomUserId);
            Tag invalidTag = randomTag;
            Tag storageTag = randomTag.DeepClone();

            var invalidTagException =
                new InvalidTagException(
                    message: "Tag is invalid, fix the errors and try again.");

            invalidTagException.AddData(
                key: nameof(Tag.UpdatedWhen),
                values: $"Date is the same as {nameof(Tag.UpdatedWhen)}");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: invalidTagException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    invalidTag.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidTag,
                    storageTag))
                        .ReturnsAsync(invalidTag);

            // when
            ValueTask<Tag> modifyTagTask =
                this.tagService.ModifyTagAsync(
                    invalidTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    modifyTagTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(
                    invalidTag.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidTag,
                    storageTag),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfUpdatedByIsNotSameAsCurrentUserIdAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomString();
            string differentUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Tag randomTag = CreateRandomModifyTag(randomDateTimeOffset, randomUserId);
            Tag invalidTag = randomTag;
            invalidTag.UpdatedBy = differentUserId;

            var invalidTagException =
                new InvalidTagException(
                    message: "Tag is invalid, fix the errors and try again.");

            invalidTagException.AddData(
                key: nameof(Tag.UpdatedBy),
                values: $"Expected value to be '{randomUserId}' but found '{differentUserId}'.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: invalidTagException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Tag> modifyTagTask =
                this.tagService.ModifyTagAsync(
                    invalidTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    modifyTagTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfUpdatedWhenIsSameAsCreatedWhenAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Tag randomTag = CreateRandomModifyTag(randomDateTimeOffset, randomUserId);
            Tag invalidTag = randomTag;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidTag.UpdatedWhen = invalidTag.CreatedWhen;

            var invalidTagException =
                new InvalidTagException(
                    message: "Tag is invalid, fix the errors and try again.");

            invalidTagException.AddData(
                key: nameof(Tag.UpdatedWhen),
                values: new[]
                {
                    $"Date is the same as {nameof(Tag.CreatedWhen)}",
                    $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                        $"but found {invalidTag.UpdatedWhen}"
                });

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: invalidTagException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Tag> modifyTagTask =
                this.tagService.ModifyTagAsync(
                    invalidTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    modifyTagTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
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
        [MemberData(nameof(MinutesBeforeOrAfter))]
        public async Task ShouldThrowValidationExceptionOnModifyIfUpdatedWhenIsNotRecentAndLogItAsync(int minutes)
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Tag randomTag = CreateRandomModifyTag(randomDateTimeOffset, randomUserId);
            Tag invalidTag = randomTag;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidTag.UpdatedWhen = randomDateTimeOffset.AddMinutes(minutes);

            var invalidTagException =
                new InvalidTagException(
                    message: "Tag is invalid, fix the errors and try again.");

            invalidTagException.AddData(
                key: nameof(Tag.UpdatedWhen),
                values: $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                    $"but found {invalidTag.UpdatedWhen}");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: invalidTagException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<Tag> modifyTagTask =
                this.tagService.ModifyTagAsync(
                    invalidTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    modifyTagTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfTagExceedsMaxLengthAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomStringWithLengthOf(256);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Tag invalidTag = CreateRandomModifyTag(randomDateTimeOffset, randomUserId);

            var invalidTagException =
                new InvalidTagException(
                    message: "Tag is invalid, fix the errors and try again.");

            invalidTagException.AddData(
                key: nameof(Tag.CreatedBy),
                values: $"Text exceed max length of {invalidTag.CreatedBy.Length - 1} characters");

            invalidTagException.AddData(
                key: nameof(Tag.UpdatedBy),
                values: $"Text exceed max length of {invalidTag.UpdatedBy.Length - 1} characters");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: invalidTagException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Tag> modifyTagTask =
                this.tagService.ModifyTagAsync(
                    invalidTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    modifyTagTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext invalidSecurityContext)
        {
            // given
            this.ambientSecurityContext = invalidSecurityContext;
            Tag someTag = CreateRandomTag();

            var unauthorizedTagException = new UnauthorizedTagException(
                message: "The current user is not authenticated.");

            var expectedTagValidationException = new TagValidationException(
                message: "Tag validation error occurred, fix the errors and try again.",
                innerException: unauthorizedTagException);

            // when
            ValueTask<Tag> modifyTagTask =
                this.tagService.ModifyTagAsync(
                    someTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    modifyTagTask.AsTask);

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

        [Theory]
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.TagReadOnly)]
        public async Task ShouldThrowValidationExceptionOnModifyIfUserIsBlockedFromContributingAndLogItAsync(
            string blockedRole)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(blockedRole);
            Tag someTag = CreateRandomTag();

            var unauthorizedTagException = new UnauthorizedTagException(
                message: "The current user is blocked from contributing tags.");

            var expectedTagValidationException = new TagValidationException(
                message: "Tag validation error occurred, fix the errors and try again.",
                innerException: unauthorizedTagException);

            // when
            ValueTask<Tag> modifyTagTask =
                this.tagService.ModifyTagAsync(
                    someTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    modifyTagTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnModifyIfUserIsNotOwnerAndHasNoReviewRoleAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Tag randomTag = CreateRandomModifyTag(randomDateTimeOffset, randomUserId);
            Tag inputTag = randomTag;
            Tag storageTag = randomTag.DeepClone();
            storageTag.CreatedBy = GetRandomString();
            storageTag.UpdatedWhen = storageTag.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var unauthorizedTagException = new UnauthorizedTagException(
                message: "The current user is not allowed to modify this tag.");

            var expectedTagValidationException = new TagValidationException(
                message: "Tag validation error occurred, fix the errors and try again.",
                innerException: unauthorizedTagException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputTag, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    inputTag.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageTag);

            // when
            ValueTask<Tag> modifyTagTask =
                this.tagService.ModifyTagAsync(
                    inputTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    modifyTagTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(inputTag, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(
                    inputTag.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateTagAsync(
                    It.IsAny<Tag>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

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
        public async Task ShouldThrowValidationExceptionOnModifyIfApprovalStatusChangedByNonPublisherAndLogItAsync()
        {
            // given
            // a Reviewer holds write permission but is neither the owner nor in the Publisher
            // tier, so mayTransitionApprovalStatus is false. The move is Draft -> Submitted — one
            // the owner or a Publisher WOULD be allowed — so the refusal comes from the carve-out
            // gate, not from the status being a verdict.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            string ownerUserId = GetRandomString();
            Tag invalidTag = CreateRandomModifyTag(randomDateTimeOffset, randomUserId);
            invalidTag.CreatedBy = ownerUserId;
            invalidTag.ApprovalStatus = ApprovalStatus.Draft;
            Tag storageTag = invalidTag.DeepClone();
            storageTag.UpdatedWhen = storageTag.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            invalidTag.ApprovalStatus = ApprovalStatus.Submitted;

            var invalidTagException =
                new InvalidTagException(
                    message: "Tag is invalid, fix the errors and try again.");

            invalidTagException.AddData(
                key: nameof(Tag.ApprovalStatus),
                values: "Value is not the same as storage approval status");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: invalidTagException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    invalidTag.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidTag,
                    storageTag))
                        .ReturnsAsync(invalidTag);

            // when
            ValueTask<Tag> modifyTagTask =
                this.tagService.ModifyTagAsync(
                    invalidTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    modifyTagTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(
                    invalidTag.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidTag,
                    storageTag),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfIsPublishedChangedAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Tag randomTag = CreateRandomModifyTag(randomDateTimeOffset, randomUserId);
            Tag invalidTag = randomTag;
            Tag storageTag = randomTag.DeepClone();
            storageTag.UpdatedWhen = storageTag.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            invalidTag.IsPublished = true;

            var invalidTagException =
                new InvalidTagException(
                    message: "Tag is invalid, fix the errors and try again.");

            invalidTagException.AddData(
                key: nameof(Tag.IsPublished),
                values: "Value is not the same as IsPublished");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: invalidTagException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    invalidTag.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidTag,
                    storageTag))
                        .ReturnsAsync(invalidTag);

            // when
            ValueTask<Tag> modifyTagTask =
                this.tagService.ModifyTagAsync(
                    invalidTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    modifyTagTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(
                    invalidTag.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidTag,
                    storageTag),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfPublishDateChangedAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Tag randomTag = CreateRandomModifyTag(randomDateTimeOffset, randomUserId);
            Tag invalidTag = randomTag;
            Tag storageTag = randomTag.DeepClone();
            storageTag.UpdatedWhen = storageTag.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            invalidTag.PublishDate = randomDateTimeOffset;

            var invalidTagException =
                new InvalidTagException(
                    message: "Tag is invalid, fix the errors and try again.");

            invalidTagException.AddData(
                key: nameof(Tag.PublishDate),
                values: "Date is not the same as PublishDate");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: invalidTagException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    invalidTag.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidTag,
                    storageTag))
                        .ReturnsAsync(invalidTag);

            // when
            ValueTask<Tag> modifyTagTask =
                this.tagService.ModifyTagAsync(
                    invalidTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    modifyTagTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(
                    invalidTag.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidTag,
                    storageTag),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfIsApprovedByBypassChangedAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Tag randomTag = CreateRandomModifyTag(randomDateTimeOffset, randomUserId);
            Tag invalidTag = randomTag;
            Tag storageTag = randomTag.DeepClone();
            storageTag.UpdatedWhen = storageTag.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            invalidTag.IsApprovedByBypass = !storageTag.IsApprovedByBypass;

            var invalidTagException =
                new InvalidTagException(
                    message: "Tag is invalid, fix the errors and try again.");

            invalidTagException.AddData(
                key: nameof(Tag.IsApprovedByBypass),
                values: "Value is not the same as IsApprovedByBypass");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: invalidTagException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    invalidTag.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidTag,
                    storageTag))
                        .ReturnsAsync(invalidTag);

            // when
            ValueTask<Tag> modifyTagTask =
                this.tagService.ModifyTagAsync(
                    invalidTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    modifyTagTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(
                    invalidTag.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidTag,
                    storageTag),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfApprovedByBypassReasonChangedAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Tag randomTag = CreateRandomModifyTag(randomDateTimeOffset, randomUserId);
            Tag invalidTag = randomTag;
            Tag storageTag = randomTag.DeepClone();
            storageTag.UpdatedWhen = storageTag.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            storageTag.ApprovedByBypassReason = GetRandomString();
            invalidTag.ApprovedByBypassReason = GetRandomString();

            var invalidTagException =
                new InvalidTagException(
                    message: "Tag is invalid, fix the errors and try again.");

            invalidTagException.AddData(
                key: nameof(Tag.ApprovedByBypassReason),
                values: "Text is not the same as ApprovedByBypassReason");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: invalidTagException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    invalidTag.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidTag,
                    storageTag))
                        .ReturnsAsync(invalidTag);

            // when
            ValueTask<Tag> modifyTagTask =
                this.tagService.ModifyTagAsync(
                    invalidTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    modifyTagTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(
                    invalidTag.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidTag,
                    storageTag),
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
        [InlineData(ApprovalStatus.Approved)]
        [InlineData(ApprovalStatus.Rejected)]
        public async Task ShouldThrowValidationExceptionOnModifyIfStorageIsTerminalAndLogItAsync(
            ApprovalStatus terminalStatus)
        {
            // given: THE case the status pin never covered. The caller amends an approved row and
            // echoes the STORED status back unchanged, so IsNotAPermittedStatusChangeOnModify —
            // whose condition is guarded by inputStatus != storageStatus — passes, and the content
            // is written through with IsPublished and PublishDate still at their approved values.
            // The edit then goes public with no re-review.
            //
            // The owner is used here because it is the least privileged party who can reach this
            // path at all; the roles that could also reach it are covered below.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Tag randomTag = CreateRandomModifyTag(randomDateTimeOffset, randomUserId);
            Tag invalidTag = randomTag;
            Tag storageTag = randomTag.DeepClone();
            storageTag.UpdatedWhen = storageTag.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            // both sides terminal and IDENTICAL, so nothing else in the modify can refuse it
            invalidTag.ApprovalStatus = terminalStatus;
            storageTag.ApprovalStatus = terminalStatus;

            var invalidTagException =
                new InvalidTagException(
                    message: "Tag cannot be modified from status " +
                        $"{terminalStatus}.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: invalidTagException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    invalidTag.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageTag);

            // when
            ValueTask<Tag> modifyTagTask =
                this.tagService.ModifyTagAsync(
                    invalidTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    modifyTagTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

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

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagValidationException))),
                Times.Once);
        }

        [Theory]
        [InlineData(Roles.Publisher)]
        [InlineData(Roles.Admin)]
        public async Task ShouldThrowValidationExceptionOnModifyIfStorageIsTerminalForPrivilegedRolesAndLogItAsync(
            string role)
        {
            // given: terminal means terminal for EVERY role (§3.4 rules 7 and 16). An Admin in
            // particular used to have an in-place carve-out here; it is withdrawn, because a state
            // one role can edit out of is not terminal. The override verb is the only route, and
            // it changes status without touching content.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(role);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Tag randomTag = CreateRandomModifyTag(randomDateTimeOffset, randomUserId);
            Tag invalidTag = randomTag;
            Tag storageTag = randomTag.DeepClone();
            storageTag.UpdatedWhen = storageTag.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            storageTag.CreatedBy = GetRandomString();

            invalidTag.ApprovalStatus = ApprovalStatus.Approved;
            storageTag.ApprovalStatus = ApprovalStatus.Approved;
            invalidTag.CreatedBy = storageTag.CreatedBy;

            var invalidTagException =
                new InvalidTagException(
                    message: "Tag cannot be modified from status " +
                        $"{ApprovalStatus.Approved}.");

            var expectedTagValidationException =
                new TagValidationException(
                    message: "Tag validation error occurred, fix the errors and try again.",
                    innerException: invalidTagException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidTag, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    invalidTag.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageTag);

            // when
            ValueTask<Tag> modifyTagTask =
                this.tagService.ModifyTagAsync(
                    invalidTag,
                    TestContext.Current.CancellationToken);

            TagValidationException actualTagValidationException =
                await Assert.ThrowsAsync<TagValidationException>(
                    modifyTagTask.AsTask);

            // then
            actualTagValidationException.Should().BeEquivalentTo(
                expectedTagValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateTagAsync(
                    It.IsAny<Tag>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Theory]
        [InlineData(ApprovalStatus.Draft)]
        [InlineData(ApprovalStatus.Submitted)]
        public async Task ShouldModifyIfStorageIsNotTerminalAsync(
            ApprovalStatus nonTerminalStatus)
        {
            // given: the other half of the rule, and the one a refusal written too broadly would
            // break — a Draft or Submitted row still modifies exactly as it did before.
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Tag randomTag = CreateRandomModifyTag(randomDateTimeOffset, randomUserId);
            Tag inputTag = randomTag;
            Tag storageTag = randomTag.DeepClone();
            storageTag.UpdatedWhen = storageTag.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            inputTag.ApprovalStatus = nonTerminalStatus;
            storageTag.ApprovalStatus = nonTerminalStatus;

            Tag updatedTag = inputTag.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputTag, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    inputTag.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageTag);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    inputTag,
                    storageTag))
                        .ReturnsAsync(inputTag);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateTagAsync(
                    inputTag,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(updatedTag);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishTagAsync(
                    It.IsAny<EventEnvelope<Tag>>(),
                    TagEventOperation.Modified))
                        .Returns(new ValueTask<EventPublishResult<Tag>>(
                            new EventPublishResult<Tag>()));

            // when
            Tag actualTag = await this.tagService.ModifyTagAsync(
                inputTag,
                TestContext.Current.CancellationToken);

            // then
            actualTag.Should().BeEquivalentTo(updatedTag);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateTagAsync(
                    inputTag,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
