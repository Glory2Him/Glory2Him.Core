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
using G2H.Security.Client.Clients;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalReviewRequests;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Securities;

namespace Glory2Him.Core.Brokers.Securities
{
    internal class AccessBroker : IAccessBroker
    {
        private readonly IStorageBroker storageBroker;
        private readonly ISecurityClient securityClient;

        public AccessBroker(IStorageBroker storageBroker)
        {
            this.storageBroker = storageBroker;
            this.securityClient = new SecurityClient();
        }

        internal AccessBroker(IStorageBroker storageBroker, ISecurityClient securityClient)
        {
            this.storageBroker = storageBroker;
            this.securityClient = securityClient;
        }

        public async ValueTask<AccessVerdict> MayDecideApprovalAsync(
            ApprovalDecisionQuery approvalDecisionQuery,
            CancellationToken cancellationToken = default)
        {
            AccessActor actor = await BuildActorAsync(approvalDecisionQuery.SecurityContext);

            Approval? approval = await FindApprovalAsync(
                approvalDecisionQuery.EntityType,
                approvalDecisionQuery.EntityId,
                cancellationToken);

            // No approval row means no open round, and the decision function reads that as
            // Draft — which it refuses. Inventing Submitted here would let a decision be applied
            // to an entity that never entered review.
            ApprovalReviewSnapshot snapshot = approval is null
                ? ApprovalReviewSnapshot.Empty
                : await GatherAsync(approval, cancellationToken);

            IReadOnlyList<ApprovalPolicy> candidatePolicies = await GatherPoliciesAsync(
                approvalDecisionQuery.EntityType,
                cancellationToken);

            return await this.securityClient.Access.MayDecideApprovalAsync(
                new DecideApprovalRequest
                {
                    Actor = actor,
                    Decision = approvalDecisionQuery.Decision,
                    RoleSubjects = approvalDecisionQuery.RoleSubjects,
                    CandidatePolicies = candidatePolicies,
                    EntityType = approvalDecisionQuery.EntityType.ToString(),
                    ContentType = approvalDecisionQuery.ContentType?.ToString(),
                    EntityCreatedBy = approvalDecisionQuery.EntityCreatedBy,
                    ApprovalState = snapshot.State,
                    Reviews = snapshot.Reviews,
                    ApprovalComments = snapshot.ApprovalComments,
                    ConfidenceScore = approvalDecisionQuery.ConfidenceScore,
                    IsBypassRequested = approvalDecisionQuery.IsBypassRequested,
                    BypassReason = approvalDecisionQuery.BypassReason,
                });
        }

        // The comment gates share one gather: the parent approval, for its state and whether it
        // has been taken down. Neither fact is readable by ApprovalCommentService, which is
        // single-entity — which is the whole reason these live here rather than there.
        public async ValueTask<AccessVerdict> MayRecordApprovalCommentAsync(
            Guid approvalId,
            SecurityContext securityContext,
            CancellationToken cancellationToken = default)
        {
            AccessActor actor = await BuildActorAsync(securityContext);

            Approval maybeApproval = await this.storageBroker.SelectApprovalByIdAsync(
                approvalId,
                cancellationToken);

            if (maybeApproval is null)
            {
                return RefuseMissingApproval(approvalId);
            }

            return await this.securityClient.Access.MayRecordApprovalCommentAsync(
                new RecordApprovalCommentRequest
                {
                    Actor = actor,
                    ApprovalState = ToApprovalState(maybeApproval.ApprovalStatus),
                    IsParentApprovalDeleted = maybeApproval.IsDeleted,
                });
        }

        public async ValueTask<AccessVerdict> MayAmendApprovalCommentAsync(
            Guid approvalId,
            string commentCreatedBy,
            SecurityContext securityContext,
            CancellationToken cancellationToken = default)
        {
            AccessActor actor = await BuildActorAsync(securityContext);

            Approval maybeApproval = await this.storageBroker.SelectApprovalByIdAsync(
                approvalId,
                cancellationToken);

            if (maybeApproval is null)
            {
                return RefuseMissingApproval(approvalId);
            }

            return await this.securityClient.Access.MayAmendApprovalCommentAsync(
                new AmendApprovalCommentRequest
                {
                    Actor = actor,
                    CommentCreatedBy = commentCreatedBy,
                    ApprovalState = ToApprovalState(maybeApproval.ApprovalStatus),
                    IsParentApprovalDeleted = maybeApproval.IsDeleted,
                });
        }

        public async ValueTask<AccessVerdict> MayResolveApprovalCommentAsync(
            Guid approvalId,
            string commentCreatedBy,
            SecurityContext securityContext,
            CancellationToken cancellationToken = default)
        {
            AccessActor actor = await BuildActorAsync(securityContext);

            Approval maybeApproval = await this.storageBroker.SelectApprovalByIdAsync(
                approvalId,
                cancellationToken);

            if (maybeApproval is null)
            {
                return RefuseMissingApproval(approvalId);
            }

            return await this.securityClient.Access.MayResolveApprovalCommentAsync(
                new ResolveApprovalCommentRequest
                {
                    Actor = actor,
                    CommentCreatedBy = commentCreatedBy,
                    ApprovalState = ToApprovalState(maybeApproval.ApprovalStatus),
                    IsParentApprovalDeleted = maybeApproval.IsDeleted,
                });
        }

        // An action whose approval cannot be found is refused rather than waved through — the
        // comment gates and the dismissal gate share this. The caller's own not-found handling
        // reports it; here it only has to fail closed.
        private static AccessVerdict RefuseMissingApproval(Guid approvalId) =>
            new AccessVerdict
            {
                IsPermitted = false,
                DenialReason = AccessDenialReason.ParentApprovalUnavailable,
                IsBypassUsed = false,
                BypassedBlockReason = AccessDenialReason.None,
                Explanation = $"No approval was found for id {approvalId}.",
            };

        public async ValueTask<AccessVerdict> MayRecordApprovalReviewAsync(
            Guid approvalId,
            bool isAmendingOwnReview,
            SecurityContext securityContext,
            CancellationToken cancellationToken = default)
        {
            AccessActor actor = await BuildActorAsync(securityContext);

            Approval maybeApproval = await this.storageBroker.SelectApprovalByIdAsync(
                approvalId,
                cancellationToken);

            if (maybeApproval is null)
            {
                // A review whose approval cannot be found is refused rather than waved through.
                // The caller's own not-found handling reports this to the user; here it only has
                // to fail closed.
                return new AccessVerdict
                {
                    IsPermitted = false,
                    DenialReason = AccessDenialReason.ApprovalNotOpenForReview,
                    IsBypassUsed = false,
                    BypassedBlockReason = AccessDenialReason.None,
                    Explanation = $"No approval was found for id {approvalId}.",
                };
            }

            ApprovalReviewSnapshot snapshot = await GatherAsync(maybeApproval, cancellationToken);

            (string entityCreatedBy, IReadOnlyList<RoleSubject> roleSubjects, _) = await ResolveEntityAsync(
                maybeApproval.EntityType,
                maybeApproval.EntityId,
                cancellationToken);

            return await this.securityClient.Access.MayRecordApprovalReviewAsync(
                new RecordReviewRequest
                {
                    Actor = actor,

                    // Not composed from the approval's own EntityType. An association has no
                    // scoped roles of its own — every scoped question is answered from its two
                    // endpoints (§14.7 posture A′ rule 2) — so the traversal below decides how
                    // many subjects there are, and this must not second-guess it.
                    RoleSubjects = roleSubjects,
                    EntityCreatedBy = entityCreatedBy,
                    ApprovalState = snapshot.State,
                    ExistingReviews = snapshot.Reviews,
                    IsAmendingOwnReview = isAmendingOwnReview,
                });
        }

        public async ValueTask<AccessVerdict> MayAmendApprovalAsync(
            Guid approvalId,
            SecurityContext securityContext,
            CancellationToken cancellationToken = default)
        {
            AccessActor actor = await BuildActorAsync(securityContext);

            Approval maybeApproval = await this.storageBroker.SelectApprovalByIdAsync(
                approvalId,
                cancellationToken);

            if (maybeApproval is null)
            {
                return RefuseMissingApproval(approvalId);
            }

            // Subjects only. The amendment decision reads neither the round nor the reviews, so
            // gathering them would be work whose result is discarded — and would invite a later
            // change to start consulting a window that posture D rule 3 exists to let callers move.
            (string entityCreatedBy, IReadOnlyList<RoleSubject> roleSubjects, _) =
                await ResolveEntityAsync(
                    maybeApproval.EntityType,
                    maybeApproval.EntityId,
                    cancellationToken);

            return await this.securityClient.Access.MayAmendApprovalAsync(
                new AmendApprovalRequest
                {
                    Actor = actor,
                    RoleSubjects = roleSubjects,

                    // From STORAGE. Taking the submitter from a caller's copy would let anyone
                    // name themselves the owner and clear the gate on someone else's approval.
                    //
                    // The ENTITY's creator, not the approval's. The workflow owns Approval rows
                    // outright — it opens them itself when content is submitted — so
                    // Approval.CreatedBy records the system and never a person. The submitter
                    // §14.7 posture D rule 3 admits is whoever wrote the content the round is
                    // about, which is what the three sibling decisions already anchor on.
                    EntityCreatedBy = entityCreatedBy,
                });
        }


        public async ValueTask<AccessVerdict> MayDecideApprovalByIdAsync(
            Guid approvalId,
            ApprovalDecision decision,
            bool isBypassRequested,
            string? bypassReason,
            SecurityContext securityContext,
            CancellationToken cancellationToken = default)
        {
            AccessActor actor = await BuildActorAsync(securityContext);

            Approval maybeApproval = await this.storageBroker.SelectApprovalByIdAsync(
                approvalId,
                cancellationToken);

            if (maybeApproval is null)
            {
                return RefuseMissingApproval(approvalId);
            }

            // Everything below is resolved off the STORED approval's target. A payload naming a
            // different entity could otherwise move the question onto rows the caller does hold
            // a role for — the same reason the amend gate reads storage.
            (string entityCreatedBy, IReadOnlyList<RoleSubject> roleSubjects, decimal? confidenceScore) =
                await ResolveEntityAsync(
                    maybeApproval.EntityType,
                    maybeApproval.EntityId,
                    cancellationToken);

            ApprovalReviewSnapshot snapshot = await GatherAsync(maybeApproval, cancellationToken);

            IReadOnlyList<ApprovalPolicy> candidatePolicies = await GatherPoliciesAsync(
                maybeApproval.EntityType,
                cancellationToken);

            return await this.securityClient.Access.MayDecideApprovalAsync(
                new DecideApprovalRequest
                {
                    Actor = actor,
                    Decision = decision,
                    RoleSubjects = roleSubjects,
                    CandidatePolicies = candidatePolicies,
                    EntityType = maybeApproval.EntityType.ToString(),

                    // The policy key's content-type half. Only ContentItem scopes its policies
                    // this way, and its single subject carries the stored row's value — an
                    // association's policy key is its own type, never an endpoint's (§8.4).
                    ContentType = maybeApproval.EntityType == EntityType.ContentItem
                        ? roleSubjects[0].ContentType
                        : null,

                    EntityCreatedBy = entityCreatedBy,
                    ApprovalState = snapshot.State,
                    Reviews = snapshot.Reviews,
                    ApprovalComments = snapshot.ApprovalComments,
                    ConfidenceScore = confidenceScore,
                    IsBypassRequested = isBypassRequested,
                    BypassReason = bypassReason,
                });
        }

        private async ValueTask<AccessActor> BuildActorAsync(SecurityContext securityContext)
        {
            // The actor id comes from the same function that stamped CreatedBy, NOT from
            // SecurityContext.SubjectId. The self-review and self-approval rules compare the two,
            // and two resolvers make that comparison meaningless.
            //
            // This reaches the security client directly rather than through SecurityAuditBroker,
            // which would only have forwarded the same call. What the two brokers DO have to
            // share is the principal built from the envelope's actor — hence the one factory.
            string actorUserId = await this.securityClient.Audits.GetUserIdAsync(
                SecurityContextPrincipalFactory.Create(securityContext));

            return new AccessActor
            {
                UserId = actorUserId,
                Roles = securityContext.Roles,
                IsAuthenticated = securityContext.IsAuthenticated,
            };
        }

        private async ValueTask<Approval?> FindApprovalAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken)
        {
            IQueryable<Approval> approvals =
                await this.storageBroker.SelectAllApprovalsAsync(cancellationToken);

            // Deliberately unfiltered on IsDeleted. A closed approval still occupies the
            // (EntityType, EntityId) key, and reading past it would answer "no round" for a row
            // that has one.
            return approvals.FirstOrDefault(approval =>
                approval.EntityType == entityType && approval.EntityId == entityId);
        }

        // Unfiltered, deliberately — see IAccessBroker for why the caller-facing read cannot
        // answer this. Same storage source GatherAsync uses, so the half that decides WHAT to
        // dismiss and the half that decides whether to APPROVE read one view of one table.
        public async ValueTask<List<Guid>> FindDismissableApprovalReviewIdsAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default)
        {
            IQueryable<ApprovalReview> allApprovalReviews =
                await this.storageBroker.SelectAllApprovalReviewsAsync(cancellationToken);

            return allApprovalReviews
                .Where(approvalReview =>
                    approvalReview.ApprovalId == approvalId
                        && approvalReview.IsDeleted == false
                        && approvalReview.StatusId != ApprovalStatus.Dismissed)
                .Select(approvalReview => approvalReview.Id)
                .ToList();
        }

        private async ValueTask<ApprovalReviewSnapshot> GatherAsync(
            Approval approval,
            CancellationToken cancellationToken)
        {
            IQueryable<ApprovalReview> allReviews =
                await this.storageBroker.SelectAllApprovalReviewsAsync(cancellationToken);

            IQueryable<ApprovalComment> allComments =
                await this.storageBroker.SelectAllApprovalCommentsAsync(cancellationToken);

            // Soft-deleted and dismissed rows are gathered rather than filtered out. Which of
            // them count is a rule, and the rules live in one place — filtering here would make
            // half the decision silently and untestably.
            List<ReviewRecord> reviews = allReviews
                .Where(review => review.ApprovalId == approval.Id)
                .Select(review => new ReviewRecord
                {
                    CreatedBy = review.CreatedBy,
                    Verdict = ToReviewVerdict(review.StatusId),
                    IsDeleted = review.IsDeleted,
                })
                .ToList();

            List<ApprovalCommentRecord> comments = allComments
                .Where(comment => comment.ApprovalId == approval.Id)
                .Select(comment => new ApprovalCommentRecord
                {
                    IsResolved = comment.IsResolved,
                    IsDeleted = comment.IsDeleted,
                })
                .ToList();

            return new ApprovalReviewSnapshot(
                ToApprovalState(approval.ApprovalStatus),
                reviews,
                comments);
        }

        private async ValueTask<IReadOnlyList<ApprovalPolicy>> GatherPoliciesAsync(
            EntityType entityType,
            CancellationToken cancellationToken)
        {
            IQueryable<ApprovalSetting> approvalSettings =
                await this.storageBroker.SelectAllApprovalSettingsAsync(cancellationToken);

            // Soft-deleted rows ARE filtered here, unlike reviews and comments — a deleted
            // setting is skipped at every tier by §8.4, so it is not a candidate at all rather
            // than a candidate the decision discounts.
            return approvalSettings
                .Where(setting => setting.EntityType == entityType && setting.IsDeleted == false)
                .Select(setting => new ApprovalPolicy
                {
                    EntityType = setting.EntityType.ToString(),
                    ContentType = setting.ContentType.HasValue
                        ? setting.ContentType.Value.ToString()
                        : null,
                    RequireApprovals = setting.RequireApprovals,
                    RequiredNumberOfApprovals = setting.RequiredNumberOfApprovals,
                    AutoApproveIfAllApprovalRequirementsMet =
                        setting.AutoApproveIfAllApprovalRequirementsMet,
                    AllowSelfApproval = setting.AllowSelfApproval,
                    BlockOnReject = setting.BlockOnReject,
                    BlockOnZeroApprovalScore = setting.BlockOnZeroApprovalScore,
                    RequireReapprovalOnChange = setting.RequireReapprovalOnChange,
                    RequireReviewCommentResolutionBeforeApprovals =
                        setting.RequireReviewCommentResolutionBeforeApprovals,
                    DoNotAllowBypassingSettings = setting.DoNotAllowBypassingSettings,
                })
                .ToList();
        }

        // The traversal the self-review bar needs: an approval names an entity type and a row,
        // and only the row itself carries the author. It is a switch rather than a denormalised
        // column on Approval because a copied author is a second source of truth for the field
        // the whole rule turns on.
        //
        // The content type comes back on the same read, which is what lets a review role scoped
        // to one content type be recognised — the approval row alone could only ever have
        // supported the coarse tier.
        /// <summary>
        /// Reads the entity behind an approval and returns both facts the decision needs about
        /// it: who authored it, and which role subjects the actor could be authorised through.
        ///
        /// <para>The two travel together because they come from the same read. Splitting them
        /// would mean loading the row twice, and — worse — would let the subjects be composed
        /// somewhere that does not have the row in hand, which is exactly the mistake this
        /// replaces: the subjects used to be built from the approval's own <c>EntityType</c>,
        /// which is right for every entity except the one that matters.</para>
        /// </summary>
        private async ValueTask<(string CreatedBy, IReadOnlyList<RoleSubject> RoleSubjects, decimal? ConfidenceScore)> ResolveEntityAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken)
        {
            switch (entityType)
            {
                case EntityType.ContentItem:
                    var contentItem =
                        await this.storageBroker.SelectContentItemByIdAsync(entityId, cancellationToken);

                    return (
                        contentItem?.CreatedBy ?? string.Empty,
                        SubjectsFor(entityType, contentItem?.ContentType),
                        null);

                case EntityType.Tag:
                    var tag = await this.storageBroker.SelectTagByIdAsync(entityId, cancellationToken);

                    return (tag?.CreatedBy ?? string.Empty, SubjectsFor(entityType, contentType: null), null);

                case EntityType.Reaction:
                    var reaction =
                        await this.storageBroker.SelectReactionByIdAsync(entityId, cancellationToken);

                    return (reaction?.CreatedBy ?? string.Empty, SubjectsFor(entityType, contentType: null), null);

                case EntityType.BibleReference:
                    var bibleReference =
                        await this.storageBroker.SelectBibleReferenceByIdAsync(entityId, cancellationToken);

                    return (
                        bibleReference?.CreatedBy ?? string.Empty,
                        SubjectsFor(entityType, contentType: null), null);

                case EntityType.Comment:
                    var comment =
                        await this.storageBroker.SelectCommentByIdAsync(entityId, cancellationToken);

                    return (comment?.CreatedBy ?? string.Empty, SubjectsFor(entityType, contentType: null), null);

                case EntityType.Link:
                    var link = await this.storageBroker.SelectLinkByIdAsync(entityId, cancellationToken);

                    return (link?.CreatedBy ?? string.Empty, SubjectsFor(entityType, contentType: null), null);

                case EntityType.Attachment:
                    var attachment =
                        await this.storageBroker.SelectAttachmentByIdAsync(entityId, cancellationToken);

                    return (attachment?.CreatedBy ?? string.Empty, SubjectsFor(entityType, contentType: null), null);

                case EntityType.Association:
                    var association =
                        await this.storageBroker.SelectAssociationByIdAsync(entityId, cancellationToken);

                    // BOTH endpoints, and holding a role for either is enough (§14.7 posture A′
                    // rule 2). An association has no scoped roles of its own — Roles.cs issues no
                    // "Association-Reviewers" — so composing a subject from Association would ask
                    // for a role nobody can hold, which is what the row below falls back to when
                    // the association is missing, deliberately.
                    return association is null
                        ? (string.Empty, SubjectsFor(entityType, contentType: null), null)
                        : (association.CreatedBy, new List<RoleSubject>
                        {
                            new RoleSubject
                            {
                                EntityType = association.EntityAType.ToString(),
                                ContentType = association.EntityAContentType?.ToString(),
                            },
                            new RoleSubject
                            {
                                EntityType = association.EntityBType.ToString(),
                                ContentType = association.EntityBContentType?.ToString(),
                            },
                        }, association.ConfidenceScore);

                default:
                    // A new EntityType member reaching here has no traversal, so the author is
                    // unknown. Returning empty is the fail-closed answer and not an oversight:
                    // the decision function never treats blank as matching blank, so an unknown
                    // author can never satisfy "is this the author?" — it refuses the review for
                    // no-role reasons instead of silently permitting a self-review.
                    //
                    // The SUBJECT is not fail-closed, and must not be read as though it were.
                    // It is the entity type's own name, and Roles.cs issues a
                    // "{Entity}-Reviewers"/"{Entity}-Publishers" pair for every type it supports —
                    // so a new member whose roles were seeded but whose traversal case was
                    // forgotten lands here and its scoped role holders still clear the tier, with
                    // a blank author underneath. Association is the sole type with no scoped roles
                    // of its own; a future one is not. Add the case above rather than relying on
                    // this arm.
                    return (string.Empty, SubjectsFor(entityType, contentType: null), null);
            }
        }

        // Sits beside ResolveEntityAsync because it IS that read, narrowed. The gates that use
        // it want one string and have no use for the subjects or the confidence score.
        public async ValueTask<string> RetrieveEntityAuthorAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken = default)
        {
            (string entityCreatedBy, _, _) = await ResolveEntityAsync(
                entityType,
                entityId,
                cancellationToken);

            return entityCreatedBy;
        }

        // The set-shaped twin of RetrieveEntityAuthorAsync, and deliberately NOT that method in a
        // loop. ResolveEntityAsync answers one row with one read; asking it per approval is the
        // N+1 this exists to avoid, and materialising the actor's whole authored corpus instead
        // just moves the cost into an IN (...) that grows with how much they have written.
        //
        // So the ownership test is composed INTO the caller's query: one correlated EXISTS per
        // approvable type, evaluated by the database against rows it is already visiting. The
        // eight arms mirror ResolveEntityAsync's switch one-for-one on purpose — a type with a
        // traversal there and none here would silently stop answering its own author.
        public async ValueTask<IQueryable<Approval>> FilterApprovalsToEntityAuthorAsync(
            IQueryable<Approval> approvals,
            string authorUserId,
            CancellationToken cancellationToken = default)
        {
            // Fail closed, and before any read. An actor whose id could not be resolved must match
            // nothing at all — the single-row gates never treat blank as matching blank, and a
            // collection read that did would hand every approval to whoever arrived without one.
            if (string.IsNullOrWhiteSpace(authorUserId))
            {
                return Enumerable.Empty<Approval>().AsQueryable();
            }

            var contentItems = await this.storageBroker.SelectAllContentItemsAsync(cancellationToken);
            var tags = await this.storageBroker.SelectAllTagsAsync(cancellationToken);
            var reactions = await this.storageBroker.SelectAllReactionsAsync(cancellationToken);
            var bibleReferences = await this.storageBroker.SelectAllBibleReferencesAsync(cancellationToken);
            var comments = await this.storageBroker.SelectAllCommentsAsync(cancellationToken);
            var links = await this.storageBroker.SelectAllLinksAsync(cancellationToken);
            var attachments = await this.storageBroker.SelectAllAttachmentsAsync();
            var associations = await this.storageBroker.SelectAllAssociationsAsync(cancellationToken);

            // The EntityType arm is tested alongside every id match rather than matching the id
            // alone. Ids are Guids and a collision across two tables is not a realistic worry —
            // but the pairing is what "the author of THIS approval's entity" means, and a filter
            // that reads the id without the discriminator is only accidentally right.
            //
            // No IsDeleted clause on the authored side: a soft-deleted entity keeps its author,
            // and dropping its approval here would hide the round from the very person whose
            // work it is about. What is visible is the APPROVAL's own state, which the caller
            // filtered before handing the query over.
            return approvals.Where(approval =>
                (approval.EntityType == EntityType.ContentItem
                    && contentItems.Any(entity =>
                        entity.Id == approval.EntityId && entity.CreatedBy == authorUserId))
                || (approval.EntityType == EntityType.Tag
                    && tags.Any(entity =>
                        entity.Id == approval.EntityId && entity.CreatedBy == authorUserId))
                || (approval.EntityType == EntityType.Reaction
                    && reactions.Any(entity =>
                        entity.Id == approval.EntityId && entity.CreatedBy == authorUserId))
                || (approval.EntityType == EntityType.BibleReference
                    && bibleReferences.Any(entity =>
                        entity.Id == approval.EntityId && entity.CreatedBy == authorUserId))
                || (approval.EntityType == EntityType.Comment
                    && comments.Any(entity =>
                        entity.Id == approval.EntityId && entity.CreatedBy == authorUserId))
                || (approval.EntityType == EntityType.Link
                    && links.Any(entity =>
                        entity.Id == approval.EntityId && entity.CreatedBy == authorUserId))
                || (approval.EntityType == EntityType.Attachment
                    && attachments.Any(entity =>
                        entity.Id == approval.EntityId && entity.CreatedBy == authorUserId))

                // The association's OWN author, not its endpoints'. Posture A′ widens the REVIEW
                // tier to whoever holds a role on either end; it does not make them the author,
                // and this clause answers authorship alone.
                || (approval.EntityType == EntityType.Association
                    && associations.Any(entity =>
                        entity.Id == approval.EntityId && entity.CreatedBy == authorUserId)));
        }

        /// <summary>
        /// The ordinary one-subject case: an entity is authorised from itself. Only
        /// <c>Association</c> departs from this, and it composes its pair inline.
        /// </summary>
        private static IReadOnlyList<RoleSubject> SubjectsFor(
            EntityType entityType,
            ContentType? contentType) =>
            new List<RoleSubject>
            {
                new RoleSubject
                {
                    EntityType = entityType.ToString(),
                    ContentType = contentType?.ToString(),
                },
            };

        private static ApprovalState ToApprovalState(ApprovalStatus approvalStatus) =>
            approvalStatus switch
            {
                ApprovalStatus.Submitted => ApprovalState.Submitted,
                ApprovalStatus.Approved => ApprovalState.Approved,
                ApprovalStatus.Rejected => ApprovalState.Rejected,

                // Draft, and Dismissed — which an approval must never hold (§9.5). Both land on
                // Draft, the state from which nothing may be reviewed or decided.
                _ => ApprovalState.Draft,
            };

        private static ReviewVerdict ToReviewVerdict(ApprovalStatus approvalStatus) =>
            approvalStatus switch
            {
                ApprovalStatus.Approved => ReviewVerdict.Approved,
                ApprovalStatus.Rejected => ReviewVerdict.Rejected,

                // Dismissed, and the two an ApprovalReview cannot legally hold. A stored row
                // carrying Draft or Submitted is corrupt, and counting it as an approval would
                // let corruption meet a threshold — so everything that is not an explicit
                // Approved or Rejected is treated as not counting.
                _ => ReviewVerdict.Dismissed,
            };

        private sealed record ApprovalReviewSnapshot(
            ApprovalState State,
            IReadOnlyList<ReviewRecord> Reviews,
            IReadOnlyList<ApprovalCommentRecord> ApprovalComments)
        {
            public static ApprovalReviewSnapshot Empty { get; } =
                new ApprovalReviewSnapshot(
                    ApprovalState.Draft,
                    new List<ReviewRecord>(),
                    new List<ApprovalCommentRecord>());
        }

        // The gathering half of §16.7.2's verdict. Same reads as MayDecideApprovalByIdAsync —
        // resolved off the STORED approval's target, never a payload — but it asks the
        // conditions question rather than the may-this-actor question, so it carries no actor
        // and no bypass request. Whether the caller may act on the answer is asked separately.
        public async ValueTask<ApprovalConditionsVerdict?> EvaluateApprovalConditionsByIdAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default)
        {
            Approval maybeApproval = await this.storageBroker.SelectApprovalByIdAsync(
                approvalId,
                cancellationToken);

            if (maybeApproval is null)
            {
                return null;
            }

            (_, IReadOnlyList<RoleSubject> roleSubjects, decimal? confidenceScore) =
                await ResolveEntityAsync(
                    maybeApproval.EntityType,
                    maybeApproval.EntityId,
                    cancellationToken);

            ApprovalReviewSnapshot snapshot = await GatherAsync(maybeApproval, cancellationToken);

            IReadOnlyList<ApprovalPolicy> candidatePolicies = await GatherPoliciesAsync(
                maybeApproval.EntityType,
                cancellationToken);

            return await this.securityClient.Access.EvaluateApprovalConditionsAsync(
                new ApprovalConditionsRequest
                {
                    CandidatePolicies = candidatePolicies,
                    EntityType = maybeApproval.EntityType.ToString(),

                    // Only ContentItem scopes its policies by content type; an association's
                    // policy key is its own type, never an endpoint's (§8.4).
                    ContentType = maybeApproval.EntityType == EntityType.ContentItem
                        ? roleSubjects[0].ContentType
                        : null,

                    Reviews = snapshot.Reviews,
                    ApprovalComments = snapshot.ApprovalComments,
                    ConfidenceScore = confidenceScore,
                });
        }

        // Gather-only (§16.7.4). Every field is read from the STORED approval and its stored
        // entity — nothing here trusts a payload, because the owner it reports is what stops
        // somebody being invited to review their own work (§7.9 rule 3, HR-1).
        public async ValueTask<ApprovalReviewerScope?> RetrieveApprovalReviewerScopeByIdAsync(
            Guid approvalId,
            CancellationToken cancellationToken = default)
        {
            Approval maybeApproval = await this.storageBroker.SelectApprovalByIdAsync(
                approvalId,
                cancellationToken);

            if (maybeApproval is null)
            {
                return null;
            }

            (string entityCreatedBy, IReadOnlyList<RoleSubject> roleSubjects, _) =
                await ResolveEntityAsync(
                    maybeApproval.EntityType,
                    maybeApproval.EntityId,
                    cancellationToken);

            ApprovalReviewSnapshot snapshot = await GatherAsync(maybeApproval, cancellationToken);

            // Only the reviews that still stand. A withdrawn review frees the person to be asked
            // again, and a dismissed one means their verdict no longer describes the current
            // content (§9.5) — in both cases they are invitable, which is the same reasoning
            // behind the review index's filter.
            List<string> activeReviewerUserIds = snapshot.Reviews
                .Where(review =>
                    review.IsDeleted == false
                        && review.Verdict != ReviewVerdict.Dismissed)
                .Select(review => review.CreatedBy)
                .Where(createdBy => string.IsNullOrWhiteSpace(createdBy) is false)
                .Distinct()
                .ToList();

            // And the same rows again with nothing subtracted, because a different question is
            // asked of them. The set above decides INVITABILITY, which a dismissed or withdrawn
            // verdict releases; this one reports who the round INVOLVED, which nothing releases.
            // The name resolver (§16.7.4) needs the second: a panel renders a dismissed review,
            // so it must be able to name the person who wrote it.
            List<string> recordedReviewerUserIds = snapshot.Reviews
                .Select(review => review.CreatedBy)
                .Where(createdBy => string.IsNullOrWhiteSpace(createdBy) is false)
                .Distinct()
                .ToList();

            // Unfiltered on purpose (see ActiveReviewRequest): the caller-facing read applies a
            // visibility filter, and deciding invitability from a filtered view would tell a
            // moderator that somebody is invitable and then collide with the uniqueness index.
            IQueryable<ApprovalReviewRequest> allRequests =
                await this.storageBroker.SelectAllApprovalReviewRequestsAsync(cancellationToken);

            List<ActiveReviewRequest> activeRequests = allRequests
                .Where(request =>
                    request.ApprovalId == maybeApproval.Id
                        && request.IsDeleted == false)
                .Select(request => new ActiveReviewRequest
                {
                    Id = request.Id,
                    RequestedUserId = request.RequestedUserId,
                })
                .ToList();

            return new ApprovalReviewerScope
            {
                ApprovalId = maybeApproval.Id,
                ApprovalStatus = maybeApproval.ApprovalStatus,
                EntityCreatedBy = entityCreatedBy,
                RoleSubjects = roleSubjects,
                ActiveReviewerUserIds = activeReviewerUserIds,
                RecordedReviewerUserIds = recordedReviewerUserIds,
                ActiveRequests = activeRequests,
            };
        }
    }
}
