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

using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Foundations.ContentItems;
using Glory2Him.Core.Models.Orchestrations.ContentItems;

namespace Glory2Him.Core.Services.Orchestrations.ContentItems
{
    public partial interface IContentItemOrchestrationService
    {
        /// <summary>
        /// Submits a new content item as version 1 of a new group (Flow 1 — Add). The caller
        /// must be authenticated and not blocked by the <c>ReadOnly</c> or
        /// <c>ContentItem-ReadOnly</c> roles. When the normalized content duplicates an
        /// existing non-deleted item of the same content type, nothing is created and the
        /// result carries only the polite acknowledgement (design §3.4.2).
        /// </summary>
        ValueTask<ContentItemSubmissionResult> SubmitContentItemAsync(
            ContentItem contentItem,
            CancellationToken cancellationToken = default);
    }
}
