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

namespace Glory2Him.Core.Models.Configurations
{
    /// <summary>
    /// What a signing key is allowed to attest (design §16.7.1).
    ///
    /// <para>Provenance is not carried by the payload — a caller who can put a message on a
    /// public address can set any property on it, including a claim to be the workflow. The
    /// claim is made trustworthy by the KEY that signed it: the workflow holds one no ordinary
    /// publisher has, and a receiver honours <c>IsSystemIdentity</c> only on an envelope signed
    /// with it.</para>
    /// </summary>
    public enum EnvelopeSigningPurpose
    {
        /// <summary>
        /// The ordinary key. Signs envelopes carrying a human caller's identity, and may not
        /// attest a system identity. This is the default, so a key added to configuration
        /// without a stated purpose can never grant workflow authority by omission.
        /// </summary>
        General = 0,

        /// <summary>
        /// The workflow's own key. The only key whose signature makes
        /// <c>SecurityContext.IsSystemIdentity</c> believable, and the reason the approval
        /// workflow can drive an entity write that no human is permitted to make — the
        /// automatic approval fired by a reviewer's own review (§8.6 regardless-rule 1), and
        /// the demotion of a published sibling that is itself <c>Approved</c>.
        /// </summary>
        Workflow = 1,
    }
}
