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
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Foundations.ApprovalSettings.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xeptions;

namespace Glory2Him.Core.Services.Foundations.ApprovalSettings
{
    internal partial class ApprovalSettingService
    {
        private delegate ValueTask<ApprovalSetting> ReturningApprovalSettingFunction();
        private delegate ValueTask<IQueryable<ApprovalSetting>> ReturningApprovalSettingsFunction();

        private delegate ValueTask<EventEnvelope<ApprovalSetting>?>
            ReturningApprovalSettingEventEnvelopeFunction();

        // The event-path wrapper: categorizes failures with the same taxonomy as the
        // non-event TryCatch (so the two entry paths cannot diverge), plus the envelope
        // guard that only exists on this path, and ALWAYS rethrows so the substrate records
        // the delivery as Error and drives retries. Exceptions already categorized by nested
        // service calls pass through unwrapped.
        private async ValueTask<EventEnvelope<ApprovalSetting>?> TryCatchSubstrate(
            ReturningApprovalSettingEventEnvelopeFunction returningApprovalSettingEventEnvelopeFunction)
        {
            try
            {
                return await returningApprovalSettingEventEnvelopeFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutApprovalSettingException =
                    new TimeoutApprovalSettingException(
                        message: "Failed approval setting timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutApprovalSettingException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidApprovalSettingEventException invalidApprovalSettingEventException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidApprovalSettingEventException);
            }
            catch (UnauthorizedApprovalSettingException unauthorizedApprovalSettingException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: unauthorizedApprovalSettingException);
            }
            catch (NullApprovalSettingException nullApprovalSettingException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullApprovalSettingException);
            }
            catch (InvalidApprovalSettingException invalidApprovalSettingException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidApprovalSettingException);
            }
            catch (NotFoundApprovalSettingException notFoundApprovalSettingException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundApprovalSettingException);
            }
            catch (ApprovalSettingValidationException)
            {
                throw;
            }
            catch (ApprovalSettingDependencyValidationException)
            {
                throw;
            }
            catch (ApprovalSettingDependencyException)
            {
                throw;
            }
            catch (ApprovalSettingServiceException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageApprovalSettingException = new FailedStorageApprovalSettingException(
                    message: "Failed approval setting storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(
                    exception: failedStorageApprovalSettingException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsApprovalSettingException = new AlreadyExistsApprovalSettingException(
                    message: "Approval setting already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsApprovalSettingException);
            }
            // A unique-INDEX violation (EF's HasIndex().IsUnique(), and the ProcessedEvents
            // dedup index) arrives as a type that does NOT derive from DuplicateKeyException,
            // so the clause above misses it; without this it falls through to the general
            // handler and mis-reports a business-key collision as "our code is broken".
            catch (DuplicateKeyWithUniqueIndexException duplicateKeyWithUniqueIndexException)
            {
                var alreadyExistsApprovalSettingException = new AlreadyExistsApprovalSettingException(
                    message: "Approval setting already exists, "
                        + "a uniqueness rule rejected the write.",
                    innerException: duplicateKeyWithUniqueIndexException,
                    data: duplicateKeyWithUniqueIndexException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(
                    alreadyExistsApprovalSettingException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidApprovalSettingReferenceException = new InvalidApprovalSettingReferenceException(
                    message: "Invalid approval setting reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidApprovalSettingReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedApprovalSettingException = new LockedApprovalSettingException(
                    message: "Locked approval setting record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedApprovalSettingException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageApprovalSettingException = new FailedStorageApprovalSettingException(
                    message: "Failed approval setting storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageApprovalSettingException);
            }
            catch (Exception exception)
            {
                var failedApprovalSettingServiceException = new FailedApprovalSettingServiceException(
                    message: "Failed approval setting service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedApprovalSettingServiceException);
            }
        }

        private async ValueTask<ApprovalSetting> TryCatch(
            ReturningApprovalSettingFunction returningApprovalSettingFunction)
        {
            try
            {
                return await returningApprovalSettingFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutApprovalSettingException =
                    new TimeoutApprovalSettingException(
                        message: "Failed approval setting timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutApprovalSettingException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UnauthorizedApprovalSettingException unauthorizedApprovalSettingException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: unauthorizedApprovalSettingException);
            }
            catch (NullApprovalSettingException nullApprovalSettingException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullApprovalSettingException);
            }
            catch (InvalidApprovalSettingException invalidApprovalSettingException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidApprovalSettingException);
            }
            catch (NotFoundApprovalSettingException notFoundApprovalSettingException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundApprovalSettingException);
            }
            catch (SqlException sqlException)
            {
                var failedStorageApprovalSettingException = new FailedStorageApprovalSettingException(
                    message: "Failed approval setting storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(
                    exception: failedStorageApprovalSettingException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsApprovalSettingException = new AlreadyExistsApprovalSettingException(
                    message: "Approval setting already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsApprovalSettingException);
            }
            // A unique-INDEX violation (EF's HasIndex().IsUnique(), and the ProcessedEvents
            // dedup index) arrives as a type that does NOT derive from DuplicateKeyException,
            // so the clause above misses it; without this it falls through to the general
            // handler and mis-reports a business-key collision as "our code is broken".
            catch (DuplicateKeyWithUniqueIndexException duplicateKeyWithUniqueIndexException)
            {
                var alreadyExistsApprovalSettingException = new AlreadyExistsApprovalSettingException(
                    message: "Approval setting already exists, "
                        + "a uniqueness rule rejected the write.",
                    innerException: duplicateKeyWithUniqueIndexException,
                    data: duplicateKeyWithUniqueIndexException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(
                    alreadyExistsApprovalSettingException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidApprovalSettingReferenceException = new InvalidApprovalSettingReferenceException(
                    message: "Invalid approval setting reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidApprovalSettingReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedApprovalSettingException = new LockedApprovalSettingException(
                    message: "Locked approval setting record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedApprovalSettingException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageApprovalSettingException = new FailedStorageApprovalSettingException(
                    message: "Failed approval setting storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageApprovalSettingException);
            }
            catch (Exception exception)
            {
                var failedApprovalSettingServiceException = new FailedApprovalSettingServiceException(
                    message: "Failed approval setting service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedApprovalSettingServiceException);
            }
        }

        private async ValueTask<IQueryable<ApprovalSetting>> TryCatch(
            ReturningApprovalSettingsFunction returningApprovalSettingsFunction)
        {
            try
            {
                return await returningApprovalSettingsFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutApprovalSettingException =
                    new TimeoutApprovalSettingException(
                        message: "Failed approval setting timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutApprovalSettingException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageApprovalSettingException = new FailedStorageApprovalSettingException(
                    message: "Failed approval setting storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(
                    exception: failedStorageApprovalSettingException);
            }
            catch (Exception exception)
            {
                var failedApprovalSettingServiceException = new FailedApprovalSettingServiceException(
                    message: "Failed approval setting service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedApprovalSettingServiceException);
            }
        }

        private async ValueTask<ApprovalSettingValidationException>
            CreateAndLogValidationExceptionAsync(Xeption exception)
        {
            var approvalSettingValidationException = new ApprovalSettingValidationException(
                message: "Approval setting validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalSettingValidationException);

            return approvalSettingValidationException;
        }

        private async ValueTask<ApprovalSettingDependencyException>
            CreateAndLogDependencyExceptionAsync(Xeption exception)
        {
            var approvalSettingDependencyException = new ApprovalSettingDependencyException(
                message: "Approval setting dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalSettingDependencyException);

            return approvalSettingDependencyException;
        }

        // Intentionally a named twin of CreateAndLogDependencyExceptionAsync (same wrapper,
        // same LogError): timeouts categorize as a non-critical dependency failure, but keep
        // their own seam so the call site reads as a timeout and the behavior can diverge
        // later without touching generic dependency handling. Mirrors The Standard's
        // EventHighway EventAddressV2Service.
        private async ValueTask<ApprovalSettingDependencyException>
            CreateAndLogTimeoutDependencyExceptionAsync(Xeption exception)
        {
            var approvalSettingDependencyException =
                new ApprovalSettingDependencyException(
                    message: "Approval setting dependency error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalSettingDependencyException);

            return approvalSettingDependencyException;
        }

        private async ValueTask<ApprovalSettingDependencyException>
            CreateAndLogCriticalDependencyExceptionAsync(Xeption exception)
        {
            var approvalSettingDependencyException = new ApprovalSettingDependencyException(
                message: "Approval setting dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogCriticalAsync(approvalSettingDependencyException);

            return approvalSettingDependencyException;
        }

        private async ValueTask<ApprovalSettingDependencyValidationException>
            CreateAndLogDependencyValidationExceptionAsync(Xeption exception)
        {
            var approvalSettingDependencyValidationException = new ApprovalSettingDependencyValidationException(
                message: "Approval setting dependency validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalSettingDependencyValidationException);

            return approvalSettingDependencyValidationException;
        }

        private async ValueTask<ApprovalSettingServiceException> CreateAndLogServiceExceptionAsync(Xeption exception)
        {
            var approvalSettingServiceException = new ApprovalSettingServiceException(
                message: "Approval setting service error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(approvalSettingServiceException);

            return approvalSettingServiceException;
        }
    }
}
