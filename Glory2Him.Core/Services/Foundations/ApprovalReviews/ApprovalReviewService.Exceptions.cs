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
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.ApprovalReviews.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xeptions;

namespace Glory2Him.Core.Services.Foundations.ApprovalReviews
{
    internal partial class ApprovalReviewService
    {
        private delegate ValueTask<ApprovalReview> ReturningApprovalReviewFunction();
        private delegate ValueTask<IQueryable<ApprovalReview>> ReturningApprovalReviewsFunction();

        private delegate ValueTask<EventEnvelope<ApprovalReview>?>
            ReturningApprovalReviewEventEnvelopeFunction();

        // The event-path wrapper: categorizes failures with the same taxonomy as the
        // non-event TryCatch (so the two entry paths cannot diverge), plus the envelope
        // guard that only exists on this path, and ALWAYS rethrows so the substrate records
        // the delivery as Error and drives retries. Exceptions already categorized by nested
        // service calls pass through unwrapped.
        private async ValueTask<EventEnvelope<ApprovalReview>?> TryCatchSubstrate(
            ReturningApprovalReviewEventEnvelopeFunction returningApprovalReviewEventEnvelopeFunction)
        {
            try
            {
                return await returningApprovalReviewEventEnvelopeFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutApprovalReviewException =
                    new TimeoutApprovalReviewException(
                        message: "Failed approval review timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutApprovalReviewException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidApprovalReviewEventException invalidApprovalReviewEventException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidApprovalReviewEventException);
            }
            catch (UnauthorizedApprovalReviewException unauthorizedApprovalReviewException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: unauthorizedApprovalReviewException);
            }
            catch (NullApprovalReviewException nullApprovalReviewException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullApprovalReviewException);
            }
            catch (InvalidApprovalReviewException invalidApprovalReviewException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidApprovalReviewException);
            }
            catch (NotFoundApprovalReviewException notFoundApprovalReviewException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundApprovalReviewException);
            }
            catch (ApprovalReviewValidationException)
            {
                throw;
            }
            catch (ApprovalReviewDependencyValidationException)
            {
                throw;
            }
            catch (ApprovalReviewDependencyException)
            {
                throw;
            }
            catch (ApprovalReviewServiceException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageApprovalReviewException = new FailedStorageApprovalReviewException(
                    message: "Failed approval review storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageApprovalReviewException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsApprovalReviewException = new AlreadyExistsApprovalReviewException(
                    message: "Approval review already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsApprovalReviewException);
            }
            // A unique-INDEX violation (EF's HasIndex().IsUnique(), and the ProcessedEvents
            // dedup index) arrives as a type that does NOT derive from DuplicateKeyException,
            // so the clause above misses it; without this it falls through to the general
            // handler and mis-reports a business-key collision as "our code is broken".
            catch (DuplicateKeyWithUniqueIndexException duplicateKeyWithUniqueIndexException)
            {
                var alreadyExistsApprovalReviewException = new AlreadyExistsApprovalReviewException(
                    message: "Approval review already exists, "
                        + "a uniqueness rule rejected the write.",
                    innerException: duplicateKeyWithUniqueIndexException,
                    data: duplicateKeyWithUniqueIndexException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(
                    alreadyExistsApprovalReviewException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidApprovalReviewReferenceException = new InvalidApprovalReviewReferenceException(
                    message: "Invalid approval review reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidApprovalReviewReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedApprovalReviewException = new LockedApprovalReviewException(
                    message: "Locked approval review record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedApprovalReviewException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageApprovalReviewException = new FailedStorageApprovalReviewException(
                    message: "Failed approval review storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageApprovalReviewException);
            }
            catch (Exception exception)
            {
                var failedApprovalReviewServiceException = new FailedApprovalReviewServiceException(
                    message: "Failed approval review service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedApprovalReviewServiceException);
            }
        }

        private async ValueTask<ApprovalReview> TryCatch(ReturningApprovalReviewFunction returningApprovalReviewFunction)
        {
            try
            {
                return await returningApprovalReviewFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutApprovalReviewException =
                    new TimeoutApprovalReviewException(
                        message: "Failed approval review timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutApprovalReviewException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UnauthorizedApprovalReviewException unauthorizedApprovalReviewException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: unauthorizedApprovalReviewException);
            }
            catch (NullApprovalReviewException nullApprovalReviewException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullApprovalReviewException);
            }
            catch (InvalidApprovalReviewException invalidApprovalReviewException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidApprovalReviewException);
            }
            catch (NotFoundApprovalReviewException notFoundApprovalReviewException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundApprovalReviewException);
            }
            catch (SqlException sqlException)
            {
                var failedStorageApprovalReviewException = new FailedStorageApprovalReviewException(
                    message: "Failed approval review storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageApprovalReviewException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsApprovalReviewException = new AlreadyExistsApprovalReviewException(
                    message: "Approval review already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsApprovalReviewException);
            }
            // A unique-INDEX violation (EF's HasIndex().IsUnique(), and the ProcessedEvents
            // dedup index) arrives as a type that does NOT derive from DuplicateKeyException,
            // so the clause above misses it; without this it falls through to the general
            // handler and mis-reports a business-key collision as "our code is broken".
            catch (DuplicateKeyWithUniqueIndexException duplicateKeyWithUniqueIndexException)
            {
                var alreadyExistsApprovalReviewException = new AlreadyExistsApprovalReviewException(
                    message: "Approval review already exists, "
                        + "a uniqueness rule rejected the write.",
                    innerException: duplicateKeyWithUniqueIndexException,
                    data: duplicateKeyWithUniqueIndexException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(
                    alreadyExistsApprovalReviewException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidApprovalReviewReferenceException = new InvalidApprovalReviewReferenceException(
                    message: "Invalid approval review reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidApprovalReviewReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedApprovalReviewException = new LockedApprovalReviewException(
                    message: "Locked approval review record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedApprovalReviewException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageApprovalReviewException = new FailedStorageApprovalReviewException(
                    message: "Failed approval review storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageApprovalReviewException);
            }
            catch (Exception exception)
            {
                var failedApprovalReviewServiceException = new FailedApprovalReviewServiceException(
                    message: "Failed approval review service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedApprovalReviewServiceException);
            }
        }

        private async ValueTask<IQueryable<ApprovalReview>> TryCatch(
            ReturningApprovalReviewsFunction returningApprovalReviewsFunction)
        {
            try
            {
                return await returningApprovalReviewsFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutApprovalReviewException =
                    new TimeoutApprovalReviewException(
                        message: "Failed approval review timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutApprovalReviewException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageApprovalReviewException = new FailedStorageApprovalReviewException(
                    message: "Failed approval review storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageApprovalReviewException);
            }
            catch (Exception exception)
            {
                var failedApprovalReviewServiceException = new FailedApprovalReviewServiceException(
                    message: "Failed approval review service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedApprovalReviewServiceException);
            }
        }

        private async ValueTask<ApprovalReviewValidationException> CreateAndLogValidationExceptionAsync(Xeption exception)
        {
            var approvalReviewValidationException = new ApprovalReviewValidationException(
                message: "Approval review validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalReviewValidationException);

            return approvalReviewValidationException;
        }

        private async ValueTask<ApprovalReviewDependencyException> CreateAndLogDependencyExceptionAsync(Xeption exception)
        {
            var approvalReviewDependencyException = new ApprovalReviewDependencyException(
                message: "Approval review dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalReviewDependencyException);

            return approvalReviewDependencyException;
        }

        // Intentionally a named twin of CreateAndLogDependencyExceptionAsync (same wrapper,
        // same LogError): timeouts categorize as a non-critical dependency failure, but keep
        // their own seam so the call site reads as a timeout and the behavior can diverge
        // later without touching generic dependency handling. Mirrors The Standard's
        // EventHighway EventAddressV2Service.
        private async ValueTask<ApprovalReviewDependencyException>
            CreateAndLogTimeoutDependencyExceptionAsync(Xeption exception)
        {
            var approvalReviewDependencyException =
                new ApprovalReviewDependencyException(
                    message: "Approval review dependency error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalReviewDependencyException);

            return approvalReviewDependencyException;
        }

        private async ValueTask<ApprovalReviewDependencyException>
            CreateAndLogCriticalDependencyExceptionAsync(Xeption exception)
        {
            var approvalReviewDependencyException = new ApprovalReviewDependencyException(
                message: "Approval review dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogCriticalAsync(approvalReviewDependencyException);

            return approvalReviewDependencyException;
        }

        private async ValueTask<ApprovalReviewDependencyValidationException> CreateAndLogDependencyValidationExceptionAsync(
            Xeption exception)
        {
            var approvalReviewDependencyValidationException = new ApprovalReviewDependencyValidationException(
                message: "Approval review dependency validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalReviewDependencyValidationException);

            return approvalReviewDependencyValidationException;
        }

        private async ValueTask<ApprovalReviewServiceException> CreateAndLogServiceExceptionAsync(Xeption exception)
        {
            var approvalReviewServiceException = new ApprovalReviewServiceException(
                message: "Approval review service error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalReviewServiceException);

            return approvalReviewServiceException;
        }
    }
}
