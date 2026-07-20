// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’" 
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6 
// ────────────────────────────────────────────────────────────────────────────────

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EFxceptions;
using G2H.StorageClient.Clients;
using Glory2Him.Core.Models.Foundations.ApprovalComments;
using Glory2Him.Core.Models.Foundations.ApprovalReviews;
using Glory2Him.Core.Models.Foundations.Approvals;
using Glory2Him.Core.Models.Foundations.ApprovalSettingRoles;
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.Core.Models.Foundations.Attachments;
using Glory2Him.Core.Models.Foundations.BibleReferences;
using Glory2Him.Core.Models.Foundations.Comments;
using Glory2Him.Core.Models.Foundations.ContentItemAssociations;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Foundations.ContentTypes;
using Glory2Him.Core.Models.Foundations.Links;
using Glory2Him.Core.Models.Foundations.ProcessedEvents;
using Glory2Him.Core.Models.Foundations.Reactions;
using Glory2Him.Core.Models.Foundations.Tags;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Glory2Him.Core.Brokers.Storages.Sql
{
    public partial class StorageBroker : EFxceptionsContext, IStorageBroker
    {
        private readonly IConfiguration configuration;
        private readonly IEFCoreClient efCoreClient;

        public StorageBroker(IConfiguration configuration)
        {
            this.configuration = configuration;
            efCoreClient = new EFCoreClient(this);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

            string connectionString = configuration
                .GetConnectionString(name: "Glory2HimConnectionString") ?? string.Empty;

            optionsBuilder.UseSqlServer(connectionString, config => config.UseHierarchyId());
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            AddConfigurations(modelBuilder);
        }

        private static void AddConfigurations(ModelBuilder modelBuilder)
        {
            AddApprovalConfigurations(modelBuilder.Entity<Approval>());
            AddApprovalCommentConfigurations(modelBuilder.Entity<ApprovalComment>());
            AddApprovalReviewConfigurations(modelBuilder.Entity<ApprovalReview>());
            AddApprovalSettingConfigurations(modelBuilder.Entity<ApprovalSetting>());
            AddApprovalSettingRoleConfigurations(modelBuilder.Entity<ApprovalSettingRole>());
            AddAttachmentConfigurations(modelBuilder.Entity<Attachment>());
            AddBibleReferenceConfigurations(modelBuilder.Entity<BibleReference>());
            AddCommentConfigurations(modelBuilder.Entity<Comment>());
            AddContentItemConfigurations(modelBuilder.Entity<ContentItem>());
            AddContentItemAssociationConfigurations(modelBuilder.Entity<ContentItemAssociation>());
            AddContentItemSettingConfigurations(modelBuilder.Entity<ContentItemSetting>());
            AddContentTypeConfigurations(modelBuilder.Entity<ContentType>());
            AddLinkConfigurations(modelBuilder.Entity<Link>());
            AddProcessedEventConfigurations(modelBuilder.Entity<ProcessedEvent>());
            AddReactionConfigurations(modelBuilder.Entity<Reaction>());
            AddTagConfigurations(modelBuilder.Entity<Tag>());
        }

        private async ValueTask<T> InsertAsync<T>(T @object, CancellationToken cancellationToken = default)
            where T : class =>
                await efCoreClient.InsertAsync(@object, cancellationToken);

        private async ValueTask<IQueryable<T>> SelectAllAsync<T>(CancellationToken cancellationToken = default)
            where T : class =>
                await efCoreClient.SelectAllAsync<T>(cancellationToken);

        private async ValueTask<T> SelectAsync<T>(object[] @objectIds, CancellationToken cancellationToken = default)
            where T : class =>
                await efCoreClient.SelectAsync<T>(@objectIds, cancellationToken);

        private async ValueTask<T> UpdateAsync<T>(T @object, CancellationToken cancellationToken = default)
            where T : class =>
                await efCoreClient.UpdateAsync(@object, cancellationToken);

        private async ValueTask<T> DeleteAsync<T>(T @object, CancellationToken cancellationToken = default)
            where T : class =>
                await efCoreClient.DeleteAsync(@object, cancellationToken);

        private async ValueTask BulkInsertAsync<T>(
            IEnumerable<T> objects,
            bool useTransaction = true,
            CancellationToken cancellationToken = default)
                where T : class =>
                    await efCoreClient.BulkInsertAsync<T>(objects, useTransaction, cancellationToken);

        private async ValueTask<IEnumerable<T>> BulkReadAsync<T>(
            IEnumerable<T> objects,
            CancellationToken cancellationToken = default)
                where T : class =>
                    await efCoreClient.BulkReadAsync<T>(objects, cancellationToken);

        private async ValueTask BulkUpdateAsync<T>(
            IEnumerable<T> objects,
            bool useTransaction = true,
            CancellationToken cancellationToken = default)
                where T : class =>
                    await efCoreClient.BulkUpdateAsync<T>(objects, useTransaction, cancellationToken);

        private async ValueTask BulkDeleteAsync<T>(
            IEnumerable<T> objects,
            bool useTransaction = true,
            CancellationToken cancellationToken = default)
                where T : class =>
                    await efCoreClient.BulkDeleteAsync<T>(objects, useTransaction, cancellationToken);

        private async ValueTask BulkUpsertAsync<T>(
            IEnumerable<T> objects,
            bool useTransaction = true,
            CancellationToken cancellationToken = default)
                where T : class =>
                    await efCoreClient.BulkUpsertAsync<T>(objects, useTransaction, cancellationToken);

        private async ValueTask<bool> ExistsAsync<T>(
            object[] objectIds,
            CancellationToken cancellationToken = default)
                where T : class =>
                    await efCoreClient.ExistsAsync<T>(objectIds, cancellationToken);
    }
}