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

        // A comment whose approval cannot be found is refused rather than waved through. The
        // caller's own not-found handling reports it; here it only has to fail closed.
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

            (string entityCreatedBy, ContentType? contentType) = await ResolveEntityAsync(
                maybeApproval.EntityType,
                maybeApproval.EntityId,
                cancellationToken);

            return await this.securityClient.Access.MayRecordApprovalReviewAsync(
                new RecordReviewRequest
                {
                    Actor = actor,
                    RoleSubjects = new List<RoleSubject>
                    {
                        new RoleSubject
                        {
                            EntityType = maybeApproval.EntityType.ToString(),
                            ContentType = contentType?.ToString(),
                        },
                    },
                    EntityCreatedBy = entityCreatedBy,
                    ApprovalState = snapshot.State,
                    ExistingReviews = snapshot.Reviews,
                    IsAmendingOwnReview = isAmendingOwnReview,
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
        private async ValueTask<(string CreatedBy, ContentType? ContentType)> ResolveEntityAsync(
            EntityType entityType,
            Guid entityId,
            CancellationToken cancellationToken)
        {
            switch (entityType)
            {
                case EntityType.ContentItem:
                    var contentItem =
                        await this.storageBroker.SelectContentItemByIdAsync(entityId, cancellationToken);

                    return (contentItem?.CreatedBy ?? string.Empty, contentItem?.ContentType);

                case EntityType.Tag:
                    var tag = await this.storageBroker.SelectTagByIdAsync(entityId, cancellationToken);

                    return (tag?.CreatedBy ?? string.Empty, null);

                case EntityType.Reaction:
                    var reaction =
                        await this.storageBroker.SelectReactionByIdAsync(entityId, cancellationToken);

                    return (reaction?.CreatedBy ?? string.Empty, null);

                case EntityType.BibleReference:
                    var bibleReference =
                        await this.storageBroker.SelectBibleReferenceByIdAsync(entityId, cancellationToken);

                    return (bibleReference?.CreatedBy ?? string.Empty, null);

                case EntityType.Comment:
                    var comment =
                        await this.storageBroker.SelectCommentByIdAsync(entityId, cancellationToken);

                    return (comment?.CreatedBy ?? string.Empty, null);

                case EntityType.Link:
                    var link = await this.storageBroker.SelectLinkByIdAsync(entityId, cancellationToken);

                    return (link?.CreatedBy ?? string.Empty, null);

                case EntityType.Attachment:
                    var attachment =
                        await this.storageBroker.SelectAttachmentByIdAsync(entityId, cancellationToken);

                    return (attachment?.CreatedBy ?? string.Empty, null);

                case EntityType.Association:
                    var association =
                        await this.storageBroker.SelectAssociationByIdAsync(entityId, cancellationToken);

                    return (association?.CreatedBy ?? string.Empty, null);

                default:
                    // A new EntityType member reaching here has no traversal, so the author is
                    // unknown. Returning empty is the fail-closed answer and not an oversight:
                    // the decision function never treats blank as matching blank, so an unknown
                    // author can never satisfy "is this the author?" — it refuses the review for
                    // no-role reasons instead of silently permitting a self-review.
                    return (string.Empty, null);
            }
        }

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
    }
}
