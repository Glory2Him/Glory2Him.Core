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
using Force.DeepCloner;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
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
                Times.Once);

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
                Times.Once);

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
                Times.Once);

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
    }
}