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
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.BibleReferences.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.BibleReferences
{
    public partial class BibleReferenceServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfBibleReferenceIsNullAndLogItAsync()
        {
            // given
            BibleReference nullBibleReference = null;

            var nullBibleReferenceException =
                new NullBibleReferenceException(message: "Bible reference is null.");

            var expectedBibleReferenceValidationException =
                new BibleReferenceValidationException(
                    message: "Bible reference validation error occurred, fix the errors and try again.",
                    innerException: nullBibleReferenceException);

            // when
            ValueTask<BibleReference> modifyBibleReferenceTask =
                this.bibleReferenceService.ModifyBibleReferenceAsync(
                    nullBibleReference,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualBibleReferenceValidationException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(
                    modifyBibleReferenceTask.AsTask);

            // then
            actualBibleReferenceValidationException.Should().BeEquivalentTo(
                expectedBibleReferenceValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedBibleReferenceValidationException))),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfBibleReferenceIsInvalidAndLogItAsync(
            string invalidText)
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            DateTimeOffset startDate = randomDateTimeOffset.AddSeconds(-90);
            DateTimeOffset endDate = randomDateTimeOffset;

            var invalidBibleReference = new BibleReference
            {
                Id = Guid.Empty,
                Reference = invalidText,
                Translation = invalidText,
                ContentItemGroupId = Guid.Empty,
                CreatedBy = invalidText,
                UpdatedBy = invalidText,
                CreatedWhen = default,
                UpdatedWhen = default
            };

            var invalidBibleReferenceException =
                new InvalidBibleReferenceException(
                    message: "Bible reference is invalid, fix the errors and try again.");

            invalidBibleReferenceException.AddData(
                key: nameof(BibleReference.Id),
                values: "Id is required");

            invalidBibleReferenceException.AddData(
                key: nameof(BibleReference.Reference),
                values: "Text is required");

            invalidBibleReferenceException.AddData(
                key: nameof(BibleReference.Translation),
                values: "Text is required");

            invalidBibleReferenceException.AddData(
                key: nameof(BibleReference.ContentItemGroupId),
                values: "Id is required");

            invalidBibleReferenceException.AddData(
                key: nameof(BibleReference.CreatedBy),
                values: "Text is required");

            invalidBibleReferenceException.AddData(
                key: nameof(BibleReference.UpdatedBy),
                values: "Text is required");

            invalidBibleReferenceException.AddData(
                key: nameof(BibleReference.CreatedWhen),
                values: "Date is required");

            invalidBibleReferenceException.AddData(
                key: nameof(BibleReference.UpdatedWhen),
                values: new[]
                {
                    "Date is required",
                    "Date is the same as CreatedWhen",

                    "Date is not recent. Expected a value between " +
                        $"{startDate} and {endDate} but found {default(DateTimeOffset)}"
                });

            var expectedBibleReferenceValidationException =
                new BibleReferenceValidationException(
                    message: "Bible reference validation error occurred, fix the errors and try again.",
                    innerException: invalidBibleReferenceException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidBibleReference, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidBibleReference);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidText);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<BibleReference> modifyBibleReferenceTask =
                this.bibleReferenceService.ModifyBibleReferenceAsync(
                    invalidBibleReference,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualBibleReferenceValidationException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(
                    modifyBibleReferenceTask.AsTask);

            // then
            actualBibleReferenceValidationException.Should().BeEquivalentTo(
                expectedBibleReferenceValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidBibleReference, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedBibleReferenceValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfBibleReferenceNotFoundAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            BibleReference randomBibleReference = CreateRandomModifyBibleReference(randomDateTimeOffset, randomUserId);
            BibleReference nonExistentBibleReference = randomBibleReference;
            BibleReference noBibleReference = null;

            var notFoundBibleReferenceException = new NotFoundBibleReferenceException(
                message: $"Bible reference not found with id: {nonExistentBibleReference.Id}.");

            var expectedBibleReferenceValidationException = new BibleReferenceValidationException(
                message: "Bible reference validation error occurred, fix the errors and try again.",
                innerException: notFoundBibleReferenceException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentBibleReference, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(nonExistentBibleReference);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    nonExistentBibleReference.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noBibleReference);

            // when
            ValueTask<BibleReference> modifyBibleReferenceTask =
                this.bibleReferenceService.ModifyBibleReferenceAsync(
                    nonExistentBibleReference,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualBibleReferenceValidationException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(
                    modifyBibleReferenceTask.AsTask);

            // then
            actualBibleReferenceValidationException.Should().BeEquivalentTo(
                expectedBibleReferenceValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentBibleReference, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    nonExistentBibleReference.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedBibleReferenceValidationException))),
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
            BibleReference randomBibleReference = CreateRandomModifyBibleReference(randomDateTimeOffset, randomUserId);
            BibleReference invalidBibleReference = randomBibleReference;
            BibleReference storageBibleReference = randomBibleReference.DeepClone();
            storageBibleReference.CreatedWhen = GetRandomDateTimeOffset();
            storageBibleReference.UpdatedWhen = GetRandomDateTimeOffset();

            var invalidBibleReferenceException = new InvalidBibleReferenceException(
                message: "Bible reference is invalid, fix the errors and try again.");

            invalidBibleReferenceException.AddData(
                key: nameof(BibleReference.CreatedWhen),
                values: $"Date is not the same as {nameof(BibleReference.CreatedWhen)}");

            var expectedBibleReferenceValidationException = new BibleReferenceValidationException(
                message: "Bible reference validation error occurred, fix the errors and try again.",
                innerException: invalidBibleReferenceException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidBibleReference, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidBibleReference);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    invalidBibleReference.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageBibleReference);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidBibleReference,
                    storageBibleReference))
                        .ReturnsAsync(invalidBibleReference);

            // when
            ValueTask<BibleReference> modifyBibleReferenceTask =
                this.bibleReferenceService.ModifyBibleReferenceAsync(
                    invalidBibleReference,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualBibleReferenceValidationException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(
                    modifyBibleReferenceTask.AsTask);

            // then
            actualBibleReferenceValidationException.Should().BeEquivalentTo(
                expectedBibleReferenceValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidBibleReference, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    invalidBibleReference.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidBibleReference,
                    storageBibleReference),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedBibleReferenceValidationException))),
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
            BibleReference randomBibleReference = CreateRandomModifyBibleReference(randomDateTimeOffset, randomUserId);
            BibleReference invalidBibleReference = randomBibleReference;
            BibleReference storageBibleReference = randomBibleReference.DeepClone();
            storageBibleReference.CreatedBy = GetRandomString();
            storageBibleReference.UpdatedWhen = storageBibleReference.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var invalidBibleReferenceException =
                new InvalidBibleReferenceException(
                    message: "Bible reference is invalid, fix the errors and try again.");

            invalidBibleReferenceException.AddData(
                key: nameof(BibleReference.CreatedBy),
                values: $"Text is not the same as {nameof(BibleReference.CreatedBy)}");

            var expectedBibleReferenceValidationException =
                new BibleReferenceValidationException(
                    message: "Bible reference validation error occurred, fix the errors and try again.",
                    innerException: invalidBibleReferenceException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidBibleReference, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidBibleReference);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    invalidBibleReference.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageBibleReference);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidBibleReference,
                    storageBibleReference))
                        .ReturnsAsync(invalidBibleReference);

            // when
            ValueTask<BibleReference> modifyBibleReferenceTask =
                this.bibleReferenceService.ModifyBibleReferenceAsync(
                    invalidBibleReference,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualBibleReferenceValidationException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(
                    modifyBibleReferenceTask.AsTask);

            // then
            actualBibleReferenceValidationException.Should().BeEquivalentTo(
                expectedBibleReferenceValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidBibleReference, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    invalidBibleReference.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidBibleReference,
                    storageBibleReference),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedBibleReferenceValidationException))),
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
            BibleReference randomBibleReference = CreateRandomModifyBibleReference(randomDateTimeOffset, randomUserId);
            BibleReference invalidBibleReference = randomBibleReference;
            BibleReference storageBibleReference = randomBibleReference.DeepClone();

            var invalidBibleReferenceException =
                new InvalidBibleReferenceException(
                    message: "Bible reference is invalid, fix the errors and try again.");

            invalidBibleReferenceException.AddData(
                key: nameof(BibleReference.UpdatedWhen),
                values: $"Date is the same as {nameof(BibleReference.UpdatedWhen)}");

            var expectedBibleReferenceValidationException =
                new BibleReferenceValidationException(
                    message: "Bible reference validation error occurred, fix the errors and try again.",
                    innerException: invalidBibleReferenceException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidBibleReference, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidBibleReference);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    invalidBibleReference.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageBibleReference);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidBibleReference,
                    storageBibleReference))
                        .ReturnsAsync(invalidBibleReference);

            // when
            ValueTask<BibleReference> modifyBibleReferenceTask =
                this.bibleReferenceService.ModifyBibleReferenceAsync(
                    invalidBibleReference,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualBibleReferenceValidationException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(
                    modifyBibleReferenceTask.AsTask);

            // then
            actualBibleReferenceValidationException.Should().BeEquivalentTo(
                expectedBibleReferenceValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidBibleReference, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectBibleReferenceByIdAsync(
                    invalidBibleReference.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidBibleReference,
                    storageBibleReference),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedBibleReferenceValidationException))),
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
            BibleReference randomBibleReference = CreateRandomModifyBibleReference(randomDateTimeOffset, randomUserId);
            BibleReference invalidBibleReference = randomBibleReference;
            invalidBibleReference.UpdatedBy = differentUserId;

            var invalidBibleReferenceException =
                new InvalidBibleReferenceException(
                    message: "Bible reference is invalid, fix the errors and try again.");

            invalidBibleReferenceException.AddData(
                key: nameof(BibleReference.UpdatedBy),
                values: $"Expected value to be '{randomUserId}' but found '{differentUserId}'.");

            var expectedBibleReferenceValidationException =
                new BibleReferenceValidationException(
                    message: "Bible reference validation error occurred, fix the errors and try again.",
                    innerException: invalidBibleReferenceException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidBibleReference, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidBibleReference);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<BibleReference> modifyBibleReferenceTask =
                this.bibleReferenceService.ModifyBibleReferenceAsync(
                    invalidBibleReference,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualBibleReferenceValidationException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(
                    modifyBibleReferenceTask.AsTask);

            // then
            actualBibleReferenceValidationException.Should().BeEquivalentTo(
                expectedBibleReferenceValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidBibleReference, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedBibleReferenceValidationException))),
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
            BibleReference randomBibleReference = CreateRandomModifyBibleReference(randomDateTimeOffset, randomUserId);
            BibleReference invalidBibleReference = randomBibleReference;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidBibleReference.UpdatedWhen = invalidBibleReference.CreatedWhen;

            var invalidBibleReferenceException =
                new InvalidBibleReferenceException(
                    message: "Bible reference is invalid, fix the errors and try again.");

            invalidBibleReferenceException.AddData(
                key: nameof(BibleReference.UpdatedWhen),
                values: new[]
                {
                    $"Date is the same as {nameof(BibleReference.CreatedWhen)}",
                    $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                        $"but found {invalidBibleReference.UpdatedWhen}"
                });

            var expectedBibleReferenceValidationException =
                new BibleReferenceValidationException(
                    message: "Bible reference validation error occurred, fix the errors and try again.",
                    innerException: invalidBibleReferenceException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidBibleReference, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidBibleReference);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<BibleReference> modifyBibleReferenceTask =
                this.bibleReferenceService.ModifyBibleReferenceAsync(
                    invalidBibleReference,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualBibleReferenceValidationException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(
                    modifyBibleReferenceTask.AsTask);

            // then
            actualBibleReferenceValidationException.Should().BeEquivalentTo(
                expectedBibleReferenceValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidBibleReference, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedBibleReferenceValidationException))),
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
            BibleReference randomBibleReference = CreateRandomModifyBibleReference(randomDateTimeOffset, randomUserId);
            BibleReference invalidBibleReference = randomBibleReference;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidBibleReference.UpdatedWhen = randomDateTimeOffset.AddMinutes(minutes);

            var invalidBibleReferenceException =
                new InvalidBibleReferenceException(
                    message: "Bible reference is invalid, fix the errors and try again.");

            invalidBibleReferenceException.AddData(
                key: nameof(BibleReference.UpdatedWhen),
                values: $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                    $"but found {invalidBibleReference.UpdatedWhen}");

            var expectedBibleReferenceValidationException =
                new BibleReferenceValidationException(
                    message: "Bible reference validation error occurred, fix the errors and try again.",
                    innerException: invalidBibleReferenceException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidBibleReference, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidBibleReference);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<BibleReference> modifyBibleReferenceTask =
                this.bibleReferenceService.ModifyBibleReferenceAsync(
                    invalidBibleReference,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualBibleReferenceValidationException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(
                    modifyBibleReferenceTask.AsTask);

            // then
            actualBibleReferenceValidationException.Should().BeEquivalentTo(
                expectedBibleReferenceValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidBibleReference, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedBibleReferenceValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfBibleReferenceExceedsMaxLengthAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomStringWithLengthOf(256);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            BibleReference invalidBibleReference = CreateRandomModifyBibleReference(randomDateTimeOffset, randomUserId);

            var invalidBibleReferenceException =
                new InvalidBibleReferenceException(
                    message: "Bible reference is invalid, fix the errors and try again.");

            invalidBibleReferenceException.AddData(
                key: nameof(BibleReference.CreatedBy),
                values: $"Text exceed max length of {invalidBibleReference.CreatedBy.Length - 1} characters");

            invalidBibleReferenceException.AddData(
                key: nameof(BibleReference.UpdatedBy),
                values: $"Text exceed max length of {invalidBibleReference.UpdatedBy.Length - 1} characters");

            var expectedBibleReferenceValidationException =
                new BibleReferenceValidationException(
                    message: "Bible reference validation error occurred, fix the errors and try again.",
                    innerException: invalidBibleReferenceException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidBibleReference, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidBibleReference);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<BibleReference> modifyBibleReferenceTask =
                this.bibleReferenceService.ModifyBibleReferenceAsync(
                    invalidBibleReference,
                    TestContext.Current.CancellationToken);

            BibleReferenceValidationException actualBibleReferenceValidationException =
                await Assert.ThrowsAsync<BibleReferenceValidationException>(
                    modifyBibleReferenceTask.AsTask);

            // then
            actualBibleReferenceValidationException.Should().BeEquivalentTo(
                expectedBibleReferenceValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidBibleReference, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedBibleReferenceValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}