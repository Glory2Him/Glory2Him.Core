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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Processings.Links.Exceptions;
using Moq;
using Xeptions;

namespace Glory2Him.Core.Tests.Unit.Services.Processings.Links
{
    public partial class LinkProcessingServiceTests
    {
        public enum GroupRead
        {
            Latest,
            Published,
            Collection
        }

        private Task GroupReadTask(
            GroupRead groupRead,
            Guid groupId,
            CancellationToken cancellationToken) =>
            groupRead switch
            {
                GroupRead.Latest =>
                    this.linkProcessingService.RetrieveLatestLinkByGroupIdAsync(
                        groupId, cancellationToken).AsTask(),

                GroupRead.Published =>
                    this.linkProcessingService.RetrievePublishedLinkByGroupIdAsync(
                        groupId, cancellationToken).AsTask(),

                _ =>
                    this.linkProcessingService.RetrieveLinksByGroupIdAsync(
                        groupId, cancellationToken).AsTask()
            };

        private void SetupInboundEnvelopeIfMinted(GroupRead groupRead, Guid groupId)
        {
            if (groupRead is GroupRead.Collection)
            {
                return;
            }

            EventEnvelope<Link> inboundEnvelope = CreateEventEnvelope(
                link: new Link { GroupId = groupId },
                securityContext: CreateAuthenticatedSecurityContext());

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(groupId))))
                    .ReturnsAsync(inboundEnvelope);
        }

        private void SetupServiceErrorAtTheFirstOutwardCall(
            GroupRead groupRead,
            Guid groupId,
            Exception serviceException)
        {
            if (groupRead is GroupRead.Collection)
            {
                this.linkServiceMock.Setup(service =>
                    service.RetrieveLinksByGroupIdAsync(
                        It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                            .ThrowsAsync(serviceException);

                return;
            }

            this.eventEnvelopeBrokerMock.Setup(broker =>
                broker.CreateAsync(It.Is(SameGroupRetrieveRequestAs(groupId))))
                    .ThrowsAsync(serviceException);
        }

        public static TheoryData<GroupRead, Xeption> GroupReadDependencyValidationExceptions()
        {
            var theoryData = new TheoryData<GroupRead, Xeption>();

            foreach (GroupRead groupRead in Enum.GetValues<GroupRead>())
            {
                foreach (Xeption exception in DependencyValidationExceptionCases())
                {
                    theoryData.Add(groupRead, exception);
                }
            }

            return theoryData;
        }

        public static TheoryData<GroupRead, Xeption> GroupReadDependencyExceptions()
        {
            var theoryData = new TheoryData<GroupRead, Xeption>();

            foreach (GroupRead groupRead in Enum.GetValues<GroupRead>())
            {
                foreach (Xeption exception in DependencyExceptionCases())
                {
                    theoryData.Add(groupRead, exception);
                }
            }

            return theoryData;
        }

        public static TheoryData<GroupRead> GroupReads()
        {
            var theoryData = new TheoryData<GroupRead>();

            foreach (GroupRead groupRead in Enum.GetValues<GroupRead>())
            {
                theoryData.Add(groupRead);
            }

            return theoryData;
        }

        [Theory]
        [MemberData(nameof(GroupReadDependencyValidationExceptions))]
        public async Task ShouldThrowDependencyValidationExceptionOnGroupReadIfValidationErrorOccursAndLogItAsync(
            GroupRead groupRead,
            Xeption dependencyValidationException)
        {
            // given
            Guid inputGroupId = Guid.NewGuid();

            var expectedLinkProcessingDependencyValidationException =
                new LinkProcessingDependencyValidationException(
                    message: "Link processing dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: (dependencyValidationException.InnerException as Xeption)!);

            SetupInboundEnvelopeIfMinted(groupRead, inputGroupId);

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinksByGroupIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dependencyValidationException);

            // when
            Task groupReadTask = GroupReadTask(
                groupRead,
                inputGroupId,
                TestContext.Current.CancellationToken);

            LinkProcessingDependencyValidationException
                actualLinkProcessingDependencyValidationException =
                    await Assert.ThrowsAsync<LinkProcessingDependencyValidationException>(
                        () => groupReadTask);

            // then
            actualLinkProcessingDependencyValidationException.Should().BeEquivalentTo(
                expectedLinkProcessingDependencyValidationException);

            this.linkServiceMock.Verify(service =>
                service.RetrieveLinksByGroupIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingDependencyValidationException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(GroupReadDependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnGroupReadIfDependencyErrorOccursAndLogItAsync(
            GroupRead groupRead,
            Xeption dependencyException)
        {
            // given
            Guid inputGroupId = Guid.NewGuid();

            var expectedLinkProcessingDependencyException =
                new LinkProcessingDependencyException(
                    message: "Link processing dependency error occurred, contact support.",
                    innerException: (dependencyException.InnerException as Xeption)!);

            SetupInboundEnvelopeIfMinted(groupRead, inputGroupId);

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinksByGroupIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(dependencyException);

            // when
            Task groupReadTask = GroupReadTask(
                groupRead,
                inputGroupId,
                TestContext.Current.CancellationToken);

            LinkProcessingDependencyException actualLinkProcessingDependencyException =
                await Assert.ThrowsAsync<LinkProcessingDependencyException>(
                    () => groupReadTask);

            // then
            actualLinkProcessingDependencyException.Should().BeEquivalentTo(
                expectedLinkProcessingDependencyException);

            this.linkServiceMock.Verify(service =>
                service.RetrieveLinksByGroupIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Once);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(GroupReads))]
        public async Task ShouldThrowDependencyExceptionOnGroupReadIfOperationCanceledExceptionOccursAndLogItAsync(
            GroupRead groupRead)
        {
            // given
            Guid inputGroupId = Guid.NewGuid();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutLinkProcessingException =
                new TimeoutLinkProcessingException(
                    message: "Failed link processing timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedLinkProcessingDependencyException =
                new LinkProcessingDependencyException(
                    message: "Link processing dependency error occurred, contact support.",
                    innerException: timeoutLinkProcessingException);

            SetupInboundEnvelopeIfMinted(groupRead, inputGroupId);

            this.linkServiceMock.Setup(service =>
                service.RetrieveLinksByGroupIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(operationCanceledException);

            // when
            Task groupReadTask = GroupReadTask(
                groupRead,
                inputGroupId,
                TestContext.Current.CancellationToken);

            LinkProcessingDependencyException actualLinkProcessingDependencyException =
                await Assert.ThrowsAsync<LinkProcessingDependencyException>(
                    () => groupReadTask);

            // then
            actualLinkProcessingDependencyException.Should().BeEquivalentTo(
                expectedLinkProcessingDependencyException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingDependencyException))),
                Times.Once);

            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(GroupReads))]
        public async Task ShouldThrowServiceExceptionOnGroupReadIfServiceErrorOccursAndLogItAsync(
            GroupRead groupRead)
        {
            // given
            Guid inputGroupId = Guid.NewGuid();
            var serviceException = new Exception("Service error occurred.");

            var failedLinkProcessingServiceException =
                new FailedLinkProcessingServiceException(
                    message: "Failed link processing service error occurred, please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            var expectedLinkProcessingServiceException =
                new LinkProcessingServiceException(
                    message: "Link processing service error occurred, contact support.",
                    innerException: failedLinkProcessingServiceException);

            SetupServiceErrorAtTheFirstOutwardCall(
                groupRead,
                inputGroupId,
                serviceException);

            // when
            Task groupReadTask = GroupReadTask(
                groupRead,
                inputGroupId,
                TestContext.Current.CancellationToken);

            LinkProcessingServiceException actualLinkProcessingServiceException =
                await Assert.ThrowsAsync<LinkProcessingServiceException>(
                    () => groupReadTask);

            // then
            actualLinkProcessingServiceException.Should().BeEquivalentTo(
                expectedLinkProcessingServiceException);

            this.loggingBrokerMock.Verify(broker =>
                broker.LogErrorAsync(It.Is(
                    SameExceptionAs(expectedLinkProcessingServiceException))),
                Times.Once);

            if (groupRead is GroupRead.Collection)
            {
                this.linkServiceMock.Verify(service =>
                    service.RetrieveLinksByGroupIdAsync(
                        It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                    Times.Once);
            }

            this.linkServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(GroupReads))]
        public async Task ShouldThrowOperationCanceledExceptionOnGroupReadIfCancellationRequestedAsync(
            GroupRead groupRead)
        {
            // given
            Guid inputGroupId = Guid.NewGuid();
            using var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // when
            Task groupReadTask = GroupReadTask(
                groupRead,
                inputGroupId,
                cancellationTokenSource.Token);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(() => groupReadTask);

            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.linkServiceMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
