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
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments.Exceptions;
using Glory2Him.Core.Models.Foundations.Approvals.Exceptions;
using Glory2Him.Core.Models.Orchestrations.AIReviewers.Exceptions;
using Xeptions;

namespace Glory2Him.Core.Services.Orchestrations.AIReviewers
{
    internal partial class AIReviewerOrchestrationService
    {
        // Generic in the return type so that every operation on this service shares ONE catch
        // chain. A second chain for a second return type is the kind of duplication that drifts:
        // a dependency family added to one and forgotten on the other surfaces as a raw
        // foundation exception escaping the layer (§12.2), and nothing fails until it does.
        private delegate ValueTask<T> ReturningValueFunction<T>();

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

                var timeoutAIReviewerOrchestrationException =
                    new TimeoutAIReviewerOrchestrationException(
                        message: "Failed AI reviewer orchestration timeout error occurred, " +
                            "contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(
                    exception: timeoutAIReviewerOrchestrationException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }

            // the orchestration's own validation failures. There are three rather than the
            // approval round's five: nothing on this contract takes a model to be null-checked
            // and nothing branches on an unsupported kind, so Null- and NotSupported- arms would
            // be catch blocks no operation can reach.
            catch (UnauthorizedAIReviewerOrchestrationException
                unauthorizedAIReviewerOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: unauthorizedAIReviewerOrchestrationException);
            }
            catch (NotFoundAIReviewerOrchestrationException notFoundAIReviewerOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: notFoundAIReviewerOrchestrationException);
            }
            catch (InvalidAIReviewerOrchestrationException invalidAIReviewerOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: invalidAIReviewerOrchestrationException);
            }

            // The Approval foundation's exceptions. This service reads that foundation on every
            // operation and WRITES to it on one path — the read-triggered repair opens a missing
            // round — so all four families are reachable here, and the uniqueness collision two
            // concurrent repairs produce is the caller's to retry rather than an infrastructure
            // fault.
            catch (ApprovalValidationException approvalValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(
                    exception: approvalValidationException);
            }
            catch (ApprovalDependencyValidationException approvalDependencyValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(
                    exception: approvalDependencyValidationException);
            }
            catch (ApprovalDependencyException approvalDependencyException)
            {
                throw await CreateAndLogDependencyExceptionAsync(
                    exception: approvalDependencyException);
            }
            catch (ApprovalServiceException approvalServiceException)
            {
                throw await CreateAndLogDependencyExceptionAsync(
                    exception: approvalServiceException);
            }

            // The AIReviewerAssignment foundation's exceptions (design §8.6.2). Without these the
            // whole family falls to the catch-all below and every routine refusal — a ReadOnly
            // caller, an assignment born already completed, a comments-present flag on a review
            // that never ran, and the uniqueness collision that outlives RequestAIReviewerAsync's
            // re-read — is reported as a 424 infrastructure fault, which says the server is
            // broken about a request the server understood perfectly and declined. The Conflict
            // branch on the exposer is also dead code without them: it matches on
            // AlreadyExistsAIReviewerAssignmentException sitting INSIDE a dependency validation
            // wrapper, which only these arms produce.
            catch (AIReviewerAssignmentValidationException aiReviewerAssignmentValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(
                    exception: aiReviewerAssignmentValidationException);
            }
            catch (AIReviewerAssignmentDependencyValidationException
                aiReviewerAssignmentDependencyValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(
                    exception: aiReviewerAssignmentDependencyValidationException);
            }
            catch (AIReviewerAssignmentDependencyException
                aiReviewerAssignmentDependencyException)
            {
                throw await CreateAndLogDependencyExceptionAsync(
                    exception: aiReviewerAssignmentDependencyException);
            }
            catch (AIReviewerAssignmentServiceException aiReviewerAssignmentServiceException)
            {
                throw await CreateAndLogDependencyExceptionAsync(
                    exception: aiReviewerAssignmentServiceException);
            }

            // Any OTHER downstream exception. Categorized as a dependency issue, and NEVER
            // re-surfaced as its own entity type (§1.1.3 — no foundation exception leaks to a
            // higher layer).
            catch (Xeption downstreamException)
            {
                throw await CreateAndLogDependencyExceptionAsync(
                    exception: downstreamException);
            }
            catch (Exception exception)
            {
                var failedAIReviewerOrchestrationServiceException =
                    new FailedAIReviewerOrchestrationServiceException(
                        message: "Failed AI reviewer orchestration service error occurred, " +
                            "please contact support.",
                        innerException: exception,
                        data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(
                    exception: failedAIReviewerOrchestrationServiceException);
            }
        }

        private async ValueTask<AIReviewerOrchestrationValidationException>
            CreateAndLogValidationExceptionAsync(Xeption exception)
        {
            var aiReviewerOrchestrationValidationException =
                new AIReviewerOrchestrationValidationException(
                    message: "AI reviewer orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(
                exception: aiReviewerOrchestrationValidationException);

            return aiReviewerOrchestrationValidationException;
        }

        private async ValueTask<AIReviewerOrchestrationDependencyValidationException>
            CreateAndLogDependencyValidationExceptionAsync(Xeption exception)
        {
            var aiReviewerOrchestrationDependencyValidationException =
                new AIReviewerOrchestrationDependencyValidationException(
                    message: "AI reviewer orchestration dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: (exception.InnerException as Xeption) ?? exception);

            await this.loggingBroker.LogErrorAsync(
                exception: aiReviewerOrchestrationDependencyValidationException);

            return aiReviewerOrchestrationDependencyValidationException;
        }

        private async ValueTask<AIReviewerOrchestrationDependencyException>
            CreateAndLogDependencyExceptionAsync(Xeption exception)
        {
            var aiReviewerOrchestrationDependencyException =
                new AIReviewerOrchestrationDependencyException(
                    message: "AI reviewer orchestration dependency error occurred, contact support.",
                    innerException: (exception.InnerException as Xeption) ?? exception);

            await this.loggingBroker.LogErrorAsync(
                exception: aiReviewerOrchestrationDependencyException);

            return aiReviewerOrchestrationDependencyException;
        }

        // A named twin of the dependency wrapper (same category, same LogError) so a timeout
        // reads as a timeout at the call site and can diverge later.
        private async ValueTask<AIReviewerOrchestrationDependencyException>
            CreateAndLogTimeoutDependencyExceptionAsync(Xeption exception)
        {
            var aiReviewerOrchestrationDependencyException =
                new AIReviewerOrchestrationDependencyException(
                    message: "AI reviewer orchestration dependency error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(
                exception: aiReviewerOrchestrationDependencyException);

            return aiReviewerOrchestrationDependencyException;
        }

        private async ValueTask<AIReviewerOrchestrationServiceException>
            CreateAndLogServiceExceptionAsync(Xeption exception)
        {
            var aiReviewerOrchestrationServiceException =
                new AIReviewerOrchestrationServiceException(
                    message: "AI reviewer orchestration service error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(
                exception: aiReviewerOrchestrationServiceException);

            return aiReviewerOrchestrationServiceException;
        }
    }
}
