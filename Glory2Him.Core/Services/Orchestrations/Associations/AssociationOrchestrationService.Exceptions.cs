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
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.Associations.Exceptions;
using Glory2Him.Core.Models.Orchestrations.Associations;
using Glory2Him.Core.Models.Orchestrations.Associations.Exceptions;
using Xeptions;

namespace Glory2Him.Core.Services.Orchestrations.Associations
{
    internal partial class AssociationOrchestrationService
    {
        private delegate ValueTask<AssociationSuggestionResult> ReturningAssociationSuggestionResultFunction();

        private async ValueTask<AssociationSuggestionResult> TryCatch(
            ReturningAssociationSuggestionResultFunction returningAssociationSuggestionResultFunction)
        {
            try
            {
                return await returningAssociationSuggestionResultFunction();
            }
            catch (OperationCanceledException operationCanceledException)
                when (operationCanceledException.CancellationToken.IsCancellationRequested is false)
            {
                var timeoutException =
                    new TimeoutException("The dependency operation timed out.");

                var timeoutAssociationOrchestrationException =
                    new TimeoutAssociationOrchestrationException(
                        message: "Failed content item association orchestration timeout error occurred, " +
                            "contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(
                    exception: timeoutAssociationOrchestrationException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }

            // the orchestration's own validation failures
            catch (UnauthorizedAssociationOrchestrationException unauthorizedAssociationOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: unauthorizedAssociationOrchestrationException);
            }
            catch (NullAssociationOrchestrationException nullAssociationOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: nullAssociationOrchestrationException);
            }
            catch (NotFoundAssociationOrchestrationException notFoundAssociationOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: notFoundAssociationOrchestrationException);
            }
            catch (InvalidAssociationOrchestrationException invalidAssociationOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: invalidAssociationOrchestrationException);
            }

            // the Association foundation's exceptions
            catch (AssociationValidationException associationValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(
                    exception: associationValidationException);
            }
            catch (AssociationDependencyValidationException associationDependencyValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(
                    exception: associationDependencyValidationException);
            }
            catch (AssociationDependencyException associationDependencyException)
            {
                throw await CreateAndLogDependencyExceptionAsync(
                    exception: associationDependencyException);
            }
            catch (AssociationServiceException associationServiceException)
            {
                throw await CreateAndLogDependencyExceptionAsync(
                    exception: associationServiceException);
            }

            // Any OTHER downstream foundation exception — an endpoint service's dependency or
            // service failure (its validation failures are already turned into a not-found at the
            // resolution site). Categorized as a dependency issue, and NEVER re-surfaced as its
            // own entity type (§1.1.3 — no foundation exception leaks to a higher layer).
            catch (Xeption downstreamException)
            {
                throw await CreateAndLogDependencyExceptionAsync(
                    exception: downstreamException);
            }
            catch (Exception exception)
            {
                var failedAssociationOrchestrationServiceException =
                    new FailedAssociationOrchestrationServiceException(
                        message: "Failed content item association orchestration service error occurred, " +
                            "please contact support.",
                        innerException: exception,
                        data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(
                    exception: failedAssociationOrchestrationServiceException);
            }
        }

        private async ValueTask<AssociationOrchestrationValidationException>
            CreateAndLogValidationExceptionAsync(Xeption exception)
        {
            var associationOrchestrationValidationException =
                new AssociationOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(
                exception: associationOrchestrationValidationException);

            return associationOrchestrationValidationException;
        }

        private async ValueTask<AssociationOrchestrationDependencyValidationException>
            CreateAndLogDependencyValidationExceptionAsync(Xeption exception)
        {
            var associationOrchestrationDependencyValidationException =
                new AssociationOrchestrationDependencyValidationException(
                    message: "Content item association orchestration dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: (exception.InnerException as Xeption) ?? exception);

            await this.loggingBroker.LogErrorAsync(
                exception: associationOrchestrationDependencyValidationException);

            return associationOrchestrationDependencyValidationException;
        }

        private async ValueTask<AssociationOrchestrationDependencyException>
            CreateAndLogDependencyExceptionAsync(Xeption exception)
        {
            var associationOrchestrationDependencyException =
                new AssociationOrchestrationDependencyException(
                    message: "Content item association orchestration dependency error occurred, contact support.",
                    innerException: (exception.InnerException as Xeption) ?? exception);

            await this.loggingBroker.LogErrorAsync(
                exception: associationOrchestrationDependencyException);

            return associationOrchestrationDependencyException;
        }

        // A named twin of the dependency wrapper (same category, same LogError) so a timeout
        // reads as a timeout at the call site and can diverge later.
        private async ValueTask<AssociationOrchestrationDependencyException>
            CreateAndLogTimeoutDependencyExceptionAsync(Xeption exception)
        {
            var associationOrchestrationDependencyException =
                new AssociationOrchestrationDependencyException(
                    message: "Content item association orchestration dependency error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(
                exception: associationOrchestrationDependencyException);

            return associationOrchestrationDependencyException;
        }

        private async ValueTask<AssociationOrchestrationServiceException>
            CreateAndLogServiceExceptionAsync(Xeption exception)
        {
            var associationOrchestrationServiceException =
                new AssociationOrchestrationServiceException(
                    message: "Content item association orchestration service error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(
                exception: associationOrchestrationServiceException);

            return associationOrchestrationServiceException;
        }
    }
}
