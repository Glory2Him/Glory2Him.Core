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

namespace Glory2Him.Core.Models.Securities
{
    /// <summary>
    /// The actor recorded in <c>CreatedBy</c>, <c>UpdatedBy</c> and <c>DeletedBy</c> when the
    /// system acts on its OWN account — an approval opened because content was submitted, a
    /// round re-approved because its conditions came to be met, an invitation retired because
    /// the person answered it (§7.9 rule 6), a review dismissed because the content moved under
    /// it (§9.5). Nobody asked for any of those; recording the human whose request happened to
    /// be on the stack would name a person who did not act.
    /// </summary>
    ///
    /// <remarks>
    /// <para><b>Why a sentinel and not an account.</b> These columns are <c>nvarchar</c> and hold
    /// whatever the audit pipeline resolves; nothing in the solution parses one as a
    /// <c>Guid</c>, joins it to the identity store, or resolves it to a display name. A real
    /// service account would buy nothing and cost a loginable principal that could be granted
    /// roles by mistake. The precedent is already set in the data: seeding writes
    /// <c>"system-seed"</c>, which stays distinct — startup seeding and a runtime act are
    /// different provenances, and one of them predates the row.</para>
    ///
    /// <para><b>It must never collide with a real account id.</b> True today because ids are
    /// GUIDs, and false the day a host issues human-readable subject ids. That is the one
    /// assumption this constant rests on.</para>
    ///
    /// <para><b>Blank is not an option.</b> The audit client refuses a null or whitespace user
    /// id outright, so §10.7.1's machine context — <c>SubjectId = null</c> — throws on this
    /// path rather than recording a system act. A non-empty token is mandatory.</para>
    /// </remarks>
    public static class SystemIdentity
    {
        /// <summary>
        /// The value stamped into the audit columns. Read by humans far more often than by
        /// code, which is most of what an audit column is for.
        /// </summary>
        public const string UserId = "system";

        /// <summary>
        /// The display name carried alongside it, so a context that renders a username does not
        /// fall back to the triggering person's.
        /// </summary>
        public const string Username = "system";
    }
}
