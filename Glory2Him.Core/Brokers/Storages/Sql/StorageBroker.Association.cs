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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Associations;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    internal partial class StorageBroker
    {
        public DbSet<Association> Associations { get; set; }

        public async ValueTask<Association> InsertAssociationAsync(
            Association association, CancellationToken cancellationToken = default) =>
                await InsertAsync(association, cancellationToken);

        public async ValueTask<Association?> SelectAssociationByPairAsync(
            EntityType entityAType,
            EntityType entityBType,
            Guid entityAEffectiveId,
            Guid entityBEffectiveId,
            string userId,
            CancellationToken cancellationToken = default) =>
            await Associations
                .Where(association =>
                    association.EntityAType == entityAType
                        && association.EntityBType == entityBType
                        && association.EntityAEffectiveId == entityAEffectiveId
                        && association.EntityBEffectiveId == entityBEffectiveId
                        && association.UserId == userId)
                .OrderBy(association => association.IsDeleted)
                .ThenByDescending(association => association.UpdatedWhen)
                .FirstOrDefaultAsync(cancellationToken);

        public async ValueTask<Association?> SelectOverlappingAssociationAsync(
            EntityType entityAType,
            EntityType entityBType,
            string userId,
            Guid entityAGroupId,
            Guid entityBGroupId,
            Scope entityAScope,
            Scope entityBScope,
            Guid entityAEffectiveId,
            Guid entityBEffectiveId,
            Guid? excludedAssociationId = null,
            CancellationToken cancellationToken = default) =>
            await Associations
                .Where(association =>
                    association.IsDeleted == false
                        && (excludedAssociationId == null
                            || association.Id != excludedAssociationId)
                        && association.EntityAType == entityAType
                        && association.EntityBType == entityBType
                        && association.UserId == userId
                        && association.EntityAGroupId == entityAGroupId
                        && association.EntityBGroupId == entityBGroupId
                        && (entityAScope == Scope.AllVersions
                            || association.EntityAScope == Scope.AllVersions
                            || association.EntityAEffectiveId == entityAEffectiveId)
                        && (entityBScope == Scope.AllVersions
                            || association.EntityBScope == Scope.AllVersions
                            || association.EntityBEffectiveId == entityBEffectiveId))
                .OrderByDescending(association => association.UpdatedWhen)
                .FirstOrDefaultAsync(cancellationToken);

        public async ValueTask<bool> ExistsLiveAssociationOnPairAsync(
            EntityType entityAType,
            EntityType entityBType,
            string userId,
            Guid entityAEffectiveId,
            Guid entityBEffectiveId,
            Guid excludedAssociationId,
            CancellationToken cancellationToken = default) =>
            await Associations
                .AnyAsync(
                    association =>
                        association.Id != excludedAssociationId
                            && association.IsDeleted == false
                            && association.EntityAType == entityAType
                            && association.EntityBType == entityBType
                            && association.UserId == userId
                            && association.EntityAEffectiveId == entityAEffectiveId
                            && association.EntityBEffectiveId == entityBEffectiveId,
                    cancellationToken);

        public async ValueTask<IQueryable<Association>> SelectAllAssociationsAsync(
            CancellationToken cancellationToken = default) =>
            await SelectAllAsync<Association>(cancellationToken);

        public async ValueTask<Association> SelectAssociationByIdAsync(
            Guid associationId, CancellationToken cancellationToken = default) =>
                await SelectAsync<Association>(new object[] { associationId }, cancellationToken);

        public async ValueTask<Association> UpdateAssociationAsync(
            Association association, CancellationToken cancellationToken = default) =>
                await UpdateAsync(association, cancellationToken);

        public async ValueTask<Association> DeleteAssociationAsync(
            Association association, CancellationToken cancellationToken = default) =>
                await DeleteAsync(association, cancellationToken);

        public async ValueTask BulkInsertAssociationsAsync(
            List<Association> associations, CancellationToken cancellationToken = default) =>
                await BulkInsertAsync(associations, cancellationToken: cancellationToken);

        public async ValueTask BulkUpdateAssociationsAsync(
            List<Association> associations, CancellationToken cancellationToken = default) =>
                await BulkUpdateAsync(associations, cancellationToken: cancellationToken);

        public async ValueTask BulkDeleteAssociationsAsync(
            List<Association> associations, CancellationToken cancellationToken = default) =>
                await BulkDeleteAsync(associations, cancellationToken: cancellationToken);

        public async ValueTask<IEnumerable<Association>> BulkReadAssociationsAsync(
            List<Association> associations,
            CancellationToken cancellationToken = default) =>
            await BulkReadAsync(associations, cancellationToken);

        public async ValueTask BulkUpsertAssociationsAsync(
            List<Association> associations,
            CancellationToken cancellationToken = default) =>
            await BulkUpsertAsync(associations, cancellationToken: cancellationToken);

        public async ValueTask<bool> ExistsAssociationAsync(
            Guid associationId,
            CancellationToken cancellationToken = default) =>
            await ExistsAsync<Association>(new object[] { associationId }, cancellationToken);
    }
}