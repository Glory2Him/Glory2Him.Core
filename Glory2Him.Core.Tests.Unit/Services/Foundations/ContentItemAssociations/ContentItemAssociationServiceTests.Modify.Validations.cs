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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentItemAssociations;
using Glory2Him.Core.Models.Foundations.ContentItemAssociations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItemAssociations
{
    public partial class ContentItemAssociationServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfContentItemAssociationIsNullAndLogItAsync()
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
            ValueTask<ContentItemAssociation> modifyContentItemAssociationTask =
                this.contentItemAssociationService.ModifyContentItemAssociationAsync(
                    nullContentItemAssociation,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    modifyContentItemAssociationTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnModifyIfContentItemAssociationIsInvalidAndLogItAsync(
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
                values: "Date is required");

            invalidContentItemAssociationException.AddData(
                key: nameof(ContentItemAssociation.UpdatedWhen),
                values: new[]
                {
                    "Date is required",
                    "Date is the same as CreatedWhen",

                    "Date is not recent. Expected a value between " +
                        $"{startDate} and {endDate} but found {default(DateTimeOffset)}"
                });

            var expectedContentItemAssociationValidationException =
                new ContentItemAssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidText);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ContentItemAssociation> modifyContentItemAssociationTask =
                this.contentItemAssociationService.ModifyContentItemAssociationAsync(
                    invalidContentItemAssociation,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    modifyContentItemAssociationTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfContentItemAssociationNotFoundAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            ContentItemAssociation randomContentItemAssociation =
                CreateRandomModifyContentItemAssociation(randomDateTimeOffset, randomUserId);
            ContentItemAssociation nonExistentContentItemAssociation = randomContentItemAssociation;
            ContentItemAssociation noContentItemAssociation = null;

            var notFoundContentItemAssociationException = new NotFoundContentItemAssociationException(
                message: $"Content item association not found with id: {nonExistentContentItemAssociation.Id}.");

            var expectedContentItemAssociationValidationException = new ContentItemAssociationValidationException(
                message: "Content item association validation error occurred, fix the errors and try again.",
                innerException: notFoundContentItemAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(nonExistentContentItemAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    nonExistentContentItemAssociation.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noContentItemAssociation);

            // when
            ValueTask<ContentItemAssociation> modifyContentItemAssociationTask =
                this.contentItemAssociationService.ModifyContentItemAssociationAsync(
                    nonExistentContentItemAssociation,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    modifyContentItemAssociationTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentContentItemAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    nonExistentContentItemAssociation.Id,
                    TestContext.Current.CancellationToken),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfStorageCreatedWhenNotSameAsInputAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            ContentItemAssociation randomContentItemAssociation =
                CreateRandomModifyContentItemAssociation(randomDateTimeOffset, randomUserId);
            ContentItemAssociation invalidContentItemAssociation = randomContentItemAssociation;
            ContentItemAssociation storageContentItemAssociation = randomContentItemAssociation.DeepClone();
            storageContentItemAssociation.CreatedWhen = GetRandomDateTimeOffset();
            storageContentItemAssociation.UpdatedWhen = GetRandomDateTimeOffset();

            var invalidContentItemAssociationException = new InvalidContentItemAssociationException(
                message: "Content item association is invalid, fix the errors and try again.");

            invalidContentItemAssociationException.AddData(
                key: nameof(ContentItemAssociation.CreatedWhen),
                values: $"Date is not the same as {nameof(ContentItemAssociation.CreatedWhen)}");

            var expectedContentItemAssociationValidationException = new ContentItemAssociationValidationException(
                message: "Content item association validation error occurred, fix the errors and try again.",
                innerException: invalidContentItemAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    invalidContentItemAssociation.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItemAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentItemAssociation,
                    storageContentItemAssociation))
                        .ReturnsAsync(invalidContentItemAssociation);

            // when
            ValueTask<ContentItemAssociation> modifyContentItemAssociationTask =
                this.contentItemAssociationService.ModifyContentItemAssociationAsync(
                    invalidContentItemAssociation,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    modifyContentItemAssociationTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    invalidContentItemAssociation.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentItemAssociation,
                    storageContentItemAssociation),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfStorageCreatedByNotSameAsInputAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Reviewer);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            ContentItemAssociation randomContentItemAssociation =
                CreateRandomModifyContentItemAssociation(randomDateTimeOffset, randomUserId);
            ContentItemAssociation invalidContentItemAssociation = randomContentItemAssociation;
            ContentItemAssociation storageContentItemAssociation = randomContentItemAssociation.DeepClone();
            storageContentItemAssociation.CreatedBy = GetRandomString();
            storageContentItemAssociation.UpdatedWhen =
                storageContentItemAssociation.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var invalidContentItemAssociationException =
                new InvalidContentItemAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidContentItemAssociationException.AddData(
                key: nameof(ContentItemAssociation.CreatedBy),
                values: $"Text is not the same as {nameof(ContentItemAssociation.CreatedBy)}");

            var expectedContentItemAssociationValidationException =
                new ContentItemAssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    invalidContentItemAssociation.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItemAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentItemAssociation,
                    storageContentItemAssociation))
                        .ReturnsAsync(invalidContentItemAssociation);

            // when
            ValueTask<ContentItemAssociation> modifyContentItemAssociationTask =
                this.contentItemAssociationService.ModifyContentItemAssociationAsync(
                    invalidContentItemAssociation,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    modifyContentItemAssociationTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    invalidContentItemAssociation.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentItemAssociation,
                    storageContentItemAssociation),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfStorageUpdatedWhenSameAsInputAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            ContentItemAssociation randomContentItemAssociation =
                CreateRandomModifyContentItemAssociation(randomDateTimeOffset, randomUserId);
            ContentItemAssociation invalidContentItemAssociation = randomContentItemAssociation;
            ContentItemAssociation storageContentItemAssociation = randomContentItemAssociation.DeepClone();

            var invalidContentItemAssociationException =
                new InvalidContentItemAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidContentItemAssociationException.AddData(
                key: nameof(ContentItemAssociation.UpdatedWhen),
                values: $"Date is the same as {nameof(ContentItemAssociation.UpdatedWhen)}");

            var expectedContentItemAssociationValidationException =
                new ContentItemAssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    invalidContentItemAssociation.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItemAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentItemAssociation,
                    storageContentItemAssociation))
                        .ReturnsAsync(invalidContentItemAssociation);

            // when
            ValueTask<ContentItemAssociation> modifyContentItemAssociationTask =
                this.contentItemAssociationService.ModifyContentItemAssociationAsync(
                    invalidContentItemAssociation,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    modifyContentItemAssociationTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    invalidContentItemAssociation.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidContentItemAssociation,
                    storageContentItemAssociation),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfUpdatedByIsNotSameAsCurrentUserIdAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomString();
            string differentUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ContentItemAssociation randomContentItemAssociation =
                CreateRandomModifyContentItemAssociation(randomDateTimeOffset, randomUserId);
            ContentItemAssociation invalidContentItemAssociation = randomContentItemAssociation;
            invalidContentItemAssociation.UpdatedBy = differentUserId;

            var invalidContentItemAssociationException =
                new InvalidContentItemAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidContentItemAssociationException.AddData(
                key: nameof(ContentItemAssociation.UpdatedBy),
                values: $"Expected value to be '{randomUserId}' but found '{differentUserId}'.");

            var expectedContentItemAssociationValidationException =
                new ContentItemAssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ContentItemAssociation> modifyContentItemAssociationTask =
                this.contentItemAssociationService.ModifyContentItemAssociationAsync(
                    invalidContentItemAssociation,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    modifyContentItemAssociationTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfUpdatedWhenIsSameAsCreatedWhenAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ContentItemAssociation randomContentItemAssociation =
                CreateRandomModifyContentItemAssociation(randomDateTimeOffset, randomUserId);
            ContentItemAssociation invalidContentItemAssociation = randomContentItemAssociation;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidContentItemAssociation.UpdatedWhen = invalidContentItemAssociation.CreatedWhen;

            var invalidContentItemAssociationException =
                new InvalidContentItemAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidContentItemAssociationException.AddData(
                key: nameof(ContentItemAssociation.UpdatedWhen),
                values: new[]
                {
                    $"Date is the same as {nameof(ContentItemAssociation.CreatedWhen)}",
                    $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                        $"but found {invalidContentItemAssociation.UpdatedWhen}"
                });

            var expectedContentItemAssociationValidationException =
                new ContentItemAssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ContentItemAssociation> modifyContentItemAssociationTask =
                this.contentItemAssociationService.ModifyContentItemAssociationAsync(
                    invalidContentItemAssociation,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    modifyContentItemAssociationTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfUpdatedWhenIsNotRecentAndLogItAsync(int minutes)
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ContentItemAssociation randomContentItemAssociation =
                CreateRandomModifyContentItemAssociation(randomDateTimeOffset, randomUserId);
            ContentItemAssociation invalidContentItemAssociation = randomContentItemAssociation;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidContentItemAssociation.UpdatedWhen = randomDateTimeOffset.AddMinutes(minutes);

            var invalidContentItemAssociationException =
                new InvalidContentItemAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidContentItemAssociationException.AddData(
                key: nameof(ContentItemAssociation.UpdatedWhen),
                values: $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                    $"but found {invalidContentItemAssociation.UpdatedWhen}");

            var expectedContentItemAssociationValidationException =
                new ContentItemAssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<ContentItemAssociation> modifyContentItemAssociationTask =
                this.contentItemAssociationService.ModifyContentItemAssociationAsync(
                    invalidContentItemAssociation,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    modifyContentItemAssociationTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfContentItemAssociationExceedsMaxLengthAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomStringWithLengthOf(256);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ContentItemAssociation invalidContentItemAssociation =
                CreateRandomModifyContentItemAssociation(randomDateTimeOffset, randomUserId);

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
                broker.ApplyModifyAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ContentItemAssociation> modifyContentItemAssociationTask =
                this.contentItemAssociationService.ModifyContentItemAssociationAsync(
                    invalidContentItemAssociation,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    modifyContentItemAssociationTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()),
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
        public async Task
            ShouldThrowValidationExceptionOnModifyIfAssociationConfidenceReasonExceedsMaxLengthAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ContentItemAssociation invalidContentItemAssociation =
                CreateRandomModifyContentItemAssociation(randomDateTimeOffset, randomUserId);

            invalidContentItemAssociation.AssociationConfidenceReason = GetRandomStringWithLengthOf(501);

            var invalidContentItemAssociationException =
                new InvalidContentItemAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidContentItemAssociationException.AddData(
                key: nameof(ContentItemAssociation.AssociationConfidenceReason),

                values: "Text exceed max length of " +
                    $"{invalidContentItemAssociation.AssociationConfidenceReason.Length - 1} characters");

            var expectedContentItemAssociationValidationException =
                new ContentItemAssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ContentItemAssociation> modifyContentItemAssociationTask =
                this.contentItemAssociationService.ModifyContentItemAssociationAsync(
                    invalidContentItemAssociation,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    modifyContentItemAssociationTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()),
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
        [MemberData(nameof(OutOfRangeConfidenceScores))]
        public async Task ShouldThrowValidationExceptionOnModifyIfAssociationConfidenceScoreIsOutOfRangeAndLogItAsync(
            int outOfRangeConfidenceScore)
        {
            // given
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ContentItemAssociation invalidContentItemAssociation =
                CreateRandomModifyContentItemAssociation(randomDateTimeOffset, randomUserId);

            invalidContentItemAssociation.AssociationConfidenceScore = outOfRangeConfidenceScore;

            var invalidContentItemAssociationException =
                new InvalidContentItemAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidContentItemAssociationException.AddData(
                key: nameof(ContentItemAssociation.AssociationConfidenceScore),
                values: "Value is not within range of 0 and 10");

            var expectedContentItemAssociationValidationException =
                new ContentItemAssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidContentItemAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ContentItemAssociation> modifyContentItemAssociationTask =
                this.contentItemAssociationService.ModifyContentItemAssociationAsync(
                    invalidContentItemAssociation,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    modifyContentItemAssociationTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidContentItemAssociation, It.IsAny<SecurityContext>()),
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
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnModifyIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext invalidSecurityContext)
        {
            // given
            this.ambientSecurityContext = invalidSecurityContext;
            ContentItemAssociation someContentItemAssociation = CreateRandomContentItemAssociation();

            var unauthorizedContentItemAssociationException = new UnauthorizedContentItemAssociationException(
                message: "The current user is not authenticated.");

            var expectedContentItemAssociationValidationException =
                new ContentItemAssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedContentItemAssociationException);

            // when
            ValueTask<ContentItemAssociation> modifyContentItemAssociationTask =
                this.contentItemAssociationService.ModifyContentItemAssociationAsync(
                    someContentItemAssociation,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    modifyContentItemAssociationTask.AsTask);

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
        [InlineData(Roles.ReadOnly)]
        [InlineData(Roles.ContentItemAssociationReadOnly)]
        public async Task ShouldThrowValidationExceptionOnModifyIfUserIsBlockedFromContributingAndLogItAsync(
            string blockedRole)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(blockedRole);
            ContentItemAssociation someContentItemAssociation = CreateRandomContentItemAssociation();

            var unauthorizedContentItemAssociationException = new UnauthorizedContentItemAssociationException(
                message: "The current user is blocked from contributing content item associations.");

            var expectedContentItemAssociationValidationException =
                new ContentItemAssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedContentItemAssociationException);

            // when
            ValueTask<ContentItemAssociation> modifyContentItemAssociationTask =
                this.contentItemAssociationService.ModifyContentItemAssociationAsync(
                    someContentItemAssociation,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    modifyContentItemAssociationTask.AsTask);

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

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfUserIsNotOwnerAndHasNoReviewRoleAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();

            ContentItemAssociation randomContentItemAssociation =
                CreateRandomModifyContentItemAssociation(randomDateTimeOffset, randomUserId);

            ContentItemAssociation inputContentItemAssociation = randomContentItemAssociation;
            ContentItemAssociation storageContentItemAssociation = randomContentItemAssociation.DeepClone();
            storageContentItemAssociation.CreatedBy = GetRandomString();

            storageContentItemAssociation.UpdatedWhen =
                storageContentItemAssociation.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var unauthorizedContentItemAssociationException = new UnauthorizedContentItemAssociationException(
                message: "The current user is not allowed to modify this content item association.");

            var expectedContentItemAssociationValidationException =
                new ContentItemAssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedContentItemAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputContentItemAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputContentItemAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    inputContentItemAssociation.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItemAssociation);

            // when
            ValueTask<ContentItemAssociation> modifyContentItemAssociationTask =
                this.contentItemAssociationService.ModifyContentItemAssociationAsync(
                    inputContentItemAssociation,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationValidationException actualContentItemAssociationValidationException =
                await Assert.ThrowsAsync<ContentItemAssociationValidationException>(
                    modifyContentItemAssociationTask.AsTask);

            // then
            actualContentItemAssociationValidationException.Should().BeEquivalentTo(
                expectedContentItemAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(inputContentItemAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    inputContentItemAssociation.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateContentItemAssociationAsync(
                    It.IsAny<ContentItemAssociation>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

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