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
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Links.Exceptions;
using Microsoft.Data.SqlClient;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Links
{
    public partial class LinkServiceTests
    {
        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnRetrievingLinkByIdEventIfCancellationRequestedAsync()
        {
            // given
            EventEnvelope<Link> requestEnvelope = CreateRandomLinkRequestEnvelope();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<EventEnvelope<Link>?> onRetrievingTask =
                this.linkService.OnRetrievingLinkByIdAsync(
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
        public async Task ShouldThrowDependencyExceptionOnRetrievingLinkByIdEventIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            EventEnvelope<Link> requestEnvelope = CreateRandomLinkRequestEnvelope();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutLinkException =
                new TimeoutLinkException(
                    message: "Failed link timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedLinkDependencyException = new LinkDependencyException(
                message: "Link dependency error occurred, contact support.",
                innerException: timeoutLinkException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    requestEnvelope.Content.Id,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            ValueTask<EventEnvelope<Link>?> onRetrievingTask =
                this.linkService.OnRetrievingLinkByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            LinkDependencyException actualLinkDependencyException =
                await Assert.ThrowsAsync<LinkDependencyException>(
                    onRetrievingTask.AsTask);

            // then: the nested retrieve categorizes the timeout and logs it exactly once —
            // the substrate wrapper must not double-wrap or re-log it.
            actualLinkDependencyException.Should().BeEquivalentTo(
                expectedLinkDependencyException);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(
                    requestEnvelope.Content.Id,
                    It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldPassThroughDependencyExceptionOnRetrievingLinkByIdEventAsync()
        {
            // given
            EventEnvelope<Link> requestEnvelope = CreateRandomLinkRequestEnvelope();
            SqlException sqlException = GetSqlException();

            var failedStorageLinkException = new FailedStorageLinkException(
                message: "Failed link storage error occurred, contact support.",
                innerException: sqlException,
                data: sqlException.Data);

            var expectedLinkDependencyException = new LinkDependencyException(
                message: "Link dependency error occurred, contact support.",
                innerException: failedStorageLinkException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    requestEnvelope.Content.Id,
                    It.IsAny<CancellationToken>()))
                        .ThrowsAsync(sqlException);

            // when
            ValueTask<EventEnvelope<Link>?> onRetrievingTask =
                this.linkService.OnRetrievingLinkByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            LinkDependencyException actualLinkDependencyException =
                await Assert.ThrowsAsync<LinkDependencyException>(
                    onRetrievingTask.AsTask);

            // then: the nested retrieve's categorized exception surfaces unwrapped and is
            // logged exactly once — the substrate wrapper must not double-wrap or re-log it.
            actualLinkDependencyException.Should().BeEquivalentTo(
                expectedLinkDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogCriticalAsync(It.Is(
                    SameExceptionAs(expectedLinkDependencyException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnRetrievingLinkByIdEventIfServiceErrorOccursAndLogItAsync()
        {
            // given
            Link storageLink = CreateRandomLink();
            storageLink.IsDeleted = false;
            storageLink.ApprovalStatus = ApprovalStatus.Approved;
            storageLink.IsPublished = true;
            storageLink.PublishDate = null;
            var serviceException = new Exception();

            var requestEnvelope = new EventEnvelope<Link>
            {
                SecurityContext = CreateAuthenticatedSecurityContext(),
                Content = new Link { Id = storageLink.Id },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() }
            };

            var failedLinkServiceException = new FailedLinkServiceException(
                message: "Failed link service error occurred, please contact support.",
                innerException: serviceException,
                data: serviceException.Data);

            var expectedLinkServiceException = new LinkServiceException(
                message: "Link service error occurred, contact support.",
                innerException: failedLinkServiceException);

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(
                    storageLink.Id,
                    It.IsAny<CancellationToken>()))
                        .ReturnsAsync(storageLink);

            this.dateTimeBrokerMock.Setup(broker =>
                broker.GetCurrentDateTimeOffsetAsync())
                    .ReturnsAsync(GetRandomDateTimeOffset());

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateNextAsync(requestEnvelope, storageLink))
                    .ThrowsAsync(serviceException);

            // when
            ValueTask<EventEnvelope<Link>?> onRetrievingTask =
                this.linkService.OnRetrievingLinkByIdAsync(
                    requestEnvelope,
                    TestContext.Current.CancellationToken);

            LinkServiceException actualLinkServiceException =
                await Assert.ThrowsAsync<LinkServiceException>(
                    onRetrievingTask.AsTask);

            // then
            actualLinkServiceException.Should().BeEquivalentTo(
                expectedLinkServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkServiceException))),
                Times.Once);

            this.securityAuditBrokerMock.VerifyNoOtherCalls();
            this.eventBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
