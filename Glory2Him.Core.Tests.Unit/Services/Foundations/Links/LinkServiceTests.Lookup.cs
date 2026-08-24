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
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.Links.Exceptions;
using Moq;

namespace Glory2Him.Core.Tests.Unit.Services.Foundations.Links
{
    public partial class LinkServiceTests
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

            Link tombstone = CreateProbeRow(
                id: tombstoneId, groupId: groupId, isPublished: true, isDeleted: true);

            Link target = CreateProbeRow(
                id: targetId, groupId: groupId, isPublished: false, isDeleted: false);

            SetupProbeStore(tombstone, target);

            // when
            Guid? actualId = await this.linkService.FindPublishedLinkIdByGroupAsync(
                groupId: groupId,
                excludedLinkId: targetId,
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
            Guid? actualId = await this.linkService.FindPublishedLinkIdByGroupAsync(
                groupId: groupId,
                excludedLinkId: targetId,
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
            Guid? actualId = await this.linkService.FindPublishedLinkIdByGroupAsync(
                groupId: groupId,
                excludedLinkId: targetId,
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
                this.linkService.FindPublishedLinkIdByGroupAsync(
                    groupId: invalidGroupId,
                    excludedLinkId: Guid.NewGuid(),
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            await Assert.ThrowsAsync<LinkValidationException>(probeTask.AsTask);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectAllLinksAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private void SetupProbeStore(params Link[] rows) =>
            this.storageBrokerMock.Setup(broker =>
                broker.SelectAllLinksAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(rows.AsQueryable());

        private static Link CreateProbeRow(
            Guid id,
            Guid groupId,
            bool isPublished,
            bool isDeleted) =>
            new Link
            {
                Id = id,
                GroupId = groupId,
                Version = 1,
                IsPublished = isPublished,
                IsDeleted = isDeleted,
                ApprovalStatus = ApprovalStatus.Approved,
            };

        // ── the publication swap's probe (#291) ─────────────────────────────────────
        // The swap arrives on the workflow's system identity: CreateSystemAsync keeps the
        // original caller's SubjectId but DROPS their roles. Handed to the caller-facing
        // RetrieveLinkByIdAsync that actor is refused — the target is mid-promotion so not
        // publicly visible, and a role-less non-owner is neither owner nor review-role holder.
        // These pin that the probe admits it instead, which is the whole reason the swap uses a
        // gated probe rather than a filtered read.
        [Fact]
        public async Task ShouldFindThePublishedSiblingForAnActorWhoIsNeitherOwnerNorReviewerAsync()
        {
            // given
            var groupId = Guid.Parse("ee000000-1111-1111-1111-111111111111");
            var incumbentId = Guid.Parse("ee000000-2222-2222-2222-222222222222");
            var targetId = Guid.Parse("ee000000-3333-3333-3333-333333333333");

            Link target = CreateProbeRow(
                id: targetId, groupId: groupId, isPublished: false, isDeleted: false);

            target.CreatedBy = "someone-else-entirely";

            Link incumbent = CreateProbeRow(
                id: incumbentId, groupId: groupId, isPublished: true, isDeleted: false);

            // exactly what CreateSystemAsync produces — no roles, and a subject that is the
            // deciding reviewer rather than the row's owner
            var systemEnvelope = new EventEnvelope<Link>
            {
                Content = new Link { Id = targetId },
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
                broker.SelectLinkByIdAsync(targetId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(target);

            SetupProbeStore(target, incumbent);

            // when
            Guid? actualId = await this.linkService.FindPublishedSiblingLinkIdAsync(
                linkId: targetId,
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
            var groupId = Guid.Parse("ee111111-1111-1111-1111-111111111111");
            var targetId = Guid.Parse("ee111111-3333-3333-3333-333333333333");

            Link deletedTarget = CreateProbeRow(
                id: targetId, groupId: groupId, isPublished: false, isDeleted: true);

            var systemEnvelope = new EventEnvelope<Link>
            {
                Content = new Link { Id = targetId },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() },

                SecurityContext = new SecurityContext
                {
                    IsAuthenticated = true,
                    IsSystemIdentity = true,
                    Roles = []
                }
            };

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(targetId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(deletedTarget);

            // when
            ValueTask<Guid?> probeTask = this.linkService.FindPublishedSiblingLinkIdAsync(
                linkId: targetId,
                inboundEnvelope: systemEnvelope,
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            await Assert.ThrowsAsync<LinkValidationException>(probeTask.AsTask);
        }
    }
}
