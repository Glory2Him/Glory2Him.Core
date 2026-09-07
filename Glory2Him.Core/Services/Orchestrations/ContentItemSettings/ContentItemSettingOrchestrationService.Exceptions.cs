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
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Foundations.ContentItemSettings.Exceptions;
using Glory2Him.Core.Models.Orchestrations.ContentItemSettings.Exceptions;
using Xeptions;

namespace Glory2Him.Core.Services.Orchestrations.ContentItemSettings
{
    internal partial class ContentItemSettingOrchestrationService
    {
        // Generic in the return type so the entity write path and the event path share ONE catch
        // chain. The event handler returns a reply envelope rather than an entity, and a second
        // chain for that second shape is the kind of duplication that drifts: a family added to
        // one and forgotten on the other surfaces as a raw foundation exception escaping the layer
        // (§12.2), and nothing fails until it does. ApprovalOrchestrationService is written this
        // way for the same reason.
        private delegate ValueTask<T> ReturningValueFunction<T>();

        private delegate ValueTask<IQueryable<ContentItemSetting>>
            ReturningContentItemSettingsFunction();

        private async ValueTask<T> TryCatch<T>(
            ReturningValueFunction<T> returningValueFunction)
        {
            try
            {
                return await returningValueFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutContentItemSettingOrchestrationException =
                    new TimeoutContentItemSettingOrchestrationException(
                        message: "Failed content item setting orchestration timeout error occurred, "
                            + "contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(
                    exception: timeoutContentItemSettingOrchestrationException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }

            // the orchestration's own validation failures
            catch (NullContentItemSettingOrchestrationException nullContentItemSettingOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: nullContentItemSettingOrchestrationException);
            }
            catch (NotFoundContentItemSettingOrchestrationException notFoundContentItemSettingOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: notFoundContentItemSettingOrchestrationException);
            }

            // THE EVENT PATH'S TWO, and both are validation failures rather than anything more
            // exotic. An unverifiable envelope and a content type the item contradicts are each a
            // request this service will not carry out, and the substrate treats any throw the same
            // way — the delivery is recorded as Error and retried — so what the category buys is a
            // consistent answer if this handler is ever called directly.
            catch (InvalidContentItemSettingEventOrchestrationException
                invalidContentItemSettingEventOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: invalidContentItemSettingEventOrchestrationException);
            }
            catch (ContentTypeMismatchContentItemSettingOrchestrationException
                contentTypeMismatchContentItemSettingOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: contentTypeMismatchContentItemSettingOrchestrationException);
            }

            // THE FOUNDATION'S ANSWERS, CARRIED THROUGH IN KIND. A validation failure stays a
            // validation failure and a dependency failure stays a dependency failure, so the
            // exposer maps this service's exceptions to the same status codes it mapped the
            // foundation's to — the layer changed, the meaning did not.
            catch (ContentItemSettingValidationException contentItemSettingValidationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: contentItemSettingValidationException);
            }
            catch (ContentItemSettingDependencyValidationException contentItemSettingDependencyValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(
                    exception: contentItemSettingDependencyValidationException);
            }
            catch (ContentItemSettingDependencyException contentItemSettingDependencyException)
            {
                throw await CreateAndLogDependencyExceptionAsync(
                    exception: contentItemSettingDependencyException);
            }
            // A SERVICE failure stays a service failure. Routing this to the dependency wrapper
            // moved every endpoint from 500 to 424 — the opposite of the invariant stated above.
            catch (ContentItemSettingServiceException contentItemSettingServiceException)
            {
                throw await CreateAndLogServiceExceptionAsync(
                    exception: contentItemSettingServiceException);
            }

            // ANY OTHER DOWNSTREAM FOUNDATION EXCEPTION — in practice the ContentItem service's
            // dependency and service failures while the content type is being derived. Its
            // validation failures are already turned into a not-found at the resolution site, so
            // what reaches here is a store that could not answer. Categorised as a dependency
            // issue and never re-surfaced as its own entity type, which is the clause
            // AssociationOrchestrationService carries for exactly this case.
            //
            // Without it a SQL failure on the item read fell into the general handler below and
            // answered 500 rather than 424 — and the comment above promising a dependency
            // validation exception "must keep its category" was not true of anything.
            catch (Xeption downstreamException)
            {
                throw await CreateAndLogDependencyExceptionAsync(
                    exception: downstreamException);
            }
            catch (Exception exception)
            {
                var failedContentItemSettingOrchestrationServiceException =
                    new FailedContentItemSettingOrchestrationServiceException(
                        message: "Failed content item setting orchestration service error occurred, "
                            + "contact support.",
                        innerException: exception,
                        data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(
                    exception: failedContentItemSettingOrchestrationServiceException);
            }
        }

        // The queryable read takes the same mapping. Written as its own delegate rather than made
        // generic because the two return shapes are the whole difference, and a generic here
        // would hide which one a call site is on.
        private async ValueTask<IQueryable<ContentItemSetting>> TryCatchQueryable(
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

                var timeoutContentItemSettingOrchestrationException =
                    new TimeoutContentItemSettingOrchestrationException(
                        message: "Failed content item setting orchestration timeout error occurred, "
                            + "contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(
                    exception: timeoutContentItemSettingOrchestrationException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            // The collection read cannot fail validation — it takes no arguments to validate — so
            // the two validation clauses that once stood here were unreachable and are gone. What
            // it can do is fail to reach the store.
            catch (ContentItemSettingDependencyException contentItemSettingDependencyException)
            {
                throw await CreateAndLogDependencyExceptionAsync(
                    exception: contentItemSettingDependencyException);
            }
            catch (ContentItemSettingServiceException contentItemSettingServiceException)
            {
                throw await CreateAndLogServiceExceptionAsync(
                    exception: contentItemSettingServiceException);
            }
            // NO catch (Xeption) HERE, unlike the write path above. This path calls one foundation,
            // and that foundation's queryable TryCatch emits only the two types caught above — a
            // dependency exception for a timeout or a SqlException, a service exception for
            // everything else — while a cancellation is rethrown raw and is not a Xeption at all.
            // The clause would be unreachable by the same argument that retired the two validation
            // clauses above it, so it is not written.
            catch (Exception exception)
            {
                var failedContentItemSettingOrchestrationServiceException =
                    new FailedContentItemSettingOrchestrationServiceException(
                        message: "Failed content item setting orchestration service error occurred, "
                            + "contact support.",
                        innerException: exception,
                        data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(
                    exception: failedContentItemSettingOrchestrationServiceException);
            }
        }

        // THE ITEM'S OWN NOT-FOUND, recognised by shape rather than by type, the same way
        // AssociationOrchestrationService recognises an unresolvable endpoint: a plain validation
        // exception from the endpoint's service means the row is missing or not visible, while a
        // DEPENDENCY validation exception means the store refused for its own reasons and must
        // keep its category.
        private static bool IsContentItemNotFound(Exception exception)
        {
            string exceptionName = exception.GetType().Name;

            return exceptionName.EndsWith("ValidationException", StringComparison.Ordinal)
                && exceptionName.EndsWith("DependencyValidationException", StringComparison.Ordinal) is false;
        }

        private async ValueTask<ContentItemSettingOrchestrationValidationException>
            CreateAndLogValidationExceptionAsync(Xeption exception)
        {
            var contentItemSettingOrchestrationValidationException =
                new ContentItemSettingOrchestrationValidationException(
                    message: "Content item setting orchestration validation error occurred, "
                        + "fix the errors and try again.",
                    innerException: (exception.InnerException as Xeption) ?? exception);

            await this.loggingBroker.LogErrorAsync(
                exception: contentItemSettingOrchestrationValidationException);

            return contentItemSettingOrchestrationValidationException;
        }

        private async ValueTask<ContentItemSettingOrchestrationDependencyValidationException>
            CreateAndLogDependencyValidationExceptionAsync(Xeption exception)
        {
            var contentItemSettingOrchestrationDependencyValidationException =
                new ContentItemSettingOrchestrationDependencyValidationException(
                    message: "Content item setting orchestration dependency validation error occurred, "
                        + "fix the errors and try again.",
                    innerException: (exception.InnerException as Xeption) ?? exception);

            await this.loggingBroker.LogErrorAsync(
                exception: contentItemSettingOrchestrationDependencyValidationException);

            return contentItemSettingOrchestrationDependencyValidationException;
        }

        private async ValueTask<ContentItemSettingOrchestrationDependencyException>
            CreateAndLogDependencyExceptionAsync(Xeption exception)
        {
            var contentItemSettingOrchestrationDependencyException =
                new ContentItemSettingOrchestrationDependencyException(
                    message: "Content item setting orchestration dependency error occurred, contact support.",
                    innerException: (exception.InnerException as Xeption) ?? exception);

            await this.loggingBroker.LogErrorAsync(
                exception: contentItemSettingOrchestrationDependencyException);

            return contentItemSettingOrchestrationDependencyException;
        }

        // A named twin of the dependency wrapper (same category, same LogError) so a timeout reads
        // as a timeout at the call site and can diverge later.
        private async ValueTask<ContentItemSettingOrchestrationDependencyException>
            CreateAndLogTimeoutDependencyExceptionAsync(Xeption exception)
        {
            var contentItemSettingOrchestrationDependencyException =
                new ContentItemSettingOrchestrationDependencyException(
                    message: "Content item setting orchestration dependency error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(
                exception: contentItemSettingOrchestrationDependencyException);

            return contentItemSettingOrchestrationDependencyException;
        }

        private async ValueTask<ContentItemSettingOrchestrationServiceException>
            CreateAndLogServiceExceptionAsync(Xeption exception)
        {
            var contentItemSettingOrchestrationServiceException =
                new ContentItemSettingOrchestrationServiceException(
                    message: "Content item setting orchestration service error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(
                exception: contentItemSettingOrchestrationServiceException);

            return contentItemSettingOrchestrationServiceException;
        }
    }
}
