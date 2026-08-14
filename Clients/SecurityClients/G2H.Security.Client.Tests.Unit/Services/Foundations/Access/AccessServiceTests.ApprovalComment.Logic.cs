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

            RecordCommentRequest recordCommentRequest =
                CreateRandomRecordCommentRequest(actor: unauthenticatedActor);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalCommentAsync(recordCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotAuthenticated);
        }

        [Fact]
        public async Task ShouldRefuseRecordingACommentWhenTheParentApprovalIsDeletedAsync()
        {
            // given: the foreign key still resolves — deletion is a flag, not a row removal —
            // so this is the half of "existing, non-deleted parent" the key cannot express
            RecordCommentRequest recordCommentRequest =
                CreateRandomRecordCommentRequest(isParentApprovalDeleted: true);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalCommentAsync(recordCommentRequest);

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
            RecordCommentRequest recordCommentRequest = CreateRandomRecordCommentRequest(
                approvalState: ApprovalState.Approved,
                isParentApprovalDeleted: true);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalCommentAsync(recordCommentRequest);

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
            RecordCommentRequest recordCommentRequest =
                CreateRandomRecordCommentRequest(approvalState: closedState);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalCommentAsync(recordCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.ApprovalNotOpenForComment);
        }

        [Fact]
        public async Task ShouldPermitRecordingACommentOnAnOpenApprovalWithoutAnyTierAsync()
        {
            // given: commenting is not reviewing — an actor holding no role at all may speak
            RecordCommentRequest recordCommentRequest = CreateRandomRecordCommentRequest(
                actor: CreateRandomAccessActor(roles: new List<string>()));

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayRecordApprovalCommentAsync(recordCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        [Fact]
        public async Task ShouldRefuseAmendingACommentWhenTheActorIsNotAuthenticatedAsync()
        {
            // given
            string authorId = GetRandomString();

            AmendCommentRequest amendCommentRequest = CreateRandomAmendCommentRequest(
                actor: CreateRandomAccessActor(userId: authorId, isAuthenticated: false),
                commentCreatedBy: authorId);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayAmendApprovalCommentAsync(amendCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotAuthenticated);
        }

        [Fact]
        public async Task ShouldRefuseAmendingACommentTheActorDidNotWriteAsync()
        {
            // given
            AmendCommentRequest amendCommentRequest = CreateRandomAmendCommentRequest(
                actor: CreateRandomAccessActor(userId: GetRandomString()),
                commentCreatedBy: GetRandomString());

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayAmendApprovalCommentAsync(amendCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotCommentAuthor);
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
            AmendCommentRequest amendCommentRequest = CreateRandomAmendCommentRequest(
                actor: CreateRandomAccessActor(roles: new List<string> { role }),
                commentCreatedBy: GetRandomString());

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayAmendApprovalCommentAsync(amendCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotCommentAuthor);
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

            AmendCommentRequest amendCommentRequest = CreateRandomAmendCommentRequest(
                actor: CreateRandomAccessActor(userId: authorId),
                commentCreatedBy: authorId,
                approvalState: closedState);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayAmendApprovalCommentAsync(amendCommentRequest);

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

            AmendCommentRequest amendCommentRequest = CreateRandomAmendCommentRequest(
                actor: CreateRandomAccessActor(userId: authorId, roles: new List<string>()),
                commentCreatedBy: authorId);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayAmendApprovalCommentAsync(amendCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        [Fact]
        public async Task ShouldRefuseResolvingACommentWhenTheActorIsNotAuthenticatedAsync()
        {
            // given
            string authorId = GetRandomString();

            ResolveCommentRequest resolveCommentRequest = CreateRandomResolveCommentRequest(
                actor: CreateRandomAccessActor(userId: authorId, isAuthenticated: false),
                commentCreatedBy: authorId);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayResolveApprovalCommentAsync(resolveCommentRequest);

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
            ResolveCommentRequest resolveCommentRequest = CreateRandomResolveCommentRequest(
                actor: CreateRandomAccessActor(roles: new List<string> { role }),
                commentCreatedBy: GetRandomString());

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayResolveApprovalCommentAsync(resolveCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.NotCommentAuthor);
        }

        [Fact]
        public async Task ShouldPermitAnAdminResolvingAnotherPersonsCommentAsync()
        {
            // given: resolving records that a question was answered, which changes no words —
            // the one comment operation an Admin may perform on someone else's row
            ResolveCommentRequest resolveCommentRequest = CreateRandomResolveCommentRequest(
                actor: CreateRandomAccessActor(
                    roles: new List<string> { RoleNames.Admin }),
                commentCreatedBy: GetRandomString());

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayResolveApprovalCommentAsync(resolveCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeTrue();
            actualVerdict.DenialReason.Should().Be(AccessDenialReason.None);
        }

        [Fact]
        public async Task ShouldPermitTheAuthorResolvingTheirOwnCommentWithoutAnyRoleAsync()
        {
            // given
            string authorId = GetRandomString();

            ResolveCommentRequest resolveCommentRequest = CreateRandomResolveCommentRequest(
                actor: CreateRandomAccessActor(userId: authorId, roles: new List<string>()),
                commentCreatedBy: authorId);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayResolveApprovalCommentAsync(resolveCommentRequest);

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
            ResolveCommentRequest resolveCommentRequest = CreateRandomResolveCommentRequest(
                actor: CreateRandomAccessActor(
                    roles: new List<string> { RoleNames.Admin }),
                commentCreatedBy: GetRandomString(),
                approvalState: closedState);

            // when
            AccessVerdict actualVerdict =
                await this.accessService.MayResolveApprovalCommentAsync(resolveCommentRequest);

            // then
            actualVerdict.IsPermitted.Should().BeFalse();

            actualVerdict.DenialReason.Should()
                .Be(AccessDenialReason.ApprovalNotOpenForComment);
        }
    }
}
