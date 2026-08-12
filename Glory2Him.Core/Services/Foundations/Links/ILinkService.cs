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
using Glory2Him.Core.Models.Foundations.Links;

namespace Glory2Him.Core.Services.Foundations.Links
{
    internal partial interface ILinkService
    {
        ValueTask<Link> AddLinkAsync(
            Link link,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<Link>> RetrieveAllLinksAsync(
            CancellationToken cancellationToken = default);

        ValueTask<Link> RetrieveLinkByIdAsync(
            Guid linkId,
            CancellationToken cancellationToken = default);

        ValueTask<Link> ModifyLinkAsync(
            Link link,
            CancellationToken cancellationToken = default);

        ValueTask<Link> RemoveLinkByIdAsync(
            Guid linkId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default);

        ValueTask<Link> HardRemoveLinkByIdAsync(
            Guid linkId,
            CancellationToken cancellationToken = default);

        ValueTask<Link> SubmitLinkByIdAsync(
            Guid linkId,
            CancellationToken cancellationToken = default);

        ValueTask<Link> ApproveLinkAsync(
            Link link,
            CancellationToken cancellationToken = default);
    }
}
