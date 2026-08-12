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
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.BibleReferences.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xeptions;

namespace Glory2Him.Core.Services.Foundations.BibleReferences
{
    internal partial class BibleReferenceService
    {
        private delegate ValueTask<BibleReference> ReturningBibleReferenceFunction();
        private delegate ValueTask<IQueryable<BibleReference>> ReturningBibleReferencesFunction();

        private delegate ValueTask<EventEnvelope<BibleReference>?>
            ReturningBibleReferenceEventEnvelopeFunction();

        // The event-path wrapper: categorizes failures with the same taxonomy as the
        // non-event TryCatch (so the two entry paths cannot diverge), plus the envelope
        // guard that only exists on this path, and ALWAYS rethrows so the substrate records
        // the delivery as Error and drives retries. Exceptions already categorized by nested
        // service calls pass through unwrapped.
        private async ValueTask<EventEnvelope<BibleReference>?> TryCatchSubstrate(
            ReturningBibleReferenceEventEnvelopeFunction returningBibleReferenceEventEnvelopeFunction)
        {
            try
            {
                return await returningBibleReferenceEventEnvelopeFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutBibleReferenceException =
                    new TimeoutBibleReferenceException(
                        message: "Failed bible reference timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutBibleReferenceException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidBibleReferenceEventException invalidBibleReferenceEventException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidBibleReferenceEventException);
            }
            catch (UnauthorizedBibleReferenceException unauthorizedBibleReferenceException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: unauthorizedBibleReferenceException);
            }
            catch (NullBibleReferenceException nullBibleReferenceException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullBibleReferenceException);
            }
            catch (InvalidBibleReferenceException invalidBibleReferenceException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidBibleReferenceException);
            }
            catch (NotFoundBibleReferenceException notFoundBibleReferenceException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundBibleReferenceException);
            }
            catch (BibleReferenceValidationException)
            {
                throw;
            }
            catch (BibleReferenceDependencyValidationException)
            {
                throw;
            }
            catch (BibleReferenceDependencyException)
            {
                throw;
            }
            catch (BibleReferenceServiceException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageBibleReferenceException = new FailedStorageBibleReferenceException(
                    message: "Failed bible reference storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(
                    exception: failedStorageBibleReferenceException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsBibleReferenceException = new AlreadyExistsBibleReferenceException(
                    message: "Bible reference already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsBibleReferenceException);
            }
            // A unique-INDEX violation (EF's HasIndex().IsUnique(), and the ProcessedEvents
            // dedup index) arrives as a type that does NOT derive from DuplicateKeyException,
            // so the clause above misses it; without this it falls through to the general
            // handler and mis-reports a business-key collision as "our code is broken".
            catch (DuplicateKeyWithUniqueIndexException duplicateKeyWithUniqueIndexException)
            {
                var alreadyExistsBibleReferenceException = new AlreadyExistsBibleReferenceException(
                    message: "Bible reference already exists, "
                        + "a uniqueness rule rejected the write.",
                    innerException: duplicateKeyWithUniqueIndexException,
                    data: duplicateKeyWithUniqueIndexException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(
                    alreadyExistsBibleReferenceException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidBibleReferenceReferenceException = new InvalidBibleReferenceReferenceException(
                    message: "Invalid bible reference reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidBibleReferenceReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedBibleReferenceException = new LockedBibleReferenceException(
                    message: "Locked bible reference record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedBibleReferenceException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageBibleReferenceException = new FailedStorageBibleReferenceException(
                    message: "Failed bible reference storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageBibleReferenceException);
            }
            catch (Exception exception)
            {
                var failedBibleReferenceServiceException = new FailedBibleReferenceServiceException(
                    message: "Failed bible reference service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedBibleReferenceServiceException);
            }
        }

        private async ValueTask<BibleReference> TryCatch(
            ReturningBibleReferenceFunction returningBibleReferenceFunction)
        {
            try
            {
                return await returningBibleReferenceFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutBibleReferenceException =
                    new TimeoutBibleReferenceException(
                        message: "Failed bible reference timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutBibleReferenceException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UnauthorizedBibleReferenceException unauthorizedBibleReferenceException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: unauthorizedBibleReferenceException);
            }
            catch (NullBibleReferenceException nullBibleReferenceException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullBibleReferenceException);
            }
            catch (InvalidBibleReferenceException invalidBibleReferenceException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidBibleReferenceException);
            }
            catch (NotFoundBibleReferenceException notFoundBibleReferenceException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundBibleReferenceException);
            }
            catch (SqlException sqlException)
            {
                var failedStorageBibleReferenceException = new FailedStorageBibleReferenceException(
                    message: "Failed bible reference storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(
                    exception: failedStorageBibleReferenceException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsBibleReferenceException = new AlreadyExistsBibleReferenceException(
                    message: "Bible reference already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsBibleReferenceException);
            }
            // A unique-INDEX violation (EF's HasIndex().IsUnique(), and the ProcessedEvents
            // dedup index) arrives as a type that does NOT derive from DuplicateKeyException,
            // so the clause above misses it; without this it falls through to the general
            // handler and mis-reports a business-key collision as "our code is broken".
            catch (DuplicateKeyWithUniqueIndexException duplicateKeyWithUniqueIndexException)
            {
                var alreadyExistsBibleReferenceException = new AlreadyExistsBibleReferenceException(
                    message: "Bible reference already exists, "
                        + "a uniqueness rule rejected the write.",
                    innerException: duplicateKeyWithUniqueIndexException,
                    data: duplicateKeyWithUniqueIndexException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(
                    alreadyExistsBibleReferenceException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidBibleReferenceReferenceException = new InvalidBibleReferenceReferenceException(
                    message: "Invalid bible reference reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidBibleReferenceReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedBibleReferenceException = new LockedBibleReferenceException(
                    message: "Locked bible reference record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedBibleReferenceException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageBibleReferenceException = new FailedStorageBibleReferenceException(
                    message: "Failed bible reference storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageBibleReferenceException);
            }
            catch (Exception exception)
            {
                var failedBibleReferenceServiceException = new FailedBibleReferenceServiceException(
                    message: "Failed bible reference service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedBibleReferenceServiceException);
            }
        }

        private async ValueTask<IQueryable<BibleReference>> TryCatch(
            ReturningBibleReferencesFunction returningBibleReferencesFunction)
        {
            try
            {
                return await returningBibleReferencesFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutBibleReferenceException =
                    new TimeoutBibleReferenceException(
                        message: "Failed bible reference timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutBibleReferenceException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageBibleReferenceException = new FailedStorageBibleReferenceException(
                    message: "Failed bible reference storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(
                    exception: failedStorageBibleReferenceException);
            }
            catch (Exception exception)
            {
                var failedBibleReferenceServiceException = new FailedBibleReferenceServiceException(
                    message: "Failed bible reference service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedBibleReferenceServiceException);
            }
        }

        private async ValueTask<BibleReferenceValidationException>
            CreateAndLogValidationExceptionAsync(Xeption exception)
        {
            var bibleReferenceValidationException = new BibleReferenceValidationException(
                message: "Bible reference validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(bibleReferenceValidationException);

            return bibleReferenceValidationException;
        }

        private async ValueTask<BibleReferenceDependencyException>
            CreateAndLogDependencyExceptionAsync(Xeption exception)
        {
            var bibleReferenceDependencyException = new BibleReferenceDependencyException(
                message: "Bible reference dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(bibleReferenceDependencyException);

            return bibleReferenceDependencyException;
        }

        // Intentionally a named twin of CreateAndLogDependencyExceptionAsync (same wrapper,
        // same LogError): timeouts categorize as a non-critical dependency failure, but keep
        // their own seam so the call site reads as a timeout and the behavior can diverge
        // later without touching generic dependency handling. Mirrors The Standard's
        // EventHighway EventAddressV2Service.
        private async ValueTask<BibleReferenceDependencyException>
            CreateAndLogTimeoutDependencyExceptionAsync(Xeption exception)
        {
            var bibleReferenceDependencyException =
                new BibleReferenceDependencyException(
                    message: "Bible reference dependency error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(bibleReferenceDependencyException);

            return bibleReferenceDependencyException;
        }

        private async ValueTask<BibleReferenceDependencyException>
            CreateAndLogCriticalDependencyExceptionAsync(Xeption exception)
        {
            var bibleReferenceDependencyException = new BibleReferenceDependencyException(
                message: "Bible reference dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogCriticalAsync(bibleReferenceDependencyException);

            return bibleReferenceDependencyException;
        }

        private async ValueTask<BibleReferenceDependencyValidationException>
            CreateAndLogDependencyValidationExceptionAsync(Xeption exception)
        {
            var bibleReferenceDependencyValidationException = new BibleReferenceDependencyValidationException(
                message: "Bible reference dependency validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(bibleReferenceDependencyValidationException);

            return bibleReferenceDependencyValidationException;
        }

        private async ValueTask<BibleReferenceServiceException> CreateAndLogServiceExceptionAsync(Xeption exception)
        {
            var bibleReferenceServiceException = new BibleReferenceServiceException(
                message: "Bible reference service error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(bibleReferenceServiceException);

            return bibleReferenceServiceException;
        }
    }
}
