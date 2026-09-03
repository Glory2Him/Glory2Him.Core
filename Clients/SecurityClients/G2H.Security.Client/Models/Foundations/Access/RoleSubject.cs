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

namespace G2H.Security.Client.Models.Foundations.Access
{
    /// <summary>
    /// One thing a granular role can be scoped to — an entity type, optionally narrowed to a
    /// content type. A decision names <b>every</b> subject the actor could be authorised through,
    /// and holding a matching role for any one of them is enough.
    ///
    /// <para>The list exists because not every entity is authorised from itself. An association
    /// is authorised from its <i>two endpoints</i>, so it names both: a publisher trusted with one
    /// end can see both, and the pairing is the thing being decided. A content item names one.
    /// Modelling this as a list rather than a single pair is what lets one decision function serve
    /// both without the caller pre-computing the answer.</para>
    ///
    /// <para>Note this is <b>not</b> the policy key. Which <c>ApprovalSetting</c> row applies is a
    /// separate question answered from the entity's own type (an association's policy is keyed on
    /// <c>Association</c>, never on its endpoints).</para>
    /// </summary>
    public class RoleSubject
    {
        /// <summary>
        /// The entity type name, spelled exactly as it appears in role names — for example
        /// <c>ContentItem</c>, giving <c>ContentItem-Reviewers</c>.
        /// </summary>
        public required string EntityType { get; init; }

        /// <summary>
        /// The content type name when this subject carries one, giving access to the narrow tier
        /// <c>ContentItem-Testimony-Reviewers</c> as well as the coarse <c>ContentItem-Reviewers</c>.
        /// Null when the subject has no content type, which is every entity type but
        /// <c>ContentItem</c>.
        /// </summary>
        public required string? ContentType { get; init; }

        /// <summary>
        /// True when the gatherer could not read the entity this subject describes, so
        /// <see cref="ContentType"/> is <b>unknown</b> rather than absent.
        ///
        /// <para>The two cases look identical on the wire and must not be treated alike. A
        /// <c>Tag</c> subject legitimately carries no content type — only <c>ContentItem</c> has
        /// one (§18.6 rule 5) — and there the narrow tier simply does not exist. A content item
        /// that has been hard-removed out from under its approval carries none either, and there
        /// the narrow tier exists but cannot be decided.</para>
        ///
        /// <para>That distinction only matters to the BLOCK. A grant that cannot be composed
        /// fails closed on its own: the actor is left needing a wider role. A block that cannot
        /// be composed would fail OPEN, handing a sanctioned user back the very approval whose
        /// entity is gone — so the veto reads this flag and refuses instead. Not required, and
        /// false by default: a gatherer that never sets it describes entities it actually
        /// read.</para>
        /// </summary>
        public bool IsEntityUnresolved { get; init; }
    }
}
