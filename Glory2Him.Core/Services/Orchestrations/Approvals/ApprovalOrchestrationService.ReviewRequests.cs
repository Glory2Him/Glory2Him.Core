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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using G2H.Security.Client.Models.Foundations.Access;
using G2H.Security.Client.Models.Securities;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests.Exceptions;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.IdentityUsers;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals.Exceptions;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Services.Orchestrations.Approvals
{
    /// <summary>
    /// The invitation half of the approval workflow (design 7.9, 16.7.4) - asking somebody to
    /// review, withdrawing the ask, and listing who could be asked.
    ///
    /// <para>These are the operations that need BOTH stores. Whether a round is open, who owns
    /// the entity and who has already reviewed all come from Core through IAccessBroker; who
    /// holds a review-tier role comes from the identity store through IIdentityUserService
    /// (12.7.1). Neither store can answer alone, which is why the composition lives here rather
    /// than in a foundation.</para>
    ///
    /// <para>The tier NAMES are composed here too, from the approval's role subjects, so 18.6's
    /// convention keeps a single home; the identity service is handed finished names and only
    /// reports membership.</para>
    /// </summary>
    internal partial class ApprovalOrchestrationService
    {
        public ValueTask<IReadOnlyList<ReviewerCandidate>> RetrieveReviewerCandidatesAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken = default) =>
            TryCatch<IReadOnlyList<ReviewerCandidate>>(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRetrieveReviewerCandidates(entityType, entityId);

                ApprovalReviewerScope scope = await ResolveReviewerScopeAsync(
                    entityType: entityType,
                    entityId: entityId,
                    onSecurityContext: ValidateUserMayRequestApprovalReviews,
                    cancellationToken: cancellationToken);

                IReadOnlyList<IdentityUser> tierMembers =
                    await this.identityUserService.RetrieveIdentityUsersInRolesAsync(
                        roleNames: ComposeReviewTierRoleNames(scope.RoleSubjects),
                        cancellationToken: cancellationToken);

                // The three subtractions of 16.7.4, in one pass. Each is a person who cannot
                // usefully be invited: the owner would be reviewing their own work (rule 3,
                // HR-1), an active reviewer has already answered, and an invited person is
                // already on the panel.
                var excludedUserIds = new HashSet<string>(StringComparer.Ordinal);

                if (string.IsNullOrWhiteSpace(scope.EntityCreatedBy) is false)
                {
                    excludedUserIds.Add(scope.EntityCreatedBy);
                }

                foreach (string reviewerUserId in scope.ActiveReviewerUserIds)
                {
                    excludedUserIds.Add(reviewerUserId);
                }

                foreach (ActiveReviewRequest activeRequest in scope.ActiveRequests)
                {
                    excludedUserIds.Add(activeRequest.RequestedUserId);
                }

                return tierMembers
                    .Where(member =>
                        excludedUserIds.Contains(member.Id.ToString()) is false)
                    .Select(member => new ReviewerCandidate
                    {
                        UserId = member.Id.ToString(),
                        DisplayName = ComposeDisplayName(member),
                    })
                    .OrderBy(candidate => candidate.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            });

        public ValueTask<ApprovalReviewRequest> RequestApprovalReviewAsync(
            EntityType entityType,
            Guid entityId,
            string requestedUserId,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRequestApprovalReview(entityType, entityId, requestedUserId);

                ApprovalReviewerScope scope = await ResolveReviewerScopeAsync(
                    entityType: entityType,
                    entityId: entityId,
                    onSecurityContext: ValidateUserMayRequestApprovalReviews,
                    cancellationToken: cancellationToken);

                // Rule 7 - the round has to be open. An invitation to a closed round would sit
                // in the panel forever with nothing that could answer it.
                ValidateApprovalRoundIsOpenForRequests(scope, entityType, entityId);

                // Rule 4, and it comes BEFORE the eligibility work on purpose: asking twice is a
                // harmless thing for a UI to do, so it returns the standing state rather than
                // spending an identity-store read to reach the same place, and rather than
                // colliding with UX_ApprovalReviewRequests_ApprovalId_RequestedUserId.
                ActiveReviewRequest standingRequest = scope.ActiveRequests
                    .FirstOrDefault(request =>
                        request.RequestedUserId == requestedUserId);

                if (standingRequest is not null)
                {
                    return await this.approvalReviewRequestService
                        .RetrieveApprovalReviewRequestByIdAsync(
                            standingRequest.Id,
                            cancellationToken);
                }

                // The other half of rule 4: somebody who has already answered needs no asking.
                // Reported as an ordinary refusal rather than silently, because unlike a repeat
                // invitation this is the caller misreading the panel.
                ValidateRequestedUserHasNotAlreadyReviewed(scope, requestedUserId);

                // Rule 3 - the invited person must be worth inviting. Both halves are checked
                // against STORED facts: the owner comes off the entity, and the tier membership
                // out of the identity store. Taking the caller's word for either is the
                // caller-supplied-identity mistake ApprovalReview.ReviewerId was deleted for.
                ValidateRequestedUserIsNotTheEntityOwner(scope, requestedUserId);

                IReadOnlyList<IdentityUser> tierMembers =
                    await this.identityUserService.RetrieveIdentityUsersInRolesAsync(
                        roleNames: ComposeReviewTierRoleNames(scope.RoleSubjects),
                        cancellationToken: cancellationToken);

                IdentityUser requestedUser = tierMembers
                    .FirstOrDefault(member => member.Id.ToString() == requestedUserId);

                ValidateRequestedUserIsInTheReviewTier(requestedUser, requestedUserId);

                var approvalReviewRequest = new ApprovalReviewRequest
                {
                    Id = Guid.NewGuid(),
                    ApprovalId = scope.ApprovalId,
                    RequestedUserId = requestedUserId,

                    // Denormalised at request time (7.9): the name is fixed here rather than
                    // re-read later across a boundary that may be unavailable.
                    RequestedUserDisplayName = ComposeDisplayName(requestedUser),
                };

                // The foundation re-decides the caller's tier and stamps the audit values; this
                // layer never assumes its own gate was the only one (14.6 rule 2).
                return await this.approvalReviewRequestService.AddApprovalReviewRequestAsync(
                    approvalReviewRequest,
                    cancellationToken);
            });

        public ValueTask<ApprovalReviewRequest> WithdrawApprovalReviewRequestAsync(
            Guid approvalReviewRequestId,
            string deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnWithdrawApprovalReviewRequest(approvalReviewRequestId);

                // The envelope captures the ambient caller so the tier gate has something to run
                // against. The foundation gates again on the same tier (7.9 rule 5) and owns the
                // pending check, so this is the coarse half of a deliberate pair.
                var withdrawRequest = new ApprovalReviewRequest { Id = approvalReviewRequestId };

                EventEnvelope<ApprovalReviewRequest> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: withdrawRequest);

                ValidateUserMayRequestApprovalReviews(envelope.SecurityContext);

                try
                {
                    return await this.approvalReviewRequestService
                        .RemoveApprovalReviewRequestByIdAsync(
                            approvalReviewRequestId: approvalReviewRequestId,
                            deletionReason: deletionReason,
                            cancellationToken: cancellationToken);
                }

                // Translated HERE because this operation is keyed on the request ROW rather than
                // on an entity. Every sibling resolves an approval first and raises its own
                // not-found at that site; this one deliberately does no lookup — the foundation
                // owns the pending check — so there is no earlier place for a missing row to
                // become a not-found. Without this the foundation's validation exception
                // categorises as a dependency-validation failure and the caller is told 400 for
                // an id that simply does not exist, leaving the exposer's NotFound branch dead.
                catch (ApprovalReviewRequestValidationException approvalReviewRequestValidationException)
                    when (approvalReviewRequestValidationException.InnerException
                        is NotFoundApprovalReviewRequestException)
                {
                    throw new NotFoundApprovalOrchestrationException(
                        message: "Approval review request not found with id: "
                            + $"{approvalReviewRequestId}.");
                }
            });

        /// <summary>
        /// Rule 6 - the invited person answered, so their invitation retires itself. Runs under
        /// the SYSTEM identity through the foundation's workflow seam, because the retirement is
        /// nobody's act: DeletedBy must say "answered", not name whoever happened to trigger it.
        ///
        /// <para>Silent when there is no invitation, which is the common case: most reviews are
        /// recorded by people who were never formally asked.</para>
        /// </summary>
        private async ValueTask RetireAnsweredReviewRequestAsync(
            Guid approvalId,
            string reviewerUserId,
            CancellationToken cancellationToken)
        {
            if (approvalId == Guid.Empty || string.IsNullOrWhiteSpace(reviewerUserId))
            {
                return;
            }

            ApprovalReviewerScope maybeScope =
                await this.accessBroker.RetrieveApprovalReviewerScopeByIdAsync(
                    approvalId,
                    cancellationToken);

            ActiveReviewRequest answeredRequest = maybeScope?.ActiveRequests
                .FirstOrDefault(request => request.RequestedUserId == reviewerUserId);

            if (answeredRequest is null)
            {
                return;
            }

            await this.approvalReviewRequestWorkflowService
                .RetireAnsweredApprovalReviewRequestAsync(
                    approvalReviewRequestId: answeredRequest.Id,
                    cancellationToken: cancellationToken);
        }

        // The shared opening move of both caller-facing invitation operations: capture the
        // ambient caller, gate them, resolve the approval behind the entity, and gather the
        // scope. Kept together because doing them in a different order would gate against
        // something other than the stored row.
        private async ValueTask<ApprovalReviewerScope> ResolveReviewerScopeAsync(
            EntityType entityType,
            Guid entityId,
            Action<SecurityContext> onSecurityContext,
            CancellationToken cancellationToken)
        {
            var scopeRequest = new Approval
            {
                EntityType = entityType,
                EntityId = entityId
            };

            EventEnvelope<Approval> envelope =
                await this.eventEnvelopeBroker.CreateAsync(content: scopeRequest);

            onSecurityContext(envelope.SecurityContext);

            // Unfiltered, for the same reason the verdict's lookup is: a soft-deleted approval
            // still occupies the key, and a filtered read would report "no approval" for one
            // that exists (9.7.2 rule 3).
            ApprovalEntityMatch maybeMatch =
                await this.approvalService.FindApprovalByEntityAsync(
                    entityType: entityType,
                    entityId: entityId,
                    cancellationToken: cancellationToken);

            ValidateStorageApprovalExists(maybeMatch, entityType, entityId);

            ApprovalReviewerScope maybeScope =
                await this.accessBroker.RetrieveApprovalReviewerScopeByIdAsync(
                    maybeMatch.Id,
                    cancellationToken);

            ValidateStorageReviewerScopeResolved(maybeScope, entityType, entityId);

            return maybeScope;
        }

        // 18.6, composed in ONE place. The global tier first, then the entity-scoped pair for
        // every subject the approval names, then the content-type pair where a subject carries
        // one - which is ContentItem and nothing else (18.6 rule 5).
        //
        // An association names BOTH endpoints, so a publisher trusted with either end is
        // invitable for the pairing; that is why this walks a list rather than one subject.
        private static IReadOnlyList<string> ComposeReviewTierRoleNames(
            IReadOnlyList<RoleSubject> roleSubjects)
        {
            var roleNames = new List<string>
            {
                Roles.Reviewer,
                Roles.Publisher,
                Roles.Admin,
            };

            foreach (RoleSubject roleSubject in roleSubjects ?? Array.Empty<RoleSubject>())
            {
                if (string.IsNullOrWhiteSpace(roleSubject?.EntityType))
                {
                    continue;
                }

                roleNames.Add(RoleNames.ReviewerFor(roleSubject.EntityType));
                roleNames.Add(RoleNames.PublisherFor(roleSubject.EntityType));

                if (string.IsNullOrWhiteSpace(roleSubject.ContentType) is false)
                {
                    roleNames.Add(
                        RoleNames.ReviewerFor(roleSubject.EntityType, roleSubject.ContentType));

                    roleNames.Add(
                        RoleNames.PublisherFor(roleSubject.EntityType, roleSubject.ContentType));
                }
            }

            return roleNames.Distinct(StringComparer.Ordinal).ToList();
        }

        // Presentation only, and never an identity. Preferred name first because it is what the
        // person chose to be called; then the full name; then the username, which every account
        // has, so this never returns blank.
        private static string ComposeDisplayName(IdentityUser identityUser)
        {
            if (identityUser is null)
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(identityUser.PreferredName) is false)
            {
                return identityUser.PreferredName.Trim();
            }

            string fullName = $"{identityUser.Name} {identityUser.Surname}".Trim();

            return string.IsNullOrWhiteSpace(fullName) is false
                ? fullName
                : identityUser.UserName;
        }
    }
}
