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
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Reactions.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Reactions
{
    public partial class ReactionServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfReactionIsNullAndLogItAsync()
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
            ValueTask<Reaction> modifyReactionTask =
                this.reactionService.ModifyReactionAsync(
                    nullReaction,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualReactionValidationException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    modifyReactionTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnModifyIfReactionIsInvalidAndLogItAsync(
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
                values: "Date is required");

            invalidReactionException.AddData(
                key: nameof(Reaction.UpdatedWhen),
                values: new[]
                {
                    "Date is required",
                    "Date is the same as CreatedWhen",

                    "Date is not recent. Expected a value between " +
                        $"{startDate} and {endDate} but found {default(DateTimeOffset)}"
                });

            var expectedReactionValidationException =
                new ReactionValidationException(
                    message: "Reaction validation error occurred, fix the errors and try again.",
                    innerException: invalidReactionException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidText);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Reaction> modifyReactionTask =
                this.reactionService.ModifyReactionAsync(
                    invalidReaction,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualReactionValidationException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    modifyReactionTask.AsTask);

            // then
            actualReactionValidationException.Should().BeEquivalentTo(
                expectedReactionValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfReactionNotFoundAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Reaction randomReaction = CreateRandomModifyReaction(randomDateTimeOffset, randomUserId);
            Reaction nonExistentReaction = randomReaction;
            Reaction noReaction = null;

            var notFoundReactionException = new NotFoundReactionException(
                message: $"Reaction not found with id: {nonExistentReaction.Id}.");

            var expectedReactionValidationException = new ReactionValidationException(
                message: "Reaction validation error occurred, fix the errors and try again.",
                innerException: notFoundReactionException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentReaction, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(nonExistentReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectReactionByIdAsync(
                    nonExistentReaction.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noReaction);

            // when
            ValueTask<Reaction> modifyReactionTask =
                this.reactionService.ModifyReactionAsync(
                    nonExistentReaction,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualReactionValidationException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    modifyReactionTask.AsTask);

            // then
            actualReactionValidationException.Should().BeEquivalentTo(
                expectedReactionValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentReaction, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectReactionByIdAsync(
                    nonExistentReaction.Id,
                    TestContext.Current.CancellationToken),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfStorageCreatedWhenNotSameAsInputAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Reaction randomReaction = CreateRandomModifyReaction(randomDateTimeOffset, randomUserId);
            Reaction invalidReaction = randomReaction;
            Reaction storageReaction = randomReaction.DeepClone();
            storageReaction.CreatedWhen = GetRandomDateTimeOffset();
            storageReaction.UpdatedWhen = GetRandomDateTimeOffset();

            var invalidReactionException = new InvalidReactionException(
                message: "Reaction is invalid, fix the errors and try again.");

            invalidReactionException.AddData(
                key: nameof(Reaction.CreatedWhen),
                values: $"Date is not the same as {nameof(Reaction.CreatedWhen)}");

            var expectedReactionValidationException = new ReactionValidationException(
                message: "Reaction validation error occurred, fix the errors and try again.",
                innerException: invalidReactionException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectReactionByIdAsync(
                    invalidReaction.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidReaction,
                    storageReaction))
                        .ReturnsAsync(invalidReaction);

            // when
            ValueTask<Reaction> modifyReactionTask =
                this.reactionService.ModifyReactionAsync(
                    invalidReaction,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualReactionValidationException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    modifyReactionTask.AsTask);

            // then
            actualReactionValidationException.Should().BeEquivalentTo(
                expectedReactionValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectReactionByIdAsync(
                    invalidReaction.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidReaction,
                    storageReaction),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfStorageCreatedByNotSameAsInputAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Reaction randomReaction = CreateRandomModifyReaction(randomDateTimeOffset, randomUserId);
            Reaction invalidReaction = randomReaction;
            Reaction storageReaction = randomReaction.DeepClone();
            storageReaction.CreatedBy = GetRandomString();
            storageReaction.UpdatedWhen = storageReaction.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var invalidReactionException =
                new InvalidReactionException(
                    message: "Reaction is invalid, fix the errors and try again.");

            invalidReactionException.AddData(
                key: nameof(Reaction.CreatedBy),
                values: $"Text is not the same as {nameof(Reaction.CreatedBy)}");

            var expectedReactionValidationException =
                new ReactionValidationException(
                    message: "Reaction validation error occurred, fix the errors and try again.",
                    innerException: invalidReactionException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectReactionByIdAsync(
                    invalidReaction.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidReaction,
                    storageReaction))
                        .ReturnsAsync(invalidReaction);

            // when
            ValueTask<Reaction> modifyReactionTask =
                this.reactionService.ModifyReactionAsync(
                    invalidReaction,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualReactionValidationException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    modifyReactionTask.AsTask);

            // then
            actualReactionValidationException.Should().BeEquivalentTo(
                expectedReactionValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectReactionByIdAsync(
                    invalidReaction.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidReaction,
                    storageReaction),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfStorageUpdatedWhenSameAsInputAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Reaction randomReaction = CreateRandomModifyReaction(randomDateTimeOffset, randomUserId);
            Reaction invalidReaction = randomReaction;
            Reaction storageReaction = randomReaction.DeepClone();

            var invalidReactionException =
                new InvalidReactionException(
                    message: "Reaction is invalid, fix the errors and try again.");

            invalidReactionException.AddData(
                key: nameof(Reaction.UpdatedWhen),
                values: $"Date is the same as {nameof(Reaction.UpdatedWhen)}");

            var expectedReactionValidationException =
                new ReactionValidationException(
                    message: "Reaction validation error occurred, fix the errors and try again.",
                    innerException: invalidReactionException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectReactionByIdAsync(
                    invalidReaction.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidReaction,
                    storageReaction))
                        .ReturnsAsync(invalidReaction);

            // when
            ValueTask<Reaction> modifyReactionTask =
                this.reactionService.ModifyReactionAsync(
                    invalidReaction,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualReactionValidationException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    modifyReactionTask.AsTask);

            // then
            actualReactionValidationException.Should().BeEquivalentTo(
                expectedReactionValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectReactionByIdAsync(
                    invalidReaction.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidReaction,
                    storageReaction),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfUpdatedByIsNotSameAsCurrentUserIdAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomString();
            string differentUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Reaction randomReaction = CreateRandomModifyReaction(randomDateTimeOffset, randomUserId);
            Reaction invalidReaction = randomReaction;
            invalidReaction.UpdatedBy = differentUserId;

            var invalidReactionException =
                new InvalidReactionException(
                    message: "Reaction is invalid, fix the errors and try again.");

            invalidReactionException.AddData(
                key: nameof(Reaction.UpdatedBy),
                values: $"Expected value to be '{randomUserId}' but found '{differentUserId}'.");

            var expectedReactionValidationException =
                new ReactionValidationException(
                    message: "Reaction validation error occurred, fix the errors and try again.",
                    innerException: invalidReactionException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Reaction> modifyReactionTask =
                this.reactionService.ModifyReactionAsync(
                    invalidReaction,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualReactionValidationException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    modifyReactionTask.AsTask);

            // then
            actualReactionValidationException.Should().BeEquivalentTo(
                expectedReactionValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfUpdatedWhenIsSameAsCreatedWhenAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Reaction randomReaction = CreateRandomModifyReaction(randomDateTimeOffset, randomUserId);
            Reaction invalidReaction = randomReaction;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidReaction.UpdatedWhen = invalidReaction.CreatedWhen;

            var invalidReactionException =
                new InvalidReactionException(
                    message: "Reaction is invalid, fix the errors and try again.");

            invalidReactionException.AddData(
                key: nameof(Reaction.UpdatedWhen),
                values: new[]
                {
                    $"Date is the same as {nameof(Reaction.CreatedWhen)}",
                    $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                        $"but found {invalidReaction.UpdatedWhen}"
                });

            var expectedReactionValidationException =
                new ReactionValidationException(
                    message: "Reaction validation error occurred, fix the errors and try again.",
                    innerException: invalidReactionException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Reaction> modifyReactionTask =
                this.reactionService.ModifyReactionAsync(
                    invalidReaction,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualReactionValidationException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    modifyReactionTask.AsTask);

            // then
            actualReactionValidationException.Should().BeEquivalentTo(
                expectedReactionValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfUpdatedWhenIsNotRecentAndLogItAsync(int minutes)
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Reaction randomReaction = CreateRandomModifyReaction(randomDateTimeOffset, randomUserId);
            Reaction invalidReaction = randomReaction;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidReaction.UpdatedWhen = randomDateTimeOffset.AddMinutes(minutes);

            var invalidReactionException =
                new InvalidReactionException(
                    message: "Reaction is invalid, fix the errors and try again.");

            invalidReactionException.AddData(
                key: nameof(Reaction.UpdatedWhen),
                values: $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                    $"but found {invalidReaction.UpdatedWhen}");

            var expectedReactionValidationException =
                new ReactionValidationException(
                    message: "Reaction validation error occurred, fix the errors and try again.",
                    innerException: invalidReactionException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<Reaction> modifyReactionTask =
                this.reactionService.ModifyReactionAsync(
                    invalidReaction,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualReactionValidationException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    modifyReactionTask.AsTask);

            // then
            actualReactionValidationException.Should().BeEquivalentTo(
                expectedReactionValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfReactionExceedsMaxLengthAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomStringWithLengthOf(256);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Reaction invalidReaction = CreateRandomModifyReaction(randomDateTimeOffset, randomUserId);

            var invalidReactionException =
                new InvalidReactionException(
                    message: "Reaction is invalid, fix the errors and try again.");

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
                broker.ApplyModifyAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Reaction> modifyReactionTask =
                this.reactionService.ModifyReactionAsync(
                    invalidReaction,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualReactionValidationException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    modifyReactionTask.AsTask);

            // then
            actualReactionValidationException.Should().BeEquivalentTo(
                expectedReactionValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidReaction, It.IsAny<SecurityContext>()),
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