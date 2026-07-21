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

namespace Glory2Him.Core.Models.Events
{
    /// <summary>
    /// Describes a durable event subscription registered through the <c>EventBroker</c>.
    /// Every subscription is declared centrally in <c>EventSubscriptionRegistration</c> so the
    /// full set of configured subscriptions is always visible in one place.
    /// </summary>
    /// <remarks>
    /// <see cref="Id"/> and <see cref="Name"/> must be stable, fixed values (not generated at
    /// runtime). Registration is idempotent on <see cref="Id"/>: re-running registration reuses
    /// the existing listener instead of creating a duplicate. Because registration is
    /// retrieve-or-register, changing <see cref="Name"/>, <see cref="Description"/>, or the
    /// operation passed on subscribe for an existing <see cref="Id"/> does not update the
    /// stored listener; assign a new <see cref="Id"/> (and remove the old listener) to change
    /// a subscription's shape.
    /// </remarks>
    public sealed class EventSubscription
    {
        /// <summary>
        /// The stable, unique identifier of this subscription. Used as both the event listener
        /// and event handler identity in the event substrate.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// The stable, globally unique name of this subscription, for example
        /// <c>"Approval.ContentItemAdded"</c>.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// A short description of what this subscription does. Required by the event substrate.
        /// </summary>
        public string Description { get; init; } = string.Empty;
    }
}
