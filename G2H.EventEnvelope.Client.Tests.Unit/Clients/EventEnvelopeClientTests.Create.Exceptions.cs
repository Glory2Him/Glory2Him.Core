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
using G2H.EventEnvelope.Client.Models.Clients.Exceptions;
using G2H.EventEnvelope.Client.Models.Foundations;
using Moq;
using Xeptions;

namespace G2H.EventEnvelope.Client.Tests.Unit.Clients
{
    public partial class EventEnvelopeClientTests
    {
        [Theory]
        [MemberData(nameof(ValidationExceptions))]
        public async Task ShouldThrowValidationExceptionOnCreateIfValidationErrorOccursAsync(
            Xeption validationException)
        {
            // given
            string someContent = GetRandomString();

            var expectedEventEnvelopeClientValidationException =
                new EventEnvelopeClientValidationException(
                    message: "Event envelope client validation error occurred, fix errors and try again.",
                    innerException: (validationException.InnerException as Xeption)!,
                    data: validationException.InnerException?.Data!);

            this.eventEnvelopeServiceMock.Setup(service =>
                service.CreateAsync(It.IsAny<string>()))
                    .Throws(validationException);

            // when
            ValueTask<EventEnvelope<string>> createTask =
                this.eventEnvelopeClient.CreateAsync(someContent);

            EventEnvelopeClientValidationException actualEventEnvelopeClientValidationException =
                await Assert.ThrowsAsync<EventEnvelopeClientValidationException>(createTask.AsTask);

            // then
            actualEventEnvelopeClientValidationException.Should()
                .BeEquivalentTo(expectedEventEnvelopeClientValidationException);

            this.eventEnvelopeServiceMock.Verify(service =>
                service.CreateAsync(It.IsAny<string>()),
                    Times.Once);

            this.eventEnvelopeServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [MemberData(nameof(DependencyExceptions))]
        public async Task ShouldThrowDependencyExceptionOnCreateIfDependencyErrorOccursAsync(
            Xeption dependencyException)
        {
            // given
            string someContent = GetRandomString();

            var expectedEventEnvelopeClientDependencyException =
                new EventEnvelopeClientDependencyException(
                    message: "Event envelope client dependency error occurred, please contact support.",
                    innerException: (dependencyException.InnerException as Xeption)!,
                    data: dependencyException.InnerException?.Data!);

            this.eventEnvelopeServiceMock.Setup(service =>
                service.CreateAsync(It.IsAny<string>()))
                    .Throws(dependencyException);

            // when
            ValueTask<EventEnvelope<string>> createTask =
                this.eventEnvelopeClient.CreateAsync(someContent);

            EventEnvelopeClientDependencyException actualEventEnvelopeClientDependencyException =
                await Assert.ThrowsAsync<EventEnvelopeClientDependencyException>(createTask.AsTask);

            // then
            actualEventEnvelopeClientDependencyException.Should()
                .BeEquivalentTo(expectedEventEnvelopeClientDependencyException);

            this.eventEnvelopeServiceMock.Verify(service =>
                service.CreateAsync(It.IsAny<string>()),
                    Times.Once);

            this.eventEnvelopeServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowServiceExceptionOnCreateIfServiceErrorOccursAsync()
        {
            // given
            string someContent = GetRandomString();
            var serviceException = new Exception(message: GetRandomString());

            var expectedEventEnvelopeClientServiceException =
                new EventEnvelopeClientServiceException(
                    message: "Event envelope client service error occurred, please contact support.",
                    innerException: serviceException,
                    data: serviceException.Data);

            this.eventEnvelopeServiceMock.Setup(service =>
                service.CreateAsync(It.IsAny<string>()))
                    .Throws(serviceException);

            // when
            ValueTask<EventEnvelope<string>> createTask =
                this.eventEnvelopeClient.CreateAsync(someContent);

            EventEnvelopeClientServiceException actualEventEnvelopeClientServiceException =
                await Assert.ThrowsAsync<EventEnvelopeClientServiceException>(createTask.AsTask);

            // then
            actualEventEnvelopeClientServiceException.Should()
                .BeEquivalentTo(expectedEventEnvelopeClientServiceException);

            this.eventEnvelopeServiceMock.Verify(service =>
                service.CreateAsync(It.IsAny<string>()),
                    Times.Once);

            this.eventEnvelopeServiceMock.VerifyNoOtherCalls();
        }
    }
}
