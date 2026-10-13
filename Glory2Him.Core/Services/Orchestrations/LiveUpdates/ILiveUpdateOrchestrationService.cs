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
using Glory2Him.Core.Models.Events;
using Glory2Him.Core.Models.Foundations.Associations;
using Glory2Him.Core.Models.Foundations.ContentItemSettings;

namespace Glory2Him.Core.Services.Orchestrations.LiveUpdates
{
    /// <summary>
    /// The forwarder (§EVN26). It hears the facts that change what an open page shows, and tells
    /// every open page through <c>ILiveUpdateService</c> that a read it holds may be stale
    /// (<c>Backend/Orchestrations/LiveUpdateOrchestrationService.md</c>).
    ///
    /// <para>Every ear verifies the fact it hears before it reads anything, and answers
    /// <c>null</c>, since nothing replies to a fact (§EVN20). Nothing calls it but the
    /// subscriptions <c>EventSubscriptionRegistration</c> binds to it.</para>
    /// </summary>
    internal interface ILiveUpdateOrchestrationService
    {
        /// <summary>
        /// Hears <c>Association-Approved</c>. A reaction on a content item that is now counted
        /// tells every open page that the counts of the item's canonically visible version may
        /// have moved — only where that version exists and its winning setting shows reactions
        /// (§SEC14.8 rules 1 and 3).
        /// </summary>
        ValueTask<EventEnvelope<Association>?> OnAssociationApprovedAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Hears <c>Association-Rejected</c>. A rejection takes a reaction out of a count only
        /// where it overrides an approved one; one of an open round moves nothing, and the page's
        /// read finds the counts as they were. Runs the reactions body
        /// <see cref="OnAssociationApprovedAsync"/> runs.
        /// </summary>
        ValueTask<EventEnvelope<Association>?> OnAssociationRejectedAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Hears <c>Association-Submitted</c>, which the approval workflow's ear hears too. An
        /// approved reaction sent back to <c>Submitted</c> leaves its item's counts until its
        /// round is decided again. Runs the reactions body
        /// <see cref="OnAssociationApprovedAsync"/> runs.
        /// </summary>
        ValueTask<EventEnvelope<Association>?> OnAssociationSubmittedAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Hears <c>Association-Removed</c>, under either name the address carries:
        /// <c>AssociationRemoved</c> or <c>AssociationHardRemoved</c> (§EVN2 rule 4). A removed
        /// reaction leaves its item's counts. Runs the reactions body
        /// <see cref="OnAssociationApprovedAsync"/> runs.
        /// </summary>
        ValueTask<EventEnvelope<Association>?> OnAssociationRemovedAsync(
            EventEnvelope<Association> envelope,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Hears <c>ContentItemSetting-Added</c>. An override added for one item tells every open
        /// page that the settings it shows may have changed, naming no content item: every setting
        /// row is public-read (§SEC14.7 posture C rule 2).
        /// </summary>
        ValueTask<EventEnvelope<ContentItemSetting>?> OnContentItemSettingAddedAsync(
            EventEnvelope<ContentItemSetting> envelope,
            CancellationToken cancellationToken = default);
    }
}
