// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, 'I am the way and the truth and the life.
//                  No one comes to the Father except through me.'" 
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6 
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Linq;
using System.Threading.Tasks;
using EFxceptions.Models.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Glory2Him.Core.Models.Foundations.ContentTypes.Exceptions;
using Xeptions;

namespace Glory2Him.Core.Services.Foundations.ContentTypes
{
    public partial class ContentTypeService
    {
        private delegate ValueTask<ContentType> ReturningContentTypeFunction();
        private delegate ValueTask<IQueryable<ContentType>> ReturningContentTypesFunction();

        private async ValueTask<ContentType> TryCatch(ReturningContentTypeFunction returningContentTypeFunction)
        {
            try
            {
                return await returningContentTypeFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutContentTypeException = new TimeoutContentTypeException(
                    message: "Content type timed out, contact support.",
                    innerException: new TimeoutException(),
                    data: operationCanceledException.Data);

                throw await CreateAndLogDependencyException(timeoutContentTypeException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (NullContentTypeException nullContentTypeException)
            {
                throw await CreateAndLogValidationException(nullContentTypeException);
            }
            catch (InvalidContentTypeException invalidContentTypeException)
            {
                throw await CreateAndLogValidationException(invalidContentTypeException);
            }
            catch (NotFoundContentTypeException notFoundContentTypeException)
            {
                throw await CreateAndLogValidationException(notFoundContentTypeException);
            }
            catch (SqlException sqlException)
            {
                var failedStorageContentTypeException = new FailedStorageContentTypeException(
                    message: "Failed content type storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyException(failedStorageContentTypeException);
            }
            catch (DuplicateKeyException duplicateKeyException)
            {
                var alreadyExistsContentTypeException = new AlreadyExistsContentTypeException(
                    message: "Content type already exists with the same Id.",
                    innerException: duplicateKeyException,
                    data: duplicateKeyException.Data);

                throw await CreateAndLogDependencyValidationException(alreadyExistsContentTypeException);
            }
            catch (ForeignKeyConstraintConflictException foreignKeyConstraintConflictException)
            {
                var invalidContentTypeReferenceException = new InvalidContentTypeReferenceException(
                    message: "Invalid content type reference error occurred.",
                    innerException: foreignKeyConstraintConflictException,
                    data: foreignKeyConstraintConflictException.Data);

                throw await CreateAndLogDependencyValidationException(invalidContentTypeReferenceException);
            }
            catch (DbUpdateException dbUpdateException)
            {
                var failedStorageContentTypeException = new FailedStorageContentTypeException(
                    message: "Failed content type storage error occurred, contact support.",
                    innerException: dbUpdateException,
                    data: dbUpdateException.Data);

                throw await CreateAndLogDependencyException(failedStorageContentTypeException);
            }
            catch (Exception exception)
            {
                var failedContentTypeServiceException = new FailedContentTypeServiceException(
                    message: "Failed content type service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceException(failedContentTypeServiceException);
            }
        }

        private async ValueTask<IQueryable<ContentType>> TryCatch(
            ReturningContentTypesFunction returningContentTypesFunction)
        {
            try
            {
                return await returningContentTypesFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutContentTypeException = new TimeoutContentTypeException(
                    message: "Content type timed out, contact support.",
                    innerException: new TimeoutException(),
                    data: operationCanceledException.Data);

                throw await CreateAndLogDependencyException(timeoutContentTypeException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (SqlException sqlException)
            {
                var failedStorageContentTypeException = new FailedStorageContentTypeException(
                    message: "Failed content type storage error occurred, contact support.",
                    innerException: sqlException,
                    data: sqlException.Data);

                throw await CreateAndLogCriticalDependencyException(failedStorageContentTypeException);
            }
            catch (Exception exception)
            {
                var failedContentTypeServiceException = new FailedContentTypeServiceException(
                    message: "Failed content type service error occurred, please contact support.",
                    innerException: exception,
                    data: exception.Data);

                throw await CreateAndLogServiceException(failedContentTypeServiceException);
            }
        }

        private async ValueTask<ContentTypeValidationException> CreateAndLogValidationException(Xeption exception)
        {
            var contentTypeValidationException = new ContentTypeValidationException(
                message: "Content type validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(contentTypeValidationException);

            return contentTypeValidationException;
        }

        private async ValueTask<ContentTypeDependencyException> CreateAndLogDependencyException(Xeption exception)
        {
            var contentTypeDependencyException = new ContentTypeDependencyException(
                message: "Content type dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(contentTypeDependencyException);

            return contentTypeDependencyException;
        }

        private async ValueTask<ContentTypeDependencyException> CreateAndLogCriticalDependencyException(Xeption exception)
        {
            var contentTypeDependencyException = new ContentTypeDependencyException(
                message: "Content type dependency error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogCriticalAsync(contentTypeDependencyException);

            return contentTypeDependencyException;
        }

        private async ValueTask<ContentTypeDependencyValidationException> CreateAndLogDependencyValidationException(
            Xeption exception)
        {
            var contentTypeDependencyValidationException = new ContentTypeDependencyValidationException(
                message: "Content type dependency validation error occurred, fix the errors and try again.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(contentTypeDependencyValidationException);

            return contentTypeDependencyValidationException;
        }

        private async ValueTask<ContentTypeServiceException> CreateAndLogServiceException(Xeption exception)
        {
            var contentTypeServiceException = new ContentTypeServiceException(
                message: "Content type service error occurred, contact support.",
                innerException: exception);

            await this.loggingBroker.LogErrorAsync(contentTypeServiceException);

            return contentTypeServiceException;
        }
    }
}
