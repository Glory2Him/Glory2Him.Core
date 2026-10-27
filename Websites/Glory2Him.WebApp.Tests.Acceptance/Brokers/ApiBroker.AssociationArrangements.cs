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
using System.Threading.Tasks;
using Glory2Him.Core.Models.Configurations;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.Approvals;
using Microsoft.EntityFrameworkCore;
using CoreAssociation = Glory2Him.Core.Models.Foundations.Associations.Association;
using CoreContentItem = Glory2Him.Core.Models.Foundations.ContentItems.ContentItem;
using CoreTag = Glory2Him.Core.Models.Foundations.Tags.Tag;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    /// <summary>
    /// Association rows arranged and torn down beneath HTTP — the sibling of
    /// <c>ApiBroker.ReactionArrangements.cs</c>. No endpoint removes an association physically.
    /// </summary>
    public partial class ApiBroker
    {
        public async ValueTask<CoreAssociation> GetCoreAssociationByIdAsync(Guid associationId) =>
            await this.storageBroker.SelectAssociationByIdAsync(associationId);

        /// <summary>
        /// An editorial tag on a post, approved and published, as the approval workflow leaves
        /// one. Beneath HTTP because the upsert writes a tag pairing for review and no endpoint
        /// approves it. Its endpoint columns are the ones the foundation's add derives: the
        /// post's group under its default scope, and the tag's own id under its.
        /// </summary>
        public async ValueTask<CoreAssociation> InsertPublishedEditorialTagAssociationAsync(
            CoreContentItem contentItem,
            CoreTag tag,
            string authorUserId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var association = new CoreAssociation
            {
                Id = Guid.NewGuid(),
                EntityAType = EntityType.ContentItem,
                EntityAKeyId = contentItem.Id,
                EntityAGroupId = contentItem.GroupId,
                EntityAScope = EntityTypeVersioning.DefaultScopeFor(EntityType.ContentItem),
                EntityAContentType = contentItem.ContentType,
                EntityBType = EntityType.Tag,
                EntityBKeyId = tag.Id,
                EntityBGroupId = tag.Id,
                EntityBScope = EntityTypeVersioning.DefaultScopeFor(EntityType.Tag),
                UserId = null,
                ApprovalStatus = ApprovalStatus.Approved,
                IsPublished = true,
                PublishDate = now,
                IsDeleted = false,
                DeletedBy = null,
                CreatedBy = authorUserId,
                CreatedWhen = now,
                UpdatedBy = authorUserId,
                UpdatedWhen = now
            };

            return await this.storageBroker.InsertAssociationAsync(association);
        }

        /// <summary>
        /// Physically removes an association if it is still there, and the approval round its
        /// write opened. A reader's reaction is written at <c>Submitted</c>, so the approval
        /// workflow opens a round on it as the request is served; leaving that round behind would
        /// leave the test's data behind.
        /// </summary>
        public async ValueTask RemoveCoreAssociationByIdAsync(Guid associationId)
        {
            Approval storedApproval =
                await this.storageBroker.SelectApprovalByEntityAsync(
                    EntityType.Association,
                    associationId);

            if (storedApproval is not null)
            {
                await this.storageBroker.DeleteApprovalAsync(storedApproval);
            }

            CoreAssociation storedAssociation =
                await this.storageBroker.SelectAssociationByIdAsync(associationId);

            if (storedAssociation is not null)
            {
                await this.storageBroker.DeleteAssociationAsync(storedAssociation);
            }
        }

        /// <summary>
        /// Removes every association either of whose endpoints is the given content item, with
        /// their approval rounds. A test that posts more than once, or whose response body is
        /// not the result it expects, cannot name every row its posts wrote, so it tears down by
        /// the item it arranged instead. Either endpoint, because the foundation writes the pair
        /// in canonical order rather than the order it was posted in.
        /// </summary>
        public async ValueTask RemoveCoreAssociationsOnContentItemAsync(Guid contentItemId)
        {
            IQueryable<CoreAssociation> allAssociations =
                await this.storageBroker.SelectAllAssociationsAsync();

            List<Guid> associationIds = await allAssociations
                .Where(association =>
                    association.EntityAKeyId == contentItemId
                    || association.EntityBKeyId == contentItemId)
                .Select(association => association.Id)
                .ToListAsync();

            foreach (Guid associationId in associationIds)
            {
                await RemoveCoreAssociationByIdAsync(associationId);
            }
        }
    }
}
