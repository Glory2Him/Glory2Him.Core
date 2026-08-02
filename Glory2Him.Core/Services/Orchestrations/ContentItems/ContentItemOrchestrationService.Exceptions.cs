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
using Glory2Him.Core.Models.Orchestrations.ContentItems.Exceptions;
using Xeptions;

namespace Glory2Him.Core.Services.Orchestrations.ContentItems
{
    internal partial class ContentItemOrchestrationService
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

                var timeoutContentItemOrchestrationException =
                    new TimeoutContentItemOrchestrationException(
                        message: "Failed content item orchestration timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(
                    exception: timeoutContentItemOrchestrationException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidContentItemOrchestrationEventException invalidContentItemOrchestrationEventException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: invalidContentItemOrchestrationEventException);
            }
            catch (UnauthorizedContentItemOrchestrationException unauthorizedContentItemOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: unauthorizedContentItemOrchestrationException);
            }
            catch (NullContentItemOrchestrationException nullContentItemOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullContentItemOrchestrationException);
            }
            catch (NotFoundContentItemOrchestrationException notFoundContentItemOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: notFoundContentItemOrchestrationException);
            }
            catch (InvalidContentItemOrchestrationException invalidContentItemOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidContentItemOrchestrationException);
            }
            catch (AlreadyExistsContentItemOrchestrationException alreadyExistsContentItemOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: alreadyExistsContentItemOrchestrationException);
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
                var failedContentItemOrchestrationServiceException =
                    new FailedContentItemOrchestrationServiceException(
                        message: "Failed content item orchestration service error occurred, " +
                            "please contact support.",
                        innerException: exception,
                        data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(
                    exception: failedContentItemOrchestrationServiceException);
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

                var timeoutContentItemOrchestrationException =
                    new TimeoutContentItemOrchestrationException(
                        message: "Failed content item orchestration timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(
                    exception: timeoutContentItemOrchestrationException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UnauthorizedContentItemOrchestrationException unauthorizedContentItemOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: unauthorizedContentItemOrchestrationException);
            }
            catch (NullContentItemOrchestrationException nullContentItemOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullContentItemOrchestrationException);
            }
            catch (NotFoundContentItemOrchestrationException notFoundContentItemOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: notFoundContentItemOrchestrationException);
            }
            catch (InvalidContentItemOrchestrationException invalidContentItemOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidContentItemOrchestrationException);
            }
            catch (AlreadyExistsContentItemOrchestrationException alreadyExistsContentItemOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: alreadyExistsContentItemOrchestrationException);
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
                var failedContentItemOrchestrationServiceException =
                    new FailedContentItemOrchestrationServiceException(
                        message: "Failed content item orchestration service error occurred, " +
                            "please contact support.",
                        innerException: exception,
                        data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(
                    exception: failedContentItemOrchestrationServiceException);
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

                var timeoutContentItemOrchestrationException =
                    new TimeoutContentItemOrchestrationException(
                        message: "Failed content item orchestration timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(
                    exception: timeoutContentItemOrchestrationException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidContentItemOrchestrationException invalidContentItemOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidContentItemOrchestrationException);
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
                var failedContentItemOrchestrationServiceException =
                    new FailedContentItemOrchestrationServiceException(
                        message: "Failed content item orchestration service error occurred, " +
                            "please contact support.",
                        innerException: exception,
                        data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(
                    exception: failedContentItemOrchestrationServiceException);
            }
        }

        private async ValueTask<ContentItemOrchestrationValidationException> CreateAndLogValidationExceptionAsync(
            Xeption exception)
        {
            var contentItemOrchestrationValidationException =
                new ContentItemOrchestrationValidationException(
                    message: "Content item orchestration validation error occurred, fix the errors and try again.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(exception: contentItemOrchestrationValidationException);

            return contentItemOrchestrationValidationException;
        }

        private async ValueTask<ContentItemOrchestrationDependencyValidationException>
            CreateAndLogDependencyValidationExceptionAsync(Xeption exception)
        {
            var contentItemOrchestrationDependencyValidationException =
                new ContentItemOrchestrationDependencyValidationException(
                    message: "Content item orchestration dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: (exception.InnerException as Xeption)!);

            await this.loggingBroker.LogErrorAsync(
                exception: contentItemOrchestrationDependencyValidationException);

            return contentItemOrchestrationDependencyValidationException;
        }

        private async ValueTask<ContentItemOrchestrationDependencyException> CreateAndLogDependencyExceptionAsync(
            Xeption exception)
        {
            var contentItemOrchestrationDependencyException =
                new ContentItemOrchestrationDependencyException(
                    message: "Content item orchestration dependency error occurred, contact support.",
                    innerException: (exception.InnerException as Xeption)!);

            await this.loggingBroker.LogErrorAsync(exception: contentItemOrchestrationDependencyException);

            return contentItemOrchestrationDependencyException;
        }

        // Intentionally a named twin of CreateAndLogDependencyExceptionAsync (same wrapper,
        // same LogError): timeouts categorize as a non-critical dependency failure, but keep
        // their own seam so the call site reads as a timeout and the behavior can diverge
        // later without touching generic dependency handling.
        private async ValueTask<ContentItemOrchestrationDependencyException>
            CreateAndLogTimeoutDependencyExceptionAsync(Xeption exception)
        {
            var contentItemOrchestrationDependencyException =
                new ContentItemOrchestrationDependencyException(
                    message: "Content item orchestration dependency error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(exception: contentItemOrchestrationDependencyException);

            return contentItemOrchestrationDependencyException;
        }

        private async ValueTask<ContentItemOrchestrationServiceException> CreateAndLogServiceExceptionAsync(
            Xeption exception)
        {
            var contentItemOrchestrationServiceException =
                new ContentItemOrchestrationServiceException(
                    message: "Content item orchestration service error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(exception: contentItemOrchestrationServiceException);

            return contentItemOrchestrationServiceException;
        }
    }
}
