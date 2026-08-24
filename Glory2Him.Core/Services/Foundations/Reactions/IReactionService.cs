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
    /// <summary>
    /// The reaction foundation service contract. Public — unlike most of its sibling foundation
    /// interfaces — because an exposer binds to it: <c>ReactionsController</c> in the portal host
    /// takes it as its only dependency, and a public controller constructor cannot accept a
    /// less-accessible parameter type. Only the contract is public; <c>ReactionService</c>, the
    /// brokers behind it and the outer exception types stay internal and reach the host through
    /// <c>InternalsVisibleTo</c>, so the implementation remains Core's to change.
    ///
    /// <para>This is also the entity's <b>top-layer</b> service, which is what makes binding an
    /// exposer to a foundation correct here rather than a shortcut (design §10.17 rule 3).
    /// §12.3.1 names <c>Reaction</c> among the entities that need nothing above their foundation:
    /// it touches one entity type, so there is nothing to orchestrate, and it is Single-Row
    /// (§7.5.1), so there is no version fork and the approval workflow subscribes to these
    /// foundation facts directly. The <c>ReactionOrchestration</c> of the old §12.5 entry 5 is
    /// withdrawn and is not coming.</para>
    /// </summary>
    public partial interface IReactionService
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

        ValueTask<Reaction> TransitionReactionApprovalAsync(
            Reaction reaction,
            CancellationToken cancellationToken = default);
    }
}
