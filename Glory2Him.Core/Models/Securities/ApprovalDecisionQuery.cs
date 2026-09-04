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
using G2H.Security.Client.Models.Foundations.Access;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Events;

namespace Glory2Him.Core.Models.Securities
{
    /// <summary>
    /// What a foundation service supplies when it asks whether the caller may apply an approval
    /// decision. The broker fills in everything else — the policy rows, the approval's state, its
    /// reviews and its comments — because those live on other entities and a foundation service
    /// serves one.
    ///
    /// <para>Every property is <c>required</c> for the same reason the client's own requests are:
    /// each one is consulted by a rule, and a value quietly defaulted is a rule quietly weakened.
    /// Here it also stops a caller passing the wrong entity's author by omission.</para>
    /// </summary>
    internal class ApprovalDecisionQuery
    {
        /// <summary>
        /// The type of the entity being decided on — its <b>own</b> type, which is also the
        /// entity-type half of the policy key. For an association this is
        /// <see cref="EntityType.Association"/>, never an endpoint's type.
        /// </summary>
        public required EntityType EntityType { get; init; }

        /// <summary>
        /// The row being decided on. Paired with <see cref="EntityType"/> this locates the
        /// <c>Approval</c>, which is the only route from an entity to its reviews.
        /// </summary>
        public required Guid EntityId { get; init; }

        /// <summary>
        /// The content-type half of the policy key, or null for the entity-type default tier.
        /// Null for every entity type but <c>ContentItem</c>.
        /// </summary>
        public required ContentType? ContentType { get; init; }

        /// <summary>
        /// The personality half of the policy key (design §8.4): for an association, whether
        /// its <c>UserId</c> is set; null for every other entity type, where the tier does not
        /// exist. From STORAGE, like the author — the caller's copy does not decide which
        /// policy judges it.
        /// </summary>
        public required bool? IsPersonal { get; init; }

        /// <summary>
        /// Every subject the caller could be authorised through. Most entities name one, built
        /// from their own type; an association names both endpoints, because it is authorised
        /// from them rather than from itself.
        /// </summary>
        public required IReadOnlyList<RoleSubject> RoleSubjects { get; init; }

        /// <summary>
        /// The <c>CreatedBy</c> of the entity itself. Supplied by the caller rather than resolved
        /// here because the caller already holds the storage row — and because the
        /// <c>Approval</c> record's own <c>CreatedBy</c> is the submitter, which is a different
        /// question and would make the self-approval bar quietly wrong when they differ.
        /// </summary>
        public required string EntityCreatedBy { get; init; }

        /// <summary>
        /// The entity's confidence score, or null when it has none or has not been scored.
        /// </summary>
        public required decimal? ConfidenceScore { get; init; }

        /// <summary>
        /// Which way the caller is moving the approval. Approving and rejecting are gated
        /// differently.
        /// </summary>
        public required ApprovalDecision Decision { get; init; }

        /// <summary>
        /// Whether the caller is approving <i>over</i> unmet conditions rather than because they
        /// are met.
        /// </summary>
        public required bool IsBypassRequested { get; init; }

        /// <summary>
        /// The reason recorded alongside a bypass. A bypass without one is refused.
        /// </summary>
        public required string? BypassReason { get; init; }

        /// <summary>
        /// The acting caller's context, taken from the inbound envelope so the rules hold on the
        /// event path as well as the direct one.
        /// </summary>
        public required SecurityContext SecurityContext { get; init; }
    }
}
