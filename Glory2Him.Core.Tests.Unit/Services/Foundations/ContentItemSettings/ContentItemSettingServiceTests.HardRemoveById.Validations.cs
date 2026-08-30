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
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfIdIsInvalidAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            var invalidContentItemSettingId = Guid.Empty;

            var invalidContentItemSettingException = new InvalidContentItemSettingException(
                message: "Content item setting is invalid, fix the errors and try again.");

            invalidContentItemSettingException.UpsertDataList(
                key: nameof(ContentItemSetting.Id),
                value: "Id is required");

            var expectedContentItemSettingValidationException = new ContentItemSettingValidationException(
                message: "Content item setting validation error occurred, fix the errors and try again.",
                innerException: invalidContentItemSettingException);

            // when
            ValueTask<ContentItemSetting> hardRemoveContentItemSettingByIdTask =
                this.contentItemSettingService.HardRemoveContentItemSettingByIdAsync(
                    invalidContentItemSettingId,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    hardRemoveContentItemSettingByIdTask.AsTask);

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
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfContentItemSettingNotFoundAndLogItAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(Roles.Administrators);
            Guid someContentItemSettingId = Guid.NewGuid();
            ContentItemSetting noContentItemSetting = null;

            var notFoundContentItemSettingException = new NotFoundContentItemSettingException(
                message: $"Content item setting not found with id: {someContentItemSettingId}.");

            var expectedContentItemSettingValidationException = new ContentItemSettingValidationException(
                message: "Content item setting validation error occurred, fix the errors and try again.",
                innerException: notFoundContentItemSettingException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(noContentItemSetting);

            // when
            ValueTask<ContentItemSetting> hardRemoveContentItemSettingByIdTask =
                this.contentItemSettingService.HardRemoveContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    hardRemoveContentItemSettingByIdTask.AsTask);

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

        [Theory]
        [MemberData(nameof(UnauthenticatedSecurityContexts))]
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfUserIsNotAuthenticatedAndLogItAsync(
            SecurityContext invalidSecurityContext)
        {
            // given
            this.ambientSecurityContext = invalidSecurityContext;
            Guid someContentItemSettingId = Guid.NewGuid();
            var unauthorizedContentItemSettingException = new UnauthorizedContentItemSettingException(
                message: "The current user is not authenticated.");

            var expectedContentItemSettingValidationException = new ContentItemSettingValidationException(
                message: "Content item setting validation error occurred, fix the errors and try again.",
                innerException: unauthorizedContentItemSettingException);

            // when
            ValueTask<ContentItemSetting> hardRemoveContentItemSettingByIdTask =
                this.contentItemSettingService.HardRemoveContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    hardRemoveContentItemSettingByIdTask.AsTask);

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

        [Theory]
        [MemberData(nameof(NonAdminRoleSets))]
        public async Task ShouldThrowValidationExceptionOnHardRemoveByIdIfUserIsNotAdminAndLogItAsync(
            string[] nonAdminRoles)
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext(nonAdminRoles);
            Guid someContentItemSettingId = Guid.NewGuid();
            var unauthorizedContentItemSettingException = new UnauthorizedContentItemSettingException(
                message: "The current user is not allowed to administer content item settings.");

            var expectedContentItemSettingValidationException = new ContentItemSettingValidationException(
                message: "Content item setting validation error occurred, fix the errors and try again.",
                innerException: unauthorizedContentItemSettingException);

            // when
            ValueTask<ContentItemSetting> hardRemoveContentItemSettingByIdTask =
                this.contentItemSettingService.HardRemoveContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    hardRemoveContentItemSettingByIdTask.AsTask);

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
        public async Task ShouldBlockHardRemoveWhenTheCallerIsGloballyReadOnlyAndLogItAsync()
        {
            // given: the global ReadOnly ban outranks Administrators, so a banned administrator is refused before
            // the row is even read — the destructive surface is not an exception to the site-wide
            // contribution ban.
            this.ambientSecurityContext =
                CreateAuthenticatedSecurityContext(Roles.Administrators, Roles.ReadOnly);

            Guid someContentItemSettingId = Guid.NewGuid();

            var unauthorizedContentItemSettingException = new UnauthorizedContentItemSettingException(
                message: "The current user is blocked from administering content item settings.");

            var expectedContentItemSettingValidationException = new ContentItemSettingValidationException(
                message: "Content item setting validation error occurred, fix the errors and try again.",
                innerException: unauthorizedContentItemSettingException);

            // when
            ValueTask<ContentItemSetting> hardRemoveContentItemSettingByIdTask =
                this.contentItemSettingService.HardRemoveContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    TestContext.Current.CancellationToken);

            ContentItemSettingValidationException actualContentItemSettingValidationException =
                await Assert.ThrowsAsync<ContentItemSettingValidationException>(
                    hardRemoveContentItemSettingByIdTask.AsTask);

            // then
            actualContentItemSettingValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingValidationException))),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteContentItemSettingAsync(
                    It.IsAny<ContentItemSetting>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
