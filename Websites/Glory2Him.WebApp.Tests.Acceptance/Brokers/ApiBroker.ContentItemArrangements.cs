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
using System.Threading.Tasks;
using Glory2Him.Core.Models.Enums;
using CoreContentItem = Glory2Him.Core.Models.Foundations.ContentItems.ContentItem;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    /// <summary>
    /// ContentItem rows arranged and torn down beneath HTTP, for state no endpoint can produce —
    /// the sibling of <c>ApiBroker.TagArrangements.cs</c>, and the same shape for the same
    /// reasons.
    /// </summary>
    public partial class ApiBroker
    {
        public async ValueTask<CoreContentItem> InsertSubmittedContentItemAsync(string authorUserId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var contentItem = new CoreContentItem
            {
                Id = Guid.NewGuid(),

                // A version group of one. GroupId and Version are control fields the caller
                // never supplies (§12.4.1 rule 6) — which is exactly why an arrangement that
                // writes them has to go through the storage broker rather than the endpoint.
                GroupId = Guid.NewGuid(),
                Version = 1,

                ContentType = ContentType.Story,
                Title = $"Acceptance content item {Guid.NewGuid():N}",
                Author = "Acceptance suite",
                Content = $"Arranged by the acceptance suite {Guid.NewGuid():N}",

                // Required and derived from the content (§3.4.2). Nothing recomputes it on this
                // path, so a distinct value per arrangement is what keeps the duplicate-content
                // probe from matching two unrelated fixtures.
                ContentHash = Guid.NewGuid().ToString("N"),

                ApprovalStatus = ApprovalStatus.Submitted,
                IsPublished = false,
                IsDeleted = false,
                CreatedBy = authorUserId,
                CreatedWhen = now,
                UpdatedBy = authorUserId,
                UpdatedWhen = now
            };

            return await this.storageBroker.InsertContentItemAsync(contentItem);
        }

        public async ValueTask<CoreContentItem> GetCoreContentItemByIdAsync(Guid contentItemId) =>
            await this.storageBroker.SelectContentItemByIdAsync(contentItemId);

        public async ValueTask RemoveCoreContentItemAsync(CoreContentItem contentItem) =>
            await this.storageBroker.DeleteContentItemAsync(contentItem);

        /// <summary>
        /// Physically removes a contentItem if it is still there, whatever state it is in. Every
        /// acceptance test finishes with this so the database is left as it was found: the API's
        /// own delete is a SOFT delete, so a test that tore down through the endpoint still left
        /// its row behind, and a test whose assertion threw left a live one.
        /// </summary>
        public async ValueTask RemoveCoreContentItemByIdAsync(Guid contentItemId)
        {
            CoreContentItem storedContentItem =
                await this.storageBroker.SelectContentItemByIdAsync(contentItemId);

            if (storedContentItem is not null)
            {
                await this.storageBroker.DeleteContentItemAsync(storedContentItem);
            }
        }

        /// <summary>
        /// Writes one version into a group, at whatever state the caller names.
        ///
        /// <para>Arranged beneath HTTP because it has to be: <c>GroupId</c> and <c>Version</c> are
        /// control fields the add derives (§12.4.1 rule 6), and the approve transition that would
        /// publish a row arrives as an event rather than an endpoint. A suite that could only use
        /// the API could never build a group with more than one version, which is the only shape
        /// the group reads are interesting on.</para>
        /// </summary>
        public async ValueTask<CoreContentItem> InsertContentItemVersionAsync(
            Guid groupId,
            int version,
            ApprovalStatus approvalStatus,
            bool isPublished,
            string authorUserId,
            bool isDeleted = false,
            ContentType contentType = ContentType.Story)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var contentItem = new CoreContentItem
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                Version = version,
                ContentType = contentType,
                Title = $"Version {version} of {groupId:N}",
                Author = "Acceptance suite",
                Content = $"Version {version} body {Guid.NewGuid():N}",
                ContentHash = Guid.NewGuid().ToString("N"),
                ApprovalStatus = approvalStatus,
                IsPublished = isPublished,
                PublishDate = isPublished ? now : null,

                // A TAKEDOWN, when a test asks for one. Removal never touches ApprovalStatus
                // (§9.7.6), so a row inserted deleted keeps whatever status it was given - which
                // is exactly the shape the approval-side gates have to refuse.
                IsDeleted = isDeleted,
                DeletedBy = isDeleted ? authorUserId : null,
                DeletedWhen = isDeleted ? now : null,
                CreatedBy = authorUserId,
                CreatedWhen = now,
                UpdatedBy = authorUserId,
                UpdatedWhen = now
            };

            return await this.storageBroker.InsertContentItemAsync(contentItem);
        }
    }
}
