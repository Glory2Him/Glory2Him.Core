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
using G2H.Security.Client.Models.Securities;

namespace G2H.Security.Client.Tests.Unit.Services.Foundations.Access
{
    public partial class AccessServiceTests
    {
        [Fact]
        public async Task ShouldRefuseDecidingAnApprovalWhenTheActorIsNotAuthenticatedAsync()
        {
            // given
            AccessActor unauthenticatedActor = CreateRandomAccessActor(
                roles: new List<string> { RoleNames.Admin },
                isAuthenticated: false);

            DecideApprovalRequest decideApprovalRequest =
                CreateRandomDecideApprovalRequest(actor: unauthenticatedActor);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotAuthenticated);
        }

        // HR-3. A reviewer is a different job, not a weaker one, and gets its own reason.
        [Fact]
        public async Task ShouldRefuseDecidingAnApprovalWhenTheActorOnlyHoldsAReviewTierRoleAsync()
        {
            // given
            AccessActor reviewerOnly = CreateRandomAccessActor(
                roles: new List<string> { RoleNames.Reviewer });

            DecideApprovalRequest decideApprovalRequest =
                CreateRandomDecideApprovalRequest(actor: reviewerOnly);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.ReviewerMayNotDecide);
        }

        [Fact]
        public async Task ShouldRefuseDecidingAnApprovalWhenTheActorHoldsNoTierAtAllAsync()
        {
            // given
            AccessActor readOnlyActor = CreateRandomAccessActor(
                roles: new List<string> { RoleNames.ReadOnly });

            DecideApprovalRequest decideApprovalRequest =
                CreateRandomDecideApprovalRequest(actor: readOnlyActor);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotInPublisherTier);
        }

        [Theory]
        [InlineData(RoleNames.Publisher)]
        [InlineData(RoleNames.Admin)]
        public async Task ShouldPermitDecidingAnApprovalForEachGlobalPublisherTierRoleAsync(
            string globalRole)
        {
            // given
            AccessActor actor = CreateRandomAccessActor(
                roles: new List<string> { globalRole });

            DecideApprovalRequest decideApprovalRequest =
                CreateRandomDecideApprovalRequest(actor: actor);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);

            actualVerdict.Explanation.Should()
                .Be("Actor may approve this entity (HR-4 route 1).");
        }

        [Fact]
        public async Task ShouldPermitDecidingAnApprovalForAnEntityTypeScopedPublisherRoleAsync()
        {
            // given
            string entityType = GetRandomString();

            ApprovalPolicy approvalPolicy = CreateRandomApprovalPolicy(
                entityType: entityType,
                requireApprovals: false);

            AccessActor scopedPublisher = CreateRandomAccessActor(
                roles: new List<string> { RoleNames.PublisherFor(entityType) });

            DecideApprovalRequest decideApprovalRequest = CreateRandomDecideApprovalRequest(
                actor: scopedPublisher,
                policy: approvalPolicy,

                roleSubjects: new List<RoleSubject>
                {
                    CreateRandomRoleSubject(entityType: entityType),
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        [Theory]
        [InlineData(ApprovalState.Draft)]
        [InlineData(ApprovalState.Approved)]
        [InlineData(ApprovalState.Rejected)]
        public async Task ShouldRefuseDecidingAnApprovalWhenTheApprovalIsNotSubmittedAsync(
            ApprovalState closedApprovalState)
        {
            // given
            DecideApprovalRequest decideApprovalRequest =
                CreateRandomDecideApprovalRequest(approvalState: closedApprovalState);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.ApprovalNotOpenForReview);
        }

        // §8.6 regardless-rule 1. No role and no setting relaxes it: a publisher who filed a
        // review has spent their vote on this round.
        [Fact]
        public async Task ShouldRefuseDecidingAnApprovalWhenTheActorHoldsAnActiveReviewEvenAsAdminAsync()
        {
            // given
            string actorId = GetRandomString();

            AccessActor adminHoldingAReview = CreateRandomAccessActor(
                userId: actorId,
                roles: new List<string> { RoleNames.Admin });

            ApprovalPolicy permissiveApprovalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: false,
                allowSelfApproval: true);

            DecideApprovalRequest decideApprovalRequest = CreateRandomDecideApprovalRequest(
                actor: adminHoldingAReview,
                policy: permissiveApprovalPolicy,

                reviews: new List<ReviewRecord>
                {
                    CreateRandomReviewRecord(reviewerId: actorId),
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.ReviewerOnThisRoundMayNotDecide);
        }

        // HR-2.
        [Fact]
        public async Task ShouldRefuseTheAuthorApprovingTheirOwnEntityWhenSelfApprovalIsNotAllowedAsync()
        {
            // given
            string authorId = GetRandomString();

            AccessActor author = CreateRandomAccessActor(
                userId: authorId,
                roles: new List<string> { RoleNames.Publisher });

            ApprovalPolicy approvalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: false,
                allowSelfApproval: false);

            DecideApprovalRequest decideApprovalRequest = CreateRandomDecideApprovalRequest(
                actor: author,
                policy: approvalPolicy,
                entityCreatedBy: authorId);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.SelfApprovalNotPermitted);
        }

        [Fact]
        public async Task ShouldPermitTheAuthorApprovingTheirOwnEntityWhenSelfApprovalIsAllowedAsync()
        {
            // given
            string authorId = GetRandomString();

            AccessActor author = CreateRandomAccessActor(
                userId: authorId,
                roles: new List<string> { RoleNames.Publisher });

            ApprovalPolicy approvalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: false,
                allowSelfApproval: true);

            DecideApprovalRequest decideApprovalRequest = CreateRandomDecideApprovalRequest(
                actor: author,
                policy: approvalPolicy,
                entityCreatedBy: authorId);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        // §9.7.5. A rejection withholds approval rather than granting it, so neither the
        // threshold nor the bypass lock has anything to say about it.
        [Fact]
        public async Task ShouldPermitRejectingWhenTheConditionsAreNotMetAndBypassingIsLockedAsync()
        {
            // given
            ApprovalPolicy lockedDownApprovalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: true,
                requiredNumberOfApprovals: 5,
                blockOnReject: true,
                blockOnZeroApprovalScore: true,
                requireReviewCommentResolutionBeforeApprovals: true,
                doNotAllowBypassingSettings: true);

            DecideApprovalRequest decideApprovalRequest = CreateRandomDecideApprovalRequest(
                decision: ApprovalDecision.Reject,
                policy: lockedDownApprovalPolicy,
                confidenceScore: 0m,

                comments: new List<CommentRecord>
                {
                    CreateRandomCommentRecord(isResolved: false),
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
            actualVerdict.Explanation.Should().Be("Actor may reject this approval.");
        }

        [Fact]
        public async Task ShouldPermitTheAuthorRejectingTheirOwnEntityWhenSelfApprovalIsNotAllowedAsync()
        {
            // given
            string authorId = GetRandomString();

            AccessActor author = CreateRandomAccessActor(
                userId: authorId,
                roles: new List<string> { RoleNames.Publisher });

            ApprovalPolicy approvalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: false,
                allowSelfApproval: false);

            DecideApprovalRequest decideApprovalRequest = CreateRandomDecideApprovalRequest(
                actor: author,
                decision: ApprovalDecision.Reject,
                policy: approvalPolicy,
                entityCreatedBy: authorId);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.Explanation.Should().Be("Actor may reject this approval.");
        }

        [Fact]
        public async Task ShouldPermitApprovingByBypassWhenAReasonIsRecordedAsync()
        {
            // given
            ApprovalPolicy unmetApprovalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: true,
                requiredNumberOfApprovals: 5,
                doNotAllowBypassingSettings: false);

            DecideApprovalRequest decideApprovalRequest = CreateRandomDecideApprovalRequest(
                policy: unmetApprovalPolicy,
                isBypassRequested: true,
                bypassReason: GetRandomString());

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);

            actualVerdict.Explanation.Should()
                .Be("Actor may approve this entity by bypass (HR-4 route 3).");
        }

        [Fact]
        public async Task ShouldRefuseABypassWhenThePolicyClosesTheBypassRouteAsync()
        {
            // given
            ApprovalPolicy bypassLockedApprovalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: true,
                requiredNumberOfApprovals: 5,
                doNotAllowBypassingSettings: true);

            DecideApprovalRequest decideApprovalRequest = CreateRandomDecideApprovalRequest(
                policy: bypassLockedApprovalPolicy,
                isBypassRequested: true,
                bypassReason: GetRandomString());

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.BypassNotPermitted);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ShouldRefuseABypassWhenNoReasonIsRecordedAsync(string? invalidBypassReason)
        {
            // given
            ApprovalPolicy unmetApprovalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: true,
                requiredNumberOfApprovals: 5,
                doNotAllowBypassingSettings: false);

            DecideApprovalRequest decideApprovalRequest = CreateRandomDecideApprovalRequest(
                policy: unmetApprovalPolicy,
                isBypassRequested: true,
                bypassReason: invalidBypassReason);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.BypassReasonRequired);
        }

        // The refusal carries the SPECIFIC §8.5 block reason, not a generic
        // ApprovalConditionsNotMet, so the caller can tell a threshold apart from a blocker.
        [Fact]
        public async Task ShouldRefuseApprovingWithTheThresholdReasonWhenNoBypassWasRequestedAsync()
        {
            // given
            ApprovalPolicy unmetApprovalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: true,
                requiredNumberOfApprovals: 2);

            DecideApprovalRequest decideApprovalRequest = CreateRandomDecideApprovalRequest(
                policy: unmetApprovalPolicy,
                isBypassRequested: false);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.ApprovalThresholdNotMet);

            actualVerdict.DenialReason.Should()
                .NotBe(AccessDenialReason.ApprovalConditionsNotMet);

            actualVerdict.Explanation.Should()
                .Be("The approval conditions are not met and no bypass was requested. "
                    + "0 of 2 required approvals recorded.");
        }

        [Fact]
        public async Task ShouldRefuseApprovingWithTheUnresolvedCommentReasonWhenNoBypassWasRequestedAsync()
        {
            // given
            ApprovalPolicy commentGatedApprovalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: false,
                requireReviewCommentResolutionBeforeApprovals: true);

            DecideApprovalRequest decideApprovalRequest = CreateRandomDecideApprovalRequest(
                policy: commentGatedApprovalPolicy,
                isBypassRequested: false,

                comments: new List<CommentRecord>
                {
                    CreateRandomCommentRecord(isResolved: false),
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.BlockedByUnresolvedComment);

            actualVerdict.Explanation.Should()
                .Be("The approval conditions are not met and no bypass was requested. "
                    + "An approval comment is still unresolved.");
        }
    }
}
