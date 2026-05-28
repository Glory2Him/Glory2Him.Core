// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Threading.Tasks;
using EFxceptions.Models.Exceptions;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Microsoft.Data.SqlClient;

namespace Glory2Him.Core.Services.Foundations.ContentItems
{
    public partial class ContentItemService
    {
        private delegate ValueTask<ContentItem> ReturningContentItemFunction();

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

                var contentItemDependencyException = new ContentItemDependencyException(
                    message: "Content item dependency error occurred, contact support.",
                    innerException: timeoutContentItemException);

                await this.loggingBroker.LogErrorAsync(contentItemDependencyException);

                throw contentItemDependencyException;
            }
            catch (SqlException sqlException)
            {
                var failedStorageContentItemException = new FailedStorageContentItemException(
                    message: "Failed content item storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                var contentItemDependencyException = new ContentItemDependencyException(
                    message: "Content item dependency error occurred, contact support.",
                    innerException: failedStorageContentItemException);

                await this.loggingBroker.LogCriticalAsync(contentItemDependencyException);

                throw contentItemDependencyException;
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsContentItemException = new AlreadyExistsContentItemException(
                    message: "Content item already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                var contentItemDependencyValidationException = new ContentItemDependencyValidationException(
                    message: "Content item dependency validation error occurred, fix the errors and try again.",
                    innerException: alreadyExistsContentItemException);

                await this.loggingBroker.LogErrorAsync(contentItemDependencyValidationException);

                throw contentItemDependencyValidationException;
            }
            catch (NullContentItemException nullContentItemException)
            {
                var contentItemValidationException = new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: nullContentItemException);

                await this.loggingBroker.LogErrorAsync(contentItemValidationException);

                throw contentItemValidationException;
            }
            catch (InvalidContentItemException invalidContentItemException)
            {
                var contentItemValidationException = new ContentItemValidationException(
                    message: "Content item validation error occurred, fix the errors and try again.",
                    innerException: invalidContentItemException);

                await this.loggingBroker.LogErrorAsync(contentItemValidationException);

                throw contentItemValidationException;
            }
        }
    }
}
