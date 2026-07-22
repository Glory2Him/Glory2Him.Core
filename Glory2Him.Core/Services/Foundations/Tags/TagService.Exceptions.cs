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
using Glory2Him.Core.Models.Foundations.Tags;
using Glory2Him.Core.Models.Foundations.Tags.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xeptions;

namespace Glory2Him.Core.Services.Foundations.Tags
{
    public partial class TagService
    {
        private delegate ValueTask<Tag> ReturningTagFunction();
        private delegate ValueTask<IQueryable<Tag>> ReturningTagsFunction();

        private delegate ValueTask<EventEnvelope<Tag>?>
            ReturningTagEventEnvelopeFunction();

        // The event-path wrapper: categorizes failures with the same taxonomy as the
        // non-event TryCatch (so the two entry paths cannot diverge), plus the envelope
        // guard that only exists on this path, and ALWAYS rethrows so the substrate records
        // the delivery as Error and drives retries. Exceptions already categorized by nested
        // service calls pass through unwrapped.
        private async ValueTask<EventEnvelope<Tag>?> TryCatchSubstrate(
            ReturningTagEventEnvelopeFunction returningTagEventEnvelopeFunction)
        {
            try
            {
                return await returningTagEventEnvelopeFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutTagException =
                    new TimeoutTagException(
                        message: "Failed tag timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutTagException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidTagEventException invalidTagEventException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidTagEventException);
            }
            catch (NullTagException nullTagException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullTagException);
            }
            catch (InvalidTagException invalidTagException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidTagException);
            }
            catch (NotFoundTagException notFoundTagException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundTagException);
            }
            catch (TagValidationException)
            {
                throw;
            }
            catch (TagDependencyValidationException)
            {
                throw;
            }
            catch (TagDependencyException)
            {
                throw;
            }
            catch (TagServiceException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageTagException = new FailedStorageTagException(
                    message: "Failed tag storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageTagException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsTagException = new AlreadyExistsTagException(
                    message: "Tag already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsTagException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidTagReferenceException = new InvalidTagReferenceException(
                    message: "Invalid tag reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidTagReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedTagException = new LockedTagException(
                    message: "Locked tag record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedTagException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageTagException = new FailedStorageTagException(
                    message: "Failed tag storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageTagException);
            }
            catch (Exception exception)
            {
                var failedTagServiceException = new FailedTagServiceException(
                    message: "Failed tag service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedTagServiceException);
            }
        }

        private async ValueTask<Tag> TryCatch(ReturningTagFunction returningTagFunction)
        {
            try
            {
                return await returningTagFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutTagException =
                    new TimeoutTagException(
                        message: "Failed tag timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutTagException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (NullTagException nullTagException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullTagException);
            }
            catch (InvalidTagException invalidTagException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidTagException);
            }
            catch (NotFoundTagException notFoundTagException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundTagException);
            }
            catch (SqlException sqlException)
            {
                var failedStorageTagException = new FailedStorageTagException(
                    message: "Failed tag storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageTagException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsTagException = new AlreadyExistsTagException(
                    message: "Tag already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsTagException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidTagReferenceException = new InvalidTagReferenceException(
                    message: "Invalid tag reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidTagReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedTagException = new LockedTagException(
                    message: "Locked tag record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedTagException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageTagException = new FailedStorageTagException(
                    message: "Failed tag storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageTagException);
            }
            catch (Exception exception)
            {
                var failedTagServiceException = new FailedTagServiceException(
                    message: "Failed tag service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedTagServiceException);
            }
        }

        private async ValueTask<IQueryable<Tag>> TryCatch(
            ReturningTagsFunction returningTagsFunction)
        {
            try
            {
                return await returningTagsFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutTagException =
                    new TimeoutTagException(
                        message: "Failed tag timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutTagException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageTagException = new FailedStorageTagException(
                    message: "Failed tag storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageTagException);
            }
            catch (Exception exception)
            {
                var failedTagServiceException = new FailedTagServiceException(
                    message: "Failed tag service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedTagServiceException);
            }
        }

        private async ValueTask<TagValidationException> CreateAndLogValidationExceptionAsync(Xeption exception)
        {
            var tagValidationException = new TagValidationException(
                message: "Tag validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(tagValidationException);

            return tagValidationException;
        }

        private async ValueTask<TagDependencyException> CreateAndLogDependencyExceptionAsync(Xeption exception)
        {
            var tagDependencyException = new TagDependencyException(
                message: "Tag dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(tagDependencyException);

            return tagDependencyException;
        }

        // Intentionally a named twin of CreateAndLogDependencyExceptionAsync (same wrapper,
        // same LogError): timeouts categorize as a non-critical dependency failure, but keep
        // their own seam so the call site reads as a timeout and the behavior can diverge
        // later without touching generic dependency handling. Mirrors The Standard's
        // EventHighway EventAddressV2Service.
        private async ValueTask<TagDependencyException>
            CreateAndLogTimeoutDependencyExceptionAsync(Xeption exception)
        {
            var tagDependencyException =
                new TagDependencyException(
                    message: "Tag dependency error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(tagDependencyException);

            return tagDependencyException;
        }

        private async ValueTask<TagDependencyException>
            CreateAndLogCriticalDependencyExceptionAsync(Xeption exception)
        {
            var tagDependencyException = new TagDependencyException(
                message: "Tag dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogCriticalAsync(tagDependencyException);

            return tagDependencyException;
        }

        private async ValueTask<TagDependencyValidationException> CreateAndLogDependencyValidationExceptionAsync(
            Xeption exception)
        {
            var tagDependencyValidationException = new TagDependencyValidationException(
                message: "Tag dependency validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(tagDependencyValidationException);

            return tagDependencyValidationException;
        }

        private async ValueTask<TagServiceException> CreateAndLogServiceExceptionAsync(Xeption exception)
        {
            var tagServiceException = new TagServiceException(
                message: "Tag service error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(tagServiceException);

            return tagServiceException;
        }
    }
}
