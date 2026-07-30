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
using System.Collections.Generic;

namespace Glory2Him.Core.Models.Configurations
{
    /// <summary>
    /// Fixed, well-known identifiers for the event substrate: the Glory 2 Him participant and
    /// one event address per entity operation (for example <c>ContentItem-Adding</c>). Each
    /// entity's identifiers live in its own partial (<c>EventBrokerIdentifiers.[Entity].cs</c>);
    /// this file holds the participant and composes the flat address map used for
    /// registration. These identifiers are persisted in the event store, so they must never
    /// change once deployed; registration is idempotent on these values.
    /// </summary>
    public static partial class EventBrokerIdentifiers
    {
        public static readonly Guid Glory2HimParticipantId =
            new Guid("019f814e-89c0-70a2-9587-2701065a097d");

        internal static readonly IReadOnlyDictionary<Guid, string> EventAddresses;

        // runs after every partial's field initializers, so all per-entity maps are populated
        static EventBrokerIdentifiers()
        {
            var eventAddresses = new Dictionary<Guid, string>();

            var entityEventAddresses = new[]
            {
                ApprovalEventAddresses,
                ApprovalCommentEventAddresses,
                ApprovalReviewEventAddresses,
                ApprovalSettingEventAddresses,
                ApprovalSettingPublisherRoleEventAddresses,
                ApprovalSettingReviewerRoleEventAddresses,
                AttachmentEventAddresses,
                BibleReferenceEventAddresses,
                CommentEventAddresses,
                ContentItemEventAddresses,
                ContentItemSubmissionEventAddresses,
                ContentItemAssociationEventAddresses,
                ContentItemSettingEventAddresses,
                ContentTypeEventAddresses,
                LinkEventAddresses,
                ReactionEventAddresses,
                TagEventAddresses
            };

            foreach (IReadOnlyDictionary<Guid, string> entityAddresses in entityEventAddresses)
            {
                foreach (KeyValuePair<Guid, string> eventAddress in entityAddresses)
                    eventAddresses.Add(eventAddress.Key, eventAddress.Value);
            }

            EventAddresses = eventAddresses;
        }
    }
}
