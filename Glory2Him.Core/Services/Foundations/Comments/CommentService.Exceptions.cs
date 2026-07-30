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
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.Comments.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xeptions;

namespace Glory2Him.Core.Services.Foundations.Comments
{
    internal partial class CommentService
    {
        private delegate ValueTask<Comment> ReturningCommentFunction();
        private delegate ValueTask<IQueryable<Comment>> ReturningCommentsFunction();

        private delegate ValueTask<EventEnvelope<Comment>?>
            ReturningCommentEventEnvelopeFunction();

        // The event-path wrapper: categorizes failures with the same taxonomy as the
        // non-event TryCatch (so the two entry paths cannot diverge), plus the envelope
        // guard that only exists on this path, and ALWAYS rethrows so the substrate records
        // the delivery as Error and drives retries. Exceptions already categorized by nested
        // service calls pass through unwrapped.
        private async ValueTask<EventEnvelope<Comment>?> TryCatchSubstrate(
            ReturningCommentEventEnvelopeFunction returningCommentEventEnvelopeFunction)
        {
            try
            {
                return await returningCommentEventEnvelopeFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutCommentException =
                    new TimeoutCommentException(
                        message: "Failed comment timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutCommentException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidCommentEventException invalidCommentEventException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidCommentEventException);
            }
            catch (NullCommentException nullCommentException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullCommentException);
            }
            catch (InvalidCommentException invalidCommentException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidCommentException);
            }
            catch (NotFoundCommentException notFoundCommentException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundCommentException);
            }
            catch (CommentValidationException)
            {
                throw;
            }
            catch (CommentDependencyValidationException)
            {
                throw;
            }
            catch (CommentDependencyException)
            {
                throw;
            }
            catch (CommentServiceException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageCommentException = new FailedStorageCommentException(
                    message: "Failed comment storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageCommentException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsCommentException = new AlreadyExistsCommentException(
                    message: "Comment already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsCommentException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidCommentReferenceException = new InvalidCommentReferenceException(
                    message: "Invalid comment reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidCommentReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedCommentException = new LockedCommentException(
                    message: "Locked comment record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedCommentException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageCommentException = new FailedStorageCommentException(
                    message: "Failed comment storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageCommentException);
            }
            catch (Exception exception)
            {
                var failedCommentServiceException = new FailedCommentServiceException(
                    message: "Failed comment service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedCommentServiceException);
            }
        }

        private async ValueTask<Comment> TryCatch(ReturningCommentFunction returningCommentFunction)
        {
            try
            {
                return await returningCommentFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutCommentException =
                    new TimeoutCommentException(
                        message: "Failed comment timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutCommentException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (NullCommentException nullCommentException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullCommentException);
            }
            catch (InvalidCommentException invalidCommentException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidCommentException);
            }
            catch (NotFoundCommentException notFoundCommentException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundCommentException);
            }
            catch (SqlException sqlException)
            {
                var failedStorageCommentException = new FailedStorageCommentException(
                    message: "Failed comment storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageCommentException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsCommentException = new AlreadyExistsCommentException(
                    message: "Comment already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsCommentException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidCommentReferenceException = new InvalidCommentReferenceException(
                    message: "Invalid comment reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidCommentReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedCommentException = new LockedCommentException(
                    message: "Locked comment record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedCommentException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageCommentException = new FailedStorageCommentException(
                    message: "Failed comment storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageCommentException);
            }
            catch (Exception exception)
            {
                var failedCommentServiceException = new FailedCommentServiceException(
                    message: "Failed comment service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedCommentServiceException);
            }
        }

        private async ValueTask<IQueryable<Comment>> TryCatch(
            ReturningCommentsFunction returningCommentsFunction)
        {
            try
            {
                return await returningCommentsFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutCommentException =
                    new TimeoutCommentException(
                        message: "Failed comment timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutCommentException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageCommentException = new FailedStorageCommentException(
                    message: "Failed comment storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageCommentException);
            }
            catch (Exception exception)
            {
                var failedCommentServiceException = new FailedCommentServiceException(
                    message: "Failed comment service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedCommentServiceException);
            }
        }

        private async ValueTask<CommentValidationException> CreateAndLogValidationExceptionAsync(Xeption exception)
        {
            var commentValidationException = new CommentValidationException(
                message: "Comment validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(commentValidationException);

            return commentValidationException;
        }

        private async ValueTask<CommentDependencyException> CreateAndLogDependencyExceptionAsync(Xeption exception)
        {
            var commentDependencyException = new CommentDependencyException(
                message: "Comment dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(commentDependencyException);

            return commentDependencyException;
        }

        // Intentionally a named twin of CreateAndLogDependencyExceptionAsync (same wrapper,
        // same LogError): timeouts categorize as a non-critical dependency failure, but keep
        // their own seam so the call site reads as a timeout and the behavior can diverge
        // later without touching generic dependency handling. Mirrors The Standard's
        // EventHighway EventAddressV2Service.
        private async ValueTask<CommentDependencyException>
            CreateAndLogTimeoutDependencyExceptionAsync(Xeption exception)
        {
            var commentDependencyException =
                new CommentDependencyException(
                    message: "Comment dependency error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(commentDependencyException);

            return commentDependencyException;
        }

        private async ValueTask<CommentDependencyException>
            CreateAndLogCriticalDependencyExceptionAsync(Xeption exception)
        {
            var commentDependencyException = new CommentDependencyException(
                message: "Comment dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogCriticalAsync(commentDependencyException);

            return commentDependencyException;
        }

        private async ValueTask<CommentDependencyValidationException> CreateAndLogDependencyValidationExceptionAsync(
            Xeption exception)
        {
            var commentDependencyValidationException = new CommentDependencyValidationException(
                message: "Comment dependency validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(commentDependencyValidationException);

            return commentDependencyValidationException;
        }

        private async ValueTask<CommentServiceException> CreateAndLogServiceExceptionAsync(Xeption exception)
        {
            var commentServiceException = new CommentServiceException(
                message: "Comment service error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(commentServiceException);

            return commentServiceException;
        }
    }
}
