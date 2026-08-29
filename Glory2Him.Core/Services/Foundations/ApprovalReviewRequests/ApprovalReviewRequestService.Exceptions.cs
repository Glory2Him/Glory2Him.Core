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
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xeptions;

namespace Glory2Him.Core.Services.Foundations.ApprovalReviewRequests
{
    internal partial class ApprovalReviewRequestService
    {
        private delegate ValueTask<ApprovalReviewRequest> ReturningApprovalReviewRequestFunction();
        private delegate ValueTask<IQueryable<ApprovalReviewRequest>> ReturningApprovalReviewRequestsFunction();

        private delegate ValueTask<EventEnvelope<ApprovalReviewRequest>?>
            ReturningApprovalReviewRequestEventEnvelopeFunction();

        // The event-path wrapper: categorizes failures with the same taxonomy as the
        // non-event TryCatch (so the two entry paths cannot diverge), plus the envelope
        // guard that only exists on this path, and ALWAYS rethrows so the substrate records
        // the delivery as Error and drives retries. Exceptions already categorized by nested
        // service calls pass through unwrapped.
        private async ValueTask<EventEnvelope<ApprovalReviewRequest>?> TryCatchSubstrate(
            ReturningApprovalReviewRequestEventEnvelopeFunction returningApprovalReviewRequestEventEnvelopeFunction)
        {
            try
            {
                return await returningApprovalReviewRequestEventEnvelopeFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutApprovalReviewRequestException =
                    new TimeoutApprovalReviewRequestException(
                        message: "Failed approval review request timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutApprovalReviewRequestException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidApprovalReviewRequestEventException invalidApprovalReviewRequestEventException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidApprovalReviewRequestEventException);
            }
            catch (UnauthorizedApprovalReviewRequestException unauthorizedApprovalReviewRequestException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: unauthorizedApprovalReviewRequestException);
            }
            catch (NullApprovalReviewRequestException nullApprovalReviewRequestException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullApprovalReviewRequestException);
            }
            catch (InvalidApprovalReviewRequestException invalidApprovalReviewRequestException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidApprovalReviewRequestException);
            }
            catch (NotFoundApprovalReviewRequestException notFoundApprovalReviewRequestException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundApprovalReviewRequestException);
            }
            catch (ApprovalReviewRequestValidationException)
            {
                throw;
            }
            catch (ApprovalReviewRequestDependencyValidationException)
            {
                throw;
            }
            catch (ApprovalReviewRequestDependencyException)
            {
                throw;
            }
            catch (ApprovalReviewRequestServiceException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageApprovalReviewRequestException = new FailedStorageApprovalReviewRequestException(
                    message: "Failed approval review request storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageApprovalReviewRequestException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsApprovalReviewRequestException = new AlreadyExistsApprovalReviewRequestException(
                    message: "Approval review request already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsApprovalReviewRequestException);
            }
            // A unique-INDEX violation (EF's HasIndex().IsUnique(), and the ProcessedEvents
            // dedup index) arrives as a type that does NOT derive from DuplicateKeyException,
            // so the clause above misses it; without this it falls through to the general
            // handler and mis-reports a business-key collision as "our code is broken".
            catch (DuplicateKeyWithUniqueIndexException duplicateKeyWithUniqueIndexException)
            {
                var alreadyExistsApprovalReviewRequestException = new AlreadyExistsApprovalReviewRequestException(
                    message: "Approval review request already exists, "
                        + "a uniqueness rule rejected the write.",
                    innerException: duplicateKeyWithUniqueIndexException,
                    data: duplicateKeyWithUniqueIndexException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(
                    alreadyExistsApprovalReviewRequestException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidApprovalReviewRequestReferenceException = new InvalidApprovalReviewRequestReferenceException(
                    message: "Invalid approval review request reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidApprovalReviewRequestReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedApprovalReviewRequestException = new LockedApprovalReviewRequestException(
                    message: "Locked approval review request record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedApprovalReviewRequestException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageApprovalReviewRequestException = new FailedStorageApprovalReviewRequestException(
                    message: "Failed approval review request storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageApprovalReviewRequestException);
            }
            catch (Exception exception)
            {
                var failedApprovalReviewRequestServiceException = new FailedApprovalReviewRequestServiceException(
                    message: "Failed approval review request service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedApprovalReviewRequestServiceException);
            }
        }

        private async ValueTask<ApprovalReviewRequest> TryCatch(ReturningApprovalReviewRequestFunction returningApprovalReviewRequestFunction)
        {
            try
            {
                return await returningApprovalReviewRequestFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutApprovalReviewRequestException =
                    new TimeoutApprovalReviewRequestException(
                        message: "Failed approval review request timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutApprovalReviewRequestException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UnauthorizedApprovalReviewRequestException unauthorizedApprovalReviewRequestException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: unauthorizedApprovalReviewRequestException);
            }
            catch (NullApprovalReviewRequestException nullApprovalReviewRequestException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullApprovalReviewRequestException);
            }
            catch (InvalidApprovalReviewRequestException invalidApprovalReviewRequestException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidApprovalReviewRequestException);
            }
            catch (NotFoundApprovalReviewRequestException notFoundApprovalReviewRequestException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundApprovalReviewRequestException);
            }
            catch (SqlException sqlException)
            {
                var failedStorageApprovalReviewRequestException = new FailedStorageApprovalReviewRequestException(
                    message: "Failed approval review request storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageApprovalReviewRequestException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsApprovalReviewRequestException = new AlreadyExistsApprovalReviewRequestException(
                    message: "Approval review request already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsApprovalReviewRequestException);
            }
            // A unique-INDEX violation (EF's HasIndex().IsUnique(), and the ProcessedEvents
            // dedup index) arrives as a type that does NOT derive from DuplicateKeyException,
            // so the clause above misses it; without this it falls through to the general
            // handler and mis-reports a business-key collision as "our code is broken".
            catch (DuplicateKeyWithUniqueIndexException duplicateKeyWithUniqueIndexException)
            {
                var alreadyExistsApprovalReviewRequestException = new AlreadyExistsApprovalReviewRequestException(
                    message: "Approval review request already exists, "
                        + "a uniqueness rule rejected the write.",
                    innerException: duplicateKeyWithUniqueIndexException,
                    data: duplicateKeyWithUniqueIndexException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(
                    alreadyExistsApprovalReviewRequestException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidApprovalReviewRequestReferenceException = new InvalidApprovalReviewRequestReferenceException(
                    message: "Invalid approval review request reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidApprovalReviewRequestReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedApprovalReviewRequestException = new LockedApprovalReviewRequestException(
                    message: "Locked approval review request record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedApprovalReviewRequestException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageApprovalReviewRequestException = new FailedStorageApprovalReviewRequestException(
                    message: "Failed approval review request storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageApprovalReviewRequestException);
            }
            catch (Exception exception)
            {
                var failedApprovalReviewRequestServiceException = new FailedApprovalReviewRequestServiceException(
                    message: "Failed approval review request service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedApprovalReviewRequestServiceException);
            }
        }

        private async ValueTask<IQueryable<ApprovalReviewRequest>> TryCatch(
            ReturningApprovalReviewRequestsFunction returningApprovalReviewRequestsFunction)
        {
            try
            {
                return await returningApprovalReviewRequestsFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutApprovalReviewRequestException =
                    new TimeoutApprovalReviewRequestException(
                        message: "Failed approval review request timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutApprovalReviewRequestException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageApprovalReviewRequestException = new FailedStorageApprovalReviewRequestException(
                    message: "Failed approval review request storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageApprovalReviewRequestException);
            }
            catch (Exception exception)
            {
                var failedApprovalReviewRequestServiceException = new FailedApprovalReviewRequestServiceException(
                    message: "Failed approval review request service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedApprovalReviewRequestServiceException);
            }
        }

        private async ValueTask<ApprovalReviewRequestValidationException> CreateAndLogValidationExceptionAsync(Xeption exception)
        {
            var approvalReviewRequestValidationException = new ApprovalReviewRequestValidationException(
                message: "Approval review request validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalReviewRequestValidationException);

            return approvalReviewRequestValidationException;
        }

        private async ValueTask<ApprovalReviewRequestDependencyException> CreateAndLogDependencyExceptionAsync(Xeption exception)
        {
            var approvalReviewRequestDependencyException = new ApprovalReviewRequestDependencyException(
                message: "Approval review request dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalReviewRequestDependencyException);

            return approvalReviewRequestDependencyException;
        }

        // Intentionally a named twin of CreateAndLogDependencyExceptionAsync (same wrapper,
        // same LogError): timeouts categorize as a non-critical dependency failure, but keep
        // their own seam so the call site reads as a timeout and the behavior can diverge
        // later without touching generic dependency handling. Mirrors The Standard's
        // EventHighway EventAddressV2Service.
        private async ValueTask<ApprovalReviewRequestDependencyException>
            CreateAndLogTimeoutDependencyExceptionAsync(Xeption exception)
        {
            var approvalReviewRequestDependencyException =
                new ApprovalReviewRequestDependencyException(
                    message: "Approval review request dependency error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalReviewRequestDependencyException);

            return approvalReviewRequestDependencyException;
        }

        private async ValueTask<ApprovalReviewRequestDependencyException>
            CreateAndLogCriticalDependencyExceptionAsync(Xeption exception)
        {
            var approvalReviewRequestDependencyException = new ApprovalReviewRequestDependencyException(
                message: "Approval review request dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogCriticalAsync(approvalReviewRequestDependencyException);

            return approvalReviewRequestDependencyException;
        }

        private async ValueTask<ApprovalReviewRequestDependencyValidationException> CreateAndLogDependencyValidationExceptionAsync(
            Xeption exception)
        {
            var approvalReviewRequestDependencyValidationException = new ApprovalReviewRequestDependencyValidationException(
                message: "Approval review request dependency validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalReviewRequestDependencyValidationException);

            return approvalReviewRequestDependencyValidationException;
        }

        private async ValueTask<ApprovalReviewRequestServiceException> CreateAndLogServiceExceptionAsync(Xeption exception)
        {
            var approvalReviewRequestServiceException = new ApprovalReviewRequestServiceException(
                message: "Approval review request service error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalReviewRequestServiceException);

            return approvalReviewRequestServiceException;
        }
    }
}
