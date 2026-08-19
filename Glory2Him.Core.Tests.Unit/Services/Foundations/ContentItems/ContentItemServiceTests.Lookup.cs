// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, 'I am the way and the truth and the life.
//                  No one comes to the Father except through me.'"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.ContentItems
{
    public partial class ContentItemServiceTests
    {
        // These tests sit against the STORAGE BROKER, one layer below the probe, because that is
        // the only seam at which "does this read filter tombstones" can be answered. The
        // publication swap's own tests mock this probe, so they cannot see its predicate — which
        // is exactly how the original defect survived: the swap used the visibility-filtered
        // collection read while its test stubbed that read to return the tombstone anyway.
        [Fact]
        public async Task ShouldFindThePublishedTombstoneHoldingTheGroupSlotAsync()
        {
            // given: THE case the probe exists for. A soft delete never clears IsPublished and
            // the slot index names that column alone, so a removed row still occupies the group's
            // published slot — while being invisible to every caller-facing read. A probe that
            // filtered it out would report no incumbent, the swap would skip the demote, and the
            // promote would be refused by the unique index for every future approval in the group.
            var groupId = Guid.Parse("dddddddd-1111-1111-1111-111111111111");
            var tombstoneId = Guid.Parse("dddddddd-2222-2222-2222-222222222222");
            var targetId = Guid.Parse("dddddddd-3333-3333-3333-333333333333");

            ContentItem tombstone = CreateProbeRow(
                id: tombstoneId, groupId: groupId, isPublished: true, isDeleted: true);

            ContentItem target = CreateProbeRow(
                id: targetId, groupId: groupId, isPublished: false, isDeleted: false);

            SetupProbeStore(tombstone, target);

            // when
            Guid? actualId = await this.contentItemService.FindPublishedContentItemIdByGroupAsync(
                groupId: groupId,
                excludedContentItemId: targetId,
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualId.Should().Be(tombstoneId);
        }

        [Fact]
        public async Task ShouldFindNoPublishedRowWhenTheGroupSlotIsFreeAsync()
        {
            // given
            var groupId = Guid.Parse("eeeeeeee-1111-1111-1111-111111111111");
            var targetId = Guid.Parse("eeeeeeee-3333-3333-3333-333333333333");

            SetupProbeStore(
                CreateProbeRow(
                    id: Guid.Parse("eeeeeeee-2222-2222-2222-222222222222"),
                    groupId: groupId, isPublished: false, isDeleted: false),
                CreateProbeRow(
                    id: targetId, groupId: groupId, isPublished: false, isDeleted: false));

            // when
            Guid? actualId = await this.contentItemService.FindPublishedContentItemIdByGroupAsync(
                groupId: groupId,
                excludedContentItemId: targetId,
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualId.Should().BeNull();
        }

        [Fact]
        public async Task ShouldExcludeTheTargetAndOtherGroupsFromThePublishedProbeAsync()
        {
            // given: one decoy per conjunct, so a weaker predicate returns the wrong row rather
            // than merely being able to.
            var groupId = Guid.Parse("ffffffff-1111-1111-1111-111111111111");
            var otherGroupId = Guid.Parse("ffffffff-9999-9999-9999-999999999999");
            var targetId = Guid.Parse("ffffffff-3333-3333-3333-333333333333");
            var incumbentId = Guid.Parse("ffffffff-4444-4444-4444-444444444444");

            SetupProbeStore(
                // published, but it IS the target
                CreateProbeRow(id: targetId, groupId: groupId, isPublished: true, isDeleted: false),

                // published, but a different group
                CreateProbeRow(
                    id: Guid.Parse("ffffffff-5555-5555-5555-555555555555"),
                    groupId: otherGroupId, isPublished: true, isDeleted: false),

                // same group, but not published
                CreateProbeRow(
                    id: Guid.Parse("ffffffff-6666-6666-6666-666666666666"),
                    groupId: groupId, isPublished: false, isDeleted: false),

                // the only true incumbent
                CreateProbeRow(
                    id: incumbentId, groupId: groupId, isPublished: true, isDeleted: false));

            // when
            Guid? actualId = await this.contentItemService.FindPublishedContentItemIdByGroupAsync(
                groupId: groupId,
                excludedContentItemId: targetId,
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualId.Should().Be(incumbentId);
        }

        [Fact]
        public async Task ShouldThrowValidationExceptionOnFindPublishedIfGroupIdIsInvalidAsync()
        {
            // given
            var invalidGroupId = Guid.Empty;

            // when
            ValueTask<Guid?> probeTask =
                this.contentItemService.FindPublishedContentItemIdByGroupAsync(
                    groupId: invalidGroupId,
                    excludedContentItemId: Guid.NewGuid(),
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            await Assert.ThrowsAsync<ContentItemValidationException>(probeTask.AsTask);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllContentItemsAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldCountSoftDeletedRowsInTheGroupHighWaterMarkAsync()
        {
            // given: THE case #271 is about. A tombstone still owns its version number, because
            // the unique index on (GroupId, Version) carries no IsDeleted filter. If the
            // high-water mark skipped it — the way the TIP check deliberately does — the fork
            // would number its new row onto the tombstone and collide, failing every subsequent
            // fork in that group.
            //
            // The tip and the high-water mark answer different questions on purpose: nobody edits
            // a tombstone, but a tombstone still holds a number.
            var groupId = Guid.Parse("11111111-aaaa-aaaa-aaaa-111111111111");

            ContentItem liveVersionOne = CreateProbeRow(
                id: Guid.NewGuid(), groupId: groupId, isPublished: false, isDeleted: false);

            liveVersionOne.Version = 1;

            ContentItem deletedVersionTwo = CreateProbeRow(
                id: Guid.NewGuid(), groupId: groupId, isPublished: false, isDeleted: true);

            deletedVersionTwo.Version = 2;

            SetupProbeStore(liveVersionOne, deletedVersionTwo);

            // when
            int actualHighestVersion =
                await this.contentItemService.FindHighestVersionInGroupAsync(
                    groupId: groupId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then: 2, not 1 — so the fork numbers its successor 3 and clears the tombstone
            actualHighestVersion.Should().Be(2);
        }

        [Fact]
        public async Task ShouldReportZeroHighWaterMarkForAnUnknownGroupAsync()
        {
            // given: the first version of a brand-new group, which numbers itself 1.
            var groupId = Guid.Parse("22222222-aaaa-aaaa-aaaa-222222222222");

            ContentItem foreignGroupRow = CreateProbeRow(
                id: Guid.NewGuid(), groupId: Guid.NewGuid(), isPublished: false, isDeleted: false);

            foreignGroupRow.Version = 99;

            SetupProbeStore(foreignGroupRow);

            // when
            int actualHighestVersion =
                await this.contentItemService.FindHighestVersionInGroupAsync(
                    groupId: groupId,
                    cancellationToken: TestContext.Current.CancellationToken);

            // then: the foreign row's 99 is invisible — scoping is by group
            actualHighestVersion.Should().Be(0);
        }

        private void SetupProbeStore(params ContentItem[] rows) =>
            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(rows.AsQueryable());

        private static ContentItem CreateProbeRow(
            Guid id,
            Guid groupId,
            bool isPublished,
            bool isDeleted) =>
            new ContentItem
            {
                Id = id,
                GroupId = groupId,
                Version = 1,
                IsPublished = isPublished,
                IsDeleted = isDeleted,
                ApprovalStatus = ApprovalStatus.Approved,
            };
    }
}
