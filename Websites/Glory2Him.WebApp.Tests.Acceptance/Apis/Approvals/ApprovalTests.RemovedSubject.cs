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
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.WebApp.Tests.Acceptance.Models.Approvals;
using CoreApprovalSetting = Glory2Him.Core.Models.Foundations.ApprovalSettings.ApprovalSetting;
using RESTFulSense.Exceptions;
using CoreContentItem = Glory2Him.Core.Models.Foundations.ContentItems.ContentItem;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.Approvals
{
    public partial class ApprovalApiTests
    {
        /// <summary>
        /// §14.5 rule 3: a soft-deleted entity is not found for EVERY caller, Administrators
        /// included. The verdict read repairs a missing round, and until this was closed the gate
        /// it repaired behind asked whether the entity's AUTHOR could be read — which a tombstone
        /// answers exactly as it did before the takedown, because the arms behind that probe are
        /// raw by-id reads and this repository has no EF global query filters.
        ///
        /// <para>Removal deliberately leaves the approval alone (§9.7.6), so the row keeps its
        /// <c>Submitted</c> status and passed the still-in-play gate too. A read therefore minted
        /// an approval for a taken-down item and answered 200 where the rule requires
        /// not-found.</para>
        /// </summary>
        [Fact]
        public async Task ShouldAnswerNotFoundForATakenDownItemAndOpenNoRoundForItAsync()
        {
            // given: a submitted item that has since been taken down, with no approval row —
            // the seeded shape, plus the takedown
            string authorUserId = Guid.NewGuid().ToString();

            CoreContentItem takenDownItem = await this.apiBroker.InsertContentItemVersionAsync(
                groupId: Guid.NewGuid(),
                version: 1,
                approvalStatus: ApprovalStatus.Submitted,
                isPublished: false,
                authorUserId: authorUserId,
                isDeleted: true);

            try
            {
                // when
                ValueTask<ApprovalVerdict> getVerdictTask =
                    this.apiBroker.GetApprovalVerdictAsync(
                        EntityType.ContentItem,
                        takenDownItem.Id);

                // then: not found, and not a verdict about a tombstone
                await Assert.ThrowsAsync<HttpResponseNotFoundException>(() =>
                    getVerdictTask.AsTask());

                // and nothing was written on the way: no round exists for a removed subject,
                // so no later read can find one and no policy can drive it anywhere
                Approval mintedApproval = await this.apiBroker.GetCoreApprovalByEntityAsync(
                    EntityType.ContentItem,
                    takenDownItem.Id);

                mintedApproval.Should().BeNull();
            }
            finally
            {
                Approval approval = await this.apiBroker.GetCoreApprovalByEntityAsync(
                    EntityType.ContentItem, takenDownItem.Id);

                if (approval is not null)
                {
                    await this.apiBroker.RemoveApprovalAsync(approval);
                }

                await this.apiBroker.RemoveCoreContentItemByIdAsync(takenDownItem.Id);
            }
        }

        /// <summary>
        /// The repair's happy path from the outside: a live submitted item with no approval row
        /// gets its round opened, and the round comes back <c>Submitted</c>.
        ///
        /// <para><b>This does not on its own prove #427.</b> The seeded ContentItem policy requires
        /// two approvals and never auto-approves, so a round with no reviews stays <c>Submitted</c>
        /// whether or not the read evaluates it — this test passes against the pre-fix code too.
        /// It is kept for the repair itself; the sibling below is what pins the ruling.</para>
        /// </summary>
        [Fact]
        public async Task ShouldOpenTheRoundOnAReadWithoutDecidingItAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();

            CoreContentItem submittedItem = await this.apiBroker.InsertContentItemVersionAsync(
                groupId: Guid.NewGuid(),
                version: 1,
                approvalStatus: ApprovalStatus.Submitted,
                isPublished: false,
                authorUserId: authorUserId);

            try
            {
                // when
                ApprovalVerdict actualVerdict = await this.apiBroker.GetApprovalVerdictAsync(
                    EntityType.ContentItem,
                    submittedItem.Id);

                // then: the round exists, which is what the panel needed
                Approval openedApproval = await this.apiBroker.GetCoreApprovalByEntityAsync(
                    EntityType.ContentItem,
                    submittedItem.Id);

                openedApproval.Should().NotBeNull();
                openedApproval.Id.Should().Be(actualVerdict.ApprovalId);

                // and it is OPEN: the read created the record it needed to answer and applied no
                // outcome to it, under any identity
                openedApproval.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
                openedApproval.IsApprovedByBypass.Should().BeFalse();

                // and the entity was never published behind the read either
                CoreContentItem storedItem =
                    await this.apiBroker.GetCoreContentItemByIdAsync(submittedItem.Id);

                storedItem.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
                storedItem.IsPublished.Should().BeFalse();
            }
            finally
            {
                Approval approval = await this.apiBroker.GetCoreApprovalByEntityAsync(
                    EntityType.ContentItem, submittedItem.Id);

                if (approval is not null)
                {
                    await this.apiBroker.RemoveApprovalAsync(approval);
                }

                await this.apiBroker.RemoveCoreContentItemByIdAsync(submittedItem.Id);
            }
        }

        /// <summary>
        /// #426 from the outside, for the case the first cut of this change MISSED: a taken-down
        /// entity whose round ALREADY EXISTS.
        ///
        /// <para>That is the ordinary takedown — an item is removed after it has been through the
        /// flow, so it has an approval — and the visibility gate was originally placed only inside
        /// the missing-round repair branch, which this case never enters. The read therefore
        /// answered 200 with the whole verdict on a tombstone: approval id, status, counts, block
        /// reasons. An id that never existed answered 404, so the two were trivially told apart and
        /// the route was the takedown oracle §14.5 rule 3 forbids.</para>
        /// </summary>
        [Fact]
        public async Task ShouldAnswerNotFoundForATakenDownItemThatAlreadyHasARoundAsync()
        {
            // given: a submitted item WITH its round already opened, then taken down
            string authorUserId = Guid.NewGuid().ToString();

            CoreContentItem takenDownItem = await this.apiBroker.InsertContentItemVersionAsync(
                groupId: Guid.NewGuid(),
                version: 1,
                approvalStatus: ApprovalStatus.Submitted,
                isPublished: false,
                authorUserId: authorUserId,
                isDeleted: true);

            Approval openRound = await this.apiBroker.InsertSubmittedApprovalAsync(
                entityType: EntityType.ContentItem,
                entityId: takenDownItem.Id,
                authorUserId: authorUserId);

            try
            {
                // when
                ValueTask<ApprovalVerdict> getVerdictTask =
                    this.apiBroker.GetApprovalVerdictAsync(
                        EntityType.ContentItem,
                        takenDownItem.Id);

                // then: the round exists and is perfectly readable, and the answer is still 404 —
                // the SUBJECT is what decides, not the round
                await Assert.ThrowsAsync<HttpResponseNotFoundException>(() =>
                    getVerdictTask.AsTask());
            }
            finally
            {
                await this.apiBroker.RemoveApprovalAsync(openRound);
                await this.apiBroker.RemoveCoreContentItemByIdAsync(takenDownItem.Id);
            }
        }

        /// <summary>
        /// #427, and the test that genuinely proves it: the subject's live policy WOULD
        /// auto-approve the round the moment anything evaluated it.
        ///
        /// <para>A narrow <c>(ContentItem, VerseImage)</c> policy is arranged with
        /// <c>RequireApprovals = false</c> and <c>AutoApproveIfAllApprovalRequirementsMet = true</c>
        /// — the shape the seed writes for the personal-association tier, and one an administrator
        /// may set on any tier. Under the pre-fix repair, which re-ran the whole added flow, this
        /// read drove the fresh round to <c>Approved</c> and published the approving command under
        /// the workflow identity. Under the ruling it opens the round and stops.</para>
        ///
        /// <para>The NARROW tier is used deliberately: it wins for this one content type (§8.4)
        /// and leaves every other ContentItem test on the seeded house policy.</para>
        /// </summary>
        [Fact]
        public async Task ShouldNotApproveOnAReadEvenWhereThePolicyWouldAutoApproveAsync()
        {
            // given
            string authorUserId = Guid.NewGuid().ToString();
            DateTimeOffset arrangedWhen = DateTimeOffset.UtcNow;

            var autoApprovingPolicy = new CoreApprovalSetting
            {
                Id = Guid.NewGuid(),
                EntityType = EntityType.ContentItem,
                ContentType = ContentType.VerseImage,
                RequireApprovals = false,
                RequiredNumberOfApprovals = 0,
                AutoApproveIfAllApprovalRequirementsMet = true,
                AllowSelfApproval = false,
                BlockOnReject = false,
                BlockOnZeroApprovalScore = false,
                RequireReapprovalOnChange = false,
                RequireReviewCommentResolutionBeforeApprovals = false,
                DoNotAllowBypassingSettings = false,
                IsDeleted = false,
                CreatedBy = authorUserId,
                CreatedWhen = arrangedWhen,
                UpdatedBy = authorUserId,
                UpdatedWhen = arrangedWhen,
            };

            await this.apiBroker.InsertCoreApprovalSettingAsync(autoApprovingPolicy);

            CoreContentItem submittedItem = await this.apiBroker.InsertContentItemVersionAsync(
                groupId: Guid.NewGuid(),
                version: 1,
                approvalStatus: ApprovalStatus.Submitted,
                isPublished: false,
                authorUserId: authorUserId,
                contentType: ContentType.VerseImage);

            try
            {
                // when: the read repairs the missing round under a policy that would approve it
                await this.apiBroker.GetApprovalVerdictAsync(
                    EntityType.ContentItem,
                    submittedItem.Id);

                // then: the round was opened and LEFT OPEN
                Approval openedApproval = await this.apiBroker.GetCoreApprovalByEntityAsync(
                    EntityType.ContentItem,
                    submittedItem.Id);

                openedApproval.Should().NotBeNull();
                openedApproval.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
                openedApproval.IsApprovedByBypass.Should().BeFalse();

                // and the entity was neither approved nor published behind the read
                CoreContentItem storedItem =
                    await this.apiBroker.GetCoreContentItemByIdAsync(submittedItem.Id);

                storedItem.ApprovalStatus.Should().Be(ApprovalStatus.Submitted);
                storedItem.IsPublished.Should().BeFalse();
            }
            finally
            {
                Approval approval = await this.apiBroker.GetCoreApprovalByEntityAsync(
                    EntityType.ContentItem, submittedItem.Id);

                if (approval is not null)
                {
                    await this.apiBroker.RemoveApprovalAsync(approval);
                }

                await this.apiBroker.RemoveCoreContentItemByIdAsync(submittedItem.Id);
                await this.apiBroker.RemoveCoreApprovalSettingByIdAsync(autoApprovingPolicy.Id);
            }
        }
    }
}
