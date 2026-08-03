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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Tags
{
    public partial class TagServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnRetrievingTagByIdEventIfCancellationRequestedAsync()
        {
            // given
            EventEnvelope<Tag> requestEnvelope = CreateRandomTagRequestEnvelope();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<EventEnvelope<Tag>?> onRetrievingTask =
                this.tagService.OnRetrievingTagByIdAsync(
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
        public async Task ShouldThrowDependencyExceptionOnRetrievingTagByIdEventIfOperationCanceledExceptionOccursAndLogItAsync()
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
                broker.SelectTagByIdAsync(
                    requestEnvelope.Content.Id,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<EventEnvelope<Tag>?> onRetrievingTask =
                this.tagService.OnRetrievingTagByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            TagDependencyException actualTagDependencyException =
                await Assert.ThrowsAsync<TagDependencyException>(
                    onRetrievingTask.AsTask);

            // then: the nested retrieve categorizes the timeout and logs it exactly once —
            // the substrate wrapper must not double-wrap or re-log it.
            actualTagDependencyException.Should().BeEquivalentTo(
                expectedTagDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectTagByIdAsync(
                    requestEnvelope.Content.Id,
                    It.IsAny<CancellationToken>()),
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
        public async Task ShouldPassThroughDependencyExceptionOnRetrievingTagByIdEventAsync()
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
                broker.SelectTagByIdAsync(
                    requestEnvelope.Content.Id,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<EventEnvelope<Tag>?> onRetrievingTask =
                this.tagService.OnRetrievingTagByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            TagDependencyException actualTagDependencyException =
                await Assert.ThrowsAsync<TagDependencyException>(
                    onRetrievingTask.AsTask);

            // then: the nested retrieve's categorized exception surfaces unwrapped and is
            // logged exactly once — the substrate wrapper must not double-wrap or re-log it.
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

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRetrievingTagByIdEventIfServiceErrorOccursAndLogItAsync()
        {
            // given
            Tag storageTag = CreateRandomTag();
            storageTag.IsDeleted = false;
            storageTag.ApprovalStatus = ApprovalStatus.Approved;
            storageTag.IsPublished = true;
            storageTag.PublishDate = null;
            var serviceException = new Exception();

            var requestEnvelope = new EventEnvelope<Tag>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new Tag { Id = storageTag.Id },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var failedTagServiceException = new FailedTagServiceException(
                message: "Failed tag service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedTagServiceException = new TagServiceException(
                message: "Tag service error occurred, contact support.",
                innerException: failedTagServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectTagByIdAsync(
                    storageTag.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageTag);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateNextAsync(requestEnvelope, storageTag))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<EventEnvelope<Tag>?> onRetrievingTask =
                this.tagService.OnRetrievingTagByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            TagServiceException actualTagServiceException =
                await Assert.ThrowsAsync<TagServiceException>(
                    onRetrievingTask.AsTask);

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
