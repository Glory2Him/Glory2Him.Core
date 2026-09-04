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
using Glory2Him.Core.Models.Securities;
using Glory2Him.Core.Models.Events;
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
            Guid? actualId = await this.contentItemService.FindPublishedSiblingContentItemIdAsync(
                contentItemId: targetId,
                inboundEnvelope: CreateProbeEnvelope(targetId),
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
            Guid? actualId = await this.contentItemService.FindPublishedSiblingContentItemIdAsync(
                contentItemId: targetId,
                inboundEnvelope: CreateProbeEnvelope(targetId),
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
            Guid? actualId = await this.contentItemService.FindPublishedSiblingContentItemIdAsync(
                contentItemId: targetId,
                inboundEnvelope: CreateProbeEnvelope(targetId),
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
                this.contentItemService.FindPublishedSiblingContentItemIdAsync(
                    contentItemId: invalidGroupId,
                    inboundEnvelope: CreateProbeEnvelope(invalidGroupId),
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

        // The probe resolves its target by id and then reads the whole store, so the stub
        // answers both. Every row goes into SelectXByIdAsync as well, which is what lets a
        // ported test name any of them as the target.
        private void SetupProbeStore(params ContentItem[] rows)
        {
            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllContentItemsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(rows.AsQueryable());

            foreach (ContentItem row in rows)
            {
                ContentItem captured = row;

                this.storageBrokerMock.Setup(broker =>
                    broker.SelectContentItemByIdAsync(captured.Id, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(captured);
            }
        }

        // The workflow's own envelope: authenticated, system identity, and NO roles — exactly
        // what CreateSystemAsync hands the swap.
        private static EventEnvelope<ContentItem> CreateProbeEnvelope(Guid contentItemId) =>
            new EventEnvelope<ContentItem>
            {
                Content = new ContentItem { Id = contentItemId },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() },

                SecurityContext = new SecurityContext
                {
                    IsAuthenticated = true,
                    IsSystemIdentity = true,
                    Roles = []
                }
            };

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

        [Fact]
        public async Task ShouldRefuseTheSiblingProbeForABlockedCallerAsync()
        {
            // given: the contribution gate is the probe's ONLY authorization check, and the
            // probe reads over the UNFILTERED store — so if the gate goes, a blocked caller
            // learns which row holds a group's published slot, tombstones included. Deleting
            // the gate left the whole suite green before this test existed.
            var targetId = Guid.Parse("ab000000-3333-3333-3333-333333333333");

            var blockedEnvelope = new EventEnvelope<ContentItem>
            {
                Content = new ContentItem { Id = targetId },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() },

                SecurityContext = new SecurityContext
                {
                    IsAuthenticated = true,
                    IsSystemIdentity = true,
                    Roles = [Roles.ReadOnly]
                }
            };

            // when
            ValueTask<Guid?> probeTask = this.contentItemService.FindPublishedSiblingContentItemIdAsync(
                contentItemId: targetId,
                inboundEnvelope: blockedEnvelope,
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            await Assert.ThrowsAsync<ContentItemValidationException>(probeTask.AsTask);

            // refused BEFORE any read — the gate is not a filter applied to results
            this.storageBrokerMock.Verify(broker =>
                broker.SelectContentItemByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllContentItemsAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnSiblingProbeIfTargetIsMissingAsync()
        {
            // given: the case the NotFound clause was added to TryCatchIdentifier for. Without
            // that clause this surfaces as a service exception — "our code is broken" — rather
            // than the validation failure it is.
            var targetId = Guid.Parse("ab111111-3333-3333-3333-333333333333");
            ContentItem missingContentItem = null;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(targetId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(missingContentItem);

            // when
            ValueTask<Guid?> probeTask = this.contentItemService.FindPublishedSiblingContentItemIdAsync(
                contentItemId: targetId,
                inboundEnvelope: CreateProbeEnvelope(targetId),
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            await Assert.ThrowsAsync<ContentItemValidationException>(probeTask.AsTask);

            // and it never went on to read the store for an incumbent
            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllContentItemsAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // ── the publication swap's probe (#291) ─────────────────────────────────────
        // The swap arrives on the workflow's system identity: CreateSystemAsync keeps the
        // original caller's SubjectId but DROPS their roles. Handed to the caller-facing
        // RetrieveContentItemByIdAsync that actor is refused — the target is mid-promotion so not
        // publicly visible, and a role-less non-owner is neither owner nor review-role holder.
        // These pin that the probe admits it instead, which is the whole reason the swap uses a
        // gated probe rather than a filtered read.
        [Fact]
        public async Task ShouldFindThePublishedSiblingForAnActorWhoIsNeitherOwnerNorReviewerAsync()
        {
            // given
            var groupId = Guid.Parse("cc000000-1111-1111-1111-111111111111");
            var incumbentId = Guid.Parse("cc000000-2222-2222-2222-222222222222");
            var targetId = Guid.Parse("cc000000-3333-3333-3333-333333333333");

            ContentItem target = CreateProbeRow(
                id: targetId, groupId: groupId, isPublished: false, isDeleted: false);

            target.CreatedBy = "someone-else-entirely";

            ContentItem incumbent = CreateProbeRow(
                id: incumbentId, groupId: groupId, isPublished: true, isDeleted: false);

            // exactly what CreateSystemAsync produces — no roles, and a subject that is the
            // deciding reviewer rather than the row's owner
            var systemEnvelope = new EventEnvelope<ContentItem>
            {
                Content = new ContentItem { Id = targetId },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() },

                SecurityContext = new SecurityContext
                {
                    IsAuthenticated = true,
                    IsSystemIdentity = true,
                    SubjectId = "the-deciding-reviewer",
                    Roles = []
                }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(targetId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(target);

            SetupProbeStore(target, incumbent);

            // when
            Guid? actualId = await this.contentItemService.FindPublishedSiblingContentItemIdAsync(
                contentItemId: targetId,
                inboundEnvelope: systemEnvelope,
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualId.Should().Be(incumbentId);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnFindPublishedSiblingIfTargetIsSoftDeletedAsync()
        {
            // given: a tombstone has no group membership to promote into. Note this is the
            // TARGET being deleted — an incumbent tombstone still holds the slot and must
            // still be found, which the sibling test above pins.
            var groupId = Guid.Parse("cc111111-1111-1111-1111-111111111111");
            var targetId = Guid.Parse("cc111111-3333-3333-3333-333333333333");

            ContentItem deletedTarget = CreateProbeRow(
                id: targetId, groupId: groupId, isPublished: false, isDeleted: true);

            var systemEnvelope = new EventEnvelope<ContentItem>
            {
                Content = new ContentItem { Id = targetId },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() },

                SecurityContext = new SecurityContext
                {
                    IsAuthenticated = true,
                    IsSystemIdentity = true,
                    Roles = []
                }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemByIdAsync(targetId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(deletedTarget);

            // when
            ValueTask<Guid?> probeTask = this.contentItemService.FindPublishedSiblingContentItemIdAsync(
                contentItemId: targetId,
                inboundEnvelope: systemEnvelope,
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            await Assert.ThrowsAsync<ContentItemValidationException>(probeTask.AsTask);
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnFindHighestVersionInGroupIfCancellationRequestedAsync()
        {
            // given
            Guid someGroupId = Guid.NewGuid();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<int> findHighestVersionTask =
                this.contentItemService.FindHighestVersionInGroupAsync(
                    someGroupId,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(
                findHighestVersionTask.AsTask);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnFindPublishedSiblingIfCancellationRequestedAsync()
        {
            // given
            var someContentItemId = Guid.NewGuid();
            EventEnvelope<ContentItem> inboundEnvelope = CreateProbeEnvelope(someContentItemId);
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<Guid?> probeTask =
                this.contentItemService.FindPublishedSiblingContentItemIdAsync(
                    contentItemId: someContentItemId,
                    inboundEnvelope: inboundEnvelope,
                    cancellationToken: cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(probeTask.AsTask);

            this.storageBrokerMock.VerifyNoOtherCalls();
            this.loggingBrokerMock.VerifyNoOtherCalls();
        }
    }
}
