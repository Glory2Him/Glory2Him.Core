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
using EFxceptions.Models.Exceptions;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xeptions;

namespace Glory2Him.Core.Services.Foundations.ContentItems
{
    internal partial class ContentItemService
    {
        private delegate ValueTask<ContentItem> ReturningContentItemFunction();
        private delegate ValueTask<IQueryable<ContentItem>> ReturningContentItemsFunction();

        private delegate ValueTask<bool> ReturningBooleanFunction();

        private delegate ValueTask<EventEnvelope<ContentItem>?>
            ReturningContentItemEventEnvelopeFunction();

        // The event-path wrapper: categorizes failures with the same taxonomy as the
        // non-event TryCatch (so the two entry paths cannot diverge), plus the envelope
        // guard that only exists on this path, and ALWAYS rethrows so the substrate records
        // the delivery as Error and drives retries. Exceptions already categorized by nested
        // service calls pass through unwrapped.
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

                var timeoutContentItemException =
                    new TimeoutContentItemException(
                        message: "Failed content item timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutContentItemException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidContentItemEventException invalidContentItemEventException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidContentItemEventException);
            }
            catch (UnauthorizedContentItemException unauthorizedContentItemException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: unauthorizedContentItemException);
            }
            catch (NullContentItemException nullContentItemException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullContentItemException);
            }
            catch (InvalidContentItemException invalidContentItemException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidContentItemException);
            }
            catch (NotFoundContentItemException notFoundContentItemException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundContentItemException);
            }
            catch (ContentItemValidationException)
            {
                throw;
            }
            catch (ContentItemDependencyValidationException)
            {
                throw;
            }
            catch (ContentItemDependencyException)
            {
                throw;
            }
            catch (ContentItemServiceException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageContentItemException = new FailedStorageContentItemException(
                    message: "Failed content item storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageContentItemException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsContentItemException = new AlreadyExistsContentItemException(
                    message: "Content item already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(exception: alreadyExistsContentItemException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidContentItemReferenceException = new InvalidContentItemReferenceException(
                    message: "Invalid content item reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(
                    exception: invalidContentItemReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedContentItemException = new LockedContentItemException(
                    message: "Locked content item record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(exception: lockedContentItemException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageContentItemException = new FailedStorageContentItemException(
                    message: "Failed content item storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(exception: failedStorageContentItemException);
            }
            catch (Exception exception)
            {
                var failedContentItemServiceException = new FailedContentItemServiceException(
                    message: "Failed content item service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(exception: failedContentItemServiceException);
            }
        }

        private async ValueTask<ContentItem> TryCatch(ReturningContentItemFunction returningContentItemFunction)
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

                var timeoutContentItemException =
                    new TimeoutContentItemException(
                        message: "Failed content item timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutContentItemException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UnauthorizedContentItemException unauthorizedContentItemException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: unauthorizedContentItemException);
            }
            catch (NullContentItemException nullContentItemException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullContentItemException);
            }
            catch (InvalidContentItemException invalidContentItemException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidContentItemException);
            }
            catch (SqlException sqlException)
            {
                var failedStorageContentItemException = new FailedStorageContentItemException(
                    message: "Failed content item storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageContentItemException);
            }
            catch (NotFoundContentItemException notFoundContentItemException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundContentItemException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsContentItemException = new AlreadyExistsContentItemException(
                    message: "Content item already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(exception: alreadyExistsContentItemException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidContentItemReferenceException = new InvalidContentItemReferenceException(
                    message: "Invalid content item reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(
                    exception: invalidContentItemReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedContentItemException = new LockedContentItemException(
                    message: "Locked content item record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(exception: lockedContentItemException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageContentItemException = new FailedStorageContentItemException(
                    message: "Failed content item storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(exception: failedStorageContentItemException);
            }
            catch (Exception exception)
            {
                var failedContentItemServiceException = new FailedContentItemServiceException(
                    message: "Failed content item service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(exception: failedContentItemServiceException);
            }
        }

        private async ValueTask<bool> TryCatch(ReturningBooleanFunction returningBooleanFunction)
        {
            try
            {
                return await returningBooleanFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutContentItemException =
                    new TimeoutContentItemException(
                        message: "Failed content item timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutContentItemException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UnauthorizedContentItemException unauthorizedContentItemException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: unauthorizedContentItemException);
            }
            catch (InvalidContentItemException invalidContentItemException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidContentItemException);
            }
            catch (SqlException sqlException)
            {
                var failedStorageContentItemException = new FailedStorageContentItemException(
                    message: "Failed content item storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageContentItemException);
            }
            catch (Exception exception)
            {
                var failedContentItemServiceException = new FailedContentItemServiceException(
                    message: "Failed content item service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(exception: failedContentItemServiceException);
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

                var timeoutContentItemException =
                    new TimeoutContentItemException(
                        message: "Failed content item timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutContentItemException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageContentItemException = new FailedStorageContentItemException(
                    message: "Failed content item storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageContentItemException);
            }
            catch (Exception exception)
            {
                var failedContentItemServiceException = new FailedContentItemServiceException(
                    message: "Failed content item service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(exception: failedContentItemServiceException);
            }
        }

        private async ValueTask<ContentItemValidationException> CreateAndLogValidationExceptionAsync(Xeption exception)
        {
            var contentItemValidationException = new ContentItemValidationException(
                message: "Content item validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(exception: contentItemValidationException);

            return contentItemValidationException;
        }

        private async ValueTask<ContentItemDependencyException> CreateAndLogCriticalDependencyExceptionAsync(
            Xeption exception)
        {
            var contentItemDependencyException = new ContentItemDependencyException(
                message: "Content item dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogCriticalAsync(exception: contentItemDependencyException);

            return contentItemDependencyException;
        }

        // Intentionally a named twin of CreateAndLogDependencyExceptionAsync (same wrapper,
        // same LogError): timeouts categorize as a non-critical dependency failure, but keep
        // their own seam so the call site reads as a timeout and the behavior can diverge
        // later without touching generic dependency handling. Mirrors The Standard's
        // EventHighway EventAddressV2Service.
        private async ValueTask<ContentItemDependencyException>
            CreateAndLogTimeoutDependencyExceptionAsync(Xeption exception)
        {
            var contentItemDependencyException =
                new ContentItemDependencyException(
                    message: "Content item dependency error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(exception: contentItemDependencyException);

            return contentItemDependencyException;
        }

        private async ValueTask<ContentItemDependencyValidationException> CreateAndLogDependencyValidationExceptionAsync(
            Xeption exception)
        {
            var contentItemDependencyValidationException = new ContentItemDependencyValidationException(
                message: "Content item dependency validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(exception: contentItemDependencyValidationException);

            return contentItemDependencyValidationException;
        }

        private async ValueTask<ContentItemDependencyException> CreateAndLogDependencyExceptionAsync(Xeption exception)
        {
            var contentItemDependencyException = new ContentItemDependencyException(
                message: "Content item dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(exception: contentItemDependencyException);

            return contentItemDependencyException;
        }

        private async ValueTask<ContentItemServiceException> CreateAndLogServiceExceptionAsync(Xeption exception)
        {
            var contentItemServiceException = new ContentItemServiceException(
                message: "Content item service error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(exception: contentItemServiceException);

            return contentItemServiceException;
        }
    }
}
