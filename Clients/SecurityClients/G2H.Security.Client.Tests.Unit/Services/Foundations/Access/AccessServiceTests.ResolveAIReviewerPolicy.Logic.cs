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
        // §8.6.2. The tiering itself is exhaustively covered against EvaluateApprovalConditionsAsync
        // above — both ride the same private ResolvePolicy — so these pin only that this method
        // reports IsOffered off the same resolved row. The second composed answer,
        // IsAutomaticallyRequested, is pinned on its own below.
        [Fact]
        public async Task ShouldReportTheResolvedIsAIReviewerOfferedAsync()
        {
            // given
            string entityType = GetRandomString();
            string contentType = GetRandomString();

            ApprovalPolicy contentTypeRow = CreateRandomApprovalPolicy(
                entityType: entityType,
                contentType: contentType,
                isAIReviewerOffered: true);

            var resolveAIReviewerPolicyRequest = new ResolveAIReviewerPolicyRequest
            {
                CandidatePolicies = new List<ApprovalPolicy> { contentTypeRow },
                EntityType = entityType,
                ContentType = contentType,
                IsPersonal = null,
            };

            // when
            AIReviewerPolicyVerdict actualVerdict =
                await this.accessService.ResolveAIReviewerPolicyAsync(
                    resolveAIReviewerPolicyRequest);

            // then
            actualVerdict.IsOffered.Should().BeTrue();
        }

        // §8.4 rule 2: fail-closed. No row at all resolves to the system default, which never
        // offers Berean and never asks it automatically either — a fallback that granted an
        // automatic action in an environment the seed has not reached is exactly what the
        // fail-closed reading forbids (§8.6.2.1, criterion 7).
        [Fact]
        public async Task ShouldReportNotOfferedUnderTheFailClosedSystemDefaultAsync()
        {
            // given
            var resolveAIReviewerPolicyRequest = new ResolveAIReviewerPolicyRequest
            {
                CandidatePolicies = new List<ApprovalPolicy>(),
                EntityType = GetRandomString(),
                ContentType = null,
                IsPersonal = null,
            };

            // when
            AIReviewerPolicyVerdict actualVerdict =
                await this.accessService.ResolveAIReviewerPolicyAsync(
                    resolveAIReviewerPolicyRequest);

            // then
            actualVerdict.IsOffered.Should().BeFalse();
            actualVerdict.IsAutomaticallyRequested.Should().BeFalse();
        }

        // §8.6.2 / §8.6.2.1, criterion 6: the composition is the whole point of this verdict, so
        // all four combinations are pinned rather than only the "both on" case. Nothing above
        // this decision function re-derives IsAutomaticallyRequested on its own (§8.6.1 rule 4).
        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, false)]
        public async Task ShouldComposeIsAutomaticallyRequestedFromBothSwitchesAsync(
            bool isAIReviewerOffered,
            bool isAIReviewerAutomaticallyRequested,
            bool expectedIsAutomaticallyRequested)
        {
            // given
            string entityType = GetRandomString();
            string contentType = GetRandomString();

            ApprovalPolicy contentTypeRow = CreateRandomApprovalPolicy(
                entityType: entityType,
                contentType: contentType,
                isAIReviewerOffered: isAIReviewerOffered,
                isAIReviewerAutomaticallyRequested: isAIReviewerAutomaticallyRequested);

            var resolveAIReviewerPolicyRequest = new ResolveAIReviewerPolicyRequest
            {
                CandidatePolicies = new List<ApprovalPolicy> { contentTypeRow },
                EntityType = entityType,
                ContentType = contentType,
                IsPersonal = null,
            };

            // when
            AIReviewerPolicyVerdict actualVerdict =
                await this.accessService.ResolveAIReviewerPolicyAsync(
                    resolveAIReviewerPolicyRequest);

            // then
            actualVerdict.IsAutomaticallyRequested.Should().Be(expectedIsAutomaticallyRequested);
        }

        // The content-type row wins even when a broader row offers Berean — the same
        // most-specific-wins rule §8.4 applies to every other field.
        [Fact]
        public async Task ShouldResolveTheContentTypeRowOverAWiderOfferingRowAsync()
        {
            // given
            string entityType = GetRandomString();
            string contentType = GetRandomString();

            ApprovalPolicy entityTypeDefault = CreateRandomApprovalPolicy(
                entityType: entityType,
                contentType: null,
                isAIReviewerOffered: true);

            ApprovalPolicy narrowContentTypeRow = CreateRandomApprovalPolicy(
                entityType: entityType,
                contentType: contentType,
                isAIReviewerOffered: false);

            var resolveAIReviewerPolicyRequest = new ResolveAIReviewerPolicyRequest
            {
                CandidatePolicies = new List<ApprovalPolicy>
                {
                    entityTypeDefault,
                    narrowContentTypeRow,
                },
                EntityType = entityType,
                ContentType = contentType,
                IsPersonal = null,
            };

            // when
            AIReviewerPolicyVerdict actualVerdict =
                await this.accessService.ResolveAIReviewerPolicyAsync(
                    resolveAIReviewerPolicyRequest);

            // then
            actualVerdict.IsOffered.Should().BeFalse();
        }
    }
}
