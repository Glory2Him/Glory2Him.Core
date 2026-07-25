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
using EFxceptions;
using Glory2Him.Core.Models.Foundations.ApprovalSettingPublisherRoles;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker : EFxceptionsContext, IStorageBroker
    {
        public DbSet<ApprovalSettingPublisherRole> ApprovalSettingPublisherRoles { get; set; }

        public async ValueTask<ApprovalSettingPublisherRole> InsertApprovalSettingPublisherRoleAsync(
            ApprovalSettingPublisherRole approvalSettingPublisherRole,
            CancellationToken cancellationToken = default) =>
            await InsertAsync(approvalSettingPublisherRole, cancellationToken);

        public async ValueTask<IQueryable<ApprovalSettingPublisherRole>> SelectAllApprovalSettingPublisherRolesAsync(
            CancellationToken cancellationToken = default) =>
            await SelectAllAsync<ApprovalSettingPublisherRole>(cancellationToken);

        public async ValueTask<ApprovalSettingPublisherRole> SelectApprovalSettingPublisherRoleByIdAsync(
            Guid approvalSettingPublisherRoleId,
            CancellationToken cancellationToken = default) =>
            await SelectAsync<ApprovalSettingPublisherRole>(new object[] { approvalSettingPublisherRoleId }, cancellationToken);

        public async ValueTask<ApprovalSettingPublisherRole> UpdateApprovalSettingPublisherRoleAsync(
            ApprovalSettingPublisherRole approvalSettingPublisherRole,
            CancellationToken cancellationToken = default) =>
            await UpdateAsync(approvalSettingPublisherRole, cancellationToken);

        public async ValueTask<ApprovalSettingPublisherRole> DeleteApprovalSettingPublisherRoleAsync(
            ApprovalSettingPublisherRole approvalSettingPublisherRole,
            CancellationToken cancellationToken = default) =>
            await DeleteAsync(approvalSettingPublisherRole, cancellationToken);

        public async ValueTask BulkInsertApprovalSettingPublisherRolesAsync(
            List<ApprovalSettingPublisherRole> approvalSettingPublisherRoles,
            CancellationToken cancellationToken = default) =>
            await BulkInsertAsync(approvalSettingPublisherRoles, cancellationToken: cancellationToken);

        public async ValueTask BulkUpdateApprovalSettingPublisherRolesAsync(
            List<ApprovalSettingPublisherRole> approvalSettingPublisherRoles,
            CancellationToken cancellationToken = default) =>
            await BulkUpdateAsync(approvalSettingPublisherRoles, cancellationToken: cancellationToken);

        public async ValueTask BulkDeleteApprovalSettingPublisherRolesAsync(
            List<ApprovalSettingPublisherRole> approvalSettingPublisherRoles,
            CancellationToken cancellationToken = default) =>
            await BulkDeleteAsync(approvalSettingPublisherRoles, cancellationToken: cancellationToken);

        public async ValueTask<IEnumerable<ApprovalSettingPublisherRole>> BulkReadApprovalSettingPublisherRolesAsync(
            List<ApprovalSettingPublisherRole> approvalSettingPublisherRoles,
            CancellationToken cancellationToken = default) =>
            await BulkReadAsync(approvalSettingPublisherRoles, cancellationToken);

        public async ValueTask BulkUpsertApprovalSettingPublisherRolesAsync(
            List<ApprovalSettingPublisherRole> approvalSettingPublisherRoles,
            CancellationToken cancellationToken = default) =>
            await BulkUpsertAsync(approvalSettingPublisherRoles, cancellationToken: cancellationToken);

        public async ValueTask<bool> ExistsApprovalSettingPublisherRoleAsync(
            Guid approvalSettingPublisherRoleId,
            CancellationToken cancellationToken = default) =>
            await ExistsAsync<ApprovalSettingPublisherRole>(new object[] { approvalSettingPublisherRoleId }, cancellationToken);
    }
}
