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
        // WHAT THESE TESTS CAN AND CANNOT SAY. They stub the narrow storage reads, so they sit
        // ABOVE the predicate rather than at it — "does this read filter tombstones" is no longer
        // answerable here, and the stubs below deliberately do not pretend otherwise. Nor is it
        // answered anywhere else: #486 removed the link narrow-read fixture as a per-entity
        // re-proof of predicate translation, keeping the content-item one as the single canonical
        // proof of the SHAPE. The link predicate bodies in StorageBroker.Link.cs are knowingly
        // left unasserted — a divergence between them and their content-item twins would not be
        // caught by any test in this repository.
        //
        // What is still proved here is the half the SERVICE owns and the broker cannot: that the
        // group is taken off the STORED row rather than from the caller, that the target excludes
        // itself, and that the high-water mark counts what it is handed. That distinction matters
        // because of how the original defect survived — the swap used the visibility-filtered
        // collection read while its test stubbed that read to return the tombstone anyway. A stub
        // that re-implements the predicate reproduces exactly that blind spot.

        /// <summary>
        /// A bad id is the CALLER's fault and must be reported as one. The guard lives inside
        /// TryCatchList, which originally had no Invalid/Unauthorized arm - so the exception fell
        /// through to catch (Exception) and came back as a LinkServiceException, telling
        /// the caller "contact support" and filing an error log for their own bad input.
        /// </summary>
        [Fact]
        public async Task ShouldThrowValidationExceptionOnRetrieveByGroupIdIfGroupIdIsInvalidAsync()
        {
            // given
            this.ambientSecurityContext = CreateAuthenticatedSecurityContext();

            // when
            ValueTask<IReadOnlyList<Link>> retrieveTask =
                this.linkService.RetrieveLinksByGroupIdAsync(
                    Guid.Empty,
                    TestContext.Current.CancellationToken);

            LinkValidationException actualException =
                await Assert.ThrowsAsync<LinkValidationException>(retrieveTask.AsTask);

            // then
            actualException.InnerException.Should().BeOfType<InvalidLinkException>();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinksByGroupIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldReturnWhateverRowHoldsTheGroupSlotAsync()
        {
            // given: a row the caller-facing reads would never show. WHY the slot read returns
            // it — a soft delete never clears IsPublished, and the read is unfiltered on
            // IsDeleted — is not proved against a real catalogue anywhere (#486). What is proved
            // here is that the service hands back whatever that read names, without filtering it
            // a second time.
            var groupId = Guid.Parse("dddddddd-1111-1111-1111-111111111111");
            var tombstoneId = Guid.Parse("dddddddd-2222-2222-2222-222222222222");
            var targetId = Guid.Parse("dddddddd-3333-3333-3333-333333333333");

            Link tombstone = CreateProbeRow(
                id: tombstoneId, groupId: groupId, isPublished: true, isDeleted: true);

            Link target = CreateProbeRow(
                id: targetId, groupId: groupId, isPublished: false, isDeleted: false);

            this.publishedLinkId = tombstoneId;
            SetupProbeStore(tombstone, target);

            // when
            Guid? actualId = await this.linkService.FindPublishedSiblingLinkIdAsync(
                linkId: targetId,
                inboundEnvelope: CreateProbeEnvelope(targetId),
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualId.Should().Be(tombstoneId);
        }


        /// <summary>
        /// THE TARGET MUST NOT FIND ITSELF. The storage read is told which row to ignore, and the
        /// service supplies the target's own id for it — so this seeds a store whose only
        /// slot-holder IS the target. A service that passed the wrong id, or Guid.Empty, gets the
        /// target back and the swap would demote the very row it is promoting.
        ///
        /// <para>The other probe tests cannot catch that: their incumbent is a different row, so
        /// the exclusion never decides the answer.</para>
        /// </summary>
        [Fact]
        public async Task ShouldNotReturnTheTargetItselfAsTheGroupIncumbentAsync()
        {
            // given
            var groupId = Guid.Parse("aaaa1111-1111-1111-1111-111111111111");
            var targetId = Guid.Parse("aaaa1111-3333-3333-3333-333333333333");

            Link publishedTarget = CreateProbeRow(
                id: targetId, groupId: groupId, isPublished: true, isDeleted: false);

            // the storage read would name the TARGET, so only the excluded id can refuse it
            this.publishedLinkId = targetId;
            SetupProbeStore(publishedTarget);

            // when
            Guid? actualId = await this.linkService.FindPublishedSiblingLinkIdAsync(
                linkId: targetId,
                inboundEnvelope: CreateProbeEnvelope(targetId),
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualId.Should().BeNull();

            this.storageBrokerMock.Verify(broker =>
                broker.SelectPublishedLinkInGroupAsync(
                    groupId, targetId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// A free slot answers null rather than reaching for another group's incumbent. The store
        /// deliberately HAS a published row - in a different group - and names it as the row the
        /// slot read would return, so the group the service passes is the only thing that refuses
        /// it. Seeding nothing published would have made this pass whatever the service asked for.
        /// </summary>
        [Fact]
        public async Task ShouldFindNoPublishedRowWhenTheGroupSlotIsFreeAsync()
        {
            // given
            var groupId = Guid.Parse("eeeeeeee-1111-1111-1111-111111111111");
            var targetId = Guid.Parse("eeeeeeee-3333-3333-3333-333333333333");
            var otherGroupIncumbentId = Guid.Parse("eeeeeeee-4444-4444-4444-444444444444");

            this.publishedLinkId = otherGroupIncumbentId;

            SetupProbeStore(
                CreateProbeRow(
                    id: Guid.Parse("eeeeeeee-2222-2222-2222-222222222222"),
                    groupId: groupId, isPublished: false, isDeleted: false),
                CreateProbeRow(
                    id: targetId, groupId: groupId, isPublished: false, isDeleted: false),

                // published, and the slot read would name it - but it is another group's
                CreateProbeRow(
                    id: otherGroupIncumbentId,
                    groupId: Guid.Parse("eeeeeeee-9999-9999-9999-999999999999"),
                    isPublished: true, isDeleted: false));

            // when
            Guid? actualId = await this.linkService.FindPublishedSiblingLinkIdAsync(
                linkId: targetId,
                inboundEnvelope: CreateProbeEnvelope(targetId),
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            actualId.Should().BeNull();

            // and the group it asked about was the STORED row's, not the caller's
            this.storageBrokerMock.Verify(broker =>
                broker.SelectPublishedLinkInGroupAsync(
                    groupId, targetId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ShouldExcludeTheTargetAndOtherGroupsFromThePublishedProbeAsync()
        {
            // given: a decoy for each argument the service chooses - the group, taken off the
            // stored target row, and the target's own id. The IsPublished decoy is left in as
            // documentation of the storage predicate, which no test proves against a real
            // catalogue (#486).
            this.publishedLinkId = Guid.Parse("ffffffff-4444-4444-4444-444444444444");
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
            Guid? actualId = await this.linkService.FindPublishedSiblingLinkIdAsync(
                linkId: targetId,
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
                this.linkService.FindPublishedSiblingLinkIdAsync(
                    linkId: invalidGroupId,
                    inboundEnvelope: CreateProbeEnvelope(invalidGroupId),
                    cancellationToken: TestContext.Current.CancellationToken);

            // then
            await Assert.ThrowsAsync<LinkValidationException>(probeTask.AsTask);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkVersionsInGroupAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // The probe resolves its target by id and then asks the storage layer two NARROW
        // questions - which row holds the group's published slot, and what version numbers the
        // group already owns. The stub answers both over the seeded rows, applying the arguments
        // the SERVICE chooses: the group, which comes off the STORED row rather than from the
        // caller, and the excluded id.
        //
        // The PREDICATES themselves - the unfiltered slot read that lets a tombstone still hold
        // the slot, and the version read that counts tombstones (#271) - moved into IStorageBroker
        // with the await that lets the caller's token reach the database. Their BODIES are not
        // asserted anywhere: #486 kept ContentItemNarrowReadTests as the one canonical proof that
        // reads of this shape translate, and accepted that the link twins go unproved.
        // Which row the STORAGE read would name as the group's published incumbent. Set by a test
        // that cares; left empty otherwise, in which case the probe finds nothing.
        private Guid publishedLinkId;

        private void SetupProbeStore(params Link[] rows)
        {
            // Keyed on the GROUP and the EXCLUDED ID only — the two things the service decides.
            // IsPublished is deliberately NOT evaluated here: that is the storage predicate, and a
            // stub that re-implemented it would pass whether or not the real read still carried it.
            this.storageBrokerMock.Setup(broker =>
                broker.SelectPublishedLinkInGroupAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Guid groupId, Guid excludedLinkId, CancellationToken _) =>
                            rows.FirstOrDefault(row =>
                                row.GroupId == groupId
                                    && row.Id != excludedLinkId
                                    && row.Id == publishedLinkId));

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkVersionsInGroupAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Guid groupId, CancellationToken _) =>
                            rows.Where(row => row.GroupId == groupId)
                                .Select(row => row.Version)
                                .ToList());

            foreach (Link row in rows)
            {
                Link captured = row;

                this.storageBrokerMock.Setup(broker =>
                    broker.SelectLinkByIdAsync(captured.Id, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(captured);
            }
        }

        // The workflow's own envelope: authenticated, system identity, and NO roles — exactly
        // what CreateSystemAsync hands the swap.
        private static EventEnvelope<Link> CreateProbeEnvelope(Guid linkId) =>
            new EventEnvelope<Link>
            {
                Content = new Link { Id = linkId },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() },

                SecurityContext = new SecurityContext
                {
                    IsAuthenticated = true,
                    IsSystemIdentity = true,
                    Roles = []
                }
            };

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

        [Fact]
        public async Task ShouldRefuseTheSiblingProbeForABlockedCallerAsync()
        {
            // given: the contribution gate is the probe's ONLY authorization check, and the
            // probe reads over the UNFILTERED store — so if the gate goes, a blocked caller
            // learns which row holds a group's published slot, tombstones included. Deleting
            // the gate left the whole suite green before this test existed.
            var targetId = Guid.Parse("ba000000-3333-3333-3333-333333333333");

            var blockedEnvelope = new EventEnvelope<Link>
            {
                Content = new Link { Id = targetId },
                Metadata = new EventMetadata { EventId = Guid.NewGuid() },

                SecurityContext = new SecurityContext
                {
                    IsAuthenticated = true,
                    IsSystemIdentity = true,
                    Roles = [Roles.ReadOnly]
                }
            };

            // when
            ValueTask<Guid?> probeTask = this.linkService.FindPublishedSiblingLinkIdAsync(
                linkId: targetId,
                inboundEnvelope: blockedEnvelope,
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            await Assert.ThrowsAsync<LinkValidationException>(probeTask.AsTask);

            // refused BEFORE any read — the gate is not a filter applied to results
            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkVersionsInGroupAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ShouldThrowNotFoundOnSiblingProbeIfTargetIsMissingAsync()
        {
            // given: the case the NotFound clause was added to TryCatchIdentifier for. Without
            // that clause this surfaces as a service exception — "our code is broken" — rather
            // than the validation failure it is.
            var targetId = Guid.Parse("ba111111-3333-3333-3333-333333333333");
            Link missingLink = null;

            this.storageBrokerMock.Setup(broker =>
                broker.SelectLinkByIdAsync(targetId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(missingLink);

            // when
            ValueTask<Guid?> probeTask = this.linkService.FindPublishedSiblingLinkIdAsync(
                linkId: targetId,
                inboundEnvelope: CreateProbeEnvelope(targetId),
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            await Assert.ThrowsAsync<LinkValidationException>(probeTask.AsTask);

            // and it never went on to read the store for an incumbent
            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkVersionsInGroupAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

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

            this.publishedLinkId = incumbentId;
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

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnFindHighestVersionInGroupIfCancellationRequestedAsync()
        {
            // given
            Guid someGroupId = Guid.NewGuid();
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<int> findTask =
                this.linkService.FindHighestVersionInGroupAsync(
                    someGroupId,
                    cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(findTask.AsTask);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkVersionsInGroupAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);

            // pins WHERE the guard sits, not merely that it exists. The operation mints an
            // envelope before it reads anything, so a guard that drifted below that await would
            // still surface OperationCanceledException and still satisfy the storage assertion
            // above — this is the assertion that catches the drift.
            this.eventEnvelopeBrokerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ShouldThrowOperationCanceledExceptionOnFindPublishedSiblingIfCancellationRequestedAsync()
        {
            // given
            Guid someLinkId = Guid.NewGuid();
            EventEnvelope<Link> inboundEnvelope = CreateProbeEnvelope(someLinkId);
            var cancellationToken = new CancellationToken(canceled: true);

            // when
            ValueTask<Guid?> probeTask =
                this.linkService.FindPublishedSiblingLinkIdAsync(
                    linkId: someLinkId,
                    inboundEnvelope: inboundEnvelope,
                    cancellationToken: cancellationToken);

            // then
            await Assert.ThrowsAsync<OperationCanceledException>(probeTask.AsTask);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);

            this.storageBrokerMock.Verify(broker =>
                broker.SelectLinkVersionsInGroupAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
