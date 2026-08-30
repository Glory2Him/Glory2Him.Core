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
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Foundations.ContentItemSettings.Exceptions;
using Glory2Him.Core.Models.Securities;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItemSettings
{
    public partial class ContentItemSettingServiceTests
    {
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            var invalidContentItemSettingId = Guid.Empty;

            var invalidContentItemSettingException = new InvalidContentItemSettingException(
                message: "Content item setting is invalid, fix the errors and try again.");

            invalidContentItemSettingException.UpsertDataList(
                key: "Id",
                value: "Id is required");

            var expectedContentItemSettingValidationException = new ContentItemSettingValidationException(
                message: "Content item setting validation error occurred, fix the errors and try again.",
                innerException: invalidContentItemSettingException);

            // when
            ValueTask<Glory2Him.Core.Models.Foundations.ContentItemSettings.ContentItemSetting>
                retrieveContentItemSettingByIdTask =
                this.contentItemSettingService.RetrieveContentItemSettingByIdAsync(
                    invalidContentItemSettingId,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    retrieveContentItemSettingByIdTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfContentItemSettingNotFoundAndLogItAsync()
        {
            // given
            Guid someContentItemSettingId = Guid.NewGuid();
            ContentItemSetting nullContentItemSetting = null;

            var notFoundContentItemSettingException =
                new NotFoundContentItemSettingException(
                    message: $"Content item setting not found with id: {someContentItemSettingId}.");

            var expectedContentItemSettingValidationException =
                new ContentItemSettingValidationException(
                    message: "Content item setting validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentItemSettingException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(nullContentItemSetting);

            // when
            ValueTask<ContentItemSetting> retrieveContentItemSettingByIdTask =
                this.contentItemSettingService.RetrieveContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    retrieveContentItemSettingByIdTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfContentItemSettingIsSoftDeletedAndLogItAsync()
        {
            // given: even an Admin caller gets not-found for a soft-deleted row —
            // deleted beats privilege
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            ContentItemSetting storageContentItemSetting = CreateRandomContentItemSetting();
            storageContentItemSetting.IsDeleted = true;
            Guid contentItemSettingId = storageContentItemSetting.Id;

            var notFoundContentItemSettingException =
                new NotFoundContentItemSettingException(
                    message: $"Content item setting not found with id: {contentItemSettingId}.");

            var expectedContentItemSettingValidationException =
                new ContentItemSettingValidationException(
                    message: "Content item setting validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentItemSettingException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    contentItemSettingId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItemSetting);

            // when
            ValueTask<ContentItemSetting> retrieveContentItemSettingByIdTask =
                this.contentItemSettingService.RetrieveContentItemSettingByIdAsync(
                    contentItemSettingId,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    retrieveContentItemSettingByIdTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    contentItemSettingId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogInformationAsync(
                    $"Content item setting read denied. Content item setting {contentItemSettingId} is " +
                        "soft-deleted; reported to the caller as not found."),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnRetrieveByIdIfSoftDeletedAndUserIsAnonymousAndLogItAsync(
            SecurityContext anonymousSecurityContext)
        {
            // given: the deleted row is the only denial a reader can meet, and it reads
            // the same to an anonymous caller as it does to an Admin
            this.ambientSecurityContext = anonymousSecurityContext;
            ContentItemSetting storageContentItemSetting = CreateRandomContentItemSetting();
            storageContentItemSetting.IsDeleted = true;
            Guid contentItemSettingId = storageContentItemSetting.Id;

            var notFoundContentItemSettingException =
                new NotFoundContentItemSettingException(
                    message: $"Content item setting not found with id: {contentItemSettingId}.");

            var expectedContentItemSettingValidationException =
                new ContentItemSettingValidationException(
                    message: "Content item setting validation error occurred, fix the errors and try again.",
                    innerException: notFoundContentItemSettingException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    contentItemSettingId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(storageContentItemSetting);

            // when
            ValueTask<ContentItemSetting> retrieveContentItemSettingByIdTask =
                this.contentItemSettingService.RetrieveContentItemSettingByIdAsync(
                    contentItemSettingId,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    retrieveContentItemSettingByIdTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    contentItemSettingId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogInformationAsync(
                    $"Content item setting read denied. Content item setting {contentItemSettingId} is " +
                        "soft-deleted; reported to the caller as not found."),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
