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
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles;
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xeptions;

namespace Glory2Him.Core.Services.Foundations.ApprovalSettingPublisherRoles
{
    internal partial class ApprovalSettingPublisherRoleService
    {
        private delegate ValueTask<ApprovalSettingPublisherRole> ReturningApprovalSettingPublisherRoleFunction();
        private delegate ValueTask<IQueryable<ApprovalSettingPublisherRole>> ReturningApprovalSettingPublisherRolesFunction();

        private delegate ValueTask<EventEnvelope<ApprovalSettingPublisherRole>?>
            ReturningApprovalSettingPublisherRoleEventEnvelopeFunction();

        // The event-path wrapper: categorizes failures with the same taxonomy as the
        // non-event TryCatch (so the two entry paths cannot diverge), plus the envelope
        // guard that only exists on this path, and ALWAYS rethrows so the substrate records
        // the delivery as Error and drives retries. Exceptions already categorized by nested
        // service calls pass through unwrapped.
        private async ValueTask<EventEnvelope<ApprovalSettingPublisherRole>?> TryCatchSubstrate(
            ReturningApprovalSettingPublisherRoleEventEnvelopeFunction returningApprovalSettingPublisherRoleEventEnvelopeFunction)
        {
            try
            {
                return await returningApprovalSettingPublisherRoleEventEnvelopeFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutApprovalSettingPublisherRoleException =
                    new TimeoutApprovalSettingPublisherRoleException(
                        message: "Failed approval setting publisher role timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutApprovalSettingPublisherRoleException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidApprovalSettingPublisherRoleEventException invalidApprovalSettingPublisherRoleEventException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidApprovalSettingPublisherRoleEventException);
            }
            catch (NullApprovalSettingPublisherRoleException nullApprovalSettingPublisherRoleException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullApprovalSettingPublisherRoleException);
            }
            catch (InvalidApprovalSettingPublisherRoleException invalidApprovalSettingPublisherRoleException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidApprovalSettingPublisherRoleException);
            }
            catch (NotFoundApprovalSettingPublisherRoleException notFoundApprovalSettingPublisherRoleException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundApprovalSettingPublisherRoleException);
            }
            catch (ApprovalSettingPublisherRoleValidationException)
            {
                throw;
            }
            catch (ApprovalSettingPublisherRoleDependencyValidationException)
            {
                throw;
            }
            catch (ApprovalSettingPublisherRoleDependencyException)
            {
                throw;
            }
            catch (ApprovalSettingPublisherRoleServiceException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageApprovalSettingPublisherRoleException = new FailedStorageApprovalSettingPublisherRoleException(
                    message: "Failed approval setting publisher role storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageApprovalSettingPublisherRoleException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsApprovalSettingPublisherRoleException = new AlreadyExistsApprovalSettingPublisherRoleException(
                    message: "Approval setting publisher role already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsApprovalSettingPublisherRoleException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidApprovalSettingPublisherRoleReferenceException = new InvalidApprovalSettingPublisherRoleReferenceException(
                    message: "Invalid approval setting publisher role reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidApprovalSettingPublisherRoleReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedApprovalSettingPublisherRoleException = new LockedApprovalSettingPublisherRoleException(
                    message: "Locked approval setting publisher role record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedApprovalSettingPublisherRoleException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageApprovalSettingPublisherRoleException = new FailedStorageApprovalSettingPublisherRoleException(
                    message: "Failed approval setting publisher role storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageApprovalSettingPublisherRoleException);
            }
            catch (Exception exception)
            {
                var failedApprovalSettingPublisherRoleServiceException = new FailedApprovalSettingPublisherRoleServiceException(
                    message: "Failed approval setting publisher role service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedApprovalSettingPublisherRoleServiceException);
            }
        }

        private async ValueTask<ApprovalSettingPublisherRole> TryCatch(ReturningApprovalSettingPublisherRoleFunction returningApprovalSettingPublisherRoleFunction)
        {
            try
            {
                return await returningApprovalSettingPublisherRoleFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutApprovalSettingPublisherRoleException =
                    new TimeoutApprovalSettingPublisherRoleException(
                        message: "Failed approval setting publisher role timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutApprovalSettingPublisherRoleException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (NullApprovalSettingPublisherRoleException nullApprovalSettingPublisherRoleException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullApprovalSettingPublisherRoleException);
            }
            catch (InvalidApprovalSettingPublisherRoleException invalidApprovalSettingPublisherRoleException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidApprovalSettingPublisherRoleException);
            }
            catch (NotFoundApprovalSettingPublisherRoleException notFoundApprovalSettingPublisherRoleException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundApprovalSettingPublisherRoleException);
            }
            catch (SqlException sqlException)
            {
                var failedStorageApprovalSettingPublisherRoleException = new FailedStorageApprovalSettingPublisherRoleException(
                    message: "Failed approval setting publisher role storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageApprovalSettingPublisherRoleException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsApprovalSettingPublisherRoleException = new AlreadyExistsApprovalSettingPublisherRoleException(
                    message: "Approval setting publisher role already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsApprovalSettingPublisherRoleException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidApprovalSettingPublisherRoleReferenceException = new InvalidApprovalSettingPublisherRoleReferenceException(
                    message: "Invalid approval setting publisher role reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidApprovalSettingPublisherRoleReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedApprovalSettingPublisherRoleException = new LockedApprovalSettingPublisherRoleException(
                    message: "Locked approval setting publisher role record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedApprovalSettingPublisherRoleException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageApprovalSettingPublisherRoleException = new FailedStorageApprovalSettingPublisherRoleException(
                    message: "Failed approval setting publisher role storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageApprovalSettingPublisherRoleException);
            }
            catch (Exception exception)
            {
                var failedApprovalSettingPublisherRoleServiceException = new FailedApprovalSettingPublisherRoleServiceException(
                    message: "Failed approval setting publisher role service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedApprovalSettingPublisherRoleServiceException);
            }
        }

        private async ValueTask<IQueryable<ApprovalSettingPublisherRole>> TryCatch(
            ReturningApprovalSettingPublisherRolesFunction returningApprovalSettingPublisherRolesFunction)
        {
            try
            {
                return await returningApprovalSettingPublisherRolesFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutApprovalSettingPublisherRoleException =
                    new TimeoutApprovalSettingPublisherRoleException(
                        message: "Failed approval setting publisher role timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutApprovalSettingPublisherRoleException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageApprovalSettingPublisherRoleException = new FailedStorageApprovalSettingPublisherRoleException(
                    message: "Failed approval setting publisher role storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageApprovalSettingPublisherRoleException);
            }
            catch (Exception exception)
            {
                var failedApprovalSettingPublisherRoleServiceException = new FailedApprovalSettingPublisherRoleServiceException(
                    message: "Failed approval setting publisher role service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedApprovalSettingPublisherRoleServiceException);
            }
        }

        private async ValueTask<ApprovalSettingPublisherRoleValidationException> CreateAndLogValidationExceptionAsync(Xeption exception)
        {
            var approvalSettingPublisherRoleValidationException = new ApprovalSettingPublisherRoleValidationException(
                message: "Approval setting publisher role validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalSettingPublisherRoleValidationException);

            return approvalSettingPublisherRoleValidationException;
        }

        private async ValueTask<ApprovalSettingPublisherRoleDependencyException> CreateAndLogDependencyExceptionAsync(Xeption exception)
        {
            var approvalSettingPublisherRoleDependencyException = new ApprovalSettingPublisherRoleDependencyException(
                message: "Approval setting publisher role dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalSettingPublisherRoleDependencyException);

            return approvalSettingPublisherRoleDependencyException;
        }

        // Intentionally a named twin of CreateAndLogDependencyExceptionAsync (same wrapper,
        // same LogError): timeouts categorize as a non-critical dependency failure, but keep
        // their own seam so the call site reads as a timeout and the behavior can diverge
        // later without touching generic dependency handling. Mirrors The Standard's
        // EventHighway EventAddressV2Service.
        private async ValueTask<ApprovalSettingPublisherRoleDependencyException>
            CreateAndLogTimeoutDependencyExceptionAsync(Xeption exception)
        {
            var approvalSettingPublisherRoleDependencyException =
                new ApprovalSettingPublisherRoleDependencyException(
                    message: "Approval setting publisher role dependency error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalSettingPublisherRoleDependencyException);

            return approvalSettingPublisherRoleDependencyException;
        }

        private async ValueTask<ApprovalSettingPublisherRoleDependencyException>
            CreateAndLogCriticalDependencyExceptionAsync(Xeption exception)
        {
            var approvalSettingPublisherRoleDependencyException = new ApprovalSettingPublisherRoleDependencyException(
                message: "Approval setting publisher role dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogCriticalAsync(approvalSettingPublisherRoleDependencyException);

            return approvalSettingPublisherRoleDependencyException;
        }

        private async ValueTask<ApprovalSettingPublisherRoleDependencyValidationException> CreateAndLogDependencyValidationExceptionAsync(
            Xeption exception)
        {
            var approvalSettingPublisherRoleDependencyValidationException = new ApprovalSettingPublisherRoleDependencyValidationException(
                message: "Approval setting publisher role dependency validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalSettingPublisherRoleDependencyValidationException);

            return approvalSettingPublisherRoleDependencyValidationException;
        }

        private async ValueTask<ApprovalSettingPublisherRoleServiceException> CreateAndLogServiceExceptionAsync(Xeption exception)
        {
            var approvalSettingPublisherRoleServiceException = new ApprovalSettingPublisherRoleServiceException(
                message: "Approval setting publisher role service error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalSettingPublisherRoleServiceException);

            return approvalSettingPublisherRoleServiceException;
        }
    }
}
