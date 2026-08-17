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

namespace Glory2Him.Core.Models.Events.Foundations
{
    /// <summary>
    /// The operations a <c>ContentItem</c> event can represent — requests (present tense:
    /// <see cref="Adding"/>, <see cref="Modifying"/>, <see cref="RemovingById"/>,
    /// <see cref="RetrievingById"/>) answered by responder handlers, and facts (past tense:
    /// <see cref="Added"/>, <see cref="Modified"/>, <see cref="Removed"/>) published by
    /// the service after the work is done. Every operation maps to its own event address (for
    /// example <c>ContentItem-Adding</c>) and composes the stored event name (for example
    /// <c>"ContentItemAdding"</c>). Entity-specific operations may be appended here (with a
    /// matching event address in <c>EventBrokerIdentifiers</c>) without affecting any other
    /// entity.
    /// </summary>
    public enum ContentItemEventOperation
    {
        Adding,
        Modifying,
        RemovingById,
        HardRemovingById,
        RetrievingById,

        // The narrow state-transition requests (design §9.7.1). Each moves one field group
        // the general modify does not own — Submitting moves the approval status Draft →
        // Submitted, Approving decides a submission — and each answers on its own event
        // address so it can be authorized in its own right (§8.6.1, §14.6).
        Submitting,
        Approving,
        Added,
        Modified,
        Removed,
        HardRemoved,

        // The facts the transitions publish. Submitted and Approved follow the operation;
        // Rejected follows the DECISION — an approve that rejects publishes Rejected, not
        // Approved, so a subscriber keying on the fact name is never told the opposite of
        // what happened.
        Submitted,
        Approved,
        Rejected,

        // The version fork's demotion of the previous latest row (§3.4 rule 18, §9.7.1 rule 2).
        // A FACT with no request address behind it, which is deliberate: the demotion is a step
        // inside the fork rather than an act of its own, and a public request address would let
        // a caller demote a version without creating its replacement — leaving a GroupId with no
        // IsLatestVersion = true row and therefore no edit tip at all.
        //
        // It publishes here rather than on Modified so a demotion cannot be read as a content
        // amendment. §10.17 rule 2 tolerated the Modified by having the approval workflow
        // subscribe to the top-layer fact instead; announcing the demotion honestly is stricter
        // than relying on nobody listening.
        Demoted
    }
}
