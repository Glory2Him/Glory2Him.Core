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
using Glory2Him.Core.Models.Foundations.Reactions;

namespace Glory2Him.Core.Services.Foundations.Reactions
{
    internal partial interface IReactionService
    {
        ValueTask<Reaction> AddReactionAsync(
            Reaction reaction,
            CancellationToken cancellationToken = default);

        ValueTask<IQueryable<Reaction>> RetrieveAllReactionsAsync(
            CancellationToken cancellationToken = default);

        ValueTask<Reaction> RetrieveReactionByIdAsync(
            Guid reactionId,
            CancellationToken cancellationToken = default);

        ValueTask<Reaction> ModifyReactionAsync(
            Reaction reaction,
            CancellationToken cancellationToken = default);

        ValueTask<Reaction> RemoveReactionByIdAsync(
            Guid reactionId,
            string? deletionReason = null,
            CancellationToken cancellationToken = default);

        ValueTask<Reaction> HardRemoveReactionByIdAsync(
            Guid reactionId,
            CancellationToken cancellationToken = default);

        ValueTask<Reaction> SubmitReactionByIdAsync(
            Guid reactionId,
            CancellationToken cancellationToken = default);

        ValueTask<Reaction> ApproveReactionAsync(
            Reaction reaction,
            CancellationToken cancellationToken = default);
    }
}
