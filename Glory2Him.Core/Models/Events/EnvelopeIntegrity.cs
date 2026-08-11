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
    /// The cryptographic integrity details of an <see cref="EventEnvelope{T}"/> — an HMAC over the
    /// event name, direction, content, security context, request context and metadata, produced on
    /// publish and checked on receive to detect tampering with a stored event.
    /// </summary>
    public sealed class EnvelopeIntegrity
    {
        /// <summary>
        /// The name of the algorithm used to compute the signature. Recorded for documentation only:
        /// a verifier <b>never</b> reads this to decide how to check the signature — it uses its own
        /// configured algorithm, so a forged <c>"none"</c> here cannot downgrade the check.
        /// </summary>
        public string Algorithm { get; init; } = "HMACSHA256";

        /// <summary>
        /// The identifier of the signing key (see <c>EventEnvelopeSigningKey</c>). Verification uses
        /// it to select which key to check against, so historic events survive key rotation. Swapping
        /// it to another key's id simply makes verification fail — the recomputed signature will not
        /// match — so it does not need to be signed itself.
        /// </summary>
        public string KeyId { get; init; } = string.Empty;

        /// <summary>
        /// The computed signature of the envelope's signed portion, used to detect tampering.
        /// </summary>
        public string Signature { get; init; } = string.Empty;

        /// <summary>
        /// The date and time at which the envelope was signed.
        /// </summary>
        public DateTimeOffset SignedDate { get; init; }
    }
}
