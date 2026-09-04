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

namespace Glory2Him.Core.Models.Securities
{
    /// <summary>
    /// Everything the invitation flow needs to know about an approval's SUBJECT, gathered in one
    /// read (design 7.9, 16.7.4).
    ///
    /// <para>It exists so the orchestration can answer "who may be invited to review this?"
    /// without holding the seven entity services. Each field is here because a 7.9 rule needs it,
    /// and nothing else crosses back - the row bodies stay inside the broker, the same discipline
    /// ApprovalEntityMatch follows.</para>
    ///
    /// <para>Gather-only: producing one writes nothing, decides nothing and grants nothing. The
    /// decisions it feeds are made above it.</para>
    /// </summary>
    public class ApprovalReviewerScope
    {
        /// <summary>The approval the invitation would hang off.</summary>
        public required Guid ApprovalId { get; init; }

        /// <summary>
        /// The approval's status, which decides the window: 7.9 rule 7 refuses an invitation
        /// unless the round is open.
        /// </summary>
        public required ApprovalStatus ApprovalStatus { get; init; }

        /// <summary>
        /// The entity owner (its CreatedBy), so rule 3 can refuse to invite the person whose work
        /// is under review. Read from the STORED entity rather than from any caller payload -
        /// self-review is exactly what HR-1 exists to stop.
        /// </summary>
        public required string EntityCreatedBy { get; init; }

        /// <summary>
        /// Every subject the review tier could be composed from (18.6). Usually one; an
        /// association names both its endpoints, so a publisher trusted with either end qualifies.
        /// Carrying the subjects rather than a finished list of role names keeps the naming
        /// convention in one place - the caller composes, this only reports what to compose from.
        /// </summary>
        public required IReadOnlyList<RoleSubject> RoleSubjects { get; init; }

        /// <summary>
        /// Who already holds an ACTIVE review on this approval. Rule 4 dissolves an invitation
        /// aimed at one of them - a person who has answered does not need asking - and rule 5
        /// refuses to withdraw the invitation they answered. The candidates read does NOT
        /// subtract them: it describes the round's population, and a surface renders an answered
        /// person inert rather than hiding them.
        ///
        /// <para>Soft-deleted and dismissed reviews are excluded here: a withdrawn or stale
        /// verdict leaves the person invitable again, which is the same reasoning the review
        /// index's filter carries.</para>
        /// </summary>
        public required IReadOnlyList<string> ActiveReviewerUserIds { get; init; }

        /// <summary>
        /// Every account id stamped on a review row of this approval — dismissed and
        /// soft-deleted rows INCLUDED. The superset of <see cref="ActiveReviewerUserIds"/>, and
        /// it exists because that field cannot answer this question: it subtracts exactly the
        /// rows a panel still renders.
        ///
        /// <para>With <see cref="ActiveRequests"/> it is the name resolver's ENTIRE set (16.7.4)
        /// - the tier is not read there, so these ids are the only route by which a past
        /// reviewer gets a name. A reviewer whose verdict was dismissed by a later edit, or
        /// withdrawn, is still shown, so the resolver has to be able to name them, and nothing
        /// about their state changes that the round involved them.</para>
        ///
        /// <para>Never a substitute for <see cref="ActiveReviewerUserIds"/> in the 7.9 rules.
        /// Whether somebody may be invited or their invitation withdrawn turns on a review that
        /// still STANDS; this one only reports who appears on the record.</para>
        /// </summary>
        public required IReadOnlyList<string> RecordedReviewerUserIds { get; init; }

        /// <summary>
        /// The invitations still outstanding on this approval. Rule 4 dissolves a duplicate
        /// rather than colliding with the uniqueness index, and rule 6 finds the row to retire
        /// once its target answers. The candidates read does NOT subtract these people - a
        /// surface shows them under their own heading, which it cannot do if it never receives
        /// them.
        /// </summary>
        public required IReadOnlyList<ActiveReviewRequest> ActiveRequests { get; init; }
    }
}
