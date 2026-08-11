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

namespace G2H.EventEnvelope.Client.Models.Foundations
{
    /// <summary>
    /// The cryptographic integrity details of an <see cref="EventEnvelope{T}"/>.
    ///
    /// <para><b>Unimplemented contract.</b> This type describes the shape a signature would take;
    /// this client neither populates nor checks it. A populated <c>Integrity</c> has still not
    /// been verified by anything, and <c>null</c> — which is every envelope today — is not
    /// thereby suspect.</para>
    /// </summary>
    public sealed class EventEnvelopeIntegrity
    {
        /// <summary>
        /// The name of the algorithm used to compute the signature.
        /// </summary>
        public string Algorithm { get; init; } = "HMACSHA256";

        /// <summary>
        /// The computed signature of the envelope's contents, used to detect tampering.
        /// </summary>
        public string Signature { get; init; } = string.Empty;

        /// <summary>
        /// The date and time at which the envelope was signed.
        /// </summary>
        public DateTimeOffset SignedDate { get; init; }
    }
}
