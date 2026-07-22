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
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Links.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xeptions;

namespace Glory2Him.Core.Services.Foundations.Links
{
    public partial class LinkService
    {
        private delegate ValueTask<Link> ReturningLinkFunction();
        private delegate ValueTask<IQueryable<Link>> ReturningLinksFunction();

        private delegate ValueTask<EventEnvelope<Link>?>
            ReturningLinkEventEnvelopeFunction();

        // The event-path wrapper: categorizes failures with the same taxonomy as the
        // non-event TryCatch (so the two entry paths cannot diverge), plus the envelope
        // guard that only exists on this path, and ALWAYS rethrows so the substrate records
        // the delivery as Error and drives retries. Exceptions already categorized by nested
        // service calls pass through unwrapped.
        private async ValueTask<EventEnvelope<Link>?> TryCatchSubstrate(
            ReturningLinkEventEnvelopeFunction returningLinkEventEnvelopeFunction)
        {
            try
            {
                return await returningLinkEventEnvelopeFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutLinkException =
                    new TimeoutLinkException(
                        message: "Failed link timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutLinkException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (InvalidLinkEventException invalidLinkEventException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidLinkEventException);
            }
            catch (NullLinkException nullLinkException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullLinkException);
            }
            catch (InvalidLinkException invalidLinkException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidLinkException);
            }
            catch (NotFoundLinkException notFoundLinkException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundLinkException);
            }
            catch (LinkValidationException)
            {
                throw;
            }
            catch (LinkDependencyValidationException)
            {
                throw;
            }
            catch (LinkDependencyException)
            {
                throw;
            }
            catch (LinkServiceException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageLinkException = new FailedStorageLinkException(
                    message: "Failed link storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageLinkException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsLinkException = new AlreadyExistsLinkException(
                    message: "Link already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsLinkException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidLinkReferenceException = new InvalidLinkReferenceException(
                    message: "Invalid link reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidLinkReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedLinkException = new LockedLinkException(
                    message: "Locked link record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedLinkException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageLinkException = new FailedStorageLinkException(
                    message: "Failed link storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageLinkException);
            }
            catch (Exception exception)
            {
                var failedLinkServiceException = new FailedLinkServiceException(
                    message: "Failed link service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedLinkServiceException);
            }
        }

        private async ValueTask<Link> TryCatch(ReturningLinkFunction returningLinkFunction)
        {
            try
            {
                return await returningLinkFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutLinkException =
                    new TimeoutLinkException(
                        message: "Failed link timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutLinkException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (NullLinkException nullLinkException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: nullLinkException);
            }
            catch (InvalidLinkException invalidLinkException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: invalidLinkException);
            }
            catch (NotFoundLinkException notFoundLinkException)
            {
                throw await CreateAndLogValidationExceptionAsync(exception: notFoundLinkException);
            }
            catch (SqlException sqlException)
            {
                var failedStorageLinkException = new FailedStorageLinkException(
                    message: "Failed link storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageLinkException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsLinkException = new AlreadyExistsLinkException(
                    message: "Link already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(alreadyExistsLinkException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidLinkReferenceException = new InvalidLinkReferenceException(
                    message: "Invalid link reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(invalidLinkReferenceException);
            }
            catch (DbUpdateConcurrencyException dbUpdateConcurrencyException)
            {
                var lockedLinkException = new LockedLinkException(
                    message: "Locked link record, please try again later.",
                    innerException: dbUpdateConcurrencyException,
                    data: dbUpdateConcurrencyException.Data);

                throw await CreateAndLogDependencyValidationExceptionAsync(lockedLinkException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageLinkException = new FailedStorageLinkException(
                    message: "Failed link storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyExceptionAsync(failedStorageLinkException);
            }
            catch (Exception exception)
            {
                var failedLinkServiceException = new FailedLinkServiceException(
                    message: "Failed link service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedLinkServiceException);
            }
        }

        private async ValueTask<IQueryable<Link>> TryCatch(
            ReturningLinksFunction returningLinksFunction)
        {
            try
            {
                return await returningLinksFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutLinkException =
                    new TimeoutLinkException(
                        message: "Failed link timeout error occurred, contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(exception: timeoutLinkException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageLinkException = new FailedStorageLinkException(
                    message: "Failed link storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyExceptionAsync(exception: failedStorageLinkException);
            }
            catch (Exception exception)
            {
                var failedLinkServiceException = new FailedLinkServiceException(
                    message: "Failed link service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(failedLinkServiceException);
            }
        }

        private async ValueTask<LinkValidationException> CreateAndLogValidationExceptionAsync(Xeption exception)
        {
            var linkValidationException = new LinkValidationException(
                message: "Link validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(linkValidationException);

            return linkValidationException;
        }

        private async ValueTask<LinkDependencyException> CreateAndLogDependencyExceptionAsync(Xeption exception)
        {
            var linkDependencyException = new LinkDependencyException(
                message: "Link dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(linkDependencyException);

            return linkDependencyException;
        }

        // Intentionally a named twin of CreateAndLogDependencyExceptionAsync (same wrapper,
        // same LogError): timeouts categorize as a non-critical dependency failure, but keep
        // their own seam so the call site reads as a timeout and the behavior can diverge
        // later without touching generic dependency handling. Mirrors The Standard's
        // EventHighway EventAddressV2Service.
        private async ValueTask<LinkDependencyException>
            CreateAndLogTimeoutDependencyExceptionAsync(Xeption exception)
        {
            var linkDependencyException =
                new LinkDependencyException(
                    message: "Link dependency error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(linkDependencyException);

            return linkDependencyException;
        }

        private async ValueTask<LinkDependencyException>
            CreateAndLogCriticalDependencyExceptionAsync(Xeption exception)
        {
            var linkDependencyException = new LinkDependencyException(
                message: "Link dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogCriticalAsync(linkDependencyException);

            return linkDependencyException;
        }

        private async ValueTask<LinkDependencyValidationException> CreateAndLogDependencyValidationExceptionAsync(
            Xeption exception)
        {
            var linkDependencyValidationException = new LinkDependencyValidationException(
                message: "Link dependency validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(linkDependencyValidationException);

            return linkDependencyValidationException;
        }

        private async ValueTask<LinkServiceException> CreateAndLogServiceExceptionAsync(Xeption exception)
        {
            var linkServiceException = new LinkServiceException(
                message: "Link service error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(linkServiceException);

            return linkServiceException;
        }
    }
}
