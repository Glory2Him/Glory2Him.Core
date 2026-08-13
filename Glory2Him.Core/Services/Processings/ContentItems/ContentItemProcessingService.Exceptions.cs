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
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Processings.ContentItems.Exceptions;
using Xeptions;

namespace Glory2Him.Core.Services.Processings.ContentItems
{
    internal partial class ContentItemProcessingService
    {
        private delegate ValueTask<ContentItem> ReturningContentItemFunction();

        private delegate ValueTask<IQueryable<ContentItem>> ReturningContentItemsFunction();

        private delegate ValueTask<EventEnvelope<ContentItem>?> ReturningContentItemEventEnvelopeFunction();

        private async ValueTask<EventEnvelope<ContentItem>?> TryCatchSubstrate(
            ReturningContentItemEventEnvelopeFunction returningContentItemEventEnvelopeFunction)
        {
            try
            {
                return await returningContentItemEventEnvelopeFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutContentItemProcessingException =
                    new TimeoutContentItemProcessingException(
                        message: "Failed content item processing timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(
                    exception: timeoutContentItemProcessingException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidContentItemProcessingEventException invalidContentItemProcessingEventException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: invalidContentItemProcessingEventException);
            }
            catch (UnauthorizedContentItemProcessingException unauthorizedContentItemProcessingException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: unauthorizedContentItemProcessingException);
            }
            catch (NullContentItemProcessingException nullContentItemProcessingException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullContentItemProcessingException);
            }
            catch (NotFoundContentItemProcessingException notFoundContentItemProcessingException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: notFoundContentItemProcessingException);
            }
            catch (InvalidContentItemProcessingException invalidContentItemProcessingException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidContentItemProcessingException);
            }
            catch (AlreadyExistsContentItemProcessingException alreadyExistsContentItemProcessingException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: alreadyExistsContentItemProcessingException);
            }
            catch (ContentItemValidationException contentItemValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(exception: contentItemValidationException);
            }
            catch (ContentItemDependencyValidationException contentItemDependencyValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(
                    exception: contentItemDependencyValidationException);
            }
            catch (ContentItemDependencyException contentItemDependencyException)
            {
                throw await CreateAndLogDependencyExceptionAsync(exception: contentItemDependencyException);
            }
            catch (ContentItemServiceException contentItemServiceException)
            {
                throw await CreateAndLogDependencyExceptionAsync(exception: contentItemServiceException);
            }
            catch (Exception exception)
            {
                var failedContentItemProcessingServiceException =
                    new FailedContentItemProcessingServiceException(
                        message: "Failed content item processing service error occurred, " +
                            "please contact support.",
                        innerException: exception,
                        data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(
                    exception: failedContentItemProcessingServiceException);
            }
        }

        private async ValueTask<ContentItem> TryCatch(
            ReturningContentItemFunction returningContentItemFunction)
        {
            try
            {
                return await returningContentItemFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutContentItemProcessingException =
                    new TimeoutContentItemProcessingException(
                        message: "Failed content item processing timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(
                    exception: timeoutContentItemProcessingException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UnauthorizedContentItemProcessingException unauthorizedContentItemProcessingException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: unauthorizedContentItemProcessingException);
            }
            catch (NullContentItemProcessingException nullContentItemProcessingException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullContentItemProcessingException);
            }
            catch (NotFoundContentItemProcessingException notFoundContentItemProcessingException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: notFoundContentItemProcessingException);
            }
            catch (InvalidContentItemProcessingException invalidContentItemProcessingException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidContentItemProcessingException);
            }
            catch (AlreadyExistsContentItemProcessingException alreadyExistsContentItemProcessingException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: alreadyExistsContentItemProcessingException);
            }
            catch (ContentItemValidationException contentItemValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(exception: contentItemValidationException);
            }
            catch (ContentItemDependencyValidationException contentItemDependencyValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(
                    exception: contentItemDependencyValidationException);
            }
            catch (ContentItemDependencyException contentItemDependencyException)
            {
                throw await CreateAndLogDependencyExceptionAsync(exception: contentItemDependencyException);
            }
            catch (ContentItemServiceException contentItemServiceException)
            {
                throw await CreateAndLogDependencyExceptionAsync(exception: contentItemServiceException);
            }
            catch (Exception exception)
            {
                var failedContentItemProcessingServiceException =
                    new FailedContentItemProcessingServiceException(
                        message: "Failed content item processing service error occurred, " +
                            "please contact support.",
                        innerException: exception,
                        data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(
                    exception: failedContentItemProcessingServiceException);
            }
        }

        private async ValueTask<IQueryable<ContentItem>> TryCatch(
            ReturningContentItemsFunction returningContentItemsFunction)
        {
            try
            {
                return await returningContentItemsFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutContentItemProcessingException =
                    new TimeoutContentItemProcessingException(
                        message: "Failed content item processing timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(
                    exception: timeoutContentItemProcessingException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidContentItemProcessingException invalidContentItemProcessingException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidContentItemProcessingException);
            }
            catch (ContentItemValidationException contentItemValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(exception: contentItemValidationException);
            }
            catch (ContentItemDependencyValidationException contentItemDependencyValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(
                    exception: contentItemDependencyValidationException);
            }
            catch (ContentItemDependencyException contentItemDependencyException)
            {
                throw await CreateAndLogDependencyExceptionAsync(exception: contentItemDependencyException);
            }
            catch (ContentItemServiceException contentItemServiceException)
            {
                throw await CreateAndLogDependencyExceptionAsync(exception: contentItemServiceException);
            }
            catch (Exception exception)
            {
                var failedContentItemProcessingServiceException =
                    new FailedContentItemProcessingServiceException(
                        message: "Failed content item processing service error occurred, " +
                            "please contact support.",
                        innerException: exception,
                        data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(
                    exception: failedContentItemProcessingServiceException);
            }
        }

        private async ValueTask<ContentItemProcessingValidationException> CreateAndLogValidationExceptionAsync(
            Xeption exception)
        {
            var contentItemProcessingValidationException =
                new ContentItemProcessingValidationException(
                    message: "Content item processing validation error occurred, fix the errors and try again.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(exception: contentItemProcessingValidationException);

            return contentItemProcessingValidationException;
        }

        private async ValueTask<ContentItemProcessingDependencyValidationException>
            CreateAndLogDependencyValidationExceptionAsync(Xeption exception)
        {
            var contentItemProcessingDependencyValidationException =
                new ContentItemProcessingDependencyValidationException(
                    message: "Content item processing dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: (exception.InnerException as Xeption)!);

            await this.loggingBroker.LogErrorAsync(
                exception: contentItemProcessingDependencyValidationException);

            return contentItemProcessingDependencyValidationException;
        }

        private async ValueTask<ContentItemProcessingDependencyException> CreateAndLogDependencyExceptionAsync(
            Xeption exception)
        {
            var contentItemProcessingDependencyException =
                new ContentItemProcessingDependencyException(
                    message: "Content item processing dependency error occurred, contact support.",
                    innerException: (exception.InnerException as Xeption)!);

            await this.loggingBroker.LogErrorAsync(exception: contentItemProcessingDependencyException);

            return contentItemProcessingDependencyException;
        }

        // Intentionally a named twin of CreateAndLogDependencyExceptionAsync (same wrapper,
        // same LogError): timeouts categorize as a non-critical dependency failure, but keep
        // their own seam so the call site reads as a timeout and the behavior can diverge
        // later without touching generic dependency handling.
        private async ValueTask<ContentItemProcessingDependencyException>
            CreateAndLogTimeoutDependencyExceptionAsync(Xeption exception)
        {
            var contentItemProcessingDependencyException =
                new ContentItemProcessingDependencyException(
                    message: "Content item processing dependency error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(exception: contentItemProcessingDependencyException);

            return contentItemProcessingDependencyException;
        }

        private async ValueTask<ContentItemProcessingServiceException> CreateAndLogServiceExceptionAsync(
            Xeption exception)
        {
            var contentItemProcessingServiceException =
                new ContentItemProcessingServiceException(
                    message: "Content item processing service error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(exception: contentItemProcessingServiceException);

            return contentItemProcessingServiceException;
        }
    }
}
