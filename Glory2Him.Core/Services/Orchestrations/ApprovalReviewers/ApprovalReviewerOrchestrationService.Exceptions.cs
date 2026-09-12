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
using Glory2Him.Core.Models.Foundations.ApprovalComments.Exceptions;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests.Exceptions;
using Glory2Him.Core.Models.Foundations.Approvals.Exceptions;
using Glory2Him.Core.Models.Foundations.IdentityUsers.Exceptions;
using Glory2Him.Core.Models.Orchestrations.ApprovalReviewers.Exceptions;
using Xeptions;

namespace Glory2Him.Core.Services.Orchestrations.ApprovalReviewers
{
    internal partial class ApprovalReviewerOrchestrationService
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

                var timeoutApprovalReviewerOrchestrationException =
                    new TimeoutApprovalReviewerOrchestrationException(
                        message: "Failed approval reviewer orchestration timeout error occurred, " +
                            "contact support.",
                        innerException: timeoutException,
                        data: timeoutException.Data);

                throw await CreateAndLogTimeoutDependencyExceptionAsync(
                    exception: timeoutApprovalReviewerOrchestrationException);
            }
            catch (OperationCanceledException)
            {
                throw;
            }

            // The orchestration's own validation failures. THREE rather than the approval round's
            // five: nothing on this contract takes a model to be null-checked and nothing branches
            // on an unsupported kind, so Null- and NotSupported- arms would be catch blocks no
            // operation can reach.
            catch (UnauthorizedApprovalReviewerOrchestrationException
                unauthorizedApprovalReviewerOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: unauthorizedApprovalReviewerOrchestrationException);
            }
            catch (NotFoundApprovalReviewerOrchestrationException
                notFoundApprovalReviewerOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: notFoundApprovalReviewerOrchestrationException);
            }
            catch (InvalidApprovalReviewerOrchestrationException
                invalidApprovalReviewerOrchestrationException)
            {
                throw await CreateAndLogValidationExceptionAsync(
                    exception: invalidApprovalReviewerOrchestrationException);
            }

            // ARM 1 — the ApprovalReviewRequest foundation (design §7.9, §16.7.4). Without these
            // the whole family falls to the catch-all below and every routine refusal — an
            // over-long deletion reason, a ReadOnly caller, a uniqueness collision — is reported
            // as a 424 infrastructure fault, and the exposer's Conflict branch is dead code that
            // can never be reached.
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

            // ARM 2 — the ApprovalComment foundation, reached through the display-name resolver's
            // round-keyed comment read (§12.5.4 business rule 3). The two failure-shaped blocks
            // are named explicitly rather than left to reach the catch-all by coincidence: a
            // family not named here is a raw foundation exception escaping the layer the moment a
            // caller changes shape, even where today's wrapping happens to match.
            catch (ApprovalCommentValidationException approvalCommentValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(
                    exception: approvalCommentValidationException);
            }
            catch (ApprovalCommentDependencyValidationException
                approvalCommentDependencyValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(
                    exception: approvalCommentDependencyValidationException);
            }
            catch (ApprovalCommentDependencyException approvalCommentDependencyException)
            {
                throw await CreateAndLogDependencyExceptionAsync(
                    exception: approvalCommentDependencyException);
            }
            catch (ApprovalCommentServiceException approvalCommentServiceException)
            {
                throw await CreateAndLogDependencyExceptionAsync(
                    exception: approvalCommentServiceException);
            }

            // ARM 3 — the IdentityUser foundation (§12.7.1), a THREE-block arm rather than four.
            // IIdentityUserService exposes two read-only operations, and a read-only contract has
            // no uniqueness collision, no foreign-key violation and no constraint conflict, so
            // there is no IdentityUserDependencyValidationException anywhere in the solution and
            // none should be added. The absent type is correct rather than an omission; nobody is
            // to mint a fourth for symmetry with the arms beside it.
            catch (IdentityUserValidationException identityUserValidationException)
            {
                throw await CreateAndLogDependencyValidationExceptionAsync(
                    exception: identityUserValidationException);
            }
            catch (IdentityUserDependencyException identityUserDependencyException)
            {
                throw await CreateAndLogDependencyExceptionAsync(
                    exception: identityUserDependencyException);
            }
            catch (IdentityUserServiceException identityUserServiceException)
            {
                throw await CreateAndLogDependencyExceptionAsync(
                    exception: identityUserServiceException);
            }

            // ARM 4 — the Approval foundation, and the arm the read-triggered repair pays for.
            // This service reads that foundation on every operation and WRITES to it on one path
            // — the repair opens a missing round — so all four families are reachable here, and
            // the uniqueness collision two concurrent repairs produce on
            // UX_Approvals_EntityType_EntityId is the caller's to retry rather than an
            // infrastructure fault: the exposer answers it 400 and retrying finds the round the
            // winner opened.
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
                var failedApprovalReviewerOrchestrationServiceException =
                    new FailedApprovalReviewerOrchestrationServiceException(
                        message: "Failed approval reviewer orchestration service error occurred, " +
                            "please contact support.",
                        innerException: exception,
                        data: exception.Data);

                throw await CreateAndLogServiceExceptionAsync(
                    exception: failedApprovalReviewerOrchestrationServiceException);
            }
        }

        private async ValueTask<ApprovalReviewerOrchestrationValidationException>
            CreateAndLogValidationExceptionAsync(Xeption exception)
        {
            var approvalReviewerOrchestrationValidationException =
                new ApprovalReviewerOrchestrationValidationException(
                    message: "Approval reviewer orchestration validation error occurred, " +
                        "fix the errors and try again.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(
                exception: approvalReviewerOrchestrationValidationException);

            return approvalReviewerOrchestrationValidationException;
        }

        private async ValueTask<ApprovalReviewerOrchestrationDependencyValidationException>
            CreateAndLogDependencyValidationExceptionAsync(Xeption exception)
        {
            var approvalReviewerOrchestrationDependencyValidationException =
                new ApprovalReviewerOrchestrationDependencyValidationException(
                    message: "Approval reviewer orchestration dependency validation error " +
                        "occurred, fix the errors and try again.",
                    innerException: (exception.InnerException as Xeption) ?? exception);

            await this.loggingBroker.LogErrorAsync(
                exception: approvalReviewerOrchestrationDependencyValidationException);

            return approvalReviewerOrchestrationDependencyValidationException;
        }

        private async ValueTask<ApprovalReviewerOrchestrationDependencyException>
            CreateAndLogDependencyExceptionAsync(Xeption exception)
        {
            var approvalReviewerOrchestrationDependencyException =
                new ApprovalReviewerOrchestrationDependencyException(
                    message: "Approval reviewer orchestration dependency error occurred, " +
                        "contact support.",
                    innerException: (exception.InnerException as Xeption) ?? exception);

            await this.loggingBroker.LogErrorAsync(
                exception: approvalReviewerOrchestrationDependencyException);

            return approvalReviewerOrchestrationDependencyException;
        }

        // A named twin of the dependency wrapper (same category, same LogError) so a timeout
        // reads as a timeout at the call site and can diverge later.
        private async ValueTask<ApprovalReviewerOrchestrationDependencyException>
            CreateAndLogTimeoutDependencyExceptionAsync(Xeption exception)
        {
            var approvalReviewerOrchestrationDependencyException =
                new ApprovalReviewerOrchestrationDependencyException(
                    message: "Approval reviewer orchestration dependency error occurred, " +
                        "contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(
                exception: approvalReviewerOrchestrationDependencyException);

            return approvalReviewerOrchestrationDependencyException;
        }

        private async ValueTask<ApprovalReviewerOrchestrationServiceException>
            CreateAndLogServiceExceptionAsync(Xeption exception)
        {
            var approvalReviewerOrchestrationServiceException =
                new ApprovalReviewerOrchestrationServiceException(
                    message: "Approval reviewer orchestration service error occurred, " +
                        "contact support.",
                    innerException: exception);

            await this.loggingBroker.LogErrorAsync(
                exception: approvalReviewerOrchestrationServiceException);

            return approvalReviewerOrchestrationServiceException;
        }
    }
}
