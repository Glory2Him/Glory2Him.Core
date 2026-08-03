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
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles;
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ApprovalSettingReviewerRoles
{
    public partial class ApprovalSettingReviewerRoleServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfApprovalSettingReviewerRoleIsNullAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            ApprovalSettingReviewerRole nullApprovalSettingReviewerRole = null;

            var nullApprovalSettingReviewerRoleException =
                new NullApprovalSettingReviewerRoleException(message: "Approval setting reviewer role is null.");

            var expectedApprovalSettingReviewerRoleValidationException =
                new ApprovalSettingReviewerRoleValidationException(
                    message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                    innerException: nullApprovalSettingReviewerRoleException);

            // when
            ValueTask<ApprovalSettingReviewerRole> modifyApprovalSettingReviewerRoleTask =
                this.approvalSettingReviewerRoleService.ModifyApprovalSettingReviewerRoleAsync(
                    nullApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    modifyApprovalSettingReviewerRoleTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleValidationException))),
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
        public async Task ShouldThrowValidationExceptionOnModifyIfApprovalSettingReviewerRoleIsInvalidAndLogItAsync(
            string invalidText)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            DateTimeOffset startDate = randomDateTimeOffset.AddSeconds(-90);
            DateTimeOffset endDate = randomDateTimeOffset;

            var invalidApprovalSettingReviewerRole = new ApprovalSettingReviewerRole
            {
                Id = Guid.Empty,
                ApprovalSettingId = Guid.Empty,
                RoleName = invalidText,
                CreatedBy = invalidText,
                UpdatedBy = invalidText,
                CreatedWhen = default,
                UpdatedWhen = default
            };

            var invalidApprovalSettingReviewerRoleException =
                new InvalidApprovalSettingReviewerRoleException(
                    message: "Approval setting reviewer role is invalid, fix the errors and try again.");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.Id),
                values: "Id is required");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.RoleName),
                values: "Text is required");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.ApprovalSettingId),
                values: "Id is required");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.CreatedBy),
                values: "Text is required");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.UpdatedBy),
                values: "Text is required");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.CreatedWhen),
                values: "Date is required");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.UpdatedWhen),
                values: new[]
                {
                    "Date is required",
                    "Date is the same as CreatedWhen",

                    "Date is not recent. Expected a value between " +
                        $"{startDate} and {endDate} but found {default(DateTimeOffset)}"
                });

            var expectedApprovalSettingReviewerRoleValidationException =
                new ApprovalSettingReviewerRoleValidationException(
                    message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingReviewerRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSettingReviewerRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidText);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalSettingReviewerRole> modifyApprovalSettingReviewerRoleTask =
                this.approvalSettingReviewerRoleService.ModifyApprovalSettingReviewerRoleAsync(
                    invalidApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    modifyApprovalSettingReviewerRoleTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfApprovalSettingReviewerRoleNotFoundAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            ApprovalSettingReviewerRole randomApprovalSettingReviewerRole = CreateRandomModifyApprovalSettingReviewerRole(randomDateTimeOffset, randomUserId);
            ApprovalSettingReviewerRole nonExistentApprovalSettingReviewerRole = randomApprovalSettingReviewerRole;
            ApprovalSettingReviewerRole noApprovalSettingReviewerRole = null;

            var notFoundApprovalSettingReviewerRoleException = new NotFoundApprovalSettingReviewerRoleException(
                message: $"Approval setting reviewer role not found with id: {nonExistentApprovalSettingReviewerRole.Id}.");

            var expectedApprovalSettingReviewerRoleValidationException = new ApprovalSettingReviewerRoleValidationException(
                message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                innerException: notFoundApprovalSettingReviewerRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentApprovalSettingReviewerRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(nonExistentApprovalSettingReviewerRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    nonExistentApprovalSettingReviewerRole.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noApprovalSettingReviewerRole);

            // when
            ValueTask<ApprovalSettingReviewerRole> modifyApprovalSettingReviewerRoleTask =
                this.approvalSettingReviewerRoleService.ModifyApprovalSettingReviewerRoleAsync(
                    nonExistentApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    modifyApprovalSettingReviewerRoleTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(nonExistentApprovalSettingReviewerRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    nonExistentApprovalSettingReviewerRole.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleValidationException))),
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            ApprovalSettingReviewerRole randomApprovalSettingReviewerRole = CreateRandomModifyApprovalSettingReviewerRole(randomDateTimeOffset, randomUserId);
            ApprovalSettingReviewerRole invalidApprovalSettingReviewerRole = randomApprovalSettingReviewerRole;
            ApprovalSettingReviewerRole storageApprovalSettingReviewerRole = randomApprovalSettingReviewerRole.DeepClone();
            storageApprovalSettingReviewerRole.CreatedWhen = GetRandomDateTimeOffset();
            storageApprovalSettingReviewerRole.UpdatedWhen = GetRandomDateTimeOffset();

            var invalidApprovalSettingReviewerRoleException = new InvalidApprovalSettingReviewerRoleException(
                message: "Approval setting reviewer role is invalid, fix the errors and try again.");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.CreatedWhen),
                values: $"Date is not the same as {nameof(ApprovalSettingReviewerRole.CreatedWhen)}");

            var expectedApprovalSettingReviewerRoleValidationException = new ApprovalSettingReviewerRoleValidationException(
                message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                innerException: invalidApprovalSettingReviewerRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSettingReviewerRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    invalidApprovalSettingReviewerRole.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalSettingReviewerRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidApprovalSettingReviewerRole,
                    storageApprovalSettingReviewerRole))
                        .ReturnsAsync(invalidApprovalSettingReviewerRole);

            // when
            ValueTask<ApprovalSettingReviewerRole> modifyApprovalSettingReviewerRoleTask =
                this.approvalSettingReviewerRoleService.ModifyApprovalSettingReviewerRoleAsync(
                    invalidApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    modifyApprovalSettingReviewerRoleTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    invalidApprovalSettingReviewerRole.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidApprovalSettingReviewerRole,
                    storageApprovalSettingReviewerRole),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleValidationException))),
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            ApprovalSettingReviewerRole randomApprovalSettingReviewerRole = CreateRandomModifyApprovalSettingReviewerRole(randomDateTimeOffset, randomUserId);
            ApprovalSettingReviewerRole invalidApprovalSettingReviewerRole = randomApprovalSettingReviewerRole;
            ApprovalSettingReviewerRole storageApprovalSettingReviewerRole = randomApprovalSettingReviewerRole.DeepClone();
            storageApprovalSettingReviewerRole.CreatedBy = GetRandomString();
            storageApprovalSettingReviewerRole.UpdatedWhen = storageApprovalSettingReviewerRole.UpdatedWhen.AddDays(GetRandomNegativeNumber());

            var invalidApprovalSettingReviewerRoleException =
                new InvalidApprovalSettingReviewerRoleException(
                    message: "Approval setting reviewer role is invalid, fix the errors and try again.");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.CreatedBy),
                values: $"Text is not the same as {nameof(ApprovalSettingReviewerRole.CreatedBy)}");

            var expectedApprovalSettingReviewerRoleValidationException =
                new ApprovalSettingReviewerRoleValidationException(
                    message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingReviewerRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSettingReviewerRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    invalidApprovalSettingReviewerRole.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalSettingReviewerRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidApprovalSettingReviewerRole,
                    storageApprovalSettingReviewerRole))
                        .ReturnsAsync(invalidApprovalSettingReviewerRole);

            // when
            ValueTask<ApprovalSettingReviewerRole> modifyApprovalSettingReviewerRoleTask =
                this.approvalSettingReviewerRoleService.ModifyApprovalSettingReviewerRoleAsync(
                    invalidApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    modifyApprovalSettingReviewerRoleTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    invalidApprovalSettingReviewerRole.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidApprovalSettingReviewerRole,
                    storageApprovalSettingReviewerRole),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleValidationException))),
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            string randomUserId = GetRandomString();
            ApprovalSettingReviewerRole randomApprovalSettingReviewerRole = CreateRandomModifyApprovalSettingReviewerRole(randomDateTimeOffset, randomUserId);
            ApprovalSettingReviewerRole invalidApprovalSettingReviewerRole = randomApprovalSettingReviewerRole;
            ApprovalSettingReviewerRole storageApprovalSettingReviewerRole = randomApprovalSettingReviewerRole.DeepClone();

            var invalidApprovalSettingReviewerRoleException =
                new InvalidApprovalSettingReviewerRoleException(
                    message: "Approval setting reviewer role is invalid, fix the errors and try again.");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.UpdatedWhen),
                values: $"Date is the same as {nameof(ApprovalSettingReviewerRole.UpdatedWhen)}");

            var expectedApprovalSettingReviewerRoleValidationException =
                new ApprovalSettingReviewerRoleValidationException(
                    message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingReviewerRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSettingReviewerRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    invalidApprovalSettingReviewerRole.Id,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageApprovalSettingReviewerRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidApprovalSettingReviewerRole,
                    storageApprovalSettingReviewerRole))
                        .ReturnsAsync(invalidApprovalSettingReviewerRole);

            // when
            ValueTask<ApprovalSettingReviewerRole> modifyApprovalSettingReviewerRoleTask =
                this.approvalSettingReviewerRoleService.ModifyApprovalSettingReviewerRoleAsync(
                    invalidApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    modifyApprovalSettingReviewerRoleTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectApprovalSettingReviewerRoleByIdAsync(
                    invalidApprovalSettingReviewerRole.Id,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync(
                    invalidApprovalSettingReviewerRole,
                    storageApprovalSettingReviewerRole),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleValidationException))),
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            string randomUserId = GetRandomString();
            string differentUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ApprovalSettingReviewerRole randomApprovalSettingReviewerRole = CreateRandomModifyApprovalSettingReviewerRole(randomDateTimeOffset, randomUserId);
            ApprovalSettingReviewerRole invalidApprovalSettingReviewerRole = randomApprovalSettingReviewerRole;
            invalidApprovalSettingReviewerRole.UpdatedBy = differentUserId;

            var invalidApprovalSettingReviewerRoleException =
                new InvalidApprovalSettingReviewerRoleException(
                    message: "Approval setting reviewer role is invalid, fix the errors and try again.");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.UpdatedBy),
                values: $"Expected value to be '{randomUserId}' but found '{differentUserId}'.");

            var expectedApprovalSettingReviewerRoleValidationException =
                new ApprovalSettingReviewerRoleValidationException(
                    message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingReviewerRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSettingReviewerRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalSettingReviewerRole> modifyApprovalSettingReviewerRoleTask =
                this.approvalSettingReviewerRoleService.ModifyApprovalSettingReviewerRoleAsync(
                    invalidApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    modifyApprovalSettingReviewerRoleTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleValidationException))),
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ApprovalSettingReviewerRole randomApprovalSettingReviewerRole = CreateRandomModifyApprovalSettingReviewerRole(randomDateTimeOffset, randomUserId);
            ApprovalSettingReviewerRole invalidApprovalSettingReviewerRole = randomApprovalSettingReviewerRole;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidApprovalSettingReviewerRole.UpdatedWhen = invalidApprovalSettingReviewerRole.CreatedWhen;

            var invalidApprovalSettingReviewerRoleException =
                new InvalidApprovalSettingReviewerRoleException(
                    message: "Approval setting reviewer role is invalid, fix the errors and try again.");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.UpdatedWhen),
                values: new[]
                {
                    $"Date is the same as {nameof(ApprovalSettingReviewerRole.CreatedWhen)}",
                    $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                        $"but found {invalidApprovalSettingReviewerRole.UpdatedWhen}"
                });

            var expectedApprovalSettingReviewerRoleValidationException =
                new ApprovalSettingReviewerRoleValidationException(
                    message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingReviewerRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSettingReviewerRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalSettingReviewerRole> modifyApprovalSettingReviewerRoleTask =
                this.approvalSettingReviewerRoleService.ModifyApprovalSettingReviewerRoleAsync(
                    invalidApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    modifyApprovalSettingReviewerRoleTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleValidationException))),
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
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            string randomUserId = GetRandomString();
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ApprovalSettingReviewerRole randomApprovalSettingReviewerRole = CreateRandomModifyApprovalSettingReviewerRole(randomDateTimeOffset, randomUserId);
            ApprovalSettingReviewerRole invalidApprovalSettingReviewerRole = randomApprovalSettingReviewerRole;
            DateTimeOffset currentDateTime = randomDateTimeOffset;
            DateTimeOffset startDate = currentDateTime.AddSeconds(-90);
            DateTimeOffset endDate = currentDateTime;
            invalidApprovalSettingReviewerRole.UpdatedWhen = randomDateTimeOffset.AddMinutes(minutes);

            var invalidApprovalSettingReviewerRoleException =
                new InvalidApprovalSettingReviewerRoleException(
                    message: "Approval setting reviewer role is invalid, fix the errors and try again.");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.UpdatedWhen),
                values: $"Date is not recent. Expected a value between {startDate} and {endDate} " +
                    $"but found {invalidApprovalSettingReviewerRole.UpdatedWhen}");

            var expectedApprovalSettingReviewerRoleValidationException =
                new ApprovalSettingReviewerRoleValidationException(
                    message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingReviewerRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSettingReviewerRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(currentDateTime);

            // when
            ValueTask<ApprovalSettingReviewerRole> modifyApprovalSettingReviewerRoleTask =
                this.approvalSettingReviewerRoleService.ModifyApprovalSettingReviewerRoleAsync(
                    invalidApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    modifyApprovalSettingReviewerRoleTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnModifyIfApprovalSettingReviewerRoleExceedsMaxLengthAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Admin);
            string randomUserId = GetRandomStringWithLengthOf(256);
            DateTimeOffset randomDateTimeOffset = GetRandomDateTimeOffset();
            ApprovalSettingReviewerRole invalidApprovalSettingReviewerRole = CreateRandomModifyApprovalSettingReviewerRole(randomDateTimeOffset, randomUserId);

            var invalidApprovalSettingReviewerRoleException =
                new InvalidApprovalSettingReviewerRoleException(
                    message: "Approval setting reviewer role is invalid, fix the errors and try again.");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.CreatedBy),
                values: $"Text exceed max length of {invalidApprovalSettingReviewerRole.CreatedBy.Length - 1} characters");

            invalidApprovalSettingReviewerRoleException.AddData(
                key: nameof(ApprovalSettingReviewerRole.UpdatedBy),
                values: $"Text exceed max length of {invalidApprovalSettingReviewerRole.UpdatedBy.Length - 1} characters");

            var expectedApprovalSettingReviewerRoleValidationException =
                new ApprovalSettingReviewerRoleValidationException(
                    message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                    innerException: invalidApprovalSettingReviewerRoleException);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()))
                    .ReturnsAsync(invalidApprovalSettingReviewerRole);

            this.securityAuditBrokerMock.Setup(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()))
                    .ReturnsAsync(randomUserId);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(randomDateTimeOffset);

            // when
            ValueTask<ApprovalSettingReviewerRole> modifyApprovalSettingReviewerRoleTask =
                this.approvalSettingReviewerRoleService.ModifyApprovalSettingReviewerRoleAsync(
                    invalidApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    modifyApprovalSettingReviewerRoleTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.ApplyModifyAuditValuesAsync(invalidApprovalSettingReviewerRole, It.IsAny<SecurityContext>()),
                Times.Once);

            this.securityAuditBrokerMock.Verify(broker =>
                broker.GetUserIdAsync(It.IsAny<SecurityContext>()),
                Times.Once);

            this.dateTimeBrokerMock.Verify(broker =>
                broker.GetCurrentDateTimeOffsetAsync(),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleValidationException))),
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
            ApprovalSettingReviewerRole someApprovalSettingReviewerRole = CreateRandomApprovalSettingReviewerRole();

            var unauthorizedApprovalSettingReviewerRoleException = new UnauthorizedApprovalSettingReviewerRoleException(
                message: "The current user is not authenticated.");

            var expectedApprovalSettingReviewerRoleValidationException = new ApprovalSettingReviewerRoleValidationException(
                message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalSettingReviewerRoleException);

            // when
            ValueTask<ApprovalSettingReviewerRole> modifyApprovalSettingReviewerRoleTask =
                this.approvalSettingReviewerRoleService.ModifyApprovalSettingReviewerRoleAsync(
                    someApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    modifyApprovalSettingReviewerRoleTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(NonAdminRoleSets))]
        public async Task ShouldThrowValidationExceptionOnModifyIfUserIsNotAdminAndLogItAsync(
            string[] nonAdminRoles)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(nonAdminRoles);
            ApprovalSettingReviewerRole someApprovalSettingReviewerRole = CreateRandomApprovalSettingReviewerRole();

            var unauthorizedApprovalSettingReviewerRoleException = new UnauthorizedApprovalSettingReviewerRoleException(
                message: "The current user is not allowed to administer approval setting reviewer roles.");

            var expectedApprovalSettingReviewerRoleValidationException = new ApprovalSettingReviewerRoleValidationException(
                message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                innerException: unauthorizedApprovalSettingReviewerRoleException);

            // when
            ValueTask<ApprovalSettingReviewerRole> modifyApprovalSettingReviewerRoleTask =
                this.approvalSettingReviewerRoleService.ModifyApprovalSettingReviewerRoleAsync(
                    someApprovalSettingReviewerRole,
                    TestContext.Current.CancellationToken);

            ApprovalSettingReviewerRoleValidationException actualApprovalSettingReviewerRoleValidationException =
                await Assert.ThrowsAsync<ApprovalSettingReviewerRoleValidationException>(
                    modifyApprovalSettingReviewerRoleTask.AsTask);

            // then
            actualApprovalSettingReviewerRoleValidationException.Should().BeEquivalentTo(
                expectedApprovalSettingReviewerRoleValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedApprovalSettingReviewerRoleValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
