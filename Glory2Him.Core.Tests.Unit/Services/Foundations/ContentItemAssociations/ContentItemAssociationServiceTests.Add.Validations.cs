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
using Glory2Him.Core.Models.Foundations.ContentItemAssociations;
using Glory2Him.Core.Models.Foundations.ContentItemAssociations.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItemAssociations
{
    public partial class ContentItemAssociationServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfContentItemAssociationIsNullAndLogItAsync()
        {
            // given
            ContentItemAssociation nullContentItemAssociation = null;

            var nullContentItemAssociationException =
                new NullContentItemAssociationException(message: "Content item association is null.");

            var expectedContentItemAssociationValidationException =
                new ContentItemAssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: nullContentItemAssociationException);

            // when
            ValueTask<ContentItemAssociation> addContentItemAssociationTask =
                this.contentItemAssociationService.AddContentItemAssociationAsync(
                    nullContentItemAssociation,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    addContentItemAssociationTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemAssociationValidationException))),
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
        public async Task ShouldThrowValidationExceptionOnAddIfContentItemAssociationIsInvalidAndLogItAsync(
            string invalidText)
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            DateTimeOffset startDate = randomDateTimeOffset.AddSeconds(-90);
            DateTimeOffset endDate = randomDateTimeOffset;

            var invalidContentItemAssociation = new ContentItemAssociation
            {
                Id = Guid.Empty,
                LinkedEntityId = Guid.Empty,
                CreatedBy = invalidText,
                UpdatedBy = invalidText,
                CreatedWhen = default,
                UpdatedWhen = default
            };

            var invalidContentItemAssociationException =
                new InvalidContentItemAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidContentItemAssociationException.AddData(
                key: nameof(ContentItemAssociation.Id),
                values: "Id is required");

            invalidContentItemAssociationException.AddData(
                key: nameof(ContentItemAssociation.LinkedEntityId),
                values: "Id is required");

            invalidContentItemAssociationException.AddData(
                key: nameof(ContentItemAssociation.CreatedBy),
                values: "Text is required");

            invalidContentItemAssociationException.AddData(
                key: nameof(ContentItemAssociation.UpdatedBy),
                values: "Text is required");

            invalidContentItemAssociationException.AddData(
                key: nameof(ContentItemAssociation.CreatedWhen),
                values: new[]
                {
                    "Date is required",

                    "Date is not recent. Expected a value between " +
                        $"{startDate} and {endDate} but found {default(DateTimeOffset)}"
                });

            invalidContentItemAssociationException.AddData(
                key: nameof(ContentItemAssociation.UpdatedWhen),
                values: "Date is required");

            var expectedContentItemAssociationValidationException =
                new ContentItemAssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidText);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ContentItemAssociation> addContentItemAssociationTask =
                this.contentItemAssociationService.AddContentItemAssociationAsync(
                    invalidContentItemAssociation,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    addContentItemAssociationTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnAddIfContentItemAssociationTextExceedsMaxLengthAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomStringWithLengthOf(256);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();

            ContentItemAssociation invalidContentItemAssociation =
                CreateContentItemAssociationFiller(randomDateTimeOffset, randomUserId).Create();

            var invalidContentItemAssociationException =
                new InvalidContentItemAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidContentItemAssociationException.AddData(
                key: nameof(ContentItemAssociation.CreatedBy),
                values: $"Text exceed max length of {invalidContentItemAssociation.CreatedBy.Length - 1} characters");

            invalidContentItemAssociationException.AddData(
                key: nameof(ContentItemAssociation.UpdatedBy),
                values: $"Text exceed max length of {invalidContentItemAssociation.UpdatedBy.Length - 1} characters");

            var expectedContentItemAssociationValidationException =
                new ContentItemAssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ContentItemAssociation> addContentItemAssociationTask =
                this.contentItemAssociationService.AddContentItemAssociationAsync(
                    invalidContentItemAssociation,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    addContentItemAssociationTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemAssociationValidationException))),
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
            ContentItemAssociation randomContentItemAssociation =
                CreateContentItemAssociationFiller(randomDateTimeOffset, randomUserId).Create();
            ContentItemAssociation invalidContentItemAssociation = randomContentItemAssociation;
            invalidContentItemAssociation.UpdatedWhen = GetRandomDateTimeOffset();

            var invalidContentItemAssociationException =
                new InvalidContentItemAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidContentItemAssociationException.AddData(
                key: nameof(ContentItemAssociation.UpdatedWhen),
                values: $"Date is not the same as {nameof(ContentItemAssociation.CreatedWhen)}");

            var expectedContentItemAssociationValidationException =
                new ContentItemAssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ContentItemAssociation> addContentItemAssociationTask =
                this.contentItemAssociationService.AddContentItemAssociationAsync(
                    invalidContentItemAssociation,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    addContentItemAssociationTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemAssociationValidationException))),
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
            ContentItemAssociation randomContentItemAssociation =
                CreateContentItemAssociationFiller(randomDateTimeOffset, randomUserId).Create();
            ContentItemAssociation invalidContentItemAssociation = randomContentItemAssociation;
            invalidContentItemAssociation.CreatedBy = differentUserId;
            invalidContentItemAssociation.UpdatedBy = differentUserId;

            var invalidContentItemAssociationException =
                new InvalidContentItemAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidContentItemAssociationException.AddData(
                key: nameof(ContentItemAssociation.CreatedBy),
                values: $"Expected value to be '{randomUserId}' but found '{differentUserId}'.");

            var expectedContentItemAssociationValidationException =
                new ContentItemAssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ContentItemAssociation> addContentItemAssociationTask =
                this.contentItemAssociationService.AddContentItemAssociationAsync(
                    invalidContentItemAssociation,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    addContentItemAssociationTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemAssociationValidationException))),
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
            ContentItemAssociation randomContentItemAssociation =
                CreateContentItemAssociationFiller(randomDateTimeOffset, randomUserId).Create();
            ContentItemAssociation invalidContentItemAssociation = randomContentItemAssociation;
            invalidContentItemAssociation.UpdatedBy = GetRandomString();

            var invalidContentItemAssociationException =
                new InvalidContentItemAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidContentItemAssociationException.AddData(
                key: nameof(ContentItemAssociation.UpdatedBy),
                values: $"Text is not the same as {nameof(ContentItemAssociation.CreatedBy)}");

            var expectedContentItemAssociationValidationException =
                new ContentItemAssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ContentItemAssociation> addContentItemAssociationTask =
                this.contentItemAssociationService.AddContentItemAssociationAsync(
                    invalidContentItemAssociation,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    addContentItemAssociationTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemAssociationValidationException))),
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
            ContentItemAssociation randomContentItemAssociation =
                CreateContentItemAssociationFiller(randomDateTimeOffset, randomUserId).Create();
            ContentItemAssociation invalidContentItemAssociation = randomContentItemAssociation;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidContentItemAssociation.CreatedWhen = randomDateTimeOffset.AddMinutes(minutes);
            invalidContentItemAssociation.UpdatedWhen = invalidContentItemAssociation.CreatedWhen;

            var invalidContentItemAssociationException =
                new InvalidContentItemAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidContentItemAssociationException.AddData(
                key: nameof(ContentItemAssociation.CreatedWhen),
                values: $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                    $"but found {invalidContentItemAssociation.CreatedWhen}");

            var expectedContentItemAssociationValidationException =
                new ContentItemAssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyAddAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<ContentItemAssociation> addContentItemAssociationTask =
                this.contentItemAssociationService.AddContentItemAssociationAsync(
                    invalidContentItemAssociation,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    addContentItemAssociationTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyAddAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
