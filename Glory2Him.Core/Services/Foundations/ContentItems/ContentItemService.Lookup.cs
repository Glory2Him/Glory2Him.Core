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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.ContentItems.Exceptions;
using Glory2Him.Core.Models.Foundations.ContentItems;

namespace Glory2Him.Core.Services.Foundations.ContentItems
{
    internal partial class ContentItemService
    {
        public ValueTask<int> FindHighestVersionInGroupAsync(
            Guid groupId,
            CancellationToken cancellationToken = default) =>
            TryCatchVersion(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var findRequest = new ContentItem { GroupId = groupId };

                EventEnvelope<ContentItem> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: findRequest);

                ValidateUserIsAllowedToContribute(envelope.SecurityContext);
                ValidateOnFindHighestVersionInGroup(groupId);

                // UNFILTERED, and for a different reason than the tip is filtered. Which row may
                // be EDITED is a question about live rows — nobody amends a tombstone. Which
                // version number is FREE is a question about every row that has ever existed,
                // because the unique index on (GroupId, Version) carries no IsDeleted filter: a
                // soft-deleted row still owns its number.
                //
                // Conflating the two was issue #271. The tip check skips tombstones, so a live v1
                // under a soft-deleted v2 looked like the tip; the fork then numbered its
                // successor v2 and collided, failing every fork in that group from then on.
                //
                // A lineage is not renumbered by removing a row from it — the same argument
                // §9.7.7 rule 7 records for the published slot.
                IQueryable<ContentItem> allContentItems =
                    await this.storageBroker.SelectAllContentItemsAsync(cancellationToken);

                var groupVersions = allContentItems
                    .Where(contentItem => contentItem.GroupId == groupId)
                    .Select(contentItem => contentItem.Version)
                    .ToList();

                return groupVersions.Count is 0
                    ? 0
                    : groupVersions.Max();
            });


        // The swap's single probe, replacing a caller-FILTERED read plus a group lookup. Gated
        // and unfiltered, it resolves the target's group off the stored row and returns the
        // incumbent holding that group's slot. The gate is the same contribution check its
        // sibling runs, and it admits the workflow's system identity because that check blocks
        // roles rather than requiring them (#291).
        public ValueTask<Guid?> FindPublishedSiblingContentItemIdAsync(
            Guid contentItemId,
            EventEnvelope<ContentItem> inboundEnvelope,
            CancellationToken cancellationToken = default) =>
            TryCatchIdentifier(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // The envelope is READ, not chained. Every other envelope-taking entry point
                // on this service calls CreateNextAsync first, because each of them writes and
                // publishes a fact that has to hang off the caller's causation link. This one
                // publishes nothing and records no ProcessedEvents row, so there is no link to
                // extend — minting a successor here would only produce an envelope used for the
                // SecurityContext already in hand.
                ValidateUserIsAllowedToContribute(inboundEnvelope.SecurityContext);
                ValidateOnFindPublishedSiblingContentItem(contentItemId);

                // The group comes from the STORED row. A caller-supplied GroupId would let one
                // group's approval unpublish another group's live row.
                ContentItem maybeContentItem = await this.storageBroker.SelectContentItemByIdAsync(
                    contentItemId: contentItemId,
                    cancellationToken: cancellationToken);

                ValidateStorageContentItem(maybeContentItem, contentItemId);

                if (maybeContentItem.IsDeleted)
                {
                    throw new NotFoundContentItemException(
                        message: $"Content item not found with id: {contentItemId}.");
                }

                IQueryable<ContentItem> allContentItems =
                    await this.storageBroker.SelectAllContentItemsAsync(cancellationToken);

                // UNFILTERED on the incumbent side too: a soft delete never clears
                // IsPublished and the slot index names that column alone, so a tombstone still
                // holds the slot. Skipping it would leave the group permanently unpublishable.
                ContentItem? publishedContentItem = allContentItems.FirstOrDefault(contentItem =>
                    contentItem.GroupId == maybeContentItem.GroupId
                        && contentItem.IsPublished
                        && contentItem.Id != contentItemId);

                return publishedContentItem?.Id;
            });

        private static void ValidateOnFindPublishedSiblingContentItem(Guid contentItemId) =>
            Validate(
                message: "Content item is invalid, fix the errors and try again.",
                (Rule: IsInvalid(contentItemId), Parameter: nameof(ContentItem.Id)));
    }
}
