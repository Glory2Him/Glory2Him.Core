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
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles;
using Glory2Him.Core.Models.Foundations.ApprovalSettingReviewerRoles.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xeptions;

namespace Glory2Him.Core.Services.Foundations.ApprovalSettingReviewerRoles
{
    internal partial class ApprovalSettingReviewerRoleService
    {
        private delegate ValueTask<ApprovalSettingReviewerRole> ReturningApprovalSettingReviewerRoleFunction();
        private delegate ValueTask<IQueryable<ApprovalSettingReviewerRole>> ReturningApprovalSettingReviewerRolesFunction();

        private delegate ValueTask<EventEnvelope<ApprovalSettingReviewerRole>?>
            ReturningApprovalSettingReviewerRoleEventEnvelopeFunction();

        // The event-path wrapper: categorizes failures with the same taxonomy as the
        // non-event TryCatch (so the two entry paths cannot diverge), plus the envelope
        // guard that only exists on this path, and ALWAYS rethrows so the substrate records
        // the delivery as Error and drives retries. Exceptions already categorized by nested
        // service calls pass through unwrapped.
        private async ValueTask<EventEnvelope<ApprovalSettingReviewerRole>?> TryCatchSubstrate(
            ReturningApprovalSettingReviewerRoleEventEnvelopeFunction returningApprovalSettingReviewerRoleEventEnvelopeFunction)
        {
            try
            {
                return await returningApprovalSettingReviewerRoleEventEnvelopeFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutApprovalSettingReviewerRoleException =
                    new TimeoutApprovalSettingReviewerRoleException(
                        message: "Failed approval setting reviewer role timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutApprovalSettingReviewerRoleException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidApprovalSettingReviewerRoleEventException invalidApprovalSettingReviewerRoleEventException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidApprovalSettingReviewerRoleEventException);
            }
            catch (UnauthorizedApprovalSettingReviewerRoleException unauthorizedApprovalSettingReviewerRoleException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: unauthorizedApprovalSettingReviewerRoleException);
            }
            catch (NullApprovalSettingReviewerRoleException nullApprovalSettingReviewerRoleException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullApprovalSettingReviewerRoleException);
            }
            catch (InvalidApprovalSettingReviewerRoleException invalidApprovalSettingReviewerRoleException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidApprovalSettingReviewerRoleException);
            }
            catch (NotFoundApprovalSettingReviewerRoleException notFoundApprovalSettingReviewerRoleException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundApprovalSettingReviewerRoleException);
            }
            catch (ApprovalSettingReviewerRoleValidationException)
            {
                throw;
            }
            catch (ApprovalSettingReviewerRoleDependencyValidationException)
            {
                throw;
            }
            catch (ApprovalSettingReviewerRoleDependencyException)
            {
                throw;
            }
            catch (ApprovalSettingReviewerRoleServiceException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageApprovalSettingReviewerRoleException = new FailedStorageApprovalSettingReviewerRoleException(
                    message: "Failed approval setting reviewer role storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageApprovalSettingReviewerRoleException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsApprovalSettingReviewerRoleException = new AlreadyExistsApprovalSettingReviewerRoleException(
                    message: "Approval setting reviewer role already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsApprovalSettingReviewerRoleException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidApprovalSettingReviewerRoleReferenceException = new InvalidApprovalSettingReviewerRoleReferenceException(
                    message: "Invalid approval setting reviewer role reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidApprovalSettingReviewerRoleReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedApprovalSettingReviewerRoleException = new LockedApprovalSettingReviewerRoleException(
                    message: "Locked approval setting reviewer role record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedApprovalSettingReviewerRoleException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageApprovalSettingReviewerRoleException = new FailedStorageApprovalSettingReviewerRoleException(
                    message: "Failed approval setting reviewer role storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageApprovalSettingReviewerRoleException);
            }
            catch (Exception exception)
            {
                var failedApprovalSettingReviewerRoleServiceException = new FailedApprovalSettingReviewerRoleServiceException(
                    message: "Failed approval setting reviewer role service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedApprovalSettingReviewerRoleServiceException);
            }
        }

        private async ValueTask<ApprovalSettingReviewerRole> TryCatch(ReturningApprovalSettingReviewerRoleFunction returningApprovalSettingReviewerRoleFunction)
        {
            try
            {
                return await returningApprovalSettingReviewerRoleFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutApprovalSettingReviewerRoleException =
                    new TimeoutApprovalSettingReviewerRoleException(
                        message: "Failed approval setting reviewer role timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutApprovalSettingReviewerRoleException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UnauthorizedApprovalSettingReviewerRoleException unauthorizedApprovalSettingReviewerRoleException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: unauthorizedApprovalSettingReviewerRoleException);
            }
            catch (NullApprovalSettingReviewerRoleException nullApprovalSettingReviewerRoleException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullApprovalSettingReviewerRoleException);
            }
            catch (InvalidApprovalSettingReviewerRoleException invalidApprovalSettingReviewerRoleException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidApprovalSettingReviewerRoleException);
            }
            catch (NotFoundApprovalSettingReviewerRoleException notFoundApprovalSettingReviewerRoleException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundApprovalSettingReviewerRoleException);
            }
            catch (SqlException sqlException)
            {
                var failedStorageApprovalSettingReviewerRoleException = new FailedStorageApprovalSettingReviewerRoleException(
                    message: "Failed approval setting reviewer role storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageApprovalSettingReviewerRoleException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsApprovalSettingReviewerRoleException = new AlreadyExistsApprovalSettingReviewerRoleException(
                    message: "Approval setting reviewer role already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsApprovalSettingReviewerRoleException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidApprovalSettingReviewerRoleReferenceException = new InvalidApprovalSettingReviewerRoleReferenceException(
                    message: "Invalid approval setting reviewer role reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidApprovalSettingReviewerRoleReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedApprovalSettingReviewerRoleException = new LockedApprovalSettingReviewerRoleException(
                    message: "Locked approval setting reviewer role record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedApprovalSettingReviewerRoleException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageApprovalSettingReviewerRoleException = new FailedStorageApprovalSettingReviewerRoleException(
                    message: "Failed approval setting reviewer role storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageApprovalSettingReviewerRoleException);
            }
            catch (Exception exception)
            {
                var failedApprovalSettingReviewerRoleServiceException = new FailedApprovalSettingReviewerRoleServiceException(
                    message: "Failed approval setting reviewer role service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedApprovalSettingReviewerRoleServiceException);
            }
        }

        private async ValueTask<IQueryable<ApprovalSettingReviewerRole>> TryCatch(
            ReturningApprovalSettingReviewerRolesFunction returningApprovalSettingReviewerRolesFunction)
        {
            try
            {
                return await returningApprovalSettingReviewerRolesFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutApprovalSettingReviewerRoleException =
                    new TimeoutApprovalSettingReviewerRoleException(
                        message: "Failed approval setting reviewer role timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutApprovalSettingReviewerRoleException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageApprovalSettingReviewerRoleException = new FailedStorageApprovalSettingReviewerRoleException(
                    message: "Failed approval setting reviewer role storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageApprovalSettingReviewerRoleException);
            }
            catch (Exception exception)
            {
                var failedApprovalSettingReviewerRoleServiceException = new FailedApprovalSettingReviewerRoleServiceException(
                    message: "Failed approval setting reviewer role service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedApprovalSettingReviewerRoleServiceException);
            }
        }

        private async ValueTask<ApprovalSettingReviewerRoleValidationException> CreateAndLogValidationExceptionAsync(Xeption exception)
        {
            var approvalSettingReviewerRoleValidationException = new ApprovalSettingReviewerRoleValidationException(
                message: "Approval setting reviewer role validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalSettingReviewerRoleValidationException);

            return approvalSettingReviewerRoleValidationException;
        }

        private async ValueTask<ApprovalSettingReviewerRoleDependencyException> CreateAndLogDependencyExceptionAsync(Xeption exception)
        {
            var approvalSettingReviewerRoleDependencyException = new ApprovalSettingReviewerRoleDependencyException(
                message: "Approval setting reviewer role dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalSettingReviewerRoleDependencyException);

            return approvalSettingReviewerRoleDependencyException;
        }

        // Intentionally a named twin of CreateAndLogDependencyExceptionAsync (same wrapper,
        // same LogError): timeouts categorize as a non-critical dependency failure, but keep
        // their own seam so the call site reads as a timeout and the behavior can diverge
        // later without touching generic dependency handling. Mirrors The Standard's
        // EventHighway EventAddressV2Service.
        private async ValueTask<ApprovalSettingReviewerRoleDependencyException>
            CreateAndLogTimeoutDependencyExceptionAsync(Xeption exception)
        {
            var approvalSettingReviewerRoleDependencyException =
                new ApprovalSettingReviewerRoleDependencyException(
                    message: "Approval setting reviewer role dependency error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalSettingReviewerRoleDependencyException);

            return approvalSettingReviewerRoleDependencyException;
        }

        private async ValueTask<ApprovalSettingReviewerRoleDependencyException>
            CreateAndLogCriticalDependencyExceptionAsync(Xeption exception)
        {
            var approvalSettingReviewerRoleDependencyException = new ApprovalSettingReviewerRoleDependencyException(
                message: "Approval setting reviewer role dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogCriticalAsync(approvalSettingReviewerRoleDependencyException);

            return approvalSettingReviewerRoleDependencyException;
        }

        private async ValueTask<ApprovalSettingReviewerRoleDependencyValidationException> CreateAndLogDependencyValidationExceptionAsync(
            Xeption exception)
        {
            var approvalSettingReviewerRoleDependencyValidationException = new ApprovalSettingReviewerRoleDependencyValidationException(
                message: "Approval setting reviewer role dependency validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalSettingReviewerRoleDependencyValidationException);

            return approvalSettingReviewerRoleDependencyValidationException;
        }

        private async ValueTask<ApprovalSettingReviewerRoleServiceException> CreateAndLogServiceExceptionAsync(Xeption exception)
        {
            var approvalSettingReviewerRoleServiceException = new ApprovalSettingReviewerRoleServiceException(
                message: "Approval setting reviewer role service error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalSettingReviewerRoleServiceException);

            return approvalSettingReviewerRoleServiceException;
        }
    }
}
