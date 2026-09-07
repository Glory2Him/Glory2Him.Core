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
using FluentAssertions;
using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Tests.Integration.Brokers;
using Xunit;

namespace Glory2Him.Core.Tests.Integration.Services.Foundations.Associations
{
    /// <summary>
    /// Proves the association NARROW READS against a real catalogue: the exact-pair probe, the
    /// overlap probe and the scope-change duplicate check.
    ///
    /// <para>These predicates key on <c>EntityAEffectiveId</c> and <c>EntityBEffectiveId</c>,
    /// which are PERSISTED COMPUTED columns — the value the read matches on is produced by the
    /// database, not by the row the test hands it. That alone is a reason these belong here: an
    /// in-memory queryable would compare a default Guid on both sides and agree with itself.</para>
    /// </summary>
    [Collection(NarrowReadIntegrationCollection.Name)]
    public sealed class AssociationNarrowReadTests : IDisposable
    {
        private readonly NarrowReadQueryBroker broker;
        private readonly List<Association> seededAssociations;

        public AssociationNarrowReadTests(NarrowReadQueryBroker broker)
        {
            this.broker = broker;
            this.seededAssociations = new List<Association>();
        }

        [Fact]
        public async Task ShouldReturnTheRowOccupyingThePairAsync()
        {
            // given
            string userId = Guid.NewGuid().ToString();
            Guid entityAGroupId = Guid.NewGuid();
            Guid entityBGroupId = Guid.NewGuid();

            Association storageAssociation = CreateAssociation(
                userId: userId,
                entityAGroupId: entityAGroupId,
                entityBGroupId: entityBGroupId);

            await SeedAsync(storageAssociation);

            // when: AllVersions on both endpoints, so each effective id is its group id
            Association match = await this.broker.StorageBroker.SelectAssociationByPairAsync(
                EntityType.ContentItem,
                EntityType.Tag,
                entityAGroupId,
                entityBGroupId,
                userId,
                TestContext.Current.CancellationToken);

            // then
            match.Should().NotBeNull();
            match.Id.Should().Be(storageAssociation.Id);
        }

        /// <summary>
        /// The retrieve-or-add flow must see a soft-deleted row: it still occupies the pair under
        /// the takedown it records, and a filtered probe would launder it (§7.4/§14.6).
        /// </summary>
        [Fact]
        public async Task ShouldSeeASoftDeletedRowBecauseTheProbeIsUnfilteredAsync()
        {
            // given
            string userId = Guid.NewGuid().ToString();
            Guid entityAGroupId = Guid.NewGuid();
            Guid entityBGroupId = Guid.NewGuid();

            Association takedownAssociation = CreateAssociation(
                userId: userId,
                entityAGroupId: entityAGroupId,
                entityBGroupId: entityBGroupId);

            takedownAssociation.IsDeleted = true;

            await SeedAsync(takedownAssociation);

            // when
            Association match = await this.broker.StorageBroker.SelectAssociationByPairAsync(
                EntityType.ContentItem,
                EntityType.Tag,
                entityAGroupId,
                entityBGroupId,
                userId,
                TestContext.Current.CancellationToken);

            // then
            match.Should().NotBeNull();
            match.Id.Should().Be(takedownAssociation.Id);
            match.IsDeleted.Should().BeTrue();
        }

        /// <summary>
        /// The ordering is what the server has to get right: OrderBy(IsDeleted) prefers the live
        /// row, and the live row here is deliberately the OLDER of the two so a match decided by
        /// recency alone would return the deleted one.
        /// </summary>
        [Fact]
        public async Task ShouldPreferTheLiveRowOverASoftDeletedOneOnTheSamePairAsync()
        {
            // given
            string userId = Guid.NewGuid().ToString();
            Guid entityAGroupId = Guid.NewGuid();
            Guid entityBGroupId = Guid.NewGuid();

            Association liveAssociation = CreateAssociation(
                userId: userId,
                entityAGroupId: entityAGroupId,
                entityBGroupId: entityBGroupId);

            liveAssociation.UpdatedWhen = new DateTimeOffset(2021, 3, 4, 0, 0, 0, TimeSpan.Zero);

            Association deletedAssociation = CreateAssociation(
                userId: userId,
                entityAGroupId: entityAGroupId,
                entityBGroupId: entityBGroupId);

            deletedAssociation.IsDeleted = true;
            deletedAssociation.UpdatedWhen = new DateTimeOffset(2025, 9, 6, 0, 0, 0, TimeSpan.Zero);

            await SeedAsync(deletedAssociation, liveAssociation);

            // when
            Association match = await this.broker.StorageBroker.SelectAssociationByPairAsync(
                EntityType.ContentItem,
                EntityType.Tag,
                entityAGroupId,
                entityBGroupId,
                userId,
                TestContext.Current.CancellationToken);

            // then
            match.Should().NotBeNull();
            match.Id.Should().Be(liveAssociation.Id);
            match.IsDeleted.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldMatchOnlyTheRowWithTheSameUserIdWhenPairsCollideOnUserAsync()
        {
            // given: an identical pair belonging to somebody else. UserId is part of the key, so
            // dropping it would hand one person another person's association.
            Guid entityAGroupId = Guid.NewGuid();
            Guid entityBGroupId = Guid.NewGuid();

            Association othersAssociation = CreateAssociation(
                userId: Guid.NewGuid().ToString(),
                entityAGroupId: entityAGroupId,
                entityBGroupId: entityBGroupId);

            await SeedAsync(othersAssociation);

            // when
            Association match = await this.broker.StorageBroker.SelectAssociationByPairAsync(
                EntityType.ContentItem,
                EntityType.Tag,
                entityAGroupId,
                entityBGroupId,
                Guid.NewGuid().ToString(),
                TestContext.Current.CancellationToken);

            // then
            match.Should().BeNull();
        }

        /// <summary>
        /// An endpoint's coverage intersects when either side spans AllVersions — the whole group
        /// contains the other's version — which is the branch a plain effective-id equality would
        /// miss entirely.
        /// </summary>
        [Fact]
        public async Task ShouldReturnOverlapWhenAllVersionsRequestSpansAThisVersionOnlyRowAsync()
        {
            // given: a stored row pinned to ONE version of the group
            string userId = Guid.NewGuid().ToString();
            Guid entityAGroupId = Guid.NewGuid();
            Guid entityAKeyId = Guid.NewGuid();
            Guid entityBGroupId = Guid.NewGuid();

            Association pinnedAssociation = CreateAssociation(
                userId: userId,
                entityAGroupId: entityAGroupId,
                entityBGroupId: entityBGroupId);

            pinnedAssociation.EntityAScope = Scope.ThisVersionOnly;
            pinnedAssociation.EntityAKeyId = entityAKeyId;

            await SeedAsync(pinnedAssociation);

            // when: the request spans every version of the same group
            Association match = await this.broker.StorageBroker.SelectOverlappingAssociationAsync(
                entityAType: EntityType.ContentItem,
                entityBType: EntityType.Tag,
                userId: userId,
                entityAGroupId: entityAGroupId,
                entityBGroupId: entityBGroupId,
                entityAScope: Scope.AllVersions,
                entityBScope: Scope.AllVersions,
                entityAEffectiveId: entityAGroupId,
                entityBEffectiveId: entityBGroupId,
                excludedAssociationId: null,
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            match.Should().NotBeNull();
            match.Id.Should().Be(pinnedAssociation.Id);
        }

        /// <summary>
        /// Two ThisVersionOnly endpoints on DIFFERENT versions of one group do NOT overlap —
        /// other versions do not inherit — so the read must not flag them.
        /// </summary>
        [Fact]
        public async Task ShouldNotReturnOverlapForTwoDifferentPinnedVersionsAsync()
        {
            // given
            string userId = Guid.NewGuid().ToString();
            Guid entityAGroupId = Guid.NewGuid();
            Guid entityBGroupId = Guid.NewGuid();

            Association pinnedAssociation = CreateAssociation(
                userId: userId,
                entityAGroupId: entityAGroupId,
                entityBGroupId: entityBGroupId);

            pinnedAssociation.EntityAScope = Scope.ThisVersionOnly;
            pinnedAssociation.EntityAKeyId = Guid.NewGuid();

            await SeedAsync(pinnedAssociation);

            // when: the request pins a DIFFERENT version of the same group
            Association match = await this.broker.StorageBroker.SelectOverlappingAssociationAsync(
                entityAType: EntityType.ContentItem,
                entityBType: EntityType.Tag,
                userId: userId,
                entityAGroupId: entityAGroupId,
                entityBGroupId: entityBGroupId,
                entityAScope: Scope.ThisVersionOnly,
                entityBScope: Scope.AllVersions,
                entityAEffectiveId: Guid.NewGuid(),
                entityBEffectiveId: entityBGroupId,
                excludedAssociationId: null,
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            match.Should().BeNull();
        }

        /// <summary>
        /// Only LIVE rows can double-render, so the overlap probe excludes tombstones — the one
        /// place its posture differs from the exact-pair probe.
        /// </summary>
        [Fact]
        public async Task ShouldNotReturnASoftDeletedRowAsAnOverlapAsync()
        {
            // given
            string userId = Guid.NewGuid().ToString();
            Guid entityAGroupId = Guid.NewGuid();
            Guid entityBGroupId = Guid.NewGuid();

            Association deletedAssociation = CreateAssociation(
                userId: userId,
                entityAGroupId: entityAGroupId,
                entityBGroupId: entityBGroupId);

            deletedAssociation.IsDeleted = true;

            await SeedAsync(deletedAssociation);

            // when
            Association match = await this.broker.StorageBroker.SelectOverlappingAssociationAsync(
                entityAType: EntityType.ContentItem,
                entityBType: EntityType.Tag,
                userId: userId,
                entityAGroupId: entityAGroupId,
                entityBGroupId: entityBGroupId,
                entityAScope: Scope.AllVersions,
                entityBScope: Scope.AllVersions,
                entityAEffectiveId: entityAGroupId,
                entityBEffectiveId: entityBGroupId,
                excludedAssociationId: null,
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            match.Should().BeNull();
        }

        [Fact]
        public async Task ShouldNotReturnTheExcludedRowAsAnOverlapAsync()
        {
            // given: the row being moved must not overlap itself
            string userId = Guid.NewGuid().ToString();
            Guid entityAGroupId = Guid.NewGuid();
            Guid entityBGroupId = Guid.NewGuid();

            Association movingAssociation = CreateAssociation(
                userId: userId,
                entityAGroupId: entityAGroupId,
                entityBGroupId: entityBGroupId);

            await SeedAsync(movingAssociation);

            // when
            Association match = await this.broker.StorageBroker.SelectOverlappingAssociationAsync(
                entityAType: EntityType.ContentItem,
                entityBType: EntityType.Tag,
                userId: userId,
                entityAGroupId: entityAGroupId,
                entityBGroupId: entityBGroupId,
                entityAScope: Scope.AllVersions,
                entityBScope: Scope.AllVersions,
                entityAEffectiveId: entityAGroupId,
                entityBEffectiveId: entityBGroupId,
                excludedAssociationId: movingAssociation.Id,
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            match.Should().BeNull();
        }

        /// <summary>
        /// The scope-change duplicate check: UX_Associations_Pair keys on the EFFECTIVE id, which
        /// a scope toggle recomputes, so the row can move onto a key another row already holds.
        /// </summary>
        [Fact]
        public async Task ShouldReportThePairAsOccupiedByAnotherLiveRowAsync()
        {
            // given
            string userId = Guid.NewGuid().ToString();
            Guid entityAGroupId = Guid.NewGuid();
            Guid entityBGroupId = Guid.NewGuid();

            Association incumbentAssociation = CreateAssociation(
                userId: userId,
                entityAGroupId: entityAGroupId,
                entityBGroupId: entityBGroupId);

            await SeedAsync(incumbentAssociation);

            // when: a DIFFERENT row asks whether the key it would move onto is free
            bool isOccupied = await this.broker.StorageBroker.ExistsLiveAssociationOnPairAsync(
                entityAType: EntityType.ContentItem,
                entityBType: EntityType.Tag,
                userId: userId,
                entityAEffectiveId: entityAGroupId,
                entityBEffectiveId: entityBGroupId,
                excludedAssociationId: Guid.NewGuid(),
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            isOccupied.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldNotReportThePairAsOccupiedByTheRowBeingMovedAsync()
        {
            // given: the only row on the key is the one asking
            string userId = Guid.NewGuid().ToString();
            Guid entityAGroupId = Guid.NewGuid();
            Guid entityBGroupId = Guid.NewGuid();

            Association movingAssociation = CreateAssociation(
                userId: userId,
                entityAGroupId: entityAGroupId,
                entityBGroupId: entityBGroupId);

            await SeedAsync(movingAssociation);

            // when
            bool isOccupied = await this.broker.StorageBroker.ExistsLiveAssociationOnPairAsync(
                entityAType: EntityType.ContentItem,
                entityBType: EntityType.Tag,
                userId: userId,
                entityAEffectiveId: entityAGroupId,
                entityBEffectiveId: entityBGroupId,
                excludedAssociationId: movingAssociation.Id,
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            isOccupied.Should().BeFalse();
        }

        [Fact]
        public async Task ShouldNotReportThePairAsOccupiedByASoftDeletedRowAsync()
        {
            // given: the unique index filters WHERE IsDeleted = 0, so a tombstone does not block
            // the move
            string userId = Guid.NewGuid().ToString();
            Guid entityAGroupId = Guid.NewGuid();
            Guid entityBGroupId = Guid.NewGuid();

            Association deletedAssociation = CreateAssociation(
                userId: userId,
                entityAGroupId: entityAGroupId,
                entityBGroupId: entityBGroupId);

            deletedAssociation.IsDeleted = true;

            await SeedAsync(deletedAssociation);

            // when
            bool isOccupied = await this.broker.StorageBroker.ExistsLiveAssociationOnPairAsync(
                entityAType: EntityType.ContentItem,
                entityBType: EntityType.Tag,
                userId: userId,
                entityAEffectiveId: entityAGroupId,
                entityBEffectiveId: entityBGroupId,
                excludedAssociationId: Guid.NewGuid(),
                cancellationToken: TestContext.Current.CancellationToken);

            // then
            isOccupied.Should().BeFalse();
        }

        // AllVersions on both endpoints, so each EFFECTIVE id the database computes is that
        // endpoint's group id — which is what the reads above are keyed on.
        private static Association CreateAssociation(
            string userId,
            Guid entityAGroupId,
            Guid entityBGroupId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            return new Association
            {
                Id = Guid.NewGuid(),
                EntityAType = EntityType.ContentItem,
                EntityAContentType = ContentType.Testimony,
                EntityAGroupId = entityAGroupId,
                EntityAKeyId = Guid.NewGuid(),
                EntityAScope = Scope.AllVersions,
                EntityBType = EntityType.Tag,
                EntityBGroupId = entityBGroupId,
                EntityBKeyId = Guid.NewGuid(),
                EntityBScope = Scope.AllVersions,
                UserId = userId,
                ApprovalStatus = ApprovalStatus.Draft,
                CreatedBy = userId,
                CreatedWhen = now,
                UpdatedBy = userId,
                UpdatedWhen = now,
                DeletedBy = null,
                DeletedWhen = null,
            };
        }

        private async Task SeedAsync(params Association[] associations)
        {
            await this.broker.SeedAsync(associations);
            this.seededAssociations.AddRange(associations);
        }

        public void Dispose() =>
            this.broker.ClearAsync(this.seededAssociations).AsTask().GetAwaiter().GetResult();
    }
}
