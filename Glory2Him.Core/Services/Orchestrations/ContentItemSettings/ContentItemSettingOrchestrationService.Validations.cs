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
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;
using Glory2Him.Core.Models.Orchestrations.ContentItemSettings.Exceptions;

namespace Glory2Him.Core.Services.Orchestrations.ContentItemSettings
{
    internal partial class ContentItemSettingOrchestrationService
    {
        // The item's own service answers whether it exists and whether this caller may see it
        // (§16.6), so the read carries the visibility posture rather than reinventing it. Its
        // not-found has already been logged there; to this flow the endpoint simply did not
        // resolve, and the caller is told that and not which of the two reasons applied.
        //
        // A store that could not ANSWER is a different thing and must not be mistaken for a
        // missing row — IsContentItemNotFound lets those through to the dependency clause in the
        // .Exceptions partial, where they keep their category.
        private async ValueTask<ContentItem> ResolveContentItemAsync(
            Guid contentItemId,
            CancellationToken cancellationToken)
        {
            try
            {
                return await this.contentItemService.RetrieveContentItemByIdAsync(
                    contentItemId,
                    cancellationToken);
            }
            catch (Exception contentItemException)
                when (IsContentItemNotFound(contentItemException))
            {
                throw new NotFoundContentItemSettingOrchestrationException(
                    message: $"The content item was not found with id: {contentItemId}.");
            }
        }

        private static void ValidateContentItemSettingIsNotNull(ContentItemSetting contentItemSetting)
        {
            if (contentItemSetting is null)
            {
                throw new NullContentItemSettingOrchestrationException(
                    message: "Content item setting is null.");
            }
        }
    }
}
