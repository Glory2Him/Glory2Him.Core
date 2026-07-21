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
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Foundations.ContentItemSettings.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItemSettings
{
    public partial class ContentItemSettingServiceTests
    {
        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnHardRemoveByIdIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            Guid someContentItemSettingId = Guid.NewGuid();

            var expectedContentItemSettingDependencyException = new ContentItemSettingDependencyException(
                message: "Content item setting dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<ContentItemSetting> hardRemoveContentItemSettingByIdTask =
                this.contentItemSettingService.HardRemoveContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    TestContext.Current.CancellationToken);

            ContentItemSettingDependencyException actualContentItemSettingDependencyException =
                await Assert.ThrowsAsync<ContentItemSettingDependencyException>(
                    hardRemoveContentItemSettingByIdTask.AsTask);

            // then
            actualContentItemSettingDependencyException.Should().BeEquivalentTo(
                expectedContentItemSettingDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnHardRemoveByIdIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            Guid someContentItemSettingId = Guid.NewGuid();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutContentItemSettingException =
                new TimeoutContentItemSettingException(
                    message: "Failed content item setting timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedContentItemSettingDependencyException = new ContentItemSettingDependencyException(
                message: "Content item setting dependency error occurred, contact support.",
                innerException: timeoutContentItemSettingException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<ContentItemSetting> hardRemoveContentItemSettingByIdTask =
                this.contentItemSettingService.HardRemoveContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    TestContext.Current.CancellationToken);

            ContentItemSettingDependencyException actualContentItemSettingDependencyException =
                await Assert.ThrowsAsync<ContentItemSettingDependencyException>(
                    hardRemoveContentItemSettingByIdTask.AsTask);

            // then
            actualContentItemSettingDependencyException.Should().BeEquivalentTo(
                expectedContentItemSettingDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnHardRemoveByIdIfCancellationRequestedAsync()
        {
            // given
            Guid someContentItemSettingId = Guid.NewGuid();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<ContentItemSetting> hardRemoveContentItemSettingByIdTask =
                this.contentItemSettingService.HardRemoveContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                hardRemoveContentItemSettingByIdTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnHardRemoveByIdIfSqlErrorOccursAndLogItAsync()
        {
            // given
            Guid someContentItemSettingId = Guid.NewGuid();
            SqlException sqlException = GetSqlException();

            var failedStorageContentItemSettingException = new FailedStorageContentItemSettingException(
                message: "Failed content item setting storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedContentItemSettingDependencyException = new ContentItemSettingDependencyException(
                message: "Content item setting dependency error occurred, contact support.",
                innerException: failedStorageContentItemSettingException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<ContentItemSetting> hardRemoveContentItemSettingByIdTask =
                this.contentItemSettingService.HardRemoveContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    TestContext.Current.CancellationToken);

            ContentItemSettingDependencyException actualContentItemSettingDependencyException =
                await Assert.ThrowsAsync<ContentItemSettingDependencyException>(
                    hardRemoveContentItemSettingByIdTask.AsTask);

            // then
            actualContentItemSettingDependencyException.Should().BeEquivalentTo(
                expectedContentItemSettingDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyValidationExceptionOnHardRemoveByIdIfDbUpdateConcurrencyExceptionOccursAndLogItAsync()
        {
            // given
            Guid someContentItemSettingId = Guid.NewGuid();
            ContentItemSetting someContentItemSetting = CreateRandomContentItemSetting();
            var dbUpdateConcurrencyException = new DbUpdateConcurrencyException();

            var lockedContentItemSettingException = new LockedContentItemSettingException(
                message: "Locked content item setting record, please try again later.",
                innerException: dbUpdateConcurrencyException,
                data: dbUpdateConcurrencyException.Data);

            var expectedContentItemSettingDependencyValidationException =
                new ContentItemSettingDependencyValidationException(
                message: "Content item setting dependency validation error occurred, fix the errors and try again.",
                innerException: lockedContentItemSettingException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    TestContext.Current.CancellationToken))
                        .ReturnsAsync(someContentItemSetting);

            this.storageBrokerMock.Setup(broker =>
                broker.DeleteContentItemSettingAsync(
                    someContentItemSetting,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(dbUpdateConcurrencyException);

            // when
            ValueTask<ContentItemSetting> hardRemoveContentItemSettingByIdTask =
                this.contentItemSettingService.HardRemoveContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    TestContext.Current.CancellationToken);

            ContentItemSettingDependencyValidationException actualContentItemSettingDependencyValidationException =
                await Assert.ThrowsAsync<ContentItemSettingDependencyValidationException>(
                    hardRemoveContentItemSettingByIdTask.AsTask);

            // then
            actualContentItemSettingDependencyValidationException.Should().BeEquivalentTo(
                expectedContentItemSettingDependencyValidationException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.storageBrokerMock.Verify(broker =>
                broker.DeleteContentItemSettingAsync(
                    someContentItemSetting,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingDependencyValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnHardRemoveByIdIfServiceErrorOccursAndLogItAsync()
        {
            // given
            Guid someContentItemSettingId = Guid.NewGuid();
            var serviceException = new Exception();

            var failedContentItemSettingServiceException = new FailedContentItemSettingServiceException(
                message: "Failed content item setting service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedContentItemSettingServiceException = new ContentItemSettingServiceException(
                message: "Content item setting service error occurred, contact support.",
                innerException: failedContentItemSettingServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<ContentItemSetting> hardRemoveContentItemSettingByIdTask =
                this.contentItemSettingService.HardRemoveContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    TestContext.Current.CancellationToken);

            ContentItemSettingServiceException actualContentItemSettingServiceException =
                await Assert.ThrowsAsync<ContentItemSettingServiceException>(
                    hardRemoveContentItemSettingByIdTask.AsTask);

            // then
            actualContentItemSettingServiceException.Should().BeEquivalentTo(
                expectedContentItemSettingServiceException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    someContentItemSettingId,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
