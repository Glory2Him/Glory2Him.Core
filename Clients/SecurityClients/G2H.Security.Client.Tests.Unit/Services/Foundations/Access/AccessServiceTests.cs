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

using System.Collections.Generic;
using G2H.Security.Client.Models.Foundations.Access;
using G2H.Security.Client.Models.Securities;
using G2H.Security.Client.Services.Foundations.Access;
using Tynamix.ObjectFiller;

namespace G2H.Security.Client.Tests.Unit.Services.Foundations.Access
{
    public partial class AccessServiceTests
    {
        private readonly IAccessService accessService;

        public AccessServiceTests() =>
            this.accessService = new AccessService();

        private static AccessActor CreateRandomAccessActor(
            string? userId = null,
            IReadOnlyList<string>? roles = null,
            bool isAuthenticated = true) =>
            new AccessActor
            {
                UserId = userId ?? GetRandomString(),
                Roles = roles ?? new List<string>(),
                IsAuthenticated = isAuthenticated,
            };

        // entityType null means a RANDOM one, as every test has always read it; the global tier
        // is asked for explicitly through isGlobal so no existing test silently changes tier.
        private static ApprovalPolicy CreateRandomApprovalPolicy(
            string? entityType = null,
            string? contentType = null,
            bool? isPersonal = null,
            bool isGlobal = false,
            bool requireApprovals = true,
            int requiredNumberOfApprovals = 1,
            bool autoApproveIfAllApprovalRequirementsMet = false,
            bool allowSelfApproval = false,
            bool blockOnReject = false,
            bool blockOnZeroApprovalScore = false,
            bool requireReapprovalOnChange = false,
            bool requireReviewCommentResolutionBeforeApprovals = false,
            bool doNotAllowBypassingSettings = false) =>
            new ApprovalPolicy
            {
                EntityType = isGlobal ? null : entityType ?? GetRandomString(),
                ContentType = contentType,
                IsPersonal = isPersonal,
                RequireApprovals = requireApprovals,
                RequiredNumberOfApprovals = requiredNumberOfApprovals,
                AutoApproveIfAllApprovalRequirementsMet = autoApproveIfAllApprovalRequirementsMet,
                AllowSelfApproval = allowSelfApproval,
                BlockOnReject = blockOnReject,
                BlockOnZeroApprovalScore = blockOnZeroApprovalScore,
                RequireReapprovalOnChange = requireReapprovalOnChange,

                RequireReviewCommentResolutionBeforeApprovals =
                    requireReviewCommentResolutionBeforeApprovals,

                DoNotAllowBypassingSettings = doNotAllowBypassingSettings,
            };

        private static RecordApprovalCommentRequest CreateRandomRecordApprovalCommentRequest(
            AccessActor? actor = null,
            ApprovalState approvalState = ApprovalState.Submitted,
            bool isParentApprovalDeleted = false) =>
            new RecordApprovalCommentRequest
            {
                Actor = actor ?? CreateRandomAccessActor(),
                ApprovalState = approvalState,
                IsParentApprovalDeleted = isParentApprovalDeleted,
            };

        private static AmendApprovalCommentRequest CreateRandomAmendApprovalCommentRequest(
            AccessActor? actor = null,
            string? commentCreatedBy = null,
            ApprovalState approvalState = ApprovalState.Submitted,
            bool isParentApprovalDeleted = false) =>
            new AmendApprovalCommentRequest
            {
                Actor = actor ?? CreateRandomAccessActor(),
                CommentCreatedBy = commentCreatedBy ?? GetRandomString(),
                ApprovalState = approvalState,
                IsParentApprovalDeleted = isParentApprovalDeleted,
            };

        private static ResolveApprovalCommentRequest CreateRandomResolveApprovalCommentRequest(
            AccessActor? actor = null,
            string? commentCreatedBy = null,
            ApprovalState approvalState = ApprovalState.Submitted,
            bool isParentApprovalDeleted = false) =>
            new ResolveApprovalCommentRequest
            {
                Actor = actor ?? CreateRandomAccessActor(),
                CommentCreatedBy = commentCreatedBy ?? GetRandomString(),
                ApprovalState = approvalState,
                IsParentApprovalDeleted = isParentApprovalDeleted,
            };

        private static ReviewRecord CreateRandomReviewRecord(
            string? createdBy = null,
            ReviewVerdict verdict = ReviewVerdict.Approved,
            bool isDeleted = false)
        {
            return new ReviewRecord
            {
                CreatedBy = createdBy ?? GetRandomString(),
                Verdict = verdict,
                IsDeleted = isDeleted,
            };
        }

        private static ApprovalCommentRecord CreateRandomApprovalCommentRecord(
            bool isResolved = true,
            bool isDeleted = false) =>
            new ApprovalCommentRecord
            {
                IsResolved = isResolved,
                IsDeleted = isDeleted,
            };

        private static RoleSubject CreateRandomRoleSubject(
            string? entityType = null,
            string? contentType = null) =>
            new RoleSubject
            {
                EntityType = entityType ?? GetRandomString(),
                ContentType = contentType,
            };

        private static ApprovalConditionsRequest CreateRandomApprovalConditionsRequest(
            IReadOnlyList<ApprovalPolicy>? candidatePolicies = null,
            string? entityType = null,
            string? contentType = null,
            bool? isPersonal = null,
            IReadOnlyList<ReviewRecord>? reviews = null,
            IReadOnlyList<ApprovalCommentRecord>? comments = null,
            decimal? confidenceScore = null) =>
            new ApprovalConditionsRequest
            {
                CandidatePolicies = candidatePolicies ?? new List<ApprovalPolicy>(),
                EntityType = entityType ?? GetRandomString(),
                ContentType = contentType,
                IsPersonal = isPersonal,
                Reviews = reviews ?? new List<ReviewRecord>(),
                ApprovalComments = comments ?? new List<ApprovalCommentRecord>(),
                ConfidenceScore = confidenceScore,
            };

        // Binds a request to ONE policy row so a §8.5 test can vary a single policy flag
        // without also having to keep the entity type and content type in step by hand.
        private static ApprovalConditionsRequest CreateApprovalConditionsRequestFor(
            ApprovalPolicy approvalPolicy,
            IReadOnlyList<ReviewRecord>? reviews = null,
            IReadOnlyList<ApprovalCommentRecord>? comments = null,
            decimal? confidenceScore = null) =>
            CreateRandomApprovalConditionsRequest(
                candidatePolicies: new List<ApprovalPolicy> { approvalPolicy },
                entityType: approvalPolicy.EntityType,
                contentType: approvalPolicy.ContentType,
                isPersonal: approvalPolicy.IsPersonal,
                reviews: reviews,
                comments: comments,
                confidenceScore: confidenceScore);

        private static RecordReviewRequest CreateRandomRecordReviewRequest(
            AccessActor? actor = null,
            IReadOnlyList<RoleSubject>? roleSubjects = null,
            string? entityCreatedBy = null,
            ApprovalState approvalState = ApprovalState.Submitted,
            IReadOnlyList<ReviewRecord>? existingReviews = null,
            bool isAmendingOwnReview = false) =>
            new RecordReviewRequest
            {
                Actor = actor ?? CreateRandomAccessActor(
                    roles: new List<string> { RoleNames.Reviewers }),

                RoleSubjects = roleSubjects ?? new List<RoleSubject>(),
                EntityCreatedBy = entityCreatedBy ?? GetRandomString(),
                ApprovalState = approvalState,
                ExistingReviews = existingReviews ?? new List<ReviewRecord>(),
                IsAmendingOwnReview = isAmendingOwnReview,
            };

        private static AmendApprovalRequest CreateRandomAmendApprovalRequest(
            AccessActor? actor = null,
            IReadOnlyList<RoleSubject>? roleSubjects = null,
            string? entityCreatedBy = null) =>
            new AmendApprovalRequest
            {
                Actor = actor ?? CreateRandomAccessActor(
                    roles: new List<string> { RoleNames.Reviewers }),

                RoleSubjects = roleSubjects ?? new List<RoleSubject>(),

                // Someone else by default, so a tier test cannot pass through the owner branch
                // and quietly stop testing the tier.
                EntityCreatedBy = entityCreatedBy ?? GetRandomString(),
            };

        private static DecideApprovalRequest CreateRandomDecideApprovalRequest(
            AccessActor? actor = null,
            ApprovalDecision decision = ApprovalDecision.Approve,
            ApprovalPolicy? policy = null,
            IReadOnlyList<RoleSubject>? roleSubjects = null,
            string? entityCreatedBy = null,
            ApprovalState approvalState = ApprovalState.Submitted,
            IReadOnlyList<ReviewRecord>? reviews = null,
            IReadOnlyList<ApprovalCommentRecord>? comments = null,
            decimal? confidenceScore = null,
            bool isBypassRequested = false,
            string? bypassReason = null)
        {
            ApprovalPolicy resolvedPolicy = policy
                ?? CreateRandomApprovalPolicy(requireApprovals: false);

            return new DecideApprovalRequest
            {
                Actor = actor ?? CreateRandomAccessActor(
                    roles: new List<string> { RoleNames.Publishers }),

                Decision = decision,
                RoleSubjects = roleSubjects ?? new List<RoleSubject>(),
                CandidatePolicies = new List<ApprovalPolicy> { resolvedPolicy },
                EntityType = resolvedPolicy.EntityType!,
                ContentType = resolvedPolicy.ContentType,
                IsPersonal = resolvedPolicy.IsPersonal,
                EntityCreatedBy = entityCreatedBy ?? GetRandomString(),
                ApprovalState = approvalState,
                Reviews = reviews ?? new List<ReviewRecord>(),
                ApprovalComments = comments ?? new List<ApprovalCommentRecord>(),
                ConfidenceScore = confidenceScore,
                IsBypassRequested = isBypassRequested,
                BypassReason = bypassReason,
            };
        }

        private static string GetRandomString() =>
            new MnemonicString(wordCount: GetRandomNumber()).GetValue();

        private static int GetRandomNumber() =>
            new IntRange(min: 2, max: 10).GetValue();
    }
}
