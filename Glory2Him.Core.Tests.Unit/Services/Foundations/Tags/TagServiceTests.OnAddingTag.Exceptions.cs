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
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Tags
{
    public partial class TagServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnAddingTagEventIfCancellationRequestedAsync()
        {
            // given
            EventEnvelope<Tag> requestEnvelope = CreateRandomTagRequestEnvelope();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<EventEnvelope<Tag>?> onAddingTask =
                this.tagService.OnAddingTagAsync(
                    requestEnvelope,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                onAddingTask.AsTask);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnAddingTagEventIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            EventEnvelope<Tag> requestEnvelope = CreateRandomTagRequestEnvelope();

            var expectedTagDependencyException = new TagDependencyException(
                message: "Tag dependency error occurred, contact support.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.TagOnAddingTagSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<EventEnvelope<Tag>?> onAddingTask =
                this.tagService.OnAddingTagAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            TagDependencyException actualTagDependencyException =
                await Assert.ThrowsAsync<TagDependencyException>(
                    onAddingTask.AsTask);

            // then
            actualTagDependencyException.Should().BeEquivalentTo(
                expectedTagDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnAddingTagEventIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            EventEnvelope<Tag> requestEnvelope = CreateRandomTagRequestEnvelope();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutTagException =
                new TimeoutTagException(
                    message: "Failed tag timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedTagDependencyException = new TagDependencyException(
                message: "Tag dependency error occurred, contact support.",
                innerException: timeoutTagException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.TagOnAddingTagSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<EventEnvelope<Tag>?> onAddingTask =
                this.tagService.OnAddingTagAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            TagDependencyException actualTagDependencyException =
                await Assert.ThrowsAsync<TagDependencyException>(
                    onAddingTask.AsTask);

            // then
            actualTagDependencyException.Should().BeEquivalentTo(
                expectedTagDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.TagOnAddingTagSubscriptionName,
                    TestContext.Current.CancellationToken),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowCriticalDependencyExceptionOnAddingTagEventIfSqlErrorOccursAndLogItAsync()
        {
            // given
            EventEnvelope<Tag> requestEnvelope = CreateRandomTagRequestEnvelope();
            SqlException sqlException = GetSqlException();

            var failedStorageTagException = new FailedStorageTagException(
                message: "Failed tag storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedTagDependencyException = new TagDependencyException(
                message: "Tag dependency error occurred, contact support.",
                innerException: failedStorageTagException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.TagOnAddingTagSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<EventEnvelope<Tag>?> onAddingTask =
                this.tagService.OnAddingTagAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            TagDependencyException actualTagDependencyException =
                await Assert.ThrowsAsync<TagDependencyException>(
                    onAddingTask.AsTask);

            // then
            actualTagDependencyException.Should().BeEquivalentTo(
                expectedTagDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedTagDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnAddingTagEventIfErrorOccursAndLogItAsync(
            Exception thrownException,
            Xeption expectedInnerException)
        {
            // given
            EventEnvelope<Tag> requestEnvelope = CreateRandomTagRequestEnvelope();

            var expectedTagDependencyValidationException = new TagDependencyValidationException(
                message: "Tag dependency validation error occurred, fix the errors and try again.",
                innerException: expectedInnerException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.TagOnAddingTagSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(thrownException);

            // when
            ValueTask<EventEnvelope<Tag>?> onAddingTask =
                this.tagService.OnAddingTagAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            TagDependencyValidationException actualTagDependencyValidationException =
                await Assert.ThrowsAsync<TagDependencyValidationException>(
                    onAddingTask.AsTask);

            // then
            actualTagDependencyValidationException.Should().BeEquivalentTo(
                expectedTagDependencyValidationException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagDependencyValidationException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnAddingTagEventIfServiceErrorOccursAndLogItAsync()
        {
            // given
            EventEnvelope<Tag> requestEnvelope = CreateRandomTagRequestEnvelope();
            var serviceException = new Exception();

            var failedTagServiceException = new FailedTagServiceException(
                message: "Failed tag service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedTagServiceException = new TagServiceException(
                message: "Tag service error occurred, contact support.",
                innerException: failedTagServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectProcessedEventExistsAsync(
                    requestEnvelope.Metadata.EventId,
                    EventBrokerIdentifiers.TagOnAddingTagSubscriptionName,
                    TestContext.Current.CancellationToken))
                        .ThrowsAsync(serviceException);

            // when
            ValueTask<EventEnvelope<Tag>?> onAddingTask =
                this.tagService.OnAddingTagAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            TagServiceException actualTagServiceException =
                await Assert.ThrowsAsync<TagServiceException>(
                    onAddingTask.AsTask);

            // then
            actualTagServiceException.Should().BeEquivalentTo(
                expectedTagServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedTagServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
