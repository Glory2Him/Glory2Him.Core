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
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalComments.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xeptions;

namespace Glory2Him.Core.Services.Foundations.ApprovalComments
{
    internal partial class ApprovalCommentService
    {
        private delegate ValueTask<ApprovalComment> ReturningApprovalCommentFunction();
        private delegate ValueTask<IQueryable<ApprovalComment>> ReturningApprovalCommentsFunction();

        private delegate ValueTask<EventEnvelope<ApprovalComment>?>
            ReturningApprovalCommentEventEnvelopeFunction();

        // The event-path wrapper: categorizes failures with the same taxonomy as the
        // non-event TryCatch (so the two entry paths cannot diverge), plus the envelope
        // guard that only exists on this path, and ALWAYS rethrows so the substrate records
        // the delivery as Error and drives retries. Exceptions already categorized by nested
        // service calls pass through unwrapped.
        private async ValueTask<EventEnvelope<ApprovalComment>?> TryCatchSubstrate(
            ReturningApprovalCommentEventEnvelopeFunction returningApprovalCommentEventEnvelopeFunction)
        {
            try
            {
                return await returningApprovalCommentEventEnvelopeFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutApprovalCommentException =
                    new TimeoutApprovalCommentException(
                        message: "Failed approval comment timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutApprovalCommentException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidApprovalCommentEventException invalidApprovalCommentEventException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidApprovalCommentEventException);
            }
            catch (NullApprovalCommentException nullApprovalCommentException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullApprovalCommentException);
            }
            catch (InvalidApprovalCommentException invalidApprovalCommentException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidApprovalCommentException);
            }
            catch (NotFoundApprovalCommentException notFoundApprovalCommentException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundApprovalCommentException);
            }
            catch (ApprovalCommentValidationException)
            {
                throw;
            }
            catch (ApprovalCommentDependencyValidationException)
            {
                throw;
            }
            catch (ApprovalCommentDependencyException)
            {
                throw;
            }
            catch (ApprovalCommentServiceException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageApprovalCommentException = new FailedStorageApprovalCommentException(
                    message: "Failed approval comment storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(
                    exception: failedStorageApprovalCommentException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsApprovalCommentException = new AlreadyExistsApprovalCommentException(
                    message: "Approval comment already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsApprovalCommentException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidApprovalCommentReferenceException = new InvalidApprovalCommentReferenceException(
                    message: "Invalid approval comment reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidApprovalCommentReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedApprovalCommentException = new LockedApprovalCommentException(
                    message: "Locked approval comment record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedApprovalCommentException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageApprovalCommentException = new FailedStorageApprovalCommentException(
                    message: "Failed approval comment storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageApprovalCommentException);
            }
            catch (Exception exception)
            {
                var failedApprovalCommentServiceException = new FailedApprovalCommentServiceException(
                    message: "Failed approval comment service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedApprovalCommentServiceException);
            }
        }

        private async ValueTask<ApprovalComment> TryCatch(
            ReturningApprovalCommentFunction returningApprovalCommentFunction)
        {
            try
            {
                return await returningApprovalCommentFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutApprovalCommentException =
                    new TimeoutApprovalCommentException(
                        message: "Failed approval comment timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutApprovalCommentException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (NullApprovalCommentException nullApprovalCommentException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullApprovalCommentException);
            }
            catch (InvalidApprovalCommentException invalidApprovalCommentException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidApprovalCommentException);
            }
            catch (NotFoundApprovalCommentException notFoundApprovalCommentException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundApprovalCommentException);
            }
            catch (SqlException sqlException)
            {
                var failedStorageApprovalCommentException = new FailedStorageApprovalCommentException(
                    message: "Failed approval comment storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(
                    exception: failedStorageApprovalCommentException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsApprovalCommentException = new AlreadyExistsApprovalCommentException(
                    message: "Approval comment already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsApprovalCommentException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidApprovalCommentReferenceException = new InvalidApprovalCommentReferenceException(
                    message: "Invalid approval comment reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidApprovalCommentReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedApprovalCommentException = new LockedApprovalCommentException(
                    message: "Locked approval comment record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedApprovalCommentException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageApprovalCommentException = new FailedStorageApprovalCommentException(
                    message: "Failed approval comment storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageApprovalCommentException);
            }
            catch (Exception exception)
            {
                var failedApprovalCommentServiceException = new FailedApprovalCommentServiceException(
                    message: "Failed approval comment service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedApprovalCommentServiceException);
            }
        }

        private async ValueTask<IQueryable<ApprovalComment>> TryCatch(
            ReturningApprovalCommentsFunction returningApprovalCommentsFunction)
        {
            try
            {
                return await returningApprovalCommentsFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutApprovalCommentException =
                    new TimeoutApprovalCommentException(
                        message: "Failed approval comment timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutApprovalCommentException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageApprovalCommentException = new FailedStorageApprovalCommentException(
                    message: "Failed approval comment storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(
                    exception: failedStorageApprovalCommentException);
            }
            catch (Exception exception)
            {
                var failedApprovalCommentServiceException = new FailedApprovalCommentServiceException(
                    message: "Failed approval comment service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedApprovalCommentServiceException);
            }
        }

        private async ValueTask<ApprovalCommentValidationException> CreateAndLogValidationExceptionAsync(
            Xeption exception)
        {
            var approvalCommentValidationException = new ApprovalCommentValidationException(
                message: "Approval comment validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalCommentValidationException);

            return approvalCommentValidationException;
        }

        private async ValueTask<ApprovalCommentDependencyException> CreateAndLogDependencyExceptionAsync(
            Xeption exception)
        {
            var approvalCommentDependencyException = new ApprovalCommentDependencyException(
                message: "Approval comment dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalCommentDependencyException);

            return approvalCommentDependencyException;
        }

        // Intentionally a named twin of CreateAndLogDependencyExceptionAsync (same wrapper,
        // same LogError): timeouts categorize as a non-critical dependency failure, but keep
        // their own seam so the call site reads as a timeout and the behavior can diverge
        // later without touching generic dependency handling. Mirrors The Standard's
        // EventHighway EventAddressV2Service.
        private async ValueTask<ApprovalCommentDependencyException>
            CreateAndLogTimeoutDependencyExceptionAsync(Xeption exception)
        {
            var approvalCommentDependencyException =
                new ApprovalCommentDependencyException(
                    message: "Approval comment dependency error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalCommentDependencyException);

            return approvalCommentDependencyException;
        }

        private async ValueTask<ApprovalCommentDependencyException>
            CreateAndLogCriticalDependencyExceptionAsync(Xeption exception)
        {
            var approvalCommentDependencyException = new ApprovalCommentDependencyException(
                message: "Approval comment dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogCriticalAsync(approvalCommentDependencyException);

            return approvalCommentDependencyException;
        }

        private async ValueTask<ApprovalCommentDependencyValidationException>
            CreateAndLogDependencyValidationExceptionAsync(
            Xeption exception)
        {
            var approvalCommentDependencyValidationException = new ApprovalCommentDependencyValidationException(
                message: "Approval comment dependency validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalCommentDependencyValidationException);

            return approvalCommentDependencyValidationException;
        }

        private async ValueTask<ApprovalCommentServiceException> CreateAndLogServiceExceptionAsync(Xeption exception)
        {
            var approvalCommentServiceException = new ApprovalCommentServiceException(
                message: "Approval comment service error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalCommentServiceException);

            return approvalCommentServiceException;
        }
    }
}
