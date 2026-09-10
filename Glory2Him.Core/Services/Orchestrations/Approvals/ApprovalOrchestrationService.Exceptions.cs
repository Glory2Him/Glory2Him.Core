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
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests.Exceptions;
using Glory2Him.Core.Models.Foundations.Approvals.Exceptions;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Xeptions;

namespace Glory2Him.Core.Services.Orchestrations.Approvals
{
    internal partial class ApprovalOrchestrationService
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

                var timeoutApprovalOrchestrationException =
                    new TimeoutApprovalOrchestrationException(
                        message: "Failed content item association orchestration timeout error occurred, " +
                            "contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(
                    exception: timeoutApprovalOrchestrationException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }

            // the orchestration's own validation failures
            catch (UnauthorizedApprovalOrchestrationException unauthorizedApprovalOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: unauthorizedApprovalOrchestrationException);
            }
            catch (NullApprovalOrchestrationException nullApprovalOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: nullApprovalOrchestrationException);
            }
            catch (NotSupportedApprovalOrchestrationException
                notSupportedApprovalOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    notSupportedApprovalOrchestrationException);
            }

            catch (NotFoundApprovalOrchestrationException notFoundApprovalOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: notFoundApprovalOrchestrationException);
            }
            catch (InvalidApprovalOrchestrationException invalidApprovalOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: invalidApprovalOrchestrationException);
            }

            // the Approval foundation's exceptions
            catch (ApprovalValidationException associationValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(
                    exception: associationValidationException);
            }
            catch (ApprovalDependencyValidationException associationDependencyValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(
                    exception: associationDependencyValidationException);
            }
            catch (ApprovalDependencyException associationDependencyException)
            {
                throw await CreateAndLogDependencyExceptionAsync(
                    exception: associationDependencyException);
            }
            catch (ApprovalServiceException associationServiceException)
            {
                throw await CreateAndLogDependencyExceptionAsync(
                    exception: associationServiceException);
            }


            // The ApprovalReviewRequest foundation's exceptions (design 7.9, 16.7.4). Without
            // these the whole family falls to the catch-all below and every routine refusal - an
            // over-long deletion reason, a ReadOnly caller, a uniqueness collision - is reported
            // as a 424 infrastructure fault, and the Conflict branch on the exposer is dead code
            // that can never be reached.
            catch (ApprovalReviewRequestValidationException approvalReviewRequestValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(
                    exception: approvalReviewRequestValidationException);
            }
            catch (ApprovalReviewRequestDependencyValidationException
                approvalReviewRequestDependencyValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(
                    exception: approvalReviewRequestDependencyValidationException);
            }
            catch (ApprovalReviewRequestDependencyException
                approvalReviewRequestDependencyException)
            {
                throw await CreateAndLogDependencyExceptionAsync(
                    exception: approvalReviewRequestDependencyException);
            }
            catch (ApprovalReviewRequestServiceException approvalReviewRequestServiceException)
            {
                throw await CreateAndLogDependencyExceptionAsync(
                    exception: approvalReviewRequestServiceException);
            }

            // The AIReviewerAssignment foundation's exceptions (design 8.6.2), for exactly the
            // reason the ApprovalReviewRequest arm above exists: Berean's assignment has the same
            // four families, and left to the catch-all below every one of them reaches the client
            // as a 424, which says the server is broken about something it understood perfectly.
            //
            // ONE SOURCE REMAINS on this service, now that the caller-facing trio lives on
            // IAIReviewerOrchestrationService with its own catch chain: the workflow's own
            // return-to-pending, reached through IAIReviewerAssignmentWorkflowService from the
            // edit and reset flows. Its own helper logs and swallows what it raises, so nothing
            // routine arrives here today — the arms stay because that seam is a foundation call
            // like any other, and a family this chain does not name is a raw foundation exception
            // escaping the layer the moment the swallow is narrowed or another caller appears.
            //
            // The routine refusals this used to describe — a ReadOnly caller, an assignment born
            // already completed, the uniqueness collision that outlives a re-read — belong to
            // that other service now, and its chain names them.
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
                var failedApprovalOrchestrationServiceException =
                    new FailedApprovalOrchestrationServiceException(
                        message: "Failed content item association orchestration service error occurred, " +
                            "please contact support.",
                        innerException: exception,
                        data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(
                    exception: failedApprovalOrchestrationServiceException);
            }
        }

        private async ValueTask<ApprovalOrchestrationValidationException>
            CreateAndLogValidationExceptionAsync(Xeption exception)
        {
            var associationOrchestrationValidationException =
                new ApprovalOrchestrationValidationException(
                    message: "Content item association orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(
                exception: associationOrchestrationValidationException);

            return associationOrchestrationValidationException;
        }

        private async ValueTask<ApprovalOrchestrationDependencyValidationException>
            CreateAndLogDependencyValidationExceptionAsync(Xeption exception)
        {
            var associationOrchestrationDependencyValidationException =
                new ApprovalOrchestrationDependencyValidationException(
                    message: "Content item association orchestration dependency validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: (exception.InnerException as Xeption) ?? exception);

            await this.loggingBroker.LogErrorAsync(
                exception: associationOrchestrationDependencyValidationException);

            return associationOrchestrationDependencyValidationException;
        }

        private async ValueTask<ApprovalOrchestrationDependencyException>
            CreateAndLogDependencyExceptionAsync(Xeption exception)
        {
            var associationOrchestrationDependencyException =
                new ApprovalOrchestrationDependencyException(
                    message: "Content item association orchestration dependency error occurred, contact support.",
                    innerException: (exception.InnerException as Xeption) ?? exception);

            await this.loggingBroker.LogErrorAsync(
                exception: associationOrchestrationDependencyException);

            return associationOrchestrationDependencyException;
        }

        // A named twin of the dependency wrapper (same category, same LogError) so a timeout
        // reads as a timeout at the call site and can diverge later.
        private async ValueTask<ApprovalOrchestrationDependencyException>
            CreateAndLogTimeoutDependencyExceptionAsync(Xeption exception)
        {
            var associationOrchestrationDependencyException =
                new ApprovalOrchestrationDependencyException(
                    message: "Content item association orchestration dependency error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(
                exception: associationOrchestrationDependencyException);

            return associationOrchestrationDependencyException;
        }

        private async ValueTask<ApprovalOrchestrationServiceException>
            CreateAndLogServiceExceptionAsync(Xeption exception)
        {
            var associationOrchestrationServiceException =
                new ApprovalOrchestrationServiceException(
                    message: "Content item association orchestration service error occurred, contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(
                exception: associationOrchestrationServiceException);

            return associationOrchestrationServiceException;
        }
    }
}
