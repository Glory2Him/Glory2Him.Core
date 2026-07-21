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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Reactions.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Reactions
{
    public partial class ReactionServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfReactionIsNullAndLogItAsync()
        {
            // given
            Reaction nullReaction = null;

            var nullReactionException =
                new NullReactionException(message: "Reaction is null.");

            var expectedReactionValidationException =
                new ReactionValidationException(
                    message: "Reaction validation error occurred, fix the errors and try again.",
                    innerException: nullReactionException);

            // when
            ValueTask<Reaction> addReactionTask =
                this.reactionService.AddReactionAsync(
                    nullReaction,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualReactionValidationException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    addReactionTask.AsTask);

            // then
            actualReactionValidationException.Should().BeEquivalentTo(
                expectedReactionValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedReactionValidationException))),
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
        public async Task ShouldThrowValidationExceptionOnAddIfReactionIsInvalidAndLogItAsync(
            string invalidText)
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            DateTimeOffset startDate = randomDateTimeOffset.AddSeconds(-90);
            DateTimeOffset endDate = randomDateTimeOffset;

            var invalidReaction = new Reaction
            {
                Id = Guid.Empty,
                Name = invalidText,
                UnicodeEmoji = invalidText,
                CreatedBy = invalidText,
                UpdatedBy = invalidText,
                CreatedWhen = default,
                UpdatedWhen = default
            };

            var invalidReactionException =
                new InvalidReactionException(
                    message: "Reaction is invalid, fix the errors and try again.");

            invalidReactionException.AddData(
                key: nameof(Reaction.Id),
                values: "Id is required");

            invalidReactionException.AddData(
                key: nameof(Reaction.Name),
                values: "Text is required");

            invalidReactionException.AddData(
                key: nameof(Reaction.UnicodeEmoji),
                values: "Text is required");

            invalidReactionException.AddData(
                key: nameof(Reaction.CreatedBy),
                values: "Text is required");

            invalidReactionException.AddData(
                key: nameof(Reaction.UpdatedBy),
                values: "Text is required");

            invalidReactionException.AddData(
                key: nameof(Reaction.CreatedWhen),
                values: new[]
                {
                    "Date is required",

                    "Date is not recent. Expected a value between " +
                        $"{startDate} and {endDate} but found {default(DateTimeOffset)}"
                });

            invalidReactionException.AddData(
                key: nameof(Reaction.UpdatedWhen),
                values: "Date is required");

            var expectedReactionValidationException =
                new ReactionValidationException(
                    message: "Reaction validation error occurred, fix the errors and try again.",
                    innerException: invalidReactionException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidText);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Reaction> addReactionTask =
                this.reactionService.AddReactionAsync(
                    invalidReaction,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualReactionValidationException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    addReactionTask.AsTask);

            // then
            actualReactionValidationException.Should().BeEquivalentTo(
                expectedReactionValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedReactionValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfReactionNameExceedsMaxLengthAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomStringWithLengthOf(256);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Reaction invalidReaction = CreateReactionFiller(randomDateTimeOffset, randomUserId).Create();
            invalidReaction.Name = GetRandomStringWithLengthOf(31);
            invalidReaction.UnicodeEmoji = GetRandomStringWithLengthOf(17);

            var invalidReactionException =
                new InvalidReactionException(
                    message: "Reaction is invalid, fix the errors and try again.");

            invalidReactionException.AddData(
                key: nameof(Reaction.Name),
                values: $"Text exceed max length of {invalidReaction.Name.Length - 1} characters");

            invalidReactionException.AddData(
                key: nameof(Reaction.UnicodeEmoji),
                values: $"Text exceed max length of {invalidReaction.UnicodeEmoji.Length - 1} characters");

            invalidReactionException.AddData(
                key: nameof(Reaction.CreatedBy),
                values: $"Text exceed max length of {invalidReaction.CreatedBy.Length - 1} characters");

            invalidReactionException.AddData(
                key: nameof(Reaction.UpdatedBy),
                values: $"Text exceed max length of {invalidReaction.UpdatedBy.Length - 1} characters");

            var expectedReactionValidationException =
                new ReactionValidationException(
                    message: "Reaction validation error occurred, fix the errors and try again.",
                    innerException: invalidReactionException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Reaction> addReactionTask =
                this.reactionService.AddReactionAsync(
                    invalidReaction,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualReactionValidationException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    addReactionTask.AsTask);

            // then
            actualReactionValidationException.Should().BeEquivalentTo(
                expectedReactionValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedReactionValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfUpdatedWhenIsNotSameAsCreatedWhenAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Reaction randomReaction = CreateReactionFiller(randomDateTimeOffset, randomUserId).Create();
            Reaction invalidReaction = randomReaction;
            invalidReaction.UpdatedWhen = GetRandomDateTimeOffset();

            var invalidReactionException =
                new InvalidReactionException(
                    message: "Reaction is invalid, fix the errors and try again.");

            invalidReactionException.AddData(
                key: nameof(Reaction.UpdatedWhen),
                values: $"Date is not the same as {nameof(Reaction.CreatedWhen)}");

            var expectedReactionValidationException =
                new ReactionValidationException(
                    message: "Reaction validation error occurred, fix the errors and try again.",
                    innerException: invalidReactionException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Reaction> addReactionTask =
                this.reactionService.AddReactionAsync(
                    invalidReaction,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualReactionValidationException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    addReactionTask.AsTask);

            // then
            actualReactionValidationException.Should().BeEquivalentTo(
                expectedReactionValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedReactionValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfCreatedByIsNotSameAsCurrentUserIdAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomString();
            string differentUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Reaction randomReaction = CreateReactionFiller(randomDateTimeOffset, randomUserId).Create();
            Reaction invalidReaction = randomReaction;
            invalidReaction.CreatedBy = differentUserId;
            invalidReaction.UpdatedBy = differentUserId;

            var invalidReactionException =
                new InvalidReactionException(
                    message: "Reaction is invalid, fix the errors and try again.");

            invalidReactionException.AddData(
                key: nameof(Reaction.CreatedBy),
                values: $"Expected value to be '{randomUserId}' but found '{differentUserId}'.");

            var expectedReactionValidationException =
                new ReactionValidationException(
                    message: "Reaction validation error occurred, fix the errors and try again.",
                    innerException: invalidReactionException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Reaction> addReactionTask =
                this.reactionService.AddReactionAsync(
                    invalidReaction,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualReactionValidationException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    addReactionTask.AsTask);

            // then
            actualReactionValidationException.Should().BeEquivalentTo(
                expectedReactionValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedReactionValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfUpdatedByIsNotSameAsCreatedByAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Reaction randomReaction = CreateReactionFiller(randomDateTimeOffset, randomUserId).Create();
            Reaction invalidReaction = randomReaction;
            invalidReaction.UpdatedBy = GetRandomString();

            var invalidReactionException =
                new InvalidReactionException(
                    message: "Reaction is invalid, fix the errors and try again.");

            invalidReactionException.AddData(
                key: nameof(Reaction.UpdatedBy),
                values: $"Text is not the same as {nameof(Reaction.CreatedBy)}");

            var expectedReactionValidationException =
                new ReactionValidationException(
                    message: "Reaction validation error occurred, fix the errors and try again.",
                    innerException: invalidReactionException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Reaction> addReactionTask =
                this.reactionService.AddReactionAsync(
                    invalidReaction,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualReactionValidationException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    addReactionTask.AsTask);

            // then
            actualReactionValidationException.Should().BeEquivalentTo(
                expectedReactionValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedReactionValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(MinutesBeforeOrAfter))]
        public async Task ShouldThrowValidationExceptionOnAddIfCreatedWhenIsNotRecentAndLogItAsync(int minutes)
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Reaction randomReaction = CreateReactionFiller(randomDateTimeOffset, randomUserId).Create();
            Reaction invalidReaction = randomReaction;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidReaction.CreatedWhen = randomDateTimeOffset.AddMinutes(minutes);
            invalidReaction.UpdatedWhen = invalidReaction.CreatedWhen;

            var invalidReactionException =
                new InvalidReactionException(
                    message: "Reaction is invalid, fix the errors and try again.");

            invalidReactionException.AddData(
                key: nameof(Reaction.CreatedWhen),
                values: $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                    $"but found {invalidReaction.CreatedWhen}");

            var expectedReactionValidationException =
                new ReactionValidationException(
                    message: "Reaction validation error occurred, fix the errors and try again.",
                    innerException: invalidReactionException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<Reaction> addReactionTask =
                this.reactionService.AddReactionAsync(
                    invalidReaction,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualReactionValidationException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    addReactionTask.AsTask);

            // then
            actualReactionValidationException.Should().BeEquivalentTo(
                expectedReactionValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedReactionValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
