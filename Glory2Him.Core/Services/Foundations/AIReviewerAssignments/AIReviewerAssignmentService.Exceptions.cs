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
using EFxceptions.Models.Exceptions;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xeptions;

namespace Glory2Him.Core.Services.Foundations.AIReviewerAssignments
{
    internal partial class AIReviewerAssignmentService
    {
        private delegate ValueTask<AIReviewerAssignment> ReturningAIReviewerAssignmentFunction();

        private delegate ValueTask<AIReviewerAssignment?> ReturningNullableAIReviewerAssignmentFunction();

        private delegate ValueTask<EventEnvelope<AIReviewerAssignment>?>
            ReturningAIReviewerAssignmentEventEnvelopeFunction();

        // The event-path wrapper: categorizes failures with the same taxonomy as the
        // non-event TryCatch (so the two entry paths cannot diverge), plus the envelope
        // guard that only exists on this path, and ALWAYS rethrows so the substrate records
        // the delivery as Error and drives retries. Exceptions already categorized by nested
        // service calls pass through unwrapped.
        private async ValueTask<EventEnvelope<AIReviewerAssignment>?> TryCatchSubstrate(
            ReturningAIReviewerAssignmentEventEnvelopeFunction returningAIReviewerAssignmentEventEnvelopeFunction)
        {
            try
            {
                return await returningAIReviewerAssignmentEventEnvelopeFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutAIReviewerAssignmentException =
                    new TimeoutAIReviewerAssignmentException(
                        message: "Failed AI reviewer assignment timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(
                    exception: timeoutAIReviewerAssignmentException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidAIReviewerAssignmentEventException invalidAIReviewerAssignmentEventException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: invalidAIReviewerAssignmentEventException);
            }
            catch (UnauthorizedAIReviewerAssignmentException unauthorizedAIReviewerAssignmentException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: unauthorizedAIReviewerAssignmentException);
            }
            catch (NullAIReviewerAssignmentException nullAIReviewerAssignmentException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullAIReviewerAssignmentException);
            }
            catch (InvalidAIReviewerAssignmentException invalidAIReviewerAssignmentException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidAIReviewerAssignmentException);
            }
            catch (NotFoundAIReviewerAssignmentException notFoundAIReviewerAssignmentException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundAIReviewerAssignmentException);
            }
            catch (AIReviewerAssignmentValidationException)
            {
                throw;
            }
            catch (AIReviewerAssignmentDependencyValidationException)
            {
                throw;
            }
            catch (AIReviewerAssignmentDependencyException)
            {
                throw;
            }
            catch (AIReviewerAssignmentServiceException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageAIReviewerAssignmentException = new FailedStorageAIReviewerAssignmentException(
                    message: "Failed AI reviewer assignment storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(
                    exception: failedStorageAIReviewerAssignmentException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsAIReviewerAssignmentException = new AlreadyExistsAIReviewerAssignmentException(
                    message: "AI reviewer assignment already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsAIReviewerAssignmentException);
            }
            // A unique-INDEX violation (EF's HasIndex().IsUnique(), and the ProcessedEvents
            // dedup index) arrives as a type that does NOT derive from DuplicateKeyException,
            // so the clause above misses it; without this it falls through to the general
            // handler and mis-reports a business-key collision as "our code is broken".
            catch (DuplicateKeyWithUniqueIndexException duplicateKeyWithUniqueIndexException)
            {
                var alreadyExistsAIReviewerAssignmentException = new AlreadyExistsAIReviewerAssignmentException(
                    message: "AI reviewer assignment already exists, "
                        + "a uniqueness rule rejected the write.",
                    innerException: duplicateKeyWithUniqueIndexException,
                    data: duplicateKeyWithUniqueIndexException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(
                    alreadyExistsAIReviewerAssignmentException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidAIReviewerAssignmentReferenceException = new InvalidAIReviewerAssignmentReferenceException(
                    message: "Invalid AI reviewer assignment reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidAIReviewerAssignmentReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedAIReviewerAssignmentException = new LockedAIReviewerAssignmentException(
                    message: "Locked AI reviewer assignment record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedAIReviewerAssignmentException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageAIReviewerAssignmentException = new FailedStorageAIReviewerAssignmentException(
                    message: "Failed AI reviewer assignment storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageAIReviewerAssignmentException);
            }
            catch (Exception exception)
            {
                var failedAIReviewerAssignmentServiceException = new FailedAIReviewerAssignmentServiceException(
                    message: "Failed AI reviewer assignment service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedAIReviewerAssignmentServiceException);
            }
        }

        private async ValueTask<AIReviewerAssignment> TryCatch(
            ReturningAIReviewerAssignmentFunction returningAIReviewerAssignmentFunction)
        {
            try
            {
                return await returningAIReviewerAssignmentFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutAIReviewerAssignmentException =
                    new TimeoutAIReviewerAssignmentException(
                        message: "Failed AI reviewer assignment timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(
                    exception: timeoutAIReviewerAssignmentException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UnauthorizedAIReviewerAssignmentException unauthorizedAIReviewerAssignmentException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: unauthorizedAIReviewerAssignmentException);
            }
            catch (NullAIReviewerAssignmentException nullAIReviewerAssignmentException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullAIReviewerAssignmentException);
            }
            catch (InvalidAIReviewerAssignmentException invalidAIReviewerAssignmentException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidAIReviewerAssignmentException);
            }
            catch (NotFoundAIReviewerAssignmentException notFoundAIReviewerAssignmentException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundAIReviewerAssignmentException);
            }
            catch (SqlException sqlException)
            {
                var failedStorageAIReviewerAssignmentException = new FailedStorageAIReviewerAssignmentException(
                    message: "Failed AI reviewer assignment storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(
                    exception: failedStorageAIReviewerAssignmentException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsAIReviewerAssignmentException = new AlreadyExistsAIReviewerAssignmentException(
                    message: "AI reviewer assignment already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsAIReviewerAssignmentException);
            }
            // A unique-INDEX violation (EF's HasIndex().IsUnique(), and the ProcessedEvents
            // dedup index) arrives as a type that does NOT derive from DuplicateKeyException,
            // so the clause above misses it; without this it falls through to the general
            // handler and mis-reports a business-key collision as "our code is broken".
            catch (DuplicateKeyWithUniqueIndexException duplicateKeyWithUniqueIndexException)
            {
                var alreadyExistsAIReviewerAssignmentException = new AlreadyExistsAIReviewerAssignmentException(
                    message: "AI reviewer assignment already exists, "
                        + "a uniqueness rule rejected the write.",
                    innerException: duplicateKeyWithUniqueIndexException,
                    data: duplicateKeyWithUniqueIndexException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(
                    alreadyExistsAIReviewerAssignmentException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidAIReviewerAssignmentReferenceException = new InvalidAIReviewerAssignmentReferenceException(
                    message: "Invalid AI reviewer assignment reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidAIReviewerAssignmentReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedAIReviewerAssignmentException = new LockedAIReviewerAssignmentException(
                    message: "Locked AI reviewer assignment record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedAIReviewerAssignmentException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageAIReviewerAssignmentException = new FailedStorageAIReviewerAssignmentException(
                    message: "Failed AI reviewer assignment storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageAIReviewerAssignmentException);
            }
            catch (Exception exception)
            {
                var failedAIReviewerAssignmentServiceException = new FailedAIReviewerAssignmentServiceException(
                    message: "Failed AI reviewer assignment service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedAIReviewerAssignmentServiceException);
            }
        }

        // The round-keyed "or null" read's taxonomy: it validates its own id (InvalidAIReviewerAssignmentException)
        // and reads storage, but raises none of the write-only catches (DuplicateKey, foreign
        // key, concurrency) and no NotFound — a missing or unauthorized-to-see assignment is
        // this read's ordinary null answer, never an exception (§14.6 rule 12).
        private async ValueTask<AIReviewerAssignment?> TryCatchNullable(
            ReturningNullableAIReviewerAssignmentFunction returningNullableAIReviewerAssignmentFunction)
        {
            try
            {
                return await returningNullableAIReviewerAssignmentFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutAIReviewerAssignmentException =
                    new TimeoutAIReviewerAssignmentException(
                        message: "Failed AI reviewer assignment timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(
                    exception: timeoutAIReviewerAssignmentException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidAIReviewerAssignmentException invalidAIReviewerAssignmentException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidAIReviewerAssignmentException);
            }
            catch (SqlException sqlException)
            {
                var failedStorageAIReviewerAssignmentException = new FailedStorageAIReviewerAssignmentException(
                    message: "Failed AI reviewer assignment storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(
                    exception: failedStorageAIReviewerAssignmentException);
            }
            catch (Exception exception)
            {
                var failedAIReviewerAssignmentServiceException = new FailedAIReviewerAssignmentServiceException(
                    message: "Failed AI reviewer assignment service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedAIReviewerAssignmentServiceException);
            }
        }

        private async ValueTask<AIReviewerAssignmentValidationException> CreateAndLogValidationExceptionAsync(
            Xeption exception)
        {
            var aiReviewerAssignmentValidationException = new AIReviewerAssignmentValidationException(
                message: "AI reviewer assignment validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(aiReviewerAssignmentValidationException);

            return aiReviewerAssignmentValidationException;
        }

        private async ValueTask<AIReviewerAssignmentDependencyException> CreateAndLogDependencyExceptionAsync(
            Xeption exception)
        {
            var aiReviewerAssignmentDependencyException = new AIReviewerAssignmentDependencyException(
                message: "AI reviewer assignment dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(aiReviewerAssignmentDependencyException);

            return aiReviewerAssignmentDependencyException;
        }

        // Intentionally a named twin of CreateAndLogDependencyExceptionAsync (same wrapper,
        // same LogError): timeouts categorize as a non-critical dependency failure, but keep
        // their own seam so the call site reads as a timeout and the behavior can diverge
        // later without touching generic dependency handling. Mirrors The Standard's
        // EventHighway EventAddressV2Service.
        private async ValueTask<AIReviewerAssignmentDependencyException>
            CreateAndLogTimeoutDependencyExceptionAsync(Xeption exception)
        {
            var aiReviewerAssignmentDependencyException =
                new AIReviewerAssignmentDependencyException(
                    message: "AI reviewer assignment dependency error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(aiReviewerAssignmentDependencyException);

            return aiReviewerAssignmentDependencyException;
        }

        private async ValueTask<AIReviewerAssignmentDependencyException>
            CreateAndLogCriticalDependencyExceptionAsync(Xeption exception)
        {
            var aiReviewerAssignmentDependencyException = new AIReviewerAssignmentDependencyException(
                message: "AI reviewer assignment dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogCriticalAsync(aiReviewerAssignmentDependencyException);

            return aiReviewerAssignmentDependencyException;
        }

        private async ValueTask<AIReviewerAssignmentDependencyValidationException>
            CreateAndLogDependencyValidationExceptionAsync(Xeption exception)
        {
            var aiReviewerAssignmentDependencyValidationException =
                new AIReviewerAssignmentDependencyValidationException(
                    message: "AI reviewer assignment dependency validation error occurred, fix the errors and try again.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(aiReviewerAssignmentDependencyValidationException);

            return aiReviewerAssignmentDependencyValidationException;
        }

        private async ValueTask<AIReviewerAssignmentServiceException> CreateAndLogServiceExceptionAsync(
            Xeption exception)
        {
            var aiReviewerAssignmentServiceException = new AIReviewerAssignmentServiceException(
                message: "AI reviewer assignment service error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(aiReviewerAssignmentServiceException);

            return aiReviewerAssignmentServiceException;
        }
    }
}
