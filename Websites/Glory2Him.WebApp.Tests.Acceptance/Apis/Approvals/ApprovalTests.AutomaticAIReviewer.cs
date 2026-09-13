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
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.AIReviewerAssignments;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Orchestrations.AIReviewers;
using CoreApprovalSetting = Glory2Him.Core.Models.Foundations.ApprovalSettings.ApprovalSetting;
using HttpTag = Glory2Him.WebApp.Tests.Acceptance.Models.Tags.Tag;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Approvals
{
    /// <summary>
    /// §8.6.2.1's automatic assignment over real HTTP — a moderator opening a round that has
    /// entered review finds Berean already on it, with nobody having pressed anything.
    ///
    /// <para><b>What only this level can prove.</b> A unit test that invokes the handler directly
    /// cannot fail when the registration binds the wrong address or the wrong event name, and
    /// that failure is SILENT: the name is inside the HMAC, so the fact is delivered, verified
    /// against a name it was never signed with, and discarded. These two tests never call the
    /// handler — they make the entity move and then ask the panel what it sees.</para>
    ///
    /// <para><b>The entity is <c>Tag</c>, and the calls are named because the obvious helpers
    /// cannot work.</b> <c>InsertSubmittedApprovalAsync</c>, <c>InsertContentItemVersionAsync</c>,
    /// <c>InsertSubmittedTagAsync</c> and <c>InsertDraftTagAsync</c> all write through the storage
    /// broker directly and publish NOTHING, so a test arranged through any of them can never see a
    /// subscription fire. None of the four appears below. <c>Tag</c> rather than
    /// <c>ContentItem</c> because ContentItem's submit verb is not exposed over HTTP yet, so it
    /// cannot drive the second test's half at all.</para>
    ///
    /// <para><b>The policy IS arranged beneath HTTP</b>, and that is not the same ban: an
    /// <c>ApprovalSetting</c> is read as a verdict and publishes no fact these tests wait on. It
    /// has to be the <c>Tag</c> entity-type DEFAULT rather than a narrower row, because
    /// <c>ContentType</c> narrows policies for <c>ContentItem</c> alone (§8.4) — so the seeded
    /// incumbent is lifted out and put back byte-for-byte in the teardown, exactly as
    /// <c>ApprovalSettingApiTests</c> does with <c>Link</c>'s.</para>
    /// </summary>
    public partial class ApprovalApiTests
    {
        /// <summary>
        /// Criterion 13.1 — <c>Approval-Added</c>. The endpoint's own add is what opens the
        /// round, and the round opens at the status the entity was created at, so a create at
        /// <c>Submitted</c> publishes <c>Approval-Added</c> on a round already in review.
        /// </summary>
        [Fact]
        public async Task ShouldAssignBereanWhenATagIsCreatedAlreadySubmittedAsync()
        {
            // given: the Tag tier asks for Berean without anybody clicking
            CoreApprovalSetting seededTagDefault =
                await this.apiBroker.GetCoreDefaultApprovalSettingAsync(EntityType.Tag);

            CoreApprovalSetting automaticPolicy =
                CreateAutomaticAIReviewerTagPolicy(seededTagDefault);

            HttpTag submittedTag = null;
            Approval approval = null;
            AIReviewerAssignment assignment = null;

            // THE SLOT HAS TO BE FREED FIRST. UX_ApprovalSettings_EntityTypeDefault constrains
            // LIVE rows, so two live Tag defaults cannot coexist — and the release is the ONLY
            // line before the try, so everything that could throw while the slot is empty,
            // the replacement's own insert included, is covered by the restore in the finally.
            await this.apiBroker.RemoveCoreApprovalSettingByIdAsync(seededTagDefault.Id);

            try
            {
                await this.apiBroker.InsertCoreApprovalSettingAsync(automaticPolicy);

                // when: created through the ENDPOINT, at Submitted
                submittedTag = await this.apiBroker.PostTagAsync(CreateSubmittedHttpTag());

                // then: the round opened at Submitted, which is what Approval-Added carried
                approval = await this.apiBroker.GetCoreApprovalByEntityAsync(
                    EntityType.Tag, submittedTag.Id);

                approval.Should().NotBeNull();
                approval.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);

                // and Berean is on it, with nobody having called POST api/AIReviewers/...
                AIReviewerStatus status = await this.apiBroker.GetAIReviewerStatusAsync(
                    EntityType.Tag, submittedTag.Id);

                status.IsOffered.Should().BeTrue();

                status.IsRequested.Should().BeTrue(
                    because: "the subscription on Approval-Added assigned Berean during the "
                        + "POST — a handler bound to the wrong address, or verifying against "
                        + "the wrong signed name, would leave this false and say nothing");

                status.IsAIReviewCompleted.Should().BeFalse();
                status.IsAIReviewCommentsPresent.Should().BeFalse();

                // and it is the same row POST api/AIReviewers/... writes — one live assignment on
                // this round, both flags down
                assignment = await this.apiBroker
                    .GetCoreAIReviewerAssignmentByApprovalIdAsync(approval.Id);

                assignment.Should().NotBeNull();
                assignment.ApprovalId.Should().Be(approval.Id);
                assignment.IsAIReviewCompleted.Should().BeFalse();
                assignment.IsAIReviewCommentsPresent.Should().BeFalse();
                assignment.IsDeleted.Should().BeFalse();
            }
            finally
            {
                await RemoveAutomaticAIReviewerArrangementAsync(
                    assignment, approval, submittedTag, automaticPolicy, seededTagDefault);
            }
        }

        /// <summary>
        /// Criterion 13.2 — <c>Approval-Modified</c>. A round opened at <c>Draft</c> does NOT get
        /// Berean, which is gate 3's Draft case end to end; the submit verb then drives the round
        /// to <c>Submitted</c> through <c>ModifyApprovalAsync</c>, whose <c>Approval-Modified</c>
        /// is delivered before the call returns.
        ///
        /// <para>The create goes through <c>POST api/Tags</c> rather than
        /// <c>InsertDraftTagAsync</c> — the draft has to have a REAL round behind it, and the
        /// helper opens none.</para>
        /// </summary>
        [Fact]
        public async Task ShouldAssignBereanWhenADraftTagIsSubmittedThroughTheVerbAsync()
        {
            // given
            CoreApprovalSetting seededTagDefault =
                await this.apiBroker.GetCoreDefaultApprovalSettingAsync(EntityType.Tag);

            CoreApprovalSetting automaticPolicy =
                CreateAutomaticAIReviewerTagPolicy(seededTagDefault);

            HttpTag draftTag = null;
            Approval approval = null;
            AIReviewerAssignment assignment = null;

            // Freed first, and the only line before the try — see the sibling above.
            await this.apiBroker.RemoveCoreApprovalSettingByIdAsync(seededTagDefault.Id);

            try
            {
                await this.apiBroker.InsertCoreApprovalSettingAsync(automaticPolicy);

                // when: created at Draft through the endpoint, which opens the round at Draft
                draftTag = await this.apiBroker.PostTagAsync(CreateDraftHttpTag());

                // then: NOTHING yet. A round opened at Draft has not entered review, and Berean
                // must not read content its author has not offered (§9.2) — observed rather than
                // assumed, so the assertion below is a change rather than a state that was always
                // going to hold.
                AIReviewerStatus draftStatus = await this.apiBroker.GetAIReviewerStatusAsync(
                    EntityType.Tag, draftTag.Id);

                draftStatus.IsOffered.Should().BeTrue();
                draftStatus.IsRequested.Should().BeFalse();

                // when: submitted through the VERB, whose fact is delivered before it returns
                await this.apiBroker.SubmitTagByIdAsync(draftTag.Id);

                // then: the round followed the entity, and Berean went on with it
                approval = await this.apiBroker.GetCoreApprovalByEntityAsync(
                    EntityType.Tag, draftTag.Id);

                approval.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);

                AIReviewerStatus submittedStatus = await this.apiBroker.GetAIReviewerStatusAsync(
                    EntityType.Tag, draftTag.Id);

                submittedStatus.IsRequested.Should().BeTrue(
                    because: "the round reached Submitted through ModifyApprovalAsync, whose "
                        + "Approval-Modified is the second address the automatic assignment "
                        + "binds");

                assignment = await this.apiBroker
                    .GetCoreAIReviewerAssignmentByApprovalIdAsync(approval.Id);

                assignment.Should().NotBeNull();
                assignment.IsAIReviewCompleted.Should().BeFalse();
                assignment.IsAIReviewCommentsPresent.Should().BeFalse();
            }
            finally
            {
                await RemoveAutomaticAIReviewerArrangementAsync(
                    assignment, approval, draftTag, automaticPolicy, seededTagDefault);
            }
        }

        /// <summary>
        /// The seeded <c>Tag</c> default with the two AI switches ON and EVERY other field copied
        /// off the incumbent. Copying rather than composing is deliberate: the approval fields
        /// decide what the round does once it opens, and a policy that could auto-approve would
        /// move the tag out from under the test while it was asking about Berean.
        /// </summary>
        private static CoreApprovalSetting CreateAutomaticAIReviewerTagPolicy(
            CoreApprovalSetting seededTagDefault)
        {
            DateTimeOffset arrangedWhen = DateTimeOffset.UtcNow;

            return new CoreApprovalSetting
            {
                Id = Guid.NewGuid(),
                EntityType = EntityType.Tag,

                // The entity-type DEFAULT tier, which is the only one Tag has: ContentType
                // narrows policies for ContentItem alone, and CK_ApprovalSetting_
                // ContentTypeRequiresContentItem refuses a populated one anywhere else (§8.4).
                ContentType = null,
                IsPersonal = null,
                RequireApprovals = seededTagDefault.RequireApprovals,
                RequiredNumberOfApprovals = seededTagDefault.RequiredNumberOfApprovals,

                AutoApproveIfAllApprovalRequirementsMet =
                    seededTagDefault.AutoApproveIfAllApprovalRequirementsMet,

                AllowSelfApproval = seededTagDefault.AllowSelfApproval,
                BlockOnReject = seededTagDefault.BlockOnReject,
                BlockOnZeroApprovalScore = seededTagDefault.BlockOnZeroApprovalScore,
                RequireReapprovalOnChange = seededTagDefault.RequireReapprovalOnChange,

                RequireReviewCommentResolutionBeforeApprovals =
                    seededTagDefault.RequireReviewCommentResolutionBeforeApprovals,

                DoNotAllowBypassingSettings = seededTagDefault.DoNotAllowBypassingSettings,

                // THE TWO THIS SUITE IS ABOUT. The offer alone puts the control on the panel; the
                // pair is what makes Berean arrive without it being pressed (§8.6.2.1).
                IsAIReviewerOffered = true,
                IsAIReviewerAutomaticallyRequested = true,

                // Left OFF. Voting is a separate switch and nothing here casts a vote.
                IsAIAllowedToVote = false,
                IsDeleted = false,
                CreatedBy = seededTagDefault.CreatedBy,
                CreatedWhen = arrangedWhen,
                UpdatedBy = seededTagDefault.UpdatedBy,
                UpdatedWhen = arrangedWhen,
            };
        }

        private static HttpTag CreateSubmittedHttpTag() =>
            CreateHttpTagAt(ApprovalStatus.Submitted);

        private static HttpTag CreateDraftHttpTag() =>
            CreateHttpTagAt(ApprovalStatus.Draft);

        // POST api/Tags accepts a create at Submitted: TagsController applies no status gate, and
        // the add rule refuses only a VERDICT status (Approved/Rejected), never Submitted.
        private static HttpTag CreateHttpTagAt(ApprovalStatus approvalStatus) =>
            new HttpTag
            {
                Id = Guid.NewGuid(),
                Name = Guid.NewGuid().ToString("N").Substring(0, 30),
                ApprovalStatus = approvalStatus,
                IsPublished = false,
            };

        // Ordered by what points at what: the assignment carries the approval's FK, the approval
        // carries the tag's key, and the policy row holds the Tag default slot until the seeded
        // incumbent can go back into it. Every step is guarded, because the arrangement it undoes
        // runs inside the try and may have stopped anywhere in it.
        private async ValueTask RemoveAutomaticAIReviewerArrangementAsync(
            AIReviewerAssignment assignment,
            Approval approval,
            HttpTag tag,
            CoreApprovalSetting automaticPolicy,
            CoreApprovalSetting seededTagDefault)
        {
            if (assignment is not null)
            {
                await this.apiBroker.RemoveCoreAIReviewerAssignmentByIdAsync(assignment.Id);
            }

            if (approval is not null)
            {
                await this.apiBroker.RemoveApprovalAsync(approval);
            }

            if (tag is not null)
            {
                await this.apiBroker.RemoveCoreTagByIdAsync(tag.Id);
            }

            await this.apiBroker.RemoveCoreApprovalSettingByIdAsync(automaticPolicy.Id);
            await this.apiBroker.InsertCoreApprovalSettingAsync(seededTagDefault);
        }
    }
}
