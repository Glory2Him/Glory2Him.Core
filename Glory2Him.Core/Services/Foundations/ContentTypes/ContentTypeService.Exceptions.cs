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
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Glory2Him.Core.Models.Foundations.ContentTypes.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xeptions;

namespace Glory2Him.Core.Services.Foundations.ContentTypes
{
    public partial class ContentTypeService
    {
        private delegate ValueTask<ContentType> ReturningContentTypeFunction();
        private delegate ValueTask<IQueryable<ContentType>> ReturningContentTypesFunction();

        private delegate ValueTask<EventEnvelope<ContentType>?>
            ReturningContentTypeEventEnvelopeFunction();

        // The event-path wrapper: categorizes failures with the same taxonomy as the
        // non-event TryCatch (so the two entry paths cannot diverge), plus the envelope
        // guard that only exists on this path, and ALWAYS rethrows so the substrate records
        // the delivery as Error and drives retries. Exceptions already categorized by nested
        // service calls pass through unwrapped.
        private async ValueTask<EventEnvelope<ContentType>?> TryCatchSubstrate(
            ReturningContentTypeEventEnvelopeFunction returningContentTypeEventEnvelopeFunction)
        {
            try
            {
                return await returningContentTypeEventEnvelopeFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutContentTypeException =
                    new TimeoutContentTypeException(
                        message: "Failed content type timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutContentTypeException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidContentTypeEventException invalidContentTypeEventException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidContentTypeEventException);
            }
            catch (NullContentTypeException nullContentTypeException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullContentTypeException);
            }
            catch (InvalidContentTypeException invalidContentTypeException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidContentTypeException);
            }
            catch (NotFoundContentTypeException notFoundContentTypeException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundContentTypeException);
            }
            catch (ContentTypeValidationException)
            {
                throw;
            }
            catch (ContentTypeDependencyValidationException)
            {
                throw;
            }
            catch (ContentTypeDependencyException)
            {
                throw;
            }
            catch (ContentTypeServiceException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageContentTypeException = new FailedStorageContentTypeException(
                    message: "Failed content type storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageContentTypeException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsContentTypeException = new AlreadyExistsContentTypeException(
                    message: "Content type already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsContentTypeException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidContentTypeReferenceException = new InvalidContentTypeReferenceException(
                    message: "Invalid content type reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidContentTypeReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedContentTypeException = new LockedContentTypeException(
                    message: "Locked content type record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedContentTypeException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageContentTypeException = new FailedStorageContentTypeException(
                    message: "Failed content type storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageContentTypeException);
            }
            catch (Exception exception)
            {
                var failedContentTypeServiceException = new FailedContentTypeServiceException(
                    message: "Failed content type service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedContentTypeServiceException);
            }
        }

        private async ValueTask<ContentType> TryCatch(ReturningContentTypeFunction returningContentTypeFunction)
        {
            try
            {
                return await returningContentTypeFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutContentTypeException =
                    new TimeoutContentTypeException(
                        message: "Failed content type timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutContentTypeException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (NullContentTypeException nullContentTypeException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullContentTypeException);
            }
            catch (InvalidContentTypeException invalidContentTypeException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidContentTypeException);
            }
            catch (NotFoundContentTypeException notFoundContentTypeException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundContentTypeException);
            }
            catch (SqlException sqlException)
            {
                var failedStorageContentTypeException = new FailedStorageContentTypeException(
                    message: "Failed content type storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageContentTypeException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsContentTypeException = new AlreadyExistsContentTypeException(
                    message: "Content type already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsContentTypeException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidContentTypeReferenceException = new InvalidContentTypeReferenceException(
                    message: "Invalid content type reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidContentTypeReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedContentTypeException = new LockedContentTypeException(
                    message: "Locked content type record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedContentTypeException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageContentTypeException = new FailedStorageContentTypeException(
                    message: "Failed content type storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageContentTypeException);
            }
            catch (Exception exception)
            {
                var failedContentTypeServiceException = new FailedContentTypeServiceException(
                    message: "Failed content type service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedContentTypeServiceException);
            }
        }

        private async ValueTask<IQueryable<ContentType>> TryCatch(
            ReturningContentTypesFunction returningContentTypesFunction)
        {
            try
            {
                return await returningContentTypesFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutContentTypeException =
                    new TimeoutContentTypeException(
                        message: "Failed content type timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutContentTypeException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageContentTypeException = new FailedStorageContentTypeException(
                    message: "Failed content type storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageContentTypeException);
            }
            catch (Exception exception)
            {
                var failedContentTypeServiceException = new FailedContentTypeServiceException(
                    message: "Failed content type service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedContentTypeServiceException);
            }
        }

        private async ValueTask<ContentTypeValidationException> CreateAndLogValidationExceptionAsync(Xeption exception)
        {
            var contentTypeValidationException = new ContentTypeValidationException(
                message: "Content type validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(contentTypeValidationException);

            return contentTypeValidationException;
        }

        private async ValueTask<ContentTypeDependencyException> CreateAndLogDependencyExceptionAsync(Xeption exception)
        {
            var contentTypeDependencyException = new ContentTypeDependencyException(
                message: "Content type dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(contentTypeDependencyException);

            return contentTypeDependencyException;
        }

        // Intentionally a named twin of CreateAndLogDependencyExceptionAsync (same wrapper,
        // same LogError): timeouts categorize as a non-critical dependency failure, but keep
        // their own seam so the call site reads as a timeout and the behavior can diverge
        // later without touching generic dependency handling. Mirrors The Standard's
        // EventHighway EventAddressV2Service.
        private async ValueTask<ContentTypeDependencyException>
            CreateAndLogTimeoutDependencyExceptionAsync(Xeption exception)
        {
            var contentTypeDependencyException =
                new ContentTypeDependencyException(
                    message: "Content type dependency error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(contentTypeDependencyException);

            return contentTypeDependencyException;
        }

        private async ValueTask<ContentTypeDependencyException>
            CreateAndLogCriticalDependencyExceptionAsync(Xeption exception)
        {
            var contentTypeDependencyException = new ContentTypeDependencyException(
                message: "Content type dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogCriticalAsync(contentTypeDependencyException);

            return contentTypeDependencyException;
        }

        private async ValueTask<ContentTypeDependencyValidationException> CreateAndLogDependencyValidationExceptionAsync(
            Xeption exception)
        {
            var contentTypeDependencyValidationException = new ContentTypeDependencyValidationException(
                message: "Content type dependency validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(contentTypeDependencyValidationException);

            return contentTypeDependencyValidationException;
        }

        private async ValueTask<ContentTypeServiceException> CreateAndLogServiceExceptionAsync(Xeption exception)
        {
            var contentTypeServiceException = new ContentTypeServiceException(
                message: "Content type service error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(contentTypeServiceException);

            return contentTypeServiceException;
        }
    }
}
