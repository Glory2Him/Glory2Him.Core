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
using G2H.EventEnvelope.Client.Models.Foundations;
using G2H.EventEnvelope.Client.Models.Foundations.Exceptions;
using G2H.EventEnvelope.Client.Services.Foundations.Events;
using Moq;

namespace G2H.EventEnvelope.Client.Tests.Unit.Services.Foundations.Events
{
    public partial class EventEnvelopeServiceTests
    {
        [Fact]
        public async Task ShouldThrowServiceExceptionOnCreateIfServiceErrorOccursAndLogItAsync()
        {
            // given
            string someContent = GetRandomString();
            var serviceException = new Exception();

            var failedEventEnvelopeServiceException =
                new FailedEventEnvelopeServiceException(
                    message: "Failed event envelope service error occurred, please contact support.",
                    innerException: serviceException);

            var expectedEventEnvelopeServiceException =
                new EventEnvelopeServiceException(
                    message: "Event envelope service error occurred, please contact support.",
                    innerException: failedEventEnvelopeServiceException);

            var eventEnvelopeServiceMock = new Mock<EventEnvelopeService>(
                this.identifierBrokerMock.Object,
                this.dateTimeBrokerMock.Object,
                this.securityBrokerMock.Object)
            { CallBase = true };

            eventEnvelopeServiceMock.Setup(service =>
                service.ValidateOnCreate(It.IsAny<string>()))
                    .Throws(serviceException);

            // when
            ValueTask<EventEnvelope<string>> createTask =
                eventEnvelopeServiceMock.Object.CreateAsync(someContent, TestContext.Current.CancellationToken);

            EventEnvelopeServiceException actualEventEnvelopeServiceException =
                await Assert.ThrowsAsync<EventEnvelopeServiceException>(createTask.AsTask);

            // then
            actualEventEnvelopeServiceException.Should()
                .BeEquivalentTo(expectedEventEnvelopeServiceException);

            eventEnvelopeServiceMock.Verify(service =>
                service.ValidateOnCreate(It.IsAny<string>()),
                    Times.Once);

            eventEnvelopeServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnCreateIfBrokerErrorOccursAndLogItAsync()
        {
            // given
            string someContent = GetRandomString();
            var brokerException = new Exception();

            var failedEventEnvelopeServiceException =
                new FailedEventEnvelopeServiceException(
                    message: "Failed event envelope service error occurred, please contact support.",
                    innerException: brokerException);

            var expectedEventEnvelopeServiceException =
                new EventEnvelopeServiceException(
                    message: "Event envelope service error occurred, please contact support.",
                    innerException: failedEventEnvelopeServiceException);

            this.securityBrokerMock.Setup(broker =>
                broker.GetCurrentSecurityContextAsync())
                    .Throws(brokerException);

            // when
            ValueTask<EventEnvelope<string>> createTask =
                this.eventEnvelopeService.CreateAsync(someContent, TestContext.Current.CancellationToken);

            EventEnvelopeServiceException actualEventEnvelopeServiceException =
                await Assert.ThrowsAsync<EventEnvelopeServiceException>(createTask.AsTask);

            // then
            actualEventEnvelopeServiceException.Should()
                .BeEquivalentTo(expectedEventEnvelopeServiceException);

            this.securityBrokerMock.Verify(broker =>
                broker.GetCurrentSecurityContextAsync(),
                    Times.Once);

            this.securityBrokerMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowDependencyExceptionOnCreateIfOperationCanceledExceptionOccursAndLogItAsync()
        {
            // given
            string someContent = GetRandomString();
            var operationCanceledException = new OperationCanceledException();

            var timeoutException =
                new TimeoutException("The dependency operation timed out.");

            var timeoutEventEnvelopeException =
                new TimeoutEventEnvelopeException(
                    message: "Failed event envelope timeout error occurred, contact support.",
                    innerException: timeoutException,
                    data: timeoutException.Data);

            var expectedEventEnvelopeDependencyException =
                new EventEnvelopeDependencyException(
                    message: "Event envelope dependency error occurred, contact support.",
                    innerException: timeoutEventEnvelopeException);

            this.securityBrokerMock.Setup(broker =>
                broker.GetCurrentSecurityContextAsync())
                    .Throws(operationCanceledException);

            // when
            ValueTask<EventEnvelope<string>> createTask =
                this.eventEnvelopeService.CreateAsync(someContent, TestContext.Current.CancellationToken);

            EventEnvelopeDependencyException actualEventEnvelopeDependencyException =
                await Assert.ThrowsAsync<EventEnvelopeDependencyException>(createTask.AsTask);

            // then
            actualEventEnvelopeDependencyException.Should()
                .BeEquivalentTo(expectedEventEnvelopeDependencyException);

            this.securityBrokerMock.Verify(broker =>
                broker.GetCurrentSecurityContextAsync(),
                    Times.Once);

            this.securityBrokerMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnCreateIfCancellationRequestedAsync()
        {
            // given
            string someContent = GetRandomString();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<EventEnvelope<string>> createTask =
                this.eventEnvelopeService.CreateAsync(someContent, cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(createTask.AsTask);

            this.securityBrokerMock.VerifyNoOtherCalls();
            this.identifierBrokerMock.VerifyNoOtherCalls();
            this.dateTimeBrokerMock.VerifyNoOtherCalls();
        }
    }
}
