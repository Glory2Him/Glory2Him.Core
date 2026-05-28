// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Linq;
using System.Threading.Tasks;
using EFxceptions.Models.Exceptions;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Microsoft.Data.SqlClient;
using Xeptions;

namespace Glory2Him.Core.Services.Foundations.ContentItems
{
    public partial class ContentItemService
    {
        private delegate ValueTask<ContentItem> ReturningContentItemFunction();
        private delegate ValueTask<IQueryable<ContentItem>> ReturningContentItemsFunction();

        private async ValueTask<ContentItem> TryCatch(ReturningContentItemFunction returningContentItemFunction)
        {
            try
            {
                return await returningContentItemFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutContentItemException = new TimeoutContentItemException(
                    message: "Content item timed out, contact support.",
                    innerException: new TimeoutException(),
                    data: operationCanceledException.Data);

                throw await CreateAndLogDependencyException(timeoutContentItemException);
            }
            catch (SqlException sqlException)
            {
                var failedStorageContentItemException = new FailedStorageContentItemException(
                    message: "Failed content item storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyException(failedStorageContentItemException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsContentItemException = new AlreadyExistsContentItemException(
                    message: "Content item already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationException(alreadyExistsContentItemException);
            }
            catch (NullContentItemException nullContentItemException)
            {
                throw await CreateAndLogValidationException(nullContentItemException);
            }
            catch (InvalidContentItemException invalidContentItemException)
            {
                throw await CreateAndLogValidationException(invalidContentItemException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                var failedContentItemServiceException = new FailedContentItemServiceException(
                    message: "Failed content item service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceException(failedContentItemServiceException);
            }
        }

        private async ValueTask<IQueryable<ContentItem>> TryCatch(
            ReturningContentItemsFunction returningContentItemsFunction)
        {
            try
            {
                return await returningContentItemsFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutContentItemException = new TimeoutContentItemException(
                    message: "Content item timed out, contact support.",
                    innerException: new TimeoutException(),
                    data: operationCanceledException.Data);

                throw await CreateAndLogDependencyException(timeoutContentItemException);
            }
            catch (SqlException sqlException)
            {
                var failedStorageContentItemException = new FailedStorageContentItemException(
                    message: "Failed content item storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyException(failedStorageContentItemException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                var failedContentItemServiceException = new FailedContentItemServiceException(
                    message: "Failed content item service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceException(failedContentItemServiceException);
            }
        }

        private async ValueTask<ContentItemValidationException> CreateAndLogValidationException(Xeption exception)
        {
            var contentItemValidationException = new ContentItemValidationException(
                message: "Content item validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(contentItemValidationException);

            return contentItemValidationException;
        }

        private async ValueTask<ContentItemDependencyException> CreateAndLogCriticalDependencyException(
            Xeption exception)
        {
            var contentItemDependencyException = new ContentItemDependencyException(
                message: "Content item dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogCriticalAsync(contentItemDependencyException);

            return contentItemDependencyException;
        }

        private async ValueTask<ContentItemDependencyValidationException> CreateAndLogDependencyValidationException(
            Xeption exception)
        {
            var contentItemDependencyValidationException = new ContentItemDependencyValidationException(
                message: "Content item dependency validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(contentItemDependencyValidationException);

            return contentItemDependencyValidationException;
        }

        private async ValueTask<ContentItemDependencyException> CreateAndLogDependencyException(Xeption exception)
        {
            var contentItemDependencyException = new ContentItemDependencyException(
                message: "Content item dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(contentItemDependencyException);

            return contentItemDependencyException;
        }

        private async ValueTask<ContentItemServiceException> CreateAndLogServiceException(Xeption exception)
        {
            var contentItemServiceException = new ContentItemServiceException(
                message: "Content item service error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(contentItemServiceException);

            return contentItemServiceException;
        }
    }
}
