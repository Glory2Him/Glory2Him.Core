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
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Foundations.Access;

namespace G2H.Security.Client.Tests.Unit.Services.Foundations.Access
{
    public partial class AccessServiceTests
    {
        [Fact]
        public async Task ShouldResolveTheContentTypeRowOverTheEntityTypeDefaultAsync()
        {
            // given
            string entityType = GetRandomString();
            string contentType = GetRandomString();

            ApprovalPolicy entityTypeDefault = CreateRandomApprovalPolicy(
                entityType: entityType,
                contentType: null,
                requiredNumberOfApprovals: 1);

            ApprovalPolicy contentTypeRow = CreateRandomApprovalPolicy(
                entityType: entityType,
                contentType: contentType,
                requiredNumberOfApprovals: 3);

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateRandomApprovalConditionsRequest(
                    candidatePolicies: new List<ApprovalPolicy>
                    {
                        entityTypeDefault,
                        contentTypeRow,
                    },
                    entityType: entityType,
                    contentType: contentType);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.RequiredNumberOfApprovals.Should().Be(3);
        }

        [Fact]
        public async Task ShouldResolveTheEntityTypeDefaultWhenNoContentTypeRowExistsAsync()
        {
            // given
            string entityType = GetRandomString();
            string contentType = GetRandomString();

            ApprovalPolicy entityTypeDefault = CreateRandomApprovalPolicy(
                entityType: entityType,
                contentType: null,
                requiredNumberOfApprovals: 4);

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateRandomApprovalConditionsRequest(
                    candidatePolicies: new List<ApprovalPolicy> { entityTypeDefault },
                    entityType: entityType,
                    contentType: contentType);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.RequiredNumberOfApprovals.Should().Be(4);
        }

        [Fact]
        public async Task ShouldNotResolveAContentTypeRowBelongingToADifferentContentTypeAsync()
        {
            // given
            string entityType = GetRandomString();
            string contentType = GetRandomString();
            string differentContentType = GetRandomString();

            ApprovalPolicy entityTypeDefault = CreateRandomApprovalPolicy(
                entityType: entityType,
                contentType: null,
                requiredNumberOfApprovals: 2);

            ApprovalPolicy differentContentTypeRow = CreateRandomApprovalPolicy(
                entityType: entityType,
                contentType: differentContentType,
                requiredNumberOfApprovals: 9);

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateRandomApprovalConditionsRequest(
                    candidatePolicies: new List<ApprovalPolicy>
                    {
                        entityTypeDefault,
                        differentContentTypeRow,
                    },
                    entityType: entityType,
                    contentType: contentType);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.RequiredNumberOfApprovals.Should().Be(2);
        }

        // §8.4: the winning row supplies EVERY field. The narrow row below sets only
        // RequireApprovals permissively and leaves the four blocking flags off; if any single
        // field were merged from the broader row, one of the three blockers staged here would
        // fire and the conditions would not be met.
        [Fact]
        public async Task ShouldResolveTheWinningPolicyWholesaleWithoutMergingTheBroaderRowAsync()
        {
            // given
            string entityType = GetRandomString();
            string contentType = GetRandomString();

            ApprovalPolicy strictEntityTypeRow = CreateRandomApprovalPolicy(
                entityType: entityType,
                contentType: null,
                requireApprovals: true,
                requiredNumberOfApprovals: 5,
                blockOnReject: true,
                blockOnZeroApprovalScore: true,
                requireReviewCommentResolutionBeforeApprovals: true);

            ApprovalPolicy permissiveContentTypeRow = CreateRandomApprovalPolicy(
                entityType: entityType,
                contentType: contentType,
                requireApprovals: false);

            var reviews = new List<ReviewRecord>
            {
                CreateRandomReviewRecord(verdict: ReviewVerdict.Rejected),
            };

            var comments = new List<ApprovalCommentRecord>
            {
                CreateRandomApprovalCommentRecord(isResolved: false),
            };

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateRandomApprovalConditionsRequest(
                    candidatePolicies: new List<ApprovalPolicy>
                    {
                        strictEntityTypeRow,
                        permissiveContentTypeRow,
                    },
                    entityType: entityType,
                    contentType: contentType,
                    reviews: reviews,
                    comments: comments,
                    confidenceScore: 0m);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.AreConditionsMet.Should().BeTrue();
            actualVerdict.BlockReason.Should().Be(AccessDenialReason.None);
            actualVerdict.RequiredNumberOfApprovals.Should().Be(0);

            actualVerdict.Explanation.Should()
                .Be("Conditions trivially met: the policy does not require approvals.");
        }

        [Fact]
        public async Task ShouldApplyTheFailClosedSystemDefaultWhenThereAreNoCandidatePoliciesAsync()
        {
            // given
            ApprovalConditionsRequest approvalConditionsRequest =
                CreateRandomApprovalConditionsRequest(
                    candidatePolicies: new List<ApprovalPolicy>(),
                    contentType: GetRandomString());

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.AreConditionsMet.Should().BeFalse();
            actualVerdict.BlockReason.Should().Be(AccessDenialReason.ApprovalThresholdNotMet);
            actualVerdict.RequiredNumberOfApprovals.Should().Be(1);
            actualVerdict.ApprovalCount.Should().Be(0);
        }

        // The guard on a known divergence: the ApprovalSetting entity initialises BlockOnReject
        // to FALSE, while §8.4 rule 2 requires the fail-closed system default to treat it as
        // TRUE. The approval threshold below is satisfied, so only BlockOnReject can refuse.
        [Fact]
        public async Task ShouldTreatBlockOnRejectAsTrueInTheFailClosedSystemDefaultAsync()
        {
            // given
            var reviews = new List<ReviewRecord>
            {
                CreateRandomReviewRecord(verdict: ReviewVerdict.Approved),
                CreateRandomReviewRecord(verdict: ReviewVerdict.Rejected),
            };

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateRandomApprovalConditionsRequest(
                    candidatePolicies: new List<ApprovalPolicy>(),
                    reviews: reviews);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.AreConditionsMet.Should().BeFalse();
            actualVerdict.BlockReason.Should().Be(AccessDenialReason.BlockedByRejection);
            actualVerdict.ApprovalCount.Should().Be(1);
            actualVerdict.RequiredNumberOfApprovals.Should().Be(1);
        }

        // ── The global tier (§8.4 tier 4) ─────────────────────────────────────────────────

        [Fact]
        public async Task ShouldResolveTheGlobalDefaultWhenNoRowNamesTheEntityTypeAsync()
        {
            // given
            string entityType = GetRandomString();

            ApprovalPolicy globalDefault = CreateRandomApprovalPolicy(
                isGlobal: true,
                requiredNumberOfApprovals: 5);

            ApprovalPolicy otherEntityTypeDefault = CreateRandomApprovalPolicy(
                entityType: GetRandomString(),
                requiredNumberOfApprovals: 9);

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateRandomApprovalConditionsRequest(
                    candidatePolicies: new List<ApprovalPolicy>
                    {
                        otherEntityTypeDefault,
                        globalDefault,
                    },
                    entityType: entityType,
                    contentType: null);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.RequiredNumberOfApprovals.Should().Be(5);
        }

        [Fact]
        public async Task ShouldResolveTheEntityTypeDefaultOverTheGlobalDefaultAsync()
        {
            // given
            string entityType = GetRandomString();

            ApprovalPolicy globalDefault = CreateRandomApprovalPolicy(
                isGlobal: true,
                requiredNumberOfApprovals: 5);

            ApprovalPolicy entityTypeDefault = CreateRandomApprovalPolicy(
                entityType: entityType,
                requiredNumberOfApprovals: 2);

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateRandomApprovalConditionsRequest(
                    candidatePolicies: new List<ApprovalPolicy>
                    {
                        globalDefault,
                        entityTypeDefault,
                    },
                    entityType: entityType,
                    contentType: null);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.RequiredNumberOfApprovals.Should().Be(2);
        }

        /// <summary>
        /// The global row is a stored policy, not the fail-closed fallback: where it exists it
        /// answers, and the system default of §8.4 rule 2 is never consulted.
        /// </summary>
        [Fact]
        public async Task ShouldResolveTheGlobalDefaultAheadOfTheSystemDefaultAsync()
        {
            // given: the global row asks for no approvals, which the system default never does
            ApprovalPolicy globalDefault = CreateRandomApprovalPolicy(
                isGlobal: true,
                requireApprovals: false);

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateRandomApprovalConditionsRequest(
                    candidatePolicies: new List<ApprovalPolicy> { globalDefault },
                    entityType: GetRandomString(),
                    contentType: null);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.AreConditionsMet.Should().BeTrue();
            actualVerdict.RequiredNumberOfApprovals.Should().Be(0);
        }

        // ── The personality tier (§8.4 tier 2, associations) ─────────────────────────────

        [Fact]
        public async Task ShouldResolveThePersonalRowOverTheEntityTypeDefaultForAPersonalEntityAsync()
        {
            // given
            string entityType = GetRandomString();

            ApprovalPolicy entityTypeDefault = CreateRandomApprovalPolicy(
                entityType: entityType,
                requiredNumberOfApprovals: 2);

            ApprovalPolicy personalRow = CreateRandomApprovalPolicy(
                entityType: entityType,
                isPersonal: true,
                requireApprovals: false,
                autoApproveIfAllApprovalRequirementsMet: true);

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateRandomApprovalConditionsRequest(
                    candidatePolicies: new List<ApprovalPolicy>
                    {
                        entityTypeDefault,
                        personalRow,
                    },
                    entityType: entityType,
                    contentType: null,
                    isPersonal: true);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then: a user's own reaction opens and closes its round on submit (§8.5 rules 1, 6)
            actualVerdict.AreConditionsMet.Should().BeTrue();
            actualVerdict.ShouldAutoApprove.Should().BeTrue();
        }

        /// <summary>
        /// The personal row governs personal associations ONLY. An editorial association — one
        /// with no UserId — takes the entity-type default, however the personal row is set.
        /// </summary>
        [Fact]
        public async Task ShouldNotResolveThePersonalRowForAnEditorialEntityAsync()
        {
            // given
            string entityType = GetRandomString();

            ApprovalPolicy entityTypeDefault = CreateRandomApprovalPolicy(
                entityType: entityType,
                requiredNumberOfApprovals: 2);

            ApprovalPolicy personalRow = CreateRandomApprovalPolicy(
                entityType: entityType,
                isPersonal: true,
                requireApprovals: false);

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateRandomApprovalConditionsRequest(
                    candidatePolicies: new List<ApprovalPolicy>
                    {
                        personalRow,
                        entityTypeDefault,
                    },
                    entityType: entityType,
                    contentType: null,
                    isPersonal: false);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.RequiredNumberOfApprovals.Should().Be(2);
        }

        /// <summary>
        /// An entity with NO personality — anything that is not an association — never
        /// matches a personality row, personal or editorial, even one for its own entity type.
        /// </summary>
        [Fact]
        public async Task ShouldNotResolveAPersonalityRowForAnEntityWithNoPersonalityAsync()
        {
            // given
            string entityType = GetRandomString();

            ApprovalPolicy entityTypeDefault = CreateRandomApprovalPolicy(
                entityType: entityType,
                requiredNumberOfApprovals: 3);

            ApprovalPolicy editorialRow = CreateRandomApprovalPolicy(
                entityType: entityType,
                isPersonal: false,
                requiredNumberOfApprovals: 8);

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateRandomApprovalConditionsRequest(
                    candidatePolicies: new List<ApprovalPolicy>
                    {
                        editorialRow,
                        entityTypeDefault,
                    },
                    entityType: entityType,
                    contentType: null,
                    isPersonal: null);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.RequiredNumberOfApprovals.Should().Be(3);
        }

        /// <summary>
        /// A personal association whose entity type has no personal row still resolves through
        /// the entity-type default and then the global one — the tier is skipped, not the whole
        /// hierarchy.
        /// </summary>
        [Fact]
        public async Task ShouldFallPastAnAbsentPersonalityRowToTheEntityTypeDefaultAsync()
        {
            // given
            string entityType = GetRandomString();

            ApprovalPolicy entityTypeDefault = CreateRandomApprovalPolicy(
                entityType: entityType,
                requiredNumberOfApprovals: 4);

            ApprovalConditionsRequest approvalConditionsRequest =
                CreateRandomApprovalConditionsRequest(
                    candidatePolicies: new List<ApprovalPolicy> { entityTypeDefault },
                    entityType: entityType,
                    contentType: null,
                    isPersonal: true);

            // when
            ApprovalConditionsVerdict actualVerdict =
                await this.accessService.EvaluateApprovalConditionsAsync(
                    approvalConditionsRequest);

            // then
            actualVerdict.RequiredNumberOfApprovals.Should().Be(4);
        }
    }
}
