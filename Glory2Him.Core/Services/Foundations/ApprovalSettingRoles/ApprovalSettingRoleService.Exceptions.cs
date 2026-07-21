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
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles;
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xeptions;

namespace Glory2Him.Core.Services.Foundations.ApprovalSettingRoles
{
    public partial class ApprovalSettingRoleService
    {
        private delegate ValueTask<ApprovalSettingRole> ReturningApprovalSettingRoleFunction();
        private delegate ValueTask<IQueryable<ApprovalSettingRole>> ReturningApprovalSettingRolesFunction();

        private delegate ValueTask<EventEnvelope<ApprovalSettingRole>?>
            ReturningApprovalSettingRoleEventEnvelopeFunction();

        // The event-path wrapper: categorizes failures with the same taxonomy as the
        // non-event TryCatch (so the two entry paths cannot diverge), plus the envelope
        // guard that only exists on this path, and ALWAYS rethrows so the substrate records
        // the delivery as Error and drives retries. Exceptions already categorized by nested
        // service calls pass through unwrapped.
        private async ValueTask<EventEnvelope<ApprovalSettingRole>?> TryCatchSubstrate(
            ReturningApprovalSettingRoleEventEnvelopeFunction returningApprovalSettingRoleEventEnvelopeFunction)
        {
            try
            {
                return await returningApprovalSettingRoleEventEnvelopeFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutApprovalSettingRoleException =
                    new TimeoutApprovalSettingRoleException(
                        message: "Failed approval setting role timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutApprovalSettingRoleException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidApprovalSettingRoleEventException invalidApprovalSettingRoleEventException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidApprovalSettingRoleEventException);
            }
            catch (NullApprovalSettingRoleException nullApprovalSettingRoleException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullApprovalSettingRoleException);
            }
            catch (InvalidApprovalSettingRoleException invalidApprovalSettingRoleException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidApprovalSettingRoleException);
            }
            catch (NotFoundApprovalSettingRoleException notFoundApprovalSettingRoleException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundApprovalSettingRoleException);
            }
            catch (ApprovalSettingRoleValidationException)
            {
                throw;
            }
            catch (ApprovalSettingRoleDependencyValidationException)
            {
                throw;
            }
            catch (ApprovalSettingRoleDependencyException)
            {
                throw;
            }
            catch (ApprovalSettingRoleServiceException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageApprovalSettingRoleException = new FailedStorageApprovalSettingRoleException(
                    message: "Failed approval setting role storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageApprovalSettingRoleException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsApprovalSettingRoleException = new AlreadyExistsApprovalSettingRoleException(
                    message: "Approval setting role already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsApprovalSettingRoleException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidApprovalSettingRoleReferenceException = new InvalidApprovalSettingRoleReferenceException(
                    message: "Invalid approval setting role reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidApprovalSettingRoleReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedApprovalSettingRoleException = new LockedApprovalSettingRoleException(
                    message: "Locked approval setting role record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedApprovalSettingRoleException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageApprovalSettingRoleException = new FailedStorageApprovalSettingRoleException(
                    message: "Failed approval setting role storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageApprovalSettingRoleException);
            }
            catch (Exception exception)
            {
                var failedApprovalSettingRoleServiceException = new FailedApprovalSettingRoleServiceException(
                    message: "Failed approval setting role service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedApprovalSettingRoleServiceException);
            }
        }

        private async ValueTask<ApprovalSettingRole> TryCatch(ReturningApprovalSettingRoleFunction returningApprovalSettingRoleFunction)
        {
            try
            {
                return await returningApprovalSettingRoleFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutApprovalSettingRoleException =
                    new TimeoutApprovalSettingRoleException(
                        message: "Failed approval setting role timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutApprovalSettingRoleException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (NullApprovalSettingRoleException nullApprovalSettingRoleException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullApprovalSettingRoleException);
            }
            catch (InvalidApprovalSettingRoleException invalidApprovalSettingRoleException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidApprovalSettingRoleException);
            }
            catch (NotFoundApprovalSettingRoleException notFoundApprovalSettingRoleException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundApprovalSettingRoleException);
            }
            catch (SqlException sqlException)
            {
                var failedStorageApprovalSettingRoleException = new FailedStorageApprovalSettingRoleException(
                    message: "Failed approval setting role storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageApprovalSettingRoleException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsApprovalSettingRoleException = new AlreadyExistsApprovalSettingRoleException(
                    message: "Approval setting role already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsApprovalSettingRoleException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidApprovalSettingRoleReferenceException = new InvalidApprovalSettingRoleReferenceException(
                    message: "Invalid approval setting role reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidApprovalSettingRoleReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedApprovalSettingRoleException = new LockedApprovalSettingRoleException(
                    message: "Locked approval setting role record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedApprovalSettingRoleException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageApprovalSettingRoleException = new FailedStorageApprovalSettingRoleException(
                    message: "Failed approval setting role storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageApprovalSettingRoleException);
            }
            catch (Exception exception)
            {
                var failedApprovalSettingRoleServiceException = new FailedApprovalSettingRoleServiceException(
                    message: "Failed approval setting role service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedApprovalSettingRoleServiceException);
            }
        }

        private async ValueTask<IQueryable<ApprovalSettingRole>> TryCatch(
            ReturningApprovalSettingRolesFunction returningApprovalSettingRolesFunction)
        {
            try
            {
                return await returningApprovalSettingRolesFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutApprovalSettingRoleException =
                    new TimeoutApprovalSettingRoleException(
                        message: "Failed approval setting role timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutApprovalSettingRoleException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageApprovalSettingRoleException = new FailedStorageApprovalSettingRoleException(
                    message: "Failed approval setting role storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageApprovalSettingRoleException);
            }
            catch (Exception exception)
            {
                var failedApprovalSettingRoleServiceException = new FailedApprovalSettingRoleServiceException(
                    message: "Failed approval setting role service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedApprovalSettingRoleServiceException);
            }
        }

        private async ValueTask<ApprovalSettingRoleValidationException> CreateAndLogValidationExceptionAsync(Xeption exception)
        {
            var approvalSettingRoleValidationException = new ApprovalSettingRoleValidationException(
                message: "Approval setting role validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalSettingRoleValidationException);

            return approvalSettingRoleValidationException;
        }

        private async ValueTask<ApprovalSettingRoleDependencyException> CreateAndLogDependencyExceptionAsync(Xeption exception)
        {
            var approvalSettingRoleDependencyException = new ApprovalSettingRoleDependencyException(
                message: "Approval setting role dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalSettingRoleDependencyException);

            return approvalSettingRoleDependencyException;
        }

        // Intentionally a named twin of CreateAndLogDependencyExceptionAsync (same wrapper,
        // same LogError): timeouts categorize as a non-critical dependency failure, but keep
        // their own seam so the call site reads as a timeout and the behavior can diverge
        // later without touching generic dependency handling. Mirrors The Standard's
        // EventHighway EventAddressV2Service.
        private async ValueTask<ApprovalSettingRoleDependencyException>
            CreateAndLogTimeoutDependencyExceptionAsync(Xeption exception)
        {
            var approvalSettingRoleDependencyException =
                new ApprovalSettingRoleDependencyException(
                    message: "Approval setting role dependency error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalSettingRoleDependencyException);

            return approvalSettingRoleDependencyException;
        }

        private async ValueTask<ApprovalSettingRoleDependencyException>
            CreateAndLogCriticalDependencyExceptionAsync(Xeption exception)
        {
            var approvalSettingRoleDependencyException = new ApprovalSettingRoleDependencyException(
                message: "Approval setting role dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogCriticalAsync(approvalSettingRoleDependencyException);

            return approvalSettingRoleDependencyException;
        }

        private async ValueTask<ApprovalSettingRoleDependencyValidationException> CreateAndLogDependencyValidationExceptionAsync(
            Xeption exception)
        {
            var approvalSettingRoleDependencyValidationException = new ApprovalSettingRoleDependencyValidationException(
                message: "Approval setting role dependency validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalSettingRoleDependencyValidationException);

            return approvalSettingRoleDependencyValidationException;
        }

        private async ValueTask<ApprovalSettingRoleServiceException> CreateAndLogServiceExceptionAsync(Xeption exception)
        {
            var approvalSettingRoleServiceException = new ApprovalSettingRoleServiceException(
                message: "Approval setting role service error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalSettingRoleServiceException);

            return approvalSettingRoleServiceException;
        }
    }
}
