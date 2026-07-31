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
    /// one event address per service operation. Each service's identifiers live in its own
    /// partial (<c>EventBrokerIdentifiers.[Subject].cs</c>); this file holds the participant
    /// and composes the flat address map used for registration. These identifiers are
    /// persisted in the event store, so they must never change once deployed; registration is
    /// idempotent on these values.
    ///
    /// <para><b>Address naming.</b> An address is <c>&lt;Subject&gt;-&lt;Verb&gt;</c>, where the
    /// subject is the <i>owning service</i> — its class name minus the <c>Service</c> suffix —
    /// and tense encodes direction: present participle (<c>-ing</c>) is a request the service
    /// receives, past tense (<c>-ed</c>) is a fact it publishes. So
    /// <c>ContentItemService</c> owns <c>ContentItem-Adding</c> / <c>ContentItem-Added</c>, and
    /// <c>ContentItemOrchestrationService</c> owns <c>ContentItemOrchestration-Adding</c> /
    /// <c>ContentItemOrchestration-Added</c>.</para>
    ///
    /// <para>Because the subject carries the service, the verb is free to stay the standard CRUD
    /// set (<c>Adding</c>, <c>Modifying</c>, <c>RemovingById</c>, <c>HardRemovingById</c>,
    /// <c>RetrievingById</c>) at every layer — no verb has to be invented to dodge a collision.
    /// A non-CRUD verb is introduced only when one service has two operations CRUD cannot
    /// distinguish, which happens when a state transition owns a narrower field scope than a
    /// general modify (<c>Approving</c>/<c>Approved</c>, <c>Publishing</c>/<c>Published</c>).</para>
    ///
    /// <para>Two rules keep the namespace unambiguous. Subjects must be distinct across all
    /// services, because the broker composes the stored event name as
    /// <c>subject + operation</c>. And the <c>On</c> prefix belongs to the receiver <i>method</i>
    /// (<c>OnAddingContentItemAsync</c>) and to the subscription name
    /// (<c>ContentItemOrchestrationService.OnAddingContentItem</c>) — never to the address.</para>
    ///
    /// <para>A fact asserts only the publisher's own unit of work: a foundation <c>-Added</c>
    /// means a row was written, an orchestration <c>-Added</c> means that process completed
    /// with its gates passed. They are distinct facts, so exactly one service publishes any
    /// given address and a higher layer never republishes a lower layer's fact.</para>
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
                ContentItemOrchestrationEventAddresses,
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
