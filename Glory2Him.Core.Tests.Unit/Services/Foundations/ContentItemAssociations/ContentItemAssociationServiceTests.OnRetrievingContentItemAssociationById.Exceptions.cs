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
using Glory2Him.Core.Models.Foundations.ContentItemAssociations;
using Glory2Him.Core.Models.Foundations.ContentItemAssociations.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItemAssociations
{
    public partial class ContentItemAssociationServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnRetrievingContentItemAssociationByIdEventIfCancellationRequestedAsync()
        {
            // given
            EventEnvelope<ContentItemAssociation> requestEnvelope = CreateRandomContentItemAssociationRequestEnvelope();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<EventEnvelope<ContentItemAssociation>?> onRetrievingTask =
                this.contentItemAssociationService.OnRetrievingContentItemAssociationByIdAsync(
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
        public async Task ShouldThrowDependencyExceptionOnRetrievingContentItemAssociationByIdEventIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            EventEnvelope<ContentItemAssociation> requestEnvelope = CreateRandomContentItemAssociationRequestEnvelope();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutContentItemAssociationException =
                new TimeoutContentItemAssociationException(
                    message: "Failed content item association timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedContentItemAssociationDependencyException = new ContentItemAssociationDependencyException(
                message: "Content item association dependency error occurred, contact support.",
                innerException: timeoutContentItemAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    requestEnvelope.Content.Id,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<EventEnvelope<ContentItemAssociation>?> onRetrievingTask =
                this.contentItemAssociationService.OnRetrievingContentItemAssociationByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationDependencyException actualContentItemAssociationDependencyException =
                await Assert.ThrowsAsync<ContentItemAssociationDependencyException>(
                    onRetrievingTask.AsTask);

            // then: the nested retrieve categorizes the timeout and logs it exactly once —
            // the substrate wrapper must not double-wrap or re-log it.
            actualContentItemAssociationDependencyException.Should().BeEquivalentTo(
                expectedContentItemAssociationDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    requestEnvelope.Content.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemAssociationDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldPassThroughDependencyExceptionOnRetrievingContentItemAssociationByIdEventAsync()
        {
            // given
            EventEnvelope<ContentItemAssociation> requestEnvelope = CreateRandomContentItemAssociationRequestEnvelope();
            SqlException sqlException = GetSqlException();

            var failedStorageContentItemAssociationException = new FailedStorageContentItemAssociationException(
                message: "Failed content item association storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedContentItemAssociationDependencyException = new ContentItemAssociationDependencyException(
                message: "Content item association dependency error occurred, contact support.",
                innerException: failedStorageContentItemAssociationException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    requestEnvelope.Content.Id,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<EventEnvelope<ContentItemAssociation>?> onRetrievingTask =
                this.contentItemAssociationService.OnRetrievingContentItemAssociationByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationDependencyException actualContentItemAssociationDependencyException =
                await Assert.ThrowsAsync<ContentItemAssociationDependencyException>(
                    onRetrievingTask.AsTask);

            // then: the nested retrieve's categorized exception surfaces unwrapped and is
            // logged exactly once — the substrate wrapper must not double-wrap or re-log it.
            actualContentItemAssociationDependencyException.Should().BeEquivalentTo(
                expectedContentItemAssociationDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedContentItemAssociationDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRetrievingContentItemAssociationByIdEventIfServiceErrorOccursAndLogItAsync()
        {
            // given
            ContentItemAssociation storageContentItemAssociation = CreateRandomContentItemAssociation();
            storageContentItemAssociation.IsDeleted = false;
            storageContentItemAssociation.ApprovalStatus = ApprovalStatus.Approved;
            storageContentItemAssociation.IsPublished = true;
            storageContentItemAssociation.PublishDate = null;
            var serviceException = new Exception();

            var requestEnvelope = new EventEnvelope<ContentItemAssociation>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new ContentItemAssociation { Id = storageContentItemAssociation.Id },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var failedContentItemAssociationServiceException = new FailedContentItemAssociationServiceException(
                message: "Failed content item association service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedContentItemAssociationServiceException = new ContentItemAssociationServiceException(
                message: "Content item association service error occurred, contact support.",
                innerException: failedContentItemAssociationServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemAssociationByIdAsync(
                    storageContentItemAssociation.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageContentItemAssociation);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateNextAsync(requestEnvelope, storageContentItemAssociation))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<EventEnvelope<ContentItemAssociation>?> onRetrievingTask =
                this.contentItemAssociationService.OnRetrievingContentItemAssociationByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            ContentItemAssociationServiceException actualContentItemAssociationServiceException =
                await Assert.ThrowsAsync<ContentItemAssociationServiceException>(
                    onRetrievingTask.AsTask);

            // then
            actualContentItemAssociationServiceException.Should().BeEquivalentTo(
                expectedContentItemAssociationServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedContentItemAssociationServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
