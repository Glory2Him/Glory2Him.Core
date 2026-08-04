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

using Microsoft.AspNetCore.Components;

namespace Glory2Him.WebApp.Components.CoreUI
{
    public sealed record CommentEntry(
        string AuthorName,
        string? AuthorImageUrl,
        DateTimeOffset PostedAt,
        string Body,
        int Reactions,
        bool IsReply = false);

    public partial class CommentThreadComponent
    {
        [Parameter]
        public IReadOnlyList<CommentEntry> Comments { get; set; } =
            new List<CommentEntry>();

        [Parameter]
        public bool ShowReplyForm { get; set; } = true;

        [Parameter]
        public EventCallback<CommentEntry> OnReact { get; set; }
    }
}
