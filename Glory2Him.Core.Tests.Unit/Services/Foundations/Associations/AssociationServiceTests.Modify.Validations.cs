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
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Associations
{
    public partial class AssociationServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfAssociationIsNullAndLogItAsync()
        {
            // given
            Association nullAssociation = null;

            var nullAssociationException =
                new NullAssociationException(message: "Content item association is null.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: nullAssociationException);

            // when
            ValueTask<Association> modifyAssociationTask =
                this.associationService.ModifyAssociationAsync(
                    nullAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    modifyAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfAssociationIsInvalidAndLogItAsync(
            string invalidText)
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            DateTimeOffset startDate = randomDateTimeOffset.AddSeconds(-90);
            DateTimeOffset endDate = randomDateTimeOffset;

            var invalidAssociation = new Association
            {
                Id = Guid.Empty,
                LinkedEntityId = Guid.Empty,
                CreatedBy = invalidText,
                UpdatedBy = invalidText,
                CreatedWhen = default,
                UpdatedWhen = default
            };

            var invalidAssociationException =
                new InvalidAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.AddData(
                key: nameof(Association.Id),
                values: "Id is required");

            invalidAssociationException.AddData(
                key: nameof(Association.LinkedEntityId),
                values: "Id is required");

            invalidAssociationException.AddData(
                key: nameof(Association.CreatedBy),
                values: "Text is required");

            invalidAssociationException.AddData(
                key: nameof(Association.UpdatedBy),
                values: "Text is required");

            invalidAssociationException.AddData(
                key: nameof(Association.CreatedWhen),
                values: "Date is required");

            invalidAssociationException.AddData(
                key: nameof(Association.UpdatedWhen),
                values: new[]
                {
                    "Date is required",
                    "Date is the same as CreatedWhen",

                    "Date is not recent. Expected a value between " +
                        $"{startDate} and {endDate} but found {default(DateTimeOffset)}"
                });

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidText);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Association> modifyAssociationTask =
                this.associationService.ModifyAssociationAsync(
                    invalidAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    modifyAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfAssociationNotFoundAndLogItAsync()
        {
            // given
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            Association randomAssociation =
                CreateRandomModifyAssociation(randomDateTimeOffset, randomUserId);
            Association nonExistentAssociation = randomAssociation;
            Association noAssociation = null;

            var notFoundAssociationException = new NotFoundAssociationException(
                message: $"Content item association not found with id: {nonExistentAssociation.Id}.");

            var expectedAssociationValidationException = new AssociationValidationException(
                message: "Content item association validation error occurred, fix the errors and try again.",
                innerException: notFoundAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(nonExistentAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    nonExistentAssociation.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noAssociation);

            // when
            ValueTask<Association> modifyAssociationTask =
                this.associationService.ModifyAssociationAsync(
                    nonExistentAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    modifyAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    nonExistentAssociation.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
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
            Association randomAssociation =
                CreateRandomModifyAssociation(randomDateTimeOffset, randomUserId);
            Association invalidAssociation = randomAssociation;
            Association storageAssociation = randomAssociation.DeepClone();
            storageAssociation.CreatedWhen = GetRandomDateTimeOffset();
            storageAssociation.UpdatedWhen = GetRandomDateTimeOffset();

            var invalidAssociationException = new InvalidAssociationException(
                message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.AddData(
                key: nameof(Association.CreatedWhen),
                values: $"Date is not the same as {nameof(Association.CreatedWhen)}");

            var expectedAssociationValidationException = new AssociationValidationException(
                message: "Content item association validation error occurred, fix the errors and try again.",
                innerException: invalidAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    invalidAssociation.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidAssociation,
                    storageAssociation))
                        .ReturnsAsync(invalidAssociation);

            // when
            ValueTask<Association> modifyAssociationTask =
                this.associationService.ModifyAssociationAsync(
                    invalidAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    modifyAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    invalidAssociation.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidAssociation,
                    storageAssociation),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
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
            Association randomAssociation =
                CreateRandomModifyAssociation(randomDateTimeOffset, randomUserId);
            Association invalidAssociation = randomAssociation;
            Association storageAssociation = randomAssociation.DeepClone();
            storageAssociation.CreatedBy = GetRandomString();
            storageAssociation.UpdatedWhen =
                storageAssociation.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var invalidAssociationException =
                new InvalidAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.AddData(
                key: nameof(Association.CreatedBy),
                values: $"Text is not the same as {nameof(Association.CreatedBy)}");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    invalidAssociation.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidAssociation,
                    storageAssociation))
                        .ReturnsAsync(invalidAssociation);

            // when
            ValueTask<Association> modifyAssociationTask =
                this.associationService.ModifyAssociationAsync(
                    invalidAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    modifyAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    invalidAssociation.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidAssociation,
                    storageAssociation),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
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
            Association randomAssociation =
                CreateRandomModifyAssociation(randomDateTimeOffset, randomUserId);
            Association invalidAssociation = randomAssociation;
            Association storageAssociation = randomAssociation.DeepClone();

            var invalidAssociationException =
                new InvalidAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.AddData(
                key: nameof(Association.UpdatedWhen),
                values: $"Date is the same as {nameof(Association.UpdatedWhen)}");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    invalidAssociation.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidAssociation,
                    storageAssociation))
                        .ReturnsAsync(invalidAssociation);

            // when
            ValueTask<Association> modifyAssociationTask =
                this.associationService.ModifyAssociationAsync(
                    invalidAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    modifyAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    invalidAssociation.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidAssociation,
                    storageAssociation),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
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
            Association randomAssociation =
                CreateRandomModifyAssociation(randomDateTimeOffset, randomUserId);
            Association invalidAssociation = randomAssociation;
            invalidAssociation.UpdatedBy = differentUserId;

            var invalidAssociationException =
                new InvalidAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.AddData(
                key: nameof(Association.UpdatedBy),
                values: $"Expected value to be '{randomUserId}' but found '{differentUserId}'.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Association> modifyAssociationTask =
                this.associationService.ModifyAssociationAsync(
                    invalidAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    modifyAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
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
            Association randomAssociation =
                CreateRandomModifyAssociation(randomDateTimeOffset, randomUserId);
            Association invalidAssociation = randomAssociation;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidAssociation.UpdatedWhen = invalidAssociation.CreatedWhen;

            var invalidAssociationException =
                new InvalidAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.AddData(
                key: nameof(Association.UpdatedWhen),
                values: new[]
                {
                    $"Date is the same as {nameof(Association.CreatedWhen)}",
                    $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                        $"but found {invalidAssociation.UpdatedWhen}"
                });

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Association> modifyAssociationTask =
                this.associationService.ModifyAssociationAsync(
                    invalidAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    modifyAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
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
            Association randomAssociation =
                CreateRandomModifyAssociation(randomDateTimeOffset, randomUserId);
            Association invalidAssociation = randomAssociation;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidAssociation.UpdatedWhen = randomDateTimeOffset.AddMinutes(minutes);

            var invalidAssociationException =
                new InvalidAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.AddData(
                key: nameof(Association.UpdatedWhen),
                values: $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                    $"but found {invalidAssociation.UpdatedWhen}");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<Association> modifyAssociationTask =
                this.associationService.ModifyAssociationAsync(
                    invalidAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    modifyAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfAssociationExceedsMaxLengthAndLogItAsync()
        {
            // given
            string randomUserId = GetRandomStringWithLengthOf(256);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            Association invalidAssociation =
                CreateRandomModifyAssociation(randomDateTimeOffset, randomUserId);

            var invalidAssociationException =
                new InvalidAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.AddData(
                key: nameof(Association.CreatedBy),
                values: $"Text exceed max length of {invalidAssociation.CreatedBy.Length - 1} characters");

            invalidAssociationException.AddData(
                key: nameof(Association.UpdatedBy),
                values: $"Text exceed max length of {invalidAssociation.UpdatedBy.Length - 1} characters");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Association> modifyAssociationTask =
                this.associationService.ModifyAssociationAsync(
                    invalidAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    modifyAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
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
            Association invalidAssociation =
                CreateRandomModifyAssociation(randomDateTimeOffset, randomUserId);

            invalidAssociation.AssociationConfidenceReason = GetRandomStringWithLengthOf(501);

            var invalidAssociationException =
                new InvalidAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.AddData(
                key: nameof(Association.AssociationConfidenceReason),

                values: "Text exceed max length of " +
                    $"{invalidAssociation.AssociationConfidenceReason.Length - 1} characters");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Association> modifyAssociationTask =
                this.associationService.ModifyAssociationAsync(
                    invalidAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    modifyAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
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
            Association invalidAssociation =
                CreateRandomModifyAssociation(randomDateTimeOffset, randomUserId);

            invalidAssociation.AssociationConfidenceScore = outOfRangeConfidenceScore;

            var invalidAssociationException =
                new InvalidAssociationException(
                    message: "Content item association is invalid, fix the errors and try again.");

            invalidAssociationException.AddData(
                key: nameof(Association.AssociationConfidenceScore),
                values: "Value is not within range of 0 and 10");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: invalidAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<Association> modifyAssociationTask =
                this.associationService.ModifyAssociationAsync(
                    invalidAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    modifyAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
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
            Association someAssociation = CreateRandomAssociation();

            var unauthorizedAssociationException = new UnauthorizedAssociationException(
                message: "The current user is not authenticated.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedAssociationException);

            // when
            ValueTask<Association> modifyAssociationTask =
                this.associationService.ModifyAssociationAsync(
                    someAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    modifyAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfUserIsBlockedFromContributingAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.ReadOnly);
            Association someAssociation = CreateRandomAssociation();

            var unauthorizedAssociationException = new UnauthorizedAssociationException(
                message: "The current user is blocked from contributing content item associations.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedAssociationException);

            // when
            ValueTask<Association> modifyAssociationTask =
                this.associationService.ModifyAssociationAsync(
                    someAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    modifyAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
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

            Association randomAssociation =
                CreateRandomModifyAssociation(randomDateTimeOffset, randomUserId);

            Association inputAssociation = randomAssociation;
            Association storageAssociation = randomAssociation.DeepClone();
            storageAssociation.CreatedBy = GetRandomString();

            storageAssociation.UpdatedWhen =
                storageAssociation.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var unauthorizedAssociationException = new UnauthorizedAssociationException(
                message: "The current user is not allowed to modify this content item association.");

            var expectedAssociationValidationException =
                new AssociationValidationException(
                    message: "Content item association validation error occurred, fix the errors and try again.",
                    innerException: unauthorizedAssociationException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(inputAssociation, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(inputAssociation);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectAssociationByIdAsync(
                    inputAssociation.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageAssociation);

            // when
            ValueTask<Association> modifyAssociationTask =
                this.associationService.ModifyAssociationAsync(
                    inputAssociation,
                    TestContext.Current.CancellationToken);

            AssociationValidationException actualAssociationValidationException =
                await Assert.ThrowsAsync<AssociationValidationException>(
                    modifyAssociationTask.AsTask);

            // then
            actualAssociationValidationException.Should().BeEquivalentTo(
                expectedAssociationValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(inputAssociation, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Exactly(2));

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAssociationByIdAsync(
                    inputAssociation.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.UpdateAssociationAsync(
                    It.IsAny<Association>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedAssociationValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}