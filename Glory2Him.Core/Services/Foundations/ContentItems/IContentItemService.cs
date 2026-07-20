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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.ContentItems;

namespace Glory2Him.Core.Services.Foundations.ContentItems
{
    public partial interface IContentItemService
    {
        ValueTask<ContentItem> AddContentItemAsync(
            ContentItem contentItem,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<ContentItem>> RetrieveAllContentItemsAsync(
            CancellationToken cancellationToken = default);

        ValueTask<ContentItem> RetrieveContentItemByIdAsync(
            Guid contentItemId,
            CancellationToken cancellationToken = default);

        ValueTask<ContentItem> ModifyContentItemAsync(
            ContentItem contentItem,
            CancellationToken cancellationToken = default);

        ValueTask<ContentItem> RemoveContentItemByIdAsync(
            Guid contentItemId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default);

        ValueTask<ContentItem> HardRemoveContentItemByIdAsync(
            Guid contentItemId,
            CancellationToken cancellationToken = default);
    }
}
