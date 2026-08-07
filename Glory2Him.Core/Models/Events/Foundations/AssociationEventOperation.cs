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
    /// The operations a <c>Association</c> event can represent — requests (present
    /// tense: <see cref="Adding"/>, <see cref="Modifying"/>, <see cref="RemovingById"/>,
    /// <see cref="HardRemovingById"/>, <see cref="RetrievingById"/>) answered by responder
    /// handlers, and facts (past tense: <see cref="Added"/>, <see cref="Modified"/>,
    /// <see cref="Removed"/>, <see cref="HardRemoved"/>) published by the service after the
    /// work is done. Every request operation maps to its own event address (for example
    /// <c>Association-Adding</c>) and composes the stored event name (for example
    /// <c>"AssociationAdding"</c>). <see cref="HardRemoved"/> shares the
    /// <see cref="Removed"/> event address and is distinguished purely by its event name
    /// (<c>"AssociationHardRemoved"</c>). Entity-specific operations may be appended
    /// here (with a matching event address in <c>EventBrokerIdentifiers</c>) without affecting
    /// any other entity.
    /// </summary>
    public enum AssociationEventOperation
    {
        Adding,
        Modifying,
        RemovingById,
        HardRemovingById,
        RetrievingById,
        Added,
        Modified,
        Removed,
        HardRemoved,

        // The narrow state-transition operations (design §9.7.1, §9.2). Each owns one field
        // group, and each publishes its OWN fact rather than Modified. That is the whole
        // point: the approval workflow subscribes to Modified and causes Approved, so a
        // transition that published Modified would re-enter the handler that caused it —
        // synchronously, under inline dispatch, inside the originating request.
        // Sort has NO request address. Its signature takes an anchor and a side, and an
        // EventEnvelope<Association> carries exactly one entity — there is nowhere to put the
        // anchor. Rather than invent a second-entity channel or collapse the anchor into a
        // raw SortOrder value (which is the design the anchor exists to avoid), sort is
        // direct-call only and publishes its fact like the others.
        Submitting,
        Approving,
        SettingConfidence,
        SettingScope,
        Submitted,
        Approved,
        Sorted,
        ConfidenceSet,
        Scoped
    }
}
