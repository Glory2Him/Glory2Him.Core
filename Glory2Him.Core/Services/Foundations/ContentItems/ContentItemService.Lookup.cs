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
using Glory2Him.Core.Models.Foundations.ContentItems;

namespace Glory2Him.Core.Services.Foundations.ContentItems
{
    internal partial class ContentItemService
    {
        public ValueTask<Guid?> FindPublishedContentItemIdByGroupAsync(
            Guid groupId,
            Guid excludedContentItemId,
            CancellationToken cancellationToken = default) =>
            TryCatchIdentifier(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // The probe carries no entity, so the request exists only to anchor the ambient
                // security context the contribution gate runs against — a write-flow primitive,
                // not a public read. Same shape the approval probe uses.
                var findRequest = new ContentItem { GroupId = groupId };

                EventEnvelope<ContentItem> envelope =
                    await this.eventEnvelopeBroker.CreateAsync(content: findRequest);

                ValidateUserIsAllowedToContribute(envelope.SecurityContext);
                ValidateOnFindPublishedContentItemByGroup(groupId);

                // Deliberately UNFILTERED, and this is the whole reason the probe exists rather
                // than the publication swap reusing RetrieveAllContentItemsAsync. That read applies the
                // visibility filter, whose first clause drops IsDeleted rows — but a soft delete
                // never clears IsPublished, and the slot index names that column ALONE. So a
                // tombstone still occupies the group's published slot while being invisible to
                // every caller-facing read.
                //
                // A filtered probe therefore reports "no incumbent", the swap skips the demote,
                // and the promote is refused by the unique index — permanently, for every future
                // approval in that group (§9.7.7 rule 7).
                //
                // Only an id crosses back. That reveals nothing a caller could not already infer
                // from the group having a published version.
                IQueryable<ContentItem> allContentItems =
                    await this.storageBroker.SelectAllContentItemsAsync(cancellationToken);

                ContentItem? publishedContentItem = allContentItems.FirstOrDefault(contentItem =>
                    contentItem.GroupId == groupId
                        && contentItem.IsPublished
                        && contentItem.Id != excludedContentItemId);

                return publishedContentItem?.Id;
            });

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

    }
}
