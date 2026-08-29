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

namespace Glory2Him.Core.Models.Orchestrations.Approvals
{
    /// <summary>
    /// One person who may be invited to review an approval (design 16.7.4).
    ///
    /// <para><b>Two fields, and the shortness is the design.</b> This is a user-enumeration
    /// surface, so it carries the minimum a picker needs and nothing a caller could mine: no
    /// email, no roles, no account state. A moderator learns that somebody is invitable, which
    /// they would learn anyway the moment they invited them.</para>
    ///
    /// <para>It answers "who belongs to this round", not "who is left to ask". Only the entity's
    /// own author is removed, because rule 3 refuses an invitation aimed at them outright. People
    /// who have already ANSWERED, and people already invited, are deliberately IN - a moderation
    /// surface renders them inert and under their own heading, so somebody searching for a name
    /// finds them and learns their state. It cannot show a person it was never sent.</para>
    /// </summary>
    public class ReviewerCandidate
    {
        /// <summary>
        /// The account id, and the ONLY identity here. It is what an invitation stores in
        /// RequestedUserId and what an answering review is matched against.
        /// </summary>
        public required string UserId { get; init; }

        /// <summary>
        /// What to show in the picker - the preferred name, else the full name, else the
        /// username. Presentation only: nothing compares it, and it is denormalised onto the
        /// invitation at request time (7.9) rather than re-read later.
        /// </summary>
        public required string DisplayName { get; init; }
    }
}
