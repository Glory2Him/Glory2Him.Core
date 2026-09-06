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
using Xunit;

namespace G2H.Security.Client.Tests.Unit.Services.Foundations.Access
{
    public partial class AccessServiceTests
    {
        [Fact]
        public async Task ShouldRefuseRecordingACommentWhenTheActorIsNotAuthenticatedAsync()
        {
            // given
            AccessActor unauthenticatedActor =
                CreateRandomAccessActor(isAuthenticated: false);

            RecordApprovalCommentRequest recordApprovalCommentRequest =
                CreateRandomRecordApprovalCommentRequest(actor: unauthenticatedActor);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalCommentAsync(recordApprovalCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotAuthenticated);
        }

        [Fact]
        public async Task ShouldRefuseRecordingACommentWhenTheParentApprovalIsDeletedAsync()
        {
            // given: the foreign key still resolves — deletion is a flag, not a row removal —
            // so this is the half of "existing, non-deleted parent" the key cannot express
            RecordApprovalCommentRequest recordApprovalCommentRequest =
                CreateRandomRecordApprovalCommentRequest(isParentApprovalDeleted: true);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalCommentAsync(recordApprovalCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.ParentApprovalUnavailable);
        }

        [Fact]
        public async Task ShouldReportTheDeletedParentAheadOfAClosedRoundAsync()
        {
            // given: a taken-down approval is also a closed one, and the more specific fact is
            // the one worth reporting
            RecordApprovalCommentRequest recordApprovalCommentRequest =
                CreateRandomRecordApprovalCommentRequest(
                approvalState: ApprovalState.Approved,
                isParentApprovalDeleted: true);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalCommentAsync(recordApprovalCommentRequest);

            // then
            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.ParentApprovalUnavailable);
        }

        [Theory]
        [InlineData(ApprovalState.Draft)]
        [InlineData(ApprovalState.Approved)]
        [InlineData(ApprovalState.Rejected)]
        public async Task ShouldRefuseRecordingACommentWhenTheRoundIsNotOpenAsync(
            ApprovalState closedState)
        {
            // given
            RecordApprovalCommentRequest recordApprovalCommentRequest =
                CreateRandomRecordApprovalCommentRequest(approvalState: closedState);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalCommentAsync(recordApprovalCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.ApprovalNotOpenForComment);
        }

        [Fact]
        public async Task ShouldPermitRecordingACommentOnAnOpenApprovalWithoutAnyTierAsync()
        {
            // given: commenting is not reviewing — an actor holding no role at all may speak
            RecordApprovalCommentRequest recordApprovalCommentRequest =
                CreateRandomRecordApprovalCommentRequest(
                actor: CreateRandomAccessActor(roles: new List<string>()));

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalCommentAsync(recordApprovalCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        /// <summary>
        /// AN ASK MAY NOT BE BORN SETTLED. Creating a resolved question IS resolving one, a moment
        /// earlier and through a gate that never asks who may resolve — so without this any caller
        /// could file a question that holds nothing shut and walk past
        /// <c>RequireReviewCommentResolutionBeforeApprovals</c> without answering to the publisher
        /// tier the resolve operation gates on (§14.7 rule 5).
        /// </summary>
        [Fact]
        public async Task ShouldRefuseRecordingAnAskThatIsAlreadySettledAsync()
        {
            // given
            RecordApprovalCommentRequest recordApprovalCommentRequest =
                CreateRandomRecordApprovalCommentRequest(isAsk: true, isSettled: true);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalCommentAsync(recordApprovalCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.SettledAskNotPermitted);
        }

        /// <summary>
        /// The other three pairings stand. §7.8 rule 1's reasoning is preserved exactly where it
        /// still applies: a REMARK may be born either way, because refusing an outstanding one
        /// would be the "impossible to leave a remark without holding the approval shut" outcome
        /// that rule exists to prevent. And a remark born outstanding is fail-CLOSED — it blocks
        /// where nothing had to, which grants nobody anything.
        /// </summary>
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public async Task ShouldPermitEveryOtherBirthPairingAsync(bool isAsk, bool isSettled)
        {
            // given
            RecordApprovalCommentRequest recordApprovalCommentRequest =
                CreateRandomRecordApprovalCommentRequest(isAsk: isAsk, isSettled: isSettled);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalCommentAsync(recordApprovalCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        /// <summary>
        /// The pairing is asked LAST, after the actor and the round. A caller who may not comment
        /// at all must not learn anything about the thread from the shape of their own payload —
        /// and a closed round refuses every comment, settled ask included, for the closed-round
        /// reason (§14.5).
        /// </summary>
        [Fact]
        public async Task ShouldReportTheClosedRoundRatherThanTheSettledAskAsync()
        {
            // given
            RecordApprovalCommentRequest recordApprovalCommentRequest =
                CreateRandomRecordApprovalCommentRequest(
                approvalState: ApprovalState.Approved,
                isAsk: true,
                isSettled: true);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalCommentAsync(recordApprovalCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.ApprovalNotOpenForComment);
        }

        [Fact]
        public async Task ShouldRefuseAmendingACommentWhenTheActorIsNotAuthenticatedAsync()
        {
            // given
            string authorId = GetRandomString();

            AmendApprovalCommentRequest amendApprovalCommentRequest =
                CreateRandomAmendApprovalCommentRequest(
                actor: CreateRandomAccessActor(userId: authorId, isAuthenticated: false),
                commentCreatedBy: authorId);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayAmendApprovalCommentAsync(amendApprovalCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotAuthenticated);
        }

        [Fact]
        public async Task ShouldRefuseAmendingACommentTheActorDidNotWriteAsync()
        {
            // given
            AmendApprovalCommentRequest amendApprovalCommentRequest =
                CreateRandomAmendApprovalCommentRequest(
                actor: CreateRandomAccessActor(userId: GetRandomString()),
                commentCreatedBy: GetRandomString());

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayAmendApprovalCommentAsync(amendApprovalCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotApprovalCommentAuthor);
        }

        [Theory]
        [InlineData(RoleNames.Administrators)]
        [InlineData(RoleNames.Publishers)]
        [InlineData(RoleNames.Reviewers)]
        public async Task ShouldRefuseAmendingAnotherPersonsCommentWhateverTheRoleAsync(
            string role)
        {
            // given: no tier widens the amend gate. An administrator who needs past an unresolved
            // comment resolves it or bypasses the block; neither rewrites another's words.
            AmendApprovalCommentRequest amendApprovalCommentRequest =
                CreateRandomAmendApprovalCommentRequest(
                actor: CreateRandomAccessActor(roles: new List<string> { role }),
                commentCreatedBy: GetRandomString());

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayAmendApprovalCommentAsync(amendApprovalCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotApprovalCommentAuthor);
        }

        [Theory]
        [InlineData(ApprovalState.Draft)]
        [InlineData(ApprovalState.Approved)]
        [InlineData(ApprovalState.Rejected)]
        public async Task ShouldRefuseAmendingACommentOnceTheRoundHasClosedAsync(
            ApprovalState closedState)
        {
            // given: what was said stands as recorded
            string authorId = GetRandomString();

            AmendApprovalCommentRequest amendApprovalCommentRequest =
                CreateRandomAmendApprovalCommentRequest(
                actor: CreateRandomAccessActor(userId: authorId),
                commentCreatedBy: authorId,
                approvalState: closedState);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayAmendApprovalCommentAsync(amendApprovalCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.ApprovalNotOpenForComment);
        }

        [Fact]
        public async Task ShouldPermitTheAuthorAmendingTheirOwnCommentWhileTheRoundIsOpenAsync()
        {
            // given
            string authorId = GetRandomString();

            AmendApprovalCommentRequest amendApprovalCommentRequest =
                CreateRandomAmendApprovalCommentRequest(
                actor: CreateRandomAccessActor(userId: authorId, roles: new List<string>()),
                commentCreatedBy: authorId);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayAmendApprovalCommentAsync(amendApprovalCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        [Fact]
        public async Task ShouldRefuseAmendingACommentWhenTheParentApprovalIsDeletedAsync()
        {
            // given: a taken-down approval stops its comments being editable with it, or a
            // thread goes on living under an approval that no longer exists to anyone
            string authorId = GetRandomString();

            AmendApprovalCommentRequest amendApprovalCommentRequest =
                CreateRandomAmendApprovalCommentRequest(
                    actor: CreateRandomAccessActor(userId: authorId),
                    commentCreatedBy: authorId,
                    isParentApprovalDeleted: true);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayAmendApprovalCommentAsync(
                    amendApprovalCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.ParentApprovalUnavailable);
        }

        [Fact]
        public async Task ShouldRefuseResolvingACommentWhenTheParentApprovalIsDeletedAsync()
        {
            // given
            ResolveApprovalCommentRequest resolveApprovalCommentRequest =
                CreateRandomResolveApprovalCommentRequest(
                    actor: CreateRandomAccessActor(
                        roles: new List<string> { RoleNames.Administrators }),
                    commentCreatedBy: GetRandomString(),
                    isParentApprovalDeleted: true);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayResolveApprovalCommentAsync(
                    resolveApprovalCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.ParentApprovalUnavailable);
        }

        [Fact]
        public async Task ShouldRefuseTheAuthorResolvingOnceTheRoundHasClosedAsync()
        {
            // given: the closed-round bar is not an administrator-only rule — narrowing it to admins
            // would otherwise survive the suite
            string authorId = GetRandomString();

            ResolveApprovalCommentRequest resolveApprovalCommentRequest =
                CreateRandomResolveApprovalCommentRequest(
                    actor: CreateRandomAccessActor(userId: authorId),
                    commentCreatedBy: authorId,
                    approvalState: ApprovalState.Approved);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayResolveApprovalCommentAsync(
                    resolveApprovalCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.ApprovalNotOpenForComment);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ShouldRefuseEveryCommentGateWhenTheActorUserIdIsBlankAsync(
            string? invalidUserId)
        {
            // given: authenticated but carrying no resolvable id. Blank must never match blank,
            // or "is this the author?" answers yes for every row whose author was never stamped.
            var actorWithoutUserId = new AccessActor
            {
                UserId = invalidUserId!,
                Roles = new List<string> { RoleNames.Administrators },
                IsAuthenticated = true,
            };

            // when
            AccessVerdict recordVerdict =
                await this.accessService.MayRecordApprovalCommentAsync(
                    CreateRandomRecordApprovalCommentRequest(actor: actorWithoutUserId));

            AccessVerdict amendVerdict =
                await this.accessService.MayAmendApprovalCommentAsync(
                    CreateRandomAmendApprovalCommentRequest(
                        actor: actorWithoutUserId,
                        commentCreatedBy: invalidUserId!));

            AccessVerdict resolveVerdict =
                await this.accessService.MayResolveApprovalCommentAsync(
                    CreateRandomResolveApprovalCommentRequest(
                        actor: actorWithoutUserId,
                        commentCreatedBy: invalidUserId!));

            // then
            recordVerdict.DenialReason.Should().Be(AccessDenialReason.NotAuthenticated);
            amendVerdict.DenialReason.Should().Be(AccessDenialReason.NotAuthenticated);
            resolveVerdict.DenialReason.Should().Be(AccessDenialReason.NotAuthenticated);
        }

        [Fact]
        public async Task ShouldRefuseResolvingACommentWhenTheActorIsNotAuthenticatedAsync()
        {
            // given
            string authorId = GetRandomString();

            ResolveApprovalCommentRequest resolveApprovalCommentRequest =
                CreateRandomResolveApprovalCommentRequest(
                actor: CreateRandomAccessActor(userId: authorId, isAuthenticated: false),
                commentCreatedBy: authorId);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayResolveApprovalCommentAsync(resolveApprovalCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotAuthenticated);
        }

        [Fact]
        public async Task ShouldRefuseResolvingAnotherPersonsCommentOnTheReviewTierAsync()
        {
            // given: the gate widens to the PUBLISHER tier and stops there. A reviewer is never
            // held by RequireReviewCommentResolutionBeforeApprovals — they cast a verdict, they
            // do not decide the round — so settling somebody else's ask is not theirs to do. One
            // who wants to answer an outstanding comment writes a comment of their own.
            ResolveApprovalCommentRequest resolveApprovalCommentRequest =
                CreateRandomResolveApprovalCommentRequest(
                actor: CreateRandomAccessActor(
                    roles: new List<string> { RoleNames.Reviewers }),
                commentCreatedBy: GetRandomString());

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayResolveApprovalCommentAsync(resolveApprovalCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotApprovalCommentAuthor);
        }

        [Fact]
        public async Task ShouldRefuseResolvingAnotherPersonsCommentOnAnotherTypesPublishTierAsync()
        {
            // given: a publisher of ANOTHER content type. The narrow tier widens into the coarse
            // one, never sideways (§18.6 rule 4), so this must not reach a devotional's thread.
            RoleSubject subject = CreateRandomRoleSubject(
                entityType: "ContentItem", contentType: "Devotional");

            ResolveApprovalCommentRequest resolveApprovalCommentRequest =
                CreateRandomResolveApprovalCommentRequest(
                actor: CreateRandomAccessActor(
                    roles: new List<string>
                    {
                        RoleNames.PublishersFor("ContentItem", "Quote")
                    }),
                roleSubjects: new List<RoleSubject> { subject },
                commentCreatedBy: GetRandomString());

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayResolveApprovalCommentAsync(resolveApprovalCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotApprovalCommentAuthor);
        }

        [Fact]
        public async Task ShouldPermitAnAdminResolvingAnotherPersonsCommentAsync()
        {
            // given: resolving records that a comment is settled, which changes no words — the
            // one comment operation somebody other than the author may perform on the row.
            // Administrators sits inside the publisher tier, so the §14.7 rule 5 route survives
            // the widening rather than being replaced by it.
            ResolveApprovalCommentRequest resolveApprovalCommentRequest =
                CreateRandomResolveApprovalCommentRequest(
                actor: CreateRandomAccessActor(
                    roles: new List<string> { RoleNames.Administrators }),
                commentCreatedBy: GetRandomString());

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayResolveApprovalCommentAsync(resolveApprovalCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        [Fact]
        public async Task ShouldPermitAGlobalPublisherResolvingAnotherPersonsCommentAsync()
        {
            // given: an outstanding comment holds the APPROVAL shut, and the people that block
            // stops are exactly the people who decide it
            ResolveApprovalCommentRequest resolveApprovalCommentRequest =
                CreateRandomResolveApprovalCommentRequest(
                actor: CreateRandomAccessActor(
                    roles: new List<string> { RoleNames.Publishers }),
                commentCreatedBy: GetRandomString());

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayResolveApprovalCommentAsync(resolveApprovalCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ShouldPermitAScopedPublisherResolvingAnotherPersonsCommentAsync(
            bool isNarrowRole)
        {
            // given: both spellings of the scoped tier reach the same subject —
            // ContentItem-Devotional-Publishers ⊂ ContentItem-Publishers (§18.6 rule 4)
            RoleSubject subject = CreateRandomRoleSubject(
                entityType: "ContentItem", contentType: "Devotional");

            string scopedRole = isNarrowRole
                ? RoleNames.PublishersFor("ContentItem", "Devotional")
                : RoleNames.PublishersFor("ContentItem");

            ResolveApprovalCommentRequest resolveApprovalCommentRequest =
                CreateRandomResolveApprovalCommentRequest(
                actor: CreateRandomAccessActor(roles: new List<string> { scopedRole }),
                roleSubjects: new List<RoleSubject> { subject },
                commentCreatedBy: GetRandomString());

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayResolveApprovalCommentAsync(resolveApprovalCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        [Fact]
        public async Task ShouldRefuseResolvingWhenTheActorIsSanctionedOnTheSubjectAsync()
        {
            // given: the veto reaches IsResolved at last. §18.6 rule 3 records that a scoped block
            // does not reach the comment thread and singles out THIS field as where that reasoning
            // strains, because settling a comment clears a §8.5 gate. A publisher sanctioned on
            // the type may no longer move it.
            RoleSubject subject = CreateRandomRoleSubject(
                entityType: "ContentItem", contentType: "Devotional");

            ResolveApprovalCommentRequest resolveApprovalCommentRequest =
                CreateRandomResolveApprovalCommentRequest(
                actor: CreateRandomAccessActor(
                    roles: new List<string>
                    {
                        RoleNames.PublishersFor("ContentItem"),
                        RoleNames.ReadOnlyFor("ContentItem", "Devotional")
                    }),
                roleSubjects: new List<RoleSubject> { subject },
                commentCreatedBy: GetRandomString());

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayResolveApprovalCommentAsync(resolveApprovalCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.BlockedByReadOnlyRole);
        }

        [Fact]
        public async Task ShouldRefuseTheSanctionedAuthorResolvingTheirOwnCommentAsync()
        {
            // given: the veto is asked BEFORE the author branch, because a block covers the
            // holder's own rows and the author admit is a grant like any other (§18.6 rule 2).
            // Without that ordering a sanctioned contributor could still clear the one gate
            // holding their own submission's approval shut.
            string authorId = GetRandomString();

            RoleSubject subject = CreateRandomRoleSubject(
                entityType: "ContentItem", contentType: "Devotional");

            ResolveApprovalCommentRequest resolveApprovalCommentRequest =
                CreateRandomResolveApprovalCommentRequest(
                actor: CreateRandomAccessActor(
                    userId: authorId,
                    roles: new List<string> { RoleNames.ReadOnly }),
                roleSubjects: new List<RoleSubject> { subject },
                commentCreatedBy: authorId);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayResolveApprovalCommentAsync(resolveApprovalCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.BlockedByReadOnlyRole);
        }

        [Fact]
        public async Task ShouldPermitTheAuthorResolvingTheirOwnCommentWithoutAnyRoleAsync()
        {
            // given
            string authorId = GetRandomString();

            ResolveApprovalCommentRequest resolveApprovalCommentRequest =
                CreateRandomResolveApprovalCommentRequest(
                actor: CreateRandomAccessActor(userId: authorId, roles: new List<string>()),
                commentCreatedBy: authorId);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayResolveApprovalCommentAsync(resolveApprovalCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        [Theory]
        [InlineData(ApprovalState.Draft)]
        [InlineData(ApprovalState.Approved)]
        [InlineData(ApprovalState.Rejected)]
        public async Task ShouldRefuseAnAdminResolvingOnceTheRoundHasClosedAsync(
            ApprovalState closedState)
        {
            // given: the block this flag feeds has already been evaluated for the last time
            ResolveApprovalCommentRequest resolveApprovalCommentRequest =
                CreateRandomResolveApprovalCommentRequest(
                actor: CreateRandomAccessActor(
                    roles: new List<string> { RoleNames.Administrators }),
                commentCreatedBy: GetRandomString(),
                approvalState: closedState);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayResolveApprovalCommentAsync(resolveApprovalCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.ApprovalNotOpenForComment);
        }
    }
}
