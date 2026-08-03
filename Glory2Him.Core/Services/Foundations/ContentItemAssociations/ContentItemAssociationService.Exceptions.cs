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
using Glory2Him.Core.Models.Foundations.ContentItemAssociations;
using Glory2Him.Core.Models.Foundations.ContentItemAssociations.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xeptions;

namespace Glory2Him.Core.Services.Foundations.ContentItemAssociations
{
    internal partial class ContentItemAssociationService
    {
        private delegate ValueTask<ContentItemAssociation> ReturningContentItemAssociationFunction();

        private delegate ValueTask<IQueryable<ContentItemAssociation>>
            ReturningContentItemAssociationsFunction();

        private delegate ValueTask<EventEnvelope<ContentItemAssociation>?>
            ReturningContentItemAssociationEventEnvelopeFunction();

        // The event-path wrapper: categorizes failures with the same taxonomy as the
        // non-event TryCatch (so the two entry paths cannot diverge), plus the envelope
        // guard that only exists on this path, and ALWAYS rethrows so the substrate records
        // the delivery as Error and drives retries. Exceptions already categorized by nested
        // service calls pass through unwrapped.
        private async ValueTask<EventEnvelope<ContentItemAssociation>?> TryCatchSubstrate(
            ReturningContentItemAssociationEventEnvelopeFunction
                returningContentItemAssociationEventEnvelopeFunction)
        {
            try
            {
                return await returningContentItemAssociationEventEnvelopeFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutContentItemAssociationException =
                    new TimeoutContentItemAssociationException(
                        message: "Failed content item association timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(
                    exception: timeoutContentItemAssociationException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidContentItemAssociationEventException invalidContentItemAssociationEventException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: invalidContentItemAssociationEventException);
            }
            catch (UnauthorizedContentItemAssociationException unauthorizedContentItemAssociationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: unauthorizedContentItemAssociationException);
            }
            catch (NullContentItemAssociationException nullContentItemAssociationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: nullContentItemAssociationException);
            }
            catch (InvalidContentItemAssociationException invalidContentItemAssociationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: invalidContentItemAssociationException);
            }
            catch (NotFoundContentItemAssociationException notFoundContentItemAssociationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: notFoundContentItemAssociationException);
            }
            catch (ContentItemAssociationValidationException)
            {
                throw;
            }
            catch (ContentItemAssociationDependencyValidationException)
            {
                throw;
            }
            catch (ContentItemAssociationDependencyException)
            {
                throw;
            }
            catch (ContentItemAssociationServiceException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageContentItemAssociationException =
                    new FailedStorageContentItemAssociationException(
                        message: "Failed content item association storage error occurred, contact support.",
                        innerException: sqlException,
                        data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(
                    exception: failedStorageContentItemAssociationException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsContentItemAssociationException =
                    new AlreadyExistsContentItemAssociationException(
                        message: "Content item association already exists with the same Id.",
                        innerException: duplicateKeyException,
                        data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(
                    alreadyExistsContentItemAssociationException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidContentItemAssociationReferenceException =
                    new InvalidContentItemAssociationReferenceException(
                        message: "Invalid content item association reference error occurred.",
                        innerException: foreignKeyConstraintConflictException,
                        data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(
                    invalidContentItemAssociationReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedContentItemAssociationException = new LockedContentItemAssociationException(
                    message: "Locked content item association record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(
                    lockedContentItemAssociationException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageContentItemAssociationException =
                    new FailedStorageContentItemAssociationException(
                        message: "Failed content item association storage error occurred, contact support.",
                        innerException: dbUpdateException,
                        data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(
                    failedStorageContentItemAssociationException);
            }
            catch (Exception exception)
            {
                var failedContentItemAssociationServiceException =
                    new FailedContentItemAssociationServiceException(
                        message: "Failed content item association service error occurred, please contact support.",
                        innerException: exception,
                        data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(
                    failedContentItemAssociationServiceException);
            }
        }

        private async ValueTask<ContentItemAssociation> TryCatch(
            ReturningContentItemAssociationFunction returningContentItemAssociationFunction)
        {
            try
            {
                return await returningContentItemAssociationFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutContentItemAssociationException =
                    new TimeoutContentItemAssociationException(
                        message: "Failed content item association timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(
                    exception: timeoutContentItemAssociationException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UnauthorizedContentItemAssociationException unauthorizedContentItemAssociationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: unauthorizedContentItemAssociationException);
            }
            catch (NullContentItemAssociationException nullContentItemAssociationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: nullContentItemAssociationException);
            }
            catch (InvalidContentItemAssociationException invalidContentItemAssociationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: invalidContentItemAssociationException);
            }
            catch (NotFoundContentItemAssociationException notFoundContentItemAssociationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: notFoundContentItemAssociationException);
            }
            catch (SqlException sqlException)
            {
                var failedStorageContentItemAssociationException =
                    new FailedStorageContentItemAssociationException(
                        message: "Failed content item association storage error occurred, contact support.",
                        innerException: sqlException,
                        data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(
                    exception: failedStorageContentItemAssociationException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsContentItemAssociationException =
                    new AlreadyExistsContentItemAssociationException(
                        message: "Content item association already exists with the same Id.",
                        innerException: duplicateKeyException,
                        data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(
                    alreadyExistsContentItemAssociationException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidContentItemAssociationReferenceException =
                    new InvalidContentItemAssociationReferenceException(
                        message: "Invalid content item association reference error occurred.",
                        innerException: foreignKeyConstraintConflictException,
                        data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(
                    invalidContentItemAssociationReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedContentItemAssociationException = new LockedContentItemAssociationException(
                    message: "Locked content item association record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(
                    lockedContentItemAssociationException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageContentItemAssociationException =
                    new FailedStorageContentItemAssociationException(
                        message: "Failed content item association storage error occurred, contact support.",
                        innerException: dbUpdateException,
                        data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(
                    failedStorageContentItemAssociationException);
            }
            catch (Exception exception)
            {
                var failedContentItemAssociationServiceException =
                    new FailedContentItemAssociationServiceException(
                        message: "Failed content item association service error occurred, please contact support.",
                        innerException: exception,
                        data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(
                    failedContentItemAssociationServiceException);
            }
        }

        private async ValueTask<IQueryable<ContentItemAssociation>> TryCatch(
            ReturningContentItemAssociationsFunction returningContentItemAssociationsFunction)
        {
            try
            {
                return await returningContentItemAssociationsFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutContentItemAssociationException =
                    new TimeoutContentItemAssociationException(
                        message: "Failed content item association timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(
                    exception: timeoutContentItemAssociationException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageContentItemAssociationException =
                    new FailedStorageContentItemAssociationException(
                        message: "Failed content item association storage error occurred, contact support.",
                        innerException: sqlException,
                        data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(
                    exception: failedStorageContentItemAssociationException);
            }
            catch (Exception exception)
            {
                var failedContentItemAssociationServiceException =
                    new FailedContentItemAssociationServiceException(
                        message: "Failed content item association service error occurred, please contact support.",
                        innerException: exception,
                        data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(
                    failedContentItemAssociationServiceException);
            }
        }

        private async ValueTask<ContentItemAssociationValidationException>
            CreateAndLogValidationExceptionAsync(Xeption exception)
        {
            var contentItemAssociationValidationException = new ContentItemAssociationValidationException(
                message: "Content item association validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(contentItemAssociationValidationException);

            return contentItemAssociationValidationException;
        }

        private async ValueTask<ContentItemAssociationDependencyException>
            CreateAndLogDependencyExceptionAsync(Xeption exception)
        {
            var contentItemAssociationDependencyException = new ContentItemAssociationDependencyException(
                message: "Content item association dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(contentItemAssociationDependencyException);

            return contentItemAssociationDependencyException;
        }

        // Intentionally a named twin of CreateAndLogDependencyExceptionAsync (same wrapper,
        // same LogError): timeouts categorize as a non-critical dependency failure, but keep
        // their own seam so the call site reads as a timeout and the behavior can diverge
        // later without touching generic dependency handling. Mirrors The Standard's
        // EventHighway EventAddressV2Service.
        private async ValueTask<ContentItemAssociationDependencyException>
            CreateAndLogTimeoutDependencyExceptionAsync(Xeption exception)
        {
            var contentItemAssociationDependencyException =
                new ContentItemAssociationDependencyException(
                    message: "Content item association dependency error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(contentItemAssociationDependencyException);

            return contentItemAssociationDependencyException;
        }

        private async ValueTask<ContentItemAssociationDependencyException>
            CreateAndLogCriticalDependencyExceptionAsync(Xeption exception)
        {
            var contentItemAssociationDependencyException = new ContentItemAssociationDependencyException(
                message: "Content item association dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogCriticalAsync(contentItemAssociationDependencyException);

            return contentItemAssociationDependencyException;
        }

        private async ValueTask<ContentItemAssociationDependencyValidationException>
            CreateAndLogDependencyValidationExceptionAsync(Xeption exception)
        {
            var contentItemAssociationDependencyValidationException =
                new ContentItemAssociationDependencyValidationException(
                    message: "Content item association dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(contentItemAssociationDependencyValidationException);

            return contentItemAssociationDependencyValidationException;
        }

        private async ValueTask<ContentItemAssociationServiceException>
            CreateAndLogServiceExceptionAsync(Xeption exception)
        {
            var contentItemAssociationServiceException = new ContentItemAssociationServiceException(
                message: "Content item association service error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(contentItemAssociationServiceException);

            return contentItemAssociationServiceException;
        }
    }
}
