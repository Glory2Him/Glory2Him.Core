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
using Microsoft.Data.SqlClient;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItemSettings
{
    public partial class ContentItemSettingServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnRetrievingContentItemSettingByIdEventIfCancellationRequestedAsync()
        {
            // given
            EventEnvelope<ContentItemSetting> requestEnvelope = CreateRandomContentItemSettingRequestEnvelope();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<EventEnvelope<ContentItemSetting>?> onRetrievingTask =
                this.contentItemSettingService.OnRetrievingContentItemSettingByIdAsync(
                    requestEnvelope,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                onRetrievingTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnRetrievingContentItemSettingByIdEventIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            EventEnvelope<ContentItemSetting> requestEnvelope = CreateRandomContentItemSettingRequestEnvelope();
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
                    requestEnvelope.Content.Id,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<EventEnvelope<ContentItemSetting>?> onRetrievingTask =
                this.contentItemSettingService.OnRetrievingContentItemSettingByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemSettingDependencyException actualContentItemSettingDependencyException =
                await Assert.ThrowsAsync<ContentItemSettingDependencyException>(
                    onRetrievingTask.AsTask);

            // then: the nested retrieve categorizes the timeout and logs it exactly once —
            // the substrate wrapper must not double-wrap or re-log it.
            actualContentItemSettingDependencyException.Should().BeEquivalentTo(
                expectedContentItemSettingDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    requestEnvelope.Content.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldPassThroughDependencyExceptionOnRetrievingContentItemSettingByIdEventAsync()
        {
            // given
            EventEnvelope<ContentItemSetting> requestEnvelope = CreateRandomContentItemSettingRequestEnvelope();
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
                    requestEnvelope.Content.Id,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<EventEnvelope<ContentItemSetting>?> onRetrievingTask =
                this.contentItemSettingService.OnRetrievingContentItemSettingByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemSettingDependencyException actualContentItemSettingDependencyException =
                await Assert.ThrowsAsync<ContentItemSettingDependencyException>(
                    onRetrievingTask.AsTask);

            // then: the nested retrieve's categorized exception surfaces unwrapped and is
            // logged exactly once — the substrate wrapper must not double-wrap or re-log it.
            actualContentItemSettingDependencyException.Should().BeEquivalentTo(
                expectedContentItemSettingDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRetrievingContentItemSettingByIdEventIfServiceErrorOccursAndLogItAsync()
        {
            // given
            ContentItemSetting storageContentItemSetting = CreateRandomContentItemSetting();
            var serviceException = new Exception();

            var requestEnvelope = new EventEnvelope<ContentItemSetting>
            {
                Content = new ContentItemSetting { Id = storageContentItemSetting.Id },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var failedContentItemSettingServiceException = new FailedContentItemSettingServiceException(
                message: "Failed content item setting service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedContentItemSettingServiceException = new ContentItemSettingServiceException(
                message: "Content item setting service error occurred, contact support.",
                innerException: failedContentItemSettingServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemSettingByIdAsync(
                    storageContentItemSetting.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItemSetting);

            this.eventEnvelopeFactoryMock.Setup(factory =>
                factory.CreateNextAsync(requestEnvelope, storageContentItemSetting))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<EventEnvelope<ContentItemSetting>?> onRetrievingTask =
                this.contentItemSettingService.OnRetrievingContentItemSettingByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemSettingServiceException actualContentItemSettingServiceException =
                await Assert.ThrowsAsync<ContentItemSettingServiceException>(
                    onRetrievingTask.AsTask);

            // then
            actualContentItemSettingServiceException.Should().BeEquivalentTo(
                expectedContentItemSettingServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemSettingServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
