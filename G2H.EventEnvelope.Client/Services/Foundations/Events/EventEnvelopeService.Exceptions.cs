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
using G2H.EventEnvelope.Client.Models.Foundations.Exceptions;
using Xeptions;

namespace G2H.EventEnvelope.Client.Services.Foundations.Events
{
    internal partial class EventEnvelopeService
    {
        private delegate ValueTask<T> ReturningObjectFunction<T>();

        private async ValueTask<T> TryCatch<T>(ReturningObjectFunction<T> returningObjectFunction)
        {
            try
            {
                return await returningObjectFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutEventEnvelopeException =
                    new TimeoutEventEnvelopeException(
                        message: "Failed event envelope timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(
                    exception: timeoutEventEnvelopeException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidArgumentEventEnvelopeException invalidArgumentEventEnvelopeException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: invalidArgumentEventEnvelopeException);
            }
            catch (Exception exception)
            {
                var failedEventEnvelopeServiceException =
                    new FailedEventEnvelopeServiceException(
                        message: "Failed event envelope service error occurred, please contact support.",
                        innerException: exception);

                throw await CreateAndLogServiceExceptionAsync(exception: failedEventEnvelopeServiceException);
            }
        }

        private async ValueTask<EventEnvelopeValidationException> CreateAndLogValidationExceptionAsync(
            Xeption exception)
        {
            var eventEnvelopeValidationException =
                new EventEnvelopeValidationException(
                    message: "Event envelope validation errors occurred, please try again.",
                    innerException: exception);

            return eventEnvelopeValidationException;
        }

        private async ValueTask<EventEnvelopeDependencyException> CreateAndLogTimeoutDependencyExceptionAsync(
            Xeption exception)
        {
            var eventEnvelopeDependencyException =
                new EventEnvelopeDependencyException(
                    message: "Event envelope dependency error occurred, contact support.",
                    innerException: exception);

            return eventEnvelopeDependencyException;
        }

        private async ValueTask<EventEnvelopeServiceException> CreateAndLogServiceExceptionAsync(
            Xeption exception)
        {
            var eventEnvelopeServiceException =
                new EventEnvelopeServiceException(
                    message: "Event envelope service error occurred, please contact support.",
                    innerException: exception);

            return eventEnvelopeServiceException;
        }
    }
}
