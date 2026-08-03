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
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Reactions.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xeptions;

namespace Glory2Him.Core.Services.Foundations.Reactions
{
    internal partial class ReactionService
    {
        private delegate ValueTask<Reaction> ReturningReactionFunction();
        private delegate ValueTask<IQueryable<Reaction>> ReturningReactionsFunction();

        private delegate ValueTask<EventEnvelope<Reaction>?>
            ReturningReactionEventEnvelopeFunction();

        // The event-path wrapper: categorizes failures with the same taxonomy as the
        // non-event TryCatch (so the two entry paths cannot diverge), plus the envelope
        // guard that only exists on this path, and ALWAYS rethrows so the substrate records
        // the delivery as Error and drives retries. Exceptions already categorized by nested
        // service calls pass through unwrapped.
        private async ValueTask<EventEnvelope<Reaction>?> TryCatchSubstrate(
            ReturningReactionEventEnvelopeFunction returningReactionEventEnvelopeFunction)
        {
            try
            {
                return await returningReactionEventEnvelopeFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutReactionException =
                    new TimeoutReactionException(
                        message: "Failed reaction timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutReactionException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidReactionEventException invalidReactionEventException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidReactionEventException);
            }
            catch (UnauthorizedReactionException unauthorizedReactionException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: unauthorizedReactionException);
            }
            catch (NullReactionException nullReactionException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullReactionException);
            }
            catch (InvalidReactionException invalidReactionException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidReactionException);
            }
            catch (NotFoundReactionException notFoundReactionException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundReactionException);
            }
            catch (ReactionValidationException)
            {
                throw;
            }
            catch (ReactionDependencyValidationException)
            {
                throw;
            }
            catch (ReactionDependencyException)
            {
                throw;
            }
            catch (ReactionServiceException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageReactionException = new FailedStorageReactionException(
                    message: "Failed reaction storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageReactionException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsReactionException = new AlreadyExistsReactionException(
                    message: "Reaction already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsReactionException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidReactionReferenceException = new InvalidReactionReferenceException(
                    message: "Invalid reaction reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidReactionReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedReactionException = new LockedReactionException(
                    message: "Locked reaction record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedReactionException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageReactionException = new FailedStorageReactionException(
                    message: "Failed reaction storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageReactionException);
            }
            catch (Exception exception)
            {
                var failedReactionServiceException = new FailedReactionServiceException(
                    message: "Failed reaction service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedReactionServiceException);
            }
        }

        private async ValueTask<Reaction> TryCatch(ReturningReactionFunction returningReactionFunction)
        {
            try
            {
                return await returningReactionFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutReactionException =
                    new TimeoutReactionException(
                        message: "Failed reaction timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutReactionException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UnauthorizedReactionException unauthorizedReactionException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: unauthorizedReactionException);
            }
            catch (NullReactionException nullReactionException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullReactionException);
            }
            catch (InvalidReactionException invalidReactionException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidReactionException);
            }
            catch (NotFoundReactionException notFoundReactionException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundReactionException);
            }
            catch (SqlException sqlException)
            {
                var failedStorageReactionException = new FailedStorageReactionException(
                    message: "Failed reaction storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageReactionException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsReactionException = new AlreadyExistsReactionException(
                    message: "Reaction already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsReactionException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidReactionReferenceException = new InvalidReactionReferenceException(
                    message: "Invalid reaction reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidReactionReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedReactionException = new LockedReactionException(
                    message: "Locked reaction record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedReactionException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageReactionException = new FailedStorageReactionException(
                    message: "Failed reaction storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageReactionException);
            }
            catch (Exception exception)
            {
                var failedReactionServiceException = new FailedReactionServiceException(
                    message: "Failed reaction service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedReactionServiceException);
            }
        }

        private async ValueTask<IQueryable<Reaction>> TryCatch(
            ReturningReactionsFunction returningReactionsFunction)
        {
            try
            {
                return await returningReactionsFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutReactionException =
                    new TimeoutReactionException(
                        message: "Failed reaction timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutReactionException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageReactionException = new FailedStorageReactionException(
                    message: "Failed reaction storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageReactionException);
            }
            catch (Exception exception)
            {
                var failedReactionServiceException = new FailedReactionServiceException(
                    message: "Failed reaction service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedReactionServiceException);
            }
        }

        private async ValueTask<ReactionValidationException> CreateAndLogValidationExceptionAsync(Xeption exception)
        {
            var reactionValidationException = new ReactionValidationException(
                message: "Reaction validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(reactionValidationException);

            return reactionValidationException;
        }

        private async ValueTask<ReactionDependencyException> CreateAndLogDependencyExceptionAsync(Xeption exception)
        {
            var reactionDependencyException = new ReactionDependencyException(
                message: "Reaction dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(reactionDependencyException);

            return reactionDependencyException;
        }

        // Intentionally a named twin of CreateAndLogDependencyExceptionAsync (same wrapper,
        // same LogError): timeouts categorize as a non-critical dependency failure, but keep
        // their own seam so the call site reads as a timeout and the behavior can diverge
        // later without touching generic dependency handling. Mirrors The Standard's
        // EventHighway EventAddressV2Service.
        private async ValueTask<ReactionDependencyException>
            CreateAndLogTimeoutDependencyExceptionAsync(Xeption exception)
        {
            var reactionDependencyException =
                new ReactionDependencyException(
                    message: "Reaction dependency error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(reactionDependencyException);

            return reactionDependencyException;
        }

        private async ValueTask<ReactionDependencyException>
            CreateAndLogCriticalDependencyExceptionAsync(Xeption exception)
        {
            var reactionDependencyException = new ReactionDependencyException(
                message: "Reaction dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogCriticalAsync(reactionDependencyException);

            return reactionDependencyException;
        }

        private async ValueTask<ReactionDependencyValidationException> CreateAndLogDependencyValidationExceptionAsync(
            Xeption exception)
        {
            var reactionDependencyValidationException = new ReactionDependencyValidationException(
                message: "Reaction dependency validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(reactionDependencyValidationException);

            return reactionDependencyValidationException;
        }

        private async ValueTask<ReactionServiceException> CreateAndLogServiceExceptionAsync(Xeption exception)
        {
            var reactionServiceException = new ReactionServiceException(
                message: "Reaction service error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(reactionServiceException);

            return reactionServiceException;
        }
    }
}
