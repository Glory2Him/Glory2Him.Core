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

using System;
using System.Threading.Tasks;
using FluentAssertions;
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.WebApp.Tests.Acceptance.Models.Approvals;
using CoreContentItem = Glory2Him.Core.Models.Foundations.ContentItems.ContentItem;
using CoreTag = Glory2Him.Core.Models.Foundations.Tags.Tag;
using WireContentItem = Glory2Him.WebApp.Tests.Acceptance.Models.ContentItems.ContentItem;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Approvals
{
    public partial class ApprovalApiTests
    {
        /// <summary>
        /// THE ROUND THAT WAS NEVER OPENED. Seed data is written straight to the storage broker,
        /// so no fact is ever published for it and no approval exists — and the moderation
        /// screen for such an item renders nothing at all, because every control on it hangs off
        /// this read. The verdict is meant to REPAIR that (it opens the round, and decides
        /// nothing on it — §16.7.2), and this
        /// is the shape of the seeded database exactly: a draft, with no approval row.
        /// </summary>
        [Fact]
        public async Task ShouldAnswerADraftWithNoApprovalRowAsBlockedByDraftStatusAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();

            CoreContentItem draft = await this.apiBroker.InsertContentItemVersionAsync(
                groupId: Guid.NewGuid(),
                version: 1,
                approvalStatus: ApprovalStatus.Draft,
                isPublished: false,
                authorUserId: authorUserId);

            try
            {
                // when
                ApprovalVerdict actualVerdict =
                    await this.apiBroker.GetApprovalVerdictAsync(EntityType.ContentItem, draft.Id);

                // then: a draft is a 200 that says why nothing can happen yet, never a 404
                actualVerdict.EntityId.Should().Be(draft.Id);
                actualVerdict.ApprovalStatus.Should().Be((int)ApprovalStatus.Draft);

                actualVerdict.BlockReasons.Should().ContainSingle(reason =>
                    reason.Code == (int)AccessDenialReason.BlockedDueToDraftStatus);

                // and a draft has no round to waive, so no bypass is offered to anyone
                actualVerdict.IsBypassAllowedForCurrentUser.Should().BeFalse();
                actualVerdict.CanApprove.Should().BeFalse();

                // and the round now EXISTS, which is what makes every later read answer
                Approval repairedApproval = await this.apiBroker.GetCoreApprovalByEntityAsync(
                    EntityType.ContentItem, draft.Id);

                repairedApproval.Should().NotBeNull();
                repairedApproval.Id.Should().Be(actualVerdict.ApprovalId);
            }
            finally
            {
                Approval approval = await this.apiBroker.GetCoreApprovalByEntityAsync(
                    EntityType.ContentItem, draft.Id);

                if (approval is not null)
                {
                    await this.apiBroker.RemoveApprovalAsync(approval);
                }

                await this.apiBroker.RemoveCoreContentItemByIdAsync(draft.Id);
            }
        }

        /// <summary>
        /// The same repair for a SUBMITTED item, which then reports the seeded house policy: two
        /// approvals required, none recorded.
        /// </summary>
        [Fact]
        public async Task ShouldAnswerASubmittedItemWithNoApprovalRowAgainstTheSeededPolicyAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();

            CoreContentItem submitted =
                await this.apiBroker.InsertSubmittedContentItemAsync(authorUserId);

            try
            {
                // when
                ApprovalVerdict actualVerdict = await this.apiBroker.GetApprovalVerdictAsync(
                    EntityType.ContentItem, submitted.Id);

                // then
                actualVerdict.ApprovalStatus.Should().Be((int)ApprovalStatus.Submitted);
                actualVerdict.RequiredNumberOfApprovals.Should().Be(2);
                actualVerdict.ApprovalCount.Should().Be(0);

                actualVerdict.BlockReasons.Should().Contain(reason =>
                    reason.Code == (int)AccessDenialReason.ApprovalThresholdNotMet);

                // an administrator may bypass a submitted round under the seeded policy
                actualVerdict.IsBypassAllowedForCurrentUser.Should().BeTrue();
            }
            finally
            {
                Approval approval = await this.apiBroker.GetCoreApprovalByEntityAsync(
                    EntityType.ContentItem, submitted.Id);

                if (approval is not null)
                {
                    await this.apiBroker.RemoveApprovalAsync(approval);
                }

                await this.apiBroker.RemoveCoreContentItemByIdAsync(submitted.Id);
            }
        }

        /// <summary>
        /// THE MODERATOR'S NEXT CLICK: the draft is submitted through the edit form, which is the
        /// §9.2 rule 3 carve-out on the general modify. §9.2 rule 6 has the round move with it,
        /// so the verdict stops saying "not submitted yet" and starts reporting the policy.
        /// </summary>
        [Fact]
        public async Task ShouldReportASubmissionMadeThroughTheModifyCarveOutAgainstThePolicyAsync()
        {
            // given: a seeded-style draft, its round opened by the first read
            string authorUserId = Guid.NewGuid().ToString();

            CoreContentItem draft = await this.apiBroker.InsertContentItemVersionAsync(
                groupId: Guid.NewGuid(),
                version: 1,
                approvalStatus: ApprovalStatus.Draft,
                isPublished: false,
                authorUserId: authorUserId);

            try
            {
                ApprovalVerdict draftVerdict =
                    await this.apiBroker.GetApprovalVerdictAsync(EntityType.ContentItem, draft.Id);

                draftVerdict.ApprovalStatus.Should().Be((int)ApprovalStatus.Draft);

                // when: the administrator — in the publishing tier, so entitled to the carve-out
                // (§9.2 rule 4) — submits it through the same modify the edit form uses
                WireContentItem submitted = await this.apiBroker.GetContentItemByIdAsync(draft.Id);
                submitted.ApprovalStatus = ApprovalStatus.Submitted;
                await this.apiBroker.PutContentItemAsync(submitted);

                ApprovalVerdict submittedVerdict =
                    await this.apiBroker.GetApprovalVerdictAsync(EntityType.ContentItem, draft.Id);

                // then: the round followed the entity, and the policy now speaks
                submittedVerdict.ApprovalStatus.Should().Be((int)ApprovalStatus.Submitted);

                submittedVerdict.BlockReasons.Should().NotContain(reason =>
                    reason.Code == (int)AccessDenialReason.BlockedDueToDraftStatus);

                submittedVerdict.BlockReasons.Should().Contain(reason =>
                    reason.Code == (int)AccessDenialReason.ApprovalThresholdNotMet);

                submittedVerdict.RequiredNumberOfApprovals.Should().Be(2);
                submittedVerdict.IsBypassAllowedForCurrentUser.Should().BeTrue();

                Approval storedApproval = await this.apiBroker.GetCoreApprovalByEntityAsync(
                    EntityType.ContentItem, draft.Id);

                storedApproval.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            }
            finally
            {
                Approval approval = await this.apiBroker.GetCoreApprovalByEntityAsync(
                    EntityType.ContentItem, draft.Id);

                if (approval is not null)
                {
                    await this.apiBroker.RemoveApprovalAsync(approval);
                }

                await this.apiBroker.RemoveCoreContentItemByIdAsync(draft.Id);
            }
        }

        /// <summary>
        /// THE OTHER ROUTE TO SUBMITTED: the dedicated verb (§9.2 rule 3), which publishes
        /// -Submitted rather than -Modified. Until #424 nothing subscribed to it, so a submission
        /// through the verb moved the entity and left the round at Draft — the divergence §9.8
        /// forbids, reported by the verdict as "not submitted yet" on an item that plainly was.
        ///
        /// <para>A Tag, because it is the entity whose verb has an HTTP surface AND whose fact is
        /// signed with the bare foundation name; ContentItem's submit verb is not exposed over
        /// HTTP yet, so it could not be driven from here.</para>
        /// </summary>
        [Fact]
        public async Task ShouldReportASubmissionMadeThroughTheVerbAgainstThePolicyAsync()
        {
            // given: a draft with no round; the first read opens it at Draft
            string authorUserId = Guid.NewGuid().ToString();
            CoreTag draft = await this.apiBroker.InsertDraftTagAsync(authorUserId);

            try
            {
                ApprovalVerdict draftVerdict =
                    await this.apiBroker.GetApprovalVerdictAsync(EntityType.Tag, draft.Id);

                draftVerdict.ApprovalStatus.Should().Be((int)ApprovalStatus.Draft);

                // when: the administrator — publishing tier, so entitled (§9.2 rule 4) — submits
                // through the verb, whose -Submitted fact is delivered before the call returns
                await this.apiBroker.SubmitTagByIdAsync(draft.Id);

                ApprovalVerdict submittedVerdict =
                    await this.apiBroker.GetApprovalVerdictAsync(EntityType.Tag, draft.Id);

                // then: the round followed the entity, and the seeded policy now speaks
                submittedVerdict.ApprovalStatus.Should().Be((int)ApprovalStatus.Submitted);

                submittedVerdict.BlockReasons.Should().NotContain(reason =>
                    reason.Code == (int)AccessDenialReason.BlockedDueToDraftStatus);

                submittedVerdict.BlockReasons.Should().Contain(reason =>
                    reason.Code == (int)AccessDenialReason.ApprovalThresholdNotMet);

                submittedVerdict.RequiredNumberOfApprovals.Should().Be(2);

                Approval storedApproval =
                    await this.apiBroker.GetCoreApprovalByEntityAsync(EntityType.Tag, draft.Id);

                storedApproval.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
            }
            finally
            {
                Approval approval =
                    await this.apiBroker.GetCoreApprovalByEntityAsync(EntityType.Tag, draft.Id);

                if (approval is not null)
                {
                    await this.apiBroker.RemoveApprovalAsync(approval);
                }

                await this.apiBroker.RemoveCoreTagByIdAsync(draft.Id);
            }
        }
    }
}
