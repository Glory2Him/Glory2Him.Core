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
                roles: new List<string> { RoleNames.Administrators },
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
                roles: new List<string> { RoleNames.Reviewers });

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
        [InlineData(RoleNames.Publishers)]
        [InlineData(RoleNames.Administrators)]
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
                roles: new List<string> { RoleNames.PublishersFor(entityType) });

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
                roles: new List<string> { RoleNames.Administrators });

            ApprovalPolicy permissiveApprovalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: false,
                allowSelfApproval: true);

            DecideApprovalRequest decideApprovalRequest = CreateRandomDecideApprovalRequest(
                actor: adminHoldingAReview,
                policy: permissiveApprovalPolicy,

                reviews: new List<ReviewRecord>
                {
                    CreateRandomReviewRecord(createdBy: actorId),
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
                roles: new List<string> { RoleNames.Publishers });

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
                roles: new List<string> { RoleNames.Publishers });

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

                comments: new List<ApprovalCommentRecord>
                {
                    CreateRandomApprovalCommentRecord(isResolved: false),
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
                roles: new List<string> { RoleNames.Publishers });

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

            // the explanation names what was waived, because a bypass over nothing and a bypass
            // over a failing threshold are different events
            actualVerdict.Explanation.Should()
                .Be("Actor may approve this entity by bypass (HR-4 route 3), waiving: "
                    + "0 of 5 required approvals recorded.");
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

                comments: new List<ApprovalCommentRecord>
                {
                    CreateRandomApprovalCommentRecord(isResolved: false),
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.BlockedByUnresolvedApprovalComment);

            actualVerdict.Explanation.Should()
                .Be("The approval conditions are not met and no bypass was requested. "
                    + "An approval comment is still unresolved.");
        }

        // ── The bypass record ────────────────────────────────────────────────────────────────
        //
        // A bypass is reported on its own two members and NEVER as a denial reason. The
        // separation is what lets a caller keep writing `if (reason != None) throw` — a second
        // success sentinel on DenialReason would turn every one of those gates into a refusal
        // of the approve it had just permitted.

        [Fact]
        public async Task ShouldReportTheBypassAsUsedOnAPermittedBypassAsync()
        {
            // given
            ApprovalPolicy unmetApprovalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: true,
                requiredNumberOfApprovals: 3,
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
            actualVerdict.IsBypassUsed.Should().BeTrue();

            // and the flag agrees with the block it names. The two members move together — the
            // flag is true exactly when there is a block to report — so pinning only the flag
            // would leave it free to be hardcoded.
            actualVerdict.BypassedBlockReason.Should()
                .Be(AccessDenialReason.ApprovalThresholdNotMet);

            // THE member that must not move. Callers gate on `reason != None`, so a bypass
            // reported as a denial reason would refuse the approve this verdict permits.
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        // Nothing was actually waived. The bypass route was taken and the approve went through,
        // but no condition had to be lifted to get there — so the record says exactly that, and
        // this is the harmless case an auditor can skip past.
        [Fact]
        public async Task ShouldReportNothingWaivedWhenABypassRanOverConditionsAlreadyMetAsync()
        {
            // given
            ApprovalPolicy metApprovalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: false,
                doNotAllowBypassingSettings: false);

            DecideApprovalRequest decideApprovalRequest = CreateRandomDecideApprovalRequest(
                policy: metApprovalPolicy,
                isBypassRequested: true,
                bypassReason: GetRandomString());

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then: the approve is permitted along the bypass route, and DenialReason stays
            // None because this is a permission
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);

            // IsBypassUsed reports whether anything was ACTUALLY waived, not which route the
            // caller took. The caller writes this flag into the column that answers "what was
            // published without meeting its conditions", so reporting true for a bypass that
            // lifted nothing would enter a false positive into the one query that record exists
            // to serve.
            actualVerdict.IsBypassUsed.Should().BeFalse();

            // and the pair stays consistent: nothing was waived, so there is no block to name
            actualVerdict.BypassedBlockReason.Should().Be(AccessDenialReason.None);

            // the explanation still says a bypass was ASKED for — the request happened and is
            // worth reporting, it just cost nothing
            actualVerdict.Explanation.Should()
                .Be("Actor may approve this entity by bypass (HR-4 route 3), though the "
                    + "conditions were already met — nothing was waived.");
        }

        // The one anybody would later go looking for: someone else looked at this and said no,
        // and it was published anyway.
        [Fact]
        public async Task ShouldReportTheStandingRejectionABypassWaivedAsync()
        {
            // given
            ApprovalPolicy rejectionBlockingApprovalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: true,
                requiredNumberOfApprovals: 1,
                blockOnReject: true,
                doNotAllowBypassingSettings: false);

            DecideApprovalRequest decideApprovalRequest = CreateRandomDecideApprovalRequest(
                policy: rejectionBlockingApprovalPolicy,
                isBypassRequested: true,
                bypassReason: GetRandomString(),

                // the threshold is MET, so the rejection is the only thing left to waive —
                // without the approving review this would report the threshold instead and the
                // assertion would pass for the wrong reason
                reviews: new List<ReviewRecord>
                {
                    CreateRandomReviewRecord(verdict: ReviewVerdict.Approved),
                    CreateRandomReviewRecord(verdict: ReviewVerdict.Rejected),
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.IsBypassUsed.Should().BeTrue();

            actualVerdict.BypassedBlockReason.Should()
                .Be(AccessDenialReason.BlockedByRejection);

            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        [Fact]
        public async Task ShouldReportTheUnresolvedCommentABypassWaivedAsync()
        {
            // given
            ApprovalPolicy commentGatedApprovalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: false,
                requireReviewCommentResolutionBeforeApprovals: true,
                doNotAllowBypassingSettings: false);

            DecideApprovalRequest decideApprovalRequest = CreateRandomDecideApprovalRequest(
                policy: commentGatedApprovalPolicy,
                isBypassRequested: true,
                bypassReason: GetRandomString(),

                comments: new List<ApprovalCommentRecord>
                {
                    CreateRandomApprovalCommentRecord(isResolved: false),
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.IsBypassUsed.Should().BeTrue();

            actualVerdict.BypassedBlockReason.Should()
                .Be(AccessDenialReason.BlockedByUnresolvedApprovalComment);

            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        [Fact]
        public async Task ShouldReportTheShortApprovalCountABypassWaivedAsync()
        {
            // given
            ApprovalPolicy unmetApprovalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: true,
                requiredNumberOfApprovals: 4,
                doNotAllowBypassingSettings: false);

            DecideApprovalRequest decideApprovalRequest = CreateRandomDecideApprovalRequest(
                policy: unmetApprovalPolicy,
                isBypassRequested: true,
                bypassReason: GetRandomString(),

                reviews: new List<ReviewRecord>
                {
                    CreateRandomReviewRecord(verdict: ReviewVerdict.Approved),
                });

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.IsBypassUsed.Should().BeTrue();

            actualVerdict.BypassedBlockReason.Should()
                .Be(AccessDenialReason.ApprovalThresholdNotMet);

            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);

            actualVerdict.Explanation.Should()
                .Be("Actor may approve this entity by bypass (HR-4 route 3), waiving: "
                    + "1 of 4 required approvals recorded.");
        }

        // An approve that met its conditions records no bypass. Without this the two members
        // could be hard-wired true and every bypass assertion above would still pass, while the
        // audit trail claimed every approval in the system had waived something.
        [Fact]
        public async Task ShouldNotReportABypassOnAnOrdinaryPermitAsync()
        {
            // given
            ApprovalPolicy metApprovalPolicy = CreateRandomApprovalPolicy(
                requireApprovals: false);

            DecideApprovalRequest decideApprovalRequest = CreateRandomDecideApprovalRequest(
                policy: metApprovalPolicy,
                isBypassRequested: false);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayDecideApprovalAsync(decideApprovalRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.IsBypassUsed.Should().BeFalse();
            actualVerdict.BypassedBlockReason.Should().Be(AccessDenialReason.None);
        }

        // A refusal waived nothing — it is the block, not a record of one being lifted. The
        // block belongs on DenialReason and nowhere else, or a caller reading the audit trail
        // would find refused approvals filed alongside genuine bypasses.
        [Fact]
        public async Task ShouldNotReportABypassOnARefusalAsync()
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

            actualVerdict.IsBypassUsed.Should().BeFalse();
            actualVerdict.BypassedBlockReason.Should().Be(AccessDenialReason.None);
        }
    }
}
