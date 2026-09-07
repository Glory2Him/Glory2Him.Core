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
        // WHAT THESE TESTS CAN AND CANNOT SAY. They stub the narrow storage reads, so they sit
        // ABOVE the predicate rather than at it — "does this read filter tombstones" is no longer
        // answerable here, and the stubs below deliberately do not pretend otherwise. That
        // question is answered against a real catalogue in ContentItemNarrowReadTests.
        //
        // What is still proved here is the half the SERVICE owns and the broker cannot: that the
        // group is taken off the STORED row rather than from the caller, that the target excludes
        // itself, and that the high-water mark counts what it is handed. That distinction matters
        // because of how the original defect survived — the swap used the visibility-filtered
        // collection read while its test stubbed that read to return the tombstone anyway. A stub
        // that re-implements the predicate reproduces exactly that blind spot.
        /// <summary>
        /// The service resolves the group off the STORED target row and returns whatever the slot
        /// read names — including a row no caller-facing read would show, because the probe never
        /// consults one.
        ///
        /// <para>That the slot read itself is UNFILTERED — a soft delete never clears IsPublished
        /// and the slot index names that column alone, so a tombstone still holds the slot — is a
        /// storage predicate, proved in <c>ContentItemNarrowReadTests</c>. It cannot be proved from
        /// here, and this stub does not pretend to.</para>
        /// </summary>
        [Fact]
        public async Task ShouldReturnWhateverRowHoldsTheGroupSlotAsync()
        {
            // given
            var groupId = Guid.Parse("dddddddd-1111-1111-1111-111111111111");
            var tombstoneId = Guid.Parse("dddddddd-2222-2222-2222-222222222222");
            var targetId = Guid.Parse("dddddddd-3333-3333-3333-333333333333");

            ContentItem tombstone = CreateProbeRow(
                id: tombstoneId, groupId: groupId, isPublished: true, isDeleted: true);

            ContentItem target = CreateProbeRow(
                id: targetId, groupId: groupId, isPublished: false, isDeleted: false);

            this.publishedContentItemId = tombstoneId;
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

        /// <summary>
        /// The two conjuncts the SERVICE supplies as arguments: the group, taken off the stored
        /// target row rather than from the caller, and the target's own id, which the probe must
        /// exclude so a row cannot find itself. A weaker call returns the wrong row here.
        /// </summary>
        [Fact]
        public async Task ShouldExcludeTheTargetAndOtherGroupsFromThePublishedProbeAsync()
        {
            // given: a decoy for each argument the service chooses. The IsPublished decoy is left
            // in as documentation of the storage predicate, which is proved elsewhere.
            this.publishedContentItemId = Guid.Parse("ffffffff-4444-4444-4444-444444444444");
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
                broker.SelectContentItemVersionsInGroupAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
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

        // The probe resolves its target by id and then asks the storage layer two NARROW
        // questions - which row holds the group's published slot, and what version numbers the
        // group already owns. The stub answers both over the seeded rows, applying the arguments
        // the SERVICE chooses: the group, which comes off the STORED row rather than from the
        // caller, and the excluded id.
        //
        // The PREDICATES themselves - that the slot read is unfiltered so a tombstone still holds
        // the slot, and that the version read counts tombstones (#271) - are no longer visible at
        // this seam, because they moved into IStorageBroker with the await that lets the caller's
        // token reach the database. They are proved against a real catalogue in
        // ContentItemNarrowReadTests, which is a stronger statement than this seam could make:
        // LINQ-to-Objects never had to translate them.
        // Which row the STORAGE read would name as the group's published incumbent. Set by a test
        // that cares; left empty otherwise, in which case the probe finds nothing.
        private Guid publishedContentItemId;

        private void SetupProbeStore(params ContentItem[] rows)
        {
            // Keyed on the GROUP and the EXCLUDED ID only — the two things the service decides.
            // IsPublished is deliberately NOT evaluated here: that is the storage predicate, and a
            // stub that re-implemented it would pass whether or not the real read still carried it.
            this.storageBrokerMock.Setup(broker =>
                broker.SelectPublishedContentItemInGroupAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Guid groupId, Guid excludedContentItemId, CancellationToken _) =>
                            rows.FirstOrDefault(row =>
                                row.GroupId == groupId
                                    && row.Id != excludedContentItemId
                                    && row.Id == publishedContentItemId));

            this.storageBrokerMock.Setup(broker =>
                broker.SelectContentItemVersionsInGroupAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Guid groupId, CancellationToken _) =>
                            rows.Where(row => row.GroupId == groupId)
                                .Select(row => row.Version)
                                .ToList());

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
                broker.SelectContentItemVersionsInGroupAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
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
                broker.SelectContentItemVersionsInGroupAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
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

            this.publishedContentItemId = incumbentId;

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

            this.publishedContentItemId = incumbentId;
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
