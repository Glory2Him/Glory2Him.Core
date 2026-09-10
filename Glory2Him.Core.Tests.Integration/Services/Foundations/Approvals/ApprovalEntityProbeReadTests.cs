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
using System.Collections.Generic;
using System.Threading.Tasks;
using EFxceptions.Models.Exceptions;
using FluentAssertions;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Tests.Integration.Brokers;
using Xunit;

namespace Glory2Him.Core.Tests.Integration.Services.Foundations.Approvals
{
    /// <summary>
    /// Proves <see cref="IStorageBroker.SelectApprovalByEntityAsync"/> against a real catalogue.
    ///
    /// <para>The predicate and the ordering used to live in <c>ApprovalService</c>, where the unit
    /// suite proved them against an in-memory queryable. They moved down so the probe could be
    /// awaited and so the cancellation token could reach the database — and LINQ-to-Objects
    /// stopped being able to stand in for SQL. These are the cases that moved with them.</para>
    /// </summary>
    [Collection(NarrowReadIntegrationCollection.Name)]
    public sealed class ApprovalEntityProbeReadTests : IDisposable
    {
        // The probed key, pinned rather than drawn. Link is deliberately NOT the zero member —
        // a defaulted EntityType would be ContentItem and would match by accident, hiding a
        // dropped EntityType conjunct.
        private const EntityType ProbeEntityType = EntityType.Link;
        private const EntityType OtherEntityType = EntityType.Comment;

        private readonly NarrowReadQueryBroker broker;
        private readonly List<Approval> seededApprovals;

        public ApprovalEntityProbeReadTests(NarrowReadQueryBroker broker)
        {
            this.broker = broker;
            this.seededApprovals = new List<Approval>();
        }

        // A plain miss on an unoccupied key used to be asserted here too. #486 removed it as
        // subsumed by the half-key test at the bottom, which is the one with real bite — that one
        // fails if either conjunct is dropped, where a lone miss on an entirely different key
        // does not. That reasoning holds and the miss stays deleted.
        //
        // The plain HIT was removed on the same grounds and put back, because the grounds were
        // wrong. Every other read-back in this file resolves a SOFT-DELETED row: the unfiltered
        // match seeds isDeleted: true, and the index-refusal test asserts on the tombstone that
        // won the key. Mutate the probe to `... && approval.IsDeleted` and all three still pass,
        // while in production every live approval reads as absent and the flow inserts a
        // duplicate the unique index then refuses. This is the only test that says a live row on
        // an occupied key comes back at all.
        [Fact]
        public async Task ShouldReturnTheRowOccupyingTheKeyAsync()
        {
            // given
            Guid probeEntityId = Guid.NewGuid();

            Approval storageApproval = CreateApproval(
                entityType: ProbeEntityType,
                entityId: probeEntityId,
                approvalStatus: ApprovalStatus.Approved,
                isDeleted: false,
                updatedWhen: DateTimeOffset.UtcNow);

            await SeedAsync(storageApproval);

            // when
            Approval match = await this.broker.StorageBroker.SelectApprovalByEntityAsync(
                ProbeEntityType,
                probeEntityId,
                TestContext.Current.CancellationToken);

            // then
            match.Should().NotBeNull();
            match.Id.Should().Be(storageApproval.Id);
            match.IsDeleted.Should().BeFalse();
        }

        /// <summary>
        /// UX_Approvals_EntityType_EntityId is not filtered on IsDeleted, so a soft-deleted row
        /// still OCCUPIES the key (§9.7.2 rule 3). A visibility-filtered read would answer "no
        /// approval" here and invite an insert that could never succeed.
        /// </summary>
        [Fact]
        public async Task ShouldReturnTheSoftDeletedRowBecauseTheProbeIsUnfilteredAsync()
        {
            // given: the ONLY row on the key is soft-deleted
            Guid probeEntityId = Guid.NewGuid();

            Approval softDeletedApproval = CreateApproval(
                entityType: ProbeEntityType,
                entityId: probeEntityId,
                approvalStatus: ApprovalStatus.Rejected,
                isDeleted: true,
                updatedWhen: DateTimeOffset.UtcNow);

            await SeedAsync(softDeletedApproval);

            // when
            Approval match = await this.broker.StorageBroker.SelectApprovalByEntityAsync(
                ProbeEntityType,
                probeEntityId,
                TestContext.Current.CancellationToken);

            // then: the closed row surfaces, so the flow reinstates it in place (§12.4.4 BR14)
            match.Should().NotBeNull();
            match.Id.Should().Be(softDeletedApproval.Id);
            match.IsDeleted.Should().BeTrue();
        }

        /// <summary>
        /// The probe orders by IsDeleted to prefer a live row over a soft-deleted one sharing the
        /// key. Against a real catalogue that tie CANNOT ARISE, and this is what says so: the
        /// unique index carries no IsDeleted filter, so the second row is refused outright.
        ///
        /// <para>The ordering is therefore belt and braces rather than a rule with a case behind
        /// it — deliberately kept, because it costs nothing and makes the answer deterministic
        /// rather than dependent on the constraint holding. The unit suite used to assert this
        /// against an in-memory list, where a store the database will not accept is trivial to
        /// build; that scenario was never reachable.</para>
        ///
        /// <para>Contrast <c>UX_Associations_Pair</c>, which IS filtered on IsDeleted — there a
        /// live row and a tombstone genuinely can share a pair, and the association read's
        /// ordering is exercised on real rows.</para>
        /// </summary>
        [Fact]
        public async Task ShouldRefuseASecondRowOnTheKeyEvenWhenTheFirstIsSoftDeletedAsync()
        {
            // given: a soft-deleted row already occupying the key
            Guid probeEntityId = Guid.NewGuid();

            Approval deletedApproval = CreateApproval(
                entityType: ProbeEntityType,
                entityId: probeEntityId,
                approvalStatus: ApprovalStatus.Dismissed,
                isDeleted: true,
                updatedWhen: new DateTimeOffset(2025, 9, 6, 0, 0, 0, TimeSpan.Zero));

            await SeedAsync(deletedApproval);

            Approval liveApproval = CreateApproval(
                entityType: ProbeEntityType,
                entityId: probeEntityId,
                approvalStatus: ApprovalStatus.Submitted,
                isDeleted: false,
                updatedWhen: new DateTimeOffset(2021, 3, 4, 0, 0, 0, TimeSpan.Zero));

            // when: through TryInsertAsync, which detaches the rejected row — left tracked in the
            // Added state it would be retried by the next save anywhere in this collection
            Exception actualException = await this.broker.TryInsertAsync(liveApproval);

            // then: the INDEX refused it, not something incidental. Asserting on any Exception
            // would pass on a mapping fault or a dropped connection and prove nothing about the
            // constraint this test exists to pin.
            actualException.Should().BeOfType<DuplicateKeyWithUniqueIndexException>();

            actualException.Message.Should().Contain(
                "UX_Approvals_EntityType_EntityId",
                because: "the key is refused by that index, which is what makes the tie the probe "
                    + "orders around unreachable");

            Approval match = await this.broker.StorageBroker.SelectApprovalByEntityAsync(
                ProbeEntityType,
                probeEntityId,
                TestContext.Current.CancellationToken);

            match.Should().NotBeNull();
            match.Id.Should().Be(deletedApproval.Id);
        }

        /// <summary>
        /// Dropping either conjunct of the match would report a key as occupied when it is free.
        /// </summary>
        [Fact]
        public async Task ShouldNotMatchARowThatSharesOnlyOneHalfOfTheKeyAsync()
        {
            // given: one row shares the entity id but carries a different entity type, the other
            // shares the entity type but a different entity id
            Guid probeEntityId = Guid.NewGuid();

            Approval sameEntityIdOnlyApproval = CreateApproval(
                entityType: OtherEntityType,
                entityId: probeEntityId,
                approvalStatus: ApprovalStatus.Approved,
                isDeleted: false,
                updatedWhen: DateTimeOffset.UtcNow);

            Approval sameEntityTypeOnlyApproval = CreateApproval(
                entityType: ProbeEntityType,
                entityId: Guid.NewGuid(),
                approvalStatus: ApprovalStatus.Submitted,
                isDeleted: false,
                updatedWhen: DateTimeOffset.UtcNow);

            await SeedAsync(sameEntityIdOnlyApproval, sameEntityTypeOnlyApproval);

            // when
            Approval match = await this.broker.StorageBroker.SelectApprovalByEntityAsync(
                ProbeEntityType,
                probeEntityId,
                TestContext.Current.CancellationToken);

            // then
            match.Should().BeNull();
        }

        private static Approval CreateApproval(
            EntityType entityType,
            Guid entityId,
            ApprovalStatus approvalStatus,
            bool isDeleted,
            DateTimeOffset updatedWhen)
        {
            string actorUserId = Guid.NewGuid().ToString();

            return new Approval
            {
                Id = Guid.NewGuid(),
                EntityType = entityType,
                EntityId = entityId,
                ApprovalStatus = approvalStatus,
                IsDeleted = isDeleted,
                DeletedBy = isDeleted ? actorUserId : null,
                DeletedWhen = isDeleted ? updatedWhen : null,
                DeletionReason = isDeleted ? "seeded" : null,
                CreatedBy = actorUserId,
                CreatedWhen = updatedWhen,
                UpdatedBy = actorUserId,
                UpdatedWhen = updatedWhen,
            };
        }

        private async Task SeedAsync(params Approval[] approvals)
        {
            await this.broker.SeedAsync(approvals);
            this.seededApprovals.AddRange(approvals);
        }

        public void Dispose() =>
            this.broker.ClearAsync(this.seededApprovals).AsTask().GetAwaiter().GetResult();
    }
}
