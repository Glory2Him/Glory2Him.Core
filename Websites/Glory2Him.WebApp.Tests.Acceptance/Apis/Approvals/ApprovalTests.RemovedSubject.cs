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
        /// #427, from the outside: the verdict read OPENS a missing round and decides nothing on
        /// it. A live submitted item with no approval row is the case the repair exists for, and
        /// the round it opens must come back <c>Submitted</c> — never <c>Approved</c>, which is
        /// where the evaluation this read no longer runs could take it.
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
    }
}
