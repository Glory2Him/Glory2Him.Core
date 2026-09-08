// ─────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, 'I am the way and the truth and the life.
//                  No one comes to the Father except through me.'"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ─────────────────────────────────────────────────────────────────────────────────

using System;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Orchestrations.Approvals;
using RESTFulSense.Exceptions;
using CoreApprovalSetting = Glory2Him.Core.Models.Foundations.ApprovalSettings.ApprovalSetting;
using CoreContentItem = Glory2Him.Core.Models.Foundations.ContentItems.ContentItem;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Approvals
{
    /// <summary>
    /// The AI-reviewer endpoint family (§8.6.2) over real HTTP: whether Berean is offered, asking
    /// for it, asking again, and taking it off the round.
    ///
    /// <para>The FEATURE SWITCH is what makes these worth running over the host rather than only
    /// under mocks. It is a resolved <c>ApprovalSetting</c>, tiered by §8.4's most-specific-wins,
    /// and the seeded default has it OFF — so a round is only offered Berean because a narrow
    /// policy row says so, resolved through the real decision function against the real settings
    /// table. Nothing about that resolution is visible from a unit test that mocks the broker.</para>
    /// </summary>
    public partial class ApprovalApiTests
    {
        /// <summary>
        /// A narrow <c>(ContentItem, Devotional)</c> tier with the switch ON. The content type is
        /// its own within this class, so these rows never meet the auto-approving
        /// <c>(ContentItem, VerseImage)</c> row the reset and removed-subject tests arrange.
        ///
        /// <para><b>The scope is NOT this suite's alone, though, and that is why every insert in
        /// this file sits inside its test's try.</b> <c>ApprovalSettingApiTests</c> writes a live
        /// <c>(ContentItem, Devotional)</c> row of its own in
        /// <c>ShouldAllowPostWhenEntityTypeContentTypePairIsHeldOnlyByASoftDeletedRowAsync</c>, and
        /// its filler hands the content types round one per call, so no pairing is reserved to
        /// anybody. <c>ApiTestCollection</c> runs both classes serially, which is what keeps two
        /// live rows from ever holding <c>UX_ApprovalSettings_EntityTypeContentType</c> at once —
        /// a row LEFT BEHIND is the only way that guarantee breaks, and it breaks a test that
        /// never touched this file.</para>
        ///
        /// <para>The approval fields are pinned to the harmless end deliberately: nothing here
        /// decides a round, and a policy that could auto-approve would move the item out from
        /// under the test while it was asking about Berean.</para>
        /// </summary>
        private static CoreApprovalSetting CreateAIReviewerOfferedPolicy(string authorUserId)
        {
            DateTimeOffset arrangedWhen = DateTimeOffset.UtcNow;

            return new CoreApprovalSetting
            {
                Id = Guid.NewGuid(),
                EntityType = EntityType.ContentItem,
                ContentType = ContentType.Devotional,
                RequireApprovals = true,
                RequiredNumberOfApprovals = 2,
                AutoApproveIfAllApprovalRequirementsMet = false,
                AllowSelfApproval = false,
                BlockOnReject = false,
                BlockOnZeroApprovalScore = false,
                RequireReapprovalOnChange = false,
                RequireReviewCommentResolutionBeforeApprovals = false,
                DoNotAllowBypassingSettings = false,
                IsAIReviewerOffered = true,

                // Left OFF. Voting is a separate switch the CHECK constraint only permits ON TOP
                // of this one, and nothing in this pass casts a vote.
                IsAIAllowedToVote = false,
                IsDeleted = false,
                CreatedBy = authorUserId,
                CreatedWhen = arrangedWhen,
                UpdatedBy = authorUserId,
                UpdatedWhen = arrangedWhen,
            };
        }

        /// <summary>
        /// The request half, end to end: an offered round reports Berean as available and
        /// unrequested, one POST assigns it pending, and a SECOND POST dissolves quietly into the
        /// standing row rather than colliding with
        /// <c>UX_AIReviewerAssignments_ApprovalId</c>.
        ///
        /// <para>The repeat is the half worth running over HTTP. A moderator double-clicking, or
        /// two of them a second apart, is the ordinary case — and the index that would refuse the
        /// second write is real here, so "the same row comes back" is a claim about the database
        /// rather than about a mock.</para>
        /// </summary>
        [Fact]
        public async Task ShouldAssignBereanAndDissolveARepeatRequestAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();
            CoreApprovalSetting offeredPolicy = CreateAIReviewerOfferedPolicy(authorUserId);
            CoreContentItem submitted = null;
            Approval approval = null;
            AIReviewerAssignment assignment = null;

            // ARRANGED INSIDE THE TRY, all of it. The policy row takes
            // UX_ApprovalSettings_EntityTypeContentType the moment it is stored and the two
            // arrangements after it can throw, so a policy inserted above the try is a row
            // holding a shared scope with nothing left to release it.
            try
            {
                await this.apiBroker.InsertCoreApprovalSettingAsync(offeredPolicy);

                submitted = await this.apiBroker.InsertContentItemVersionAsync(
                    groupId: Guid.NewGuid(),
                    version: 1,
                    approvalStatus: ApprovalStatus.Submitted,
                    isPublished: false,
                    authorUserId: authorUserId,
                    contentType: ContentType.Devotional);

                approval = await this.apiBroker.InsertSubmittedApprovalAsync(
                    EntityType.ContentItem, submitted.Id, authorUserId);

                // when: the panel asks before it renders the control
                AIReviewerStatus offeredStatus = await this.apiBroker.GetAIReviewerStatusAsync(
                    EntityType.ContentItem, submitted.Id);

                // then: offered by the narrow tier, and nobody has asked yet
                offeredStatus.IsOffered.Should().BeTrue();
                offeredStatus.IsRequested.Should().BeFalse();
                offeredStatus.IsAIReviewCompleted.Should().BeFalse();
                offeredStatus.IsAIReviewCommentsPresent.Should().BeFalse();

                // when: Berean is asked for
                assignment = await this.apiBroker.PostAIReviewerAsync(
                    EntityType.ContentItem, submitted.Id);

                // then: a pending row on THIS round
                assignment.Id.Should().NotBe(Guid.Empty);
                assignment.ApprovalId.Should().Be(approval.Id);
                assignment.IsAIReviewCompleted.Should().BeFalse();
                assignment.IsAIReviewCommentsPresent.Should().BeFalse();
                assignment.IsDeleted.Should().BeFalse();

                AIReviewerStatus requestedStatus = await this.apiBroker.GetAIReviewerStatusAsync(
                    EntityType.ContentItem, submitted.Id);

                requestedStatus.IsOffered.Should().BeTrue();
                requestedStatus.IsRequested.Should().BeTrue();

                // when: the same click again
                AIReviewerAssignment repeatAssignment = await this.apiBroker.PostAIReviewerAsync(
                    EntityType.ContentItem, submitted.Id);

                // then: the STANDING row, not a second one and not a conflict
                repeatAssignment.Id.Should().Be(assignment.Id);
            }
            finally
            {
                // Guarded one by one, because the arrangement they undo now runs inside the try
                // and may have stopped anywhere in it. The by-id removals are no-ops on a row
                // that was never written, but the id has to be read off an object that may be
                // null, and the approval is removed by the object itself. The policy row needs no
                // guard: it is built before the try and only its id is used.
                if (assignment is not null)
                {
                    await this.apiBroker.RemoveCoreAIReviewerAssignmentByIdAsync(assignment.Id);
                }

                if (approval is not null)
                {
                    await this.apiBroker.RemoveApprovalAsync(approval);
                }

                if (submitted is not null)
                {
                    await this.apiBroker.RemoveCoreContentItemByIdAsync(submitted.Id);
                }

                await this.apiBroker.RemoveCoreApprovalSettingByIdAsync(offeredPolicy.Id);
            }
        }

        /// <summary>
        /// The withdrawal half, and its idempotence. Taking Berean off a round hands back the row
        /// that went; asking again when there is nothing assigned answers <c>204</c> rather than
        /// <c>404</c> — a stale panel is not a mistake, and a moderator told "not found" for a
        /// state they were trying to reach can do nothing with the refusal.
        /// </summary>
        [Fact]
        public async Task ShouldWithdrawBereanAndAnswerNoContentWhenNothingIsAssignedAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();
            CoreApprovalSetting offeredPolicy = CreateAIReviewerOfferedPolicy(authorUserId);
            CoreContentItem submitted = null;
            Approval approval = null;
            AIReviewerAssignment assignment = null;

            // Arranged inside the try for the reason the test above states.
            try
            {
                await this.apiBroker.InsertCoreApprovalSettingAsync(offeredPolicy);

                submitted = await this.apiBroker.InsertContentItemVersionAsync(
                    groupId: Guid.NewGuid(),
                    version: 1,
                    approvalStatus: ApprovalStatus.Submitted,
                    isPublished: false,
                    authorUserId: authorUserId,
                    contentType: ContentType.Devotional);

                approval = await this.apiBroker.InsertSubmittedApprovalAsync(
                    EntityType.ContentItem, submitted.Id, authorUserId);

                assignment = await this.apiBroker.PostAIReviewerAsync(
                    EntityType.ContentItem, submitted.Id);

                // when
                AIReviewerAssignment withdrawnAssignment = await this.apiBroker.DeleteAIReviewerAsync(
                    EntityType.ContentItem, submitted.Id);

                // then: the row that went, named by the round rather than by an id the caller had
                // to have been handed earlier
                withdrawnAssignment.Id.Should().Be(assignment.Id);

                // and it is REMOVED rather than gone — read beneath the endpoints, because the
                // round-keyed read every endpoint uses is filtered to live rows
                AIReviewerAssignment storedAssignment =
                    await this.apiBroker.GetCoreAIReviewerAssignmentByIdAsync(assignment.Id);

                storedAssignment.IsDeleted.Should().BeTrue();

                // and the round reports Berean as offered again but unrequested, which is what
                // puts the "ask" control back on the panel
                AIReviewerStatus withdrawnStatus = await this.apiBroker.GetAIReviewerStatusAsync(
                    EntityType.ContentItem, submitted.Id);

                withdrawnStatus.IsOffered.Should().BeTrue();
                withdrawnStatus.IsRequested.Should().BeFalse();

                // when: withdrawn again, from a panel that had not caught up
                HttpStatusCode repeatWithdrawalStatusCode =
                    await this.apiBroker.DeleteAIReviewerReturningStatusAsync(
                        EntityType.ContentItem, submitted.Id);

                // then
                repeatWithdrawalStatusCode.Should().Be(HttpStatusCode.NoContent);
            }
            finally
            {
                // PHYSICAL, on a row the test has already withdrawn: the withdrawal is a soft
                // delete, so the row is still there holding its round's slot until this removes it.
                if (assignment is not null)
                {
                    await this.apiBroker.RemoveCoreAIReviewerAssignmentByIdAsync(assignment.Id);
                }

                if (approval is not null)
                {
                    await this.apiBroker.RemoveApprovalAsync(approval);
                }

                if (submitted is not null)
                {
                    await this.apiBroker.RemoveCoreContentItemByIdAsync(submitted.Id);
                }

                await this.apiBroker.RemoveCoreApprovalSettingByIdAsync(offeredPolicy.Id);
            }
        }

        /// <summary>
        /// FAIL-CLOSED (§8.4 rule 2). No narrow policy is arranged here, so the round resolves to
        /// the seeded default — which ships the switch OFF — and the write is refused with a
        /// <c>400</c>.
        ///
        /// <para>The status read is asserted alongside it, and the pair is the point: a panel told
        /// the feature is offered would render a control whose POST is refused, so the read and
        /// the write have to agree. Only a request that reaches the host can show that, because
        /// the agreement is between two endpoints resolving the same policy independently.</para>
        /// </summary>
        [Fact]
        public async Task ShouldRefuseToAssignBereanWhereTheFeatureIsNotOfferedAsync()
        {
            // given: an ordinary Story round, on the seeded default tier
            string authorUserId = Guid.NewGuid().ToString();
            CoreContentItem submitted = null;
            Approval approval = null;

            // No policy row here — this test's whole point is that none resolves — but the
            // arrangement sits inside the try alongside its siblings above: an item written and
            // then abandoned is still an item the collection's reads see.
            try
            {
                submitted = await this.apiBroker.InsertSubmittedContentItemAsync(authorUserId);

                approval = await this.apiBroker.InsertSubmittedApprovalAsync(
                    EntityType.ContentItem, submitted.Id, authorUserId);

                // when
                AIReviewerStatus notOfferedStatus = await this.apiBroker.GetAIReviewerStatusAsync(
                    EntityType.ContentItem, submitted.Id);

                // then: the panel is told not to offer it
                notOfferedStatus.IsOffered.Should().BeFalse();
                notOfferedStatus.IsRequested.Should().BeFalse();

                // when: a caller posts at the endpoint anyway, having never seen the panel
                ValueTask<AIReviewerAssignment> requestTask = this.apiBroker.PostAIReviewerAsync(
                    EntityType.ContentItem, submitted.Id);

                // then: refused as a bad request, not accepted and not a 424
                await Assert.ThrowsAsync<HttpResponseBadRequestException>(() =>
                    requestTask.AsTask());

                // and nothing was assigned — the refusal precedes the write, so the round is
                // exactly as it was
                AIReviewerStatus statusAfterRefusal = await this.apiBroker.GetAIReviewerStatusAsync(
                    EntityType.ContentItem, submitted.Id);

                statusAfterRefusal.IsRequested.Should().BeFalse();
            }
            finally
            {
                if (approval is not null)
                {
                    await this.apiBroker.RemoveApprovalAsync(approval);
                }

                if (submitted is not null)
                {
                    await this.apiBroker.RemoveCoreContentItemByIdAsync(submitted.Id);
                }
            }
        }
    }
}
