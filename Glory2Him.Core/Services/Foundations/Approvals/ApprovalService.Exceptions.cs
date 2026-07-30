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
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.Approvals.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xeptions;

namespace Glory2Him.Core.Services.Foundations.Approvals
{
    internal partial class ApprovalService
    {
        private delegate ValueTask<Approval> ReturningApprovalFunction();
        private delegate ValueTask<IQueryable<Approval>> ReturningApprovalsFunction();

        private delegate ValueTask<EventEnvelope<Approval>?>
            ReturningApprovalEventEnvelopeFunction();

        // The event-path wrapper: categorizes failures with the same taxonomy as the
        // non-event TryCatch (so the two entry paths cannot diverge), plus the envelope
        // guard that only exists on this path, and ALWAYS rethrows so the substrate records
        // the delivery as Error and drives retries. Exceptions already categorized by nested
        // service calls pass through unwrapped.
        private async ValueTask<EventEnvelope<Approval>?> TryCatchSubstrate(
            ReturningApprovalEventEnvelopeFunction returningApprovalEventEnvelopeFunction)
        {
            try
            {
                return await returningApprovalEventEnvelopeFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutApprovalException =
                    new TimeoutApprovalException(
                        message: "Failed approval timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutApprovalException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidApprovalEventException invalidApprovalEventException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidApprovalEventException);
            }
            catch (NullApprovalException nullApprovalException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullApprovalException);
            }
            catch (InvalidApprovalException invalidApprovalException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidApprovalException);
            }
            catch (NotFoundApprovalException notFoundApprovalException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundApprovalException);
            }
            catch (ApprovalValidationException)
            {
                throw;
            }
            catch (ApprovalDependencyValidationException)
            {
                throw;
            }
            catch (ApprovalDependencyException)
            {
                throw;
            }
            catch (ApprovalServiceException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageApprovalException = new FailedStorageApprovalException(
                    message: "Failed approval storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageApprovalException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsApprovalException = new AlreadyExistsApprovalException(
                    message: "Approval already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsApprovalException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidApprovalReferenceException = new InvalidApprovalReferenceException(
                    message: "Invalid approval reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidApprovalReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedApprovalException = new LockedApprovalException(
                    message: "Locked approval record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedApprovalException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageApprovalException = new FailedStorageApprovalException(
                    message: "Failed approval storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageApprovalException);
            }
            catch (Exception exception)
            {
                var failedApprovalServiceException = new FailedApprovalServiceException(
                    message: "Failed approval service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedApprovalServiceException);
            }
        }

        private async ValueTask<Approval> TryCatch(ReturningApprovalFunction returningApprovalFunction)
        {
            try
            {
                return await returningApprovalFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutApprovalException =
                    new TimeoutApprovalException(
                        message: "Failed approval timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutApprovalException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (NullApprovalException nullApprovalException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullApprovalException);
            }
            catch (InvalidApprovalException invalidApprovalException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidApprovalException);
            }
            catch (NotFoundApprovalException notFoundApprovalException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundApprovalException);
            }
            catch (SqlException sqlException)
            {
                var failedStorageApprovalException = new FailedStorageApprovalException(
                    message: "Failed approval storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageApprovalException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsApprovalException = new AlreadyExistsApprovalException(
                    message: "Approval already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsApprovalException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidApprovalReferenceException = new InvalidApprovalReferenceException(
                    message: "Invalid approval reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidApprovalReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedApprovalException = new LockedApprovalException(
                    message: "Locked approval record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedApprovalException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageApprovalException = new FailedStorageApprovalException(
                    message: "Failed approval storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageApprovalException);
            }
            catch (Exception exception)
            {
                var failedApprovalServiceException = new FailedApprovalServiceException(
                    message: "Failed approval service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedApprovalServiceException);
            }
        }

        private async ValueTask<IQueryable<Approval>> TryCatch(
            ReturningApprovalsFunction returningApprovalsFunction)
        {
            try
            {
                return await returningApprovalsFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutApprovalException =
                    new TimeoutApprovalException(
                        message: "Failed approval timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutApprovalException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageApprovalException = new FailedStorageApprovalException(
                    message: "Failed approval storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageApprovalException);
            }
            catch (Exception exception)
            {
                var failedApprovalServiceException = new FailedApprovalServiceException(
                    message: "Failed approval service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedApprovalServiceException);
            }
        }

        private async ValueTask<ApprovalValidationException> CreateAndLogValidationExceptionAsync(Xeption exception)
        {
            var approvalValidationException = new ApprovalValidationException(
                message: "Approval validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalValidationException);

            return approvalValidationException;
        }

        private async ValueTask<ApprovalDependencyException> CreateAndLogDependencyExceptionAsync(Xeption exception)
        {
            var approvalDependencyException = new ApprovalDependencyException(
                message: "Approval dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalDependencyException);

            return approvalDependencyException;
        }

        // Intentionally a named twin of CreateAndLogDependencyExceptionAsync (same wrapper,
        // same LogError): timeouts categorize as a non-critical dependency failure, but keep
        // their own seam so the call site reads as a timeout and the behavior can diverge
        // later without touching generic dependency handling. Mirrors The Standard's
        // EventHighway EventAddressV2Service.
        private async ValueTask<ApprovalDependencyException>
            CreateAndLogTimeoutDependencyExceptionAsync(Xeption exception)
        {
            var approvalDependencyException =
                new ApprovalDependencyException(
                    message: "Approval dependency error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalDependencyException);

            return approvalDependencyException;
        }

        private async ValueTask<ApprovalDependencyException>
            CreateAndLogCriticalDependencyExceptionAsync(Xeption exception)
        {
            var approvalDependencyException = new ApprovalDependencyException(
                message: "Approval dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogCriticalAsync(approvalDependencyException);

            return approvalDependencyException;
        }

        private async ValueTask<ApprovalDependencyValidationException> CreateAndLogDependencyValidationExceptionAsync(
            Xeption exception)
        {
            var approvalDependencyValidationException = new ApprovalDependencyValidationException(
                message: "Approval dependency validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalDependencyValidationException);

            return approvalDependencyValidationException;
        }

        private async ValueTask<ApprovalServiceException> CreateAndLogServiceExceptionAsync(Xeption exception)
        {
            var approvalServiceException = new ApprovalServiceException(
                message: "Approval service error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalServiceException);

            return approvalServiceException;
        }
    }
}
