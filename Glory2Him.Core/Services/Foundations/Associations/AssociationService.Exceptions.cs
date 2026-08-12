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
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xeptions;

namespace Glory2Him.Core.Services.Foundations.Associations
{
    internal partial class AssociationService
    {
        private delegate ValueTask<Association> ReturningAssociationFunction();

        private delegate ValueTask<IQueryable<Association>>
            ReturningAssociationsFunction();

        private delegate ValueTask<AssociationPairMatch?>
            ReturningAssociationPairMatchFunction();

        private delegate ValueTask<EventEnvelope<Association>?>
            ReturningAssociationEventEnvelopeFunction();

        // The event-path wrapper: categorizes failures with the same taxonomy as the
        // non-event TryCatch (so the two entry paths cannot diverge), plus the envelope
        // guard that only exists on this path, and ALWAYS rethrows so the substrate records
        // the delivery as Error and drives retries. Exceptions already categorized by nested
        // service calls pass through unwrapped.
        private async ValueTask<EventEnvelope<Association>?> TryCatchSubstrate(
            ReturningAssociationEventEnvelopeFunction
                returningAssociationEventEnvelopeFunction)
        {
            try
            {
                return await returningAssociationEventEnvelopeFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutAssociationException =
                    new TimeoutAssociationException(
                        message: "Failed content item association timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(
                    exception: timeoutAssociationException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidAssociationEventException invalidAssociationEventException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: invalidAssociationEventException);
            }
            catch (UnauthorizedAssociationException unauthorizedAssociationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: unauthorizedAssociationException);
            }
            catch (NullAssociationException nullAssociationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: nullAssociationException);
            }
            catch (InvalidAssociationException invalidAssociationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: invalidAssociationException);
            }
            catch (NotFoundAssociationException notFoundAssociationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: notFoundAssociationException);
            }
            catch (AssociationValidationException)
            {
                throw;
            }
            catch (AssociationDependencyValidationException)
            {
                throw;
            }
            catch (AssociationDependencyException)
            {
                throw;
            }
            catch (AssociationServiceException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageAssociationException =
                    new FailedStorageAssociationException(
                        message: "Failed content item association storage error occurred, contact support.",
                        innerException: sqlException,
                        data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(
                    exception: failedStorageAssociationException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsAssociationException =
                    new AlreadyExistsAssociationException(
                        message: "Content item association already exists with the same Id.",
                        innerException: duplicateKeyException,
                        data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(
                    alreadyExistsAssociationException);
            }
            // A violation of UX_Associations_Pair arrives as its own type, and that type does
            // NOT derive from DuplicateKeyException — EFxceptions derives both directly from
            // Exception. Without this catch the clause above misses it and a duplicate pairing
            // falls through to the general handler, surfacing as a service exception ("our
            // code is broken") rather than the dependency-validation exception a caller can
            // act on. The pair index is what makes this reachable at all.
            catch (DuplicateKeyWithUniqueIndexException duplicateKeyWithUniqueIndexException)
            {
                var alreadyExistsAssociationException =
                    new AlreadyExistsAssociationException(
                        message: "Content item association already exists, "
                            + "a uniqueness rule rejected the write.",
                        innerException: duplicateKeyWithUniqueIndexException,
                        data: duplicateKeyWithUniqueIndexException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(
                    alreadyExistsAssociationException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidAssociationReferenceException =
                    new InvalidAssociationReferenceException(
                        message: "Invalid content item association reference error occurred.",
                        innerException: foreignKeyConstraintConflictException,
                        data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(
                    invalidAssociationReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedAssociationException = new LockedAssociationException(
                    message: "Locked content item association record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(
                    lockedAssociationException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageAssociationException =
                    new FailedStorageAssociationException(
                        message: "Failed content item association storage error occurred, contact support.",
                        innerException: dbUpdateException,
                        data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(
                    failedStorageAssociationException);
            }
            catch (Exception exception)
            {
                var failedAssociationServiceException =
                    new FailedAssociationServiceException(
                        message: "Failed content item association service error occurred, please contact support.",
                        innerException: exception,
                        data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(
                    failedAssociationServiceException);
            }
        }

        private async ValueTask<Association> TryCatch(
            ReturningAssociationFunction returningAssociationFunction)
        {
            try
            {
                return await returningAssociationFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutAssociationException =
                    new TimeoutAssociationException(
                        message: "Failed content item association timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(
                    exception: timeoutAssociationException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UnauthorizedAssociationException unauthorizedAssociationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: unauthorizedAssociationException);
            }
            catch (NullAssociationException nullAssociationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: nullAssociationException);
            }
            catch (InvalidAssociationException invalidAssociationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: invalidAssociationException);
            }
            catch (NotFoundAssociationException notFoundAssociationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: notFoundAssociationException);
            }
            catch (SqlException sqlException)
            {
                var failedStorageAssociationException =
                    new FailedStorageAssociationException(
                        message: "Failed content item association storage error occurred, contact support.",
                        innerException: sqlException,
                        data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(
                    exception: failedStorageAssociationException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsAssociationException =
                    new AlreadyExistsAssociationException(
                        message: "Content item association already exists with the same Id.",
                        innerException: duplicateKeyException,
                        data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(
                    alreadyExistsAssociationException);
            }
            // A violation of UX_Associations_Pair arrives as its own type, and that type does
            // NOT derive from DuplicateKeyException — EFxceptions derives both directly from
            // Exception. Without this catch the clause above misses it and a duplicate pairing
            // falls through to the general handler, surfacing as a service exception ("our
            // code is broken") rather than the dependency-validation exception a caller can
            // act on. The pair index is what makes this reachable at all.
            catch (DuplicateKeyWithUniqueIndexException duplicateKeyWithUniqueIndexException)
            {
                var alreadyExistsAssociationException =
                    new AlreadyExistsAssociationException(
                        message: "Content item association already exists, "
                            + "a uniqueness rule rejected the write.",
                        innerException: duplicateKeyWithUniqueIndexException,
                        data: duplicateKeyWithUniqueIndexException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(
                    alreadyExistsAssociationException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidAssociationReferenceException =
                    new InvalidAssociationReferenceException(
                        message: "Invalid content item association reference error occurred.",
                        innerException: foreignKeyConstraintConflictException,
                        data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(
                    invalidAssociationReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedAssociationException = new LockedAssociationException(
                    message: "Locked content item association record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(
                    lockedAssociationException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageAssociationException =
                    new FailedStorageAssociationException(
                        message: "Failed content item association storage error occurred, contact support.",
                        innerException: dbUpdateException,
                        data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(
                    failedStorageAssociationException);
            }
            catch (Exception exception)
            {
                var failedAssociationServiceException =
                    new FailedAssociationServiceException(
                        message: "Failed content item association service error occurred, please contact support.",
                        innerException: exception,
                        data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(
                    failedAssociationServiceException);
            }
        }

        private async ValueTask<IQueryable<Association>> TryCatch(
            ReturningAssociationsFunction returningAssociationsFunction)
        {
            try
            {
                return await returningAssociationsFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutAssociationException =
                    new TimeoutAssociationException(
                        message: "Failed content item association timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(
                    exception: timeoutAssociationException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageAssociationException =
                    new FailedStorageAssociationException(
                        message: "Failed content item association storage error occurred, contact support.",
                        innerException: sqlException,
                        data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(
                    exception: failedStorageAssociationException);
            }
            catch (Exception exception)
            {
                var failedAssociationServiceException =
                    new FailedAssociationServiceException(
                        message: "Failed content item association service error occurred, please contact support.",
                        innerException: exception,
                        data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(
                    failedAssociationServiceException);
            }
        }

        // The canonical-pair probe both validates its input (null / blocked / invalid) and reads
        // storage, so it needs the validation catches the entity write path has AND the
        // read-style dependency catches — but none of the write-only ones (DuplicateKey, foreign
        // key, concurrency), which a read cannot raise.
        private async ValueTask<AssociationPairMatch?> TryCatch(
            ReturningAssociationPairMatchFunction returningAssociationPairMatchFunction)
        {
            try
            {
                return await returningAssociationPairMatchFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutAssociationException =
                    new TimeoutAssociationException(
                        message: "Failed content item association timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(
                    exception: timeoutAssociationException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UnauthorizedAssociationException unauthorizedAssociationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: unauthorizedAssociationException);
            }
            catch (NullAssociationException nullAssociationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: nullAssociationException);
            }
            catch (InvalidAssociationException invalidAssociationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: invalidAssociationException);
            }
            catch (SqlException sqlException)
            {
                var failedStorageAssociationException =
                    new FailedStorageAssociationException(
                        message: "Failed content item association storage error occurred, contact support.",
                        innerException: sqlException,
                        data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(
                    exception: failedStorageAssociationException);
            }
            catch (Exception exception)
            {
                var failedAssociationServiceException =
                    new FailedAssociationServiceException(
                        message: "Failed content item association service error occurred, please contact support.",
                        innerException: exception,
                        data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(
                    failedAssociationServiceException);
            }
        }

        private async ValueTask<AssociationValidationException>
            CreateAndLogValidationExceptionAsync(Xeption exception)
        {
            var associationValidationException = new AssociationValidationException(
                message: "Content item association validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(associationValidationException);

            return associationValidationException;
        }

        private async ValueTask<AssociationDependencyException>
            CreateAndLogDependencyExceptionAsync(Xeption exception)
        {
            var associationDependencyException = new AssociationDependencyException(
                message: "Content item association dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(associationDependencyException);

            return associationDependencyException;
        }

        // Intentionally a named twin of CreateAndLogDependencyExceptionAsync (same wrapper,
        // same LogError): timeouts categorize as a non-critical dependency failure, but keep
        // their own seam so the call site reads as a timeout and the behavior can diverge
        // later without touching generic dependency handling. Mirrors The Standard's
        // EventHighway EventAddressV2Service.
        private async ValueTask<AssociationDependencyException>
            CreateAndLogTimeoutDependencyExceptionAsync(Xeption exception)
        {
            var associationDependencyException =
                new AssociationDependencyException(
                    message: "Content item association dependency error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(associationDependencyException);

            return associationDependencyException;
        }

        private async ValueTask<AssociationDependencyException>
            CreateAndLogCriticalDependencyExceptionAsync(Xeption exception)
        {
            var associationDependencyException = new AssociationDependencyException(
                message: "Content item association dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogCriticalAsync(associationDependencyException);

            return associationDependencyException;
        }

        private async ValueTask<AssociationDependencyValidationException>
            CreateAndLogDependencyValidationExceptionAsync(Xeption exception)
        {
            var associationDependencyValidationException =
                new AssociationDependencyValidationException(
                    message: "Content item association dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(associationDependencyValidationException);

            return associationDependencyValidationException;
        }

        private async ValueTask<AssociationServiceException>
            CreateAndLogServiceExceptionAsync(Xeption exception)
        {
            var associationServiceException = new AssociationServiceException(
                message: "Content item association service error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(associationServiceException);

            return associationServiceException;
        }
    }
}
