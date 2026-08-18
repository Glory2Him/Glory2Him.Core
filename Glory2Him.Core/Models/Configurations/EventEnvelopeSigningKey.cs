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

namespace Glory2Him.Core.Models.Configurations
{
    /// <summary>
    /// One entry in the <c>EventEnvelopeSigning</c> configuration section: a symmetric HMAC key and
    /// the window in which it is used for signing. Bound from configuration (appsettings, overridden
    /// by environment variables or a key vault in a deployed host).
    ///
    /// <para>Signing selects the first key whose window contains "now". Verification resolves the
    /// key by <see cref="KeyId"/> alone and ignores the window entirely — a historic event must
    /// still verify after its signing key has been retired, which is what lets keys rotate without
    /// breaking replay.</para>
    /// </summary>
    public sealed class EventEnvelopeSigningKey
    {
        /// <summary>
        /// The stable identifier recorded on every envelope this key signs. Verification looks the
        /// key up by this value; it is never itself signed (it selects the key that would prove it).
        /// </summary>
        public string KeyId { get; init; } = string.Empty;

        /// <summary>
        /// The secret HMAC key material. Kept out of source control — a real value belongs in an
        /// environment variable or key vault, with only a local development key in
        /// <c>appsettings.Development.json</c>.
        /// </summary>
        public string Key { get; init; } = string.Empty;

        /// <summary>
        /// When this key becomes eligible for signing. Mandatory.
        /// </summary>
        public DateTimeOffset ActiveFrom { get; init; }

        /// <summary>
        /// When this key stops being eligible for signing. Optional — an open-ended key signs until a
        /// later key's window overtakes it. Never consulted during verification.
        /// </summary>
        public DateTimeOffset? ActiveTo { get; init; }

        // What this key is allowed to attest. Defaults to General, so a key configured without
        // a stated purpose cannot grant workflow authority by omission (design §16.7.1).
        public EnvelopeSigningPurpose Purpose { get; init; } = EnvelopeSigningPurpose.General;
    }
}
