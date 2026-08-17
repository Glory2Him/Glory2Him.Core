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
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Reactions.Exceptions;
using Glory2Him.Core.Models.Securities;
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
                Times.Exactly(2));

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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
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
                Times.Exactly(2));

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
                Times.Exactly(2));

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

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnModifyIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext invalidSecurityContext)
        {
            // given
            this.ambientSecurityContext = invalidSecurityContext;
            Reaction someReaction = CreateRandomReaction();

            var unauthorizedReactionException = new UnauthorizedReactionException(
                message: "The current user is not authenticated.");

            var expectedReactionValidationException = new ReactionValidationException(
                message: "Reaction validation error occurred, fix the errors and try again.",
                innerException: unauthorizedReactionException);

            // when
            ValueTask<Reaction> modifyReactionTask =
                this.reactionService.ModifyReactionAsync(
                    someReaction,
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
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.ReactionReadOnly)]
        public async Task ShouldThrowValidationExceptionOnModifyIfUserIsBlockedFromContributingAndLogItAsync(
            string blockedRole)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(blockedRole);
            Reaction someReaction = CreateRandomReaction();

            var unauthorizedReactionException = new UnauthorizedReactionException(
                message: "The current user is blocked from contributing reactions.");

            var expectedReactionValidationException = new ReactionValidationException(
                message: "Reaction validation error occurred, fix the errors and try again.",
                innerException: unauthorizedReactionException);

            // when
            ValueTask<Reaction> modifyReactionTask =
                this.reactionService.ModifyReactionAsync(
                    someReaction,
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

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfUserIsNotOwnerAndHasNoReviewRoleAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Reaction randomReaction = CreateRandomModifyReaction(randomDateTimeOffset, randomUserId);
            Reaction inputReaction = randomReaction;
            Reaction storageReaction = randomReaction.DeepClone();
            storageReaction.CreatedBy = GetRandomString();
            storageReaction.UpdatedWhen = storageReaction.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var unauthorizedReactionException = new UnauthorizedReactionException(
                message: "The current user is not allowed to modify this reaction.");

            var expectedReactionValidationException = new ReactionValidationException(
                message: "Reaction validation error occurred, fix the errors and try again.",
                innerException: unauthorizedReactionException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputReaction, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectReactionByIdAsync(
                    inputReaction.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageReaction);

            // when
            ValueTask<Reaction> modifyReactionTask =
                this.reactionService.ModifyReactionAsync(
                    inputReaction,
                    TestContext.Current.CancellationToken);

            ReactionValidationException actualReactionValidationException =
                await Assert.ThrowsAsync<ReactionValidationException>(
                    modifyReactionTask.AsTask);

            // then
            actualReactionValidationException.Should().BeEquivalentTo(
                expectedReactionValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(inputReaction, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectReactionByIdAsync(
                    inputReaction.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateReactionAsync(
                    It.IsAny<Reaction>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

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
            Reaction invalidReaction = CreateRandomModifyReaction(randomDateTimeOffset, randomUserId);
            invalidReaction.CreatedBy = ownerUserId;
            invalidReaction.ApprovalStatus = ApprovalStatus.Draft;
            Reaction storageReaction = invalidReaction.DeepClone();
            storageReaction.UpdatedWhen = storageReaction.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            invalidReaction.ApprovalStatus = ApprovalStatus.Submitted;

            var invalidReactionException =
                new InvalidReactionException(
                    message: "Reaction is invalid, fix the errors and try again.");

            invalidReactionException.AddData(
                key: nameof(Reaction.ApprovalStatus),
                values: "Value is not the same as storage approval status");

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
                Times.Exactly(2));

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
        public async Task ShouldThrowValidationExceptionOnModifyIfIsPublishedChangedAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Reaction randomReaction = CreateRandomModifyReaction(randomDateTimeOffset, randomUserId);
            Reaction invalidReaction = randomReaction;
            Reaction storageReaction = randomReaction.DeepClone();
            invalidReaction.IsPublished = true;
            storageReaction.IsPublished = false;
            storageReaction.UpdatedWhen = storageReaction.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var invalidReactionException =
                new InvalidReactionException(
                    message: "Reaction is invalid, fix the errors and try again.");

            invalidReactionException.AddData(
                key: nameof(Reaction.IsPublished),
                values: "Value is not the same as IsPublished");

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
                Times.Exactly(2));

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
        public async Task ShouldThrowValidationExceptionOnModifyIfIsApprovedByBypassChangedAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Reaction randomReaction = CreateRandomModifyReaction(randomDateTimeOffset, randomUserId);
            Reaction invalidReaction = randomReaction;
            Reaction storageReaction = randomReaction.DeepClone();
            invalidReaction.IsApprovedByBypass = !storageReaction.IsApprovedByBypass;
            storageReaction.UpdatedWhen = storageReaction.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var invalidReactionException =
                new InvalidReactionException(
                    message: "Reaction is invalid, fix the errors and try again.");

            invalidReactionException.AddData(
                key: nameof(Reaction.IsApprovedByBypass),
                values: "Value is not the same as IsApprovedByBypass");

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
                Times.Exactly(2));

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
        public async Task ShouldThrowValidationExceptionOnModifyIfApprovedByBypassReasonChangedAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Reaction randomReaction = CreateRandomModifyReaction(randomDateTimeOffset, randomUserId);
            Reaction invalidReaction = randomReaction;
            Reaction storageReaction = randomReaction.DeepClone();
            storageReaction.ApprovedByBypassReason = GetRandomString();
            invalidReaction.ApprovedByBypassReason = GetRandomString();
            storageReaction.UpdatedWhen = storageReaction.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var invalidReactionException =
                new InvalidReactionException(
                    message: "Reaction is invalid, fix the errors and try again.");

            invalidReactionException.AddData(
                key: nameof(Reaction.ApprovedByBypassReason),
                values: "Text is not the same as ApprovedByBypassReason");

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
                Times.Exactly(2));

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
        public async Task ShouldThrowValidationExceptionOnModifyIfPublishDateChangedAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Reaction randomReaction = CreateRandomModifyReaction(randomDateTimeOffset, randomUserId);
            Reaction invalidReaction = randomReaction;
            Reaction storageReaction = randomReaction.DeepClone();
            invalidReaction.PublishDate = randomDateTimeOffset;
            storageReaction.PublishDate = null;
            storageReaction.UpdatedWhen = storageReaction.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var invalidReactionException =
                new InvalidReactionException(
                    message: "Reaction is invalid, fix the errors and try again.");

            invalidReactionException.AddData(
                key: nameof(Reaction.PublishDate),
                values: "Date is not the same as PublishDate");

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
                Times.Exactly(2));

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
            Reaction randomReaction = CreateRandomModifyReaction(randomDateTimeOffset, randomUserId);
            Reaction invalidReaction = randomReaction;
            Reaction storageReaction = randomReaction.DeepClone();
            storageReaction.UpdatedWhen = storageReaction.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            // both sides terminal and IDENTICAL, so nothing else in the modify can refuse it
            invalidReaction.ApprovalStatus = terminalStatus;
            storageReaction.ApprovalStatus = terminalStatus;

            var invalidReactionException =
                new InvalidReactionException(
                    message: "Reaction cannot be modified from status " +
                        $"{terminalStatus}.");

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

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateReactionAsync(
                    It.IsAny<Reaction>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.eventBrokerMock.Verify(broker =>
                broker.PublishReactionAsync(
                    It.IsAny<EventEnvelope<Reaction>>(),
                    It.IsAny<ReactionEventOperation>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedReactionValidationException))),
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
            Reaction randomReaction = CreateRandomModifyReaction(randomDateTimeOffset, randomUserId);
            Reaction invalidReaction = randomReaction;
            Reaction storageReaction = randomReaction.DeepClone();
            storageReaction.UpdatedWhen = storageReaction.UpdatedWhen.AddDays(GetRandomNegativeNumber());
            storageReaction.CreatedBy = GetRandomString();

            invalidReaction.ApprovalStatus = ApprovalStatus.Approved;
            storageReaction.ApprovalStatus = ApprovalStatus.Approved;
            invalidReaction.CreatedBy = storageReaction.CreatedBy;

            var invalidReactionException =
                new InvalidReactionException(
                    message: "Reaction cannot be modified from status " +
                        $"{ApprovalStatus.Approved}.");

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

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateReactionAsync(
                    It.IsAny<Reaction>(),
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
            Reaction randomReaction = CreateRandomModifyReaction(randomDateTimeOffset, randomUserId);
            Reaction inputReaction = randomReaction;
            Reaction storageReaction = randomReaction.DeepClone();
            storageReaction.UpdatedWhen = storageReaction.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            inputReaction.ApprovalStatus = nonTerminalStatus;
            storageReaction.ApprovalStatus = nonTerminalStatus;

            Reaction updatedReaction = inputReaction.DeepClone();

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputReaction, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectReactionByIdAsync(
                    inputReaction.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageReaction);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    inputReaction,
                    storageReaction))
                        .ReturnsAsync(inputReaction);

            this.storageBrokerMock.Setup(broker =>
                broker.UpdateReactionAsync(
                    inputReaction,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(updatedReaction);

            this.eventBrokerMock.Setup(broker =>
                broker.PublishReactionAsync(
                    It.IsAny<EventEnvelope<Reaction>>(),
                    ReactionEventOperation.Modified))
                        .Returns(new ValueTask<EventPublishResult<Reaction>>(
                            new EventPublishResult<Reaction>()));

            // when
            Reaction actualReaction = await this.reactionService.ModifyReactionAsync(
                inputReaction,
                TestContext.Current.CancellationToken);

            // then
            actualReaction.Should().BeEquivalentTo(updatedReaction);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateReactionAsync(
                    inputReaction,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
