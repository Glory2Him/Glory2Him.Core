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

                // ONE subtraction, and only one: the entity's own author. They cannot review
                // their own work (rule 3, HR-1), so an invitation aimed at them is refused
                // outright - offering the row would be offering a click that always fails.
                //
                // People who have ALREADY ANSWERED, and people already invited, are deliberately
                // LEFT IN. This read answers "who is in scope for this round", not "who is not
                // yet dealt with". The picker renders an answered person ticked and inert and an
                // invited person under its own Requested heading, so a moderator searching for
                // somebody finds them and learns their state, instead of finding nothing and
                // wondering whether they typed the name wrong. Subtracting them here made that
                // impossible: the panel cannot show a person it was never sent.
                var excludedUserIds = new HashSet<string>(StringComparer.Ordinal);

                if (string.IsNullOrWhiteSpace(scope.EntityCreatedBy) is false)
                {
                    excludedUserIds.Add(scope.EntityCreatedBy);
                }

                // A SECOND subtraction, and it is the veto rather than a tidy-up. Somebody a
                // ReadOnly in this entity's scope covers cannot cast a vote at all (§18.6 rule
                // 2), so offering them is offering a click that always fails — the same
                // reasoning the owner is subtracted on. Unlike the two sets left in above, this
                // one is not a state a moderator can resolve by asking again.
                excludedUserIds.UnionWith(
                    await RetrieveBlockedUserIdsAsync(
                        roleSubjects: scope.RoleSubjects,
                        cancellationToken: cancellationToken));

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

                // Rule 4, BOTH halves, and they come before the eligibility work on purpose:
                // this operation is a presence check plus an add, so neither half spends an
                // identity-store read to reach a place it is already at.
                //
                // Neither errors. A repeat invitation is a UI asking twice - a double click, a
                // stale panel, another moderator half a second earlier - and returns the standing
                // row rather than colliding with
                // UX_ApprovalReviewRequests_ApprovalId_RequestedUserId.
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

                // And somebody who has ALREADY ANSWERED needs no asking. The goal of this
                // operation is that the person has been asked; an answer is more than that, so
                // it is already met. Nothing is created and nothing is returned - rule 6 retires
                // an invitation the moment its target answers, so on this path there is usually
                // no row left to hand back, and creating a fresh one would manufacture an
                // invitation that can never be withdrawn (rule 5 refuses to withdraw once a vote
                // is cast) and that nothing would ever retire, because the vote that would have
                // has already happened.
                //
                // The likely caller is a panel a few seconds stale, not a mistake worth an error.
                if (scope.ActiveReviewerUserIds.Contains(requestedUserId))
                {
                    return null;
                }

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

                // And rule 3's other half, which the tier check cannot answer: a grant and a
                // block can be held together, so somebody can be in the tier and still barred
                // from voting (§18.6 rule 2). Refused rather than dissolved like a duplicate —
                // an invitation nobody can answer is not an idempotent no-op, it is a round left
                // waiting on a vote that can never arrive.
                ValidateRequestedUserIsNotBlocked(
                    blockedUserIds: await RetrieveBlockedUserIdsAsync(
                        roleSubjects: scope.RoleSubjects,
                        cancellationToken: cancellationToken),
                    requestedUserId: requestedUserId);

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
                try
                {
                    return await this.approvalReviewRequestService.AddApprovalReviewRequestAsync(
                        approvalReviewRequest,
                        cancellationToken);
                }

                // The RACE, and rule 4 covers it too. The duplicate check above reads a scope
                // taken a moment earlier, so two callers inviting the same person can both find
                // nothing and both try to write. The index refuses the loser - correctly, one
                // active invitation per person is the invariant - but "somebody else asked them
                // half a second before you" is the same outcome as "you asked twice", and rule 4
                // does not care which caller won.
                //
                // Re-read rather than assume: the row is the winner's and this caller has never
                // seen it. If it has already gone - withdrawn between the collision and the
                // re-read, which is a narrow window but a real one - the collision is the honest
                // answer and goes back to the caller unchanged.
                catch (ApprovalReviewRequestDependencyValidationException collisionException)
                    when (collisionException.InnerException
                        is AlreadyExistsApprovalReviewRequestException)
                {
                    ApprovalReviewerScope reReadScope = await ResolveReviewerScopeAsync(
                        entityType: entityType,
                        entityId: entityId,
                        onSecurityContext: ValidateUserMayRequestApprovalReviews,
                        cancellationToken: cancellationToken);

                    ActiveReviewRequest winningRequest = reReadScope.ActiveRequests
                        .FirstOrDefault(request =>
                            request.RequestedUserId == requestedUserId);

                    if (winningRequest is null)
                    {
                        throw;
                    }

                    return await this.approvalReviewRequestService
                        .RetrieveApprovalReviewRequestByIdAsync(
                            winningRequest.Id,
                            cancellationToken);
                }
            });

        public ValueTask<IReadOnlyList<ApprovalReviewRequest>> RetrieveApprovalReviewRequestsAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken = default) =>
            TryCatch<IReadOnlyList<ApprovalReviewRequest>>(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateOnRetrieveApprovalReviewRequests(entityType, entityId);

                ApprovalReviewerScope scope = await ResolveReviewerScopeAsync(
                    entityType: entityType,
                    entityId: entityId,
                    onSecurityContext: ValidateUserMayRequestApprovalReviews,
                    cancellationToken: cancellationToken);

                // Through the CALLER-FACING read, not off the scope. The scope's ActiveRequests
                // are gathered unfiltered because invitability is a fact about storage (16.7.4),
                // and answering a person from that view would hand them rows their own posture
                // refuses. It also carries no display name, which is the one thing the surface
                // renders.
                //
                // The foundation's filter drops deleted rows, so what survives is exactly the
                // OUTSTANDING set: rule 5 soft-deletes a withdrawal and rule 6 retires an answer.
                // Pending-ness is therefore inherited rather than asserted here, and there is no
                // second definition of it to drift.
                IQueryable<ApprovalReviewRequest> allApprovalReviewRequests =
                    await this.approvalReviewRequestService
                        .RetrieveAllApprovalReviewRequestsAsync(cancellationToken);

                List<ApprovalReviewRequest> roundApprovalReviewRequests =
                    allApprovalReviewRequests
                        .Where(approvalReviewRequest =>
                            approvalReviewRequest.ApprovalId == scope.ApprovalId)
                        .ToList();

                // Ordered the way the candidates read is, and in memory for the same reason: a
                // culture-aware comparison is not a thing the database can be asked for, and the
                // two surfaces sit beside each other in the picker.
                return roundApprovalReviewRequests
                    .OrderBy(
                        approvalReviewRequest => approvalReviewRequest.RequestedUserDisplayName,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            });

        public ValueTask<ApprovalReviewRequest> WithdrawApprovalReviewRequestAsync(
            EntityType entityType,
            Guid entityId,
            string requestedUserId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default) =>
            TryCatch(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                ValidateOnWithdrawApprovalReviewRequest(
                    entityType,
                    entityId,
                    requestedUserId);

                ApprovalReviewerScope scope = await ResolveReviewerScopeAsync(
                    entityType: entityType,
                    entityId: entityId,
                    onSecurityContext: ValidateUserMayRequestApprovalReviews,
                    cancellationToken: cancellationToken);

                // The row is RESOLVED from the pair rather than supplied. That is what removes the
                // id round trip, and it also removes the not-found translation this operation used
                // to need: keyed on an id it could be handed one that names nothing, and the
                // foundation's validation failure had to be re-categorised so the exposer's
                // NotFound branch was not dead. Resolved from the round, a miss is simply an
                // invitation that is not outstanding.
                ActiveReviewRequest standingRequest = scope.ActiveRequests
                    .FirstOrDefault(request => request.RequestedUserId == requestedUserId);

                // Nothing outstanding is a no-op, not an error — withdrawing an invitation that
                // was already withdrawn, or that a rule 6 retirement has already taken, is a
                // stale panel rather than a mistake. Null here becomes the exposer's 204, which
                // is what the id-keyed route answered for the same case.
                if (standingRequest is null)
                {
                    return null;
                }

                // Rule 5 stops at the answer. Withdrawing says the invitation was a mistake, and
                // once it has been ANSWERED that is no longer something anyone gets to say — the
                // verdict stands, and the record of who was asked is part of how it came about.
                //
                // Reached only where the row is still LIVE, which after rule 6 means retirement
                // did not run or did not succeed. That is the one place a standing invitation and
                // a cast vote can coexist, and the only place this gate has anything to refuse.
                ValidateInvitationHasNotBeenAnswered(scope, requestedUserId);

                return await this.approvalReviewRequestService
                    .RemoveApprovalReviewRequestByIdAsync(
                        approvalReviewRequestId: standingRequest.Id,
                        deletionReason: deletionReason,
                        cancellationToken: cancellationToken);
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
                Roles.Reviewers,
                Roles.Publishers,
                Roles.Administrators,
            };

            foreach (RoleSubject roleSubject in roleSubjects ?? Array.Empty<RoleSubject>())
            {
                if (string.IsNullOrWhiteSpace(roleSubject?.EntityType))
                {
                    continue;
                }

                roleNames.Add(RoleNames.ReviewersFor(roleSubject.EntityType));
                roleNames.Add(RoleNames.PublishersFor(roleSubject.EntityType));

                if (string.IsNullOrWhiteSpace(roleSubject.ContentType) is false)
                {
                    roleNames.Add(
                        RoleNames.ReviewersFor(roleSubject.EntityType, roleSubject.ContentType));

                    roleNames.Add(
                        RoleNames.PublishersFor(roleSubject.EntityType, roleSubject.ContentType));
                }
            }

            return roleNames.Distinct(StringComparer.Ordinal).ToList();
        }

        // The veto's names, composed exactly as the tier's are and read the other way round: the
        // global block, then the entity-scoped one per subject, then the content-type-scoped one
        // where a subject carries a content type (18.6 rule 2). An association names both
        // endpoints, so a block on either end bars the holder from the pairing — the mirror of
        // one-endpoint-is-enough on the grant side.
        private static IReadOnlyList<string> ComposeBlockRoleNames(
            IReadOnlyList<RoleSubject> roleSubjects)
        {
            var roleNames = new List<string> { Roles.ReadOnly };

            foreach (RoleSubject roleSubject in roleSubjects ?? Array.Empty<RoleSubject>())
            {
                if (string.IsNullOrWhiteSpace(roleSubject?.EntityType))
                {
                    continue;
                }

                roleNames.Add(RoleNames.ReadOnlyFor(roleSubject.EntityType));

                if (string.IsNullOrWhiteSpace(roleSubject.ContentType) is false)
                {
                    roleNames.Add(
                        RoleNames.ReadOnlyFor(roleSubject.EntityType, roleSubject.ContentType));
                }
            }

            return roleNames.Distinct(StringComparer.Ordinal).ToList();
        }

        // Who those names belong to. Role membership lives in the Identity store and nowhere
        // else, so it is asked the same way the tier is — finished names in, members out.
        //
        // The list always carries the global ReadOnly, so it is never empty and never trips
        // IdentityUserService's fail-closed guard, which would otherwise answer "nobody is
        // blocked" for a composition bug and quietly restore every blocked person to the picker.
        private async ValueTask<HashSet<string>> RetrieveBlockedUserIdsAsync(
            IReadOnlyList<RoleSubject> roleSubjects,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<IdentityUser> blockedUsers =
                await this.identityUserService.RetrieveIdentityUsersInRolesAsync(
                    roleNames: ComposeBlockRoleNames(roleSubjects),
                    cancellationToken: cancellationToken);

            return new HashSet<string>(
                blockedUsers.Select(blockedUser => blockedUser.Id.ToString()),
                StringComparer.Ordinal);
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
