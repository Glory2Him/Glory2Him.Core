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
using System.Linq;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Links.Exceptions;
using Glory2Him.Core.Models.Processings.Links.Exceptions;
using Xeptions;

namespace Glory2Him.Core.Services.Processings.Links
{
    internal partial class LinkProcessingService
    {
        private delegate ValueTask<Link> ReturningLinkFunction();

        private delegate ValueTask<IQueryable<Link>> ReturningLinksFunction();

        private delegate ValueTask<EventEnvelope<Link>?> ReturningLinkEventEnvelopeFunction();

        private async ValueTask<EventEnvelope<Link>?> TryCatchSubstrate(
            ReturningLinkEventEnvelopeFunction returningLinkEventEnvelopeFunction)
        {
            try
            {
                return await returningLinkEventEnvelopeFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutLinkProcessingException =
                    new TimeoutLinkProcessingException(
                        message: "Failed link processing timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(
                    exception: timeoutLinkProcessingException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidLinkProcessingEventException invalidLinkProcessingEventException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: invalidLinkProcessingEventException);
            }
            catch (UnauthorizedLinkProcessingException unauthorizedLinkProcessingException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: unauthorizedLinkProcessingException);
            }
            catch (NullLinkProcessingException nullLinkProcessingException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullLinkProcessingException);
            }
            catch (NotFoundLinkProcessingException notFoundLinkProcessingException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: notFoundLinkProcessingException);
            }
            catch (InvalidLinkProcessingException invalidLinkProcessingException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidLinkProcessingException);
            }
            catch (LinkValidationException linkValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(exception: linkValidationException);
            }
            catch (LinkDependencyValidationException linkDependencyValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(
                    exception: linkDependencyValidationException);
            }
            catch (LinkDependencyException linkDependencyException)
            {
                throw await CreateAndLogDependencyExceptionAsync(exception: linkDependencyException);
            }
            catch (LinkServiceException linkServiceException)
            {
                throw await CreateAndLogDependencyExceptionAsync(exception: linkServiceException);
            }
            catch (Exception exception)
            {
                var failedLinkProcessingServiceException =
                    new FailedLinkProcessingServiceException(
                        message: "Failed link processing service error occurred, " +
                            "please contact support.",
                        innerException: exception,
                        data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(
                    exception: failedLinkProcessingServiceException);
            }
        }

        private async ValueTask<Link> TryCatch(
            ReturningLinkFunction returningLinkFunction)
        {
            try
            {
                return await returningLinkFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutLinkProcessingException =
                    new TimeoutLinkProcessingException(
                        message: "Failed link processing timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(
                    exception: timeoutLinkProcessingException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UnauthorizedLinkProcessingException unauthorizedLinkProcessingException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: unauthorizedLinkProcessingException);
            }
            catch (NullLinkProcessingException nullLinkProcessingException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullLinkProcessingException);
            }
            catch (NotFoundLinkProcessingException notFoundLinkProcessingException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: notFoundLinkProcessingException);
            }
            catch (InvalidLinkProcessingException invalidLinkProcessingException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidLinkProcessingException);
            }
            catch (LinkValidationException linkValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(exception: linkValidationException);
            }
            catch (LinkDependencyValidationException linkDependencyValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(
                    exception: linkDependencyValidationException);
            }
            catch (LinkDependencyException linkDependencyException)
            {
                throw await CreateAndLogDependencyExceptionAsync(exception: linkDependencyException);
            }
            catch (LinkServiceException linkServiceException)
            {
                throw await CreateAndLogDependencyExceptionAsync(exception: linkServiceException);
            }
            catch (Exception exception)
            {
                var failedLinkProcessingServiceException =
                    new FailedLinkProcessingServiceException(
                        message: "Failed link processing service error occurred, " +
                            "please contact support.",
                        innerException: exception,
                        data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(
                    exception: failedLinkProcessingServiceException);
            }
        }

        private async ValueTask<IQueryable<Link>> TryCatch(
            ReturningLinksFunction returningLinksFunction)
        {
            try
            {
                return await returningLinksFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutLinkProcessingException =
                    new TimeoutLinkProcessingException(
                        message: "Failed link processing timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(
                    exception: timeoutLinkProcessingException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidLinkProcessingException invalidLinkProcessingException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidLinkProcessingException);
            }
            catch (LinkValidationException linkValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(exception: linkValidationException);
            }
            catch (LinkDependencyValidationException linkDependencyValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(
                    exception: linkDependencyValidationException);
            }
            catch (LinkDependencyException linkDependencyException)
            {
                throw await CreateAndLogDependencyExceptionAsync(exception: linkDependencyException);
            }
            catch (LinkServiceException linkServiceException)
            {
                throw await CreateAndLogDependencyExceptionAsync(exception: linkServiceException);
            }
            catch (Exception exception)
            {
                var failedLinkProcessingServiceException =
                    new FailedLinkProcessingServiceException(
                        message: "Failed link processing service error occurred, " +
                            "please contact support.",
                        innerException: exception,
                        data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(
                    exception: failedLinkProcessingServiceException);
            }
        }

        private async ValueTask<LinkProcessingValidationException> CreateAndLogValidationExceptionAsync(
            Xeption exception)
        {
            var linkProcessingValidationException =
                new LinkProcessingValidationException(
                    message: "Link processing validation error occurred, fix the errors and try again.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(exception: linkProcessingValidationException);

            return linkProcessingValidationException;
        }

        private async ValueTask<LinkProcessingDependencyValidationException>
            CreateAndLogDependencyValidationExceptionAsync(Xeption exception)
        {
            var linkProcessingDependencyValidationException =
                new LinkProcessingDependencyValidationException(
                    message: "Link processing dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: (exception.InnerException as Xeption)!);

            await this.loggingBroker.LogErrorAsync(
                exception: linkProcessingDependencyValidationException);

            return linkProcessingDependencyValidationException;
        }

        private async ValueTask<LinkProcessingDependencyException> CreateAndLogDependencyExceptionAsync(
            Xeption exception)
        {
            var linkProcessingDependencyException =
                new LinkProcessingDependencyException(
                    message: "Link processing dependency error occurred, contact support.",
                    innerException: (exception.InnerException as Xeption)!);

            await this.loggingBroker.LogErrorAsync(exception: linkProcessingDependencyException);

            return linkProcessingDependencyException;
        }

        // Intentionally a named twin of CreateAndLogDependencyExceptionAsync (same wrapper,
        // same LogError): timeouts categorize as a non-critical dependency failure, but keep
        // their own seam so the call site reads as a timeout and the behavior can diverge
        // later without touching generic dependency handling.
        private async ValueTask<LinkProcessingDependencyException>
            CreateAndLogTimeoutDependencyExceptionAsync(Xeption exception)
        {
            var linkProcessingDependencyException =
                new LinkProcessingDependencyException(
                    message: "Link processing dependency error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(exception: linkProcessingDependencyException);

            return linkProcessingDependencyException;
        }

        private async ValueTask<LinkProcessingServiceException> CreateAndLogServiceExceptionAsync(
            Xeption exception)
        {
            var linkProcessingServiceException =
                new LinkProcessingServiceException(
                    message: "Link processing service error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(exception: linkProcessingServiceException);

            return linkProcessingServiceException;
        }
    }
}
