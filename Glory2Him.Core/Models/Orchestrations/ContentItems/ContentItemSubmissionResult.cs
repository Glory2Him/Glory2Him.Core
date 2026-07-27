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

using Glory2Him.Core.Models.Foundations.ContentItems;

namespace Glory2Him.Core.Models.Orchestrations.ContentItems
{
    /// <summary>
    /// Outcome of a content item submission. When the normalized content duplicates an
    /// existing non-deleted item of the same content type (design §3.4.2), no record is
    /// created: <see cref="IsCreated"/> is <c>false</c>, <see cref="ContentItem"/> is
    /// <c>null</c> and only the polite acknowledgement in <see cref="Message"/> is
    /// returned — the duplicate is never revealed to the caller.
    /// </summary>
    public class ContentItemSubmissionResult
    {
        public bool IsCreated { get; set; }
        public ContentItem? ContentItem { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
