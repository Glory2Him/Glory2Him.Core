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

using System.Threading.Tasks;
using Glory2Him.Core.Models.Events;

namespace Glory2Him.Core.Brokers.Integrities
{
    /// <summary>
    /// Signs and verifies event envelopes so tampering with a stored event — an added role, a
    /// modified payload, or a message that never went through the proper code path — is detectable
    /// on receive. Symmetric HMAC over a shared secret: the envelope is internal-only, so there is
    /// no external verifier to need asymmetric keys (design §14.6 rule 4, §5.10 of EventSubstrate.md).
    ///
    /// <para><b>The implementation refuses to construct on a host with no signing key configured</b>,
    /// so nothing that signs can be resolved and every endpoint behind one answers 500. That has to
    /// happen at construction rather than at the first publish: every foundation service commits its
    /// row before it mints and signs the fact announcing it, so a throw deferred to signing time
    /// strands the write it was refusing (#392).</para>
    /// </summary>
    internal interface IEnvelopeIntegrityBroker
    {
        /// <summary>
        /// Produces the integrity signature for an envelope about to be published to
        /// <paramref name="eventName"/> as <paramref name="direction"/>. Both are bound into the
        /// signature so the result is valid for exactly one destination and one leg of the exchange.
        /// Throws when no signing key is currently active — signing must fail closed rather than
        /// emit an unsigned envelope.
        /// </summary>
        ValueTask<EnvelopeIntegrity> SignAsync<T>(
            EventEnvelope<T> envelope,
            string eventName,
            EnvelopeDirection direction);

        /// <summary>
        /// Returns whether an envelope's signature is valid for the name and direction the receiver
        /// expects. False for a bad signature, a missing one, an unknown key id, or a name/direction
        /// that does not match what was signed. Never throws on a bad envelope — an unverifiable
        /// envelope is a false, not an exception.
        /// </summary>
        ValueTask<bool> VerifyAsync<T>(
            EventEnvelope<T> envelope,
            string expectedEventName,
            EnvelopeDirection expectedDirection);
    }
}
