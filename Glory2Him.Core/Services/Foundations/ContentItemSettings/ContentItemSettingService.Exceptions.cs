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
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Foundations.ContentItemSettings.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xeptions;

namespace Glory2Him.Core.Services.Foundations.ContentItemSettings
{
    internal partial class ContentItemSettingService
    {
        private delegate ValueTask<ContentItemSetting> ReturningContentItemSettingFunction();
        private delegate ValueTask<IQueryable<ContentItemSetting>> ReturningContentItemSettingsFunction();

        private delegate ValueTask<EventEnvelope<ContentItemSetting>?>
            ReturningContentItemSettingEventEnvelopeFunction();

        // The event-path wrapper: categorizes failures with the same taxonomy as the
        // non-event TryCatch (so the two entry paths cannot diverge), plus the envelope
        // guard that only exists on this path, and ALWAYS rethrows so the substrate records
        // the delivery as Error and drives retries. Exceptions already categorized by nested
        // service calls pass through unwrapped.
        private async ValueTask<EventEnvelope<ContentItemSetting>?> TryCatchSubstrate(
            ReturningContentItemSettingEventEnvelopeFunction returningContentItemSettingEventEnvelopeFunction)
        {
            try
            {
                return await returningContentItemSettingEventEnvelopeFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutContentItemSettingException =
                    new TimeoutContentItemSettingException(
                        message: "Failed content item setting timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutContentItemSettingException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidContentItemSettingEventException invalidContentItemSettingEventException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidContentItemSettingEventException);
            }
            catch (NullContentItemSettingException nullContentItemSettingException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullContentItemSettingException);
            }
            catch (InvalidContentItemSettingException invalidContentItemSettingException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidContentItemSettingException);
            }
            catch (NotFoundContentItemSettingException notFoundContentItemSettingException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundContentItemSettingException);
            }
            catch (ContentItemSettingValidationException)
            {
                throw;
            }
            catch (ContentItemSettingDependencyValidationException)
            {
                throw;
            }
            catch (ContentItemSettingDependencyException)
            {
                throw;
            }
            catch (ContentItemSettingServiceException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageContentItemSettingException = new FailedStorageContentItemSettingException(
                    message: "Failed content item setting storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(
                    exception: failedStorageContentItemSettingException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsContentItemSettingException = new AlreadyExistsContentItemSettingException(
                    message: "Content item setting already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsContentItemSettingException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidContentItemSettingReferenceException = new InvalidContentItemSettingReferenceException(
                    message: "Invalid content item setting reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidContentItemSettingReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedContentItemSettingException = new LockedContentItemSettingException(
                    message: "Locked content item setting record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedContentItemSettingException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageContentItemSettingException = new FailedStorageContentItemSettingException(
                    message: "Failed content item setting storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageContentItemSettingException);
            }
            catch (Exception exception)
            {
                var failedContentItemSettingServiceException = new FailedContentItemSettingServiceException(
                    message: "Failed content item setting service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedContentItemSettingServiceException);
            }
        }

        private async ValueTask<ContentItemSetting> TryCatch(
            ReturningContentItemSettingFunction returningContentItemSettingFunction)
        {
            try
            {
                return await returningContentItemSettingFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutContentItemSettingException =
                    new TimeoutContentItemSettingException(
                        message: "Failed content item setting timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutContentItemSettingException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (NullContentItemSettingException nullContentItemSettingException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullContentItemSettingException);
            }
            catch (InvalidContentItemSettingException invalidContentItemSettingException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidContentItemSettingException);
            }
            catch (NotFoundContentItemSettingException notFoundContentItemSettingException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundContentItemSettingException);
            }
            catch (SqlException sqlException)
            {
                var failedStorageContentItemSettingException = new FailedStorageContentItemSettingException(
                    message: "Failed content item setting storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(
                    exception: failedStorageContentItemSettingException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsContentItemSettingException = new AlreadyExistsContentItemSettingException(
                    message: "Content item setting already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsContentItemSettingException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidContentItemSettingReferenceException = new InvalidContentItemSettingReferenceException(
                    message: "Invalid content item setting reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidContentItemSettingReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedContentItemSettingException = new LockedContentItemSettingException(
                    message: "Locked content item setting record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedContentItemSettingException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageContentItemSettingException = new FailedStorageContentItemSettingException(
                    message: "Failed content item setting storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageContentItemSettingException);
            }
            catch (Exception exception)
            {
                var failedContentItemSettingServiceException = new FailedContentItemSettingServiceException(
                    message: "Failed content item setting service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedContentItemSettingServiceException);
            }
        }

        private async ValueTask<IQueryable<ContentItemSetting>> TryCatch(
            ReturningContentItemSettingsFunction returningContentItemSettingsFunction)
        {
            try
            {
                return await returningContentItemSettingsFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutContentItemSettingException =
                    new TimeoutContentItemSettingException(
                        message: "Failed content item setting timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutContentItemSettingException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageContentItemSettingException = new FailedStorageContentItemSettingException(
                    message: "Failed content item setting storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(
                    exception: failedStorageContentItemSettingException);
            }
            catch (Exception exception)
            {
                var failedContentItemSettingServiceException = new FailedContentItemSettingServiceException(
                    message: "Failed content item setting service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedContentItemSettingServiceException);
            }
        }

        private async ValueTask<ContentItemSettingValidationException>
            CreateAndLogValidationExceptionAsync(Xeption exception)
        {
            var contentItemSettingValidationException = new ContentItemSettingValidationException(
                message: "Content item setting validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(contentItemSettingValidationException);

            return contentItemSettingValidationException;
        }

        private async ValueTask<ContentItemSettingDependencyException>
            CreateAndLogDependencyExceptionAsync(Xeption exception)
        {
            var contentItemSettingDependencyException = new ContentItemSettingDependencyException(
                message: "Content item setting dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(contentItemSettingDependencyException);

            return contentItemSettingDependencyException;
        }

        // Intentionally a named twin of CreateAndLogDependencyExceptionAsync (same wrapper,
        // same LogError): timeouts categorize as a non-critical dependency failure, but keep
        // their own seam so the call site reads as a timeout and the behavior can diverge
        // later without touching generic dependency handling. Mirrors The Standard's
        // EventHighway EventAddressV2Service.
        private async ValueTask<ContentItemSettingDependencyException>
            CreateAndLogTimeoutDependencyExceptionAsync(Xeption exception)
        {
            var contentItemSettingDependencyException =
                new ContentItemSettingDependencyException(
                    message: "Content item setting dependency error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(contentItemSettingDependencyException);

            return contentItemSettingDependencyException;
        }

        private async ValueTask<ContentItemSettingDependencyException>
            CreateAndLogCriticalDependencyExceptionAsync(Xeption exception)
        {
            var contentItemSettingDependencyException = new ContentItemSettingDependencyException(
                message: "Content item setting dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogCriticalAsync(contentItemSettingDependencyException);

            return contentItemSettingDependencyException;
        }

        private async ValueTask<ContentItemSettingDependencyValidationException>
            CreateAndLogDependencyValidationExceptionAsync(
            Xeption exception)
        {
            var contentItemSettingDependencyValidationException = new ContentItemSettingDependencyValidationException(
                message: "Content item setting dependency validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(contentItemSettingDependencyValidationException);

            return contentItemSettingDependencyValidationException;
        }

        private async ValueTask<ContentItemSettingServiceException> CreateAndLogServiceExceptionAsync(Xeption exception)
        {
            var contentItemSettingServiceException = new ContentItemSettingServiceException(
                message: "Content item setting service error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(contentItemSettingServiceException);

            return contentItemSettingServiceException;
        }
    }
}
