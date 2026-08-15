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
        [InlineData(RoleNames.Admin)]
        [InlineData(RoleNames.Publisher)]
        [InlineData(RoleNames.Reviewer)]
        public async Task ShouldRefuseAmendingAnotherPersonsCommentWhateverTheRoleAsync(
            string role)
        {
            // given: no tier widens the amend gate. An Admin who needs past an unresolved
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
                        roles: new List<string> { RoleNames.Admin }),
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
            // given: the closed-round bar is not an Admin-only rule — narrowing it to admins
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
                Roles = new List<string> { RoleNames.Admin },
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

        [Theory]
        [InlineData(RoleNames.Publisher)]
        [InlineData(RoleNames.Reviewer)]
        public async Task ShouldRefuseResolvingAnotherPersonsCommentWithoutAdminAsync(string role)
        {
            // given: the resolve gate widens to Admin and to nobody else
            ResolveApprovalCommentRequest resolveApprovalCommentRequest =
                CreateRandomResolveApprovalCommentRequest(
                actor: CreateRandomAccessActor(roles: new List<string> { role }),
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
            // given: resolving records that a comment is settled, which changes no words —
            // the one comment operation an Admin may perform on someone else's row
            ResolveApprovalCommentRequest resolveApprovalCommentRequest =
                CreateRandomResolveApprovalCommentRequest(
                actor: CreateRandomAccessActor(
                    roles: new List<string> { RoleNames.Admin }),
                commentCreatedBy: GetRandomString());

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayResolveApprovalCommentAsync(resolveApprovalCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
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
                    roles: new List<string> { RoleNames.Admin }),
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
